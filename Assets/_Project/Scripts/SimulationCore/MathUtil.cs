using System;

namespace Sovereign.Core
{
    /// <summary>
    /// SimulationCore cannot see UnityEngine, so it cannot use Mathf. These are the
    /// handful of helpers the model actually needs, on System.Math.
    /// </summary>
    public static class MathUtil
    {
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        public static float Clamp01(float value) { return Clamp(value, 0f, 1f); }

        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }

        /// <summary>Moves a value a fraction of the way toward a target. The model's
        /// workhorse: almost every lagged variable is a partial adjustment like this.</summary>
        public static float Approach(float current, float target, float fraction)
        {
            return current + (target - current) * Clamp01(fraction);
        }

        public static float Abs(float value) { return value < 0f ? -value : value; }

        public static float Max(float a, float b) { return a > b ? a : b; }

        public static float Min(float a, float b) { return a < b ? a : b; }

        public static float Exp(float value) { return (float)Math.Exp(value); }

        public static float Log(float value) { return (float)Math.Log(value < 1e-6f ? 1e-6f : value); }

        public static float Pow(float value, float power) { return (float)Math.Pow(value, power); }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// An AnimationCurve sampled into plain floats at boot. This is how a curve
    /// authored in the Inspector crosses the boundary into a Unity-free assembly:
    /// the shape is preserved, the UnityEngine type is not.
    /// </summary>
    public struct SampledCurve
    {
        public float[] samples;
        public float minX;
        public float maxX;

        public float Evaluate(float x)
        {
            if (samples == null || samples.Length == 0) return 0f;
            if (samples.Length == 1) return samples[0];

            float span = maxX - minX;
            if (span <= 0f) return samples[0];

            float t = MathUtil.Clamp01((x - minX) / span) * (samples.Length - 1);
            int index = (int)t;
            if (index >= samples.Length - 1) return samples[samples.Length - 1];

            return MathUtil.Lerp(samples[index], samples[index + 1], t - index);
        }
    }
}
