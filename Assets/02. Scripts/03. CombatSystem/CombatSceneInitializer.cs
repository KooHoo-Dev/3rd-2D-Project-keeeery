using UnityEngine;

namespace Arcanist
{
    public class CombatSceneInitializer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CardSystem cardSystem;
        [SerializeField] private PlayerInventory playerInventory;

        // 테스트용: 나중에 제거 후 보상/저장 시스템에서 PlayerInventory를 세팅
        [Header("Temporary Test Cards")]
        [SerializeField] private CardData[] testOwnedCards;
        [SerializeField] private CardData[] testDeckCards;
        [SerializeField] private bool initializeTestInventory = true;

        private void Awake()
        {
            if (cardSystem == null)
                cardSystem = FindFirstObjectByType<CardSystem>();

            if (playerInventory == null)
                playerInventory = FindFirstObjectByType<PlayerInventory>();
        }

        private void Start()
        {
            InitializeCardSystem();
        }

        private void InitializeCardSystem()
        {
            if (cardSystem == null)
            {
                Debug.LogWarning("CombatSceneInitializer ::: CardSystem is not assigned.");
                return;
            }

            InitializeTestInventory();

            CardData[] ownedCards = GetOwnedCardsForCombat();
            CardData[] deckCards = GetDeckCardsForCombat();

            cardSystem.SetOwnedCards(ownedCards);
            cardSystem.SetPlayerDeck(deckCards);

            Debug.Log($"CombatSceneInitializer ::: CardSystem initialized. Owned = {ownedCards.Length}, Deck = {deckCards.Length}");
        }

        private void InitializeTestInventory()
        {
            if (!initializeTestInventory || playerInventory == null)
                return;

            foreach (CardData cardData in testOwnedCards)
                playerInventory.AddCard(cardData, false);

            foreach (CardData cardData in testDeckCards)
            {
                if (!playerInventory.HasCard(cardData))
                    playerInventory.AddCard(cardData, false);

                playerInventory.AddToDeck(cardData);
            }
        }

        private CardData[] GetOwnedCardsForCombat()
        {
            if (playerInventory == null)
                return testOwnedCards;

            CardData[] cards = new CardData[playerInventory.OwnedCards.Count];

            for (int i = 0; i < cards.Length; i++)
                cards[i] = playerInventory.OwnedCards[i];

            return cards;
        }

        private CardData[] GetDeckCardsForCombat()
        {
            if (playerInventory == null)
                return testDeckCards;

            CardData[] cards = new CardData[playerInventory.DeckCards.Count];

            for (int i = 0; i < cards.Length; i++)
                cards[i] = playerInventory.DeckCards[i];

            return cards;
        }
    }
}
