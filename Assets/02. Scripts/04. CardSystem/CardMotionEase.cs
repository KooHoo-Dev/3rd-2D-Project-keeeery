using UnityEngine;

namespace Arcanist
{
    internal static class CardMotionEase
    {
        public static float InCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value;
        }

        public static float OutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return 1.0f - Mathf.Pow(1.0f - value, 3.0f);
        }

        public static float FlipScale(float yRotation, float minimumScale)
        {
            return Mathf.Lerp(
                minimumScale,
                1.0f,
                Mathf.Abs(Mathf.Cos(yRotation * Mathf.Deg2Rad)));
        }
    }
}
