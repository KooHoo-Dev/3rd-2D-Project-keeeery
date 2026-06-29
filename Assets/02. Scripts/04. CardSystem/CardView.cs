using UnityEngine;
using UnityEngine.UI;

namespace Arcanist
{
    public class CardView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TooltipTrigger tooltipTrigger;

        public CardData CardData { get; private set; }

        private void Awake()
        {
            if (tooltipTrigger == null)
                tooltipTrigger = GetComponent<TooltipTrigger>();
        }

        public void Setup(CardData cardData)
        {
            CardData = cardData;

            if (CardData == null)
            {
                Clear();
                return;
            }

            SetIcon(CardData.Icon);
            SetTooltip(CardData);
        }

        public void Clear()
        {
            CardData = null;
            SetIcon(null);
            SetTooltip(null);
        }

        private void SetIcon(Sprite icon)
        {
            if (iconImage == null) return;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;

            Color color = iconImage.color;
            color.a = icon != null ? 1.0f : 0.0f;
            iconImage.color = color;
        }

        private void SetTooltip(CardData cardData)
        {
            if (tooltipTrigger == null) return;

            if (cardData == null)
            {
                tooltipTrigger.Clear();
                return;
            }

            tooltipTrigger.SetCard(cardData, true);
        }
    }
}