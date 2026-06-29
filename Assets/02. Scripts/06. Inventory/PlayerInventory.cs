using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Cards")]
        [SerializeField] private List<CardData> ownedCards = new List<CardData>();
        [SerializeField] private List<CardData> deckCards = new List<CardData>();

        [Header("Deck Rules")]
        [SerializeField] private int maxDeckCardCount = 20;
        [SerializeField] private bool allowDuplicateCards;

        private readonly HashSet<string> newCardIds = new HashSet<string>();

        public event Action OnInventoryChanged;

        public IReadOnlyList<CardData> OwnedCards => ownedCards;
        public IReadOnlyList<CardData> DeckCards => deckCards;
        public int MaxDeckCardCount => maxDeckCardCount;

        public bool HasCard(CardData cardData)
        {
            return cardData != null && ownedCards.Contains(cardData);
        }

        public bool IsInDeck(CardData cardData)
        {
            return cardData != null && deckCards.Contains(cardData);
        }

        public bool IsDeckFull()
        {
            return deckCards.Count >= maxDeckCardCount;
        }

        public bool AddCard(CardData cardData, bool addToDeck = true)
        {
            if (cardData == null || ownedCards.Contains(cardData))
                return false;

            ownedCards.Add(cardData);
            newCardIds.Add(GetCardKey(cardData));

            if (addToDeck)
                AddToDeck(cardData);
            else
                OnInventoryChanged?.Invoke();

            return true;
        }

        public bool AddToDeck(CardData cardData)
        {
            if (cardData == null || !HasCard(cardData) || IsDeckFull())
                return false;

            if (!allowDuplicateCards && deckCards.Contains(cardData))
                return false;

            deckCards.Add(cardData);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveFromDeck(CardData cardData)
        {
            if (cardData == null)
                return false;

            bool removed = deckCards.Remove(cardData);

            if (removed)
                OnInventoryChanged?.Invoke();

            return removed;
        }

        public bool RemoveFromDeckAt(int index)
        {
            if (index < 0 || index >= deckCards.Count)
                return false;

            deckCards.RemoveAt(index);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool IsNewCard(CardData cardData)
        {
            return cardData != null && newCardIds.Contains(GetCardKey(cardData));
        }

        public void MarkCardAsSeen(CardData cardData)
        {
            if (cardData == null)
                return;

            if (newCardIds.Remove(GetCardKey(cardData)))
                OnInventoryChanged?.Invoke();
        }

        private string GetCardKey(CardData cardData)
        {
            return cardData.name;
        }
    }
}
