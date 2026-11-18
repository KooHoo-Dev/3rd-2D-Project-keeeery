using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    internal sealed class RewardSelectionMotion
    {
        private const float FadeDuration = 0.2f;

        private readonly RectTransform container;
        private readonly CanvasGroup selectTextCanvasGroup;
        private readonly float centerScale;
        private readonly float moveDuration;

        public RewardSelectionMotion(
            RectTransform container,
            CanvasGroup selectTextCanvasGroup,
            float centerScale,
            float moveDuration)
        {
            this.container = container;
            this.selectTextCanvasGroup = selectTextCanvasGroup;
            this.centerScale = centerScale;
            this.moveDuration = moveDuration;
        }

        public IEnumerator FadeUnselected(
            IReadOnlyList<RewardCardView> views,
            RewardCardView selectedView)
        {
            List<CanvasGroup> targets = new();
            List<float> startAlphas = new();

            foreach (RewardCardView view in views)
            {
                if (view == null || view == selectedView || view.CanvasGroup == null)
                    continue;

                targets.Add(view.CanvasGroup);
                startAlphas.Add(view.CanvasGroup.alpha);
            }

            if (selectTextCanvasGroup != null)
            {
                targets.Add(selectTextCanvasGroup);
                startAlphas.Add(selectTextCanvasGroup.alpha);
            }

            float elapsed = 0.0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = SmoothStep(Mathf.Clamp01(elapsed / FadeDuration));

                for (int i = 0; i < targets.Count; i++)
                    targets[i].alpha = Mathf.Lerp(startAlphas[i], 0.0f, time);

                yield return null;
            }

            foreach (CanvasGroup target in targets)
                target.alpha = 0.0f;
        }

        public IEnumerator MoveToCenter(RectTransform selectedRect)
        {
            if (selectedRect == null || container == null)
                yield break;

            Vector3 startPosition = selectedRect.position;
            Vector3 targetPosition = container.position;
            Vector3 startScale = selectedRect.localScale;
            Vector3 targetScale = new(centerScale, centerScale, 1.0f);
            float duration = Mathf.Max(0.01f, moveDuration);
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                selectedRect.position = Vector3.LerpUnclamped(startPosition, targetPosition, time);
                selectedRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, time);
                yield return null;
            }

            selectedRect.position = targetPosition;
            selectedRect.localScale = targetScale;
        }

        public Vector3 GetCenterScale()
        {
            return new Vector3(centerScale, centerScale, 1.0f);
        }

        private static float EaseOutCubic(float value)
        {
            return 1.0f - Mathf.Pow(1.0f - value, 3.0f);
        }

        private static float SmoothStep(float value)
        {
            return value * value * (3.0f - 2.0f * value);
        }
    }
}
