using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arcanist
{
    public class RecentCardUIItem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Animation")]
        [SerializeField] private float showDuration = 0.12f;
        [SerializeField] private float hideDuration = 0.18f;
        [SerializeField] private Vector2 showStartOffset = new(24.0f, 0.0f);
        [SerializeField] private Vector2 hideEndOffset = new(-24.0f, 0.0f);

        private Sequence animationSequence;
        private Vector2 baseAnchoredPosition;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (rectTransform == null)
                rectTransform = transform as RectTransform;

            if (rectTransform != null)
                baseAnchoredPosition = rectTransform.anchoredPosition;
        }

        private void OnDisable()
        {
            KillAnimation();
        }

        public void Initialize(CardData cardData)
        {
            if (cardData == null)
            {
                HideImmediate();
                return;
            }

            if (icon != null)
                icon.sprite = cardData.Icon;

            if (nameText != null)
                nameText.text = cardData.CardName;

            PlayShow();
        }

        public void PlayShow()
        {
            gameObject.SetActive(true);

            KillAnimation();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (rectTransform != null)
                rectTransform.anchoredPosition = baseAnchoredPosition + showStartOffset;

            animationSequence = DOTween.Sequence().SetUpdate(true);

            if (canvasGroup != null)
                animationSequence.Join(canvasGroup
                    .DOFade(1.0f, Mathf.Max(0.01f, showDuration))
                    .SetEase(Ease.OutCubic));
            if (rectTransform != null)
                animationSequence.Join(rectTransform
                    .DOAnchorPos(baseAnchoredPosition, Mathf.Max(0.01f, showDuration))
                    .SetEase(Ease.OutCubic));

            animationSequence.OnComplete(() => animationSequence = null);
        }

        public void PlayHide(Action onComplete = null)
        {
            if (gameObject.activeInHierarchy == false)
            {
                HideImmediate();
                onComplete?.Invoke();
                return;
            }

            KillAnimation();
            animationSequence = DOTween.Sequence().SetUpdate(true);

            if (canvasGroup != null)
                animationSequence.Join(canvasGroup
                    .DOFade(0.0f, Mathf.Max(0.01f, hideDuration))
                    .SetEase(Ease.OutCubic));
            if (rectTransform != null)
                animationSequence.Join(rectTransform
                    .DOAnchorPos(baseAnchoredPosition + hideEndOffset, Mathf.Max(0.01f, hideDuration))
                    .SetEase(Ease.OutCubic));

            animationSequence.OnComplete(() =>
            {
                animationSequence = null;
                HideImmediate();
                onComplete?.Invoke();
            });
        }

        public void HideImmediate()
        {
            KillAnimation();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (rectTransform != null)
                rectTransform.anchoredPosition = baseAnchoredPosition;

            gameObject.SetActive(false);
        }

        private void KillAnimation()
        {
            animationSequence?.Kill();
            animationSequence = null;
        }
    }
}
