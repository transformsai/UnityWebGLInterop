using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JsInterop.Internal;

namespace JsInterop.Types
{
    public class JsSharedTypedArray : JsTypedArray
    {
        private GCHandle? SharedArrayHandle { get; }

        internal JsSharedTypedArray(double refId, GCHandle sharedArrayHandle) : base(refId, JsTypes.SharedTypedArray)
        {
            SharedArrayHandle = sharedArrayHandle;
        }

        public T[] Access<T>() where T : unmanaged
        {
            if (!SharedArrayHandle.HasValue) throw new ObjectDisposedException("Unable to access handle. Object has likely been disposed");
            var value = SharedArrayHandle.Value.Target;
            return value is T[] target ? target : throw new InvalidCastException($"Type mismatch. Array was of type {value.GetType()} expected {typeof(T)}");
        }

        public override void GetDataCopy<T>(T[] copyDestination)
        {
            CheckLengths(Length, copyDestination.Length);
            var src = Access<T>();
            Array.Copy(src, copyDestination, src.Length);
        }

        public override void SetDataCopy<T>(T[] copySource)
        {
            CheckLengths(copySource.Length, Length);
            var dest = Access<T>();
            Array.Copy(copySource, dest, copySource.Length);
        }

        protected override void Dispose(bool isDisposing)
        {
            SharedArrayHandle?.Free();
            base.Dispose(isDisposing);
            if (!isDisposing) Debug.Fail("Shared array left in Js Memory.");
        }
    }
}
