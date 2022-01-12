using System;
using TransformsAI.Unity.WebGL.Interop.Internal;

namespace TransformsAI.Unity.WebGL.Interop.Types
{
    public class JsTypedArray : JsObject
    {

        private int? _length;
        public int Length => _length ?? (_length = GetProp("length").As<int>()).Value;

        internal JsTypedArray(double refId, JsTypes typeId = JsTypes.TypedArray) : base(refId, typeId) { }

        public T[] GetDataCopy<T>() where T : unmanaged
        {
            var copyDestination = new T[Length];
            GetDataCopy(copyDestination);
            return copyDestination;
        }

        public virtual void GetDataCopy<T>(T[] copyDestination) where T : unmanaged
        {
            CheckLengths(Length, copyDestination.Length);
            using var dest = JsRuntime.CreateSharedTypedArray(copyDestination);
            dest.Invoke("set", this);

        }

        public virtual void SetDataCopy<T>(T[] copySource) where T : unmanaged
        {
            CheckLengths(copySource.Length, Length);
            using var src = JsRuntime.CreateSharedTypedArray(copySource);
            Invoke("set", src);
        }

        protected static void CheckLengths(int src, int dest)
        {
            if (src > dest) throw new ArgumentException("destination is smaller than source");
        }

        public static TypedArrayTypeCode GetTypeCode(Array t)
        {
            var type = t.GetType();
            if (type == typeof(float[])) return TypedArrayTypeCode.Float32Array;
            if (type == typeof(double[])) return TypedArrayTypeCode.Float64Array;
            if (type == typeof(short[])) return TypedArrayTypeCode.Int16Array;
            if (type == typeof(int[])) return TypedArrayTypeCode.Int32Array;
            if (type == typeof(sbyte[])) return TypedArrayTypeCode.Int8Array;
            if (type == typeof(ushort[])) return TypedArrayTypeCode.Uint16Array;
            if (type == typeof(uint[])) return TypedArrayTypeCode.Uint32Array;
            if (type == typeof(byte[])) return TypedArrayTypeCode.Uint8Array;
            throw new InvalidCastException("Unsupported TypedArray");
        }
        
    }


    // The name of these need to match the constructor of the JS counterpart
    // The codes here are lifted from Mono's codes 
    // https://github.com/mono/mono/blob/main/sdks/wasm/framework/src/WebAssembly.Bindings/Core/TypedArrayTypeCode.cs
    public enum TypedArrayTypeCode
    {
        Int8Array = 5,
        Uint8Array = 6,
        Int16Array = 7,
        Uint16Array = 8,
        Int32Array = 9,
        Uint32Array = 10,
        Float32Array = 13,
        Float64Array = 14,
        Uint8ClampedArray = 0xF,
    }
}
