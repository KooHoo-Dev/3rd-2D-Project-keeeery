using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private int rewardRoundCount = 2;
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

        private OverlayController overlayController;
        private RoomController roomController;
        private Coroutine spawnCoroutine;
        private Coroutine nextRewardRoundCoroutine;
        private RewardCardListView cardListView;
        private RewardSelectionMotion selectionMotion;
        private RewardCardView selectedRewardCardView;
        private AnnounceMessage announceMessage;
        private int remainingRewardRounds;
        private bool isSelectingReward;

        private void Awake()
        {
            ResolveReferences();
            BuildRuntimeServices();
        }

        public void OpenReward()
        {
            ResolveReferences();
            ResetTexts();
            remainingRewardRounds = Mathf.Max(1, rewardRoundCount);
            OpenReward(CreateRewards());
        }

        private void OpenReward(List<CardData> rewards)
        {
            EnsureRuntimeServices();
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
            cardListView.SetInteractable(false);

            StartCoroutine(PlayRewardSelection(selectedView));
        }

        public bool IsSelected(RewardCardView view)
        {
            return selectedRewardCardView == view;
        }

        private IEnumerator SpawnRewards(List<CardData> rewards)
        {
            yield return cardListView.Spawn(rewards);
            spawnCoroutine = null;
        }

        private IEnumerator PlayRewardSelection(RewardCardView selectedView)
        {
            yield return selectionMotion.FadeUnselected(cardListView.Views, selectedView);

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

            Vector3 centerScale = selectionMotion.GetCenterScale();

            yield return selectionMotion.MoveToCenter(selectedRect);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.0f, selectedHoldDuration));
            yield return selectedMotion.PlayExit(centerScale);

            GrantReward(selectedView.CardData);

            if (announceMessage != null)
                announceMessage.Show("카드가 보유 중인 카드 목록에 추가되었습니다!");

            remainingRewardRounds--;

            if (remainingRewardRounds > 0)
            {
                OpenNextRewardRound();
                yield break;
            }

            HideOverlayOrCloseReward();
        }

        private void OpenNextRewardRound()
        {
            if (nextRewardRoundCoroutine != null)
                StopCoroutine(nextRewardRoundCoroutine);

            nextRewardRoundCoroutine = StartCoroutine(OpenNextRewardRoundRoutine());
        }

        private IEnumerator OpenNextRewardRoundRoutine()
        {
            ClearRewardCards();
            ResetRewardState();
            ResetTexts();

            if (rewardWindow != null)
                rewardWindow.SetActive(true);

            cardListView.RebuildLayout();

            yield return null;

            cardListView.RebuildLayout();

            spawnCoroutine = StartCoroutine(SpawnRewards(CreateRewards()));
            nextRewardRoundCoroutine = null;
        }

        private List<CardData> CreateRewards()
        {
            if (cardDatabase == null)
                return new List<CardData>();

            RewardGradeWeights weights = new(
                commonWeight,
                rareWeight,
                epicWeight,
                legendaryWeight);
            RewardCardSelector selector = new(cardDatabase.AllCards, weights, minimumGrade);
            IEnumerable<CardData> ownedCards = playerInventory != null
                ? playerInventory.OwnedCards
                : null;

            return selector.CreateRewards(rewardCount, ownedCards);
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
                playerInventory.AddCard(selectedCard, false);
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
        }

        private void ResetTexts()
        {
            ResetCanvasGroup(rewardSelectTextCanvasGroup, 1.0f, true);
        }

        private void ClearRewardCards()
        {
            EnsureRuntimeServices();
            cardListView.Clear();
        }

        private void StopRewardCoroutines()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            if (nextRewardRoundCoroutine != null)
            {
                StopCoroutine(nextRewardRoundCoroutine);
                nextRewardRoundCoroutine = null;
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

        private void EnsureRuntimeServices()
        {
            if (cardListView == null || selectionMotion == null)
                BuildRuntimeServices();
        }

        private void BuildRuntimeServices()
        {
            cardListView = new RewardCardListView(this, cardPrefab, rewardContainer, cardSpawnDelay);
            selectionMotion = new RewardSelectionMotion(
                rewardContainer,
                rewardSelectTextCanvasGroup,
                selectedCenterScale,
                selectedMoveDuration);
        }
    }
}
