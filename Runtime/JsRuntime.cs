using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TransformsAI.Unity.WebGL.Interop.Internal;
using TransformsAI.Unity.WebGL.Interop.Types;
using UnityEngine;

using Debug = System.Diagnostics.Debug;
using Raw = TransformsAI.Unity.WebGL.Interop.Internal.RuntimeRaw;

namespace TransformsAI.Unity.WebGL.Interop
{
    public static class JsRuntime
    {
        // --- Api ---
        public static JsValue GetGlobalValue(JsValue identifier) =>
            Raw.GetGlobalObject(out var typeId, identifier.Ref(), identifier.Type()).Receive(typeId);
        public static JsValue CreateHostObject(string identifier, JsValue param1 = default, JsValue param2 = default, JsValue param3 = default) =>
            GetGlobalValue(identifier).As<JsFunction>().Construct(param1, param2, param3);
        public static JsValue CreateHostObject(string identifier, params JsValue[] args) =>
            GetGlobalValue(identifier).As<JsFunction>().Construct(args);
        public static JsObject CreateObject() =>
            Raw.CreateEmptyObject(out var typeId).Receive(typeId).As<JsObject>();

        public static JsObject CreateObject(IDictionary dictionary)
        {
            var obj = CreateObject();
            obj.Populate(dictionary);
            return obj;
        }

        public static JsString CreateString(string str)
        {
            // Can't use Receive, since we have to check the String cache. See JsString.cs for more info.
            if (JsString.TryGetString(str, out var jsString)) return jsString;
            var refId = Raw.CreateString(out var typeId, str);
            CheckException(refId, typeId);
            // It's possible that the returned string reference could point to a string that was already in the 
            // Reference cache, but not the string cache. This makes sure that they are the same reference.
            if (JsReference.TryGetRef(refId, out var existing) && existing is JsString s) return s;
            var jsStr = new JsString(refId, str);
            return jsStr;
        }

        public static JsBigInt CreateBigInt(BigInteger bigInt)
        {
            // Gets the js BigInt function
            var bigIntFunction = GetGlobalValue("BigInt").As<JsFunction>();
            return bigIntFunction.Call(bigInt.ToString()).As<JsBigInt>();
        }

        public static JsArray CreateArray() =>
            Raw.CreateArray(out var typeId).Receive(typeId).As<JsArray>();

        public static JsArray CreateArray(IEnumerable list)
        {
            var arr = CreateArray();
            foreach (var obj in list) arr.Add(CreateFromObject(obj));
            return arr;
        }

        /// <summary>
        /// This is equivalent to calling `new Function(body)` in Js. Warning: Use with caution. 
        /// This is susceptible to being blocked by security policies or could lead to attacks through injection.
        /// </summary>
        public static JsFunction CreateFunction(string body, bool cache = false)
        {
            if (cache && JsFunction.TryGetFunction(body, out var fn)) return fn;
            fn = GetGlobalValue("Function").As<JsFunction>().Construct(body).As<JsFunction>();
            if (cache) JsFunction.StoreFunction(body, fn);
            return fn;
        }


        /// <summary>
        /// Warning: Use with caution. This is equivalent to calling `new Function(body)` in Js.
        /// This is susceptible to being blocked by security policies or could lead to attacks through injection.
        /// </summary>
        public static JsFunction CreateFunction(params string[] parametersThenBody)
        {
            var args = parametersThenBody.Select(it => (JsValue)it).ToArray();
            return GetGlobalValue("Function").As<JsFunction>().Construct(args).As<JsFunction>();
        }

        public static JsValue Construct(JsFunction function, JsValue param1 = default, JsValue param2 = default, JsValue param3 = default)
        {
            if (function is JsCallback) throw new InvalidOperationException("Don't instantiate C# callbacks");
            var refId = Raw.Construct(out var typeId1, function.Ref(), param1.Ref(),
                param1.Type(), param2.Ref(),
                param2.Type(), param3.Ref(), param3.Type());
            return Receive(refId, typeId1);

        }

        /// <summary>
        /// Slower version of <see cref="Construct(JsFunction, JsValue,JsValue,JsValue)"/>. Uses an array to pass the parameters. This causes additional allocations
        /// </summary>
        public static JsValue Construct(JsFunction function, params JsValue[] p)
        {
            if (function is JsCallback) throw new InvalidOperationException("Don't instantiate C# callbacks");
            return Raw.ConstructSlow(out var typeId, function.Ref(), CreateArray(p).Ref()).Receive(typeId);
        }


