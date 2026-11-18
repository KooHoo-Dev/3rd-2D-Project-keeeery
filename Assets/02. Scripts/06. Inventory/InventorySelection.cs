namespace Arcanist
{
    internal sealed class InventorySelection
    {
        public InventoryCardSlot CollectionSlot { get; private set; }
        public InventoryCardSlot DeckSlot { get; private set; }

        public void Select(InventoryCardSlot slot)
        {
            if (slot == null)
                return;

            Clear();

            if (slot.SlotArea == InventoryCardSlotArea.Collection)
            {
                if (slot.IsOwned == false)
                    return;

                CollectionSlot = slot;
            }
            else
            {
                DeckSlot = slot;
            }

            slot.SetSelected(true);
        }

        public void RestoreCollectionSelection(InventoryCardSlot slot)
        {
            CollectionSlot = slot;
            CollectionSlot?.SetSelected(true);
        }

        public void Clear()
        {
            CollectionSlot?.SetSelected(false);
            DeckSlot?.SetSelected(false);
            CollectionSlot = null;
            DeckSlot = null;
        }
    }
}
