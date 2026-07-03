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
        private readonly List<CardData> ownedCards = new();
        private readonly List<CardData> playerDeck = new();
        private readonly List<CardData> drawPile = new();
        private readonly List<CardData> hand = new();
        private readonly List<CardData> discardPile = new();

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
        [SerializeField] private AnnounceMessage announceMessage;

        private CardGaugeController cardGaugeController;
        private CardHandUI cardHandUI;
        private CardData pendingSkillCard;
        private bool isGaugeFull;
        private bool isDeckInitialized;

        public IReadOnlyList<CardData> OwnedCards => ownedCards;
        public IReadOnlyList<CardData> PlayerDeck => playerDeck;
        public IReadOnlyList<CardData> DrawPile => drawPile;
        public IReadOnlyList<CardData> Hand => hand;
        public IReadOnlyList<CardData> DiscardPile => discardPile;

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
            ownedCards.Clear();

            if (cards == null) return;

            foreach (CardData card in cards)
                if (card != null) ownedCards.Add(card);
        }

        public void SetPlayerDeck(IEnumerable<CardData> cards)
        {
            playerDeck.Clear();

            if (cards != null)
            {
                foreach (CardData card in cards)
                    if (card != null) playerDeck.Add(card);
            }

            InitializeDrawPileFromPlayerDeck();
        }

        private void SyncPlayerDeckRuntime(IEnumerable<CardData> cards)
        {
            playerDeck.Clear();

            if (cards != null)
            {
                foreach (CardData card in cards)
                {
                    if (card != null)
                        playerDeck.Add(card);
                }
            }

            isDeckInitialized = playerDeck.Count > 0;

            for (int i = drawPile.Count - 1; i >= 0; i--)
            {
                if (!IsCardInPlayerDeck(drawPile[i]))
                    drawPile.RemoveAt(i);
            }

            for (int i = discardPile.Count - 1; i >= 0; i--)
            {
                if (!IsCardInPlayerDeck(discardPile[i]))
                    discardPile.RemoveAt(i);
            }

            foreach (CardData card in playerDeck)
            {
                if (card == null)
                    continue;

                if (drawPile.Contains(card) || discardPile.Contains(card) || hand.Contains(card))
                    continue;

                drawPile.Add(card);
            }

            Shuffle(drawPile);
        }

        public void AddOwnedCard(CardData card)
        {
            if (card == null) return;

            if (syncFromPlayerInventory && playerInventory != null)
            {
                playerInventory.AddCard(card, false);
                return;
            }

            ownedCards.Add(card);
        }

        public bool AddCardToPlayerDeck(CardData card)
        {
            if (card == null) return false;

            if (syncFromPlayerInventory && playerInventory != null)
                return playerInventory.AddToDeck(card);

            if (ownedCards.Contains(card) == false)
            {
                Debug.LogWarning($"CardSystem ::: Cannot add {card.CardName}. This card is not owned.");
                return false;
            }

            playerDeck.Add(card);
            InitializeDrawPileFromPlayerDeck();
            return true;
        }

        public bool RemoveCardFromPlayerDeck(CardData card)
        {
            if (card == null) return false;

            if (syncFromPlayerInventory && playerInventory != null)
                return playerInventory.RemoveFromDeck(card);

            bool removed = playerDeck.Remove(card);

            if (removed) InitializeDrawPileFromPlayerDeck();

            return removed;
        }

        private void InitializeDrawPileFromPlayerDeck()
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            isGaugeFull = false;
            isDeckInitialized = playerDeck.Count > 0;

            foreach (CardData card in playerDeck)
                if (card != null) drawPile.Add(card);

            Shuffle(drawPile);
        }

        private void TryDrawIfReady()
        {
            if (isDeckInitialized == false) return;

            if (isGaugeFull == false) return;

            if (cardHandUI == null || cardHandUI.IsDrawing) return;

            if (cardHandUI.HasEmptySlot == false) return;

            CardData drawnCard = DrawCardFromDeck();

            if (drawnCard == null)
                return;

            bool drawStarted = cardHandUI.TryDrawToFirstEmptySlot(drawnCard, OnCardDrawCompleted);

            if (drawStarted)
            {
                hand.Add(drawnCard);
                isGaugeFull = false;
            }
        }

        private CardData DrawCardFromDeck()
        {
            while (true)
            {
                if (drawPile.Count <= 0)
                    RefillDrawPileFromDiscardPile();

                if (drawPile.Count <= 0)
                    return null;

                CardData drawnCard = drawPile[0];
                drawPile.RemoveAt(0);

                if (IsCardInPlayerDeck(drawnCard))
                    return drawnCard;
            }
        }

        private void RefillDrawPileFromDiscardPile()
        {
            if (discardPile.Count <= 0) return;

            for (int i = discardPile.Count - 1; i >= 0; i--)
            {
                if (IsCardInPlayerDeck(discardPile[i]) == false)
                    discardPile.RemoveAt(i);
            }

            if (discardPile.Count <= 0) return;

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        private void OnCardDrawCompleted()
        {
            if (cardGaugeController != null)
                cardGaugeController.ResetGaugeAndRecharge();
        }

        private void UseCard(int slotIndex)
        {
            if (cardHandUI == null) return;
            if (cardHandUI.IsDrawing) return;

            CardData usedCard = cardHandUI.GetCard(slotIndex);
            if (usedCard == null) return;

            if (TryPrepareCardUse(usedCard) == false)
                return;

            pendingSkillCard = usedCard;
            PlayCardSkillMotion(usedCard);
            cardHandUI.UseCard(slotIndex);
            hand.Remove(usedCard);

            if (IsCardInPlayerDeck(usedCard))
                discardPile.Add(usedCard);

            TryDrawIfReady();
        }

        private bool TryPrepareCardUse(CardData card)
        {
            if (card == null)
                return false;

            if (RequiresTarget(card) == false)
                return true;

            if (TryFindValidTarget(GetCastPointTransform().position, out _))
                return true;

            ShowAnnounceMessage(noTargetAnnounceMessage);
            return false;
        }

        private bool RequiresTarget(CardData card)
        {
            if (card == null)
                return false;

            if (card.RequiresTarget)
                return true;

            if (card.Effect == null)
                return false;

            return card.Effect.RequiresTarget;
        }

        private Transform GetCastPointTransform()
        {
            if (cardCastPoint != null)
                return cardCastPoint;

            if (playerController != null)
                return playerController.transform;

            return transform;
        }

        private bool TryFindValidTarget(Vector3 origin, out Transform target)
        {
            target = null;

            if (enemyManager == null)
                return false;

            Transform foundTarget = enemyManager.FindBestTarget(origin);

            if (IsValidTarget(foundTarget) == false)
                return false;

            target = foundTarget;
            return true;
        }

        private bool IsValidTarget(Transform target)
        {
            if (target == null)
                return false;

            EnemyController enemyController = target.GetComponentInParent<EnemyController>();

            if (enemyController == null)
                return false;

            return enemyController.IsDead == false;
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

        private void PlayCardSkillMotion(CardData card)
        {
            if (card == null) return;

            if (playerAnimator == null) return;

            playerController?.BeginCardSkill();

            string triggerName = string.IsNullOrEmpty(card.AnimationTrigger)
                ? fallbackCardSkillTrigger
                : card.AnimationTrigger;

            if (string.IsNullOrEmpty(triggerName))
            {
                playerController?.EndCardSkill();
                return;
            }

            playerAnimator.ResetTrigger(triggerName);
            playerAnimator.SetTrigger(triggerName);
        }

        public void ExecutePendingCardSkill()
        {
            if (pendingSkillCard == null) return;

            CardData card = pendingSkillCard;
            pendingSkillCard = null;

            if (card.Effect == null) return;

            Transform castPoint = GetCastPointTransform();
            int facingDirection = playerController != null ? playerController.FacingDirection : 1;
            Transform target = null;

            if (TryResolveTargetForCard(card, castPoint.position, out target) == false)
            {
                ShowAnnounceMessage(noTargetAnnounceMessage);
                playerController?.EndCardSkill();
                return;
            }

            CardEffectContext context = new(card, playerController, castPoint, facingDirection, target);
            card.Effect.Execute(context);
        }

        private bool TryResolveTargetForCard(CardData card, Vector3 origin, out Transform target)
        {
            target = null;

            if (RequiresTarget(card))
                return TryFindValidTarget(origin, out target);

            TryFindValidTarget(origin, out target);
            return true;
        }

        private bool IsCardInPlayerDeck(CardData card)
        {
            return card != null && playerDeck.Contains(card);
        }

        private void Shuffle(List<CardData> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (cards[i], cards[randomIndex]) = (cards[randomIndex], cards[i]);
            }
        }
    }
}
