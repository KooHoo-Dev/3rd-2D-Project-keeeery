using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arcanist
{
    public enum InventoryCardSlotArea
    {
        Collection,
        Deck
    }

    public class InventoryCardSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [Header("UI")]
        [SerializeField] private Image slotFrame;
        [SerializeField] private Image cardIcon;
        [SerializeField] private GameObject newBadge;
        [SerializeField] private GameObject lockIcon;

        [Header("Tint")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 0.75f);
        [SerializeField] private Color emptyColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        [SerializeField] private Color selectedFrameColor = new Color(1.0f, 0.86f, 0.35f, 1.0f);

        private Color defaultFrameColor = Color.white;
        private InventoryWindow owner;
        private CardData cardData;
        private int deckIndex = -1;
        private bool isOwned;
        private bool isSelected;
        private InventoryCardSlotArea slotArea;

        public CardData CardData => cardData;
        public int DeckIndex => deckIndex;
        public bool IsOwned => isOwned;
        public InventoryCardSlotArea SlotArea => slotArea;
        public Image CardIconImage => cardIcon;

        private void Awake()
        {
            if (slotFrame != null)
                defaultFrameColor = slotFrame.color;
        }

        public void Setup(InventoryWindow owner, CardData cardData, InventoryCardSlotArea slotArea, bool isOwned, bool isNew, int deckIndex = -1)
        {
            this.owner = owner;
            this.cardData = cardData;
            this.slotArea = slotArea;
            this.isOwned = isOwned;
            this.deckIndex = deckIndex;
            isSelected = false;

            RefreshVisual(isNew);
            SendTooltipData();
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (slotFrame != null)
                slotFrame.color = selected ? selectedFrameColor : defaultFrameColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (cardData == null)
                return;

            owner.SelectSlot(this);

            if (eventData.clickCount >= 2)
                owner.ToggleSelectedCardDeck();
        }

        public void HandleDropped(PointerEventData eventData)
        {
            if (cardData == null)
                return;

            owner.HandleCardDropped(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (cardData == null)
                return;

            owner.MarkCardAsSeen(cardData);
        }

        private void RefreshVisual(bool isNew)
        {
            if (cardIcon != null)
            {
                Sprite icon = GetCardIcon(cardData);
                cardIcon.sprite = icon;
                cardIcon.color = cardData == null ? emptyColor : isOwned ? normalColor : lockedColor;
                cardIcon.enabled = cardData != null && icon != null;
            }

            if (newBadge != null)
                newBadge.SetActive(isNew && isOwned);

            if (slotFrame != null)
                slotFrame.color = defaultFrameColor;

            if (lockIcon != null)
            {
                bool showLock = slotArea == InventoryCardSlotArea.Collection && cardData != null && !isOwned;
                lockIcon.SetActive(showLock);
            }
        }

        private void SendTooltipData()
        {
            TooltipTrigger tooltipTrigger = GetComponent<TooltipTrigger>();

            if (tooltipTrigger != null)
                tooltipTrigger.SetCardData(cardData);
        }

        private Sprite GetCardIcon(CardData data)
        {
            if (data == null)
                return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            string[] memberNames = { "Icon", "CardIcon", "Sprite", "cardIcon", "icon", "sprite" };

            foreach (string memberName in memberNames)
            {
                PropertyInfo property = data.GetType().GetProperty(memberName, flags);
                if (property != null && typeof(Sprite).IsAssignableFrom(property.PropertyType))
                    return property.GetValue(data) as Sprite;

                FieldInfo field = data.GetType().GetField(memberName, flags);
                if (field != null && typeof(Sprite).IsAssignableFrom(field.FieldType))
                    return field.GetValue(data) as Sprite;
            }

            return null;
        }
    }
}
