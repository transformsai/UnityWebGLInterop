using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JsInterop.Internal;

namespace JsInterop.Types
{
    public class JsObject : JsReference
    {
        public override object RawValue => this;
        public override bool TruthyValue => true;

        internal JsObject(double refId, JsTypes typeId = JsTypes.Object) : base(typeId, refId) { }
        public void SetProp(string key, JsValue value) => JsRuntime.SetProp(this, key, value);
        public void Populate(IDictionary dictionary)
        {
            var keys = dictionary.Keys as ICollection<string>;
            if (keys == null) throw new Exception("Unsupported dictionary with non-string keys");

            foreach (var key in keys)
            {
                var rawValue = dictionary[key];
                var value = JsRuntime.CreateFromObject(rawValue);
                SetProp(key, value);
            }
        }

        public JsArray Keys => JsRuntime.GetGlobalValue("Object").Invoke("keys", this).As<JsArray>();


        public Dictionary<string, JsValue> AsDictionary()
        {
            return Keys.ToDictionary(it=>(string)it, GetProp);
        }

    }
}