        public static JsValue Call(JsFunction function, JsValue param1 = default, JsValue param2 = default, JsValue param3 = default)
        {
            var refId = Raw.Call(out var typeId1, function.Ref(), param1.Ref(),
                param1.Type(), param2.Ref(),
                param2.Type(), param3.Ref(), param3.Type());
            return Receive(refId, typeId1);
        }

        /// <summary>
        /// Slower version of <see cref="Call(JsFunction, JsValue,JsValue,JsValue)"/>. Uses an array to pass the parameters. This causes additional allocations
        /// </summary>
        public static JsValue Call(JsFunction function, params JsValue[] p) =>
            Raw.CallSlow(out var typeId, function.Ref(), CreateArray(p).Ref()).Receive(typeId);

        public static JsValue Invoke(JsValue valueRef, JsValue fnName, JsValue param1 = default, JsValue param2 = default, JsValue param3 = default)
        {
            var refId = Raw.Invoke(out var typeId1, valueRef.Ref(), valueRef.Type(), fnName.Ref(), fnName.Type(),
                param1.Ref(), param1.Type(),
                param2.Ref(), param2.Type(),
                param3.Ref(), param3.Type());
            return Receive(refId, typeId1);
        }


        /// <summary>
        /// Slower version of <see cref="Invoke(JsValue,JsValue,JsValue,JsValue,JsValue)"/>. Uses an array to pass the parameters. This causes additional allocations
        /// </summary>
        public static JsValue Invoke(JsValue valueRef, JsValue fnName, params JsValue[] p) =>
            Raw.InvokeSlow(out var typeId, valueRef.Ref(), valueRef.Type(), fnName.Ref(), fnName.Type(), CreateArray(p).Ref()).Receive(typeId);

        public static JsValue GetProp(JsValue obj, JsValue propName) =>
            Raw.GetProp(out var typeId, obj.Ref(), obj.Type(), propName.Ref(), propName.Type()).Receive(typeId);
        public static void SetProp(JsValue obj, JsValue propName, JsValue value) =>
            Raw.SetProp(out var typeId, obj.Ref(), obj.Type(), propName.Ref(), propName.Type(), value.Ref(), value.Type()).CheckException(typeId);
        public static JsValue GetArrayElement(JsArray array, int index) =>
            Raw.GetArrayElement(out var typeId, array.Ref(), index).Receive(typeId);
        public static void SetArrayElement(JsArray array, int index, JsValue value) =>
            Raw.SetArrayElement(out var typeId, array.Ref(), index, value.Ref(), value.Type()).CheckException(typeId);
        public static JsCallback CreateCallback(Delegate del)
        {
            var parameters = del.Method.GetParameters();
            var fnArgs = parameters.Select(it => it.Name);
            var paramArray = CreateArray(fnArgs);
            var refId = Raw.CreateCallback(out var typeId, paramArray.Ref());
            CheckException(refId, typeId);
            return new JsCallback(refId, del);
        }

        public static JsSharedTypedArray CreateSharedTypedArray<T>(T[] array) where T : unmanaged
        {
            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                unsafe
                {
                    fixed (T* ptr = array)
                    {
                        var refId = Raw.CreateSharedTypedArray(out var typeId, (int)ptr, array.TypeCode(), array.Length);
                        CheckException(refId, typeId);
                        return new JsSharedTypedArray(refId, handle);

                    }
                }
            }
            catch (Exception)
            {
                handle.Free();
                throw;
            }
        }

        public static JsTypedArray CreateTypedArray<T>(T[] array) where T : unmanaged
        {
            unsafe
            {
                fixed (T* ptr = array)
                {
                    var refId = Raw.CreateTypedArray(out var typeId, (int)ptr, array.TypeCode(), array.Length);
                    CheckException(refId, typeId);
                    return new JsTypedArray(refId);

                }
            }
        }

        public static JsTypedArray CreateTypedArray(TypedArrayTypeCode type)
        {
            var refId = Raw.CreateEmptyTypedArray(out var typeId, (int)type);
            CheckException(refId, typeId);
            return new JsTypedArray(refId);
        }

        internal static string GetString(JsValue obj)
        {
            var str = Raw.GetString(out var typeId, obj.Ref(), obj.Type());
            CheckException(0, typeId);
            return str;
        }

        public static double GetNumber(JsValue obj)
        {
            var num = Raw.GetNumber(out var typeId, obj.Ref(), obj.Type());
            CheckException(num, typeId);
            return num;
        }

        public static bool Equals(JsValue lhs, JsValue rhs) =>
            Raw.Equals(out var typeId, lhs.Ref(), lhs.Type(), rhs.Ref(), rhs.Type()).Receive(typeId);

