using System.Collections;
using System.Collections.Generic;
using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>Placeholder models of the arsenal, built from primitives (replaced by the PBR models in M4).</summary>
    public static class ItemModel
    {
        /// <summary>Builds the model with a child "FuseTip" at the end of the fuse. Size is real (metres).</summary>
        public static GameObject Build(ExplosiveDef def)
        {
            var root = new GameObject(def.Id);
            var paper = Mats.Surface("paint", new Color(0.62f, 0.13f, 0.1f), 0f, 0.35f, 0.1f, 0.4f);
            var band = Mats.Lit(new Color(0.82f, 0.78f, 0.68f), 0f, 0.25f);
            var cap = Mats.Lit(new Color(0.75f, 0.7f, 0.6f), 0f, 0.2f);
            float fuseLen = 0.05f;
            Vector3 tip;
            switch (def.Id)
            {
                case "cherry":
                    Part(root, MeshKit.Blob(0.022f, 0.05f, 900), Vector3.zero, Mats.Lit(new Color(0.58f, 0.07f, 0.05f), 0f, 0.55f));
                    tip = new Vector3(0f, 0.022f + fuseLen, 0f);
                    break;
                case "sparkler":
                    Part(root, MeshKit.Blob(0.032f, 0.08f, 901), Vector3.zero, Mats.Lit(new Color(0.35f, 0.35f, 0.38f), 0.3f, 0.35f));
                    tip = new Vector3(0f, 0.032f + fuseLen, 0f);
                    break;
                case "dynamite":
                    for (int i = 0; i < 3; i++) Tube(root, new Vector3((i - 1) * 0.029f, -0.1f, 0f), 0.0145f, 0.2f, Mats.Lit(new Color(0.62f, 0.12f, 0.08f), 0f, 0.3f), cap);
                    Part(root, MeshKit.RoundedBox(new Vector3(0.094f, 0.03f, 0.033f), 0.012f), Vector3.zero, Mats.Lit(new Color(0.15f, 0.15f, 0.16f), 0f, 0.5f));
                    tip = new Vector3(0f, 0.1f + fuseLen, 0f);
                    break;
                case "keg":
                    Part(root, MeshKit.Lathe("keg", new[] { new Vector2(0.17f, -0.25f), new Vector2(0.2f, -0.12f), new Vector2(0.21f, 0f), new Vector2(0.2f, 0.12f), new Vector2(0.17f, 0.25f) }, 20, true, true), Vector3.zero, Mats.Surface("wood", Color.white, 0f, 0.6f, 0.3f));
                    for (int i = -1; i <= 1; i += 2) Part(root, MeshKit.Lathe("keghoop", new[] { new Vector2(0.205f, -0.012f), new Vector2(0.212f, 0f), new Vector2(0.205f, 0.012f) }, 20), new Vector3(0f, i * 0.16f, 0f), Mats.Lit(Mats.DarkMetal, 0.8f, 0.4f));
                    tip = new Vector3(0f, 0.25f + fuseLen, 0f);
                    break;
                case "string50":
                    for (int i = 0; i < 10; i++)
                    {
                        var c = Tube(root, new Vector3((i - 4.5f) * 0.012f, -0.009f, 0f), 0.0045f, 0.018f, paper, band);
                        c.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 8f : -8f));
                    }
                    tip = new Vector3(-0.07f, 0.02f, 0f);
                    break;
                case "m80":
                    Tube(root, new Vector3(0f, -0.04f, 0f), 0.014f, 0.08f, Mats.Lit(new Color(0.16f, 0.17f, 0.2f), 0f, 0.35f), cap);
                    tip = new Vector3(0f, 0.04f + fuseLen, 0f);
                    break;
                case "pipe":
                    Tube(root, new Vector3(0f, -0.11f, 0f), 0.025f, 0.22f, Mats.Lit(Mats.Steel, 0.9f, 0.55f), Mats.Lit(new Color(0.3f, 0.3f, 0.32f), 0.9f, 0.4f));
                    tip = new Vector3(0f, 0.11f + fuseLen, 0f);
                    break;
                case "rocket":
                    // A red tube with a paper cone and a long guide stick.
                    Tube(root, new Vector3(0f, -0.05f, 0f), 0.014f, 0.11f, Mats.Lit(new Color(0.75f, 0.1f, 0.08f), 0f, 0.4f), cap);
                    Part(root, MeshKit.Lathe("rocketcone", new[] { new Vector2(0.015f, 0f), new Vector2(0f, 0.045f) }, 12, false, true), new Vector3(0f, 0.06f, 0f), Mats.Lit(new Color(0.9f, 0.75f, 0.2f), 0.3f, 0.5f));
                    Part(root, MeshKit.RoundedBox(new Vector3(0.006f, 0.42f, 0.006f), 0.002f), new Vector3(0.017f, -0.2f, 0f), Mats.Surface("wood", Color.white, 0f, 0.4f));
                    tip = new Vector3(0f, -0.05f - fuseLen * 0.2f, 0f);
                    break;
                case "cake":
                    // A square battery of tubes in bright wrapping, the tube mouths dark on top.
                    Part(root, MeshKit.RoundedBox(new Vector3(0.22f, 0.18f, 0.22f), 0.01f), new Vector3(0f, 0.07f, 0f), Mats.Lit(new Color(0.15f, 0.25f, 0.6f), 0f, 0.4f));
                    Part(root, MeshKit.RoundedBox(new Vector3(0.225f, 0.05f, 0.225f), 0.005f), new Vector3(0f, 0.1f, 0f), Mats.Lit(new Color(0.95f, 0.75f, 0.15f), 0f, 0.4f));
                    for (int i = 0; i < 16; i++)
                        Part(root, MeshKit.Lathe("caketube", new[] { new Vector2(0.018f, 0f), new Vector2(0f, 0.001f) }, 10), new Vector3(-0.075f + (i % 4) * 0.05f, 0.161f, -0.075f + (i / 4) * 0.05f), Mats.Lit(new Color(0.05f, 0.04f, 0.04f), 0f, 0.2f));
                    tip = new Vector3(0.11f, 0.03f + fuseLen, 0f);
                    break;
                case "mortar":
                    // A thick fibreglass tube on a wooden base, the shell's fuse hanging out of the top.
                    Part(root, MeshKit.RoundedBox(new Vector3(0.14f, 0.03f, 0.14f), 0.005f), new Vector3(0f, -0.005f, 0f), Mats.Surface("wood", Color.white, 0f, 0.4f));
                    Tube(root, new Vector3(0f, 0.01f, 0f), 0.04f, 0.28f, Mats.Lit(new Color(0.12f, 0.12f, 0.13f), 0.1f, 0.5f), Mats.Lit(new Color(0.05f, 0.05f, 0.05f), 0f, 0.2f));
                    tip = new Vector3(0.02f, 0.29f + fuseLen, 0f);
                    break;
                case "candle":
                    // A long thin tube in stripes.
                    for (int i = 0; i < 6; i++) Tube(root, new Vector3(0f, i * 0.06f, 0f), 0.013f, 0.06f, Mats.Lit(i % 2 == 0 ? new Color(0.8f, 0.15f, 0.12f) : new Color(0.95f, 0.85f, 0.3f), 0f, 0.4f), cap);
                    tip = new Vector3(0f, 0.36f + fuseLen, 0f);
                    break;
                case "fountain":
                    // A paper cone on a plastic foot.
                    Part(root, MeshKit.Lathe("fountain", new[] { new Vector2(0.055f, 0f), new Vector2(0.05f, 0.02f), new Vector2(0.035f, 0.11f), new Vector2(0.012f, 0.14f), new Vector2(0f, 0.141f) }, 16, true, true), Vector3.zero, Mats.Lit(new Color(0.2f, 0.65f, 0.35f), 0f, 0.4f));
                    Part(root, MeshKit.Lathe("fountainband", new[] { new Vector2(0.045f, 0f), new Vector2(0.041f, 0.03f) }, 16), new Vector3(0f, 0.05f, 0f), Mats.Lit(new Color(0.95f, 0.85f, 0.3f), 0f, 0.4f));
                    tip = new Vector3(0f, 0.14f + fuseLen, 0f);
                    break;
                case "whistler":
                case "bigrocket":
                    {
                        // Rockets: the whistler slim and silver, the big one fat and gold.
                        bool big = def.Id == "bigrocket";
                        float r = big ? 0.024f : 0.011f, h = big ? 0.18f : 0.1f;
                        Tube(root, new Vector3(0f, -h * 0.5f, 0f), r, h, Mats.Lit(big ? new Color(0.85f, 0.65f, 0.2f) : new Color(0.75f, 0.77f, 0.8f), 0.6f, 0.6f), cap);
                        Part(root, MeshKit.Lathe("rocketcone" + r, new[] { new Vector2(r * 1.05f, 0f), new Vector2(0f, r * 3.5f) }, 12, false, true), new Vector3(0f, h * 0.5f, 0f), Mats.Lit(big ? new Color(0.75f, 0.1f, 0.08f) : new Color(0.2f, 0.5f, 0.9f), 0.3f, 0.5f));
                        Part(root, MeshKit.RoundedBox(new Vector3(0.006f, big ? 0.6f : 0.4f, 0.006f), 0.002f), new Vector3(r + 0.004f, -(big ? 0.3f : 0.2f), 0f), Mats.Surface("wood", Color.white, 0f, 0.4f));
                        tip = new Vector3(0f, -h * 0.5f - fuseLen * 0.2f, 0f);
                        break;
                    }
                case "spinner":
                    // A flat paper wheel with a tube wound round its rim.
                    Part(root, MeshKit.Lathe("spinner", new[] { new Vector2(0.04f, 0f), new Vector2(0.045f, 0.006f), new Vector2(0.04f, 0.012f), new Vector2(0f, 0.012f) }, 16, true, true), Vector3.zero, Mats.Lit(new Color(0.85f, 0.2f, 0.5f), 0f, 0.4f));
                    tip = new Vector3(0.045f, 0.006f + fuseLen * 0.4f, 0f);
                    break;
                case "ufo":
                    Part(root, MeshKit.Lathe("ufo", new[] { new Vector2(0.06f, 0f), new Vector2(0.065f, 0.006f), new Vector2(0.03f, 0.02f), new Vector2(0.012f, 0.03f), new Vector2(0f, 0.031f) }, 20, true, true), Vector3.zero, Mats.Lit(new Color(0.75f, 0.78f, 0.82f), 0.7f, 0.6f));
                    tip = new Vector3(0.065f, 0.006f + fuseLen * 0.4f, 0f);
                    break;
                case "smoke":
                    Tube(root, Vector3.zero, 0.02f, 0.07f, Mats.Lit(new Color(0.25f, 0.55f, 0.3f), 0f, 0.4f), cap);
                    Part(root, MeshKit.Lathe("smokeband", new[] { new Vector2(0.0205f, 0f), new Vector2(0.0205f, 0.015f) }, 16), new Vector3(0f, 0.03f, 0f), Mats.Lit(new Color(0.95f, 0.85f, 0.2f), 0f, 0.4f));
                    tip = new Vector3(0f, 0.07f + fuseLen, 0f);
                    break;
                case "strobepot":
                    Part(root, MeshKit.Lathe("strobepot", new[] { new Vector2(0.03f, 0f), new Vector2(0.034f, 0.05f), new Vector2(0.03f, 0.055f), new Vector2(0f, 0.056f) }, 16, true, true), Vector3.zero, Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.2f, 0.4f));
                    tip = new Vector3(0f, 0.056f + fuseLen, 0f);
                    break;
                case "saturn":
                    // A row of thin tubes on a long strip of card.
                    Part(root, MeshKit.RoundedBox(new Vector3(0.3f, 0.05f, 0.06f), 0.005f), new Vector3(0f, 0.025f, 0f), Mats.Lit(new Color(0.2f, 0.2f, 0.55f), 0f, 0.4f));
                    for (int i = 0; i < 10; i++)
                        Part(root, MeshKit.Lathe("saturntube", new[] { new Vector2(0.006f, 0f), new Vector2(0f, 0.001f) }, 8), new Vector3(-0.135f + i * 0.03f, 0.051f, 0f), Mats.Lit(new Color(0.05f, 0.04f, 0.04f), 0f, 0.2f));
                    tip = new Vector3(-0.15f, 0.02f + fuseLen, 0f);
                    break;
                case "willowcake":
                case "finale":
                    {
                        bool finale = def.Id == "finale";
                        float side = finale ? 0.3f : 0.22f;
                        Part(root, MeshKit.RoundedBox(new Vector3(side, 0.2f, side), 0.01f), new Vector3(0f, 0.08f, 0f), Mats.Lit(finale ? new Color(0.6f, 0.08f, 0.1f) : new Color(0.85f, 0.65f, 0.2f), 0f, 0.4f));
                        int n = finale ? 6 : 4;
                        for (int i = 0; i < n * n; i++)
                            Part(root, MeshKit.Lathe("caketube", new[] { new Vector2(0.018f, 0f), new Vector2(0f, 0.001f) }, 10), new Vector3(-side * 0.35f + (i % n) * side * 0.7f / (n - 1), 0.181f, -side * 0.35f + (i / n) * side * 0.7f / (n - 1)), Mats.Lit(new Color(0.05f, 0.04f, 0.04f), 0f, 0.2f));
                        tip = new Vector3(side * 0.5f, 0.03f + fuseLen, 0f);
                        break;
                    }
                case "bigmortar":
                    Part(root, MeshKit.RoundedBox(new Vector3(0.22f, 0.04f, 0.22f), 0.006f), new Vector3(0f, -0.005f, 0f), Mats.Surface("wood", Color.white, 0f, 0.4f));
                    Tube(root, new Vector3(0f, 0.015f, 0f), 0.07f, 0.45f, Mats.Lit(new Color(0.2f, 0.25f, 0.2f), 0.1f, 0.5f), Mats.Lit(new Color(0.05f, 0.05f, 0.05f), 0f, 0.2f));
                    tip = new Vector3(0.03f, 0.46f + fuseLen, 0f);
                    break;
                case "megacracker":
                    Tube(root, new Vector3(0f, -0.05f, 0f), 0.017f, 0.1f, Mats.Lit(new Color(0.85f, 0.15f, 0.1f), 0f, 0.4f), cap);
                    Part(root, MeshKit.Lathe("megaband", new[] { new Vector2(0.0175f, -0.012f), new Vector2(0.0175f, 0.012f) }, 16), new Vector3(0f, 0.01f, 0f), Mats.Lit(new Color(0.95f, 0.85f, 0.2f), 0f, 0.4f));
                    tip = new Vector3(0f, 0.05f + fuseLen, 0f);
                    break;
                case "bigbang":
                    Part(root, MeshKit.Blob(0.045f, 0.03f, 903), Vector3.zero, Mats.Lit(new Color(0.08f, 0.08f, 0.09f), 0.2f, 0.6f));
                    Part(root, MeshKit.Lathe("bigbangband", new[] { new Vector2(0.0455f, -0.006f), new Vector2(0.0455f, 0.006f) }, 20), Vector3.zero, Mats.Lit(new Color(0.9f, 0.15f, 0.1f), 0f, 0.4f));
                    tip = new Vector3(0f, 0.045f + fuseLen, 0f);
                    break;
                case "string100":
                    for (int i = 0; i < 14; i++)
                    {
                        var c = Tube(root, new Vector3((i - 6.5f) * 0.011f, -0.009f, 0f), 0.0045f, 0.018f, Mats.Lit(new Color(0.85f, 0.2f, 0.1f), 0f, 0.35f), band);
                        c.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 8f : -8f));
                    }
                    tip = new Vector3(-0.08f, 0.02f, 0f);
                    break;
                case "sticky":
                    Part(root, MeshKit.Blob(0.03f, 0.25f, 904), Vector3.zero, Mats.Lit(new Color(0.35f, 0.75f, 0.2f), 0f, 0.8f));
                    tip = new Vector3(0f, 0.03f + fuseLen, 0f);
                    break;
                case "tnt":
                    for (int i = 0; i < 6; i++) Tube(root, new Vector3((i % 3 - 1) * 0.029f, -0.1f, (i / 3 - 0.5f) * 0.029f), 0.0145f, 0.2f, Mats.Lit(new Color(0.75f, 0.12f, 0.08f), 0f, 0.3f), cap);
                    for (int k = -1; k <= 1; k += 2) Part(root, MeshKit.RoundedBox(new Vector3(0.095f, 0.025f, 0.065f), 0.008f), new Vector3(0f, k * 0.05f, 0f), Mats.Lit(new Color(0.15f, 0.15f, 0.16f), 0f, 0.5f));
                    tip = new Vector3(0f, 0.1f + fuseLen, 0f);
                    break;
                case "cluster":
                    for (int i = 0; i < 7; i++) Part(root, MeshKit.Blob(0.016f, 0.05f, 910 + i), new Vector3(Mathf.Cos(i * 0.9f) * 0.018f * (i > 0 ? 1f : 0f), (i % 3) * 0.01f, Mathf.Sin(i * 0.9f) * 0.018f * (i > 0 ? 1f : 0f)), Mats.Lit(new Color(0.3f, 0.32f, 0.2f), 0.3f, 0.4f));
                    tip = new Vector3(0f, 0.035f + fuseLen, 0f);
                    break;
                case "thermite":
                    Tube(root, new Vector3(0f, -0.04f, 0f), 0.025f, 0.08f, Mats.Lit(new Color(0.55f, 0.56f, 0.58f), 0.8f, 0.5f), Mats.Lit(new Color(0.3f, 0.3f, 0.32f), 0.8f, 0.4f));
                    tip = new Vector3(0f, 0.04f + fuseLen, 0f);
                    break;
                case "timer":
                    // Two sticks taped to a black kitchen-timer box with a green display and a red LED.
                    for (int i = 0; i < 2; i++) Tube(root, new Vector3((i - 0.5f) * 0.03f, -0.09f, -0.012f), 0.0145f, 0.18f, Mats.Lit(new Color(0.62f, 0.12f, 0.08f), 0f, 0.3f), cap);
                    Part(root, MeshKit.RoundedBox(new Vector3(0.07f, 0.05f, 0.025f), 0.006f), new Vector3(0f, 0f, 0.012f), Mats.Lit(new Color(0.06f, 0.06f, 0.07f), 0.2f, 0.5f));
                    Part(root, MeshKit.RoundedBox(new Vector3(0.05f, 0.018f, 0.003f), 0.001f), new Vector3(0f, 0.006f, 0.026f), Mats.Emissive(new Color(0.2f, 0.6f, 0.3f), new Color(0.4f, 2f, 0.6f)));
                    var led = Part(root, MeshKit.RoundedBox(new Vector3(0.008f, 0.008f, 0.004f), 0.003f), new Vector3(0.025f, -0.014f, 0.026f), Mats.Emissive(new Color(0.6f, 0.05f, 0.03f), new Color(3f, 0.2f, 0.1f)));
                    led.name = "Led";
                    for (int k = -1; k <= 1; k += 2) Part(root, MeshKit.RoundedBox(new Vector3(0.075f, 0.012f, 0.04f), 0.003f), new Vector3(0f, k * 0.05f, 0f), Mats.Lit(new Color(0.45f, 0.45f, 0.48f), 0.2f, 0.6f));
                    tip = new Vector3(0f, 0.09f + fuseLen, 0f);
                    break;
                case "remote":
                    // A putty brick with a receiver, a blue LED and a wire antenna.
                    Part(root, MeshKit.RoundedBox(new Vector3(0.09f, 0.04f, 0.06f), 0.008f), Vector3.zero, Mats.Lit(new Color(0.82f, 0.8f, 0.72f), 0f, 0.3f));
                    Part(root, MeshKit.RoundedBox(new Vector3(0.04f, 0.018f, 0.03f), 0.004f), new Vector3(0.012f, 0.028f, 0f), Mats.Lit(new Color(0.1f, 0.12f, 0.1f), 0.3f, 0.5f));
                    var blue = Part(root, MeshKit.RoundedBox(new Vector3(0.007f, 0.007f, 0.007f), 0.003f), new Vector3(0.025f, 0.039f, 0f), Mats.Emissive(new Color(0.1f, 0.2f, 0.6f), new Color(0.3f, 0.8f, 3f)));
                    blue.name = "Led";
                    Part(root, MeshKit.Lathe("antenna", new[] { new Vector2(0.0012f, 0f), new Vector2(0.0012f, 0.07f), new Vector2(0f, 0.071f) }, 6), new Vector3(0.0f, 0.037f, 0f), Mats.Lit(new Color(0.1f, 0.1f, 0.1f), 0.5f, 0.4f));
                    tip = new Vector3(0f, 0.06f + fuseLen, 0f);
                    break;
                default:
                    // Black Cat: a red paper tube with a cream band and clay end caps.
                    Tube(root, new Vector3(0f, -0.035f, 0f), 0.011f, 0.07f, paper, cap);
                    Part(root, MeshKit.Lathe("bcband", new[] { new Vector2(0.0115f, -0.008f), new Vector2(0.0115f, 0.008f) }, 16), new Vector3(0f, 0.012f, 0f), band);
                    tip = new Vector3(0f, 0.035f + fuseLen, 0f);
                    break;
            }
            // The fuse: a thin waxed cord from the body to the tip, slightly bent.
            var fuse = Part(root, MeshKit.Lathe("fuse", new[] { new Vector2(0.0018f, 0f), new Vector2(0.0018f, fuseLen) }, 6), tip - Vector3.up * fuseLen, Mats.Lit(new Color(0.55f, 0.5f, 0.36f), 0f, 0.35f));
            fuse.transform.localRotation = Quaternion.Euler(0f, 0f, 9f);
            fuse.name = "Fuse";
            var tipT = new GameObject("FuseTip").transform;
            tipT.SetParent(root.transform, false);
            tipT.localPosition = tip + new Vector3(-Mathf.Sin(9f * Mathf.Deg2Rad) * fuseLen, 0f, 0f);
            return root;
        }

        /// <summary>A paper or steel tube standing on <paramref name="bottom"/>, with end caps.</summary>
        static GameObject Tube(GameObject root, Vector3 bottom, float r, float h, UnityEngine.Material body, UnityEngine.Material ends)
        {
            var t = Part(root, MeshKit.Lathe("tube" + r + h, new[] { new Vector2(r * 0.92f, 0f), new Vector2(r, r * 0.15f), new Vector2(r, h - r * 0.15f), new Vector2(r * 0.92f, h) }, 16), bottom, body);
            Part(t, MeshKit.Lathe("cap" + r, new[] { new Vector2(r * 0.93f, 0f), new Vector2(r * 0.5f, 0.0015f), new Vector2(0f, 0.002f) }, 16), new Vector3(0f, h, 0f), ends);
            return t;
        }

        static GameObject Part(GameObject root, Mesh mesh, Vector3 pos, UnityEngine.Material mat)
        {
            var go = Props.Part(root.transform, mesh, pos, mat, false);
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        /// <summary>Colour of the item's chip in the pocket bar.</summary>
        public static Color Chip(string id)
        {
            switch (id)
            {
                case "m80": return new Color(0.2f, 0.22f, 0.26f);
                case "sparkler": return new Color(0.45f, 0.45f, 0.5f);
                case "dynamite": return new Color(0.7f, 0.15f, 0.1f);
                case "keg": return Mats.Wood;
                case "pipe": return Mats.Steel;
                case "timer": return new Color(0.12f, 0.35f, 0.18f);
                case "rocket": return new Color(0.75f, 0.1f, 0.08f);
                case "cake": return new Color(0.15f, 0.25f, 0.6f);
                case "mortar": return new Color(0.15f, 0.15f, 0.16f);
                case "candle": return new Color(0.95f, 0.8f, 0.25f);
                case "fountain": return new Color(0.2f, 0.65f, 0.35f);
                case "whistler": return new Color(0.75f, 0.77f, 0.8f);
                case "bigrocket": return new Color(0.85f, 0.65f, 0.2f);
                case "spinner": return new Color(0.85f, 0.2f, 0.5f);
                case "ufo": return new Color(0.7f, 0.74f, 0.8f);
                case "smoke": return new Color(0.25f, 0.55f, 0.3f);
                case "strobepot": return new Color(0.1f, 0.1f, 0.12f);
                case "saturn": return new Color(0.2f, 0.2f, 0.55f);
                case "willowcake": return new Color(0.85f, 0.65f, 0.2f);
                case "finale": return new Color(0.6f, 0.08f, 0.1f);
                case "bigmortar": return new Color(0.2f, 0.25f, 0.2f);
                case "megacracker": return new Color(0.85f, 0.15f, 0.1f);
                case "bigbang": return new Color(0.1f, 0.1f, 0.12f);
                case "string100": return new Color(0.85f, 0.2f, 0.1f);
                case "sticky": return new Color(0.35f, 0.75f, 0.2f);
                case "tnt": return new Color(0.75f, 0.12f, 0.08f);
                case "cluster": return new Color(0.3f, 0.32f, 0.2f);
                case "thermite": return new Color(0.55f, 0.56f, 0.58f);
                case "remote": return new Color(0.18f, 0.3f, 0.6f);
                default: return new Color(0.61f, 0.16f, 0.13f);
            }
        }
    }

    /// <summary>
    /// A lit explosive out of the hand (spec §04, §08): thrown, placed or pushed into something. The fuse keeps
    /// burning; on zero it detonates with the placement it ended up in (airburst if it is still in the air).
    /// </summary>
    public sealed class LiveExplosive : MonoBehaviour
    {
        public static readonly List<LiveExplosive> Active = new List<LiveExplosive>();
        static int nextBlastId = 1;

        public ExplosiveDef Def;
        public float Remaining;
        public Placement Placement;
        public Destructible Container;
        public bool Airborne;
        public int BlastId;
        public float ReleasedWith;   // fuse left when it left the hand, for the Last Second bonus
        /// <summary>Placed items wait unlit until the player lights them (place → light → run).</summary>
        public bool Lit { get; private set; }

        Light glow;
        GameObject marker;
        ParticleSystem sparks;
        bool done;
        float wetCheck;

        /// <summary>An unlit item lying in the world, waiting for the lighter.</summary>
        public static LiveExplosive SpawnUnlit(ExplosiveDef def, Vector3 pos, Quaternion rot, Placement placement)
        {
            var live = Spawn(def, pos, rot, BoomYard.Core.Fuse.TotalBurn(def) > 0f ? BoomYard.Core.Fuse.TotalBurn(def) : 8f, placement, false);
            live.AddMarker();
            return live;
        }

        /// <summary>A soft ring on the ground so placed charges are easy to find again.</summary>
        void AddMarker()
        {
            var live = this;
            live.marker = new GameObject("Marker");
            live.marker.transform.SetParent(live.transform, false);
            var lr = live.marker.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mats.ParticleAdd;
            lr.startColor = lr.endColor = new Color(1f, 0.55f, 0.15f, 0.9f);
            lr.widthMultiplier = 0.012f;
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 24;
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.14f, 0.005f, Mathf.Sin(a) * 0.14f));
            }
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public static LiveExplosive Spawn(ExplosiveDef def, Vector3 pos, Quaternion rot, float remaining, Placement placement, bool lit = true)
        {
            var go = ItemModel.Build(def);
            go.transform.SetPositionAndRotation(pos, rot);
            var live = go.AddComponent<LiveExplosive>();
            live.Def = def;
            live.Remaining = remaining;
            live.ReleasedWith = remaining;
            live.Placement = placement;
            live.BlastId = nextBlastId++;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = def.Mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Collider col;
            if (def.Rolls || def.Id == "sparkler") col = go.AddComponent<SphereCollider>();
            else col = go.AddComponent<CapsuleCollider>();
            if (col is SphereCollider) ((SphereCollider)col).radius = def.Id == "cherry" ? 0.02f : def.Id == "bigbang" ? 0.045f : 0.03f;
            if (col is CapsuleCollider)
            {
                var cap = (CapsuleCollider)col;
                cap.radius = def.Id == "keg" ? 0.19f : def.Id == "cake" ? 0.12f : def.Id == "mortar" || def.Id == "fountain" ? 0.05f : 0.02f;
                cap.height = def.Id == "keg" ? 0.5f : def.Id == "cake" ? 0.24f : def.Id == "candle" ? 0.4f : def.Id == "mortar" ? 0.3f : 0.08f;
                if (def.Id == "cake" || def.Id == "mortar" || def.Id == "candle" || def.Id == "fountain") cap.center = new Vector3(0f, cap.height * 0.5f - 0.02f, 0f);
            }
            go.layer = Layers.Default;
            if (def.Firework != null) rb.constraints = RigidbodyConstraints.FreezeRotation;   // a set-down firework stays upright

            Active.Add(live);
            if (lit) live.Light();
            return live;
        }

        /// <summary>The fuse catches: sparks, a glow, the hiss, and the countdown starts.</summary>
        public void Light()
        {
            if (Lit || done) return;
            Lit = true;
            if (marker != null) Destroy(marker);
            if (Def.Timer) { gameObject.AddComponent<Beeper>().Live = this; return; }
            var tip = transform.Find("FuseTip");
            if (tip != null && Def.Fuse > 0f)
            {
                sparks = Fx.FuseSparks(tip);
                glow = tip.gameObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.7f, 0.35f);
                glow.range = 1.5f;
                glow.intensity = 0.8f;
                tip.gameObject.AddComponent<Flicker>();
                var hiss = tip.gameObject.AddComponent<AudioSource>();
                Sfx.I.FuseOn(hiss);
            }
        }

        public Vector3 FuseTipWorld
        {
            get { var tip = transform.Find("FuseTip"); return tip != null ? tip.position : transform.position; }
        }

        /// <summary>Unlit charges in the world, for the "light it" prompt and the tutorial.</summary>
        public static int UnlitCount
        {
            get { int n = 0; foreach (var l in Active) if (l != null && !l.Lit) n++; return n; }
        }

        public static int LitCount
        {
            get { int n = 0; foreach (var l in Active) if (l != null && l.Lit) n++; return n; }
        }

        /// <summary>Remote charges waiting for the button.</summary>
        public static int RemoteCount
        {
            get { int n = 0; foreach (var l in Active) if (l != null && l.Def.Remote && !l.done) n++; return n; }
        }

        void OnDestroy() { Active.Remove(this); }

        void Update()
        {
            if (StuckTo != null && StuckTo.Destroyed && StuckTo.IsGlass) Unstick();
            if (done || !Lit) return;
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f) { Detonate(); return; }
            // In the rain a fuse lying in the open can fizzle out (about one in six over a 3 s fuse).
            var gc = GameController.I;
            if (gc != null && gc.Raining && Def.Fuse > 0f && !Def.Timer && Remaining > 0.6f)
            {
                wetCheck += Time.deltaTime;
                if (wetCheck >= 0.5f) { wetCheck = 0f; if (Random.value < 0.03f) Fizzle(); }
            }
        }

        /// <summary>The object it was set down on (it rides with it), or null.</summary>
        public Destructible StuckTo;

        /// <summary>
        /// Holds it exactly where it was set down: resting on the surface (its underside on it, upright on the
        /// ground, flat against a wall), not moved by physics, and riding along with the object it is on.
        /// </summary>
        public void StickTo(Vector3 point, Vector3 normal, Destructible on)
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            // Fireworks stand upright whatever they are on; the rest lie the way the surface faces.
            if (Def.Firework == null && normal.y < 0.95f) transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * transform.rotation;
            transform.position = point;
            // Lift it until its bottom just touches the surface.
            var col = GetComponent<Collider>();
            if (col != null)
            {
                Physics.SyncTransforms();
                var b = col.bounds;
                var up = Def.Firework != null ? Vector3.up : normal;
                float below = Vector3.Dot(point - b.center, up) + Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z))));
                transform.position += up * Mathf.Max(0f, below + 0.002f);
            }
            if (on != null && !on.Destroyed)
            {
                StuckTo = on;
                transform.SetParent(on.transform, true);
            }
        }

        /// <summary>The thing it was on broke away under it (a pane of glass): it falls.</summary>
        void Unstick()
        {
            StuckTo = null;
            transform.SetParent(null, true);
            var rb = GetComponent<Rigidbody>();
            if (rb != null && Motor == null) rb.isKinematic = false;
        }

        /// <summary>The fuse has burnt down: its sparks, glow and hiss stop.</summary>
        void StopFuse()
        {
            if (sparks != null) Destroy(sparks.gameObject);
            if (glow != null) Destroy(glow);
            var tip = transform.Find("FuseTip");
            if (tip == null) return;
            var hiss = tip.GetComponent<AudioSource>();
            if (hiss != null) hiss.Stop();
        }

        /// <summary>The fuse goes out: it lies there unlit again, with the time it had left.</summary>
        void Fizzle()
        {
            Lit = false;
            if (sparks != null) Destroy(sparks.gameObject);
            if (glow != null) Destroy(glow);
            var tip = transform.Find("FuseTip");
            if (tip != null)
            {
                var f = tip.GetComponent<Flicker>();
                if (f != null) Destroy(f);
                var a = tip.GetComponent<AudioSource>();
                if (a != null) Destroy(a);
            }
            AddMarker();
            GameController.I.Toast("toast.fizzle");
        }

        void OnCollisionEnter(Collision c)
        {
            if (done) return;
            // A sticky bomb grabs whatever it hits first.
            if (Def.Sticky && Airborne && StuckTo == null && Motor == null && c.contactCount > 0)
            {
                var contact = c.GetContact(0);
                StickTo(contact.point, contact.normal, c.collider.GetComponentInParent<Destructible>());
                Airborne = false;
                return;
            }
            // Thrown straight into a bin, a mailbox or a box: contained, and a hole in one.
            if (Airborne && Lit && Placement == Placement.Thrown && Container == null)
            {
                var d = c.collider.GetComponentInParent<Destructible>();
                if (d != null && !d.Destroyed && (d.Def.Reaction & Reaction.Contain) != 0 && d.Def.Mass < 100f)
                {
                    Container = d;
                    Placement = Placement.Contained;
                    if (GameController.I != null) GameController.I.Bonus(BonusKind.HoleInOne);
                }
            }
            Airborne = false;
            if (Def.Fuse <= 0f && Lit) Detonate();   // molotov: breaks on contact
            if (Motor != null && Motor.Missile && Motor.Armed) Detonate();   // a thrown rocket bursts on impact
        }

        /// <summary>Set when a lit rocket was thrown: it flies under power and bursts on impact.</summary>
        public RocketMotor Motor;

        public void Detonate()
        {
            if (done) return;
            // A rocket whose fuse burns down on the ground goes up as a firework instead.
            if (Def.Firework != null && Def.Firework != "rocket" && (!Airborne || Def.Firework == "smoke"))
            {
                // A firework that fires from the ground: it stays and fires, then it is gone.
                done = true;
                StopFuse();
                FireworkDevice.Begin(this);
                return;
            }
            if (Def.Firework == "rocket" && Motor == null && !Airborne)
            {
                Motor = gameObject.AddComponent<RocketMotor>();
                Motor.Launch(this, RocketMotor.Kind.Shell, (transform.up + Random.insideUnitSphere * 0.12f).normalized);
                Remaining = 99f;   // the motor decides when it bursts
                return;
            }
            done = true;
            var pos = transform.position;
            var placement = Placement;
            if (placement == Placement.Thrown && Airborne && pos.y > 0.35f) placement = Placement.Airburst;
            if (GameController.I != null) GameController.I.OnPlayerDetonation(this, placement);
            if (Def.Units > 1) GameController.I.StartCoroutine(Sequence(Def, pos, placement, Container, BlastId));
            else Charge(Def, pos, placement, Container, BlastId);
            if (Def.BurnSeconds > 0f) FireGrid.I.AddGround(pos, Def.Radius, Def.BurnSeconds);
            Destroy(gameObject);
        }

        static void Charge(ExplosiveDef def, Vector3 pos, Placement placement, Destructible container, int blastId)
        {
            Blast.Detonate(new Blast.Charge
            {
                Position = pos, Damage = def.Damage, Radius = def.Radius, Shrapnel = def.ShrapnelRadius, Placement = placement,
                BreaksConcrete = def.BreaksConcrete, Thermite = def.Id == "thermite", BlastId = blastId, Container = container,
            });
        }

        /// <summary>Strings, cakes and clusters: one small charge after another, scattered along the line or around.</summary>
        static IEnumerator Sequence(ExplosiveDef def, Vector3 pos, Placement placement, Destructible container, int blastId)
        {
            var dir = Random.insideUnitCircle.normalized;
            for (int i = 0; i < def.Units; i++)
            {
                Vector3 p = pos;
                if (def.Id.StartsWith("string")) p += new Vector3(dir.x, 0f, dir.y) * (i * 0.04f) + Random.insideUnitSphere * 0.1f;
                else p += Random.insideUnitSphere * Mathf.Min(def.Radius, 2f);
                p.y = Mathf.Max(0.05f, p.y);
                Charge(def, p, placement, container, blastId);
                yield return new WaitForSeconds(def.UnitInterval);
            }
        }
    }

    /// <summary>A timer charge counting down: the LED blinks and it beeps once a second, faster for the last three.</summary>
    public sealed class Beeper : MonoBehaviour
    {
        public LiveExplosive Live;
        static AudioClip beep;
        AudioSource source;
        Renderer led;
        float next;

        void Start()
        {
            if (beep == null)
            {
                const int rate = 44100;
                var d = new float[rate / 14];
                for (int i = 0; i < d.Length; i++)
                {
                    float t = i / (float)rate;
                    d[i] = Mathf.Sin(2f * Mathf.PI * 2900f * t) * Mathf.Clamp01(t * 400f) * Mathf.Clamp01((d.Length - i) / 300f) * 0.5f;
                }
                beep = AudioClip.Create("beep", d.Length, 1, rate, false);
                beep.SetData(d, 0);
            }
            source = gameObject.AddComponent<AudioSource>();
            source.clip = beep;
            source.spatialBlend = 1f;
            source.minDistance = 1.5f;
            source.maxDistance = 30f;
            source.rolloffMode = AudioRolloffMode.Linear;
            var t2 = transform.Find("Led");
            led = t2 != null ? t2.GetComponent<Renderer>() : null;
            next = 0f;
        }

        float interval = 1f;

        void Update()
        {
            if (Live == null) return;
            next -= Time.deltaTime;
            if (led != null) led.enabled = next > interval - 0.12f;   // a short flash with each beep
            if (next > 0f) return;
            interval = Live.Remaining < 3f ? 0.33f : 1f;
            next = interval;
            source.volume = 0.6f * (Sfx.I != null ? Sfx.I.Volume : 1f);
            source.Play();
        }
    }

    /// <summary>
    /// A rocket under power. Thrown lit, it is a missile: it flies along the aim and bursts on impact. Lit standing
    /// on the ground, it is a firework: it climbs for about a second, then bursts into coloured stars over the street.
    /// </summary>
    public sealed class RocketMotor : MonoBehaviour
    {
        public enum Kind { Missile, Shell }

        public bool Missile { get { return kind == Kind.Missile; } }
        /// <summary>A missile only bursts on impact once it is clear of the hand.</summary>
        public bool Armed { get { return age > 0.12f; } }

        LiveExplosive live;
        Kind kind;
        Vector3 dir;
        Rigidbody rb;
        float age;


        public void Launch(LiveExplosive l, Kind k, Vector3 direction)
        {
            live = l;
            kind = k;
            dir = direction;
            transform.SetParent(null, true);   // off whatever it was standing on
            l.StuckTo = null;
            rb = GetComponent<Rigidbody>();
            rb.useGravity = k == Kind.Missile;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            var tip = transform.Find("FuseTip");
            Fx.FuseSparks(tip != null ? tip : transform);
            var hiss = gameObject.AddComponent<AudioSource>();
            hiss.clip = StreetSounds.Whistle;
            hiss.pitch = l.Def.Id == "whistler" ? Random.Range(1.7f, 1.9f) : l.Def.Id == "bigrocket" ? Random.Range(0.85f, 0.95f) : Random.Range(1.1f, 1.35f);
            hiss.spatialBlend = 1f;
            hiss.minDistance = 3f;
            hiss.maxDistance = 90f;
            hiss.volume = 0.6f * Sfx.I.Volume;
            hiss.Play();
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (kind == Kind.Missile)
            {
                // Thrust along the aim for a second, so it flies flat and far instead of arcing like a throw.
                if (age < 1f) rb.AddForce(dir * 16f, ForceMode.Acceleration);
                if (age > 3f) live.Detonate();
                return;
            }
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = dir * Mathf.Lerp(8f, 20f, Mathf.Clamp01(age * 2f));
#else
            rb.velocity = dir * Mathf.Lerp(8f, 20f, Mathf.Clamp01(age * 2f));
#endif
            if (age < 1.05f) return;   // about 15 m up: high over the house, still in view from the yard
            // The whistler bursts in strobes and crackles; the big rocket's shell is half as big again.
            var def = live != null ? live.Def : null;
            var type = def != null && def.Id == "whistler" ? (Random.value < 0.5f ? ShellType.Strobe : ShellType.Crackling) : Fireworks.RandomType();
            Fireworks.Burst(transform.position, type, def != null && def.Id == "bigrocket" ? 1.5f : 1f);
            Destroy(gameObject);
        }
    }
}
