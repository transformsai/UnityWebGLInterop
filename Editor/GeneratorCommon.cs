using System;
using System.Linq;
using System.Reflection;
using System.Text;
using TransformsAI.Unity.WebGL.Interop.Internal;

namespace TransformsAI.Unity.WebGL.Interop.Editor
{
    internal static class GeneratorCommon {

        public static readonly MethodInfo[] RuntimeMethods =
            typeof(RuntimeRaw).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(it => it.GetMethodBody() == null).ToArray();
    
        private static StringBuilder _Builder;
        public const string InstanceModuleKey = "UnityJsInteropInstance";
        public const string ConstructorModuleKey = "UnityJsInterop";

        public static StringBuilder Builder
        {
            get
            {
                for (var i = 0; i < IndentHolder.Level; i++) _Builder.Append("  ");
                return _Builder;
            }
            set => _Builder = value;
        }
    
        public static IndentHolder Brace(this StringBuilder builder)
        {
            builder.AppendLine("{");
            return new IndentHolder("}");
        }
        public static IndentHolder Indent(this StringBuilder builder)
        {
            builder.AppendLine();
            return new IndentHolder();
        }

        public class IndentHolder : IDisposable
        {
            private readonly string _closer;
            public IndentHolder(string closer = null)
            {
                _closer = closer;
                Level++;
            }

            public static int Level;
            public void Dispose()
            {
                Level--;
                Builder?.AppendLine(_closer);
            }
        }
    }
}
