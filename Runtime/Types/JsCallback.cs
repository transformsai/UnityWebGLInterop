using System;
using System.Collections.Generic;
using JsInterop.Internal;

namespace JsInterop.Types
{
    public class JsCallback : JsFunction
    {
        private Delegate Delegate { get; }

        internal JsCallback(double refId, Delegate callback) : base(refId, JsTypes.Callback)
        {
            Delegate = callback;
        }

        public override JsValue Call(params JsValue[] args) => Call(args);
        public override JsValue Call(JsValue arg1 = default, JsValue arg2 = default, JsValue arg3 = default) => Call(new[] { arg1, arg2, arg3 });

        public JsValue Call(IList<JsValue> args)
        {
            var paramList = Delegate.Method.GetParameters();
            var argArray = new object[paramList.Length];
            for (var i = 0; i < paramList.Length; i++)
            {
                var paramType = paramList[i].ParameterType;
                var argument = args.Count > i ? args[i] : JsValue.Undefined;
                argArray[i] = argument.As(paramType);
            }
            var result = Delegate.DynamicInvoke(argArray);
            return result == null ? JsValue.Undefined : JsRuntime.CreateFromObject(result);
        }
    
    
    }
}
