using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BoomYard.EditorTools
{
    /// <summary>
    /// Command-line WebGL build used by CI (.github/workflows/web.yml). The browser build is for demos and testing,
    /// at the fixed Low quality profile (spec §16); the phones are the product.
    /// </summary>
    public static class BuildScript
    {
        const string WebOutput = "build/WebGL";

        [MenuItem("BoomYard/Build WebGL")]
        public static void BuildWebGL()
        {
            // As in Overlane: static hosts like Vercel don't send Content-Encoding for Unity's .br/.gz files,
            // so ship uncompressed files and let the host compress them over HTTP.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
#if UNITY_6000_0_OR_NEWER
            // The game needs about 200 MB once loaded. Start there instead of growing from 32 MB, and cap growth at
            // 1 GB: Safari on iPhone fails with "out of bounds memory access" when the heap grows or reserves more.
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
#else
            PlayerSettings.WebGL.memorySize = 768;
#endif
            PlayerSettings.runInBackground = false;
            PlayerSettings.WebGL.template = "PROJECT:BoomYard";
            var scenes = ProjectSetup.PrepareBuild();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = WebOutput,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            Debug.Log("BoomYard WebGL build: " + report.summary.result + ", " + report.summary.totalSize + " bytes");
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
