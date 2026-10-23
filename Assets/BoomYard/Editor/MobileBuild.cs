using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BoomYard.EditorTools
{
    /// <summary>
    /// Command-line builds of the phone apps, used by CI (.github/workflows/mobile.yml):
    /// Android (signed App Bundle for Google Play when a keystore is given, otherwise an installable APK)
    /// and iOS (an Xcode project, archived and signed later on a Mac).
    /// GameCI passes the version, build number and keystore as -arguments.
    /// </summary>
    public static class MobileBuild
    {
        /// <summary>Store id of the app. Permanent once the app is published: change it before the first upload if needed.</summary>
        public const string AppId = "com.boomyard.game";

        [MenuItem("BoomYard/Build Android")]
        public static void BuildAndroid()
        {
            var scenes = Common();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // Google Play requires 64-bit; the target API follows the newest SDK in the build image.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            int code;
            if (int.TryParse(Arg("-androidVersionCode"), out code) && code > 0) PlayerSettings.Android.bundleVersionCode = code;

            string keystore = Arg("-androidKeystoreName");
            bool signed = !string.IsNullOrEmpty(keystore) && File.Exists(keystore);
            PlayerSettings.Android.useCustomKeystore = signed;
            if (signed)
            {
                PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
                PlayerSettings.Android.keystorePass = Arg("-androidKeystorePass");
                PlayerSettings.Android.keyaliasName = Arg("-androidKeyaliasName");
                PlayerSettings.Android.keyaliasPass = Arg("-androidKeyaliasPass");
            }
            // Play takes App Bundles signed with the upload key; without a key, build an APK to install for testing.
            bool bundle = signed && Arg("-androidExportType") != "androidPackage";
            EditorUserBuildSettings.buildAppBundle = bundle;
            Build(scenes, BuildTarget.Android, "build/Android/BoomYard" + (bundle ? ".aab" : ".apk"));
        }

        [MenuItem("BoomYard/Build iOS (Xcode project)")]
        public static void BuildIOS()
        {
            var scenes = Common();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.requiresFullScreen = true;
            string number = Arg("-androidVersionCode");   // the same running build number as Android
            if (!string.IsNullOrEmpty(number)) PlayerSettings.iOS.buildNumber = number;
            Build(scenes, BuildTarget.iOS, "build/iOS");
        }

        /// <summary>Settings shared with the web build: name, version, landscape, icon, no splash.</summary>
        static string[] Common()
        {
            var scenes = ProjectSetup.PrepareBuild();
            string version = Arg("-buildVersion");
            if (!string.IsNullOrEmpty(version) && version != "none") PlayerSettings.bundleVersion = version;
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            return scenes;
        }

        static void Build(string[] scenes, BuildTarget target, string path)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });
            Debug.Log("BoomYard " + target + " build: " + report.summary.result + ", " + report.summary.totalSize + " bytes -> " + path);
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>The value after a command-line flag, or "".</summary>
        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return "";
        }
    }
}
