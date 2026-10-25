using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BoomYard.EditorTools
{
    /// <summary>
    /// One-time project setup that would otherwise need clicking through the editor: main scene + build settings,
    /// landscape-only player settings, linear colour, and the shaders the runtime looks up by name.
    /// Runs automatically after scripts compile; also available from the BoomYard menu.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/BoomYard/Scenes/Main.unity";
        const string DoneKey = "BoomYard.ProjectSetup.v1";

        /// <summary>Built-in shaders the UI uses (the world's URP materials come from PipelineSetup's templates).</summary>
        public static readonly string[] RuntimeShaders = { "UI/Default", "Sprites/Default" };

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(DoneKey, false))
                {
                    SessionState.SetBool(DoneKey, true);
                    Run(false);
                }
            };
        }

        [MenuItem("BoomYard/Run Project Setup")]
        public static void RunFromMenu() { Run(true); }

        static void Run(bool verbose)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            EnsureScene();
            PipelineSetup.Run();
            EnsureShaders(RuntimeShaders);
            EnsureFogVariants();

            ApplyPlayerSettings();
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = "0.1.0";

            if (verbose) Debug.Log("BoomYard: project setup done.");
        }

        /// <summary>Landscape only (first person does not work in portrait, spec §02), on every platform. Also called by CI builds.</summary>
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "BoomYard";
            PlayerSettings.productName = "BoomYard";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.useAnimatedAutorotation = true;
            // Night realism needs linear lighting (WebGL 2, GLES3/Vulkan/Metal all support it).
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true);
            ApplyAppIcon();
        }

        const string AppIconPath = "Assets/BoomYard/Icons/AppIcon.png";

        /// <summary>Default app icon for every platform; Unity scales it to each required size.</summary>
        static void ApplyAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null) { Debug.LogWarning("BoomYard: app icon not found at " + AppIconPath); return; }
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        public static void EnsureScene()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                System.IO.Directory.CreateDirectory("Assets/BoomYard/Scenes");
                // Unity refuses an additive new scene while the open one is untitled (always the case in batch
                // builds), so replace an untitled, unchanged scene; otherwise add one and close it again.
                var open = EditorSceneManager.GetActiveScene();
                bool replace = string.IsNullOrEmpty(open.path) && !open.isDirty;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replace ? NewSceneMode.Single : NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!replace) EditorSceneManager.CloseScene(scene, true);
                AssetDatabase.Refresh();
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            // Open the main scene if the editor is sitting on an empty untitled scene.
            var active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) && !active.isDirty && !Application.isBatchMode)
                EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>Adds shaders to Graphics Settings → Always Included, so materials made at runtime find them in builds.</summary>
        public static void EnsureShaders(params string[] names)
        {
            var settings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            bool changed = false;
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogWarning("BoomYard: shader not found: " + name); continue; }
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) present = true;
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                changed = true;
            }
            if (changed)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// The arenas set exponential-squared fog from code, and the scene has none, so automatic stripping would drop
        /// the fog variants from builds: keep them explicitly (Graphics Settings → Fog Modes → Custom).
        /// </summary>
        public static void EnsureFogVariants()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var strip = so.FindProperty("m_FogStripping");
            var exp2 = so.FindProperty("m_FogKeepExp2");
            if (strip == null || exp2 == null) return;
            if (strip.intValue == 1 && exp2.boolValue) return;
            strip.intValue = 1;
            exp2.boolValue = true;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Everything a command-line build needs before BuildPipeline runs.</summary>
        public static string[] PrepareBuild()
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color32(0x0C, 0x0B, 0x0D, 0xFF);
            ApplyPlayerSettings();
            PipelineSetup.Run();
            EnsureShaders(RuntimeShaders);
            EnsureFogVariants();
            EnsureScene();
            LinkPickerFrameworks();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            return scenes.Length == 0 ? new[] { ScenePath } : scenes;
        }

        /// <summary>The iPhone picture picker (Plugins/BoomImagePicker.mm) needs PhotosUI linked into the Xcode project.</summary>
        static void LinkPickerFrameworks()
        {
            var pi = AssetImporter.GetAtPath("Assets/BoomYard/Plugins/BoomImagePicker.mm") as PluginImporter;
            if (pi == null) return;
            pi.SetCompatibleWithAnyPlatform(false);
            pi.SetCompatibleWithPlatform(BuildTarget.iOS, true);
            pi.SetPlatformData(BuildTarget.iOS, "FrameworkDependencies", "PhotosUI;Photos;");
            pi.SaveAndReimport();
        }
    }
}
