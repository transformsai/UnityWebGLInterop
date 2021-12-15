using System;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using TransformsAI.Unity.WebGL.Interop.Internal;

namespace TransformsAI.Unity.WebGL.Interop
{
    public readonly struct JsValue : IJsValue, IEquatable<JsValue>
    {
        public readonly JsTypes TypeId;
        internal readonly double Value;
        private readonly JsReference _reference;

        public static readonly JsValue Undefined = default;
        public static readonly JsValue Null = new JsValue(JsTypes.Null, 0);
        public static readonly JsValue True = new JsValue(JsTypes.Bool, 1);
        public static readonly JsValue False = new JsValue(JsTypes.Bool, 0);

        public object RawValue
        {
            get
            {
                switch (TypeId)
                {
                    case JsTypes.Undefined: return Undefined;
                    case JsTypes.Null: return null;
                    case JsTypes.Bool: return Value != 0;
                    case JsTypes.Number: return Value;
                    default: return _reference.RawValue;

                }
            }
        }


        public bool TruthyValue
        {
            get
            {
                switch (TypeId)
                {
                    case JsTypes.Undefined: return false;
                    case JsTypes.Null: return false;
                    case JsTypes.Bool: return Value != 0;
                    case JsTypes.Number: return !double.IsNaN(Value) && Value != 0 && Value != -0;
                    default: return _reference.TruthyValue;

                }
            }
        }

        public double NumberValue
        {
            get
            {
                switch (TypeId)
                {
                    case JsTypes.Undefined: return double.NaN;
                    case JsTypes.Null: return 0;
                    case JsTypes.Bool: return Value == 0 ? 0 : 1;
                    case JsTypes.Number: return Value;
                    default: return _reference.NumberValue;
                }
            }
        }

        public override string ToString()
        {
            switch (TypeId)
            {
                case JsTypes.Undefined: return "undefined";
                case JsTypes.Null: return "null";
                case JsTypes.Bool: return Value == 0 ? "false" : "true";
                case JsTypes.Number: return Value.ToString(CultureInfo.InvariantCulture);
                default: return _reference?.ToString() ?? "";
            }
        }

        private JsValue(JsTypes typeId, double value)
        {
            TypeId = typeId;
            Value = value;
            _reference = null;

            if (!typeId.IsValueType()) throw new ArgumentException(
                $"Tried to construct reference type ({typeId}) without Reference ", nameof(typeId));
        }

        internal JsValue(JsTypes typeId, double value, JsReference reference)
        {
            TypeId = typeId;
            Value = value;
            _reference = reference;

            if (reference == null) throw new ArgumentNullException(nameof(reference),
                $"Cannot create JsValue with reference typeId {typeId} with null reference");
        }

        public JsValue GetProp(JsValue key) => JsRuntime.GetProp(this, key);

        public JsValue Invoke(string functionName, params JsValue[] values) =>
            JsRuntime.Invoke(this, functionName, values);
        public JsValue Invoke(string functionName, JsValue param1 = default, JsValue param2 = default, JsValue param3 = default) =>
            JsRuntime.Invoke(this, functionName, param1, param2, param3);


        public JsValue EvaluateOnThis(string functionBody) => JsRuntime.CreateFunction(functionBody, true).Bind(this).Call();

        public T As<T>()
        {
            // Stupidity check.
            if (this is T t) return t;

            // check for references
            if (_reference is T refT) return refT;

            if (TypeId == JsTypes.Null || TypeId == JsTypes.Undefined)
            {
                // this also takes care of Nullable struct types
                var def = default(T);
                if (def == null) return default;
                throw new NullReferenceException($"Could not convert {nameof(JsValue)} to {typeof(T).Name}. Value was {this}");
            }
            var type = typeof(T);

            // type coersion

            static T1 UnsafeAs<T1, T2>(T2 val) => Unsafe.As<T2, T1>(ref val);

            //using unsafe casts to avoid boxing
            if (type == typeof(double)) return UnsafeAs<T, double>(NumberValue);
            if (type == typeof(int)) return UnsafeAs<T, int>((int)NumberValue);
            if (type == typeof(float)) return UnsafeAs<T, float>((float)NumberValue);
            if (type == typeof(bool)) return UnsafeAs<T, bool>(TruthyValue);

            //nullable casts need to be done separately. Null value is handled above
            if (type == typeof(double?)) return UnsafeAs<T, double?>(NumberValue);
            if (type == typeof(int?)) return UnsafeAs<T, int?>((int)NumberValue);
            if (type == typeof(float?)) return UnsafeAs<T, float?>((float)NumberValue);
            if (type == typeof(bool?)) return UnsafeAs<T, bool?>(TruthyValue);

            if (type == typeof(string)) return (T)(object)ToString();


            // do this last since RawValue requires boxing the values. 
            if (RawValue is T t2) return t2;

            return (T)As(type);
        }

        public object As(Type type)
        {
            if (type.IsInstanceOfType(this)) return this;


            var typeIsNullable = !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
            var isJsNullLike = TypeId == JsTypes.Null || TypeId == JsTypes.Undefined;

            if (typeIsNullable && isJsNullLike) return null;

            if (!TypeId.IsValueType())
            {
                var jsRef = _reference;
                if (type.IsInstanceOfType(jsRef)) return jsRef;
            }

            var value = RawValue;

            // Null value is handled above, so for nullable types we only care about the underlying types
            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null) type = nullableType;

            // check if raw value is requested type (avoids more Js operations)
            if (type.IsInstanceOfType(value)) return value;

            // Implement type coersion (supports null)
            if (type == typeof(bool)) return TruthyValue;
            if (type == typeof(string)) return ToString();
            if (type == typeof(double)) return NumberValue;
            if (type == typeof(float)) return (float)NumberValue;
            if (type == typeof(int)) return (int)NumberValue;

            var converter = TypeDescriptor.GetConverter(value);

            if (converter.CanConvertTo(type)) return converter.ConvertTo(value, type);

            throw new InvalidCastException($"cannot convert JsValue of type {TypeId} to {type}");
        }