        internal static void GarbageCollect(JsReference obj) =>
            Raw.GarbageCollect(out var typeId, obj.Ref(), obj.Type()).Receive(typeId);

        /// <summary>
        /// Converts a CLR object to a JsValue (if possible)
        /// </summary>
        public static JsValue CreateFromObject(object obj)
        {
            switch (obj)
            {
                case null: return null;
                case JsValue i: return i;
                case JsReference i: return i;
                case bool i: return i;
                case int i: return i;
                case float i: return i;
                case double i: return i;
                case BigInteger i: return i;
                case string i: return i;
                case Delegate d: return CreateCallback(d);
                case IDictionary i: return CreateObject(i);
                case IEnumerable i: return CreateArray(i);
                default: throw new InvalidCastException($"Object cannot be converted to {nameof(JsValue)}");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (Application.isEditor) return;

            if (Application.platform != RuntimePlatform.WebGLPlayer)
                throw new PlatformNotSupportedException("Should not be using JS-Interop on non-Webgl platforms");
#if UNITY_2020
            Raw.Initialize(OnJsCallback, Acquire, Release);
#else
            Raw.Initialize(OnJsCallback, Acquire, Release, false);
#endif
            UnityEngine.Debug.Log("Initialized JS-Interop");
        }


        private static bool Acquire(double refId)
        {
            if (!JsReference.TryGetRef(refId, out var reference))
            {
                Debug.Fail("JS tried to acquire object that was not found or disposed");
                return false;
            }
            reference.AcquireFromJs();
            return true;
        }

        private static bool Release(double refId)
        {
            if (!JsReference.TryGetRef(refId, out var reference))
            {
                Debug.Fail("Reference was missing when trying to be released from JS");
                return false;
            }
            reference.ReleaseFromJs();
            return true;
        }


        private static (double value, int typeId) OnJsCallback(double callbackRefId, double value, int typeid, bool hasParamList)
        {
            var existingCallback = Receive(callbackRefId, (int)JsTypes.Callback).As<JsCallback>();
            var param = Receive(value, typeid);

            var result = hasParamList ?
                existingCallback.Call(param.As<JsArray>()) :
                existingCallback.Call(param);

            return (result.Value, result.Type());
        }
        // --- Utilities ---


        private static double Ref(this JsValue js) => js.Value;
        private static double Ref(this JsReference js) => js?.RefValue.Ref() ?? JsValue.Undefined.Ref();
        private static int Type(this JsValue js) => (int)js.TypeId;
        private static int Type(this JsReference js) => js?.RefValue.Type() ?? JsValue.Undefined.Type();
        private static int TypeCode(this Array array) => (int)JsTypedArray.GetTypeCode(array);

        private static void CheckException(this double refId, int typeId, [CallerMemberName] string funcName = null)
        {
            if (typeId != (int)JsTypes.Exception) return;
            if (refId == 0) throw new JsException($"Unknown Js Exception at {funcName}");
            var exceptionString = Raw.GetString(out var strType, refId, (int)JsTypes.String);
            if (strType != (int)JsTypes.String) throw new JsException($"Unknown Js Exception at {funcName}");
            throw new JsException(exceptionString);
        }

        private static JsValue Receive(this double refId, int typeId, [CallerMemberName] string funcName = null)
        {
            CheckException(refId, typeId, funcName);
            var type = (JsTypes)typeId;


            if (!type.IsValueType() && JsReference.TryGetRef(refId, out var reference))
                return reference;

            switch (type)
            {
                // Values
                case JsTypes.Undefined: return JsValue.Undefined;
                case JsTypes.Null: return JsValue.Null;
                case JsTypes.Bool: return refId != 0 ? JsValue.True : JsValue.False;
                case JsTypes.Number: return refId;
                // References
                case JsTypes.BigInt: return new JsBigInt(refId);
                case JsTypes.String: return new JsString(refId);
                case JsTypes.Symbol: return new JsSymbol(refId);
                case JsTypes.Object: return new JsObject(refId);
                case JsTypes.Function: return new JsFunction(refId);
                case JsTypes.Promise: return new JsPromise(refId);
                case JsTypes.Array: return new JsArray(refId);
                case JsTypes.TypedArray: return new JsTypedArray(refId);
                // Cannot create these from just reference numbers.
                // These need to be made in C#. Therefore they should already be in the cache.
                case JsTypes.Callback:
                case JsTypes.SharedTypedArray:
                    throw new ObjectDisposedException($"Received JS reference of special type {typeId}. This object was likely disposed, but was still referenced by JS.");
                case JsTypes.Exception:
                    throw new Exception("Cannot marshal JS Exception");
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeId), $"Unknown JS type {type}");

            }
        }

    }
}
