using System;

namespace JsInterop.Internal
{
    public class JsException : Exception
    {
        public JsException(string message) : base(message) { }
    }
}
