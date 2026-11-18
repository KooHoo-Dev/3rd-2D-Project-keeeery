namespace Arcanist
{
    internal enum CardHandOperation
    {
        Idle,
        Drawing,
        Using,
    }

    internal sealed class CardHandState
    {
        private readonly CardData[] slots;

        public CardHandOperation Operation { get; private set; }
        public bool IsBusy => Operation != CardHandOperation.Idle;
        public bool HasEmptySlot => FindFirstEmptySlot() >= 0;

        public CardHandState(int slotCount)
        {
            slots = new CardData[slotCount];
        }

        public bool TryBeginDraw(CardData card, out int slotIndex)
        {
            slotIndex = -1;

            if (card == null || IsBusy)
                return false;

            slotIndex = FindFirstEmptySlot();
            if (slotIndex < 0)
                return false;

            slots[slotIndex] = card;
            Operation = CardHandOperation.Drawing;
            return true;
        }

        public void CompleteDraw(int slotIndex, bool succeeded)
        {
            if (succeeded == false && IsValidIndex(slotIndex))
                slots[slotIndex] = null;

            Operation = CardHandOperation.Idle;
        }

        public bool TryBeginUse(int slotIndex, out CardData card)
        {
            card = GetCard(slotIndex);

            if (card == null || IsBusy)
                return false;

            Operation = CardHandOperation.Using;
            return true;
        }

        public void CompleteUse(int slotIndex)
        {
            if (IsValidIndex(slotIndex))
                slots[slotIndex] = null;

            Operation = CardHandOperation.Idle;
        }

        public CardData GetCard(int slotIndex)
        {
            return IsValidIndex(slotIndex) ? slots[slotIndex] : null;
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i] = null;

            Operation = CardHandOperation.Idle;
        }

        private int FindFirstEmptySlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    return i;
            }

            return -1;
        }

        private bool IsValidIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < slots.Length;
        }
    }
}
