using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TransformsAI.Unity.WebGL.Interop.Internal;

namespace TransformsAI.Unity.WebGL.Interop
{
    public abstract class JsReference : IDisposable, IJsValue
    {
        private static readonly Dictionary<double, WeakReference<JsReference>> ReferenceMap = new Dictionary<double, WeakReference<JsReference>>();
        private JsValue? _ref;
        public JsValue RefValue => _ref ?? throw new ObjectDisposedException("Tried to access disposed reference value");
    
        private GCHandle? _jsHandle;
        private int _jsReferenceCount;

        internal JsReference(JsTypes typeId, double refId)
        {
            ReferenceMap.Add(refId, new WeakReference<JsReference>(this));
            _ref = new JsValue(typeId, refId, this);
        }

        public static bool TryGetRef(double refId, out JsReference jsReference)
        {
            jsReference = null;
            return ReferenceMap.TryGetValue(refId, out var weakRef) && weakRef.TryGetTarget(out jsReference);
        }

        public abstract object RawValue { get; }
        public abstract bool TruthyValue { get; }
        public virtual double NumberValue => JsRuntime.GetNumber(this);

        public bool IsJsNullLike => _ref?.IsJsNullLike ?? true;
        public JsValue GetProp(JsValue key) => RefValue.GetProp(key);
        // This is a No-op for things that don't inherit JsObject
        public void SetProp(string key, JsValue value) => RefValue.SetProp(key, value);
        public JsValue Invoke(string functionName, params JsValue[] values) =>
            RefValue.Invoke(functionName, values);
        public JsValue Invoke(string functionName, JsValue param1 = default, JsValue param2 = default, JsValue param3 = default) =>
            RefValue.Invoke(functionName, param1, param2, param3);

        public static implicit operator JsValue(JsReference reference) =>
            reference.RefValue;
        public string GetJsStringImpl() => JsRuntime.GetString(this);
        public override string ToString() => ReferenceEquals(RawValue, this) ? GetJsStringImpl() : RawValue?.ToString() ?? "";
        public object As(Type type) => RefValue.As(type);
        public T As<T>() => RefValue.As<T>();
        public JsValue EvaluateOnThis(string functionBody) => RefValue.EvaluateOnThis(functionBody);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~JsReference() => Dispose(false);

        protected virtual void Dispose(bool isDisposing)
        {
            if (_ref.HasValue)
            {
                ReferenceMap.Remove(_ref.Value.Value);
                JsRuntime.GarbageCollect(this);
                _ref = null;
            }

            _jsHandle?.Free();
            _jsHandle = null;
        }

    
        // Used so that this object doesn't get GC'd if it's only referenced from JS
        // This allows you to create long-lived callbacks without worrying about their life-cycles
        internal void AcquireFromJs()
        {
            _jsReferenceCount++;
            if (_jsHandle.HasValue) return;
            _jsHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        }

        internal void ReleaseFromJs()
        {
        
            _jsReferenceCount--;
            if (_jsReferenceCount > 0) return;
            _jsHandle?.Free();
            _jsHandle = null;
        }
        
        // Implement type coersion
        public static implicit operator bool(JsReference i) => i.TruthyValue;
        public static explicit operator string(JsReference i) => i.ToString();
        public static explicit operator double(JsReference i) => i.NumberValue;

    }
}
