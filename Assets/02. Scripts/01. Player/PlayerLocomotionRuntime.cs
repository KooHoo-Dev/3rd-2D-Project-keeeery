using UnityEngine;
using Utilities;

namespace Arcanist
{
    internal readonly struct PlayerLocomotionSettings
    {
        public readonly float DashPower;
        public readonly float DashDuration;
        public readonly float DashCooldown;
        public readonly float SprintMultiplier;

        public PlayerLocomotionSettings(
            float dashPower,
            float dashDuration,
            float dashCooldown,
            float sprintMultiplier)
        {
            DashPower = dashPower;
            DashDuration = dashDuration;
            DashCooldown = dashCooldown;
            SprintMultiplier = sprintMultiplier;
        }
    }

    /// <summary>
    /// Coordinates movement rules and delegates resource state and presentation.
    /// Input is supplied by PlayerController; this class does not read devices directly.
    /// </summary>
    internal sealed class PlayerLocomotionRuntime
    {
        private readonly CharacterController2D controller2D;
        private readonly PlayerLocomotionSettings settings;
        private readonly PlayerSprintRuntime sprint;
        private readonly PlayerMovementFeedback feedback;

        private float nextDashTime;
        private bool previousIsGrounded;

        public int FacingDirection { get; private set; } = 1;
        public bool IsGrounded => controller2D.IsGrounded;

        public PlayerLocomotionRuntime(
            CharacterController2D controller2D,
            PlayerLocomotionSettings settings,
            PlayerSprintRuntime sprint,
            PlayerMovementFeedback feedback)
        {
            this.controller2D = controller2D;
            this.settings = settings;
            this.sprint = sprint;
            this.feedback = feedback;
            previousIsGrounded = controller2D.IsGrounded;
        }

        public void HandleMovement(
            float horizontalInput,
            bool sprintPressed,
            bool updateMovementAnimation)
        {
            float absoluteMovement = Mathf.Abs(horizontalInput);
            bool canSprint = sprint.TrySprint(
                controller2D.IsGrounded,
                absoluteMovement > 0.0f,
                sprintPressed);

            SetSprintState(canSprint, controller2D.IsGrounded);
            ApplyFacing(horizontalInput);

            float movement = canSprint
                ? horizontalInput * settings.SprintMultiplier
                : horizontalInput;
            controller2D.SetMoveInput(movement);

            if (updateMovementAnimation)
                feedback.SetMovement(absoluteMovement, canSprint);
        }

        public void UpdateJumpInput(bool isBlocked, bool jumpPressed)
        {
            if (isBlocked || jumpPressed == false)
                return;

            bool wasGrounded = controller2D.IsGrounded;
            bool jumpAccepted = controller2D.RequestJump();

            if (jumpAccepted && wasGrounded == false)
                feedback.PlayDoubleJump(FacingDirection);
        }

        public void UpdateDashInput(bool isBlocked, bool dashPressed)
        {
            if (isBlocked || dashPressed == false || Time.time < nextDashTime)
                return;

            bool dashAccepted = controller2D.RequestDash(
                FacingDirection,
                settings.DashPower,
                settings.DashDuration);

            if (dashAccepted == false)
                return;

            nextDashTime = Time.time + settings.DashCooldown;
            feedback.PlayDash(FacingDirection);
        }

        public void UpdateStaminaRecovery()
        {
            sprint.UpdateRecovery();
        }

        public void UpdateGroundedAnimation(bool suppressTransitionTrigger)
        {
            bool isGrounded = controller2D.IsGrounded;
            feedback.SetGrounded(isGrounded);

            if (isGrounded == false)
                feedback.StopSprintLoop();

            if (suppressTransitionTrigger)
            {
                previousIsGrounded = isGrounded;
                return;
            }

            if (previousIsGrounded && isGrounded == false)
                feedback.PlayAirborneTransition(controller2D.VerticalVelocity > 0.0f);

            previousIsGrounded = isGrounded;
        }

        public void StopMovement(bool playSprintEndFx = true)
        {
            controller2D.SetMoveInput(0.0f);
            sprint.Stop();
            feedback.SetSprint(
                false,
                playSprintEndFx && controller2D.IsGrounded,
                FacingDirection);
        }

        public void ResetForRespawn()
        {
            controller2D.SetMoveInput(0.0f);
            nextDashTime = 0.0f;
            previousIsGrounded = controller2D.IsGrounded;
            sprint.Reset();
            feedback.ResetForRespawn();
        }

        private void ApplyFacing(float horizontal)
        {
            if (Mathf.Approximately(horizontal, 0.0f))
                return;

            FacingDirection = horizontal < 0.0f ? -1 : 1;
            feedback.SetFacing(FacingDirection);
        }

        private void SetSprintState(bool canSprint, bool playEndFx)
        {
            feedback.SetSprint(canSprint, playEndFx, FacingDirection);
        }
    }
}
