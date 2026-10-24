using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BoomYard.EditorTools
{
    /// <summary>
    /// Creates the render pipeline from code: a URP asset with a Universal Renderer (post-processing data, SSAO),
    /// assigned to every quality level, and template materials the runtime copies. Materials made at runtime
    /// only get their shader variants into a build if some material asset uses them, hence the templates.
    /// Everything lands in Assets/BoomYard/Generated (not committed: rebuilt on first open and before builds).
    /// </summary>
    public static class PipelineSetup
    {
        public const string Dir = "Assets/BoomYard/Generated";
        const string Templates = Dir + "/Resources/Templates";
        const string AssetPath = Dir + "/URP.asset";
        const string RendererPath = Dir + "/URP_Renderer.asset";

        public static void Run()
        {
            System.IO.Directory.CreateDirectory(Templates);
            AssetDatabase.Refresh();
            var asset = EnsurePipeline();
            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
            EnsureTemplates();
            AssetDatabase.SaveAssets();
        }

        static UniversalRenderPipelineAsset EnsurePipeline()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null) data = CreateRenderer();
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(data);
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            asset.supportsHDR = true;
            asset.msaaSampleCount = 1;            // SMAA on the camera instead: cheaper on phones
            asset.renderScale = 1f;
            asset.shadowDistance = 70f;
            asset.shadowCascadeCount = 2;
            asset.supportsCameraDepthTexture = true;
            asset.maxAdditionalLightsCount = 8;
            var so = new SerializedObject(asset);
            Set(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_MainLightShadowmapResolution", 2048);
            SetInt(so, "m_AdditionalLightsRenderingMode", (int)LightRenderingMode.PerPixel);
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 8);
            Set(so, "m_AdditionalLightShadowsSupported", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>The renderer as URP's own "Create → URP Asset" makes it (with its post-processing data), plus SSAO.</summary>
        static UniversalRendererData CreateRenderer()
        {
            UniversalRendererData data = null;
            var create = typeof(UniversalRenderPipelineAsset).GetMethod("CreateRendererAsset", BindingFlags.NonPublic | BindingFlags.Static);
            if (create != null)
                data = create.Invoke(null, new object[] { RendererPath, RendererType.UniversalRenderer, false, "Renderer" }) as UniversalRendererData;
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UniversalRendererData>();
                var pp = typeof(PostProcessData).GetMethod("GetDefaultPostProcessData", BindingFlags.NonPublic | BindingFlags.Static);
                if (pp != null) data.postProcessData = pp.Invoke(null, null) as PostProcessData;
                AssetDatabase.CreateAsset(data, RendererPath);
            }
            data.renderingMode = RenderingMode.Forward;

            var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssao.name = "SSAO";
            AssetDatabase.AddObjectToAsset(ssao, data);
            data.rendererFeatures.Add(ssao);
            string guid;
            long localId;
            var so = new SerializedObject(data);
            var map = so.FindProperty("m_RendererFeatureMap");
            if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out guid, out localId))
            {
                map.InsertArrayElementAtIndex(map.arraySize);
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var sso = new SerializedObject(ssao);
            SetFloat(sso, "m_Settings.Intensity", 1.5f);
            sso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>One template per keyword set the game uses at runtime.</summary>
        static void EnsureTemplates()
        {
            const string lit = "Universal Render Pipeline/Lit";
            Template("Lit", lit, m => { });
            Template("LitTex", lit, m =>
            {
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_SmoothnessTextureChannel", 1f);
                m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            });
            Template("LitEmissive", lit, m =>
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
            Template("LitTexEmissive", lit, m =>
            {
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_SmoothnessTextureChannel", 1f);
                m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
            });
            Template("Glass", lit, m =>
            {
                m.SetFloat("_Surface", 1f);   // transparent
                m.SetFloat("_Blend", 1f);     // premultiply: reflections stay bright on clear glass
                BaseShaderGUI.SetupMaterialBlendMode(m);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetFloat("_Smoothness", 0.95f);
            });
            Template("Smoke", "Universal Render Pipeline/Particles/Simple Lit", m =>
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);     // alpha
                BaseShaderGUI.SetupMaterialBlendMode(m);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetFloat("_Smoothness", 0f);
                m.SetColor("_BaseColor", Color.white);
            });
            Template("Sky", "Skybox/Procedural", m => { });
        }

        static void Template(string name, string shaderName, System.Action<Material> setup)
        {
            string path = Templates + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogError("BoomYard: shader not found: " + shaderName); return; }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool fresh = m == null;
            if (fresh) m = new Material(shader) { name = name };
            m.shader = shader;
            setup(m);
            if (fresh) AssetDatabase.CreateAsset(m, path);
            EditorUtility.SetDirty(m);
        }

        static void Set(SerializedObject so, string prop, bool v) { var p = so.FindProperty(prop); if (p != null) p.boolValue = v; else Debug.LogWarning("BoomYard: no " + prop); }
        static void SetInt(SerializedObject so, string prop, int v) { var p = so.FindProperty(prop); if (p != null) p.intValue = v; else Debug.LogWarning("BoomYard: no " + prop); }
        static void SetFloat(SerializedObject so, string prop, float v) { var p = so.FindProperty(prop); if (p != null) p.floatValue = v; else Debug.LogWarning("BoomYard: no " + prop); }
    }
}
