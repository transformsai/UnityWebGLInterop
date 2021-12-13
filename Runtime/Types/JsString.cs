using System;
using System.Runtime.CompilerServices;
using JsInterop.Internal;

namespace JsInterop.Types
{
    public class JsString : JsReference
    {

        // There are 2 ways create a string cache
        // either it's created in C# or it's created in JS.
        // In both cases, the string is cached until all C# references disappear.
        // In JS, we use a value-based map to make sure that we don't recreate strings that already exists.
        // This avoids allocating a string every time we pass it to JS.
        // However, This comes at the cost of checking against a string table upon creation.
        // This incurs a call to getHashCode which is of O(n) where n is the size of the string.
        // This is still desirable, as instantiating a string is also O(n) time as well as space.
        // However, we can avoid the repeating the getHashCode operation on both C# and JS by using ConditionalWeakTable
        // This makes sure that repeated calls to TryGetJsString with the same C# string return the same JsString instance
        // This is a constant time lookup based on the GCHandle of the current reference.
        // However, if the C# cache fails, the JS cache will check the string value using a JS map.
        // JS is guaranteed to return the same Value for equal strings.
        // Examples:
        // var x = JsRuntime.CreateString("Cheem"); // Creates a string in JS
        // var y = JsRuntime.CreateString("Cheem"); // y and x are the same reference
        private static readonly ConditionalWeakTable<string, JsString> StringCache = new ConditionalWeakTable<string, JsString>();
        public static bool TryGetString(string str, out JsString jsString) => StringCache.TryGetValue(str, out jsString);
        public static void StoreString(string str, JsString jsStr)
        {
            if (jsStr == null) throw new NullReferenceException("Tried to store null reference");
            StringCache.Add(str, jsStr);
        }


        private string _valueCache;

        private string Value
        {
            get
            {
                if (_valueCache != null) return _valueCache;
                _valueCache = JsRuntime.GetString(this);
                StoreString(_valueCache, this);
                return _valueCache;
            }
        }

        internal JsString(double refId, string initialValue = null) : base(JsTypes.String, refId)
        {
            _valueCache = initialValue;
            if(_valueCache != null) StoreString(_valueCache, this);
        }

    
        public override object RawValue => Value;
        public override bool TruthyValue => !string.IsNullOrEmpty(Value);
        public override string ToString() => Value;
    }
}
