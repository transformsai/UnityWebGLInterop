using System;
using System.Linq;
using System.Reflection;
using System.Text;
using TransformsAI.Unity.WebGL.Interop.Internal;

namespace TransformsAI.Unity.WebGL.Interop.Editor
{
    internal static class GeneratorCommon {

        // There is no way to get `extern` methods. Need to search for static methods with no body.
        public static readonly MethodInfo[] RuntimeMethods =
            typeof(RuntimeRaw).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(it => it.GetMethodBody() == null).ToArray();
    
        
        // The initialize method requires special treatment, since it is in charge of instantiating the rest of the
        // implementation.
        internal static bool IsInitializeMethod(MethodInfo method) =>
            method.Name == nameof(RuntimeRaw.InitializeInternal);


        public static void CheckStandardMethod(MethodInfo method)
        {
            var paramList = method.GetParameters();

            var isStandardMethod =
                method.ReturnType != typeof(void) &&
                paramList.Length > 0 && paramList[0].IsOut &&
                paramList[0].ParameterType.GetElementType() == typeof(int);

            if (!isStandardMethod)
                throw new Exception($"Unsupported extern method {method.Name} in {method.DeclaringType}");
        }


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
