using System.Collections;
using System.Collections.Generic;
using JsInterop.Internal;

namespace JsInterop.Types
{
    public class JsArray : JsObject, IList<JsValue>
    {
        internal JsArray(double refId) : base(refId, JsTypes.Array) { }

        public IEnumerator<JsValue> GetEnumerator()
        {
            var count = Count;
            for (var i = 0; i < count; i++) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(JsValue item) => Invoke("push", item);

        public void Clear() => SetProp("length", 0);

        public bool Contains(JsValue item) => Invoke("includes", item);

        public void CopyTo(JsValue[] array, int index)
        {
            for (var i = 0; i < Count; i++) array[index++] = this[i];
        }

        public bool Remove(JsValue item)
        {
            var index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public int Count => GetProp("length").As<int>();
        public bool IsReadOnly => false;
        public int IndexOf(JsValue item) => Invoke("indexOf", item).As<int>();
        public void Insert(int index, JsValue item) => Invoke("splice", index, 0, item);
        public void RemoveAt(int index) => Invoke("splice", index, 1);

        public JsValue this[int index]
        {
            get => JsRuntime.GetArrayElement(this, index);
            set => JsRuntime.SetArrayElement(this, index, value);
        }

        public override bool TruthyValue => Count > 0;
    }
}
