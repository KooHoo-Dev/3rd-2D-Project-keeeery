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

        private CardHandState state;
        private CardHandView handView;
        private CardDrawMotion drawMotion;
        private CardUseMotion useMotion;
        private CardUseFxMotion useFxMotion;

        // Kept for compatibility. The previous property also meant "drawing or using".
        public bool IsDrawing => state != null && state.IsBusy;
        public bool IsBusy => state != null && state.IsBusy;
        public bool HasEmptySlot => state != null && state.HasEmptySlot;

        private void Awake()
        {
            int slotCount = cardSlots != null ? cardSlots.Length : 0;
            state = new CardHandState(slotCount);

            CardDrawMotionSettings drawSettings = new(
                cardRiseDistance,
                drawDuration,
                startYRotation,
                extraYSpin,
                startZRotation,
                overshootY,
                minFlipScaleX);
            CardUseFxSettings useFxSettings = new(
                useFxAbsorbDuration,
                useFxStartScale,
                useFxEndScale);
            useFxMotion = new CardUseFxMotion(
                this,
                cardUseFxPrefab,
                useFxParent,
                absorbTarget,
                absorbTargetCamera,
                useFxSettings);
            CardUseMotionSettings useSettings = new(
                useRiseDistance,
                useDuration,
                useScaleUp,
                useEndScale,
                useYSpin,
                useZRotation,
                useFadeStartNormalizedTime,
                useFxSpawnNormalizedTime,
                minFlipScaleX);

            handView = new CardHandView(cardSlots, cardPrefab);
            drawMotion = new CardDrawMotion(drawSettings);
            useMotion = new CardUseMotion(useSettings, useFxMotion);
        }

        public void ClearAllSlots()
        {
            StopAllCoroutines();
            state.Clear();
            handView.Clear();
            useFxMotion.Clear();
        }

        public bool TryDrawToFirstEmptySlot(CardData cardData, Action onCompleted = null)
        {
            if (cardData == null)
            {
                Debug.LogWarning("CardHandUI ::: Cannot draw because CardData is null.");
                return false;
            }

            if (state.TryBeginDraw(cardData, out int slotIndex) == false)
                return false;

            StartCoroutine(DrawRoutine(slotIndex, cardData, onCompleted));
            return true;
        }

        public void UseCard(int slotIndex)
        {
            if (state.TryBeginUse(slotIndex, out _) == false)
                return;

            StartCoroutine(UseRoutine(slotIndex));
        }

        public CardData GetCard(int slotIndex)
        {
            return state.GetCard(slotIndex);
        }

        private IEnumerator DrawRoutine(int slotIndex, CardData cardData, Action onCompleted)
        {
            RectTransform card = handView.CreateCard(slotIndex, cardData);
            bool succeeded = card != null;

            if (succeeded)
                yield return drawMotion.Play(card);

            succeeded = card != null;

            state.CompleteDraw(slotIndex, succeeded);

            if (succeeded)
                onCompleted?.Invoke();
        }

        private IEnumerator UseRoutine(int slotIndex)
        {
            RectTransform card = handView.FindCard(slotIndex);

            if (card != null)
                yield return useMotion.Play(card);

            state.CompleteUse(slotIndex);
        }
    }
}
