using UnityEngine;

namespace Arcanist
{
    internal enum CardUseFailure
    {
        None,
        InvalidCard,
        FullHealth,
        MissingTarget,
    }

    internal sealed class CardUseValidator
    {
        private readonly Player player;
        private readonly CardTargetResolver targetResolver;

        public CardUseValidator(Player player, CardTargetResolver targetResolver)
        {
            this.player = player;
            this.targetResolver = targetResolver;
        }

        public CardUseFailure Validate(CardData card, Vector3 castOrigin)
        {
            if (card == null)
                return CardUseFailure.InvalidCard;

            if (IsHealCard(card) && IsPlayerHealthFull())
                return CardUseFailure.FullHealth;

            if (CardTargetResolver.RequiresTarget(card)
                && targetResolver.TryFindTarget(castOrigin, out _) == false)
                return CardUseFailure.MissingTarget;

            return CardUseFailure.None;
        }

        private bool IsPlayerHealthFull()
        {
            return player != null
                   && player.Stat != null
                   && player.Stat.Hp >= player.Stat.MaxHp;
        }

        private static bool IsHealCard(CardData card)
        {
            return card.Effect is BuffCardEffect buffEffect
                   && buffEffect.HasModifier(BuffEffectType.HealRatio);
        }
    }
}
