using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    /// <summary>
    /// Owns the mutable card collections used during combat.
    /// Unity input, animation and UI orchestration stay in CardSystem.
    /// </summary>
    internal sealed class CardRuntimeDeck
    {
        private readonly List<CardData> ownedCards = new();
        private readonly List<CardData> playerDeck = new();
        private readonly List<CardData> drawPile = new();
        private readonly List<CardData> hand = new();
        private readonly List<CardData> discardPile = new();

        public IReadOnlyList<CardData> OwnedCards => ownedCards;
        public IReadOnlyList<CardData> PlayerDeck => playerDeck;
        public IReadOnlyList<CardData> DrawPile => drawPile;
        public IReadOnlyList<CardData> Hand => hand;
        public IReadOnlyList<CardData> DiscardPile => discardPile;
        public bool IsInitialized => playerDeck.Count > 0;

        public void SetOwnedCards(IEnumerable<CardData> cards)
        {
            ReplaceCards(ownedCards, cards);
        }

        public void SetPlayerDeck(IEnumerable<CardData> cards)
        {
            ReplaceCards(playerDeck, cards);
            ResetForNewDeck();
        }

        public void SyncPlayerDeck(IEnumerable<CardData> cards)
        {
            ReplaceCards(playerDeck, cards);
            RemoveCardsNoLongerInDeck(drawPile);
            RemoveCardsNoLongerInDeck(discardPile);

            foreach (CardData card in playerDeck)
            {
                if (drawPile.Contains(card) || discardPile.Contains(card) || hand.Contains(card))
                    continue;

                drawPile.Add(card);
            }

            Shuffle(drawPile);
        }

        public void ResetForRespawn()
        {
            hand.Clear();
            drawPile.Clear();
            discardPile.Clear();
            drawPile.AddRange(playerDeck);
            Shuffle(drawPile);
        }

        public void AddOwnedCard(CardData card)
        {
            if (card != null)
                ownedCards.Add(card);
        }

        public bool AddCardToPlayerDeck(CardData card)
        {
            if (card == null || ownedCards.Contains(card) == false)
                return false;

            playerDeck.Add(card);
            ResetForNewDeck();
            return true;
        }

        public bool RemoveCardFromPlayerDeck(CardData card)
        {
            if (card == null || playerDeck.Remove(card) == false)
                return false;

            ResetForNewDeck();
            return true;
        }

        public CardData Draw()
        {
            while (true)
            {
                if (drawPile.Count == 0)
                    RefillFromDiscardPile();

                if (drawPile.Count == 0)
                    return null;

                CardData card = drawPile[0];
                drawPile.RemoveAt(0);

                if (Contains(card))
                    return card;
            }
        }

        public void AddToHand(CardData card)
        {
            if (card != null)
                hand.Add(card);
        }

        public void RecordUse(CardData card)
        {
            if (card == null)
                return;

            hand.Remove(card);

            if (Contains(card))
                discardPile.Add(card);
        }

        public bool Contains(CardData card)
        {
            return card != null && playerDeck.Contains(card);
        }

        private void ResetForNewDeck()
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            drawPile.AddRange(playerDeck);
            Shuffle(drawPile);
        }

        private void RefillFromDiscardPile()
        {
            RemoveCardsNoLongerInDeck(discardPile);

            if (discardPile.Count == 0)
                return;

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        private void RemoveCardsNoLongerInDeck(List<CardData> cards)
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                if (Contains(cards[i]) == false)
                    cards.RemoveAt(i);
            }
        }

        private static void ReplaceCards(List<CardData> destination, IEnumerable<CardData> source)
        {
            destination.Clear();

            if (source == null)
                return;

            foreach (CardData card in source)
            {
                if (card != null)
                    destination.Add(card);
            }
        }

        private static void Shuffle(List<CardData> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (cards[i], cards[randomIndex]) = (cards[randomIndex], cards[i]);
            }
        }
    }
}
