interface JsReference extends JsValue {
  readonly type: JsTypes;
  readonly value: number;
  readonly reference: object | string;
}

class JsSpecialReference implements JsReference {
  readonly type: JsTypes;
  readonly value: number;
  readonly runtime: RuntimeContext;
  // data used by the different special references.
  readonly data: SharedArrayData | Array<string>;
  weakRef?: WeakRef<object>;

  constructor(runtime: RuntimeContext, type: JsTypes, value: number, data: any) {
    this.runtime = runtime;
    this.type = type;
    this.value = value;
    this.data = data;
    this.weakRef = undefined;
  }

  get reference(): object {

    let existing = this.weakRef?.deref();
    if (existing) return existing;

    if (!this.runtime.acquireReference(this.value))
      throw new Error("Could not re-acquire reference.")

    var ref: object;
    switch (this.type) {
      case JsTypes.SharedTypedArray:
        ref = this.GetSharedArray();
        break;
      case JsTypes.Callback:
        ref = this.GetCallbackFunction();
        break;
      default:
        throw new Error("Unsupported special type");
    }

    this.weakRef = new WeakRef(ref);
    this.runtime.weakRefFinalizer.register(ref, this.value, ref);
    return ref;
  }

  private GetSharedArray() {
    var data: SharedArrayData = <SharedArrayData>this.data;
    var sharedArray = this.runtime.arrayBuilder(data.typeCode, data.pointer, data.length);
    return sharedArray;
  }

  GetCallbackFunction() {
    // This is not a member function since we want it to be garbage collected.
    var fn = function (this: JsSpecialReference) {
      let rawArgs = arguments;

      let param: JsValue;
      let isParamArray = false;
      let paramNames = <Array<string>>this.data;

      if (paramNames.length == 0) {
        param = Undefined;
      }
      else if (paramNames.length == 1) {
        param = this.runtime.makeRefFrom(rawArgs[0])
      }
      else {
        isParamArray = true;
        // this makes sure that we only return as many params as was requested.
        let array = paramNames.map((_, i) => rawArgs[i]);
        param = this.runtime.makeRefFrom(array);
      }

      // In order to allow the called function to return values, we get C# to invoke a separate function as a return.
      // We use that function to modify a local var
      let retval = Undefined;
      let resultFn = (val: JsValue) => retval = val;

      let callbackResponseId = this.runtime.callbackCounter++;
      this.runtime.callbackResponseRegistry.set(callbackResponseId, resultFn);
      this.runtime.callbackHandler(this.value, callbackResponseId, <number>param.value, param.type, isParamArray);
      this.runtime.callbackResponseRegistry.delete(callbackResponseId);
      var value = this.runtime.getValue(<number>retval.value, retval.type);
      return value;
    }
    return fn.bind(this);

  }
}

interface SharedArrayData {
  typeCode: TypedArrayTypeCode;
  pointer: number;
  length: number
}

const Undefined: JsValue = Object.freeze({ value: 0, type: JsTypes.Undefined });
const Null: JsValue = Object.freeze({ value: 0, type: JsTypes.Null });
const True: JsValue = Object.freeze({ value: 1, type: JsTypes.Bool });
const False: JsValue = Object.freeze({ value: 0, type: JsTypes.Undefined });

class RuntimeContext implements JsRuntime {

  // 0 is reserved for special values (undefined, false, null, etc)
  refercenceCounter = 1;
  callbackCounter = 1;

  weakRefFinalizer = new FinalizationRegistry<number>(refId => this.releaseReference(refId))
  referenceRegistry = new Map<number, JsReference>();
  objectMap = new WeakMap<object, number>();
  primitiveMap = new Map<any, number>();
  callbackResponseRegistry = new Map<number, (r: JsValue) => void>();

  arrayBuilder: ArrayBuilder;
  callbackHandler: JsCallback;
  acquireReference: ReferenceHandler;
  releaseReference: ReferenceHandler;

  constructor(arrayBuilder: ArrayBuilder, callbackHandler: JsCallback, onAcquireCallback: ReferenceHandler, onReleaseCallback: ReferenceHandler) {
    this.arrayBuilder = arrayBuilder;
    this.callbackHandler = callbackHandler;

    this.acquireReference = onAcquireCallback;
    this.releaseReference = onReleaseCallback;

  }

