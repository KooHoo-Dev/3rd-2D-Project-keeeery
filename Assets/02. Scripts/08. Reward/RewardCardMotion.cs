using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class RewardCardMotion : MonoBehaviour
    {
        [Header("Base")]
        [SerializeField] private float baseScale = 1.35f;

        [Header("Draw Motion")]
        [SerializeField] private float cardRiseDistance = 750.0f;
        [SerializeField] private float drawDuration = 0.55f;
        [SerializeField] private float startYRotation = 85.0f;
        [SerializeField] private float extraYSpin = 540.0f;
        [SerializeField] private float startZRotation;
        [SerializeField] private float overshootY;
        [SerializeField] private float minFlipScaleX = 0.05f;

        [Header("Exit Motion")]
        [SerializeField] private float exitDuration = 1.0f;
        [SerializeField] private float exitDistance = 750.0f;
        [SerializeField] private float exitEndScale = 0.75f;
        [SerializeField] private float exitYRotation = 540.0f;
        [SerializeField] private float exitZRotation = -8.0f;
        [SerializeField] private float exitMinFlipScaleX = 0.08f;
        [SerializeField, Range(0.0f, 1.0f)] private float exitFadeStart = 0.65f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void PrepareDraw(Vector2 endPosition)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchoredPosition = endPosition + Vector2.down * cardRiseDistance;
            rectTransform.localScale = Vector3.one * (baseScale * 0.8f);
            rectTransform.localRotation = Quaternion.Euler(0.0f, startYRotation + extraYSpin, startZRotation);

            canvasGroup.alpha = 1.0f;
        }

        public IEnumerator PlayDraw(Vector2 endPosition)
        {
            if (rectTransform == null)
                yield break;

            Vector2 startPosition = endPosition + Vector2.down * cardRiseDistance;
            Vector2 overshootPosition = endPosition + Vector2.up * overshootY;
            float duration = Mathf.Max(0.01f, drawDuration);
            float elapsed = 0.0f;

            PrepareDraw(endPosition);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(normalizedTime);

                rectTransform.anchoredPosition = GetDrawPosition(startPosition, overshootPosition, endPosition, normalizedTime);
                ApplyDrawRotationAndScale(eased);

                yield return null;
            }

            rectTransform.anchoredPosition = endPosition;
            rectTransform.localScale = new Vector3(baseScale, baseScale, 1.0f);
            rectTransform.localRotation = Quaternion.identity;
        }

        public IEnumerator PlayExit(Vector3 startScale)
        {
            if (rectTransform == null)
                yield break;

            canvasGroup.alpha = 1.0f;
            rectTransform.SetAsLastSibling();

            Vector2 startPosition = rectTransform.anchoredPosition;
            Vector2 endPosition = startPosition + Vector2.down * exitDistance;
            Vector3 endScale = new Vector3(exitEndScale, exitEndScale, 1.0f);
            float duration = Mathf.Max(0.01f, exitDuration);
            float fadeStart = Mathf.Clamp01(exitFadeStart);
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInOutCubic(normalizedTime);

                rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
                ApplyExitRotationAndScale(startScale, endScale, eased);
                ApplyExitFade(normalizedTime, fadeStart);

                yield return null;
            }

            rectTransform.anchoredPosition = endPosition;
            rectTransform.localRotation = Quaternion.Euler(0.0f, exitYRotation, exitZRotation);
            rectTransform.localScale = new Vector3(endScale.x * exitMinFlipScaleX, endScale.y, 1.0f);
            canvasGroup.alpha = 0.0f;
        }

        private Vector2 GetDrawPosition(Vector2 startPosition, Vector2 overshootPosition, Vector2 endPosition, float normalizedTime)
        {
            if (normalizedTime < 0.82f)
            {
                float moveTime = EaseOutCubic(normalizedTime / 0.82f);
                return Vector2.LerpUnclamped(startPosition, overshootPosition, moveTime);
            }

            float settleTime = EaseOutCubic((normalizedTime - 0.82f) / 0.18f);
            return Vector2.LerpUnclamped(overshootPosition, endPosition, settleTime);
        }

        private void ApplyDrawRotationAndScale(float eased)
        {
            float yRotation = Mathf.Lerp(startYRotation + extraYSpin, 0.0f, eased);
            float zRotation = Mathf.Lerp(startZRotation, 0.0f, eased);
            float scale = Mathf.Lerp(0.8f, 1.0f, eased) * baseScale;
            float flipScaleX = Mathf.Lerp(minFlipScaleX, 1.0f, Mathf.Abs(Mathf.Cos(yRotation * Mathf.Deg2Rad)));

            rectTransform.localRotation = Quaternion.Euler(0.0f, yRotation, zRotation);
            rectTransform.localScale = new Vector3(scale * flipScaleX, scale, 1.0f);
        }

        private void ApplyExitRotationAndScale(Vector3 startScale, Vector3 endScale, float eased)
        {
            float yRotation = Mathf.Lerp(0.0f, exitYRotation, eased);
            float zRotation = Mathf.Lerp(0.0f, exitZRotation, eased);
            Vector3 scale = Vector3.LerpUnclamped(startScale, endScale, eased);
            float flipScaleX = Mathf.Lerp(exitMinFlipScaleX, 1.0f, Mathf.Abs(Mathf.Cos(yRotation * Mathf.Deg2Rad)));

            rectTransform.localRotation = Quaternion.Euler(0.0f, yRotation, zRotation);
            rectTransform.localScale = new Vector3(scale.x * flipScaleX, scale.y, 1.0f);
        }

        private void ApplyExitFade(float normalizedTime, float fadeStart)
        {
            if (normalizedTime < fadeStart)
                return;

            float fadeTime = SmoothStep(Mathf.InverseLerp(fadeStart, 1.0f, normalizedTime));
            canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, fadeTime);
        }

        private float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return 1.0f - Mathf.Pow(1.0f - value, 3.0f);
        }

        private float EaseInOutCubic(float value)
        {
            value = Mathf.Clamp01(value);

            if (value < 0.5f)
                return 4.0f * value * value * value;

            return 1.0f - Mathf.Pow(-2.0f * value + 2.0f, 3.0f) * 0.5f;
        }

        private float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3.0f - 2.0f * value);
        }
    }
}
