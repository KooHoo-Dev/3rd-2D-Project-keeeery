using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcanist
{
    public class DeckDropArea : MonoBehaviour, IDropHandler
    {
        private enum DropAreaType
        {
            Collection,
            Deck
        }

        [SerializeField] private InventoryWindow inventoryWindow;
        [SerializeField] private DropAreaType dropAreaType = DropAreaType.Deck;

        public void OnDrop(PointerEventData eventData)
        {
            CardDragHandler dragHandler = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<CardDragHandler>()
                : null;

            if (dragHandler == null || dragHandler.CardSlot == null)
                return;

            InventoryCardSlot slot = dragHandler.CardSlot;

            if (dropAreaType == DropAreaType.Deck && slot.SlotArea == InventoryCardSlotArea.Collection)
            {
                inventoryWindow.AddCardToDeck(slot.CardData);
                return;
            }

            if (dropAreaType == DropAreaType.Collection && slot.SlotArea == InventoryCardSlotArea.Deck)
                inventoryWindow.RemoveCardFromDeck(slot.CardData);
        }
    }
}
