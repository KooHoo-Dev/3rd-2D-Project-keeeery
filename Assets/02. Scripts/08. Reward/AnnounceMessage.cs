using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class AnnounceMessage : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] public CanvasGroup CanvasGroup;
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float holdDuration = 0.35f;
        [SerializeField] private float fadeOutDuration = 0.25f;

        [Header("Message Text")]
        [SerializeField] private TMPro.TMP_Text messageText;

        private Coroutine showCoroutine;

        private void Awake()
        {
            ResetMessage();
        }

        public void Show(string message)
        {
            Show(message, true);
        }

        public void Show(string message, bool useFadeIn)
        {
            if (CanvasGroup == null || messageText == null)
                return;

            messageText.text = message;

            if (showCoroutine != null)
                StopCoroutine(showCoroutine);

            showCoroutine = StartCoroutine(ShowRoutine(useFadeIn));
        }

        private IEnumerator ShowRoutine(bool useFadeIn)
        {
            if (useFadeIn)
                yield return FadeCanvasGroup(1.0f, fadeInDuration);
            else
                CanvasGroup.alpha = 1.0f;

            if (holdDuration > 0.0f)
                yield return new WaitForSecondsRealtime(holdDuration);

            yield return FadeCanvasGroup(0.0f, fadeOutDuration);

            showCoroutine = null;
        }

        private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            float startAlpha = CanvasGroup.alpha;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0.0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = t * t * (3.0f - 2.0f * t);
                CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);

                yield return null;
            }

            CanvasGroup.alpha = targetAlpha;
        }

        private void ResetMessage()
        {
            if (CanvasGroup == null) return;

            CanvasGroup.alpha = 0.0f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }
    }
}
