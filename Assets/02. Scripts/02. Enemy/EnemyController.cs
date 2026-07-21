using UnityEngine;

namespace Arcanist
{
    public class EnemyController : MonoBehaviour
    {
        public static readonly int IS_MOVE = Animator.StringToHash("IsMove");
        public static readonly int ATTACK = Animator.StringToHash("Attack");
        public static readonly int ATTACK_INDEX = Animator.StringToHash("AttackIndex");
        public static readonly int IS_HURT = Animator.StringToHash("IsHurt");
        public static readonly int DIE = Animator.StringToHash("Die");

        private enum EnemyState
        {
            Idle,
            Patrol,
            Attack
        }

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
        public bool isDead { get; private set; }
        public bool IsDead => isDead;
        public Transform TargetPoint => targetPoint != null ? targetPoint : movement.ActorTransform;
        public float MoveSpeed => moveSpeed;
        public float TraceRange => traceRange;
        public float AttackRange => attackRange;
        public float StoppingDistance => stoppingDistance;
        public float AttackCooldown => attackCooldown;
        public Vector3 PointA { get; private set; }
        public Vector3 PointB { get; private set; }
        public Vector3 GoalPoint { get; private set; }

        private EnemyMovement movement;
        private EnemyCombat combat;
        private EnemyState currentState = EnemyState.Idle;
        private float hurtEndTime;
        private float stunEndTime;
        private float stateElapsedTime;
        private float nextSensingTime;
        private bool isHurt;
        private bool isStunned;
        private bool isMovingToPointB = true;
        private bool canTraceTarget;
        private bool targetInAttackRange;

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
            combat = new EnemyCombat(Animator, Enemy, projectilePrefab, firePoint, attackCooldown);
            InitializePatrolPoints();
        }

        private void OnEnable()
        {
            isDead = false;
            isHurt = false;
            isStunned = false;
            stunEndTime = 0.0f;
            movement.Reset();
            combat.Reset();
            currentState = EnemyState.Idle;
            stateElapsedTime = 0.0f;
            RefreshSensing();
            nextSensingTime = Time.time + Random.Range(0.0f, Mathf.Max(0.02f, sensingInterval));

            if (Animator != null)
                Animator.SetBool(IS_HURT, false);

            EnemyManager.Register(this);
        }

        private void OnDisable()
        {
            EnemyManager.Unregister(this);
        }

        private void Update()
        {
            if (isDead)
                return;

            if (isStunned)
            {
                UpdateStunState();
                return;
            }

            if (isHurt)
            {
                UpdateHurtState();
                return;
            }

            UpdateSensingIfNeeded();
            stateElapsedTime += Time.deltaTime;
            UpdateState();
        }

        private void UpdateSensingIfNeeded()
        {
            if (Time.time < nextSensingTime)
                return;

            RefreshSensing();
            nextSensingTime = Time.time + Mathf.Max(0.02f, sensingInterval);
        }

        private void RefreshSensing()
        {
            if (Target == null)
            {
                canTraceTarget = false;
                targetInAttackRange = false;
                return;
            }

            Vector3 currentPosition = movement.Position;
            Vector3 targetPosition = Target.position;
            canTraceTarget = IsInRange(currentPosition, targetPosition, traceRange)
                && IsOnSameFloor(currentPosition, targetPosition);

            targetPosition.y = currentPosition.y;
            targetInAttackRange = IsInRange(currentPosition, targetPosition, attackRange);
        }

        private void InitializePatrolPoints()
        {
            PointA = movement.Position;
            PointB = patrolPoint != null ? patrolPoint.position : PointA;
            PointB = new Vector3(PointB.x, PointA.y, PointB.z);
            GoalPoint = PointB;
            isMovingToPointB = true;
        }

