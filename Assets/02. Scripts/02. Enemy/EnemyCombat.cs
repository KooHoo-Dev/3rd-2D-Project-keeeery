using UnityEngine;

namespace Arcanist
{
    public sealed class EnemyCombat
    {
        private readonly Animator animator;
        private readonly Enemy owner;
        private readonly ProjectileController projectilePrefab;
        private readonly Transform firePoint;
        private readonly float attackCooldown;

        private float nextAttackTime;

        public EnemyCombat(
            Animator animator,
            Enemy owner,
            ProjectileController projectilePrefab,
            Transform firePoint,
            float attackCooldown)
        {
            this.animator = animator;
            this.owner = owner;
            this.projectilePrefab = projectilePrefab;
            this.firePoint = firePoint;
            this.attackCooldown = attackCooldown;
        }

        public void Reset()
        {
            nextAttackTime = 0.0f;
        }

        public void TryAttack()
        {
            if (animator == null || Time.time < nextAttackTime)
                return;

            nextAttackTime = Time.time + attackCooldown;
            animator.SetInteger(EnemyController.ATTACK_INDEX, Random.Range(0, 2));
            animator.SetTrigger(EnemyController.ATTACK);
        }

        public void FireProjectile(Transform target, bool isDead)
        {
            if (isDead || target == null || projectilePrefab == null || firePoint == null || owner == null)
                return;

            Vector3 firePosition = firePoint.position;
            Vector2 direction = target.position.x >= firePosition.x ? Vector2.right : Vector2.left;
            GameObject projectileObject = CardEffectSpawner.Spawn(
                projectilePrefab.gameObject,
                firePosition,
                Quaternion.identity);

            if (projectileObject != null && projectileObject.TryGetComponent(out ProjectileController projectile))
                projectile.Initialize(direction, owner);
        }
    }
}
