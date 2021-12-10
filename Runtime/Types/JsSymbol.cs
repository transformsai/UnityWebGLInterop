using JsInterop.Internal;

namespace JsInterop.Types
{
    public class JsSymbol : JsReference
    {
        private string _labelCache;

        public string Label => _labelCache ?? (_labelCache = GetJsStringImpl());
    
        internal JsSymbol(double refId) : base(JsTypes.Symbol, refId) { }
        public override object RawValue => this;
        public override bool TruthyValue => true;

        public override string ToString() => Label;
    }
}
