using System.Collections;
using UnityEngine;

namespace Arcanist
{
    internal readonly struct CardUseMotionSettings
    {
        public readonly float RiseDistance;
        public readonly float Duration;
        public readonly float ScaleUp;
        public readonly float EndScale;
        public readonly float YSpin;
        public readonly float ZRotation;
        public readonly float FadeStart;
        public readonly float FxSpawnTime;
        public readonly float MinFlipScaleX;

        public CardUseMotionSettings(
            float riseDistance,
            float duration,
            float scaleUp,
            float endScale,
            float ySpin,
            float zRotation,
            float fadeStart,
            float fxSpawnTime,
            float minFlipScaleX)
        {
            RiseDistance = riseDistance;
            Duration = duration;
            ScaleUp = scaleUp;
            EndScale = endScale;
            YSpin = ySpin;
            ZRotation = zRotation;
            FadeStart = fadeStart;
            FxSpawnTime = fxSpawnTime;
            MinFlipScaleX = minFlipScaleX;
        }
    }

    internal sealed class CardUseMotion
    {
        private readonly CardUseMotionSettings settings;
        private readonly CardUseFxMotion fxMotion;

        public CardUseMotion(CardUseMotionSettings settings, CardUseFxMotion fxMotion)
        {
            this.settings = settings;
            this.fxMotion = fxMotion;
        }

        public IEnumerator Play(RectTransform card)
        {
            if (card == null)
                yield break;

            Vector2 startPosition = card.anchoredPosition;
            Vector2 endPosition = startPosition + Vector2.up * settings.RiseDistance;
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(card);
            canvasGroup.alpha = 1.0f;
            bool fxSpawned = false;
            float elapsed = 0.0f;

            while (elapsed < settings.Duration)
            {
                if (card == null)
                    yield break;

                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / settings.Duration);
                float eased = CardMotionEase.OutCubic(normalized);
                card.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);

                float scale = normalized < 0.35f
                    ? Mathf.Lerp(1.0f, settings.ScaleUp, normalized / 0.35f)
                    : Mathf.Lerp(settings.ScaleUp, settings.EndScale, (normalized - 0.35f) / 0.65f);
                float yRotation = Mathf.Lerp(0.0f, settings.YSpin, eased);
                float zRotation = Mathf.Lerp(0.0f, settings.ZRotation, eased);
                float flipScaleX = CardMotionEase.FlipScale(yRotation, settings.MinFlipScaleX);

                card.localScale = new Vector3(scale * flipScaleX, scale, scale);
                card.localRotation = Quaternion.Euler(0.0f, yRotation, zRotation);

                float fade = Mathf.InverseLerp(settings.FadeStart, 1.0f, normalized);
                canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, CardMotionEase.OutCubic(fade));

                if (fxSpawned == false && normalized >= settings.FxSpawnTime)
                {
                    fxMotion.Spawn(card);
                    fxSpawned = true;
                }

                yield return null;
            }

            if (card == null)
                yield break;

            if (fxSpawned == false)
                fxMotion.Spawn(card);

            Object.Destroy(card.gameObject);
        }

        private static CanvasGroup GetOrAddCanvasGroup(RectTransform card)
        {
            CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
            return canvasGroup != null ? canvasGroup : card.gameObject.AddComponent<CanvasGroup>();
        }
    }
}