  makeRefFrom(obj: any): JsValue {

    if (obj === null) return Null;
    var type = JsTypes.Undefined;
    var isPrimitive = null;

    switch (typeof obj) {
      // value types:
      case "undefined": return Undefined;
      case "boolean": return obj ? True : False;
      case "number": return { value: obj, type: JsTypes.Number };
      // reference types:
      case "bigint":
        type = JsTypes.BigInt;
        isPrimitive = true;
        break;
      case "string":
        type = JsTypes.String;
        isPrimitive = true;
        break;
      case "symbol":
        type = JsTypes.Symbol;
        isPrimitive = true;
        break;
      case "function":
        type = JsTypes.Function;
        isPrimitive = false;
        break;
      case "object":
        type = JsTypes.Object;
        isPrimitive = false;
        break;
    }

    // check if this object is stored in the cache
    if (!isPrimitive) {
      let objRef = this.objectMap.get(obj);
      if (objRef) return this.referenceRegistry.get(objRef) || Undefined;
    } else {
      let objRef = this.primitiveMap.get(obj);
      if (objRef) return this.referenceRegistry.get(objRef) || Undefined;
    }

    // check for subtypes
    if (Array.isArray(obj)) type = JsTypes.Array;
    else if (ArrayBuffer.isView(obj) && !(obj instanceof DataView)) type = JsTypes.TypedArray;
    else if (typeof obj.then === 'function') type = JsTypes.Promise;

    return this.createReference(type, obj);
  }

  createReference(type: JsTypes, obj: any): JsReference {
    let isPrimitive = !(typeof obj === 'object' || typeof obj === 'function');
    let value = this.refercenceCounter++;
    var holder: JsReference;
    switch (type) {
      case JsTypes.Callback:
      case JsTypes.SharedTypedArray:
        holder = new JsSpecialReference(this, type, value, obj);
        break;
      default:
        holder = { value, type, reference: obj };
        break;
    }
    this.referenceRegistry.set(value, holder);

    if (isPrimitive)
      this.primitiveMap.set(obj, value);
    else
      this.objectMap.set(obj, value);
    return holder;
  }

  getValue(ref: number, type: JsTypes): any {

    switch (type) {
      // value types
      case JsTypes.Undefined: return undefined;
      case JsTypes.Null: return null;
      case JsTypes.Bool: return ref ? true : false;
      case JsTypes.Number: return ref;
    }

    var holder = this.referenceRegistry.get(ref);
    if (!holder) return undefined;

    return holder.reference;
  }


  RespondToCallback(responseRefId: number, value: number, typeId: number): JsValue {
    let fn = this.callbackResponseRegistry.get(responseRefId);
    if (!fn) throw new Error("bad callback response");
    fn({ type: typeId, value: value });
    return Undefined;
  }

  GetGlobalObject(targetRef: number, targetType: number): JsValue {
    var id = this.getValue(targetRef, targetType);
    var globals = <any>globalThis;
    return this.makeRefFrom(globals[id]);
  }

  CreateEmptyObject(): JsValue {
    return this.makeRefFrom({});
  }

  CreateString(str: string): JsValue {
    return this.makeRefFrom(str);
  }

  CreateArray(): JsValue {
    return this.makeRefFrom([]);
  }

  CallSlow(functionRef: number, paramArrayRef: number): JsValue {
    let func = this.getValue(functionRef, JsTypes.Function);
    let params = this.getValue(paramArrayRef, JsTypes.Array);
    let ret = func(...params);
    return this.makeRefFrom(ret);
  }

  Call(functionRef: number, paramValue1: number, paramTypeId1: number, paramValue2: number, paramTypeId2: number, paramValue3: number, paramTypeId3: number): JsValue {

    let func = this.getValue(functionRef, JsTypes.Function);
    let param1 = this.getValue(paramValue1, paramTypeId1);
    let param2 = this.getValue(paramValue2, paramTypeId2);
    let param3 = this.getValue(paramValue3, paramTypeId3);

    let ret: any;
    // we do this if so that we don't accidentally fire the arguments keyword with the wrong amount
    if (param3 !== undefined) ret = func(param1, param2, param3);
    else if (param2 !== undefined) ret = func(param1, param2);
    else if (param1 !== undefined) ret = func(param1);
    else ret = func();

    return this.makeRefFrom(ret);
  }

  InvokeSlow(targetRef: number, targetType: number, fnNameRef: number, fnNameType: number, paramArrayRef: number): JsValue {
    var id = this.getValue(fnNameRef, fnNameType);
    let obj = this.getValue(targetRef, targetType);
    let params = this.getValue(paramArrayRef, JsTypes.Array);
    let ret = obj[id](...params);
    return this.makeRefFrom(ret);
  }

  Invoke(targetRef: number, targetType: number, fnNameRef: number, fnNameType: number, paramValue1: number, paramTypeId1: number, paramValue2: number, paramTypeId2: number, paramValue3: number, paramTypeId3: number): JsValue {

    var id = this.getValue(fnNameRef, fnNameType);
    let obj = this.getValue(targetRef, targetType);
    let param1 = this.getValue(paramValue1, paramTypeId1);
    let param2 = this.getValue(paramValue2, paramTypeId2);
    let param3 = this.getValue(paramValue3, paramTypeId3);

    let ret: any;
    // we do this if so that we don't accidentally fire the arguments keyword with the wrong amount
    if (param3 !== undefined) ret = obj[id](param1, param2, param3);
    else if (param2 !== undefined) ret = obj[id](param1, param2);
    else if (param1 !== undefined) ret = obj[id](param1);
    else ret = obj[id]();

    return this.makeRefFrom(ret);
  }

