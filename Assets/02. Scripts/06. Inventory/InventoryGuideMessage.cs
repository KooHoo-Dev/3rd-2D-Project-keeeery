using System.Collections;
using TMPro;
using UnityEngine;

namespace Arcanist
{
    public class InventoryGuideMessage : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float displayDuration = 1.2f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float shakeDistance = 8.0f;
        [SerializeField] private float shakeDuration = 0.18f;

        private RectTransform messageRect;
        private CanvasGroup canvasGroup;
        private Vector2 defaultPosition;
        private Coroutine displayRoutine;
        private Coroutine shakeRoutine;

        public void Configure(
            TMP_Text text,
            float duration,
            float fadeDuration,
            float distance,
            float shakeTime)
        {
            if (messageText == null)
                messageText = text;

            displayDuration = duration;
            fadeOutDuration = fadeDuration;
            shakeDistance = distance;
            shakeDuration = shakeTime;
            CacheComponents();
        }

        private void Awake()
        {
            CacheComponents();
            HideImmediate();
        }

        private void OnDisable()
        {
            StopRoutines();

            if (messageRect != null)
                messageRect.anchoredPosition = defaultPosition;
        }

        private void CacheComponents()
        {
            if (messageText == null)
                messageText = GetComponent<TMP_Text>();
            if (messageText == null)
                return;

            messageRect = messageText.rectTransform;
            defaultPosition = messageRect.anchoredPosition;
            canvasGroup = messageText.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = messageText.gameObject.AddComponent<CanvasGroup>();
        }

        public void ShowTemporary(string message)
        {
            if (!CanShow())
                return;

            StopDisplayRoutine();
            SetVisible(message);
            displayRoutine = StartCoroutine(ShowTemporaryRoutine());
        }

        public void ShowPersistent(string message)
        {
            if (!CanShow())
                return;

            StopDisplayRoutine();
            SetVisible(message);
        }

        public void Hide(bool animated)
        {
            if (!CanShow())
                return;

            StopDisplayRoutine();

            if (animated && gameObject.activeInHierarchy)
                displayRoutine = StartCoroutine(FadeOutRoutine());
            else
                HideImmediate();
        }

        public void Shake()
        {
            if (messageRect == null || !gameObject.activeInHierarchy)
                return;

            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);

            shakeRoutine = StartCoroutine(ShakeRoutine());
        }

        private bool CanShow()
        {
            if (messageText != null && canvasGroup != null)
                return true;

            CacheComponents();
            return messageText != null && canvasGroup != null;
        }

        private IEnumerator ShowTemporaryRoutine()
        {
            yield return new WaitForSecondsRealtime(displayDuration);
            yield return FadeOutRoutine();
        }

        private IEnumerator FadeOutRoutine()
        {
            float startAlpha = canvasGroup.alpha;
            float duration = Mathf.Max(0.01f, fadeOutDuration);
            float elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0.0f, elapsedTime / duration);
                yield return null;
            }

            HideImmediate();
            displayRoutine = null;
        }

        private IEnumerator ShakeRoutine()
        {
            float duration = Mathf.Max(0.01f, shakeDuration);
            float distance = Mathf.Max(0.0f, shakeDistance);
            float elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float offsetX = Mathf.Sin(progress * Mathf.PI * 8.0f) * distance * (1.0f - progress);
                messageRect.anchoredPosition = defaultPosition + Vector2.right * offsetX;
                yield return null;
            }

            messageRect.anchoredPosition = defaultPosition;
            shakeRoutine = null;
        }

        private void SetVisible(string message)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            canvasGroup.alpha = 1.0f;
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0.0f;
            if (messageText != null)
                messageText.gameObject.SetActive(false);
        }

        private void StopDisplayRoutine()
        {
            if (displayRoutine == null)
                return;

            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }

        private void StopRoutines()
        {
            StopDisplayRoutine();

            if (shakeRoutine == null)
                return;

            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
    }
}
