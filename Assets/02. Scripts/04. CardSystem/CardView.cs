using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arcanist
{
    public class CardView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardTypeText;
        [SerializeField] private Image iconImage;

        public CardData CardData { get; private set; }

        public void Setup(CardData cardData)
        {
            CardData = cardData;

            if (CardData == null)
            {
                Clear();
                return;
            }

            if (cardNameText != null)
                cardNameText.SetText(CardData.CardName);

            if (cardTypeText != null)
                cardTypeText.SetText(CardData.CardType.ToString());

            if (iconImage != null)
            {
                iconImage.sprite = CardData.Icon;
                iconImage.enabled = CardData.Icon != null;
                iconImage.raycastTarget = false;

                Color color = iconImage.color;
                color.a = CardData.Icon != null ? 1f : 0f;
                iconImage.color = color;
            }
        }

        public void Clear()
        {
            CardData = null;

            if (cardNameText != null)
                cardNameText.SetText(string.Empty);

            if (cardTypeText != null)
                cardTypeText.SetText(string.Empty);

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;

                Color color = iconImage.color;
                color.a = 0f;
                iconImage.color = color;
            }
        }
    }
}