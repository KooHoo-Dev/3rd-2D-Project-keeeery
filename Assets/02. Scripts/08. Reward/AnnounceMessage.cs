using DG.Tweening;
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

        private Sequence showSequence;

        private void Awake()
        {
            ResetMessage();
        }

        private void OnDisable()
        {
            showSequence?.Kill();
            showSequence = null;
        }

        public void Show(string message, bool useFadeIn = true)
        {
            if (CanvasGroup == null || messageText == null)
                return;

            messageText.text = message;

            showSequence?.Kill();

            if (!useFadeIn)
                CanvasGroup.alpha = 1.0f;

            showSequence = DOTween.Sequence().SetUpdate(true);

            if (useFadeIn)
                showSequence.Append(CanvasGroup
                    .DOFade(1.0f, Mathf.Max(0.01f, fadeInDuration))
                    .SetEase(Ease.InOutSine));

            if (holdDuration > 0.0f)
                showSequence.AppendInterval(holdDuration);

            showSequence
                .Append(CanvasGroup
                    .DOFade(0.0f, Mathf.Max(0.01f, fadeOutDuration))
                    .SetEase(Ease.InOutSine))
                .OnComplete(() => showSequence = null);
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
