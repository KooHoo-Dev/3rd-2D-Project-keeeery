using UnityEngine;

namespace Arcanist
{
    internal sealed class CardHandView
    {
        private readonly RectTransform[] slots;
        private readonly RectTransform cardPrefab;

        public CardHandView(RectTransform[] slots, RectTransform cardPrefab)
        {
            this.slots = slots;
            this.cardPrefab = cardPrefab;
        }

        public RectTransform CreateCard(int slotIndex, CardData cardData)
        {
            if (IsValidSlot(slotIndex) == false || cardPrefab == null)
                return null;

            RectTransform card = Object.Instantiate(cardPrefab, slots[slotIndex]);
            card.gameObject.SetActive(false);
            card.SetAsFirstSibling();
            ResetTransform(card);
            card.GetComponent<CardView>()?.Setup(cardData);
            GetOrAddCanvasGroup(card).alpha = 1.0f;
            card.gameObject.SetActive(true);
            return card;
        }

        public RectTransform FindCard(int slotIndex)
        {
            if (IsValidSlot(slotIndex) == false)
                return null;

            CardView cardView = slots[slotIndex].GetComponentInChildren<CardView>();
            if (cardView != null)
                return cardView.GetComponent<RectTransform>();

            if (slots[slotIndex].childCount == 0)
                return null;

            Transform child = slots[slotIndex].GetChild(slots[slotIndex].childCount - 1);
            return child.GetComponent<RectTransform>();
        }

        public void Clear()
        {
            if (slots == null)
                return;

            foreach (RectTransform slot in slots)
            {
                if (slot == null)
                    continue;

                for (int i = slot.childCount - 1; i >= 0; i--)
                    Object.Destroy(slot.GetChild(i).gameObject);
            }
        }

        private bool IsValidSlot(int slotIndex)
        {
            return slots != null
                   && slotIndex >= 0
                   && slotIndex < slots.Length
                   && slots[slotIndex] != null;
        }

        private static void ResetTransform(RectTransform card)
        {
            card.localScale = Vector3.one;
            card.localRotation = Quaternion.identity;
            card.anchoredPosition3D = Vector3.zero;
        }

        private static CanvasGroup GetOrAddCanvasGroup(RectTransform card)
        {
            CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
            return canvasGroup != null ? canvasGroup : card.gameObject.AddComponent<CanvasGroup>();
        }
    }
}
