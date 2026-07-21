using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class CardDatabase : MonoBehaviour
    {
        private static CardDatabase instance;

        public static CardDatabase Instance => instance;

        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("All Cards")]
        [SerializeField] private List<CardData> allCards = new();

        private Dictionary<string, CardData> cardLookup;

        public IReadOnlyList<CardData> AllCards => allCards;

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(transform.root.gameObject);

            BuildLookup();
        }

        private void BuildLookup()
        {
            cardLookup = new Dictionary<string, CardData>();

            foreach (CardData card in allCards)
            {
                if (card == null)
                    continue;

                if (string.IsNullOrEmpty(card.CardId))
                    continue;

                if (cardLookup.ContainsKey(card.CardId))
                {
                    Debug.LogWarning($"Duplicate CardId : {card.CardId}");
                    continue;
                }

                cardLookup.Add(card.CardId, card);
            }
        }

        public CardData GetCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;

            if (cardLookup == null)
                BuildLookup();

            cardLookup.TryGetValue(cardId, out CardData card);
            return card;
        }

    }
}
