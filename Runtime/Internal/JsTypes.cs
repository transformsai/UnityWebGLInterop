using System;

namespace JsInterop.Internal
{
    public enum JsTypes
    {
        Exception = -1,
        Undefined = 0,
        Null = 1,
        Bool = 2,
        Number = 3,
        BigInt = 4,
        String = 5,
        Symbol = 6,
        Object = 7,
        Function = 8,
        Callback = 9,
        Promise = 10,
        Array = 11,
        TypedArray = 12,
        SharedTypedArray = 13,
    }


    public static class JsEnumExtensions
    {

        public static bool IsValueType(this JsTypes type)
        {
            switch (type)
            {
                case JsTypes.Undefined:
                case JsTypes.Null:
                case JsTypes.Bool:
                case JsTypes.Number:
                    return true;
                case JsTypes.BigInt:
                case JsTypes.String:
                case JsTypes.Symbol:
                case JsTypes.Object:
                case JsTypes.Function:
                case JsTypes.Callback:
                case JsTypes.Promise:
                case JsTypes.Array:
                case JsTypes.TypedArray:
                case JsTypes.SharedTypedArray:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
