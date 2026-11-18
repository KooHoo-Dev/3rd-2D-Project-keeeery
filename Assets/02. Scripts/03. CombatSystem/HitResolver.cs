using UnityEngine;

namespace Arcanist
{
    internal enum HitResolutionStatus
    {
        Ignored,
        Parried,
        Applied,
    }

    internal readonly struct HitResolution
    {
        public HitResolutionStatus Status { get; }
        public Vector3 Position { get; }

        public HitResolution(HitResolutionStatus status, Vector3 position = default)
        {
            Status = status;
            Position = position;
        }
    }

    internal readonly struct HitEffectSettings
    {
        public readonly bool UseKnockback;
        public readonly float KnockbackForce;
        public readonly bool KnockbackFromHitBoxCenter;
        public readonly bool ResetVelocityBeforeKnockback;
        public readonly bool UseStun;
        public readonly float StunDuration;

        public HitEffectSettings(
            bool useKnockback,
            float knockbackForce,
            bool knockbackFromHitBoxCenter,
            bool resetVelocityBeforeKnockback,
            bool useStun,
            float stunDuration)
        {
            UseKnockback = useKnockback;
            KnockbackForce = knockbackForce;
            KnockbackFromHitBoxCenter = knockbackFromHitBoxCenter;
            ResetVelocityBeforeKnockback = resetVelocityBeforeKnockback;
            UseStun = useStun;
            StunDuration = stunDuration;
        }
    }

    internal sealed class HitResolver
    {
        private readonly HitEffectSettings settings;

        public HitResolver(HitEffectSettings settings)
        {
            this.settings = settings;
        }

        public HitResolution Resolve(
            CombatEntity sender,
            CombatEntity receiver,
            Collider2D collision,
            Transform hitBoxTransform,
            AttackInfo attackInfo)
        {
            if (sender == null || receiver == null || collision == null || CombatSystem.Instance == null)
                return new HitResolution(HitResolutionStatus.Ignored);

            PlayerController playerController = receiver.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsParryWindowOpen)
            {
                playerController.SuccessParry();
                return new HitResolution(HitResolutionStatus.Parried);
            }

            int damage = attackInfo.RollDamage();

            if (sender.TryGetComponent(out BuffController buffController))
                damage = buffController.ModifyDamage(damage);

            Vector3 hitPosition = collision.ClosestPoint(sender.transform.position);
            CombatEvent combatEvent = new()
            {
                EventType = CombatEventType.DamageEvent,
                Amount = damage,
                Position = hitPosition,
            };

            CombatSystem.Instance.To(sender, receiver, combatEvent);
            ApplyKnockback(receiver, collision, hitBoxTransform);
            ApplyStun(receiver);
            return new HitResolution(HitResolutionStatus.Applied, hitPosition);
        }

        private void ApplyKnockback(
            CombatEntity receiver,
            Collider2D collision,
            Transform hitBoxTransform)
        {
            if (settings.UseKnockback == false || receiver.CanReceiveKnockback == false)
                return;

            Rigidbody2D receiverRigidbody = receiver.GetComponent<Rigidbody2D>();
            if (receiverRigidbody == null)
                receiverRigidbody = receiver.GetComponentInChildren<Rigidbody2D>();
            if (receiverRigidbody == null)
                return;

            Vector2 origin = settings.KnockbackFromHitBoxCenter
                ? hitBoxTransform.position
                : collision.ClosestPoint(hitBoxTransform.position);
            Vector2 direction = receiverRigidbody.position - origin;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = receiver.transform.position.x >= hitBoxTransform.position.x
                    ? Vector2.right
                    : Vector2.left;
            }
            else
            {
                direction.Normalize();
            }

            if (settings.ResetVelocityBeforeKnockback)
                receiverRigidbody.linearVelocity = Vector2.zero;

            receiverRigidbody.AddForce(direction * settings.KnockbackForce, ForceMode2D.Impulse);
        }

        private void ApplyStun(CombatEntity receiver)
        {
            if (settings.UseStun == false
                || receiver.CanReceiveStun == false
                || settings.StunDuration <= 0.0f)
                return;

            IStunnable stunnable = receiver.GetComponent<IStunnable>();
            if (stunnable == null)
                stunnable = receiver.GetComponentInChildren<IStunnable>();
            if (stunnable == null)
                stunnable = receiver.GetComponentInParent<IStunnable>();

            stunnable?.ApplyStun(settings.StunDuration);
        }
    }
}
