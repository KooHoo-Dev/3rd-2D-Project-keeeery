using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class PlayerInventory : MonoBehaviour
    {
        private static PlayerInventory instance;

        public static PlayerInventory Instance => instance;

        [Header("Persistence")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Initial Cards")]
        [SerializeField] private List<CardData> ownedCards = new List<CardData>();
        [SerializeField] private List<CardData> deckCards = new List<CardData>();

        [Header("Deck Rules")]
        [SerializeField] private int minDeckCardCount = 6;
        [SerializeField] private int minDeckManaCost = 10;
        [SerializeField] private int maxDeckManaCost = 11;
        [SerializeField] private int maxDeckCardCount = 99;
        [SerializeField] private bool allowDuplicateCards;

        private readonly HashSet<string> newCardIds = new HashSet<string>();

        public event Action OnInventoryChanged;

        public IReadOnlyList<CardData> OwnedCards => ownedCards;
        public IReadOnlyList<CardData> DeckCards => deckCards;
        public int MinDeckCardCount => minDeckCardCount;
        public int MinDeckManaCost => minDeckManaCost;
        public int MaxDeckManaCost => maxDeckManaCost;
        public int MaxDeckCardCount => maxDeckCardCount;
        public int DeckCardCount => deckCards.Count;
        public int DeckManaCost => GetDeckManaCost();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (dontDestroyOnLoad) DontDestroyOnLoad(transform.root.gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

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

        public bool HasEnoughDeckCards()
        {
            return deckCards.Count >= minDeckCardCount;
        }

        public bool IsDeckManaCostValid()
        {
            int manaCost = GetDeckManaCost();
            return manaCost >= minDeckManaCost && manaCost <= maxDeckManaCost;
        }

        public bool CanCompleteDeckEdit()
        {
            return HasEnoughDeckCards() && IsDeckManaCostValid();
        }

        public bool WouldExceedDeckManaCost(CardData cardData)
        {
            if (cardData == null)
                return false;

            return GetDeckManaCost() + cardData.Cost > maxDeckManaCost;
        }

        private int GetDeckManaCost()
        {
            int totalCost = 0;

            foreach (var cardData in deckCards)
            {
                if (cardData == null) continue;

                totalCost += cardData.Cost;
            }

            return totalCost;
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
            if (cardData == null || !HasCard(cardData) || IsDeckFull() || WouldExceedDeckManaCost(cardData))
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

    [Serializable]
    public class CardInventorySaveData
    {
        public List<string> ownedCardIds = new List<string>();
        public List<string> deckCardIds = new List<string>();
        public List<string> newCardIds = new List<string>();
    }
}
