using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcanist
{
    public class InventoryWindow : MonoBehaviour
    {
        [Header("Debug References")]
        [SerializeField] private CardDatabase cardDatabase;
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Debug Counts")]
        [SerializeField] private int allCardCount;
        [SerializeField] private int ownedCardCount;
        [SerializeField] private int deckCardCount;
        [SerializeField] private int maxDeckCardCount;

        [Header("Slot")]
        [SerializeField] private InventoryCardSlot cardSlotPrefab;

        [Header("Parents")]
        [SerializeField] private Transform allCardContent;
        [SerializeField] private Transform deckContent;

        [Header("Drag Drop Areas")]
        [SerializeField] private RectTransform collectionDropArea;
        [SerializeField] private RectTransform deckDropArea;

        [Header("Deck Edit Action")]
        [SerializeField] private TMP_Text deckEditActionText;
        [SerializeField] private Color actionActiveColor = Color.white;
        [SerializeField] private Color actionInactiveColor = new Color(1.0f, 1.0f, 1.0f, 0.35f);
        [SerializeField] private Color actionPressedColor = new Color(0.55f, 0.55f, 0.55f, 1.0f);
        [SerializeField] private float actionHoverScale = 1.08f;

        [Header("Count Text")]
        [SerializeField] private TMP_Text allCardCountText;
        [SerializeField] private TMP_Text deckCardCountText;

        [Header("Guide Message")]
        [SerializeField] private TMP_Text guideMessageText;
        [SerializeField] private float guideMessageDuration = 1.2f;
        [SerializeField] private float guideMessageFadeOutDuration = 0.3f;

        [Header("Blocked Close Feedback")]
        [SerializeField] private float blockedCloseShakeDistance = 8.0f;
        [SerializeField] private float blockedCloseShakeDuration = 0.18f;

        [Header("Rules")]
        [SerializeField] private bool canEditDeck = true;

        [ContextMenu("Debug Refresh References")]
        private void DebugRefreshReferences()
        {
            cardDatabase = CardDatabase.Instance != null ? CardDatabase.Instance : FindFirstObjectByType<CardDatabase>();
            playerInventory = PlayerInventory.Instance != null ? PlayerInventory.Instance : FindFirstObjectByType<PlayerInventory>();
            Refresh();
        }

        private readonly List<InventoryCardSlot> allCardSlots = new List<InventoryCardSlot>();
        private readonly List<InventoryCardSlot> deckSlots = new List<InventoryCardSlot>();
        private InventoryCardSlot selectedCollectionSlot;
        private InventoryCardSlot selectedDeckSlot;
        private const int MIN_DECK_CARD_COUNT = 5;
        private const string MINIMUM_DECK_WARNING_MESSAGE = "카드 덱에는 최소 5장의 카드가 존재해야 합니다.";

        private Vector3 actionDefaultScale = Vector3.one;
        private bool canUseDeckEdit;
        private RectTransform guideMessageRect;
        private Vector2 guideMessageDefaultPosition;
        private CanvasGroup guideMessageGroup;
        private Coroutine guideMessageRoutine;
        private Coroutine guideMessageShakeRoutine;
        private bool isMinimumDeckWarningVisible;

        public CardDatabase CardDatabase => cardDatabase;
        public PlayerInventory PlayerInventory => playerInventory;
        public bool CanEditDeck => canEditDeck;

        private void Awake()
        {
            ResolveReferences();
            CacheGuideMessageComponents();

            if (deckEditActionText != null)
                actionDefaultScale = deckEditActionText.rectTransform.localScale;
        }

        private void OnEnable()
        {
            ResolveReferences();
            CacheGuideMessageComponents();

            if (playerInventory != null)
                playerInventory.OnInventoryChanged += Refresh;

            Refresh();
        }

        public void PlayMinimumDeckWarningFeedback()
        {
            ShowMinimumDeckWarning(true);
            ShakeGuideMessage();
        }

        public bool HasMinimumDeckCards()
        {
            RefreshDebugCounts();
            return deckCardCount >= MIN_DECK_CARD_COUNT;
        }

        private void CacheGuideMessageComponents()
        {
            if (guideMessageText == null)
                return;

            guideMessageRect = guideMessageText.rectTransform;
            guideMessageDefaultPosition = guideMessageRect.anchoredPosition;
            guideMessageGroup = guideMessageText.GetComponent<CanvasGroup>();

            if (guideMessageGroup == null)
                guideMessageGroup = guideMessageText.gameObject.AddComponent<CanvasGroup>();

            guideMessageGroup.alpha = 0.0f;
            guideMessageText.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (playerInventory != null)
                playerInventory.OnInventoryChanged -= Refresh;
        }

        private void ResolveReferences()
        {
            if (cardDatabase == null)
                cardDatabase = CardDatabase.Instance != null ? CardDatabase.Instance : FindFirstObjectByType<CardDatabase>();

            if (playerInventory == null)
                playerInventory = PlayerInventory.Instance != null ? PlayerInventory.Instance : FindFirstObjectByType<PlayerInventory>();

            RefreshDebugCounts();
        }

        public void SetDeckEditMode(bool canEdit)
        {
            canEditDeck = canEdit;
            RefreshButtonState();
        }

        public void Refresh()
        {
            ResolveReferences();

            ClearSelection();
            RefreshAllCards();
            RefreshDeckCards();
            RefreshCountText();
            RefreshButtonState();
        }

        public void SelectSlot(InventoryCardSlot slot)
        {
            if (slot == null)
                return;

            if (slot.SlotArea == InventoryCardSlotArea.Collection)
            {
                if (!slot.IsOwned)
                {
                    ClearSelection();
                    RefreshButtonState();
                    return;
                }

                if (selectedCollectionSlot != null)
                    selectedCollectionSlot.SetSelected(false);

                selectedCollectionSlot = slot;
                selectedCollectionSlot.SetSelected(true);

                if (selectedDeckSlot != null)
                {
                    selectedDeckSlot.SetSelected(false);
                    selectedDeckSlot = null;
                }
            }
            else
            {
                if (selectedDeckSlot != null)
                    selectedDeckSlot.SetSelected(false);

                selectedDeckSlot = slot;
                selectedDeckSlot.SetSelected(true);

                if (selectedCollectionSlot != null)
                {
                    selectedCollectionSlot.SetSelected(false);
                    selectedCollectionSlot = null;
                }
            }

            RefreshButtonState();
        }

        public void ToggleSelectedCardDeck()
        {
            if (!canEditDeck)
                return;

            if (selectedCollectionSlot != null)
            {
                TryAddCardToDeck(selectedCollectionSlot.CardData);
                return;
            }

            if (selectedDeckSlot != null && selectedDeckSlot.CardData != null)
                playerInventory.RemoveFromDeckAt(selectedDeckSlot.DeckIndex);
        }

        public void ShowUnownedCardMessage()
        {
            ShowGuideMessage("아직 획득하지 못한 카드는 카드 덱에 추가할 수 없습니다.");
        }

        public void HandleCardDropped(InventoryCardSlot sourceSlot, PointerEventData eventData)
        {
            if (!canEditDeck || sourceSlot == null || sourceSlot.CardData == null)
                return;

            RectTransform targetDeckArea = deckDropArea != null ? deckDropArea : deckContent as RectTransform;
            RectTransform targetCollectionArea = collectionDropArea != null ? collectionDropArea : allCardContent as RectTransform;

            if (sourceSlot.SlotArea == InventoryCardSlotArea.Collection)
            {
                if (IsPointerInside(targetDeckArea, eventData))
                    TryAddCardToDeck(sourceSlot.CardData);

                return;
            }

            if (sourceSlot.SlotArea == InventoryCardSlotArea.Deck && IsPointerInside(targetCollectionArea, eventData))
                playerInventory.RemoveFromDeckAt(sourceSlot.DeckIndex);
        }

        public void OnDeckEditActionPointerEnter()
        {
            if (deckEditActionText == null || !canUseDeckEdit)
                return;

            deckEditActionText.rectTransform.localScale = actionDefaultScale * actionHoverScale;
        }

        public void OnDeckEditActionPointerExit()
        {
            if (deckEditActionText == null)
                return;

            deckEditActionText.rectTransform.localScale = actionDefaultScale;
            RefreshButtonState();
        }

        public void OnDeckEditActionPointerDown()
        {
            if (deckEditActionText == null || !canUseDeckEdit)
                return;

            deckEditActionText.color = actionPressedColor;
        }

        public void OnDeckEditActionPointerUp()
        {
            if (deckEditActionText == null)
                return;

            RefreshButtonState();
        }

        public void AddCardToDeck(CardData cardData)
        {
            if (!canEditDeck || cardData == null)
                return;

            TryAddCardToDeck(cardData);
        }
        private void TryAddCardToDeck(CardData cardData)
        {
            if (cardData == null)
                return;

            if (playerInventory.IsInDeck(cardData))
            {
                ShowGuideMessage("해당 카드가 카드 덱에 이미 존재합니다.");
                return;
            }

            if (playerInventory.IsDeckFull())
            {
                ShowGuideMessage("카드 덱이 가득 찼습니다.");
                return;
            }

            playerInventory.AddToDeck(cardData);
        }

        private bool IsPointerInside(RectTransform targetRect, PointerEventData eventData)
        {
            if (targetRect == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                targetRect,
                eventData.position,
                eventData.pressEventCamera);
        }

        public void RemoveCardFromDeck(CardData cardData)
        {
            if (!canEditDeck || cardData == null)
                return;

            playerInventory.RemoveFromDeck(cardData);
        }

        public void MarkCardAsSeen(CardData cardData)
        {
            playerInventory.MarkCardAsSeen(cardData);
        }

        private void RefreshAllCards()
        {
            ClearChildren(allCardContent);
            allCardSlots.Clear();

            if (playerInventory == null || cardSlotPrefab == null || allCardContent == null)
                return;

            List<CardData> sortedCards = GetSortedAllCards();

            foreach (CardData cardData in sortedCards)
            {
                InventoryCardSlot slot = Instantiate(cardSlotPrefab, allCardContent);
                bool owned = playerInventory.HasCard(cardData);
                bool isNew = playerInventory.IsNewCard(cardData);
                slot.Setup(this, cardData, InventoryCardSlotArea.Collection, owned, isNew);
                allCardSlots.Add(slot);
            }
        }

        private void RefreshDeckCards()
        {
            ClearChildren(deckContent);
            deckSlots.Clear();

            if (playerInventory == null || cardSlotPrefab == null || deckContent == null)
                return;

            IReadOnlyList<CardData> deckCards = playerInventory.DeckCards;
            int slotCount = Mathf.Max(playerInventory.MaxDeckCardCount, deckCards.Count);

            for (int i = 0; i < slotCount; i++)
            {
                CardData cardData = i < deckCards.Count ? deckCards[i] : null;
                InventoryCardSlot slot = Instantiate(cardSlotPrefab, deckContent);
                slot.Setup(this, cardData, InventoryCardSlotArea.Deck, cardData != null, false, i);
                deckSlots.Add(slot);
            }
        }

        private List<CardData> GetSortedAllCards()
        {
            List<CardData> ownedNotInDeckCards = new List<CardData>();
            List<CardData> ownedInDeckCards = new List<CardData>();
            List<CardData> unownedCards = new List<CardData>();

            foreach (CardData cardData in GetAllCards())
            {
                if (cardData == null)
                    continue;

                bool owned = playerInventory.HasCard(cardData);

                if (!owned)
                {
                    unownedCards.Add(cardData);
                    continue;
                }

                if (playerInventory.IsInDeck(cardData))
                    ownedInDeckCards.Add(cardData);
                else
                    ownedNotInDeckCards.Add(cardData);
            }

            ownedNotInDeckCards.AddRange(ownedInDeckCards);
            ownedNotInDeckCards.AddRange(unownedCards);
            return ownedNotInDeckCards;
        }

        private IEnumerable<CardData> GetAllCards()
        {
            if (cardDatabase == null)
                yield break;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            MethodInfo getAllCardsMethod = cardDatabase.GetType().GetMethod("GetAllCards", flags);
            if (getAllCardsMethod != null && getAllCardsMethod.Invoke(cardDatabase, null) is IEnumerable<CardData> methodCards)
            {
                foreach (CardData cardData in methodCards)
                    yield return cardData;

                yield break;
            }

            PropertyInfo allCardsProperty = cardDatabase.GetType().GetProperty("AllCards", flags);
            if (allCardsProperty != null && allCardsProperty.GetValue(cardDatabase) is IEnumerable<CardData> propertyCards)
            {
                foreach (CardData cardData in propertyCards)
                    yield return cardData;

                yield break;
            }

            FieldInfo allCardsField = cardDatabase.GetType().GetField("allCards", flags);
            if (allCardsField != null && allCardsField.GetValue(cardDatabase) is IEnumerable<CardData> fieldCards)
            {
                foreach (CardData cardData in fieldCards)
                    yield return cardData;
            }
        }

        private void RefreshDebugCounts()
        {
            allCardCount = GetAllCardsCount();
            ownedCardCount = playerInventory != null ? playerInventory.OwnedCards.Count : 0;
            deckCardCount = playerInventory != null ? playerInventory.DeckCards.Count : 0;
            maxDeckCardCount = playerInventory != null ? playerInventory.MaxDeckCardCount : 0;
        }

        private void RefreshCountText()
        {
            RefreshDebugCounts();

            if (allCardCountText != null)
                allCardCountText.text = $"{ownedCardCount} / {allCardCount}";

            if (deckCardCountText != null)
            {
                deckCardCountText.text = $"{deckCardCount} / {maxDeckCardCount}";
                deckCardCountText.color = deckCardCount < 5 ? Color.red : Color.white;
            }

            RefreshMinimumDeckWarning();
        }

        private void RefreshMinimumDeckWarning()
        {
            RefreshDebugCounts();

            if (deckCardCount < MIN_DECK_CARD_COUNT)
            {
                ShowMinimumDeckWarning(false);
                return;
            }

            FadeOutMinimumDeckWarningIfNeeded();
        }

        private void ShowMinimumDeckWarning(bool forceRestart)
        {
            if (guideMessageText == null)
                return;

            StopGuideMessageRoutine();

            guideMessageText.text = MINIMUM_DECK_WARNING_MESSAGE;
            guideMessageText.gameObject.SetActive(true);

            if (guideMessageGroup != null)
                guideMessageGroup.alpha = 1.0f;

            isMinimumDeckWarningVisible = true;
        }

        private void FadeOutMinimumDeckWarningIfNeeded()
        {
            if (!isMinimumDeckWarningVisible)
                return;

            isMinimumDeckWarningVisible = false;
            StopGuideMessageRoutine();
            guideMessageRoutine = StartCoroutine(FadeOutGuideMessageRoutine());
        }

        private void ShakeGuideMessage()
        {
            if (guideMessageRect == null)
                return;

            if (guideMessageShakeRoutine != null)
                StopCoroutine(guideMessageShakeRoutine);

            guideMessageShakeRoutine = StartCoroutine(GuideMessageShakeRoutine());
        }

        private IEnumerator GuideMessageShakeRoutine()
        {
            float elapsedTime = 0.0f;
            float duration = Mathf.Max(0.01f, blockedCloseShakeDuration);
            float distance = Mathf.Max(0.0f, blockedCloseShakeDistance);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(elapsedTime / duration);
                float strength = 1.0f - progress;
                float offsetX = Mathf.Sin(progress * Mathf.PI * 8.0f) * distance * strength;

                guideMessageRect.anchoredPosition = guideMessageDefaultPosition + new Vector2(offsetX, 0.0f);
                yield return null;
            }

            guideMessageRect.anchoredPosition = guideMessageDefaultPosition;
            guideMessageShakeRoutine = null;
        }
        private void StopGuideMessageRoutine()
        {
            if (guideMessageRoutine == null)
                return;

            StopCoroutine(guideMessageRoutine);
            guideMessageRoutine = null;
        }

        private int GetAllCardsCount()
        {
            int count = 0;

            foreach (CardData cardData in GetAllCards())
                count++;

            return count;
        }
        
        private void RefreshButtonState()
        {
            if (deckEditActionText == null) return;

            canUseDeckEdit = false;
            string actionText = string.Empty;

            if (!canEditDeck)
            {
                actionText = selectedCollectionSlot != null || selectedDeckSlot != null ? "조회 전용" : string.Empty;
            }
            else if (selectedCollectionSlot != null)
            {
                actionText = "추가 >>";
                canUseDeckEdit = !playerInventory.IsDeckFull();
            }
            else if (selectedDeckSlot != null && selectedDeckSlot.CardData != null)
            {
                actionText = "<< 제거";
                canUseDeckEdit = true;
            }

            deckEditActionText.text = actionText;
            deckEditActionText.color = canUseDeckEdit ? actionActiveColor : actionInactiveColor;
        }

        private void ClearSelection()
        {
            selectedCollectionSlot = null;
            selectedDeckSlot = null;
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private void ShowGuideMessage(string message)
        {
            if (guideMessageText == null)
                return;

            isMinimumDeckWarningVisible = false;

            StopGuideMessageRoutine();

            guideMessageRoutine = StartCoroutine(ShowGuideMessageRoutine(message));
        }

        private IEnumerator ShowGuideMessageRoutine(string message)
        {
            guideMessageText.text = message;
            guideMessageText.gameObject.SetActive(true);

            if (guideMessageGroup != null)
                guideMessageGroup.alpha = 1.0f;

            yield return new WaitForSecondsRealtime(guideMessageDuration);

            yield return FadeOutGuideMessageRoutine();
        }

        private IEnumerator FadeOutGuideMessageRoutine()
        {
            float startAlpha = guideMessageGroup != null ? guideMessageGroup.alpha : 1.0f;
            float elapsedTime = 0.0f;
            float duration = Mathf.Max(0.01f, guideMessageFadeOutDuration);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                if (guideMessageGroup != null)
                    guideMessageGroup.alpha = Mathf.Lerp(startAlpha, 0.0f, elapsedTime / duration);

                yield return null;
            }

            if (guideMessageGroup != null)
                guideMessageGroup.alpha = 0.0f;

            if (!isMinimumDeckWarningVisible)
                guideMessageText.gameObject.SetActive(false);

            guideMessageRoutine = null;
        }
    }
}
