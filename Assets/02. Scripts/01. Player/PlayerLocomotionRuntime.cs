using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Arcanist
{
    internal readonly struct PlayerLocomotionSettings
    {
        public readonly float DashPower;
        public readonly float DashDuration;
        public readonly float DashCooldown;
        public readonly float SprintMultiplier;
        public readonly float StaminaCostPerSecond;
        public readonly float StaminaRecoveryPerSecond;
        public readonly float StaminaRecoveryDelay;
        public readonly float SprintInputGraceTime;

        public PlayerLocomotionSettings(
            float dashPower,
            float dashDuration,
            float dashCooldown,
            float sprintMultiplier,
            float staminaCostPerSecond,
            float staminaRecoveryPerSecond,
            float staminaRecoveryDelay,
            float sprintInputGraceTime)
        {
            DashPower = dashPower;
            DashDuration = dashDuration;
            DashCooldown = dashCooldown;
            SprintMultiplier = sprintMultiplier;
            StaminaCostPerSecond = staminaCostPerSecond;
            StaminaRecoveryPerSecond = staminaRecoveryPerSecond;
            StaminaRecoveryDelay = staminaRecoveryDelay;
            SprintInputGraceTime = sprintInputGraceTime;
        }
    }

    /// <summary>
    /// Owns mutable locomotion state and movement feedback.
    /// It is not a MonoBehaviour, so existing prefab component references stay unchanged.
    /// </summary>
    internal sealed class PlayerLocomotionRuntime
    {
        private readonly Transform ownerTransform;
        private readonly Animator animator;
        private readonly CharacterController2D controller2D;
        private readonly PlayerLocomotionSettings settings;
        private readonly GameObject doubleJumpFxPrefab;
        private readonly GameObject dashFxPrefab;
        private readonly GameObject sprintLoopFxPrefab;
        private readonly GameObject sprintEndFxPrefab;
        private readonly Transform fxSpawnPoint;
        private readonly Transform sprintFxSpawnPoint;

        private float nextDashTime;
        private float staminaRecoveryStartTime;
        private float sprintInputGraceEndTime;
        private bool isSprinting;
        private bool previousIsGrounded;
        private GameObject sprintLoopFx;

        public int FacingDirection { get; private set; } = 1;
        public bool IsGrounded => controller2D.IsGrounded;

        public PlayerLocomotionRuntime(
            Transform ownerTransform,
            Animator animator,
            CharacterController2D controller2D,
            PlayerLocomotionSettings settings,
            GameObject doubleJumpFxPrefab,
            GameObject dashFxPrefab,
            GameObject sprintLoopFxPrefab,
            GameObject sprintEndFxPrefab,
            Transform fxSpawnPoint,
            Transform sprintFxSpawnPoint)
        {
            this.ownerTransform = ownerTransform;
            this.animator = animator;
            this.controller2D = controller2D;
            this.settings = settings;
            this.doubleJumpFxPrefab = doubleJumpFxPrefab;
            this.dashFxPrefab = dashFxPrefab;
            this.sprintLoopFxPrefab = sprintLoopFxPrefab;
            this.sprintEndFxPrefab = sprintEndFxPrefab;
            this.fxSpawnPoint = fxSpawnPoint;
            this.sprintFxSpawnPoint = sprintFxSpawnPoint;
            previousIsGrounded = controller2D.IsGrounded;
        }

        public void HandleMovement(float horizontalInput, bool updateMovementAnimation)
        {
            float absoluteMovement = Mathf.Abs(horizontalInput);
            bool hasMoveInput = absoluteMovement > 0.0f;
            bool shiftPressed = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            bool canSprint = UpdateSprintState(hasMoveInput, shiftPressed);

            if (updateMovementAnimation)
                animator.SetFloat(PlayerController.MOVEMENT, canSprint ? 2.0f : absoluteMovement);

            ApplyFacing(horizontalInput);
            controller2D.SetMoveInput(canSprint
                ? horizontalInput * settings.SprintMultiplier
                : horizontalInput);
        }

        public float GetArrowHorizontalInput(float moveScale)
        {
            if (Keyboard.current == null)
                return 0.0f;

            if (Keyboard.current.leftArrowKey.isPressed)
                return -moveScale;

            if (Keyboard.current.rightArrowKey.isPressed)
                return moveScale;

            return 0.0f;
        }

        public void UpdateJumpInput(bool isBlocked)
        {
            if (isBlocked || SimpleInput.GetKeyDown(Key.Space) == false)
                return;

            bool wasGrounded = controller2D.IsGrounded;
            bool jumpAccepted = controller2D.RequestJump();

            if (jumpAccepted && wasGrounded == false)
            {
                animator.SetTrigger(PlayerController.DOUBLE_JUMP);
                SpawnFx(doubleJumpFxPrefab, fxSpawnPoint);
            }
        }

        public void UpdateDashInput(bool isBlocked)
        {
            if (isBlocked || SimpleInput.GetKeyDown(Key.A) == false || Time.time < nextDashTime)
                return;

            bool dashAccepted = controller2D.RequestDash(
                FacingDirection,
                settings.DashPower,
                settings.DashDuration);

            if (dashAccepted == false)
                return;

            nextDashTime = Time.time + settings.DashCooldown;
            animator.SetTrigger(PlayerController.DASH);
            SpawnFx(dashFxPrefab, fxSpawnPoint);
        }

        public void UpdateStaminaRecovery()
        {
            if (isSprinting || Player.LocalPlayer == null || Time.time < staminaRecoveryStartTime)
                return;

            Player.LocalPlayer.RecoverStamina(settings.StaminaRecoveryPerSecond * Time.deltaTime);
        }

        public void UpdateGroundedAnimation(bool suppressTransitionTrigger)
        {
            bool isGrounded = controller2D.IsGrounded;
            animator.SetBool(PlayerController.IS_GROUNDED, isGrounded);

            if (isGrounded == false)
                StopSprintLoopFx();

            if (suppressTransitionTrigger)
            {
                previousIsGrounded = isGrounded;
                return;
            }

            if (previousIsGrounded && isGrounded == false)
            {
                animator.SetTrigger(controller2D.VerticalVelocity > 0.0f
                    ? PlayerController.JUMP
                    : PlayerController.DESCENDING);
            }

            previousIsGrounded = isGrounded;
        }

        public void StopMovement(bool playSprintEndFx = true)
        {
            controller2D.SetMoveInput(0.0f);
            SetSprintState(false, playSprintEndFx);
        }

        public void ResetForRespawn()
        {
            StopMovement(false);
            StopSprintLoopFx();
            nextDashTime = 0.0f;
            staminaRecoveryStartTime = 0.0f;
            sprintInputGraceEndTime = 0.0f;
            isSprinting = false;
            previousIsGrounded = controller2D.IsGrounded;
            animator.SetBool(PlayerController.IS_SPRINT, false);
        }

        private bool UpdateSprintState(bool hasMoveInput, bool shiftPressed)
        {
            if (shiftPressed && hasMoveInput)
                sprintInputGraceEndTime = Time.time + settings.SprintInputGraceTime;

            bool wantsSprint = controller2D.IsGrounded
                               && shiftPressed
                               && (hasMoveInput || Time.time < sprintInputGraceEndTime);
            bool canSprint = TryConsumeSprintStamina(wantsSprint);
            SetSprintState(canSprint, controller2D.IsGrounded);
            return canSprint;
        }

        private bool TryConsumeSprintStamina(bool wantsSprint)
        {
            if (wantsSprint == false || Player.LocalPlayer == null)
                return false;

            float staminaCost = settings.StaminaCostPerSecond * Time.deltaTime;
            staminaRecoveryStartTime = Time.time + settings.StaminaRecoveryDelay;

            if (Player.LocalPlayer.HasStamina(staminaCost)
                && Player.LocalPlayer.TryUseStamina(staminaCost))
                return true;

            sprintInputGraceEndTime = 0.0f;
            return false;
        }

        private void ApplyFacing(float horizontal)
        {
            if (Mathf.Approximately(horizontal, 0.0f))
                return;

            FacingDirection = horizontal < 0.0f ? -1 : 1;
            Vector3 scale = animator.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * FacingDirection;
            animator.transform.localScale = scale;
        }

        private void SetSprintState(bool value, bool playEndFx)
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
                StartSprintLoopFx();
                return;
            }

            StopSprintLoopFx();

            if (playEndFx && controller2D.IsGrounded)
                SpawnFx(sprintEndFxPrefab, sprintFxSpawnPoint);
        }

        private void StartSprintLoopFx()
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

        private void StopSprintLoopFx()
        {
            if (sprintLoopFx == null)
                return;

            Object.Destroy(sprintLoopFx);
            sprintLoopFx = null;
        }

        private void SpawnFx(GameObject prefab, Transform customSpawnPoint)
        {
            if (prefab == null)
                return;

            Transform spawnPoint = customSpawnPoint != null ? customSpawnPoint : ownerTransform;
            GameObject fx = Object.Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            Vector3 scale = fx.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * FacingDirection;
            fx.transform.localScale = scale;
        }
    }
}
