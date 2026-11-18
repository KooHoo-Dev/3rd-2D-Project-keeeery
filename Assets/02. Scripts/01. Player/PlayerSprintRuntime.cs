using UnityEngine;

namespace Arcanist
{
    internal readonly struct PlayerSprintSettings
    {
        public readonly float StaminaCostPerSecond;
        public readonly float StaminaRecoveryPerSecond;
        public readonly float StaminaRecoveryDelay;
        public readonly float InputGraceTime;

        public PlayerSprintSettings(
            float staminaCostPerSecond,
            float staminaRecoveryPerSecond,
            float staminaRecoveryDelay,
            float inputGraceTime)
        {
            StaminaCostPerSecond = staminaCostPerSecond;
            StaminaRecoveryPerSecond = staminaRecoveryPerSecond;
            StaminaRecoveryDelay = staminaRecoveryDelay;
            InputGraceTime = inputGraceTime;
        }
    }

    /// <summary>Owns sprint stamina consumption, recovery delay and input grace state.</summary>
    internal sealed class PlayerSprintRuntime
    {
        private readonly Player player;
        private readonly PlayerSprintSettings settings;

        private float recoveryStartTime;
        private float inputGraceEndTime;

        public bool IsSprinting { get; private set; }

        public PlayerSprintRuntime(Player player, PlayerSprintSettings settings)
        {
            this.player = player;
            this.settings = settings;
        }

        public bool TrySprint(bool isGrounded, bool hasMoveInput, bool sprintPressed)
        {
            if (sprintPressed && hasMoveInput)
                inputGraceEndTime = Time.time + settings.InputGraceTime;

            bool wantsSprint = isGrounded
                               && sprintPressed
                               && (hasMoveInput || Time.time < inputGraceEndTime);

            if (wantsSprint == false || player == null)
            {
                IsSprinting = false;
                return false;
            }

            float staminaCost = settings.StaminaCostPerSecond * Time.deltaTime;
            recoveryStartTime = Time.time + settings.StaminaRecoveryDelay;

            if (player.HasStamina(staminaCost) && player.TryUseStamina(staminaCost))
            {
                IsSprinting = true;
                return true;
            }

            inputGraceEndTime = 0.0f;
            IsSprinting = false;
            return false;
        }

        public void UpdateRecovery()
        {
            if (IsSprinting || player == null || Time.time < recoveryStartTime)
                return;

            player.RecoverStamina(settings.StaminaRecoveryPerSecond * Time.deltaTime);
        }

        public void Stop()
        {
            IsSprinting = false;
        }

        public void Reset()
        {
            IsSprinting = false;
            recoveryStartTime = 0.0f;
            inputGraceEndTime = 0.0f;
        }
    }
}
