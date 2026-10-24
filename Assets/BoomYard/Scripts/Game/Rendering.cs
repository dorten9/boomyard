using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BoomYard.Game
{
    /// <summary>
    /// The look (URP): golden-hour sun and procedural sky, ambient light and reflections from that sky, the
    /// post-processing stack (ACES, bloom, colour grade, vignette, grain) and the short punches it gives each
    /// explosion (exposure flash, chromatic aberration, lens distortion).
    /// </summary>
    public sealed class Rendering : MonoBehaviour
    {
        public static Rendering I { get; private set; }

        Volume volume;
        ColorAdjustments grade;
        ChromaticAberration aberration;
        LensDistortion lens;
        Vignette vignette;
        float flash, punch, baseExposure;
        ReflectionProbe probe;

        public static Rendering Create(QualityTier tier)
        {
            var go = new GameObject("Rendering");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<Rendering>();
            r.BuildVolume(tier);
            ApplyTier(tier);
            return r;
        }

        void Awake() { I = this; }

        /// <summary>Per-tier pipeline settings (spec §15: three automatic profiles; WebGL fixed at Low).</summary>
        public static void ApplyTier(QualityTier tier)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) return;
            // Plain bilinear upscale: the FSR blit shader is stripped from builds, and without it URP skips all post.
            urp.upscalingFilter = UpscalingFilterSelection.Linear;
            switch (tier)
            {
                case QualityTier.High:
                    urp.renderScale = 1f;
                    urp.shadowDistance = 70f;
                    urp.shadowCascadeCount = 2;
                    break;
                case QualityTier.Medium:
                    urp.renderScale = 0.85f;
                    urp.shadowDistance = 50f;
                    urp.shadowCascadeCount = 2;
                    break;
                default:
                    urp.renderScale = 0.8f;
                    urp.shadowDistance = 40f;
                    urp.shadowCascadeCount = 1;
                    break;
            }
        }

        public static void SetupCamera(Camera cam, QualityTier tier)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = tier == QualityTier.Low ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            data.requiresDepthOption = CameraOverrideOption.On;
            cam.allowHDR = true;
        }

        void BuildVolume(QualityTier tier)
        {
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = p;

            var tone = p.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            var bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.72f);
            bloom.highQualityFiltering.Override(tier == QualityTier.High);

            grade = p.Add<ColorAdjustments>(true);
            baseExposure = 0.35f;
            grade.postExposure.Override(baseExposure);
            grade.contrast.Override(14f);
            grade.saturation.Override(6f);

            var wb = p.Add<WhiteBalance>(true);
            wb.temperature.Override(9f);
            wb.tint.Override(2f);

            var lgg = p.Add<LiftGammaGain>(true);
            lgg.lift.Override(new Vector4(0.98f, 1f, 1.04f, 0f));     // cool shadows
            lgg.gain.Override(new Vector4(1.04f, 1f, 0.95f, 0f));     // warm highlights

            vignette = p.Add<Vignette>(true);
            vignette.intensity.Override(0.24f);
            vignette.smoothness.Override(0.45f);

            aberration = p.Add<ChromaticAberration>(true);
            aberration.intensity.Override(0.04f);
            lens = p.Add<LensDistortion>(true);
            lens.intensity.Override(0f);

            if (tier != QualityTier.Low)
            {
                var grain = p.Add<FilmGrain>(true);
                grain.type.Override(FilmGrainLookup.Thin1);
                grain.intensity.Override(0.12f);
                grain.response.Override(0.85f);
            }
        }

        /// <summary>
        /// Golden hour: low warm sun with soft shadows, a procedural sky to match, ambient light and a reflection
        /// probe taken from that sky, and a light warm haze.
        /// </summary>
        public static Light Sunset(Transform root, Vector3 probeCentre)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.76f, 0.52f);
            sun.intensity = 2.3f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.92f;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.3f;
            sun.transform.rotation = Quaternion.Euler(13f, -58f, 0f);
            RenderSettings.sun = sun;

            // A soft blue fill from the other side, like the open sky opposite the sun.
            var fill = new GameObject("SkyFill").AddComponent<Light>();
            fill.transform.SetParent(root, false);
            fill.type = LightType.Directional;
            fill.color = new Color(0.55f, 0.66f, 0.9f);
            fill.intensity = 0.25f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(35f, 130f, 0f);

            var sky = Mats.From("Sky");
            sky.SetFloat("_SunDisk", 2f);            // high quality sun disk
            sky.SetFloat("_SunSize", 0.045f);
            sky.SetFloat("_SunSizeConvergence", 4f);
            sky.SetFloat("_AtmosphereThickness", 1.35f);
            sky.SetColor("_SkyTint", new Color(0.55f, 0.55f, 0.6f));
            sky.SetColor("_GroundColor", new Color(0.36f, 0.31f, 0.28f));
            sky.SetFloat("_Exposure", 1.25f);
            RenderSettings.skybox = sky;

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.86f, 0.68f, 0.56f);
            RenderSettings.fogDensity = 0.0085f;
            DynamicGI.UpdateEnvironment();

            // Local reflections so car paint and glass mirror the yard, rendered once after the arena is built.
            var pgo = new GameObject("Reflections");
            pgo.transform.SetParent(root, false);
            pgo.transform.position = probeCentre;
            var probe = pgo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(60f, 25f, 60f);
            probe.resolution = 128;
            probe.hdr = true;
            probe.boxProjection = false;
            if (I != null) I.probe = probe;
            return sun;
        }

        /// <summary>Re-renders the reflections (after the arena is built, and after it is wrecked).</summary>
        public void RefreshReflections()
        {
            if (probe != null) probe.RenderProbe();
        }

        /// <summary>The camera's reaction to a blast: a flash of exposure and a kick of lens distortion, by closeness.</summary>
        public void Kick(float strength)
        {
            if (GameController.ReduceFlashes) strength *= 0.3f;
            flash = Mathf.Max(flash, Mathf.Clamp01(strength));
            punch = Mathf.Max(punch, Mathf.Clamp01(strength));
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            flash = Mathf.MoveTowards(flash, 0f, dt * 2.2f);
            punch = Mathf.MoveTowards(punch, 0f, dt * 3f);
            if (grade != null) grade.postExposure.value = baseExposure + flash * 1.6f;
            if (aberration != null) aberration.intensity.value = 0.04f + punch * 0.5f;
            if (lens != null) lens.intensity.value = -punch * 0.28f;
            if (vignette != null) vignette.intensity.value = 0.24f + punch * 0.15f;
        }
    }
}
