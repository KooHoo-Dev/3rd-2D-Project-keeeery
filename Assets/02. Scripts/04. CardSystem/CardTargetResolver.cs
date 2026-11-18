using UnityEngine;

namespace Arcanist
{
    internal sealed class CardTargetResolver
    {
        private readonly EnemyManager enemyManager;

        public CardTargetResolver(EnemyManager enemyManager)
        {
            this.enemyManager = enemyManager;
        }

        public bool TryResolveForCard(CardData card, Vector3 origin, out Transform target)
        {
            target = null;

            if (RequiresTarget(card))
                return TryFindTarget(origin, out target);

            TryFindTarget(origin, out target);
            return true;
        }

        public bool TryFindTarget(Vector3 origin, out Transform target)
        {
            target = null;

            if (enemyManager == null)
                return false;

            if (enemyManager.TryFindBestTarget(origin, out ICombatTarget candidate) == false)
                return false;

            target = candidate.TargetPoint;
            return true;
        }

        public static bool RequiresTarget(CardData card)
        {
            return card != null
                   && (card.RequiresTarget || card.Effect != null && card.Effect.RequiresTarget);
        }

    }
}
