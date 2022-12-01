"use strict";
var JsTypes;
(function (JsTypes) {
    JsTypes[JsTypes["Undefined"] = 0] = "Undefined";
    JsTypes[JsTypes["Null"] = 1] = "Null";
    JsTypes[JsTypes["Bool"] = 2] = "Bool";
    JsTypes[JsTypes["Number"] = 3] = "Number";
    JsTypes[JsTypes["BigInt"] = 4] = "BigInt";
    JsTypes[JsTypes["String"] = 5] = "String";
    JsTypes[JsTypes["Symbol"] = 6] = "Symbol";
    JsTypes[JsTypes["Object"] = 7] = "Object";
    JsTypes[JsTypes["Function"] = 8] = "Function";
    JsTypes[JsTypes["Callback"] = 9] = "Callback";
    JsTypes[JsTypes["Promise"] = 10] = "Promise";
    JsTypes[JsTypes["Array"] = 11] = "Array";
    JsTypes[JsTypes["TypedArray"] = 12] = "TypedArray";
    JsTypes[JsTypes["SharedTypedArray"] = 13] = "SharedTypedArray";
    JsTypes[JsTypes["Exception"] = -1] = "Exception";
})(JsTypes || (JsTypes = {}));
var TypedArrayTypeCode;
(function (TypedArrayTypeCode) {
    TypedArrayTypeCode[TypedArrayTypeCode["Int8Array"] = 5] = "Int8Array";
    TypedArrayTypeCode[TypedArrayTypeCode["Uint8Array"] = 6] = "Uint8Array";
    TypedArrayTypeCode[TypedArrayTypeCode["Int16Array"] = 7] = "Int16Array";
    TypedArrayTypeCode[TypedArrayTypeCode["Uint16Array"] = 8] = "Uint16Array";
    TypedArrayTypeCode[TypedArrayTypeCode["Int32Array"] = 9] = "Int32Array";
    TypedArrayTypeCode[TypedArrayTypeCode["Uint32Array"] = 10] = "Uint32Array";
    TypedArrayTypeCode[TypedArrayTypeCode["Float32Array"] = 13] = "Float32Array";
    TypedArrayTypeCode[TypedArrayTypeCode["Float64Array"] = 14] = "Float64Array";
    TypedArrayTypeCode[TypedArrayTypeCode["Uint8ClampedArray"] = 15] = "Uint8ClampedArray";
})(TypedArrayTypeCode || (TypedArrayTypeCode = {}));
class JsSpecialReference {
    constructor(runtime, type, value, data) {
        this.runtime = runtime;
        this.type = type;
        this.value = value;
        this.data = data;
        this.weakRef = undefined;
    }
    get reference() {
        var _a;
        let existing = (_a = this.weakRef) === null || _a === void 0 ? void 0 : _a.deref();
        if (existing)
            return existing;
        if (!this.runtime.acquireReference(this.value))
            throw new Error("Could not re-acquire reference.");
        var ref;
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
    GetSharedArray() {
        var data = this.data;
        var sharedArray = this.runtime.arrayBuilder(data.typeCode, data.pointer, data.length);
        return sharedArray;
    }
    GetCallbackFunction() {
        // This is not a member function since we want it to be garbage collected.
        var fn = function () {
            let rawArgs = arguments;
            let param;
            let isParamArray = false;
            let paramNames = this.data;
            if (paramNames.length == 0) {
                param = Undefined;
            }
            else if (paramNames.length == 1) {
                param = this.runtime.makeRefFrom(rawArgs[0]);
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
            let resultFn = (val) => retval = val;
            let callbackResponseId = this.runtime.callbackCounter++;
            this.runtime.callbackResponseRegistry.set(callbackResponseId, resultFn);
            this.runtime.callbackHandler(this.value, callbackResponseId, param.value, param.type, isParamArray);
            this.runtime.callbackResponseRegistry.delete(callbackResponseId);
            var value = this.runtime.getValue(retval.value, retval.type);
            return value;
        };
        return fn.bind(this);
    }
}
const Undefined = Object.freeze({ value: 0, type: JsTypes.Undefined });
const Null = Object.freeze({ value: 0, type: JsTypes.Null });
const True = Object.freeze({ value: 1, type: JsTypes.Bool });
const False = Object.freeze({ value: 0, type: JsTypes.Undefined });
class RuntimeContext {
    constructor(arrayBuilder, callbackHandler, onAcquireCallback, onReleaseCallback) {
        // 0 is reserved for special values (undefined, false, null, etc)
        this.refercenceCounter = 1;
        this.callbackCounter = 1;
        this.weakRefFinalizer = new FinalizationRegistry(refId => this.releaseReference(refId));
        this.referenceRegistry = new Map();
        this.objectMap = new WeakMap();
        this.primitiveMap = new Map();
        this.callbackResponseRegistry = new Map();
        this.arrayBuilder = arrayBuilder;
        this.callbackHandler = callbackHandler;
        this.acquireReference = onAcquireCallback;
        this.releaseReference = onReleaseCallback;
    }
    makeRefFrom(obj) {
        if (obj === null)
            return Null;
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
            if (objRef)
                return this.referenceRegistry.get(objRef) || Undefined;
        }
        else {
            let objRef = this.primitiveMap.get(obj);
            if (objRef)
                return this.referenceRegistry.get(objRef) || Undefined;
        }
        // check for subtypes
        if (Array.isArray(obj))
            type = JsTypes.Array;
        else if (ArrayBuffer.isView(obj) && !(obj instanceof DataView))
            type = JsTypes.TypedArray;
        else if (typeof obj.then === 'function')
            type = JsTypes.Promise;
        return this.createReference(type, obj);
    }
    createReference(type, obj) {
        let isPrimitive = !(typeof obj === 'object' || typeof obj === 'function');
        let value = this.refercenceCounter++;
        var holder;
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
    getValue(ref, type) {
        switch (type) {
            // value types
            case JsTypes.Undefined: return undefined;
            case JsTypes.Null: return null;
            case JsTypes.Bool: return ref ? true : false;
            case JsTypes.Number: return ref;
        }
        var holder = this.referenceRegistry.get(ref);
        if (!holder)
            return undefined;
        return holder.reference;
    }
    RespondToCallback(responseRefId, value, typeId) {
        let fn = this.callbackResponseRegistry.get(responseRefId);
        if (!fn)
            throw new Error("bad callback response");
        fn({ type: typeId, value: value });
        return Undefined;
    }
    GetGlobalObject(targetRef, targetType) {
        var id = this.getValue(targetRef, targetType);
        var globals = globalThis;
        return this.makeRefFrom(globals[id]);
    }
    CreateEmptyObject() {
        return this.makeRefFrom({});
    }
    CreateString(str) {
        return this.makeRefFrom(str);
    }
    CreateArray() {
        return this.makeRefFrom([]);
    }
    CallSlow(functionRef, paramArrayRef) {
        let func = this.getValue(functionRef, JsTypes.Function);
        let params = this.getValue(paramArrayRef, JsTypes.Array);
        let ret = func(...params);
        return this.makeRefFrom(ret);
    }
    Call(functionRef, paramValue1, paramTypeId1, paramValue2, paramTypeId2, paramValue3, paramTypeId3) {
        let func = this.getValue(functionRef, JsTypes.Function);
        let param1 = this.getValue(paramValue1, paramTypeId1);
        let param2 = this.getValue(paramValue2, paramTypeId2);
        let param3 = this.getValue(paramValue3, paramTypeId3);
        let ret;
        // we do this if so that we don't accidentally fire the arguments keyword with the wrong amount
        if (param3 !== undefined)
            ret = func(param1, param2, param3);
        else if (param2 !== undefined)
            ret = func(param1, param2);
        else if (param1 !== undefined)
            ret = func(param1);
        else
            ret = func();
        return this.makeRefFrom(ret);
    }
    InvokeSlow(targetRef, targetType, fnNameRef, fnNameType, paramArrayRef) {
        var id = this.getValue(fnNameRef, fnNameType);
        let obj = this.getValue(targetRef, targetType);
        let params = this.getValue(paramArrayRef, JsTypes.Array);
        let ret = obj[id](...params);
        return this.makeRefFrom(ret);
    }
    Invoke(targetRef, targetType, fnNameRef, fnNameType, paramValue1, paramTypeId1, paramValue2, paramTypeId2, paramValue3, paramTypeId3) {
        var id = this.getValue(fnNameRef, fnNameType);
        let obj = this.getValue(targetRef, targetType);
        let param1 = this.getValue(paramValue1, paramTypeId1);
        let param2 = this.getValue(paramValue2, paramTypeId2);
        let param3 = this.getValue(paramValue3, paramTypeId3);
        let ret;
        // we do this if so that we don't accidentally fire the arguments keyword with the wrong amount
        if (param3 !== undefined)
            ret = obj[id](param1, param2, param3);
        else if (param2 !== undefined)
            ret = obj[id](param1, param2);
        else if (param1 !== undefined)
            ret = obj[id](param1);
        else
            ret = obj[id]();
        return this.makeRefFrom(ret);
    }
    ConstructSlow(functionRef, paramArrayRef) {
        let func = this.getValue(functionRef, JsTypes.Function);
        let params = this.getValue(paramArrayRef, JsTypes.Array);
        let ret = new func(...params);
        return this.makeRefFrom(ret);
    }
    Construct(functionRef, paramValue1, paramTypeId1, paramValue2, paramTypeId2, paramValue3, paramTypeId3) {
        let func = this.getValue(functionRef, JsTypes.Function);
        let param1 = this.getValue(paramValue1, paramTypeId1);
        let param2 = this.getValue(paramValue2, paramTypeId2);
        let param3 = this.getValue(paramValue3, paramTypeId3);
        let ret;
        // we do this if so that we don't accidentally fire the arguments keyword with the wrong amount
        if (param3 !== undefined)
            ret = new func(param1, param2, param3);
        else if (param2 !== undefined)
            ret = new func(param1, param2);
        else if (param1 !== undefined)
            ret = new func(param1);
        else
            ret = new func();
        return this.makeRefFrom(ret);
    }
    GetProp(objectRef, objectType, propNameValue, propNameTypeId) {
        let obj = this.getValue(objectRef, objectType);
        let name = this.getValue(propNameValue, propNameTypeId);
        let ret = obj[name];
        return this.makeRefFrom(ret);
    }
    SetProp(objectRef, objectType, propNameValue, propNameTypeId, value, valueTypeId) {
        let obj = this.getValue(objectRef, objectType);
        let name = this.getValue(propNameValue, propNameTypeId);
        let setValue = this.getValue(value, valueTypeId);
        obj[name] = setValue;
        return Undefined;
    }
    GetArrayElement(arrayRef, index) {
        let obj = this.getValue(arrayRef, JsTypes.Array);
        let ret = obj[index];
        return this.makeRefFrom(ret);
    }
    SetArrayElement(arrayRef, index, value, valueTypeId) {
        let obj = this.getValue(arrayRef, JsTypes.Array);
        let setValue = this.getValue(value, valueTypeId);
        obj[index] = setValue;
        return Undefined;
    }
    CreateCallback(paramArrayRef) {
        let paramNames = this.getValue(paramArrayRef, JsTypes.Array);
        return this.createReference(JsTypes.Callback, paramNames);
    }
    CreateSharedTypedArray(pointer, typeCode, arrayLength) {
        let arrayData = { pointer, typeCode, length: arrayLength };
        return this.createReference(JsTypes.SharedTypedArray, arrayData);
    }
    CreateTypedArray(arrayPtr, typeCode, arrayLength) {
        let sharedArray = this.arrayBuilder(typeCode, arrayPtr, arrayLength);
        let ctr = sharedArray.constructor;
        let newArr = new ctr(sharedArray.length);
        newArr.set(sharedArray);
        return this.makeRefFrom(newArr);
    }
    CreateEmptyTypedArray(typeCode) {
        //todo: make a separate function to convert typecode to constructor instead of using arrayBuilder.
        let sharedArray = this.arrayBuilder(typeCode, 0, 0);
        let ctr = sharedArray.constructor;
        let newArr = new ctr(0);
        return this.makeRefFrom(newArr);
    }
    GarbageCollect(value, typeId) {
        var _a;
        switch (typeId) {
            case JsTypes.Undefined:
            case JsTypes.Null:
            case JsTypes.Bool:
            case JsTypes.Number:
                return Undefined;
        }
        let holder = this.referenceRegistry.get(value);
        if (!holder)
            return Undefined;
        let ref;
        if (holder instanceof JsSpecialReference) {
            ref = (_a = holder.weakRef) === null || _a === void 0 ? void 0 : _a.deref();
            if (ref)
                this.weakRefFinalizer.unregister(ref);
        }
        else
            ref = holder.reference;
        if (ref) {
            if (typeof ref === 'object' || typeof ref === 'function')
                this.objectMap.delete(ref);
            else
                this.primitiveMap.delete(ref);
        }
        return Undefined;
    }
    Equals(lhsValue, lhsType, rhsValue, rhsType) {
        let lhs = this.getValue(lhsValue, lhsType);
        let rhs = this.getValue(rhsValue, rhsType);
        return this.makeRefFrom(lhs == rhs);
    }
    GetNumber(value, typeId) {
        let val = this.getValue(value, typeId);
        return { type: JsTypes.Number, value: Number(val) };
    }
    GetString(value, typeId) {
        let val = this.getValue(value, typeId);
        return { type: JsTypes.Number, value: String(val) };
    }
}
Module.UnityJsInterop = RuntimeContext;
//# sourceMappingURL=JsRuntime.jspre.map