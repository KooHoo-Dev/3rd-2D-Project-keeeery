using System;
using System.Collections;
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

        private Coroutine animationRoutine;
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

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            animationRoutine = StartCoroutine(ShowRoutine());
        }

        public void PlayHide(Action onComplete = null)
        {
            if (gameObject.activeInHierarchy == false)
            {
                HideImmediate();
                onComplete?.Invoke();
                return;
            }

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            animationRoutine = StartCoroutine(HideRoutine(onComplete));
        }

        public void HideImmediate()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

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

        private IEnumerator ShowRoutine()
        {
            float elapsed = 0.0f;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            Vector2 startPosition = baseAnchoredPosition + showStartOffset;

            if (rectTransform != null)
                rectTransform.anchoredPosition = startPosition;

            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = showDuration <= 0.0f ? 1.0f : Mathf.Clamp01(elapsed / showDuration);
                float eased = EaseOutCubic(t);

                if (canvasGroup != null)
                    canvasGroup.alpha = eased;

                if (rectTransform != null)
                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, baseAnchoredPosition, eased);

                yield return null;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1.0f;

            if (rectTransform != null)
                rectTransform.anchoredPosition = baseAnchoredPosition;

            animationRoutine = null;
        }

        private IEnumerator HideRoutine(Action onComplete)
        {
            float elapsed = 0.0f;
            Vector2 startPosition = rectTransform != null ? rectTransform.anchoredPosition : baseAnchoredPosition;
            Vector2 endPosition = baseAnchoredPosition + hideEndOffset;

            while (elapsed < hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = hideDuration <= 0.0f ? 1.0f : Mathf.Clamp01(elapsed / hideDuration);
                float eased = EaseOutCubic(t);

                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, eased);

                if (rectTransform != null)
                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);

                yield return null;
            }

            HideImmediate();
            onComplete?.Invoke();
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1.0f - Mathf.Pow(1.0f - t, 3.0f);
        }
    }
}