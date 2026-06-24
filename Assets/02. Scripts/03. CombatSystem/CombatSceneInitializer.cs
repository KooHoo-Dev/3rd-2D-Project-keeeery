using UnityEngine;

namespace Arcanist
{
    public class CombatSceneInitializer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CardSystem cardSystem;

        // 테스트용: 나중에 제거 후 시스템 상 제어
        [Header("Temporary Test Cards")]
        [SerializeField] private CardData[] testOwnedCards;
        [SerializeField] private CardData[] testDeckCards;

        private void Awake()
        {
            if (cardSystem == null)
                cardSystem = FindFirstObjectByType<CardSystem>();
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

            cardSystem.SetOwnedCards(testOwnedCards);
            cardSystem.SetPlayerDeck(testDeckCards);

            Debug.Log($"CombatSceneInitializer ::: CardSystem initialized. Owned = {testOwnedCards.Length}, Deck = {testDeckCards.Length}");
        }
    }
}
