using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class GolemShootState : BossPatternState
    {
        [SerializeField] private ProjectileController armProjectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float duration = 1.3f;
        [SerializeField] private float targetYOffset = 0.8f;

        private Coroutine coroutine;

        protected override void OnEnable()
        {
            if (BossController == null)
                return;
                
            BossController.Animator.Play("Boss_Golem_ShootCast");
            coroutine = StartCoroutine(Coroutine());
        }

        protected override void OnDisable()
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        private IEnumerator Coroutine()
        {
            yield return new WaitForSeconds(duration);
            BossController.ChangeState(typeof(GolemIdleState));
        }
        public void ShootArm()
        {
            ProjectileController projectile = Instantiate(
                armProjectilePrefab,
                firePoint.position,
                Quaternion.identity
            );

            Rigidbody2D projectileRigidbody = projectile.GetComponent<Rigidbody2D>();

            if (projectileRigidbody != null)
            {
                projectileRigidbody.gravityScale = 0.0f;
                projectileRigidbody.linearVelocity = Vector2.zero;
            }

            Vector3 targetPosition = BossController.Player.position;
            targetPosition.y += targetYOffset;

            Vector2 direction = targetPosition - firePoint.position;

            projectile.Initialize(direction, BossController.Boss);
            projectile.StartMove();
        }
    }
}
