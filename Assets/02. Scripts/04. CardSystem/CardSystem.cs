using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Arcanist
{
    [RequireComponent(typeof(CardGaugeController))]
    [RequireComponent(typeof(CardHandUI))]
    public class CardSystem : MonoBehaviour
    {
        private readonly CardRuntimeDeck runtimeDeck = new();

        [Header("Runtime Inventory")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private bool syncFromPlayerInventory = true;

        [Header("Skill Motion")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string fallbackCardSkillTrigger = "CardSkill";

        [Header("Skill Effect")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform cardCastPoint;

        [Header("Targeting")]
        [SerializeField] private EnemyManager enemyManager;

        [Header("Announce Message")]
        [SerializeField] private string noTargetAnnounceMessage = "공격할 대상이 없습니다.";
        [SerializeField] private string fullHpAnnounceMessage = "체력이 이미 가득 찼습니다.";
        [SerializeField] private AnnounceMessage announceMessage;

        [Header("Recent Card UI")]
        [SerializeField] private RecentCardUIController recentCardUIController;

        private CardGaugeController cardGaugeController;
        private CardHandUI cardHandUI;
        private CardData pendingSkillCard;
        private bool isGaugeFull;

        public IReadOnlyList<CardData> OwnedCards => runtimeDeck.OwnedCards;
        public IReadOnlyList<CardData> PlayerDeck => runtimeDeck.PlayerDeck;
        public IReadOnlyList<CardData> DrawPile => runtimeDeck.DrawPile;
        public IReadOnlyList<CardData> Hand => runtimeDeck.Hand;
        public IReadOnlyList<CardData> DiscardPile => runtimeDeck.DiscardPile;

        private void Awake()
        {
            cardGaugeController = GetComponent<CardGaugeController>();
            cardHandUI = GetComponent<CardHandUI>();

            if (playerInventory == null) 
                playerInventory = PlayerInventory.Instance != null 
                ? PlayerInventory.Instance
                : FindFirstObjectByType<PlayerInventory>();

            if (playerController == null && playerAnimator != null)
                playerController = playerAnimator.GetComponentInParent<PlayerController>();
        }

        private void OnEnable()
        {
            if (playerInventory == null)
                playerInventory = PlayerInventory.Instance != null ? PlayerInventory.Instance : FindFirstObjectByType<PlayerInventory>();

            if (syncFromPlayerInventory && playerInventory != null)
            {
                playerInventory.OnInventoryChanged += SyncFromPlayerInventory;
                SyncFromPlayerInventory();
            }
        }

        private void OnDisable()
        {
            if (playerInventory != null)
                playerInventory.OnInventoryChanged -= SyncFromPlayerInventory;
        }

        private void SyncFromPlayerInventory()
        {
            if (!syncFromPlayerInventory || playerInventory == null)
                return;

            SetOwnedCards(playerInventory.OwnedCards);
            SyncPlayerDeckRuntime(playerInventory.DeckCards);
        }

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Q))
                UseCard(0);

            if (SimpleInput.GetKeyDown(Key.W))
                UseCard(1);

            if (SimpleInput.GetKeyDown(Key.E))
                UseCard(2);

            TryDrawIfReady();
        }

        public void OnGaugeFull()
        {
            isGaugeFull = true;
            TryDrawIfReady();
        }

        public void SetOwnedCards(IEnumerable<CardData> cards)
        {
            runtimeDeck.SetOwnedCards(cards);
        }

        public void SetPlayerDeck(IEnumerable<CardData> cards)
        {
            runtimeDeck.SetPlayerDeck(cards);
            isGaugeFull = false;
        }

        private void SyncPlayerDeckRuntime(IEnumerable<CardData> cards)
        {
            runtimeDeck.SyncPlayerDeck(cards);
        }

        public void ResetRuntimeCardsForRespawn()
        {
            pendingSkillCard = null;
            isGaugeFull = false;

            runtimeDeck.ResetForRespawn();

            cardHandUI?.ClearAllSlots();
            cardGaugeController?.ResetGaugeAndRecharge();
            recentCardUIController?.Clear();
        }

        public void AddOwnedCard(CardData card)
        {
            if (card == null) return;

            if (syncFromPlayerInventory && playerInventory != null)
            {
                playerInventory.AddCard(card, false);
                return;
            }

            runtimeDeck.AddOwnedCard(card);
        }

        public bool AddCardToPlayerDeck(CardData card)
        {
            if (card == null) return false;

            if (syncFromPlayerInventory && playerInventory != null)
                return playerInventory.AddToDeck(card);

            if (runtimeDeck.AddCardToPlayerDeck(card) == false)
            {
                Debug.LogWarning($"CardSystem ::: Cannot add {card.CardName}. This card is not owned.");
                return false;
            }

            isGaugeFull = false;
            return true;
        }

        public bool RemoveCardFromPlayerDeck(CardData card)
        {
            if (card == null) return false;

            if (syncFromPlayerInventory && playerInventory != null)
                return playerInventory.RemoveFromDeck(card);

            bool removed = runtimeDeck.RemoveCardFromPlayerDeck(card);
            if (removed)
                isGaugeFull = false;
            return removed;
        }

        private void TryDrawIfReady()
        {
            if (runtimeDeck.IsInitialized == false) return;

            if (isGaugeFull == false) return;

            if (cardHandUI == null || cardHandUI.IsDrawing) return;

            if (cardHandUI.HasEmptySlot == false) return;

            CardData drawnCard = runtimeDeck.Draw();

            if (drawnCard == null)
                return;

            bool drawStarted = cardHandUI.TryDrawToFirstEmptySlot(drawnCard, OnCardDrawCompleted);

            if (drawStarted)
            {
                runtimeDeck.AddToHand(drawnCard);
                isGaugeFull = false;
            }
        }

        private void OnCardDrawCompleted()
        {
            if (cardGaugeController != null)
                cardGaugeController.ResetGaugeAndRecharge();
        }

        private void UseCard(int slotIndex)
        {
            if (cardHandUI == null) return;
            if (cardHandUI.IsDrawing) return;

            CardData usedCard = cardHandUI.GetCard(slotIndex);
            if (usedCard == null) return;

            if (TryPrepareCardUse(usedCard) == false)
                return;

            pendingSkillCard = usedCard;
            PlayCardSkillMotion(usedCard);
            cardHandUI.UseCard(slotIndex);
            runtimeDeck.RecordUse(usedCard);

            TryDrawIfReady();
        }

        private bool TryPrepareCardUse(CardData card)
        {
            if (card == null)
                return false;

            if (IsHealCard(card) && IsPlayerHpFull())
            {
                ShowAnnounceMessage(fullHpAnnounceMessage);
                return false;
            }

            if (RequiresTarget(card) == false)
                return true;

            if (TryFindValidTarget(GetCastPointTransform().position, out _))
                return true;

            ShowAnnounceMessage(noTargetAnnounceMessage);
            return false;
        }

        private bool IsHealCard(CardData card)
        {
            if (card == null || card.Effect == null)
                return false;

            if (card.Effect is BuffCardEffect buffCardEffect)
                return buffCardEffect.HasModifier(BuffEffectType.HealRatio);

            return false;
        }

        private bool IsPlayerHpFull()
        {
            Player player = null;

            if (playerController != null)
                player = playerController.GetComponent<Player>();

            if (player == null)
                player = GetComponent<Player>();

            if (player == null || player.Stat == null)
                return false;

            return player.Stat.Hp >= player.Stat.MaxHp;
        }

        private bool RequiresTarget(CardData card)
        {
            if (card == null)
                return false;

            if (card.RequiresTarget)
                return true;

            if (card.Effect == null)
                return false;

            return card.Effect.RequiresTarget;
        }

        private Transform GetCastPointTransform()
        {
            if (cardCastPoint != null)
                return cardCastPoint;

            if (playerController != null)
                return playerController.transform;

            return transform;
        }

        private bool TryFindValidTarget(Vector3 origin, out Transform target)
        {
            target = null;

            if (enemyManager == null)
                return false;

            Transform foundTarget = enemyManager.FindBestTarget(origin);

            if (IsValidTarget(foundTarget) == false)
                return false;

            target = foundTarget;
            return true;
        }

        private bool IsValidTarget(Transform target)
        {
            if (target == null)
                return false;

            EnemyController enemyController = target.GetComponentInParent<EnemyController>();

            if (enemyController != null)
                return enemyController.IsDead == false;

            BossController bossController = target.GetComponentInParent<BossController>();

            if (bossController != null)
                return bossController.IsDead == false;

            return false;
        }

        private void ShowAnnounceMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (announceMessage != null)
            {
                announceMessage.Show(message, false);
                return;
            }

            Debug.Log(message);
        }

        private void PlayCardSkillMotion(CardData card)
        {
            if (card == null) return;

            string triggerName = string.IsNullOrEmpty(card.AnimationTrigger)
                ? fallbackCardSkillTrigger
                : card.AnimationTrigger;

            if (playerAnimator == null || HasAnimatorTrigger(triggerName) == false)
            {
                ExecutePendingCardSkill();
                return;
            }

            playerController?.BeginCardSkill();
            playerAnimator.ResetTrigger(triggerName);
            playerAnimator.SetTrigger(triggerName);
        }

        private bool HasAnimatorTrigger(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName) || playerAnimator == null)
                return false;

            AnimatorControllerParameter[] parameters = playerAnimator.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Trigger && 
                    parameters[i].name == triggerName)
                    return true;
            }

            return false;
        }

        public void ExecutePendingCardSkill()
        {
            if (pendingSkillCard == null) return;

            CardData card = pendingSkillCard;
            pendingSkillCard = null;

            if (card.Effect == null) return;

            Transform castPoint = GetCastPointTransform();
            int facingDirection = playerController != null ? playerController.FacingDirection : 1;

            if (TryResolveTargetForCard(card, castPoint.position, out Transform target) == false)
            {
                ShowAnnounceMessage(noTargetAnnounceMessage);
                playerController?.EndCardSkill();
                return;
            }

            CardEffectContext context = new(card, playerController, castPoint, facingDirection, target);
            card.Effect.Execute(context);
            recentCardUIController?.ShowUsedCard(card);
        }

        private bool TryResolveTargetForCard(CardData card, Vector3 origin, out Transform target)
        {
            target = null;

            if (RequiresTarget(card))
                return TryFindValidTarget(origin, out target);

            TryFindValidTarget(origin, out target);
            return true;
        }

        private bool IsCardInPlayerDeck(CardData card)
        {
            return runtimeDeck.Contains(card);
        }
    }
}
