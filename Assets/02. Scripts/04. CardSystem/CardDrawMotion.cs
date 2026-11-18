using System.Collections;
using UnityEngine;

namespace Arcanist
{
    internal readonly struct CardDrawMotionSettings
    {
        public readonly float RiseDistance;
        public readonly float Duration;
        public readonly float StartYRotation;
        public readonly float ExtraYSpin;
        public readonly float StartZRotation;
        public readonly float OvershootY;
        public readonly float MinFlipScaleX;

        public CardDrawMotionSettings(
            float riseDistance,
            float duration,
            float startYRotation,
            float extraYSpin,
            float startZRotation,
            float overshootY,
            float minFlipScaleX)
        {
            RiseDistance = riseDistance;
            Duration = duration;
            StartYRotation = startYRotation;
            ExtraYSpin = extraYSpin;
            StartZRotation = startZRotation;
            OvershootY = overshootY;
            MinFlipScaleX = minFlipScaleX;
        }
    }

    internal sealed class CardDrawMotion
    {
        private readonly CardDrawMotionSettings settings;

        public CardDrawMotion(CardDrawMotionSettings settings)
        {
            this.settings = settings;
        }

        public IEnumerator Play(RectTransform card)
        {
            Vector2 endPosition = Vector2.zero;
            Vector2 startPosition = endPosition + Vector2.down * settings.RiseDistance;
            Vector2 overshootPosition = endPosition + Vector2.up * settings.OvershootY;

            if (card == null)
                yield break;

            card.anchoredPosition = startPosition;
            card.localScale = Vector3.one * 0.8f;
            card.localRotation = Quaternion.Euler(
                0.0f,
                settings.StartYRotation + settings.ExtraYSpin,
                settings.StartZRotation);

            float elapsed = 0.0f;

            while (elapsed < settings.Duration)
            {
                if (card == null)
                    yield break;

                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / settings.Duration);
                float eased = CardMotionEase.OutCubic(normalized);

                if (normalized < 0.82f)
                {
                    float move = CardMotionEase.OutCubic(normalized / 0.82f);
                    card.anchoredPosition = Vector2.LerpUnclamped(startPosition, overshootPosition, move);
                }
                else
                {
                    float settle = CardMotionEase.OutCubic((normalized - 0.82f) / 0.18f);
                    card.anchoredPosition = Vector2.LerpUnclamped(overshootPosition, endPosition, settle);
                }

                float yRotation = Mathf.Lerp(
                    settings.StartYRotation + settings.ExtraYSpin,
                    0.0f,
                    eased);
                float zRotation = Mathf.Lerp(settings.StartZRotation, 0.0f, eased);
                float scale = Mathf.Lerp(0.8f, 1.0f, eased);
                float flipScaleX = CardMotionEase.FlipScale(yRotation, settings.MinFlipScaleX);
                card.localRotation = Quaternion.Euler(0.0f, yRotation, zRotation);
                card.localScale = new Vector3(scale * flipScaleX, scale, scale);
                yield return null;
            }

            if (card != null)
            {
                card.anchoredPosition3D = Vector3.zero;
                card.localScale = Vector3.one;
                card.localRotation = Quaternion.identity;
            }
        }
    }
}
