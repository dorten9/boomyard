using System;
using System.Collections.Generic;

namespace BoomYard.Core
{
    public struct Vec3
    {
        public float X, Y, Z;
        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    public enum HandPartKind { Bone, Blob }

    /// <summary>
    /// One piece of a hand. A bone is a capsule from A to B with a diameter; a blob is an ellipsoid at A with the
    /// given size, turned by Yaw degrees about the grip axis.
    /// </summary>
    public struct HandPart
    {
        public HandPartKind Kind;
        public string Name;
        public Vec3 A, B, Size;
        public float Diameter, Yaw;
    }

    /// <summary>
    /// The first-person hand as a fist closed around an upright object (a firecracker, a lighter), in the grip
    /// frame: Y runs up the held object, Z points away from the camera, X to the hand's outer side. Four fingers
    /// with three bones each wrap around the object from knuckles on the outer back, the thumb crosses over the
    /// index finger, and the back of the hand and the thumb's ball are rounded blobs. Pure data, so the Unity rig
    /// and the preview tool build the same hand.
    /// </summary>
    public static class HandRig
    {
        /// <summary>Distance from the grip axis to the finger centres (a 2 cm thick object plus half a finger).</summary>
        public const float Radius = 0.019f;

        /// <summary>
        /// The whole grip turned about the object so the back of the hand faces outwards and the camera, behind,
        /// sees the thumb, the heel of the palm and the curled fingertips (as you see your own fist).
        /// </summary>
        public const float Turn = 35f;

        // Index, middle, ring, little: height of the finger row, length scale, thickness.
        static readonly float[] RowY = { 0.009f, -0.0055f, -0.0195f, -0.0325f };
        static readonly float[] Length = { 1f, 1.07f, 0.98f, 0.8f };
        static readonly float[] Thick = { 0.0165f, 0.017f, 0.016f, 0.0138f };

        /// <summary>The fist of the right hand (side 1) or the left hand (side -1).</summary>
        public static List<HandPart> Fist(float side)
        {
            var parts = new List<HandPart>();
            for (int k = 0; k < 4; k++)
            {
                float len = Length[k], d = Thick[k], y = RowY[k];
                // Joint angles around the object: knuckle on the outer back, then wrapping over the front.
                float a0 = -38f, a1 = a0 + 78f * len, a2 = a1 + 68f * len, a3 = a2 + 52f * len;
                var knuckle = At(side, a0, y + 0.002f, Radius + 0.007f);
                var j1 = At(side, a1, y, Radius);
                var j2 = At(side, a2, y - 0.001f, Radius - 0.001f);
                var tip = At(side, a3, y - 0.002f, Radius - 0.002f);
                parts.Add(Bone("Finger" + k + "a", knuckle, j1, d));
                parts.Add(Bone("Finger" + k + "b", j1, j2, d * 0.93f));
                parts.Add(Bone("Finger" + k + "c", j2, tip, d * 0.86f));
                parts.Add(Blob("Knuckle" + k, knuckle, new Vec3(d * 1.2f, d * 1.1f, d * 1.25f), -(a0 + Turn) * side));
            }
            // Back of the hand behind the knuckles, and the palm closing the fist on the camera side, where the
            // fingertips press in.
            parts.Add(Blob("Back", At(side, -58f, -0.014f, Radius + 0.008f), new Vec3(0.03f, 0.062f, 0.05f), (58f - Turn) * side));
            parts.Add(Blob("Palm", At(side, 248f, -0.014f, Radius + 0.006f), new Vec3(0.028f, 0.064f, 0.052f), -(248f + Turn) * side));
            // Ball of the thumb on the inner side, then the thumb crossing over the index finger.
            parts.Add(Blob("ThumbBall", At(side, 222f, -0.022f, Radius + 0.01f), new Vec3(0.032f, 0.05f, 0.034f), -(222f + Turn) * side));
            var t0 = At(side, 214f, -0.006f, Radius + 0.011f);
            var t1 = At(side, 178f, 0.014f, Radius + 0.006f);
            var t2 = At(side, 142f, 0.021f, Radius + 0.004f);
            parts.Add(Bone("Thumb0", t0, t1, 0.0205f));
            parts.Add(Bone("Thumb1", t1, t2, 0.0185f));
            return parts;
        }

        /// <summary>Where the wrist leaves the fist, in the grip frame (below the back of the hand).</summary>
        public static Vec3 WristTop(float side) { return At(side, -80f, -0.05f, Radius + 0.012f); }

        /// <summary>A point at an angle around the grip axis (0° = outer side, 90° = away from the camera).</summary>
        public static Vec3 At(float side, float degrees, float y, float radius)
        {
            double a = (degrees + Turn) * Math.PI / 180.0;
            return new Vec3((float)(side * radius * Math.Cos(a)), y, (float)(radius * Math.Sin(a)));
        }

        static HandPart Bone(string name, Vec3 a, Vec3 b, float d)
        {
            return new HandPart { Kind = HandPartKind.Bone, Name = name, A = a, B = b, Diameter = d };
        }

        static HandPart Blob(string name, Vec3 c, Vec3 size, float yaw)
        {
            return new HandPart { Kind = HandPartKind.Blob, Name = name, A = c, Size = size, Yaw = yaw };
        }
    }
}
