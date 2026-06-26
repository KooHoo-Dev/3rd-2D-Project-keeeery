using UnityEngine;

namespace Arcanist
{
    public enum CardType
    {
        Attack,
        Defend,
        Utility
    }

    [CreateAssetMenu(fileName = "CardData", menuName = "Arcanist/Card")]
    
    public class CardData : ScriptableObject
    {
        [Header("Card Info")]
        [SerializeField] private string cardId;
        [SerializeField] private string cardName;
        [SerializeField] private Sprite icon;
        [SerializeField] private CardType cardType;

        [Header("Player Motion")]
        [SerializeField] private string animationTrigger;

        // [Header("Debug")]
        // [SerializeField] private string debugMessage;

        public string CardId => cardId;
        public string CardName => cardName;
        public Sprite Icon => icon;
        public CardType CardType => cardType;
        public string AnimationTrigger => animationTrigger;
        // public string DebugMessage => debugMessage;
    }
}

