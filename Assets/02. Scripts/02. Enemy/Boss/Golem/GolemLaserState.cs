using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class GolemLaserState : BossPatternState
    {
        [SerializeField] private GameObject laserObject;
        [SerializeField] private Transform laserPivot;
        [SerializeField] private float readyTime = 0.5f;
        [SerializeField] private float laserDuration = 2.0f;
        [SerializeField] private float targetYOffset = 0.8f;
        [SerializeField] private float angleOffset = 0.0f;

        private Coroutine coroutine;
        private Vector3 originalLaserPivotScale;
        private bool hasOriginalLaserPivotScale;

        protected override void OnEnable()
        {
            if (BossController == null)
                return;

            if (laserObject != null)
                laserObject.SetActive(false);

            BossController.Animator.speed = 1.0f;
            BossController.Animator.Play("Boss_Golem_Laser");

            coroutine = StartCoroutine(Coroutine());
        }

        protected override void OnDisable()
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            if (laserObject != null)
                laserObject.SetActive(false);

            if (BossController != null && BossController.Animator != null)
                BossController.Animator.speed = 1.0f;
        }

        private IEnumerator Coroutine()
        {
            yield return new WaitForSeconds(readyTime);

            RotateLaserToPlayer();

            if (laserObject != null)
                laserObject.SetActive(true);

            BossController.Animator.speed = 0.0f;

            yield return new WaitForSeconds(laserDuration);

            if (laserObject != null)
                laserObject.SetActive(false);

            BossController.Animator.speed = 1.0f;
            BossController.ChangeState(typeof(GolemIdleState));
        }
        private void RotateLaserToPlayer()
        {
            if (laserPivot == null || BossController.Player == null)
                return;

            CacheLaserPivotScale();
            CompensateBossFlip();

            Vector3 targetPosition = BossController.Player.position;
            targetPosition.y += targetYOffset;

            Vector2 direction = targetPosition - laserPivot.position;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            laserPivot.rotation = Quaternion.Euler(0.0f, 0.0f, angle + angleOffset);
        }

        private void CacheLaserPivotScale()
        {
            if (hasOriginalLaserPivotScale)
                return;

            originalLaserPivotScale = laserPivot.localScale;
            hasOriginalLaserPivotScale = true;
        }

        private void CompensateBossFlip()
        {
            float facingSign = Mathf.Sign(BossController.transform.lossyScale.x);
            float scaleX = Mathf.Abs(originalLaserPivotScale.x);

            if (facingSign < 0.0f)
                scaleX *= -1.0f;

            laserPivot.localScale = new Vector3(
                scaleX,
                originalLaserPivotScale.y,
                originalLaserPivotScale.z);
        }
    }     
}