        private void UpdateState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    UpdateIdleState();
                    break;
                case EnemyState.Patrol:
                    UpdatePatrolState();
                    break;
                case EnemyState.Attack:
                    UpdateAttackState();
                    break;
            }
        }

        private void UpdateIdleState()
        {
            movement.Stop();

            if (canTraceTarget)
                ChangeState(EnemyState.Attack);
            else if (stateElapsedTime >= idleWaitTime)
                ChangeState(EnemyState.Patrol);
        }

        private void UpdatePatrolState()
        {
            if (canTraceTarget)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            if (movement.IsNear(GoalPoint))
            {
                SwitchPatrolGoal();
                ChangeState(EnemyState.Idle);
                return;
            }

            movement.MoveTo(GoalPoint, Time.deltaTime);
        }

        private void UpdateAttackState()
        {
            if (!canTraceTarget || Target == null)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            if (targetInAttackRange)
            {
                movement.Stop();
                movement.LookAt(Target.position);
                combat.TryAttack();
                return;
            }

            movement.MoveTo(Target.position, Time.deltaTime);
        }

        private void ChangeState(EnemyState nextState)
        {
            if (currentState == nextState)
                return;

            currentState = nextState;
            stateElapsedTime = 0.0f;
            movement.Stop();
        }

        private void SwitchPatrolGoal()
        {
            isMovingToPointB = !isMovingToPointB;
            GoalPoint = isMovingToPointB ? PointB : PointA;
        }

        private void UpdateHurtState()
        {
            if (Time.time >= hurtEndTime)
                EndHurt();
        }

        private void UpdateStunState()
        {
            movement.Stop();

            if (Time.time >= stunEndTime)
                EndStun();
        }

        public bool HasTargetInTraceRange()
        {
            return Target != null && IsInRange(movement.Position, Target.position, traceRange);
        }

        public bool HasTargetOnSameFloor()
        {
            return Target != null && IsOnSameFloor(movement.Position, Target.position);
        }

        public bool HasTargetInAttackRange()
        {
            if (Target == null)
                return false;

            Vector3 currentPosition = movement.Position;
            Vector3 targetPosition = Target.position;
            targetPosition.y = currentPosition.y;
            return IsInRange(currentPosition, targetPosition, attackRange);
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
            if (isDead || isStunned)
                return;

            isHurt = true;
            hurtEndTime = Time.time + hurtDuration;
            movement.Stop();
            ApplyHurtKnockback();

            if (Animator != null)
                Animator.SetBool(IS_HURT, true);
        }

        public void Stun(float duration)
        {
            if (isDead || !canBeStunned || duration <= 0.0f)
                return;

            isStunned = true;
            isHurt = false;
            stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);
            movement.Stop();

            if (Animator != null)
                Animator.SetBool(IS_HURT, true);
        }

        public void FireProjectile()
        {
            combat.FireProjectile(Target, isDead);
        }

        private void ApplyHurtKnockback()
        {
            float direction = Target != null
                ? Mathf.Sign(movement.Position.x - Target.position.x)
                : -Mathf.Sign(movement.ActorTransform.localScale.x);
            movement.ApplyHorizontalVelocity(direction, hurtKnockbackSpeed);
        }

        private void EndHurt()
        {
            isHurt = false;
            movement.Stop();

            if (Animator != null)
                Animator.SetBool(IS_HURT, false);
        }

        private void EndStun()
        {
            isStunned = false;
            movement.Stop();

            if (Animator != null)
                Animator.SetBool(IS_HURT, false);

            ChangeState(EnemyState.Idle);
        }

        public void Dead()
        {
            if (isDead)
                return;

            isDead = true;
            EnemyManager.Unregister(this);
            Target = null;
            isHurt = false;
            isStunned = false;
            movement.Stop();

            if (Animator != null)
            {
                Animator.SetBool(IS_HURT, false);
                Animator.SetTrigger(DIE);
            }
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        public void SetTarget(Transform target)
        {
            Target = target;
            RefreshSensing();
        }

        private static bool IsInRange(Vector3 from, Vector3 to, float range)
        {
            return (to - from).sqrMagnitude <= range * range;
        }

        private static bool IsOnSameFloor(Vector3 from, Vector3 to)
        {
            const float FLOOR_TOLERANCE = 1.0f;
            return Mathf.Abs(from.y - to.y) <= FLOOR_TOLERANCE;
        }
    }
}
