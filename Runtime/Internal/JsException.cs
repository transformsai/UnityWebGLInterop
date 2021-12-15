using System;

namespace TransformsAI.Unity.WebGL.Interop.Internal
{
    public class JsException : Exception
    {
        public JsException(string message) : base(message) { }
    }
}
