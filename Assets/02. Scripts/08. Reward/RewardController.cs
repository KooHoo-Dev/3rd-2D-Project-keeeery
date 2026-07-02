using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Arcanist
{
    public class RewardController : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private GameObject rewardWindow;
        [SerializeField] private RectTransform rewardContainer;

        [Header("Card")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private CardDatabase cardDatabase;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private int rewardCount = 3;
        [SerializeField] private CardGrade minimumGrade = CardGrade.Common;

        [Header("Grade Weights")]
        [SerializeField] private int commonWeight = 70;
        [SerializeField] private int rareWeight = 22;
        [SerializeField] private int epicWeight = 7;
        [SerializeField] private int legendaryWeight = 1;

        [Header("Interaction")]
        [SerializeField] private CanvasGroup rewardSelectTextCanvasGroup;
        [SerializeField] private float selectedCenterScale = 1.55f;
        [SerializeField] private float selectedMoveDuration = 0.3f;
        [SerializeField] private float selectedHoldDuration = 0.18f;
        [SerializeField] private float cardSpawnDelay = 0.08f;

        private readonly List<RewardCardView> rewardCardViews = new List<RewardCardView>();

        private OverlayController overlayController;
        private RoomController roomController;
        private Coroutine spawnCoroutine;
        private RewardCardView selectedRewardCardView;
        private AnnounceMessage announceMessage;
        private bool isSelectingReward;

        private void Awake()
        {
            ResolveReferences();
        }

        public void OpenReward()
        {
            ResolveReferences();
            ResetTexts();
            OpenReward(CreateRewards());
        }

        public void OpenReward(List<CardData> rewards)
        {
            ResetRewardState();
            ResetTexts();

            if (rewardWindow != null)
                rewardWindow.SetActive(true);

            spawnCoroutine = StartCoroutine(SpawnRewards(rewards));
        }

        public void CloseReward()
        {
            StopRewardCoroutines();
            ClearRewardCards();
            ResetRewardState();
            ResetTexts();

            if (rewardWindow != null)
                rewardWindow.SetActive(false);
        }

        public void SelectRewardCard(RewardCardView selectedView)
        {
            if (selectedView == null || isSelectingReward)
                return;

            selectedRewardCardView = selectedView;
            isSelectingReward = true;
            SetCardsInteractable(false);

            StartCoroutine(PlayRewardSelection(selectedView));
        }

        public bool IsSelected(RewardCardView view)
        {
            return selectedRewardCardView == view;
        }

        private IEnumerator SpawnRewards(List<CardData> rewards)
        {
            List<RectTransform> cardRects = new List<RectTransform>();
            List<LayoutElement> layoutElements = new List<LayoutElement>();
            List<RewardCardMotion> cardMotions = new List<RewardCardMotion>();
            List<Vector2> endPositions = new List<Vector2>();

            for (int i = 0; i < rewards.Count; i++)
            {
                RectTransform cardRect = SpawnRewardCard(rewards[i], out LayoutElement layoutElement, out RewardCardMotion cardMotion);

                if (cardRect == null || cardMotion == null)
                    continue;

                cardRects.Add(cardRect);
                layoutElements.Add(layoutElement);
                cardMotions.Add(cardMotion);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardContainer);
            Canvas.ForceUpdateCanvases();

            for (int i = 0; i < cardRects.Count; i++)
            {
                endPositions.Add(cardRects[i].anchoredPosition);
                layoutElements[i].ignoreLayout = true;
                cardMotions[i].PrepareDraw(endPositions[i]);
            }

            for (int i = 0; i < cardRects.Count; i++)
            {
                yield return cardMotions[i].PlayDraw(endPositions[i]);
                yield return new WaitForSecondsRealtime(cardSpawnDelay);
            }

            for (int i = 0; i < layoutElements.Count; i++)
                layoutElements[i].ignoreLayout = false;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardContainer);
            spawnCoroutine = null;

            for (int i = 0; i < rewardCardViews.Count; i++)
            {
                rewardCardViews[i].RefreshDefaultScale();
                rewardCardViews[i].SetInteractable(true);
            }
        }

        private RectTransform SpawnRewardCard(CardData cardData, out LayoutElement layoutElement, out RewardCardMotion cardMotion)
        {
            layoutElement = null;
            cardMotion = null;

            if (cardPrefab == null || rewardContainer == null || cardData == null)
                return null;

            GameObject cardObject = Instantiate(cardPrefab, rewardContainer);
            RectTransform cardRect = cardObject.transform as RectTransform;

            layoutElement = GetOrAddComponent<LayoutElement>(cardObject);
            layoutElement.ignoreLayout = false;

            cardMotion = GetOrAddComponent<RewardCardMotion>(cardObject);

            RewardCardView cardView = cardObject.GetComponent<RewardCardView>();

            if (cardView == null)
                cardView = cardObject.GetComponentInChildren<RewardCardView>(true);

            if (cardView != null)
            {
                cardView.Initialize(this);
                cardView.SetCard(cardData);
                cardView.SetInteractable(false);
                rewardCardViews.Add(cardView);
            }

            return cardRect;
        }

        private IEnumerator PlayRewardSelection(RewardCardView selectedView)
        {
            yield return FadeUnselectedCards(selectedView);

            RewardCardMotion selectedMotion = selectedView.GetComponentInParent<RewardCardMotion>();
            RectTransform selectedRect = selectedMotion != null ? selectedMotion.transform as RectTransform : null;

            if (selectedMotion == null || selectedRect == null)
            {
                GrantRewardAndClose(selectedView.CardData);
                yield break;
            }

            LayoutElement selectedLayoutElement = selectedRect.GetComponent<LayoutElement>();

            if (selectedLayoutElement != null)
                selectedLayoutElement.ignoreLayout = true;

            Vector3 centerScale = new Vector3(selectedCenterScale, selectedCenterScale, 1.0f);

            yield return MoveSelectedCardToCenter(selectedRect, centerScale);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.0f, selectedHoldDuration));
            yield return selectedMotion.PlayExit(centerScale);

            GrantReward(selectedView.CardData);
            HideOverlayOrCloseReward();

            if (announceMessage != null)
                announceMessage.Show();
        }

        private IEnumerator FadeUnselectedCards(RewardCardView selectedView)
        {
            const float fadeDuration = 0.2f;

            for (int i = 0; i < rewardCardViews.Count; i++)
            {
                if (rewardCardViews[i] != null && rewardCardViews[i] != selectedView)
                    StartCoroutine(FadeCanvasGroup(rewardCardViews[i].CanvasGroup, 0.0f, fadeDuration));
            }

            if (rewardSelectTextCanvasGroup != null)
                StartCoroutine(FadeCanvasGroup(rewardSelectTextCanvasGroup, 0.0f, fadeDuration));

            yield return new WaitForSecondsRealtime(fadeDuration);
        }

        private IEnumerator MoveSelectedCardToCenter(RectTransform selectedRect, Vector3 targetScale)
        {
            Vector3 startPosition = selectedRect.position;
            Vector3 targetPosition = rewardContainer.position;
            Vector3 startScale = selectedRect.localScale;
            float duration = Mathf.Max(0.01f, selectedMoveDuration);
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);

                selectedRect.position = Vector3.LerpUnclamped(startPosition, targetPosition, eased);
                selectedRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);

                yield return null;
            }

            selectedRect.position = targetPosition;
            selectedRect.localScale = targetScale;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            if (canvasGroup == null)
                yield break;

            float startAlpha = canvasGroup.alpha;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0.0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = t * t * (3.0f - 2.0f * t);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);

                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private List<CardData> CreateRewards()
        {
            List<CardData> rewards = new List<CardData>();
            HashSet<CardData> excludedCards = new HashSet<CardData>();

            if (playerInventory != null)
            {
                for (int i = 0; i < playerInventory.OwnedCards.Count; i++)
                    excludedCards.Add(playerInventory.OwnedCards[i]);
            }

            for (int i = 0; i < rewardCount; i++)
            {
                CardData reward = DrawRewardCard(excludedCards);

                if (reward == null)
                    break;

                rewards.Add(reward);
                excludedCards.Add(reward);
            }

            return rewards;
        }

        private CardData DrawRewardCard(HashSet<CardData> excludedCards)
        {
            List<CardData> candidates = GetAvailableCards(excludedCards);

            if (candidates.Count <= 0)
                return null;

            int totalWeight = 0;

            for (int i = 0; i < candidates.Count; i++)
                totalWeight += GetGradeWeight(candidates[i].Grade);

            if (totalWeight <= 0)
                return candidates[Random.Range(0, candidates.Count)];

            int randomWeight = Random.Range(0, totalWeight);
            int currentWeight = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                currentWeight += GetGradeWeight(candidates[i].Grade);

                if (randomWeight < currentWeight)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        private List<CardData> GetAvailableCards(HashSet<CardData> excludedCards)
        {
            List<CardData> candidates = new List<CardData>();

            if (cardDatabase == null)
                return candidates;

            List<CardData> allCards = cardDatabase.GetAllCards();

            for (int i = 0; i < allCards.Count; i++)
            {
                CardData card = allCards[i];

                if (card == null || excludedCards.Contains(card) || card.Grade < minimumGrade)
                    continue;

                candidates.Add(card);
            }

            return candidates;
        }

        private int GetGradeWeight(CardGrade grade)
        {
            switch (grade)
            {
                case CardGrade.Common:
                    return commonWeight;
                case CardGrade.Rare:
                    return rareWeight;
                case CardGrade.Epic:
                    return epicWeight;
                case CardGrade.Legendary:
                    return legendaryWeight;
                default:
                    return 0;
            }
        }

        private void GrantRewardAndClose(CardData selectedCard)
        {
            GrantReward(selectedCard);
            HideOverlayOrCloseReward();
        }

        private void GrantReward(CardData selectedCard)
        {
            if (selectedCard == null)
                return;

            ResolveReferences();

            if (playerInventory != null)
                playerInventory.AddCard(selectedCard);
        }

        private void HideOverlayOrCloseReward()
        {
            ResolveReferences();

            ClearRewardCards();
            ResetRewardState();

            if (overlayController != null)
                overlayController.Hide();
            else if (rewardWindow != null)
                rewardWindow.SetActive(false);

            if (roomController != null)
                roomController.CompleteReward();
        }

        private void ResolveReferences()
        {
            if (cardDatabase == null)
                cardDatabase = CardDatabase.Instance;

            if (playerInventory == null)
                playerInventory = PlayerInventory.Instance;

            if (overlayController == null)
                overlayController = FindFirstObjectByType<OverlayController>(FindObjectsInactive.Include);

            if (roomController == null)
                roomController = FindFirstObjectByType<RoomController>();

            if (announceMessage == null)
                announceMessage = FindFirstObjectByType<AnnounceMessage>(FindObjectsInactive.Include);
        }

        private void ResetRewardState()
        {
            selectedRewardCardView = null;
            isSelectingReward = false;
            rewardCardViews.Clear();
        }

        private void ResetTexts()
        {
            ResetCanvasGroup(rewardSelectTextCanvasGroup, 1.0f, true);
        }

        private void SetCardsInteractable(bool value)
        {
            for (int i = 0; i < rewardCardViews.Count; i++)
            {
                if (rewardCardViews[i] != null)
                    rewardCardViews[i].SetInteractable(value);
            }
        }

        private void ClearRewardCards()
        {
            if (rewardContainer == null)
                return;

            for (int i = rewardContainer.childCount - 1; i >= 0; i--)
                Destroy(rewardContainer.GetChild(i).gameObject);
        }

        private void StopRewardCoroutines()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            StopAllCoroutines();
        }

        private void ResetCanvasGroup(CanvasGroup canvasGroup, float alpha, bool interactable)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = alpha;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        private T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return 1.0f - Mathf.Pow(1.0f - value, 3.0f);
        }
    }
}
