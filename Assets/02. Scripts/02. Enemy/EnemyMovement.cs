using UnityEngine;

namespace Arcanist
{
    public sealed class EnemyMovement
    {
        private readonly Transform fallbackTransform;
        private readonly Rigidbody2D rigidbody;
        private readonly Animator animator;
        private readonly float moveSpeed;
        private readonly float stoppingDistance;
        private readonly bool isFacingRight;
        private readonly Vector3 originalScale;

        private bool isMoving;

        public Transform ActorTransform => rigidbody != null ? rigidbody.transform : fallbackTransform;
        public Vector3 Position => rigidbody != null ? rigidbody.position : fallbackTransform.position;

        public EnemyMovement(
            Transform fallbackTransform,
            Rigidbody2D rigidbody,
            Animator animator,
            float moveSpeed,
            float stoppingDistance,
            bool isFacingRight)
        {
            this.fallbackTransform = fallbackTransform;
            this.rigidbody = rigidbody;
            this.animator = animator;
            this.moveSpeed = moveSpeed;
            this.stoppingDistance = stoppingDistance;
            this.isFacingRight = isFacingRight;
            originalScale = ActorTransform.localScale;
        }

        public bool IsNear(Vector3 position)
        {
            return Mathf.Abs(position.x - Position.x) <= stoppingDistance;
        }

        public void MoveTo(Vector3 goalPosition, float deltaTime)
        {
            float xDistance = goalPosition.x - Position.x;

            if (Mathf.Abs(xDistance) <= stoppingDistance)
            {
                Stop();
                return;
            }

            float direction = Mathf.Sign(xDistance);
            SetFacing(direction);
            SetMoving(true);

            if (rigidbody != null)
            {
                float targetVelocityX = direction * moveSpeed;

                if (!Mathf.Approximately(rigidbody.linearVelocity.x, targetVelocityX))
                    rigidbody.linearVelocity = new Vector2(targetVelocityX, rigidbody.linearVelocity.y);

                return;
            }

            fallbackTransform.Translate(Vector3.right * (direction * moveSpeed * deltaTime));
        }

        public void Stop()
        {
            SetMoving(false);

            if (rigidbody != null && !Mathf.Approximately(rigidbody.linearVelocity.x, 0.0f))
                rigidbody.linearVelocity = new Vector2(0.0f, rigidbody.linearVelocity.y);
        }

        public void LookAt(Vector3 targetPosition)
        {
            float xDistance = targetPosition.x - Position.x;

            if (!Mathf.Approximately(xDistance, 0.0f))
                SetFacing(Mathf.Sign(xDistance));
        }

        public void ApplyHorizontalVelocity(float direction, float speed)
        {
            if (rigidbody == null || speed <= 0.0f)
                return;

            if (Mathf.Approximately(direction, 0.0f))
                direction = -Mathf.Sign(ActorTransform.localScale.x);

            rigidbody.linearVelocity = new Vector2(direction * speed, rigidbody.linearVelocity.y);
        }

        public void Reset()
        {
            isMoving = true;
            Stop();
        }

        private void SetMoving(bool value)
        {
            if (isMoving == value)
                return;

            isMoving = value;

            if (animator != null)
                animator.SetBool(EnemyController.IS_MOVE, value);
        }

        private void SetFacing(float direction)
        {
            float baseScaleX = Mathf.Abs(originalScale.x) * (isFacingRight ? 1.0f : -1.0f);
            float targetScaleX = direction * baseScaleX;
            Vector3 scale = ActorTransform.localScale;

            if (Mathf.Approximately(scale.x, targetScaleX))
                return;

            scale.x = targetScaleX;
            ActorTransform.localScale = scale;
        }
    }
}
