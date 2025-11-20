using UnityEngine;


namespace AES.Tools.Util
{
    public static class Easing
    {
        public static float EaseInQuad(float t) => t * t;
        public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float EaseOutBack(float t, float s = 1.70158f) {
            float c1 = s; float c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float EaseOutElastic(float t) {
            if (t == 0f) return 0f; if (t == 1f) return 1f; float c4 = (2f * Mathf.PI) / 3f; return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
        public static float Spring(float t, float damping = 0.5f, float freq = 12f) {
            return 1f - Mathf.Exp(-damping * t * 10f) * Mathf.Cos(freq * t);
        }
    }
}