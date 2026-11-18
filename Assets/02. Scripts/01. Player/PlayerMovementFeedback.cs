using UnityEngine;

namespace Arcanist
{
    /// <summary>Owns locomotion animation parameters and movement-related FX.</summary>
    internal sealed class PlayerMovementFeedback
    {
        private readonly Transform ownerTransform;
        private readonly Animator animator;
        private readonly GameObject doubleJumpFxPrefab;
        private readonly GameObject dashFxPrefab;
        private readonly GameObject sprintLoopFxPrefab;
        private readonly GameObject sprintEndFxPrefab;
        private readonly Transform fxSpawnPoint;
        private readonly Transform sprintFxSpawnPoint;

        private bool isSprinting;
        private GameObject sprintLoopFx;

        public PlayerMovementFeedback(
            Transform ownerTransform,
            Animator animator,
            GameObject doubleJumpFxPrefab,
            GameObject dashFxPrefab,
            GameObject sprintLoopFxPrefab,
            GameObject sprintEndFxPrefab,
            Transform fxSpawnPoint,
            Transform sprintFxSpawnPoint)
        {
            this.ownerTransform = ownerTransform;
            this.animator = animator;
            this.doubleJumpFxPrefab = doubleJumpFxPrefab;
            this.dashFxPrefab = dashFxPrefab;
            this.sprintLoopFxPrefab = sprintLoopFxPrefab;
            this.sprintEndFxPrefab = sprintEndFxPrefab;
            this.fxSpawnPoint = fxSpawnPoint;
            this.sprintFxSpawnPoint = sprintFxSpawnPoint;
        }

        public void SetMovement(float absoluteMovement, bool isSprint)
        {
            animator.SetFloat(PlayerController.MOVEMENT, isSprint ? 2.0f : absoluteMovement);
        }

        public void SetFacing(int facingDirection)
        {
            Vector3 scale = animator.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facingDirection;
            animator.transform.localScale = scale;
        }

        public void SetGrounded(bool isGrounded)
        {
            animator.SetBool(PlayerController.IS_GROUNDED, isGrounded);
        }

        public void PlayAirborneTransition(bool isAscending)
        {
            animator.SetTrigger(isAscending ? PlayerController.JUMP : PlayerController.DESCENDING);
        }

        public void PlayDoubleJump(int facingDirection)
        {
            animator.SetTrigger(PlayerController.DOUBLE_JUMP);
            SpawnFx(doubleJumpFxPrefab, fxSpawnPoint, facingDirection);
        }

        public void PlayDash(int facingDirection)
        {
            animator.SetTrigger(PlayerController.DASH);
            SpawnFx(dashFxPrefab, fxSpawnPoint, facingDirection);
        }

        public void SetSprint(bool value, bool playEndFx, int facingDirection)
        {
            if (isSprinting == value)
            {
                animator.SetBool(PlayerController.IS_SPRINT, value);
                return;
            }

            isSprinting = value;
            animator.SetBool(PlayerController.IS_SPRINT, value);

            if (value)
            {
                StartSprintLoop();
                return;
            }

            StopSprintLoop();

            if (playEndFx)
                SpawnFx(sprintEndFxPrefab, sprintFxSpawnPoint, facingDirection);
        }

        public void StopSprintLoop()
        {
            if (sprintLoopFx == null)
                return;

            Object.Destroy(sprintLoopFx);
            sprintLoopFx = null;
        }

        public void ResetForRespawn()
        {
            isSprinting = false;
            StopSprintLoop();
            animator.SetBool(PlayerController.IS_SPRINT, false);
        }

        private void StartSprintLoop()
        {
            if (sprintLoopFx != null || sprintLoopFxPrefab == null)
                return;

            Transform spawnPoint = sprintFxSpawnPoint != null ? sprintFxSpawnPoint : ownerTransform;
            sprintLoopFx = Object.Instantiate(
                sprintLoopFxPrefab,
                spawnPoint.position,
                Quaternion.identity,
                spawnPoint);
        }

        private void SpawnFx(GameObject prefab, Transform customSpawnPoint, int facingDirection)
        {
            if (prefab == null)
                return;

            Transform spawnPoint = customSpawnPoint != null ? customSpawnPoint : ownerTransform;
            GameObject fx = Object.Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            Vector3 scale = fx.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facingDirection;
            fx.transform.localScale = scale;
        }
    }
}
