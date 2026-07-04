using UnityEngine;

namespace Arcanist
{
    public class ProjectileController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private float lifeTime = 3f;
        [SerializeField] private bool moveAfterStartAnimation = true;
        [SerializeField] private bool destroyOnHit = true;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Collider2D hitCollider;
        [SerializeField] private Rigidbody2D rigidBody;

        [Header("Animator Parameters")]
        [SerializeField] private string explodeTrigger = "Explode";

        private Vector2 direction = Vector2.right;
        private bool isMoving;
        private bool isExploding;
        private float lifeTimer;
        private HitBox hitBox;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();

            if (rigidBody == null)
                rigidBody = GetComponent<Rigidbody2D>();

            if (hitBox == null)
                hitBox = GetComponentInChildren<HitBox>();
        }

        private void OnEnable()
        {
            lifeTimer = 0f;
            isExploding = false;
            isMoving = moveAfterStartAnimation == false;

            if (hitCollider != null)
                hitCollider.enabled = true;
        }

        private void Update()
        {
            if (isExploding) return;

            lifeTimer += Time.deltaTime;

            if (lifeTimer >= lifeTime)
            {
                Explode();
                return;
            }
        }

        private void FixedUpdate()
        {
            if (isExploding || isMoving == false) return;

            Move();
        }

        public void Initialize(Vector2 moveDirection, CombatEntity owner)
        {
            if (hitBox != null)
                hitBox.Initialize(owner);

            if (moveDirection.sqrMagnitude <= 0f)
                moveDirection = Vector2.right;

            direction = moveDirection.normalized;
            transform.right = direction;

            lifeTimer = 0f;
            isExploding = false;

            if (hitCollider != null)
                hitCollider.enabled = true;
        }

        public void StartMove()
        {
            if (isExploding)
                return;

            isMoving = true;
        }

        public void Explode()
        {
            if (isExploding)
                return;

            if (destroyOnHit)
            {
                DestroySelf();
                return;
            }

            isExploding = true;
            isMoving = false;

            if (hitCollider != null)
                hitCollider.enabled = false;

            if (rigidBody != null)
                rigidBody.linearVelocity = Vector2.zero;

            if (animator != null && string.IsNullOrEmpty(explodeTrigger) == false)
                animator.SetTrigger(explodeTrigger);
            else
                DestroySelf();
        }

        public void DestroySelf()
        {
            CardEffectSpawner.Despawn(gameObject);
        }

        private void Move()
        {
            Vector2 movement = direction * speed * Time.fixedDeltaTime;

            if (rigidBody != null)
                rigidBody?.MovePosition(rigidBody.position + movement);
            else
                transform.position += (Vector3)movement;
        }
    }
}