        #region Implicit Conversions

        // Map CLR types to JS types
        public static implicit operator JsValue(bool i) => i ? True : False;
        public static implicit operator JsValue(int i) => new JsValue(JsTypes.Number, i);
        public static implicit operator JsValue(float i) => new JsValue(JsTypes.Number, i);
        public static implicit operator JsValue(double i) => new JsValue(JsTypes.Number, i);
        public static implicit operator JsValue(BigInteger i) => JsRuntime.CreateBigInt(i);
        // String converter must also cover null case since it is the only reference converter.
        public static implicit operator JsValue(string i) => i == null ? Null : JsRuntime.CreateString(i);

        // Implement type coersion
        public static implicit operator bool(JsValue i) => i.TruthyValue;
        public static explicit operator string(JsValue i) => i.ToString();
        public static explicit operator double(JsValue i) => i.NumberValue;

        [Obsolete("Using this operator is inconsistent with JS semantics. Use .Equals() for === and .EqualsJs() for ==")]
        public static bool operator ==(JsValue lhs, JsValue rhs) => lhs.Equals(rhs);

        [Obsolete("Using this operator is inconsistent with JS semantics. Use .Equals() for === and .EqualsJs() for ==")]
        public static bool operator !=(JsValue lhs, JsValue rhs) => !(lhs == rhs);
        #endregion

        public bool Equals(JsValue other) => TypeId == other.TypeId && Value == other.Value && _reference == other._reference;
        public bool EqualsJs(JsValue other) => JsRuntime.Equals(this, other);

        public override bool Equals(object obj) => obj is JsValue other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) TypeId;
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                hashCode = (hashCode * 397) ^ (_reference != null ? _reference.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
