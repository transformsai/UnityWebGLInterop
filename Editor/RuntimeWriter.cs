using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JsInterop.Editor
{
    [InitializeOnLoad]
    public class RuntimeWriter : ScriptableObject
    {
        // We use static references to access these.
        // This can be set by selecting the RuntimeWriter script
        public MonoScript rawRuntime;
        public MonoScript tsGenerator;
        public MonoScript jsGenerator;
        public Object jsLib;
        public Object typescriptRuntime;


        public static void UpdateLibs()
        {
            var r = CreateInstance<RuntimeWriter>();
            var jsLibPath = AssetDatabase.GetAssetPath(r.jsLib);
            var tsLibPath = AssetDatabase.GetAssetPath(r.typescriptRuntime);
            var rawPath = AssetDatabase.GetAssetPath(r.rawRuntime);
            var jsGeneratorPath = AssetDatabase.GetAssetPath(r.jsGenerator);
            var tsGeneratorPath = AssetDatabase.GetAssetPath(r.tsGenerator);

            var dirtyTimes = new[]
            {
                File.GetLastWriteTime(rawPath),
                File.GetLastWriteTime(jsGeneratorPath),
                File.GetLastWriteTime(tsGeneratorPath)
            };

            var dirtiestTime = dirtyTimes.Max();

            if (dirtiestTime > File.GetLastWriteTime(jsLibPath))
            {
                var jsLib = JsLibGenerator.GenerateJsLib();
                var tsLib = TsLibGenerator.GenerateTsLib();
                File.WriteAllText(jsLibPath, jsLib);
                File.WriteAllText(tsLibPath, tsLib);   
            }
        }
    
        static RuntimeWriter()
        {
            EditorApplication.update += AfterLoad; 
        }

        private static void AfterLoad()
        {
            EditorApplication.update -= AfterLoad;
            UpdateLibs();
        }
    }
}
