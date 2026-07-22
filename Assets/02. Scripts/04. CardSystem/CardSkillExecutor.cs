using UnityEngine;

namespace Arcanist
{
    internal enum CardSkillExecutionStatus
    {
        NoPendingCard,
        Executed,
        MissingEffect,
        MissingTarget,
    }

    internal readonly struct CardSkillExecutionResult
    {
        public CardSkillExecutionStatus Status { get; }
        public CardData Card { get; }

        public CardSkillExecutionResult(CardSkillExecutionStatus status, CardData card = null)
        {
            Status = status;
            Card = card;
        }
    }

    internal sealed class CardSkillExecutor
    {
        private readonly Animator animator;
        private readonly PlayerController playerController;
        private readonly Transform castPoint;
        private readonly Transform fallbackCastPoint;
        private readonly string fallbackAnimationTrigger;
        private readonly CardTargetResolver targetResolver;

        private CardData pendingCard;

        public Transform CastPoint => castPoint != null ? castPoint : fallbackCastPoint;

        public CardSkillExecutor(
            Animator animator,
            PlayerController playerController,
            Transform castPoint,
            Transform fallbackCastPoint,
            string fallbackAnimationTrigger,
            CardTargetResolver targetResolver)
        {
            this.animator = animator;
            this.playerController = playerController;
            this.castPoint = castPoint;
            this.fallbackCastPoint = fallbackCastPoint;
            this.fallbackAnimationTrigger = fallbackAnimationTrigger;
            this.targetResolver = targetResolver;
        }

        /// <summary>
        /// Stores the card and starts its animation. Returns false when no animation
        /// can be played and the caller should execute the card immediately.
        /// </summary>
        public bool Begin(CardData card)
        {
            if (card == null)
                return false;

            pendingCard = card;
            string triggerName = string.IsNullOrEmpty(card.AnimationTrigger)
                ? fallbackAnimationTrigger
                : card.AnimationTrigger;

            if (HasTrigger(triggerName) == false)
                return false;

            playerController?.BeginCardSkill();
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
            return true;
        }

        public CardSkillExecutionResult ExecutePending()
        {
            if (pendingCard == null)
                return new CardSkillExecutionResult(CardSkillExecutionStatus.NoPendingCard);

            CardData card = pendingCard;
            pendingCard = null;

            if (card.Effect == null)
                return new CardSkillExecutionResult(CardSkillExecutionStatus.MissingEffect, card);

            Transform resolvedCastPoint = CastPoint;
            Vector3 castOrigin = resolvedCastPoint != null
                ? resolvedCastPoint.position
                : Vector3.zero;

            if (targetResolver.TryResolveForCard(card, castOrigin, out Transform target) == false)
            {
                playerController?.EndCardSkill();
                return new CardSkillExecutionResult(CardSkillExecutionStatus.MissingTarget, card);
            }

            int facingDirection = playerController != null ? playerController.FacingDirection : 1;
            CardEffectContext context = new(
                card,
                playerController,
                resolvedCastPoint,
                facingDirection,
                target);
            card.Effect.Execute(context);
            return new CardSkillExecutionResult(CardSkillExecutionStatus.Executed, card);
        }

        public void Reset()
        {
            pendingCard = null;
        }

        private bool HasTrigger(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName) || animator == null)
                return false;

            AnimatorControllerParameter[] parameters = animator.parameters;
            foreach (AnimatorControllerParameter parameter in parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger
                    && parameter.name == triggerName)
                    return true;
            }

            return false;
        }
    }
}
