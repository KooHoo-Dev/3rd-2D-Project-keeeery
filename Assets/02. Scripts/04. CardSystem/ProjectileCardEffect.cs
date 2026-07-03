using UnityEngine;

namespace Arcanist
{
    [CreateAssetMenu(menuName = "Arcanist/Card Effects/Projectile Card Effect")]
    public class ProjectileCardEffect : CardEffect
    {
        private enum SpawnMode
        {
            CastPoint,
            TargetPosition
        }

        private enum AimMode
        {
            Forward,
            Target,
            None
        }

        [Header("Effect Prefab")]
        [SerializeField] private GameObject effectPrefab;

        [Header("Spawn")]
        [SerializeField] private SpawnMode spawnMode = SpawnMode.CastPoint;
        [SerializeField] private Vector2 spawnOffset;

        [Header("Aim")]
        [SerializeField] private AimMode aimMode = AimMode.Forward;

        public override void Execute(CardEffectContext context)
        {
            if (effectPrefab == null)
            {
                Debug.LogWarning($"ProjectileCardEffect ::: Effect Prefab is missing. ({context.Card.CardName})");
                return;
            }

            Vector3 spawnPosition = ResolveSpawnPosition(context);
            Vector2 direction = ResolveDirection(context, spawnPosition);
            Quaternion rotation = ResolveRotation(direction);

            GameObject effectObject = CardEffectSpawner.Spawn(effectPrefab, spawnPosition, rotation);

            if (effectObject == null)
                return;

            ProjectileController projectileController = effectObject.GetComponent<ProjectileController>();

            if (projectileController != null)
                projectileController.Initialize(direction);

            CombatEntity owner = context.Player != null
                ? context.Player.GetComponent<CombatEntity>()
                : null;

            HitBox[] hitBoxes = effectObject.GetComponentsInChildren<HitBox>(true);

            for (int i = 0; i < hitBoxes.Length; i++)
            {
                if (hitBoxes[i] == null)
                    continue;

                hitBoxes[i].Initialize(owner);
            }
        }

        private Vector3 ResolveSpawnPosition(CardEffectContext context)
        {
            Vector3 basePosition = spawnMode switch
            {
                SpawnMode.TargetPosition => GetTargetPosition(context),
                _ => context.CastPoint.position
            };

            float offsetX = spawnOffset.x * Mathf.Sign(context.FacingDirection == 0 ? 1 : context.FacingDirection);
            return basePosition + new Vector3(offsetX, spawnOffset.y, 0f);
        }

        private Vector2 ResolveDirection(CardEffectContext context, Vector3 spawnPosition)
        {
            return aimMode switch
            {
                AimMode.Target => ((Vector2)GetTargetPosition(context) - (Vector2)spawnPosition).normalized,
                AimMode.None => Vector2.zero,
                _ => context.FacingDirection < 0 ? Vector2.left : Vector2.right
            };
        }

        private Quaternion ResolveRotation(Vector2 direction)
        {
            if (direction == Vector2.zero)
                return Quaternion.identity;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, angle);
        }

        private Vector3 GetTargetPosition(CardEffectContext context)
        {
            if (context.HasTarget)
                return context.TargetPosition;

            Vector2 fallbackDirection = context.FacingDirection < 0 ? Vector2.left : Vector2.right;
            return context.CastPoint.position + (Vector3)fallbackDirection;
        }
    }
}