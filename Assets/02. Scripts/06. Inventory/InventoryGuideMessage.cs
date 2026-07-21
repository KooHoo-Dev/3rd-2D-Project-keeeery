using DG.Tweening;
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
        private Sequence displaySequence;
        private Tween shakeTween;

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
            KillTweens();

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
            displaySequence = DOTween.Sequence()
                .SetUpdate(true)
                .AppendInterval(Mathf.Max(0.0f, displayDuration))
                .Append(canvasGroup.DOFade(0.0f, Mathf.Max(0.01f, fadeOutDuration)))
                .OnComplete(() =>
                {
                    displaySequence = null;
                    HideImmediate();
                });
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
            {
                displaySequence = DOTween.Sequence()
                    .SetUpdate(true)
                    .Append(canvasGroup.DOFade(0.0f, Mathf.Max(0.01f, fadeOutDuration)))
                    .OnComplete(() =>
                    {
                        displaySequence = null;
                        HideImmediate();
                    });
            }
            else
                HideImmediate();
        }

        public void Shake()
        {
            if (messageRect == null || !gameObject.activeInHierarchy)
                return;

            shakeTween?.Kill();
            float duration = Mathf.Max(0.01f, shakeDuration);
            float distance = Mathf.Max(0.0f, shakeDistance);

            shakeTween = DOTween.To(
                    () => 0.0f,
                    progress =>
                    {
                        float offsetX = Mathf.Sin(progress * Mathf.PI * 8.0f)
                            * distance
                            * (1.0f - progress);
                        messageRect.anchoredPosition = defaultPosition + Vector2.right * offsetX;
                    },
                    1.0f,
                    duration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    messageRect.anchoredPosition = defaultPosition;
                    shakeTween = null;
                });
        }

        private bool CanShow()
        {
            if (messageText != null && canvasGroup != null)
                return true;

            CacheComponents();
            return messageText != null && canvasGroup != null;
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
            displaySequence?.Kill();
            displaySequence = null;
        }

        private void KillTweens()
        {
            StopDisplayRoutine();
            shakeTween?.Kill();
            shakeTween = null;
        }
    }
}
