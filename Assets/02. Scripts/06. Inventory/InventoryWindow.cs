using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Arcanist
{
    public class InventoryWindow : MonoBehaviour
    {
        private const string DECK_CONDITION_WARNING_MESSAGE = "카드 수와 마력 용량 조건을 만족해야 합니다.";

        [Header("Debug References")]
        [SerializeField] private CardDatabase cardDatabase;
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Debug Counts")]
        [SerializeField] private int allCardCount;
        [SerializeField] private int ownedCardCount;
        [SerializeField] private int deckCardCount;
        [SerializeField] private int maxDeckCardCount;

        [Header("Slot Views")]
        [SerializeField] private InventorySlotList allCardSlotList;
        [SerializeField] private InventorySlotList deckSlotList;

        [Header("Legacy Slot References")]
        [Tooltip("Slot Views가 비어 있을 때 자동 구성에 사용합니다.")]
        [SerializeField] private InventoryCardSlot cardSlotPrefab;
        [SerializeField] private Transform allCardContent;
        [SerializeField] private Transform deckContent;

        [Header("Deck Edit Action")]
        [SerializeField] private TMP_Text deckEditActionText;
        [SerializeField] private Color actionActiveColor = Color.white;
        [SerializeField] private Color actionInactiveColor = new(1.0f, 1.0f, 1.0f, 0.35f);
        [SerializeField] private Color actionPressedColor = new(0.55f, 0.55f, 0.55f, 1.0f);
        [SerializeField] private float actionHoverScale = 1.08f;

        [Header("Count Text")]
        [SerializeField] private TMP_Text allCardCountText;
        [SerializeField] private TMP_Text deckCardCountText;
        [SerializeField] private TMP_Text deckManaCostText;

        [Header("Guide View")]
        [SerializeField] private InventoryGuideMessage guideMessage;

        [Header("Legacy Guide References")]
        [Tooltip("Guide View가 비어 있을 때 자동 구성에 사용합니다.")]
        [SerializeField] private TMP_Text guideMessageText;
        [SerializeField] private float guideMessageDuration = 1.2f;
        [SerializeField] private float guideMessageFadeOutDuration = 0.3f;
        [SerializeField] private float blockedCloseShakeDistance = 8.0f;
        [SerializeField] private float blockedCloseShakeDuration = 0.18f;

        [Header("Rules")]
        [SerializeField] private bool canEditDeck = true;

        private InventoryCardSlot selectedCollectionSlot;
        private InventoryCardSlot selectedDeckSlot;
        private Vector3 actionDefaultScale = Vector3.one;
        private bool canUseDeckEdit;
        private bool isDeckConditionWarningVisible;
        private bool suppressInventoryRefresh;

        public CardDatabase CardDatabase => cardDatabase;
        public PlayerInventory PlayerInventory => playerInventory;
        public bool CanEditDeck => canEditDeck;

        private void Awake()
        {
            ResolveReferences();
            ConfigureViews();

            if (deckEditActionText != null)
                actionDefaultScale = deckEditActionText.rectTransform.localScale;
        }

        private void OnEnable()
        {
            ResolveReferences();
            ConfigureViews();

            if (playerInventory != null)
                playerInventory.OnInventoryChanged += HandleInventoryChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (playerInventory != null)
                playerInventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        [ContextMenu("Debug Refresh References")]
        private void DebugRefreshReferences()
        {
            cardDatabase = CardDatabase.Instance != null
                ? CardDatabase.Instance
                : FindFirstObjectByType<CardDatabase>();
            playerInventory = PlayerInventory.Instance != null
                ? PlayerInventory.Instance
                : FindFirstObjectByType<PlayerInventory>();
            Refresh();
        }

        private void ResolveReferences()
        {
            if (cardDatabase == null)
                cardDatabase = CardDatabase.Instance != null
                    ? CardDatabase.Instance
                    : FindFirstObjectByType<CardDatabase>();

            if (playerInventory == null)
                playerInventory = PlayerInventory.Instance != null
                    ? PlayerInventory.Instance
                    : FindFirstObjectByType<PlayerInventory>();
        }

        private void ConfigureViews()
        {
            allCardSlotList = ResolveSlotList(allCardSlotList, allCardContent);
            deckSlotList = ResolveSlotList(deckSlotList, deckContent);

            allCardSlotList?.Configure(cardSlotPrefab, allCardContent);
            deckSlotList?.Configure(cardSlotPrefab, deckContent);

            if (guideMessage == null && guideMessageText != null)
                guideMessage = guideMessageText.GetComponent<InventoryGuideMessage>()
                    ?? guideMessageText.gameObject.AddComponent<InventoryGuideMessage>();

            guideMessage?.Configure(
                guideMessageText,
                guideMessageDuration,
                guideMessageFadeOutDuration,
                blockedCloseShakeDistance,
                blockedCloseShakeDuration);
        }

        private static InventorySlotList ResolveSlotList(InventorySlotList slotList, Transform content)
        {
            if (slotList != null || content == null)
                return slotList;

            return content.GetComponent<InventorySlotList>()
                ?? content.gameObject.AddComponent<InventorySlotList>();
        }

        private void Refresh()
        {
            ResolveReferences();
            ConfigureViews();
            ClearSelection();

            List<CardData> cards = GetSortedAllCards();
            allCardCount = cards.Count;
            allCardSlotList?.RenderCollection(this, cards, playerInventory);

            if (playerInventory != null)
            {
                deckSlotList?.RenderDeck(
                    this,
                    playerInventory.DeckCards,
                    playerInventory.MaxDeckCardCount);
            }
            else
            {
                deckSlotList?.ReleaseAll();
            }

            RefreshCountText();
            RefreshButtonState();
        }

        private void HandleInventoryChanged()
        {
            if (!suppressInventoryRefresh)
                Refresh();
        }

        private List<CardData> GetSortedAllCards()
        {
            List<CardData> result = new();

            if (cardDatabase == null || playerInventory == null)
                return result;

            List<CardData> ownedInDeckCards = new();
            List<CardData> unownedCards = new();

            foreach (CardData card in cardDatabase.AllCards)
            {
                if (card == null)
                    continue;

                if (!playerInventory.HasCard(card))
                    unownedCards.Add(card);
                else if (playerInventory.IsInDeck(card))
                    ownedInDeckCards.Add(card);
                else
                    result.Add(card);
            }

            result.AddRange(ownedInDeckCards);
            result.AddRange(unownedCards);
            return result;
        }

        public void SelectSlot(InventoryCardSlot slot)
        {
            if (slot == null)
                return;

            if (slot.SlotArea == InventoryCardSlotArea.Collection)
                SelectCollectionSlot(slot);
            else
                SelectDeckSlot(slot);

            RefreshButtonState();
        }

        private void SelectCollectionSlot(InventoryCardSlot slot)
        {
            if (!slot.IsOwned)
            {
                ClearSelection();
                return;
            }

            selectedCollectionSlot?.SetSelected(false);
            selectedDeckSlot?.SetSelected(false);
            selectedCollectionSlot = slot;
            selectedDeckSlot = null;
            selectedCollectionSlot.SetSelected(true);
        }

        private void SelectDeckSlot(InventoryCardSlot slot)
        {
            selectedCollectionSlot?.SetSelected(false);
            selectedDeckSlot?.SetSelected(false);
            selectedCollectionSlot = null;
            selectedDeckSlot = slot;
            selectedDeckSlot.SetSelected(true);
        }

        private void ClearSelection()
        {
            selectedCollectionSlot?.SetSelected(false);
            selectedDeckSlot?.SetSelected(false);
            selectedCollectionSlot = null;
            selectedDeckSlot = null;
        }

        public void ToggleSelectedCardDeck()
        {
            if (!canEditDeck)
                return;

            if (selectedCollectionSlot != null)
                TryAddCardToDeck(selectedCollectionSlot.CardData);
            else if (selectedDeckSlot != null && selectedDeckSlot.CardData != null)
                playerInventory?.RemoveFromDeckAt(selectedDeckSlot.DeckIndex);
        }

        public void AddCardToDeck(CardData cardData)
        {
            if (canEditDeck)
                TryAddCardToDeck(cardData);
        }

        private void TryAddCardToDeck(CardData cardData)
        {
            if (playerInventory == null || cardData == null)
                return;

            if (playerInventory.IsInDeck(cardData))
            {
                guideMessage?.ShowTemporary("해당 카드가 카드 덱에 이미 존재합니다.");
                return;
            }

            if (playerInventory.IsDeckFull())
            {
                guideMessage?.ShowTemporary("카드 덱이 가득 찼습니다.");
                return;
            }

            if (playerInventory.WouldExceedDeckManaCost(cardData))
            {
                guideMessage?.ShowTemporary("마력 용량을 초과합니다.");
                return;
            }

            playerInventory.AddToDeck(cardData);
        }

        public void RemoveCardFromDeck(CardData cardData)
        {
            if (canEditDeck && playerInventory != null && cardData != null)
                playerInventory.RemoveFromDeck(cardData);
        }

        public void MarkCardAsSeen(CardData cardData)
        {
            if (playerInventory == null || cardData == null || !playerInventory.IsNewCard(cardData))
                return;

            suppressInventoryRefresh = true;

            try
            {
                playerInventory.MarkCardAsSeen(cardData);
            }
            finally
            {
                suppressInventoryRefresh = false;
            }

            bool wasSelected = selectedCollectionSlot?.CardData == cardData;
            allCardSlotList?.MarkCardAsSeen(this, cardData);
            selectedCollectionSlot?.SetSelected(wasSelected);
        }

        public void ShowUnownedCardMessage()
        {
            guideMessage?.ShowTemporary("아직 획득하지 못한 카드는 카드 덱에 추가할 수 없습니다.");
        }

        public void SetDeckEditMode(bool canEdit)
        {
            canEditDeck = canEdit;

            if (canEditDeck)
            {
                isDeckConditionWarningVisible = false;
                guideMessage?.Hide(false);
                RefreshCountText();
            }

            RefreshButtonState();
        }

        public bool HasMinimumDeckCards()
        {
            return playerInventory != null && playerInventory.HasEnoughDeckCards();
        }

        public bool CanCompleteDeckEdit()
        {
            return playerInventory != null && playerInventory.CanCompleteDeckEdit();
        }

        public void PlayMinimumDeckWarningFeedback()
        {
            ShowDeckConditionWarning();
            guideMessage?.Shake();
        }

        private void RefreshCountText()
        {
            ownedCardCount = playerInventory != null ? playerInventory.OwnedCards.Count : 0;
            deckCardCount = playerInventory != null ? playerInventory.DeckCards.Count : 0;
            maxDeckCardCount = playerInventory != null ? playerInventory.MaxDeckCardCount : 0;

            if (allCardCountText != null)
                allCardCountText.text = $"{ownedCardCount} / {allCardCount}";

            RefreshDeckCountText();
            RefreshManaCostText();
            RefreshDeckConditionWarning();
        }

        private void RefreshDeckCountText()
        {
            if (deckCardCountText == null)
                return;

            deckCardCountText.text = playerInventory == null
                ? "0 / 0"
                : $"카드  현재 {deckCardCount} / 최소 {playerInventory.MinDeckCardCount}";
            deckCardCountText.color = playerInventory != null && playerInventory.HasEnoughDeckCards()
                ? Color.white
                : Color.red;
        }

        private void RefreshManaCostText()
        {
            if (deckManaCostText == null)
                return;

            int manaCost = playerInventory != null ? playerInventory.DeckManaCost : 0;
            deckManaCostText.text = playerInventory == null
                ? "0 / 0"
                : $"마력  {manaCost} / {playerInventory.MinDeckManaCost}~{playerInventory.MaxDeckManaCost}";
            deckManaCostText.color = playerInventory != null && playerInventory.IsDeckManaCostValid()
                ? Color.white
                : Color.red;
        }

        private void RefreshDeckConditionWarning()
        {
            if (playerInventory == null || !playerInventory.CanCompleteDeckEdit())
            {
                ShowDeckConditionWarning();
                return;
            }

            if (isDeckConditionWarningVisible)
            {
                isDeckConditionWarningVisible = false;
                guideMessage?.Hide(true);
            }
        }

        private void ShowDeckConditionWarning()
        {
            guideMessage?.ShowPersistent(GetDeckConditionWarningMessage());
            isDeckConditionWarningVisible = true;
        }

        private string GetDeckConditionWarningMessage()
        {
            if (playerInventory == null)
                return DECK_CONDITION_WARNING_MESSAGE;

            int manaCost = playerInventory.DeckManaCost;

            if (!playerInventory.HasEnoughDeckCards())
                return $"카드 덱에는 최소 {playerInventory.MinDeckCardCount}장의 카드가 필요합니다.";
            if (manaCost < playerInventory.MinDeckManaCost)
                return $"마력 용량이 부족합니다. ({manaCost} / {playerInventory.MinDeckManaCost}~{playerInventory.MaxDeckManaCost})";
            if (manaCost > playerInventory.MaxDeckManaCost)
                return $"마력 용량을 초과했습니다. ({manaCost} / {playerInventory.MinDeckManaCost}~{playerInventory.MaxDeckManaCost})";

            return DECK_CONDITION_WARNING_MESSAGE;
        }

        private void RefreshButtonState()
        {
            if (deckEditActionText == null)
                return;

            canUseDeckEdit = false;
            string actionText = string.Empty;

            if (!canEditDeck)
            {
                guideMessage?.ShowPersistent("현재 지역에서는 카드 덱 편집이 불가능합니다.");
            }
            else if (selectedCollectionSlot != null)
            {
                actionText = "추가 >>";
                canUseDeckEdit = playerInventory != null
                    && !playerInventory.IsDeckFull()
                    && !playerInventory.WouldExceedDeckManaCost(selectedCollectionSlot.CardData);
            }
            else if (selectedDeckSlot != null && selectedDeckSlot.CardData != null)
            {
                actionText = "<< 제거";
                canUseDeckEdit = true;
            }

            deckEditActionText.text = actionText;
            deckEditActionText.color = canUseDeckEdit ? actionActiveColor : actionInactiveColor;
        }

        public void OnDeckEditActionPointerEnter()
        {
            if (deckEditActionText != null && canUseDeckEdit)
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
            if (deckEditActionText != null && canUseDeckEdit)
                deckEditActionText.color = actionPressedColor;
        }

        public void OnDeckEditActionPointerUp()
        {
            RefreshButtonState();
        }
    }
}
