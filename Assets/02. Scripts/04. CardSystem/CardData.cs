using UnityEngine;

namespace Arcanist
{
    public enum CardType
    {
        Attack,
        Defend,
        Utility
    }

    public enum CardGrade
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "CardData", menuName = "Arcanist/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Card Info")]
        [SerializeField] private string cardId;
        [SerializeField] private string cardName;
        [SerializeField] private Sprite icon;
        [SerializeField] private CardType cardType;
        [SerializeField] private CardGrade grade = CardGrade.Common;
        [SerializeField, TextArea] private string description;

        [Header("Player Motion")]
        [SerializeField] private string animationTrigger;

        [Header("Effect")]
        [SerializeField] private CardEffect effect;

        public string CardId => cardId;
        public string CardName => cardName;
        public Sprite Icon => icon;
        public CardType CardType => cardType;
        public CardGrade Grade => grade;
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

        public string GradeText
        {
            get
            {
                switch (grade)
                {
                    case CardGrade.Common:
                        return "일반";
                    case CardGrade.Rare:
                        return "희귀";
                    case CardGrade.Epic:
                        return "영웅";
                    case CardGrade.Legendary:
                        return "전설";
                    default:
                        return "등급 없음";
                }
            }
        }
    }
}
