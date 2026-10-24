using UnityEngine;

namespace BoomYard.Game
{
    public enum QualityTier { Low, Medium, High }

    /// <summary>
    /// Three automatic quality profiles (spec §15): High 60 fps on iPhone 12 / Pixel 6, Medium 30 fps on iPhone XR,
    /// Low fixed for WebGL (one shadow cascade, short shadow distance, 30 fps) — enough for demos and testing.
    /// </summary>
    public static class Quality
    {
        public static QualityTier Tier { get; private set; }

        public static QualityTier Detect()
        {
#if UNITY_WEBGL
            return QualityTier.Low;
#else
            if (SystemInfo.systemMemorySize >= 3800 && SystemInfo.processorCount >= 6) return QualityTier.High;
            if (SystemInfo.systemMemorySize >= 2500) return QualityTier.Medium;
            return QualityTier.Low;
#endif
        }

        static void TextureLimit(int mips)
        {
#if UNITY_2022_2_OR_NEWER
            QualitySettings.globalTextureMipmapLimit = mips;
#else
            QualitySettings.masterTextureLimit = mips;
#endif
        }

        public static void Apply(QualityTier tier)
        {
            Tier = tier;
            TextureLib.Scale = tier == QualityTier.Low ? 0.5f : 1f;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            switch (tier)
            {
                case QualityTier.High:
                    Application.targetFrameRate = 60;
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.shadowDistance = 45f;
                    QualitySettings.pixelLightCount = 6;
                    QualitySettings.antiAliasing = 2;
                    TextureLimit(0);
                    break;
                case QualityTier.Medium:
                    Application.targetFrameRate = 30;
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.shadowDistance = 30f;
                    QualitySettings.pixelLightCount = 4;
                    QualitySettings.antiAliasing = 0;
                    TextureLimit(0);
                    break;
                default:
                    Application.targetFrameRate = 30;
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowCascades = 1;
                    QualitySettings.shadowDistance = 22f;
                    QualitySettings.pixelLightCount = 3;
                    QualitySettings.antiAliasing = 0;
                    TextureLimit(1);   // 1K textures
                    break;
            }
        }
    }
}
