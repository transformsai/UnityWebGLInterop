using System;
using System.Runtime.CompilerServices;
using JsInterop.Internal;

namespace JsInterop.Types
{
    public class JsFunction : JsObject
    {
        private static readonly ConditionalWeakTable<string, JsFunction> FunctionCache = new ConditionalWeakTable<string, JsFunction>();

        internal JsFunction(double refId, JsTypes typeId = JsTypes.Function) : base(refId, typeId) { }

        public JsFunction Bind(JsValue thisObj) => Invoke("Bind", thisObj).As<JsFunction>();
        public virtual JsValue Construct(JsValue[] values) => Runtime.Construct(this, values);
        public virtual JsValue Construct(JsValue param1 = default, JsValue param2 = default, JsValue param3 = default) => Runtime.Construct(this, param1, param2, param3);
        public virtual JsValue Call(params JsValue[] args) => Runtime.Call(this, args);
        public virtual JsValue Call(JsValue arg1 = default, JsValue arg2 = default, JsValue arg3 = default) => Runtime.Call(this, arg1, arg2, arg3);

        public static bool TryGetFunction(string str, out JsFunction jsFunction) => FunctionCache.TryGetValue(str, out jsFunction);
        public static void StoreFunction(string str, JsFunction jsFunction)
        {
            if (jsFunction == null) throw new NullReferenceException("Tried to store null reference");
            FunctionCache.Add(str, jsFunction);
        }

    }
}
