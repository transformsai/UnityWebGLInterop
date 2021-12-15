using System;
using System.Numerics;
using TransformsAI.Unity.WebGL.Interop.Internal;

namespace TransformsAI.Unity.WebGL.Interop.Types
{
    public class JsBigInt : JsReference
    {
        public static implicit operator JsBigInt(BigInteger b) => JsRuntime.CreateBigInt(b);
        public static implicit operator BigInteger(JsBigInt b) => b.Value;


        private BigInteger? _valueCache;
        public BigInteger Value => _valueCache ?? (_valueCache = GetValue()).Value;
        public override object RawValue => Value;
        public override bool TruthyValue => Value != BigInteger.Zero;
        public override double NumberValue => (double)Value;

        internal JsBigInt(double refId) : base(JsTypes.BigInt, refId) { }


        private BigInteger GetValue()
        {
            var str = GetJsStringImpl();
            if (!str.EndsWith("n")) throw new FormatException("Js BigInt string representation did not end with n.");
            str = str.TrimEnd('n');
            return BigInteger.Parse(str);
        }

    }
}
