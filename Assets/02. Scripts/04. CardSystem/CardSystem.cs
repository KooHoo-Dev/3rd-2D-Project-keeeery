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
        // 덱 빌딩을 고려해서 플레이어 덱은 Inspector에서 직접 관리하지 않는다.
        // 나중에 SaveData, RunData, DeckBuilder, RewardSystem 같은 외부 시스템이 아래 메서드로 주입한다.
        private readonly List<CardData> ownedCards = new();
        private readonly List<CardData> playerDeck = new();
        private readonly List<CardData> drawPile = new();
        private readonly List<CardData> hand = new();
        private readonly List<CardData> discardPile = new();

        private CardGaugeController cardGaugeController;
        private CardHandUI cardHandUI;
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

            if (cards == null)
                return;

            foreach (CardData card in cards)
            {
                if (card != null)
                    ownedCards.Add(card);
            }
        }

        public void SetPlayerDeck(IEnumerable<CardData> cards)
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

            InitializeDrawPileFromPlayerDeck();
        }

        public void AddOwnedCard(CardData card)
        {
            if (card == null)
                return;

            ownedCards.Add(card);
        }

        public bool AddCardToPlayerDeck(CardData card)
        {
            if (card == null)
                return false;

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
            if (card == null)
                return false;

            bool removed = playerDeck.Remove(card);

            if (removed)
                InitializeDrawPileFromPlayerDeck();

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
            {
                if (card != null)
                    drawPile.Add(card);
            }

            Shuffle(drawPile);
            Debug.Log($"CardSystem ::: Deck initialized. DrawPile Count = {drawPile.Count}");
        }

        private void TryDrawIfReady()
        {
            if (isDeckInitialized == false)
                return;

            if (isGaugeFull == false)
                return;

            if (cardHandUI == null || cardHandUI.IsDrawing)
                return;

            if (cardHandUI.HasEmptySlot == false)
                return;

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
            if (drawPile.Count <= 0)
                RefillDrawPileFromDiscardPile();

            if (drawPile.Count <= 0)
            {
                Debug.LogWarning("CardSystem ::: Cannot draw. DrawPile and DiscardPile are empty.");
                return null;
            }

            CardData drawnCard = drawPile[0];
            drawPile.RemoveAt(0);

            Debug.Log($"CardSystem ::: Draw Card = {drawnCard.CardName}");
            return drawnCard;
        }

        private void RefillDrawPileFromDiscardPile()
        {
            if (discardPile.Count <= 0)
                return;

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);

            Debug.Log($"CardSystem ::: DrawPile refilled from DiscardPile. DrawPile Count = {drawPile.Count}");
        }

        private void OnCardDrawCompleted()
        {
            if (cardGaugeController != null)
                cardGaugeController.ResetGaugeAndRecharge();
        }

        private void UseCard(int slotIndex)
        {
            if (cardHandUI == null)
                return;

            if (cardHandUI.IsDrawing)
                return;

            CardData usedCard = cardHandUI.GetCard(slotIndex);

            if (usedCard == null)
                return;

            cardHandUI.UseCard(slotIndex);
            hand.Remove(usedCard);
            discardPile.Add(usedCard);

            Debug.Log($"CardSystem ::: Discard Card = {usedCard.CardName}");
            TryDrawIfReady();
        }

        private void Shuffle(List<CardData> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (cards[i], cards[randomIndex]) = (cards[randomIndex], cards[i]);
            }
        }
    }
}
