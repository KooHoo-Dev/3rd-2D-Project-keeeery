using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

        private readonly InventorySelection selection = new();
        private Vector3 actionDefaultScale = Vector3.one;
        private bool canUseDeckEdit;
        private bool suppressInventoryRefresh;
        private DeckConditionView conditionView;

        public CardDatabase CardDatabase => cardDatabase;
        public PlayerInventory PlayerInventory => playerInventory;
        public bool CanEditDeck => canEditDeck;

        private void Awake()
        {
            ResolveReferences();
            ConfigureViews();
            conditionView = new DeckConditionView(
                allCardCountText,
                deckCardCountText,
                deckManaCostText,
                guideMessage);

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
            selection.Clear();

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

            UpdateDebugCounts(cards.Count);
            conditionView?.Refresh(playerInventory, cards.Count);
            RefreshButtonState();
        }

        private void HandleInventoryChanged()
        {
            if (!suppressInventoryRefresh)
                Refresh();
        }

        private List<CardData> GetSortedAllCards()
        {
            return InventoryCardSorter.SortForCollection(
                cardDatabase != null ? cardDatabase.AllCards : null,
                playerInventory);
        }

        public void SelectSlot(InventoryCardSlot slot)
        {
            selection.Select(slot);
            RefreshButtonState();
        }

        public void ToggleSelectedCardDeck()
        {
            if (!canEditDeck)
                return;

            if (selection.CollectionSlot != null)
                TryAddCardToDeck(selection.CollectionSlot.CardData);
            else if (selection.DeckSlot != null && selection.DeckSlot.CardData != null)
                playerInventory?.RemoveFromDeckAt(selection.DeckSlot.DeckIndex);
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

            DeckEditFailure failure = playerInventory.TryAddToDeck(cardData);
            if (failure != DeckEditFailure.None)
                guideMessage?.ShowTemporary(GetDeckEditFailureMessage(failure));
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

            bool wasSelected = selection.CollectionSlot?.CardData == cardData;
            allCardSlotList?.MarkCardAsSeen(this, cardData);

            if (wasSelected)
            {
                InventoryCardSlot restoredSlot = FindCollectionSlot(cardData);
                selection.RestoreCollectionSelection(restoredSlot);
            }
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
                conditionView?.HideWarning(false);
                conditionView?.Refresh(playerInventory, allCardCount);
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
            conditionView?.ShowWarning(playerInventory);
            guideMessage?.Shake();
        }

        private void UpdateDebugCounts(int totalCardCount)
        {
            allCardCount = totalCardCount;
            ownedCardCount = playerInventory != null ? playerInventory.OwnedCards.Count : 0;
            deckCardCount = playerInventory != null ? playerInventory.DeckCards.Count : 0;
            maxDeckCardCount = playerInventory != null ? playerInventory.MaxDeckCardCount : 0;
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
            else if (selection.CollectionSlot != null)
            {
                actionText = "추가 >>";
                canUseDeckEdit = playerInventory != null
                    && playerInventory.CanAddToDeck(selection.CollectionSlot.CardData) == DeckEditFailure.None;
            }
            else if (selection.DeckSlot != null && selection.DeckSlot.CardData != null)
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

        private InventoryCardSlot FindCollectionSlot(CardData cardData)
        {
            if (allCardSlotList == null)
                return null;

            foreach (InventoryCardSlot slot in allCardSlotList.ActiveSlots)
            {
                if (slot != null && slot.CardData == cardData)
                    return slot;
            }

            return null;
        }

        private static string GetDeckEditFailureMessage(DeckEditFailure failure)
        {
            return failure switch
            {
                DeckEditFailure.CardNotOwned => "아직 획득하지 못한 카드는 카드 덱에 추가할 수 없습니다.",
                DeckEditFailure.DeckFull => "카드 덱이 가득 찼습니다.",
                DeckEditFailure.DuplicateCard => "해당 카드가 카드 덱에 이미 존재합니다.",
                DeckEditFailure.ManaCostExceeded => "마력 용량을 초과합니다.",
                _ => "카드를 덱에 추가할 수 없습니다.",
            };
        }
    }
}
