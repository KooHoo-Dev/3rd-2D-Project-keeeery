using System;
using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class CardHandUI : MonoBehaviour
    {
        [Header("Card Slots")]
        [SerializeField] private RectTransform[] cardSlots;
        [SerializeField] private RectTransform cardPrefab;

        [Header("Draw Motion")]
        [SerializeField] private float cardRiseDistance = 150f;
        [SerializeField] private float drawDuration = 0.3f;
        [SerializeField] private float startYRotation = 85f;
        [SerializeField] private float extraYSpin = 540f;
        [SerializeField] private float startZRotation = -8f;
        [SerializeField] private float overshootY = 0f;
        [SerializeField] private float minFlipScaleX = 0.3f;

        [Header("Use Motion")]
        [SerializeField] private float useRiseDistance = 110f;
        [SerializeField] private float useDuration = 0.22f;
        [SerializeField] private float useScaleUp = 1.2f;
        [SerializeField] private float useEndScale = 0.75f;
        [SerializeField] private float useYSpin = 540f;
        [SerializeField] private float useZRotation = -12f;
        [SerializeField, Range(0f, 1f)] private float useFadeStartNormalizedTime = 0.45f;

        [Header("Use FX")]
        [SerializeField] private GameObject cardUseFxPrefab;
        [SerializeField] private Transform useFxParent;
        [SerializeField] private Transform absorbTarget;
        [SerializeField] private Camera absorbTargetCamera;
        [SerializeField] private float useFxAbsorbDuration = 0.32f;
        [SerializeField] private float useFxStartScale = 1f;
        [SerializeField] private float useFxEndScale = 0.2f;
        [SerializeField, Range(0f, 1f)] private float useFxSpawnNormalizedTime = 0.5f;

        private CardData[] slotCards;
        private bool isDrawing;
        private bool isUsingCard;

        public bool IsDrawing => isDrawing || isUsingCard;
        public bool HasEmptySlot => GetFirstEmptySlot() >= 0;

        private void Awake()
        {
            slotCards = new CardData[cardSlots.Length];
        }

        public bool TryDrawToFirstEmptySlot(CardData cardData, Action onCompleted = null)
        {
            if (cardData == null)
            {
                Debug.LogWarning("CardHandUI ::: Cannot draw because CardData is null.");
                return false;
            }

            if (isDrawing)
                return false;

            int slotIndex = GetFirstEmptySlot();

            if (slotIndex < 0)
                return false;

            StartCoroutine(DrawCard(slotIndex, cardData, onCompleted));
            return true;
        }

        public void UseCard(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotCards.Length)
                return;

            if (slotCards[slotIndex] == null)
                return;

            if (isDrawing || isUsingCard)
                return;

            CardView cardView = cardSlots[slotIndex].GetComponentInChildren<CardView>();
            if (cardView != null)
                cardView.DebugUse();

            RectTransform card = cardView != null
                ? cardView.GetComponent<RectTransform>()
                : GetTopCardInSlot(slotIndex);

            if (card == null)
            {
                slotCards[slotIndex] = null;
                return;
            }

            StartCoroutine(PlayUseCardMotion(slotIndex, card));
        }

        private RectTransform GetTopCardInSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= cardSlots.Length)
                return null;

            if (cardSlots[slotIndex].childCount <= 0)
                return null;

            Transform child = cardSlots[slotIndex].GetChild(cardSlots[slotIndex].childCount - 1);
            return child.GetComponent<RectTransform>();
        }

        private IEnumerator PlayUseCardMotion(int slotIndex, RectTransform card)
        {
            isUsingCard = true;

            Vector2 startPos = card.anchoredPosition;
            Vector2 endPos = startPos + Vector2.up * useRiseDistance;
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(card);
            canvasGroup.alpha = 1f;
            bool useFxSpawned = false;
            float t = 0f;

            while (t < useDuration)
            {
                if (IsDestroyed(card))
                {
                    EndUsingCard(slotIndex);
                    yield break;
                }

                t += Time.deltaTime;

                float n = Mathf.Clamp01(t / useDuration);
                float eased = EaseOutCubic(n);

                SetAnchoredPosition(card, Vector2.LerpUnclamped(startPos, endPos, eased));

                float scale;
                if (n < 0.35f)
                    scale = Mathf.Lerp(1f, useScaleUp, n / 0.35f);
                else
                    scale = Mathf.Lerp(useScaleUp, useEndScale, (n - 0.35f) / 0.65f);

                float yRotation = Mathf.Lerp(0f, useYSpin, eased);
                float zRotation = Mathf.Lerp(0f, useZRotation, eased);
                float flipScaleX = Mathf.Lerp(minFlipScaleX, 1f, Mathf.Abs(Mathf.Cos(yRotation * Mathf.Deg2Rad)));

                card.localScale = new Vector3(scale * flipScaleX, scale, scale);
                card.localRotation = Quaternion.Euler(0f, yRotation, zRotation);

                float fadeT = Mathf.InverseLerp(useFadeStartNormalizedTime, 1f, n);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, EaseOutCubic(fadeT));

                if (useFxSpawned == false && n >= useFxSpawnNormalizedTime)
                {
                    SpawnUseFx(card);
                    useFxSpawned = true;
                }

                yield return null;
            }

            if (IsDestroyed(card) == false)
            {
                if (useFxSpawned == false)
                    SpawnUseFx(card);

                Destroy(card.gameObject);
            }

            EndUsingCard(slotIndex);
        }

        private void SpawnUseFx(RectTransform card)
        {
            if (cardUseFxPrefab == null || IsDestroyed(card))
                return;

            Transform parent = useFxParent != null ? useFxParent : transform;
            GameObject fx = Instantiate(cardUseFxPrefab, parent);
            fx.transform.rotation = Quaternion.identity;
            fx.transform.localScale = Vector3.one * useFxStartScale;

            ParticleSystem particleSystem = fx.GetComponent<ParticleSystem>();
            if (particleSystem != null)
                particleSystem.Play(true);

            RectTransform fxRect = fx.GetComponent<RectTransform>();
            RectTransform parentRect = parent as RectTransform;

            if (fxRect != null && parentRect != null)
            {
                Vector2 startPoint = UiWorldToLocalPoint(parentRect, card.position);
                fxRect.anchoredPosition = startPoint;
                fxRect.localRotation = Quaternion.identity;
                fxRect.localScale = Vector3.one * useFxStartScale;

                StartCoroutine(PlayUseFxAbsorbMotion(fxRect, parentRect, startPoint));
                return;
            }

            fx.transform.position = card.position;
            StartCoroutine(PlayWorldUseFxAbsorbMotion(fx.transform));
        }

        private IEnumerator PlayUseFxAbsorbMotion(RectTransform fx, RectTransform parentRect, Vector2 startPoint)
        {
            float t = 0f;

            while (t < useFxAbsorbDuration)
            {
                if (IsDestroyed(fx))
                    yield break;

                t += Time.deltaTime;

                float n = Mathf.Clamp01(t / useFxAbsorbDuration);
                float moveT = EaseInCubic(n);
                float scaleT = EaseOutCubic(n);
                Vector2 targetPoint = GetAbsorbTargetPoint(parentRect);

                fx.anchoredPosition = Vector2.LerpUnclamped(startPoint, targetPoint, moveT);
                fx.localScale = Vector3.one * Mathf.Lerp(useFxStartScale, useFxEndScale, scaleT);

                yield return null;
            }

            if (IsDestroyed(fx) == false)
                Destroy(fx.gameObject);
        }

        private IEnumerator PlayWorldUseFxAbsorbMotion(Transform fx)
        {
            float t = 0f;
            Vector3 startPosition = fx.position;

            while (t < useFxAbsorbDuration)
            {
                if (fx == null)
                    yield break;

                t += Time.deltaTime;

                float n = Mathf.Clamp01(t / useFxAbsorbDuration);
                float moveT = EaseInCubic(n);
                float scaleT = EaseOutCubic(n);
                Vector3 targetPosition = absorbTarget != null ? absorbTarget.position : startPosition;

                fx.position = Vector3.LerpUnclamped(startPosition, targetPosition, moveT);
                fx.localScale = Vector3.one * Mathf.Lerp(useFxStartScale, useFxEndScale, scaleT);

                yield return null;
            }

            if (fx != null)
                Destroy(fx.gameObject);
        }

        private Vector2 GetAbsorbTargetPoint(RectTransform parentRect)
        {
            if (absorbTarget == null)
                return Vector2.zero;

            if (absorbTarget is RectTransform targetRect)
                return UiWorldToLocalPoint(parentRect, targetRect.position);

            Camera worldCamera = absorbTargetCamera != null ? absorbTargetCamera : Camera.main;
            return WorldTransformToLocalPoint(parentRect, absorbTarget.position, worldCamera);
        }

        private Vector2 UiWorldToLocalPoint(RectTransform parentRect, Vector3 worldPosition)
        {
            Camera uiCamera = GetCanvasCamera(parentRect);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint);
            return localPoint;
        }

        private Vector2 WorldTransformToLocalPoint(RectTransform parentRect, Vector3 worldPosition, Camera worldCamera)
        {
            if (worldCamera == null)
                return Vector2.zero;

            Camera uiCamera = GetCanvasCamera(parentRect);
            Vector2 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint);
            return localPoint;
        }

        private Camera GetCanvasCamera(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return canvas.worldCamera;

            return null;
        }

        private void EndUsingCard(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < slotCards.Length)
                slotCards[slotIndex] = null;

            isUsingCard = false;
        }

        private int GetFirstEmptySlot()
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] == null)
                    return i;
            }

            return -1;
        }

        public CardData GetCard(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotCards.Length)
                return null;

            return slotCards[slotIndex];
        }

        private IEnumerator DrawCard(int slotIndex, CardData cardData, Action onCompleted)
        {
            isDrawing = true;
            slotCards[slotIndex] = cardData;

            RectTransform slot = cardSlots[slotIndex];
            RectTransform card = Instantiate(cardPrefab, slot);

            card.gameObject.SetActive(false);
            card.SetAsLastSibling();
            ResetCardTransform(card);

            CardView cardView = card.GetComponent<CardView>();
            if (cardView != null)
                cardView.Setup(cardData);

            CanvasGroup canvasGroup = GetOrAddCanvasGroup(card);
            canvasGroup.alpha = 1f;

            card.gameObject.SetActive(true);

            Vector2 endPos = Vector2.zero;
            Vector2 startPos = endPos + Vector2.down * cardRiseDistance;
            Vector2 overshootPos = endPos + Vector2.up * overshootY;

            if (IsDestroyed(card))
            {
                EndDrawingWithoutCallback();
                yield break;
            }

            SetAnchoredPosition(card, startPos);
            card.localScale = Vector3.one * 0.8f;
            card.localRotation = Quaternion.Euler(0f, startYRotation + extraYSpin, startZRotation);

            float t = 0f;

            while (t < drawDuration)
            {
                if (IsDestroyed(card))
                {
                    EndDrawingWithoutCallback();
                    yield break;
                }

                t += Time.deltaTime;

                float n = Mathf.Clamp01(t / drawDuration);
                float eased = EaseOutCubic(n);

                if (n < 0.82f)
                {
                    float moveT = EaseOutCubic(n / 0.82f);
                    SetAnchoredPosition(card, Vector2.LerpUnclamped(startPos, overshootPos, moveT));
                }
                else
                {
                    float settleT = EaseOutCubic((n - 0.82f) / 0.18f);
                    SetAnchoredPosition(card, Vector2.LerpUnclamped(overshootPos, endPos, settleT));
                }

                if (IsDestroyed(card))
                {
                    EndDrawingWithoutCallback();
                    yield break;
                }

                float yRotation = Mathf.Lerp(startYRotation + extraYSpin, 0f, eased);
                float zRotation = Mathf.Lerp(startZRotation, 0f, eased);
                card.localRotation = Quaternion.Euler(0f, yRotation, zRotation);

                float scale = Mathf.Lerp(0.8f, 1f, eased);
                float flipScaleX = Mathf.Lerp(minFlipScaleX, 1f, Mathf.Abs(Mathf.Cos(yRotation * Mathf.Deg2Rad)));
                card.localScale = new Vector3(scale * flipScaleX, scale, scale);

                yield return null;
            }

            if (IsDestroyed(card))
            {
                EndDrawingWithoutCallback();
                yield break;
            }

            SetAnchoredPosition(card, endPos);
            card.localScale = Vector3.one;
            card.localRotation = Quaternion.identity;

            isDrawing = false;
            onCompleted?.Invoke();
        }

        private void EndDrawingWithoutCallback()
        {
            isDrawing = false;
        }

        private bool IsDestroyed(RectTransform card)
        {
            return card == null;
        }

        private void ResetCardTransform(RectTransform card)
        {
            if (IsDestroyed(card))
                return;

            card.localScale = Vector3.one;
            card.localRotation = Quaternion.identity;
            card.anchoredPosition3D = Vector3.zero;
        }

        private void SetAnchoredPosition(RectTransform card, Vector2 anchoredPosition)
        {
            if (IsDestroyed(card))
                return;

            card.anchoredPosition = anchoredPosition;
        }


        private CanvasGroup GetOrAddCanvasGroup(RectTransform card)
        {
            CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = card.gameObject.AddComponent<CanvasGroup>();

            return canvasGroup;
        }

        private float EaseInCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value;
        }

        private float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return 1f - Mathf.Pow(1f - value, 3f);
        }
    }
}
