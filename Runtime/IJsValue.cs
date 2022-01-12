using System;

namespace TransformsAI.Unity.WebGL.Interop
{
    public interface IJsValue
    {
        public object RawValue { get; }
        public bool TruthyValue { get; }
        public double NumberValue { get; }
        public JsValue GetProp(JsValue key);
        public void SetProp(string key, JsValue value);
        public JsValue Invoke(string functionName, params JsValue[] values);
        public JsValue Invoke(string functionName, JsValue param1 = default, JsValue param2 = default, JsValue param3 = default);
        public JsValue EvaluateOnThis(string functionBody);

        public bool IsJsNullLike { get; }
        public T As<T>();
        public object As(Type type);
    }
}
