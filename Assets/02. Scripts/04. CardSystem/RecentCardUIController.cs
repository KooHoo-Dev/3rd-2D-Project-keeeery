using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class RecentCardUIController : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private RecentCardUIItem itemPrefab;
        [SerializeField] private int maxCount = 4;
        [SerializeField] private int preloadCount = 4;
        [SerializeField] private float itemLifeTime = 10f;

        private readonly List<RecentCardUIItem> activeItems = new();
        private readonly Queue<RecentCardUIItem> itemPool = new();
        private readonly List<float> activeItemTimes = new();

        private void Awake()
        {
            if (contentRoot == null)
                contentRoot = transform;

            PreloadItems();
        }

        private void Update()
        {
            if (activeItems.Count != activeItemTimes.Count)
            {
                activeItemTimes.Clear();

                for (int i = 0; i < activeItems.Count; i++)
                    activeItemTimes.Add(Time.unscaledTime);

                return;
            }

            for (int i = activeItems.Count - 1; i >= 0; i--)
                if (Time.unscaledTime - activeItemTimes[i] >= itemLifeTime)
                    ReturnItem(activeItems[i]);
        }

        public void ShowUsedCard(CardData cardData)
        {
            if (cardData == null || itemPrefab == null)
                return;

            RecentCardUIItem item = GetItem();

            item.transform.SetParent(contentRoot, false);
            item.transform.SetAsFirstSibling();
            item.Initialize(cardData);

            activeItems.Insert(0, item);
            activeItemTimes.Insert(0, Time.unscaledTime);

            while (activeItems.Count > maxCount)
                ReturnItem(activeItems[^1]);
        }

        public void Clear()
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
                ReturnItemImmediate(activeItems[i]);

            activeItems.Clear();
        }

        private void PreloadItems()
        {
            if (itemPrefab == null)
                return;

            int count = Mathf.Max(0, preloadCount);

            for (int i = 0; i < count; i++)
            {
                RecentCardUIItem item = CreateItem();
                ReturnItemImmediate(item);
            }
        }

        private RecentCardUIItem GetItem()
        {
            if (itemPool.Count > 0)
                return itemPool.Dequeue();

            return CreateItem();
        }

        private RecentCardUIItem CreateItem()
        {
            RecentCardUIItem item = Instantiate(itemPrefab, contentRoot);
            item.HideImmediate();
            return item;
        }

        private void ReturnItem(RecentCardUIItem item)
        {
            if (item == null) return;

            int index = activeItems.IndexOf(item);

            if (index >= 0)
            {
                activeItems.RemoveAt(index);
                activeItemTimes.RemoveAt(index);
            }

            item.PlayHide(() =>
            {
                item.transform.SetParent(contentRoot, false);
                itemPool.Enqueue(item);
            });
        }

        private void ReturnItemImmediate(RecentCardUIItem item)
        {
            if (item == null) return;
            
            int index = activeItems.IndexOf(item);

            if (index >= 0)
            {
                activeItems.RemoveAt(index);
                activeItemTimes.RemoveAt(index);
            }


            item.HideImmediate();
            item.transform.SetParent(contentRoot, false);
            itemPool.Enqueue(item);
        }
    }
}
