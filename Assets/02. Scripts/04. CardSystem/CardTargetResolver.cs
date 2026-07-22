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

            Transform candidate = enemyManager.FindBestTarget(origin);
            if (IsAlive(candidate) == false)
                return false;

            target = candidate;
            return true;
        }

        public static bool RequiresTarget(CardData card)
        {
            return card != null
                   && (card.RequiresTarget || card.Effect != null && card.Effect.RequiresTarget);
        }

        private static bool IsAlive(Transform target)
        {
            if (target == null)
                return false;

            EnemyController enemy = target.GetComponentInParent<EnemyController>();
            if (enemy != null)
                return enemy.IsDead == false;

            BossController boss = target.GetComponentInParent<BossController>();
            return boss != null && boss.IsDead == false;
        }
    }
}
