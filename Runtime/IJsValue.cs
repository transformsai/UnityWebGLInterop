using System;

namespace JsInterop
{
    public interface IJsValue
    {
        public object RawValue { get; }
        public bool TruthyValue { get; }
        public double NumberValue { get; }
        public JsValue GetProp(JsValue key);
        public JsValue Invoke(string functionName, params JsValue[] values);
        public JsValue Invoke(string functionName, JsValue param1 = default, JsValue param2 = default, JsValue param3 = default);
        public JsValue EvaluateOnThis(string functionBody);

        public T As<T>();
        public object As(Type type);
    }
}
