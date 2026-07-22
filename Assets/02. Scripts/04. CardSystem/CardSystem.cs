using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Arcanist
{
    [RequireComponent(typeof(CardGaugeController))]
    [RequireComponent(typeof(CardHandUI))]
    public class CardSystem : MonoBehaviour
    {
        private readonly CardRuntimeDeck runtimeDeck = new();

        [Header("Runtime Inventory")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private bool syncFromPlayerInventory = true;

        [Header("Skill Motion")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string fallbackCardSkillTrigger = "CardSkill";

        [Header("Skill Effect")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform cardCastPoint;

        [Header("Targeting")]
        [SerializeField] private EnemyManager enemyManager;

        [Header("Announce Message")]
        [SerializeField] private string noTargetAnnounceMessage = "공격할 대상이 없습니다.";
        [SerializeField] private string fullHpAnnounceMessage = "체력이 이미 가득 찼습니다.";
        [SerializeField] private AnnounceMessage announceMessage;

        [Header("Recent Card UI")]
        [SerializeField] private RecentCardUIController recentCardUIController;

        private CardGaugeController cardGaugeController;
        private CardHandUI cardHandUI;
        private CardTargetResolver targetResolver;
        private CardUseValidator useValidator;
        private CardSkillExecutor skillExecutor;
        private bool isGaugeFull;

        public IReadOnlyList<CardData> OwnedCards => runtimeDeck.OwnedCards;
        public IReadOnlyList<CardData> PlayerDeck => runtimeDeck.PlayerDeck;
        public IReadOnlyList<CardData> DrawPile => runtimeDeck.DrawPile;
        public IReadOnlyList<CardData> Hand => runtimeDeck.Hand;
        public IReadOnlyList<CardData> DiscardPile => runtimeDeck.DiscardPile;

        private void Awake()
        {
            cardGaugeController = GetComponent<CardGaugeController>();
            cardHandUI = GetComponent<CardHandUI>();

            if (playerInventory == null) 
                playerInventory = PlayerInventory.Instance != null 
                ? PlayerInventory.Instance
                : FindFirstObjectByType<PlayerInventory>();

            if (playerController == null && playerAnimator != null)
                playerController = playerAnimator.GetComponentInParent<PlayerController>();

            if (enemyManager == null)
                enemyManager = FindFirstObjectByType<EnemyManager>();

            Player player = playerController != null
                ? playerController.GetComponent<Player>()
                : GetComponent<Player>();
            targetResolver = new CardTargetResolver(enemyManager);
            useValidator = new CardUseValidator(player, targetResolver);
            skillExecutor = new CardSkillExecutor(
                playerAnimator,
                playerController,
                cardCastPoint,
                playerController != null ? playerController.transform : transform,
                fallbackCardSkillTrigger,
                targetResolver);
        }

        private void OnEnable()
        {
            if (playerInventory == null)
                playerInventory = PlayerInventory.Instance != null ? PlayerInventory.Instance : FindFirstObjectByType<PlayerInventory>();

            if (syncFromPlayerInventory && playerInventory != null)
            {
                playerInventory.OnInventoryChanged += SyncFromPlayerInventory;
                SyncFromPlayerInventory();
            }
        }

        private void OnDisable()
        {
            if (playerInventory != null)
                playerInventory.OnInventoryChanged -= SyncFromPlayerInventory;
        }

        private void SyncFromPlayerInventory()
        {
            if (!syncFromPlayerInventory || playerInventory == null)
                return;

            SetOwnedCards(playerInventory.OwnedCards);
            SyncPlayerDeckRuntime(playerInventory.DeckCards);
        }

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Q))
                UseCard(0);

            if (SimpleInput.GetKeyDown(Key.W))
                UseCard(1);

            if (SimpleInput.GetKeyDown(Key.E))
                UseCard(2);

            TryDrawIfReady();
        }

        public void OnGaugeFull()
        {
            isGaugeFull = true;
            TryDrawIfReady();
        }

        public void SetOwnedCards(IEnumerable<CardData> cards)
        {
            runtimeDeck.SetOwnedCards(cards);
        }

        public void SetPlayerDeck(IEnumerable<CardData> cards)
        {
            runtimeDeck.SetPlayerDeck(cards);
            isGaugeFull = false;
        }

        private void SyncPlayerDeckRuntime(IEnumerable<CardData> cards)
        {
            runtimeDeck.SyncPlayerDeck(cards);
        }

        public void ResetRuntimeCardsForRespawn()
        {
            skillExecutor.Reset();
            isGaugeFull = false;

            runtimeDeck.ResetForRespawn();

            cardHandUI?.ClearAllSlots();
            cardGaugeController?.ResetGaugeAndRecharge();
            recentCardUIController?.Clear();
        }

        public void AddOwnedCard(CardData card)
        {
            if (card == null) return;

            if (syncFromPlayerInventory && playerInventory != null)
            {
                playerInventory.AddCard(card, false);
                return;
            }

            runtimeDeck.AddOwnedCard(card);
        }

        public bool AddCardToPlayerDeck(CardData card)
        {
            if (card == null) return false;

            if (syncFromPlayerInventory && playerInventory != null)
                return playerInventory.AddToDeck(card);

            if (runtimeDeck.AddCardToPlayerDeck(card) == false)
            {
                Debug.LogWarning($"CardSystem ::: Cannot add {card.CardName}. This card is not owned.");
                return false;
            }

            isGaugeFull = false;
            return true;
        }

        public bool RemoveCardFromPlayerDeck(CardData card)
        {
            if (card == null) return false;

            if (syncFromPlayerInventory && playerInventory != null)
                return playerInventory.RemoveFromDeck(card);

            bool removed = runtimeDeck.RemoveCardFromPlayerDeck(card);
            if (removed)
                isGaugeFull = false;
            return removed;
        }

        private void TryDrawIfReady()
        {
            if (runtimeDeck.IsInitialized == false) return;

            if (isGaugeFull == false) return;

            if (cardHandUI == null || cardHandUI.IsDrawing) return;

            if (cardHandUI.HasEmptySlot == false) return;

            CardData drawnCard = runtimeDeck.Draw();

            if (drawnCard == null)
                return;

            cardGaugeController?.BeginNextGaugeCycle();
            bool drawStarted = cardHandUI.TryDrawToFirstEmptySlot(drawnCard);

            if (drawStarted)
            {
                runtimeDeck.AddToHand(drawnCard);
                isGaugeFull = false;
            }
        }

        private void UseCard(int slotIndex)
        {
            if (cardHandUI == null) return;
            if (cardHandUI.IsDrawing) return;

            CardData usedCard = cardHandUI.GetCard(slotIndex);
            if (usedCard == null) return;

            if (TryPrepareCardUse(usedCard) == false)
                return;

            bool waitsForAnimationEvent = skillExecutor.Begin(usedCard);
            if (waitsForAnimationEvent == false)
                ExecutePendingCardSkill();

            cardHandUI.UseCard(slotIndex);
            runtimeDeck.RecordUse(usedCard);

            TryDrawIfReady();
        }

        private bool TryPrepareCardUse(CardData card)
        {
            Vector3 castOrigin = skillExecutor.CastPoint != null
                ? skillExecutor.CastPoint.position
                : transform.position;
            CardUseFailure failure = useValidator.Validate(card, castOrigin);

            switch (failure)
            {
                case CardUseFailure.None:
                    return true;
                case CardUseFailure.FullHealth:
                    ShowAnnounceMessage(fullHpAnnounceMessage);
                    return false;
                case CardUseFailure.MissingTarget:
                    ShowAnnounceMessage(noTargetAnnounceMessage);
                    return false;
                default:
                    return false;
            }
        }

        private void ShowAnnounceMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (announceMessage != null)
            {
                announceMessage.Show(message, false);
                return;
            }

            Debug.Log(message);
        }

        public void ExecutePendingCardSkill()
        {
            CardSkillExecutionResult result = skillExecutor.ExecutePending();

            if (result.Status == CardSkillExecutionStatus.MissingTarget)
            {
                ShowAnnounceMessage(noTargetAnnounceMessage);
                return;
            }

            if (result.Status == CardSkillExecutionStatus.Executed)
                recentCardUIController?.ShowUsedCard(result.Card);
        }

    }
}
