using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class HitBox : MonoBehaviour
    {
        [field: SerializeField] public AttackInfo AttackInfo { get; private set; }
        [SerializeField] private bool explodeProjectileOnHit;
        [SerializeField] private ProjectileController projectileController;

        [Header("Multi Hit")]
        [SerializeField] private bool allowMultiHit;
        [SerializeField] private float multiHitInterval = 0.25f;

        [Header("Impact Feedback")]
        [SerializeField] private bool useHitStop;
        [SerializeField] private HitStopController hitStopController;
        [SerializeField] private float hitStopDuration = 0.06f;

        [SerializeField] private bool useCameraShake;
        [SerializeField] private CameraImpulseController cameraImpulseController;

        [Header("Knockback")]
        [SerializeField] private bool useKnockback;
        [SerializeField] private float knockbackForce = 8.0f;
        [SerializeField] private bool knockbackFromHitBoxCenter = true;
        [SerializeField] private bool resetTargetVelocityBeforeKnockback = true;

        [Header("Stun")]
        [SerializeField] private bool useStun;
        [SerializeField] private float stunDuration = 2.0f;

        private bool impactFeedbackPlayed;

        private CombatEntity Owner { get; set; }

        private readonly HashSet<HurtBox> checkList = new HashSet<HurtBox>();
        private readonly Dictionary<HurtBox, float> lastHitTimes = new Dictionary<HurtBox, float>();

        private void Awake()
        {
            Owner = GetComponentInParent<CombatEntity>();

            if (projectileController == null)
                projectileController = GetComponentInParent<ProjectileController>();
        }

        private void OnEnable()
        {
            if (Owner == null)
                Owner = GetComponentInParent<CombatEntity>();

            if (projectileController == null)
                projectileController = GetComponentInParent<ProjectileController>();

            checkList.Clear();
            lastHitTimes.Clear();
            impactFeedbackPlayed = false;
        }

        public void Initialize(CombatEntity owner)
        {
            Owner = owner;
            checkList.Clear();
            lastHitTimes.Clear();
            impactFeedbackPlayed = false;
        }

        private void ResolveFeedbackControllers()
        {
            if (hitStopController == null)
                hitStopController = FindFirstObjectByType<HitStopController>();

            if (cameraImpulseController == null)
                cameraImpulseController = FindFirstObjectByType<CameraImpulseController>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            TryHit(collision);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (allowMultiHit == false)
                return;

            TryHit(collision);
        }

        private void TryHit(Collider2D collision)
        {
            if (CombatSystem.Instance == null)
                return;

            if (Owner == null)
                Owner = GetComponentInParent<CombatEntity>();

            HurtBox hurtBox = CombatSystem.Instance.GetHurtBoxOrNull(collision);

            if (hurtBox == null) return;

            CombatEntity sender = Owner;
            CombatEntity receiver = hurtBox.Owner;

            if (sender == null || receiver == null) return;

            if (sender == receiver) return;

            EnemyController enemy = receiver.GetComponent<EnemyController>();

            if (enemy != null && enemy.IsDead) return;

            PlayerController playerController = receiver.GetComponent<PlayerController>();

            if (playerController != null && playerController.IsParryWindowOpen)
            {
                playerController.SuccessParry();
                return;
            }

            if (CanHit(hurtBox) == false) return;

            RegisterHit(hurtBox);
            PlayImpactFeedbackOnce();

            int damage = AttackInfo.RollDamage();

            if (sender.TryGetComponent(out BuffController buffController))
                damage = buffController.ModifyDamage(damage);

            CombatEvent @event;
            @event.EventType = CombatEventType.DamageEvent;
            @event.Amount = damage;
            @event.Position = collision.ClosestPoint(sender.transform.position);

            hurtBox.PlayHitFx(@event.Position);
            CombatSystem.Instance.To(sender, receiver, @event);
            ApplyKnockback(receiver, collision);
            ApplyStun(receiver);

            if (explodeProjectileOnHit && projectileController != null)
                projectileController.Explode();
        }

        private void ApplyKnockback(CombatEntity receiver, Collider2D collision)
        {
            if (!useKnockback || receiver == null)
                return;

            Rigidbody2D receiverRigidbody = receiver.GetComponent<Rigidbody2D>();

            if (receiverRigidbody == null)
                receiverRigidbody = receiver.GetComponentInChildren<Rigidbody2D>();

            if (receiverRigidbody == null)
                return;

            Vector2 origin = knockbackFromHitBoxCenter
                ? transform.position
                : collision.ClosestPoint(transform.position);

            Vector2 targetPosition = receiverRigidbody.position;
            Vector2 direction = targetPosition - origin;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = receiver.transform.position.x >= transform.position.x ? Vector2.right : Vector2.left;
            else
                direction.Normalize();

            if (resetTargetVelocityBeforeKnockback)
                receiverRigidbody.linearVelocity = Vector2.zero;

            receiverRigidbody.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
        }

        private void ApplyStun(CombatEntity receiver)
        {
            if (!useStun || receiver == null || stunDuration <= 0.0f)
                return;

            receiver.SendMessage("Stun", stunDuration, SendMessageOptions.DontRequireReceiver);
        }

        private bool CanHit(HurtBox hurtBox)
        {
            if (allowMultiHit == false)
                return checkList.Contains(hurtBox) == false;

            if (lastHitTimes.TryGetValue(hurtBox, out float lastHitTime) == false)
                return true;

            return Time.time - lastHitTime >= multiHitInterval;
        }

        private void RegisterHit(HurtBox hurtBox)
        {
            if (allowMultiHit)
            {
                lastHitTimes[hurtBox] = Time.time;
                return;
            }

            checkList.Add(hurtBox);
        }

        private void PlayImpactFeedbackOnce()
        {
            if (impactFeedbackPlayed)
                return;

            impactFeedbackPlayed = true;
            ResolveFeedbackControllers();

            if (useHitStop && hitStopController != null)
                hitStopController.Play(hitStopDuration);

            if (useCameraShake && cameraImpulseController != null)
                cameraImpulseController.Play();
        }
    }
}
