using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Arcanist
{
    public class InventorySlotList : MonoBehaviour
    {
        [SerializeField] private InventoryCardSlot slotPrefab;
        [SerializeField] private Transform contentRoot;

        private readonly List<InventoryCardSlot> activeSlots = new();
        private ComponentPool<InventoryCardSlot> slotPool;

        public IReadOnlyList<InventoryCardSlot> ActiveSlots => activeSlots;

        public void Configure(InventoryCardSlot prefab, Transform parent)
        {
            if (slotPrefab == null)
                slotPrefab = prefab;
            if (contentRoot == null)
                contentRoot = parent;

            InitializePool();
        }

        private void Awake()
        {
            if (contentRoot == null)
                contentRoot = transform;

            InitializePool();
        }

        public void RenderCollection(
            InventoryWindow owner,
            IReadOnlyList<CardData> cards,
            PlayerInventory inventory)
        {
            ReleaseAll();

            if (!CanRender() || owner == null || cards == null || inventory == null)
                return;

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                InventoryCardSlot slot = GetSlot();
                slot.Setup(
                    owner,
                    card,
                    InventoryCardSlotArea.Collection,
                    inventory.HasCard(card),
                    inventory.IsNewCard(card));
            }
        }

        public void RenderDeck(
            InventoryWindow owner,
            IReadOnlyList<CardData> cards,
            int slotCount)
        {
            ReleaseAll();

            if (!CanRender() || owner == null || cards == null)
                return;

            for (int i = 0; i < slotCount; i++)
            {
                CardData card = i < cards.Count ? cards[i] : null;
                InventoryCardSlot slot = GetSlot();
                slot.Setup(owner, card, InventoryCardSlotArea.Deck, card != null, false, i);
            }
        }

        public bool MarkCardAsSeen(InventoryWindow owner, CardData card)
        {
            for (int i = 0; i < activeSlots.Count; i++)
            {
                InventoryCardSlot slot = activeSlots[i];

                if (slot == null || slot.CardData != card)
                    continue;

                slot.Setup(owner, card, InventoryCardSlotArea.Collection, true, false);
                return true;
            }

            return false;
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < activeSlots.Count; i++)
            {
                if (activeSlots[i] != null)
                    activeSlots[i].gameObject.SetActive(false);
            }

            activeSlots.Clear();
        }

        private bool CanRender()
        {
            InitializePool();
            return slotPool != null;
        }

        private void InitializePool()
        {
            if (slotPool == null && slotPrefab != null && contentRoot != null)
                slotPool = new ComponentPool<InventoryCardSlot>(slotPrefab, contentRoot);
        }

        private InventoryCardSlot GetSlot()
        {
            InventoryCardSlot slot = slotPool.Get();
            slot.transform.SetAsLastSibling();
            activeSlots.Add(slot);
            return slot;
        }
    }
}
