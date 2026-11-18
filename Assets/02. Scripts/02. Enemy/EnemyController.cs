using UnityEngine;

namespace Arcanist
{
    public class EnemyController : MonoBehaviour, ICombatTarget, IStunnable
    {
        public static readonly int IS_MOVE = Animator.StringToHash("IsMove");
        public static readonly int ATTACK = Animator.StringToHash("Attack");
        public static readonly int ATTACK_INDEX = Animator.StringToHash("AttackIndex");
        public static readonly int IS_HURT = Animator.StringToHash("IsHurt");
        public static readonly int DIE = Animator.StringToHash("Die");

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.0f;
        [SerializeField] private float traceRange = 10.0f;
        [SerializeField] private float attackRange = 3.0f;
        [SerializeField] private float stoppingDistance = 0.1f;

        [Header("State")]
        [SerializeField] private float idleWaitTime = 3.0f;
        [SerializeField, Min(0.02f)] private float sensingInterval = 0.1f;

        [Header("Attack")]
        [SerializeField] private float attackCooldown = 2.0f;

        [Header("Hurt")]
        [SerializeField] private float hurtDuration = 0.15f;
        [SerializeField] private float hurtKnockbackSpeed = 1.5f;

        [Header("Stun")]
        [SerializeField] private bool canBeStunned = true;

        [Header("Patrol")]
        [SerializeField] private Transform patrolPoint;

        [Header("Facing")]
        [SerializeField] private bool isFacingRight = true;

        [Header("Targeting")]
        [SerializeField] private EnemyGrade grade = EnemyGrade.Normal;
        [SerializeField] private Transform targetPoint;

        [Header("Projectile")]
        [SerializeField] private ProjectileController projectilePrefab;
        [SerializeField] private Transform firePoint;

        [field: SerializeField] public Transform Target { get; private set; }

        public Animator Animator { get; private set; }
        public Enemy Enemy { get; private set; }
        public Rigidbody2D Rigidbody { get; private set; }
        public EnemyGrade Grade => grade;
        public bool IsDead { get; private set; }
        public bool isDead => IsDead;
        public bool IsTargetable => this != null && isActiveAndEnabled && IsDead == false;
        public Transform TargetPoint => targetPoint != null ? targetPoint : movement.ActorTransform;
        public float MoveSpeed => moveSpeed;
        public float TraceRange => traceRange;
        public float AttackRange => attackRange;
        public float StoppingDistance => stoppingDistance;
        public float AttackCooldown => attackCooldown;
        public Vector3 PointA => stateMachine.PointA;
        public Vector3 PointB => stateMachine.PointB;
        public Vector3 GoalPoint => stateMachine.GoalPoint;

        private EnemyMovement movement;
        private EnemyCombat combat;
        private EnemySensor sensor;
        private EnemyStateMachine stateMachine;
        private EnemyReaction reaction;

        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            Enemy = GetComponentInChildren<Enemy>();
            Rigidbody = GetComponentInChildren<Rigidbody2D>();

            movement = new EnemyMovement(
                transform,
                Rigidbody,
                Animator,
                moveSpeed,
                stoppingDistance,
                isFacingRight);
            combat = new EnemyCombat(
                Animator,
                Enemy,
                projectilePrefab,
                firePoint,
                attackCooldown);
            sensor = new EnemySensor(
                movement,
                traceRange,
                attackRange,
                sensingInterval);
            stateMachine = new EnemyStateMachine(
                movement,
                combat,
                sensor,
                idleWaitTime,
                patrolPoint);
            reaction = new EnemyReaction(
                movement,
                Animator,
                hurtDuration,
                hurtKnockbackSpeed,
                canBeStunned);
        }

        private void OnEnable()
        {
            IsDead = false;
            movement.Reset();
            combat.Reset();
            reaction.Reset();
            stateMachine.Reset();
            sensor.Reset(Target);
            EnemyManager.Register(this);
        }

        private void OnDisable()
        {
            EnemyManager.Unregister(this);
        }

        private void Update()
        {
            if (IsDead)
                return;

            if (reaction.Tick())
            {
                if (reaction.ConsumeStunEnded())
                    stateMachine.ReturnToIdle();

                return;
            }

            sensor.Tick(Target);
            stateMachine.Tick(Target);
        }

        public bool HasTargetInTraceRange()
        {
            return sensor.IsInTraceRange(Target);
        }

        public bool HasTargetOnSameFloor()
        {
            return sensor.IsOnSameFloor(Target);
        }

        public bool HasTargetInAttackRange()
        {
            return sensor.IsInAttackRange(Target);
        }

        public bool IsNearGoal(Vector3 goalPosition)
        {
            return movement.IsNear(goalPosition);
        }

        public void MoveTo(Vector3 goalPosition)
        {
            movement.MoveTo(goalPosition, Time.deltaTime);
        }

        public void StopMove()
        {
            movement.Stop();
        }

        public void LookAt(Vector3 targetPosition)
        {
            movement.LookAt(targetPosition);
        }

        public void Hurt()
        {
            reaction.Hurt(IsDead, Target);
        }

        public void Stun(float duration)
        {
            reaction.Stun(IsDead, duration);
        }

        public void ApplyStun(float duration)
        {
            Stun(duration);
        }

        public void FireProjectile()
        {
            combat.FireProjectile(Target, IsDead);
        }

        public void Dead()
        {
            if (IsDead)
                return;

            IsDead = true;
            EnemyManager.Unregister(this);
            Target = null;
            reaction.OnDeath();

            if (Animator != null)
                Animator.SetTrigger(DIE);
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        public void SetTarget(Transform target)
        {
            Target = target;
            sensor.Refresh(Target);
        }
    }
}
