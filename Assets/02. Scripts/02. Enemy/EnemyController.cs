using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Arcanist
{
    public class EnemyController : MonoBehaviour
    {
        public static readonly int IS_MOVE = Animator.StringToHash("IsMove");
        public static readonly int ATTACK = Animator.StringToHash("Attack");
        public static readonly int ATTACK_INDEX = Animator.StringToHash("AttackIndex");
        public static readonly int IS_HURT = Animator.StringToHash("IsHurt");
        public static readonly int DIE = Animator.StringToHash("Die");

        private const string ANIM_TAG_ATTACK = "Attack";

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.0f;
        [SerializeField] private float traceRange = 10.0f;
        [SerializeField] private float attackRange = 3.0f;
        [SerializeField] private float stoppingDistance = 0.1f;

        [Header("Attack")]
        [SerializeField] private float attackHitDelay = 0.5f;
        [SerializeField] private float attackCooldown = 2.0f;

        [Header("Hurt")]
        [SerializeField] private float hurtDuration = 0.15f;

        [Header("Patrol")]
        [SerializeField] private Transform patrolPoint;

        [field: SerializeField] public Transform Target { get; private set; }

        public Animator Animator { get; private set; }
        public Enemy Enemy { get; private set; }

        public float MoveSpeed => moveSpeed;
        public float TraceRange => traceRange;
        public float AttackRange => attackRange;
        public float StoppingDistance => stoppingDistance;
        public float AttackHitDelay => attackHitDelay;
        public float AttackCooldown => attackCooldown;

        public Vector3 PointA { get; private set; }
        public Vector3 PointB { get; private set; }
        public Vector3 GoalPoint { get; set; }

        private Vector3 originalScale;
        private float nextAttackTime;
        private float hurtEndTime;
        private bool isHurt;
        private bool isDead;

        private EnemyStateBase currentState;
        private EnemyStateBase defaultState;
        private Dictionary<int, EnemyStateBase> stateDic;

        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            Enemy = GetComponentInChildren<Enemy>();
            originalScale = transform.localScale;

            PointA = transform.position;

            Vector3 pointB = patrolPoint != null ? patrolPoint.position : transform.position;
            pointB.y = transform.position.y;
            PointB = pointB;

            GoalPoint = PointB;

            defaultState = new EnemyPatrolState(this);
            stateDic = new Dictionary<int, EnemyStateBase>
            {
                { Animator.StringToHash(ANIM_TAG_ATTACK), new EnemyAttackState(this) }
            };
        }

        private void OnEnable()
        {
            currentState = null;
        }

        private void Update()
        {
            if (isDead)
                return;

            if (isHurt)
            {
                if (Time.time >= hurtEndTime)
                    EndHurt();
                else
                    return;
            }

            UpdateAnimState();
        }

        private void UpdateAnimState()
        {
            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);

            if (stateDic.TryGetValue(stateInfo.tagHash, out EnemyStateBase nextState) == false)
                nextState = defaultState;

            if (currentState != nextState)
            {
                currentState?.Exit();
                currentState = nextState;
                currentState.Enter();
            }

            currentState.UpdateState(stateInfo);
        }

        public bool HasTargetInTraceRange()
        {
            return Target != null && transform.IsInRange(Target.position, traceRange);
        }

        public bool HasTargetOnSameFloor()
        {
            if (Target == null)
                return false;

            return CompareFloor(transform.position, Target.position) == 0;
        }

        public bool HasTargetInAttackRange()
        {
            if (Target == null)
                return false;

            Vector3 adjustTargetPosition = Target.position;
            adjustTargetPosition.y = transform.position.y;
            return transform.IsInRange(adjustTargetPosition, attackRange);
        }

        public bool IsNearGoal(Vector3 goalPosition)
        {
            Vector3 adjustGoalPoint = transform.position;
            adjustGoalPoint.x = goalPosition.x;
            return transform.IsInRange(adjustGoalPoint, stoppingDistance);
        }

        public bool CanAttack()
        {
            return Time.time >= nextAttackTime;
        }

        public void MarkAttackUsed()
        {
            nextAttackTime = Time.time + attackCooldown;
        }

        public void MoveTo(Vector3 goalPosition)
        {
            Animator.SetBool(IS_MOVE, true);

            float moveDirection = UpdateDirection(goalPosition);
            transform.Translate(new Vector3(moveDirection, 0.0f, 0.0f) * (moveSpeed * Time.deltaTime));
        }

        public void StopMove()
        {
            Animator.SetBool(IS_MOVE, false);
        }

        public void LookAt(Vector3 targetPosition)
        {
            UpdateDirection(targetPosition);
        }

        public void TriggerAttackAnimation()
        {
            Animator.SetInteger(ATTACK_INDEX, Random.Range(0, 2));
            Animator.SetTrigger(ATTACK);
        }

        public void Hurt()
        {
            if (isDead)
                return;

            isHurt = true;
            hurtEndTime = Time.time + hurtDuration;
            StopMove();
            Animator.SetBool(IS_HURT, true);
        }

        private void EndHurt()
        {
            isHurt = false;
            Animator.SetBool(IS_HURT, false);
        }

        private float UpdateDirection(Vector3 goalPosition)
        {
            float dirToGoal = goalPosition.x - transform.position.x;
            float moveDirection = Mathf.Sign(dirToGoal);

            if (Mathf.Approximately(dirToGoal, 0.0f))
                moveDirection = transform.localScale.x >= 0.0f ? 1.0f : -1.0f;

            transform.localScale = new Vector3(
                moveDirection * Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z);

            return moveDirection;
        }

        private int CompareFloor(Vector3 a, Vector3 b)
        {
            const float EPSILON = 1.0f;
            float yDistance = a.y - b.y;

            if (Mathf.Abs(yDistance) <= EPSILON) return 0;
            if (yDistance > 0.0f) return 1;
            return -1;
        }

        public void Dead()
        {
            if (isDead) return;

            isDead = true;
            isHurt = false;
            StopMove();
            Animator.SetBool(IS_HURT, false);
            Animator.SetTrigger(DIE);
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
