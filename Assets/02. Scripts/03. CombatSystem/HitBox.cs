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

        private CombatEntity Owner { get; set; }
        private HitTargetTracker targetTracker;
        private HitResolver hitResolver;
        private bool impactFeedbackPlayed;

        private void Awake()
        {
            ResolveReferences();
            BuildRuntimeServices();
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureRuntimeServices();
            ResetRuntimeState();
        }

        public void Initialize(CombatEntity owner)
        {
            Owner = owner;
            EnsureRuntimeServices();
            ResetRuntimeState();
        }

        private void ResolveReferences()
        {
            if (Owner == null)
                Owner = GetComponentInParent<CombatEntity>();

            if (projectileController == null)
                projectileController = GetComponentInParent<ProjectileController>();
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

            if (IsUnavailableTarget(receiver))
                return;

            EnsureRuntimeServices();
            if (targetTracker.TryRegister(hurtBox) == false)
                return;

            HitResolution resolution = hitResolver.Resolve(
                sender,
                receiver,
                collision,
                transform,
                AttackInfo);

            if (resolution.Status != HitResolutionStatus.Applied)
                return;

            PlayImpactFeedback(hurtBox, resolution.Position);

            if (explodeProjectileOnHit && projectileController != null)
                projectileController.Explode();
        }

        private static bool IsUnavailableTarget(CombatEntity receiver)
        {
            ICombatTarget target = receiver.GetComponent<ICombatTarget>();
            if (target == null)
                target = receiver.GetComponentInParent<ICombatTarget>();
            if (target == null)
                target = receiver.GetComponentInChildren<ICombatTarget>();

            return target != null && (target.IsTargetable == false || target.IsDead);
        }

        private void EnsureRuntimeServices()
        {
            if (targetTracker == null || hitResolver == null)
                BuildRuntimeServices();
        }

        private void BuildRuntimeServices()
        {
            targetTracker = new HitTargetTracker(allowMultiHit, multiHitInterval);
            hitResolver = new HitResolver(new HitEffectSettings(
                useKnockback,
                knockbackForce,
                knockbackFromHitBoxCenter,
                resetTargetVelocityBeforeKnockback,
                useStun,
                stunDuration));
        }

        private void ResetRuntimeState()
        {
            targetTracker.Reset();
            impactFeedbackPlayed = false;
        }

        private void PlayImpactFeedback(HurtBox hurtBox, Vector3 hitPosition)
        {
            hurtBox.PlayHitFx(hitPosition);

            if (impactFeedbackPlayed)
                return;

            impactFeedbackPlayed = true;
            ResolveFeedbackControllers();

            if (useHitStop && hitStopController != null)
                hitStopController.Play(hitStopDuration);

            if (useCameraShake && cameraImpulseController != null)
                cameraImpulseController.Play();
        }

        private void ResolveFeedbackControllers()
        {
            if (hitStopController == null)
                hitStopController = FindFirstObjectByType<HitStopController>();

            if (cameraImpulseController == null)
                cameraImpulseController = FindFirstObjectByType<CameraImpulseController>();
        }
    }
}
