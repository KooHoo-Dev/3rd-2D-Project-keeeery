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
        [SerializeField, TextArea] private string description;

        [Header("Player Motion")]
        [SerializeField] private string animationTrigger;

        [Header("Effect")]
        [SerializeField] private CardEffect effect;

        public string CardId => cardId;
        public string CardName => cardName;
        public Sprite Icon => icon;
        public CardType CardType => cardType;
        public string Description => description;
        public string AnimationTrigger => animationTrigger;
        public CardEffect Effect => effect;

        public string TypeText
        {
            get
            {
                switch (cardType)
                {
                    case CardType.Attack:
                        return "공격 카드";
                    case CardType.Defend:
                        return "방어 카드";
                    case CardType.Utility:
                        return "특수 카드";
                    default:
                        return "카드";
                }
            }
        }
    }
}
