using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arcanist
{
    internal sealed class RewardCardListView
    {
        private readonly RewardController controller;
        private readonly GameObject cardPrefab;
        private readonly RectTransform container;
        private readonly float spawnDelay;
        private readonly List<RewardCardView> views = new();

        public IReadOnlyList<RewardCardView> Views => views;

        public RewardCardListView(
            RewardController controller,
            GameObject cardPrefab,
            RectTransform container,
            float spawnDelay)
        {
            this.controller = controller;
            this.cardPrefab = cardPrefab;
            this.container = container;
            this.spawnDelay = spawnDelay;
        }

        public IEnumerator Spawn(IReadOnlyList<CardData> rewards)
        {
            List<RectTransform> cardRects = new();
            List<LayoutElement> layoutElements = new();
            List<RewardCardMotion> cardMotions = new();
            List<Vector2> endPositions = new();

            for (int i = 0; i < rewards.Count; i++)
            {
                RectTransform cardRect = CreateCard(
                    rewards[i],
                    out LayoutElement layoutElement,
                    out RewardCardMotion cardMotion);

                if (cardRect == null || cardMotion == null)
                    continue;

                cardRects.Add(cardRect);
                layoutElements.Add(layoutElement);
                cardMotions.Add(cardMotion);
            }

            RebuildLayout();

            for (int i = 0; i < cardRects.Count; i++)
            {
                Vector2 endPosition = cardRects[i].anchoredPosition;
                endPositions.Add(endPosition);
                layoutElements[i].ignoreLayout = true;
                cardMotions[i].PrepareDraw(endPosition);
            }

            for (int i = 0; i < cardRects.Count; i++)
            {
                yield return cardMotions[i].PlayDraw(endPositions[i]);
                yield return new WaitForSecondsRealtime(spawnDelay);
            }

            foreach (LayoutElement layoutElement in layoutElements)
                layoutElement.ignoreLayout = false;

            RebuildLayout();
            SetInteractable(true);

            foreach (RewardCardView view in views)
                view?.RefreshDefaultScale();
        }

        public void SetInteractable(bool value)
        {
            foreach (RewardCardView view in views)
            {
                if (view != null)
                    view.SetInteractable(value);
            }
        }

        public void Clear()
        {
            views.Clear();

            if (container == null)
                return;

            for (int i = container.childCount - 1; i >= 0; i--)
                Object.Destroy(container.GetChild(i).gameObject);
        }

        public void RebuildLayout()
        {
            Canvas.ForceUpdateCanvases();

            if (container != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(container);

            Canvas.ForceUpdateCanvases();
        }

        private RectTransform CreateCard(
            CardData cardData,
            out LayoutElement layoutElement,
            out RewardCardMotion cardMotion)
        {
            layoutElement = null;
            cardMotion = null;

            if (cardPrefab == null || container == null || cardData == null)
                return null;

            GameObject cardObject = Object.Instantiate(cardPrefab, container);
            RectTransform cardRect = cardObject.transform as RectTransform;
            layoutElement = GetOrAddComponent<LayoutElement>(cardObject);
            layoutElement.ignoreLayout = false;
            cardMotion = GetOrAddComponent<RewardCardMotion>(cardObject);

            RewardCardView view = cardObject.GetComponent<RewardCardView>();
            if (view == null)
                view = cardObject.GetComponentInChildren<RewardCardView>(true);

            if (view != null)
            {
                view.Initialize(controller);
                view.SetCard(cardData);
                view.SetInteractable(false);
                views.Add(view);
            }

            return cardRect;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
