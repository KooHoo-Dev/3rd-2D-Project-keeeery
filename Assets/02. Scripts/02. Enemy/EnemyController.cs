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

        [Header("Attack")]
        [SerializeField] private float attackCooldown = 2.0f;

        [Header("Hurt")]
        [SerializeField] private float hurtDuration = 0.15f;
        [SerializeField] private float hurtKnockbackSpeed = 1.5f;

        [Header("Patrol")]
        [SerializeField] private Transform patrolPoint;

        [Header("Facing")]
        [SerializeField] private bool isFacingRight = true;

        [Header("Targeting")]
        [SerializeField] private EnemyGrade grade = EnemyGrade.Normal;
        [SerializeField] private Transform targetPoint;

        [field: SerializeField] public Transform Target { get; private set; }

        public Animator Animator { get; private set; }
        public Enemy Enemy { get; private set; }
        public Rigidbody2D Rigidbody { get; private set; }
        public EnemyGrade Grade => grade;
        public bool isDead;
        public bool IsDead => isDead;
        public Transform TargetPoint => targetPoint != null ? targetPoint : ActorTransform;

        public float MoveSpeed => moveSpeed;
        public float TraceRange => traceRange;
        public float AttackRange => attackRange;
        public float StoppingDistance => stoppingDistance;
        public float AttackCooldown => attackCooldown;

        public Vector3 PointA { get; private set; }
        public Vector3 PointB { get; private set; }
        public Vector3 GoalPoint { get; private set; }

        private Transform ActorTransform => Rigidbody != null ? Rigidbody.transform : transform;
        private Vector3 CurrentPosition => Rigidbody != null ? (Vector3)Rigidbody.position : transform.position;

        private EnemyState currentState = EnemyState.Idle;
        private Vector3 originalScale;
        private float nextAttackTime;
        private float hurtEndTime;
        private float stateElapsedTime;
        private bool isHurt;
        private bool isMovingToPointB = true;

        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            Enemy = GetComponentInChildren<Enemy>();
            Rigidbody = GetComponentInChildren<Rigidbody2D>();
            originalScale = ActorTransform.localScale;

            InitializePatrolPoints();
        }

        private void OnEnable()
        {
            isDead = false;
            ChangeState(EnemyState.Idle);
            EnemyManager.Register(this);
        }

        private void OnDisable()
        {
            EnemyManager.Unregister(this);
        }

        private void Update()
        {
            if (isDead) return;

            if (isHurt)
            {
                UpdateHurtState();
                return;
            }

            stateElapsedTime += Time.deltaTime;
            UpdateState();
        }

        private void InitializePatrolPoints()
        {
            PointA = CurrentPosition;

            Vector3 pointB = patrolPoint != null ? patrolPoint.position : CurrentPosition;
            pointB.y = CurrentPosition.y;
            PointB = pointB;

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
            StopMove();

            if (CanTraceTarget())
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            if (stateElapsedTime >= idleWaitTime)
                ChangeState(EnemyState.Patrol);
        }

        private void UpdatePatrolState()
        {
            if (CanTraceTarget())
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            if (IsNearGoal(GoalPoint))
            {
                SwitchPatrolGoal();
                ChangeState(EnemyState.Idle);
                return;
            }

            MoveTo(GoalPoint);
        }

        private void UpdateAttackState()
        {
            if (!CanTraceTarget())
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            if (HasTargetInAttackRange())
            {
                StopMove();
                LookAt(Target.position);
                TryAttack();
                return;
            }

            MoveTo(Target.position);
        }

        private void UpdateHurtState()
        {
            if (Time.time < hurtEndTime) return;

            EndHurt();
        }

        private void ChangeState(EnemyState nextState)
        {
            if (currentState == nextState) return;

            currentState = nextState;
            stateElapsedTime = 0.0f;
            StopMove();
        }

        private bool CanTraceTarget()
        {
            return HasTargetInTraceRange() && HasTargetOnSameFloor();
        }

        public bool HasTargetInTraceRange()
        {
            return Target != null && IsInRange(CurrentPosition, Target.position, traceRange);
        }

        public bool HasTargetOnSameFloor()
        {
            if (Target == null) return false;

            return CompareFloor(CurrentPosition, Target.position) == 0;
        }

        public bool HasTargetInAttackRange()
        {
            if (Target == null) return false;

            Vector3 currentPosition = CurrentPosition;
            Vector3 adjustedTargetPosition = Target.position;
            adjustedTargetPosition.y = currentPosition.y;

            return IsInRange(currentPosition, adjustedTargetPosition, attackRange);
        }

        public bool IsNearGoal(Vector3 goalPosition)
        {
            return Mathf.Abs(goalPosition.x - CurrentPosition.x) <= stoppingDistance;
        }

        private void SwitchPatrolGoal()
        {
            isMovingToPointB = !isMovingToPointB;
            GoalPoint = isMovingToPointB ? PointB : PointA;
        }

        private void TryAttack()
        {
            if (Time.time < nextAttackTime) return;

            nextAttackTime = Time.time + attackCooldown;
            TriggerAttackAnimation();
        }

        public void MoveTo(Vector3 goalPosition)
        {
            if (IsNearGoal(goalPosition))
            {
                StopMove();
                return;
            }

            float moveDirection = UpdateDirection(goalPosition);

            if (Mathf.Approximately(moveDirection, 0.0f))
            {
                StopMove();
                return;
            }

            Animator.SetBool(IS_MOVE, true);

            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = new Vector2(moveDirection * moveSpeed, Rigidbody.linearVelocity.y);
                return;
            }

            transform.Translate(new Vector3(moveDirection, 0.0f, 0.0f) * (moveSpeed * Time.deltaTime));
        }

        public void StopMove()
        {
            Animator.SetBool(IS_MOVE, false);

            if (Rigidbody != null)
                Rigidbody.linearVelocity = new Vector2(0.0f, Rigidbody.linearVelocity.y);
        }

        public void LookAt(Vector3 targetPosition)
        {
            UpdateDirection(targetPosition);
        }

        private float UpdateDirection(Vector3 goalPosition)
        {
            float xDistance = goalPosition.x - CurrentPosition.x;

            if (Mathf.Abs(xDistance) <= stoppingDistance) return 0.0f;

            float moveDirection = Mathf.Sign(xDistance);
            float baseScaleX = Mathf.Abs(originalScale.x) * (isFacingRight ? 1.0f : -1.0f);

            ActorTransform.localScale = new Vector3(
                moveDirection * baseScaleX,
                originalScale.y,
                originalScale.z);

            return moveDirection;
        }

        private void TriggerAttackAnimation()
        {
            Animator.SetInteger(ATTACK_INDEX, Random.Range(0, 2));
            Animator.SetTrigger(ATTACK);
        }

        public void Hurt()
        {
            if (isDead) return;

            isHurt = true;
            hurtEndTime = Time.time + hurtDuration;
            StopMove();
            ApplyHurtKnockback();
            Animator.SetBool(IS_HURT, true);
        }

        private void ApplyHurtKnockback()
        {
            if (Rigidbody == null || hurtKnockbackSpeed <= 0.0f)
                return;

            float direction = Target != null
                ? Mathf.Sign(CurrentPosition.x - Target.position.x)
                : -Mathf.Sign(ActorTransform.localScale.x);

            if (Mathf.Approximately(direction, 0.0f))
                direction = -Mathf.Sign(ActorTransform.localScale.x);

            Rigidbody.linearVelocity = new Vector2(direction * hurtKnockbackSpeed, Rigidbody.linearVelocity.y);
        }

        private void EndHurt()
        {
            isHurt = false;
            StopMove();
            Animator.SetBool(IS_HURT, false);
        }

        public void Dead()
        {
            if (isDead) return;

            isDead = true;
            EnemyManager.Unregister(this);
            isHurt = false;
            StopMove();
            Animator.SetBool(IS_HURT, false);
            Animator.SetTrigger(DIE);
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        public void SetTarget(Transform target)
        {
            Target = target;
        }

        private bool IsInRange(Vector3 from, Vector3 to, float range)
        {
            return Vector3.Distance(from, to) <= range;
        }

        private int CompareFloor(Vector3 a, Vector3 b)
        {
            const float EPSILON = 1.0f;
            float yDistance = a.y - b.y;

            if (Mathf.Abs(yDistance) <= EPSILON) return 0;

            return yDistance > 0.0f ? 1 : -1;
        }
    }
}