  ConstructSlow(functionRef: number, paramArrayRef: number): JsValue {
    let func = this.getValue(functionRef, JsTypes.Function);
    let params = this.getValue(paramArrayRef, JsTypes.Array);
    let ret = new func(...params);
    return this.makeRefFrom(ret);
  }

  Construct(functionRef: number, paramValue1: number, paramTypeId1: number, paramValue2: number, paramTypeId2: number, paramValue3: number, paramTypeId3: number): JsValue {
    let func = this.getValue(functionRef, JsTypes.Function);
    let param1 = this.getValue(paramValue1, paramTypeId1);
    let param2 = this.getValue(paramValue2, paramTypeId2);
    let param3 = this.getValue(paramValue3, paramTypeId3);

    let ret: any;
    // we do this if so that we don't accidentally fire the arguments keyword with the wrong amount
    if (param3 !== undefined) ret = new func(param1, param2, param3);
    else if (param2 !== undefined) ret = new func(param1, param2);
    else if (param1 !== undefined) ret = new func(param1);
    else ret = new func();

    return this.makeRefFrom(ret);
  }

  GetProp(objectRef: number, objectType: number, propNameValue: number, propNameTypeId: number): JsValue {
    let obj = this.getValue(objectRef, objectType);
    let name = this.getValue(propNameValue, propNameTypeId);
    let ret = obj[name];
    return this.makeRefFrom(ret);
  }

  SetProp(objectRef: number, objectType: number, propNameValue: number, propNameTypeId: number, value: number, valueTypeId: number): JsValue {
    let obj = this.getValue(objectRef, objectType);
    let name = this.getValue(propNameValue, propNameTypeId);
    let setValue = this.getValue(value, valueTypeId);
    obj[name] = setValue;
    return Undefined;
  }

  GetArrayElement(arrayRef: number, index: number): JsValue {
    let obj = this.getValue(arrayRef, JsTypes.Array);
    let ret = obj[index];
    return this.makeRefFrom(ret);
  }

  SetArrayElement(arrayRef: number, index: number, value: number, valueTypeId: number): JsValue {
    let obj = this.getValue(arrayRef, JsTypes.Array);
    let setValue = this.getValue(value, valueTypeId);
    obj[index] = setValue;
    return Undefined;
  }

  CreateCallback(paramArrayRef: number): JsValue {
    let paramNames = this.getValue(paramArrayRef, JsTypes.Array);
    return this.createReference(JsTypes.Callback, paramNames);
  }

  CreateSharedTypedArray(pointer: number, typeCode: number, arrayLength: number): JsValue {
    let arrayData: SharedArrayData = { pointer, typeCode, length: arrayLength };
    return this.createReference(JsTypes.SharedTypedArray, arrayData);
  }

  CreateTypedArray(arrayPtr: number, typeCode: number, arrayLength: number): JsValue {
    let sharedArray = this.arrayBuilder(typeCode, arrayPtr, arrayLength);
    let ctr: any = sharedArray.constructor;
    let newArr: TypedArray = new ctr(sharedArray.length);
    newArr.set(sharedArray);
    return this.makeRefFrom(newArr);
  }


  CreateEmptyTypedArray(typeCode: number): JsValue {
    //todo: make a separate function to convert typecode to constructor instead of using arrayBuilder.
    let sharedArray = this.arrayBuilder(typeCode, 0, 0);
    let ctr: any = sharedArray.constructor;
    let newArr: TypedArray = new ctr(0);
    return this.makeRefFrom(newArr);
  }

  GarbageCollect(value: number, typeId: number): JsValue {
    switch(typeId){
      case JsTypes.Undefined:
      case JsTypes.Null:
      case JsTypes.Bool:
      case JsTypes.Number:
        return Undefined;
    }

    let holder = this.referenceRegistry.get(value);
    if (!holder) return Undefined;
    let ref: any;
    if (holder instanceof JsSpecialReference) {
      ref = holder.weakRef?.deref();
      if (ref) this.weakRefFinalizer.unregister(ref);
    }
    else ref = holder.reference;

    if (ref) {
      if (typeof ref === 'object' || typeof ref === 'function')
        this.objectMap.delete(<object>ref);
      else this.primitiveMap.delete(<string>ref);
    }
    return Undefined;
  }

  Equals(lhsValue: number, lhsType: number, rhsValue: number, rhsType: number): JsValue {
    let lhs = this.getValue(lhsValue, lhsType);
    let rhs = this.getValue(rhsValue, rhsType);
    return this.makeRefFrom(lhs == rhs);
  }

  GetNumber(value: number, typeId: number): JsValue {
    let val = this.getValue(value, typeId);
    return { type: JsTypes.Number, value: Number(val) };
  }

  GetString(value: number, typeId: number): JsValue {
    let val = this.getValue(value, typeId);
    return { type: JsTypes.Number, value: String(val) };
  }
}

Module.UnityJsInterop = RuntimeContext;

