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
        [Header("Data")]
        [SerializeField] private CardDatabase cardDatabase;
        [SerializeField] private PlayerInventory playerInventory;

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

        [Header("Guide Message")]
        [SerializeField] private TMP_Text guideMessageText;
        [SerializeField] private float guideMessageDuration = 1.2f;
        [SerializeField] private float guideMessageFadeOutDuration = 0.3f;

        [Header("Rules")]
        [SerializeField] private bool canEditDeck = true;

        private readonly List<InventoryCardSlot> allCardSlots = new List<InventoryCardSlot>();
        private readonly List<InventoryCardSlot> deckSlots = new List<InventoryCardSlot>();
        private InventoryCardSlot selectedCollectionSlot;
        private InventoryCardSlot selectedDeckSlot;
        private Vector3 actionDefaultScale = Vector3.one;
        private bool canUseDeckEditAction;
        private CanvasGroup guideMessageGroup;
        private Coroutine guideMessageRoutine;
        private void Awake()
        {
            if (deckEditActionText != null)
                actionDefaultScale = deckEditActionText.rectTransform.localScale;

            if (guideMessageText != null)
            {
                guideMessageGroup = guideMessageText.GetComponent<CanvasGroup>();

                if (guideMessageGroup == null)
                    guideMessageGroup = guideMessageText.gameObject.AddComponent<CanvasGroup>();

                guideMessageGroup.alpha = 0.0f;
                guideMessageText.gameObject.SetActive(false);
            }
        }


        public PlayerInventory PlayerInventory => playerInventory;
        public bool CanEditDeck => canEditDeck;

        private void OnEnable()
        {
            if (playerInventory != null)
                playerInventory.OnInventoryChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (playerInventory != null)
                playerInventory.OnInventoryChanged -= Refresh;
        }

        public void SetDeckEditMode(bool canEdit)
        {
            canEditDeck = canEdit;
            RefreshButtonState();
        }

        public void Refresh()
        {
            ClearSelection();
            RefreshAllCards();
            RefreshDeckCards();
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
            ShowGuideMessage("아직 획득하지 못한 카드는 덱에 추가할 수 없습니다.");
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
            if (deckEditActionText == null || !canUseDeckEditAction)
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
            if (deckEditActionText == null || !canUseDeckEditAction)
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
                ShowGuideMessage("해당 카드가 덱에 이미 존재합니다.");
                return;
            }

            if (playerInventory.IsDeckFull())
            {
                ShowGuideMessage("덱이 가득 찼습니다.");
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

            foreach (CardData cardData in GetAllCards())
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

        private void RefreshButtonState()
        {
            if (deckEditActionText == null)
                return;

            canUseDeckEditAction = false;
            string actionText = string.Empty;

            if (!canEditDeck)
            {
                actionText = selectedCollectionSlot != null || selectedDeckSlot != null ? "조회 전용" : string.Empty;
            }
            else if (selectedCollectionSlot != null)
            {
                actionText = playerInventory.IsDeckFull() ? "덱 가득 참" : "추가 >>";
                canUseDeckEditAction = !playerInventory.IsDeckFull();
            }
            else if (selectedDeckSlot != null && selectedDeckSlot.CardData != null)
            {
                actionText = "<< 제거";
                canUseDeckEditAction = true;
            }

            deckEditActionText.text = actionText;
            deckEditActionText.color = canUseDeckEditAction ? actionActiveColor : actionInactiveColor;
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

            if (guideMessageRoutine != null)
                StopCoroutine(guideMessageRoutine);

            guideMessageRoutine = StartCoroutine(ShowGuideMessageRoutine(message));
        }

        private IEnumerator ShowGuideMessageRoutine(string message)
        {
            guideMessageText.text = message;
            guideMessageText.gameObject.SetActive(true);

            if (guideMessageGroup != null)
                guideMessageGroup.alpha = 1.0f;

            yield return new WaitForSecondsRealtime(guideMessageDuration);

            float elapsedTime = 0.0f;
            float duration = Mathf.Max(0.01f, guideMessageFadeOutDuration);

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                if (guideMessageGroup != null)
                    guideMessageGroup.alpha = Mathf.Lerp(1.0f, 0.0f, elapsedTime / duration);

                yield return null;
            }

            if (guideMessageGroup != null)
                guideMessageGroup.alpha = 0.0f;

            guideMessageText.gameObject.SetActive(false);
            guideMessageRoutine = null;
        }
    }
}