using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class BossController : MonoBehaviour
    {
        private readonly Dictionary<Type, BossBaseState> stateDic = new();

        private BossBaseState currentState;
        private SpriteRenderer[] spriteRenderers;
        private Vector3 originalScale;
        private bool hasStarted;

        [Header("Start")]
        [SerializeField] private bool startOnStart = true;

        [Header("Facing")]
        [SerializeField] private bool isFacingRight = true;

        [Header("Targeting")]
        [SerializeField] private EnemyGrade grade = EnemyGrade.Boss;
        [SerializeField] private Transform targetPoint;
        [SerializeField] private Transform player;

        [Header("Death Cinematic")]
        [SerializeField] private BossDeathController bossDeathController;

        public Animator Animator { get; private set; }
        private Rigidbody2D Rigidbody { get; set; }
        public Boss Boss { get; private set; }
        public Transform Player => Target != null ? Target : player;
        private Transform Target { get; set; }
        public EnemyGrade Grade => grade;
        public Transform TargetPoint => targetPoint != null ? targetPoint : ActorTransform;
        public bool IsDead { get; private set; }

        private Transform ActorTransform => Rigidbody != null ? Rigidbody.transform : transform;
        private Vector3 CurrentPosition => Rigidbody != null ? Rigidbody.position : transform.position;

        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            Rigidbody = GetComponentInChildren<Rigidbody2D>();
            Boss = GetComponentInChildren<Boss>();

            if (bossDeathController == null)
                bossDeathController = GetComponentInChildren<BossDeathController>();

            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            originalScale = ActorTransform.localScale;

            BossBaseState[] states = GetComponentsInChildren<BossBaseState>(true);

            for (int i = 0; i < states.Length; i++)
            {
                BossBaseState state = states[i];

                stateDic.Add(state.GetType(), state);
                state.Initialize(this);
                state.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            IsDead = false;
            EnemyManager.Register(this);
        }

        private void OnDisable()
        {
            EnemyManager.Unregister(this);
        }

        private void Start()
        {
            if (startOnStart)
                StartBoss();
        }

        public void SetStartOnStart(bool value)
        {
            startOnStart = value;
        }

        public void StartBoss()
        {
            if (hasStarted || IsDead)
                return;

            hasStarted = true;
            ChangeState(typeof(GolemIdleState));
        }

        public void StopBoss()
        {
            hasStarted = false;

            if (currentState != null)
            {
                currentState.gameObject.SetActive(false);
                currentState = null;
            }

            StopMove();
        }

        public void ChangeState(Type stateType)
        {
            if (stateDic.TryGetValue(stateType, out BossBaseState nextState) == false)
            {
                Debug.LogError($"등록되지 않은 보스 상태입니다 : {stateType.Name}");
                return;
            }

            if (currentState != null)
                currentState.gameObject.SetActive(false);

            currentState = nextState;
            currentState.gameObject.SetActive(true);
        }

        public void ChangeAlpha(float alpha)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                Color color = spriteRenderers[i].color;
                color.a = alpha;
                spriteRenderers[i].color = color;
            }
        }

        public void Dead()
        {
            if (IsDead) return;

            IsDead = true;
            hasStarted = false;
            EnemyManager.Unregister(this);
            Target = null;
            StopMove();

            if (bossDeathController != null)
                bossDeathController.Play();

            ChangeState(typeof(GolemDeathState));
        }

        public void SetTarget(Transform target)
        {
            Target = target;
        }

        public void StopMove()
        {
            if (Rigidbody != null)
                Rigidbody.linearVelocity = new Vector2(0.0f, Rigidbody.linearVelocity.y);
        }

        public void LookAt(Vector3 targetPosition)
        {
            float xDistance = targetPosition.x - CurrentPosition.x;

            if (Mathf.Approximately(xDistance, 0.0f))
                return;

            float direction = Mathf.Sign(xDistance);
            float baseScaleX = Mathf.Abs(originalScale.x) * (isFacingRight ? 1.0f : -1.0f);

            ActorTransform.localScale = new Vector3(
                direction * baseScaleX,
                originalScale.y,
                originalScale.z);
        }
    }
}