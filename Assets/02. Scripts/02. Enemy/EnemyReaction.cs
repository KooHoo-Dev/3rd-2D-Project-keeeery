using UnityEngine;

namespace Arcanist
{
    internal sealed class EnemyReaction
    {
        private readonly EnemyMovement movement;
        private readonly Animator animator;
        private readonly float hurtDuration;
        private readonly float hurtKnockbackSpeed;
        private readonly bool canBeStunned;

        private float hurtEndTime;
        private float stunEndTime;

        public bool IsHurt { get; private set; }
        public bool IsStunned { get; private set; }
        public bool BlocksBehaviour => IsHurt || IsStunned;

        public EnemyReaction(
            EnemyMovement movement,
            Animator animator,
            float hurtDuration,
            float hurtKnockbackSpeed,
            bool canBeStunned)
        {
            this.movement = movement;
            this.animator = animator;
            this.hurtDuration = hurtDuration;
            this.hurtKnockbackSpeed = hurtKnockbackSpeed;
            this.canBeStunned = canBeStunned;
        }

        public void Reset()
        {
            IsHurt = false;
            IsStunned = false;
            hurtEndTime = 0.0f;
            stunEndTime = 0.0f;
            SetHurtAnimation(false);
        }

        public bool Tick()
        {
            if (IsStunned)
            {
                movement.Stop();

                if (Time.time >= stunEndTime)
                    EndStun();

                return true;
            }

            if (IsHurt)
            {
                if (Time.time >= hurtEndTime)
                    EndHurt();

                return true;
            }

            return false;
        }

        public void Hurt(bool isDead, Transform target)
        {
            if (isDead || IsStunned)
                return;

            IsHurt = true;
            hurtEndTime = Time.time + hurtDuration;
            movement.Stop();
            ApplyKnockback(target);
            SetHurtAnimation(true);
        }

        public void Stun(bool isDead, float duration)
        {
            if (isDead || canBeStunned == false || duration <= 0.0f)
                return;

            IsStunned = true;
            IsHurt = false;
            stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);
            movement.Stop();
            SetHurtAnimation(true);
        }

        public bool ConsumeStunEnded()
        {
            if (IsStunned || stunEndTime >= 0.0f)
                return false;

            stunEndTime = 0.0f;
            return true;
        }

        public void OnDeath()
        {
            IsHurt = false;
            IsStunned = false;
            hurtEndTime = 0.0f;
            stunEndTime = 0.0f;
            movement.Stop();
            SetHurtAnimation(false);
        }

        private void ApplyKnockback(Transform target)
        {
            float direction = target != null
                ? Mathf.Sign(movement.Position.x - target.position.x)
                : -Mathf.Sign(movement.ActorTransform.localScale.x);
            movement.ApplyHorizontalVelocity(direction, hurtKnockbackSpeed);
        }

        private void EndHurt()
        {
            IsHurt = false;
            movement.Stop();
            SetHurtAnimation(false);
        }

        private void EndStun()
        {
            IsStunned = false;
            stunEndTime = -1.0f;
            movement.Stop();
            SetHurtAnimation(false);
        }

        private void SetHurtAnimation(bool value)
        {
            if (animator != null)
                animator.SetBool(EnemyController.IS_HURT, value);
        }
    }
}
