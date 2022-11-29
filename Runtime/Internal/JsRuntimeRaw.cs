using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AOT;

[assembly: InternalsVisibleTo("JsInterop.Editor")]

namespace TransformsAI.Unity.WebGL.Interop.Internal
{
    // This is a translation layer that only uses primitives to communicate with Unity.
    // As a result, this class doesn't use JsValue or JsReference.
    // Methods of this file are automatically turned into .jslib and .ts files for the
    // Javascript side to implement.
    internal static class RuntimeRaw
    {

        [DllImport("__Internal")]
        internal static extern double GetGlobalObject(out int returnTypeId, double targetRef, int targetType);

        [DllImport("__Internal")]
        internal static extern double CreateEmptyObject(out int returnTypeId);

        [DllImport("__Internal")]
        internal static extern double CreateString(out int returnTypeId, string str);

        [DllImport("__Internal")]
        internal static extern double CreateArray(out int returnTypeId);

        [DllImport("__Internal")]
        internal static extern double CallSlow(out int returnTypeId, double functionRef, double paramArrayRef);

        [DllImport("__Internal")]
        internal static extern double Call(out int returnTypeId, double functionRef,
            double paramValue1, int paramTypeId1,
            double paramValue2, int paramTypeId2,
            double paramValue3, int paramTypeId3);


        [DllImport("__Internal")]
        internal static extern double InvokeSlow(out int returnTypeId, double targetRef, int targetType, double fnNameRef, int fnNameType, double paramArrayRef);

        [DllImport("__Internal")]
        internal static extern double Invoke(out int returnTypeId, double targetRef, int targetType, double fnNameRef, int fnNameType,
            double paramValue1, int paramTypeId1,
            double paramValue2, int paramTypeId2,
            double paramValue3, int paramTypeId3);


        [DllImport("__Internal")]
        internal static extern double ConstructSlow(out int returnTypeId, double functionRef, double paramArrayRef);

        [DllImport("__Internal")]
        internal static extern double Construct(out int returnTypeId, double functionRef,
            double paramValue1, int paramTypeId1,
            double paramValue2, int paramTypeId2,
            double paramValue3, int paramTypeId3);

        [DllImport("__Internal")]
        internal static extern double GetProp(out int returnTypeId, double objectRef, int objectType, double propNameValue, int propNameTypeId);

        [DllImport("__Internal")]
        internal static extern double SetProp(out int returnTypeId, double objectRef, int objectType, double propNameValue, int propNameTypeId, double value, int valueTypeId);

        [DllImport("__Internal")]
        internal static extern double GetArrayElement(out int returnTypeId, double arrayRef, int index);

        [DllImport("__Internal")]
        internal static extern double SetArrayElement(out int returnTypeId, double arrayRef, int index, double value, int valueTypeId);

        [DllImport("__Internal")]
        internal static extern double CreateCallback(out int returnTypeId, double paramArrayRef);

        [DllImport("__Internal")]
        internal static extern double CreateSharedTypedArray(out int returnTypeId, int arrayPtr, int typeCode, int arrayLength);

        [DllImport("__Internal")]
        internal static extern double CreateTypedArray(out int returnTypeId, int arrayPtr, int typeCode, int arrayLength);

        [DllImport("__Internal")]
        internal static extern double CreateEmptyTypedArray(out int returnTypeId, int typeCode);

        [DllImport("__Internal")]
        internal static extern double GarbageCollect(out int returnTypeId, double value, int typeId);

        [DllImport("__Internal")]
        internal static extern double Equals(out int returnTypeId, double lhsValue, int lhsType, double rhsValue, int rhsType);

        [DllImport("__Internal")]
        internal static extern double GetNumber(out int returnTypeId, double value, int typeId);

        [DllImport("__Internal")]
        internal static extern string GetString(out int returnTypeId, double value, int typeId);

        [DllImport("__Internal")]
        internal static extern double RespondToCallback(out int returnTypeId, double responseRefId, double value, int typeId);


        // In Js, this method is used to construct the context that will handle the rest of the methods in this class.
        // This needs to be called first.
        [DllImport("__Internal")]
        internal static extern void InitializeInternal(InternalCallbackListener callbackHandler, ReferenceHandler onAcquireReference, ReferenceHandler onReleaseReference, bool oldRuntime = true);


        internal delegate bool ReferenceHandler(double refId);
        internal delegate void InternalCallbackListener(double callbackRefId, double responseRefId, double value, int typeId, bool paramsAreArray);
        // We use a tuple to avoid contaminating this class with JsValue
        internal delegate (double value, int typeId) JsCallbackListener(double callbackRefId, double value, int typeId, bool paramsAreArray);

        internal static void Initialize(JsCallbackListener onJsCallback, ReferenceHandler onAcquireReference, ReferenceHandler onReleaseReference)
        {
            _OnJsCallback = onJsCallback;
            _OnJsAcquireRef = onAcquireReference;
            _OnJsReleaseRef = onReleaseReference;
            InitializeInternal(OnCallback, OnAcquireReference, OnReleaseReference);
        }

        [MonoPInvokeCallback(typeof(InternalCallbackListener))]
        private static void OnCallback(double callbackRefId, double responseRefId, double value, int typeId, bool paramsAreArray)
        {
            if (_OnJsCallback == null) return;
            var (returnValue, returnTypeId) = _OnJsCallback.Invoke(callbackRefId, value, typeId, paramsAreArray);
            var expId = RespondToCallback(out var respTypeId, responseRefId, returnValue, returnTypeId);
            if (respTypeId != 0) Debug.Fail($"Failed to respond to callback with typeId {typeId} and expId:{expId}");
        }

        [MonoPInvokeCallback(typeof(ReferenceHandler))]
        private static bool OnAcquireReference(double callbackRefId) => _OnJsAcquireRef?.Invoke(callbackRefId) ?? false;
        [MonoPInvokeCallback(typeof(ReferenceHandler))]
        private static bool OnReleaseReference(double callbackRefId) => _OnJsReleaseRef?.Invoke(callbackRefId) ?? false;


        private static JsCallbackListener _OnJsCallback;
        private static ReferenceHandler _OnJsAcquireRef;
        private static ReferenceHandler _OnJsReleaseRef;

    }
}
