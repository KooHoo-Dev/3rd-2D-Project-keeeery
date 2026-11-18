using UnityEngine;

namespace Arcanist
{
    internal sealed class EnemySensor
    {
        private const float FloorTolerance = 1.0f;

        private readonly EnemyMovement movement;
        private readonly float traceRange;
        private readonly float attackRange;
        private readonly float sensingInterval;

        private float nextSensingTime;

        public bool CanTraceTarget { get; private set; }
        public bool IsTargetInAttackRange { get; private set; }

        public EnemySensor(
            EnemyMovement movement,
            float traceRange,
            float attackRange,
            float sensingInterval)
        {
            this.movement = movement;
            this.traceRange = traceRange;
            this.attackRange = attackRange;
            this.sensingInterval = Mathf.Max(0.02f, sensingInterval);
        }

        public void Reset(Transform target)
        {
            Refresh(target);
            nextSensingTime = Time.time + Random.Range(0.0f, sensingInterval);
        }

        public void Tick(Transform target)
        {
            if (Time.time < nextSensingTime)
                return;

            Refresh(target);
            nextSensingTime = Time.time + sensingInterval;
        }

        public void Refresh(Transform target)
        {
            if (target == null)
            {
                CanTraceTarget = false;
                IsTargetInAttackRange = false;
                return;
            }

            Vector3 currentPosition = movement.Position;
            Vector3 targetPosition = target.position;
            CanTraceTarget = IsInRange(currentPosition, targetPosition, traceRange)
                             && IsOnSameFloor(currentPosition, targetPosition);

            targetPosition.y = currentPosition.y;
            IsTargetInAttackRange = IsInRange(currentPosition, targetPosition, attackRange);
        }

        public bool IsInTraceRange(Transform target)
        {
            return target != null && IsInRange(movement.Position, target.position, traceRange);
        }

        public bool IsOnSameFloor(Transform target)
        {
            return target != null && IsOnSameFloor(movement.Position, target.position);
        }

        public bool IsInAttackRange(Transform target)
        {
            if (target == null)
                return false;

            Vector3 currentPosition = movement.Position;
            Vector3 targetPosition = target.position;
            targetPosition.y = currentPosition.y;
            return IsInRange(currentPosition, targetPosition, attackRange);
        }

        private static bool IsInRange(Vector3 from, Vector3 to, float range)
        {
            return (to - from).sqrMagnitude <= range * range;
        }

        private static bool IsOnSameFloor(Vector3 from, Vector3 to)
        {
            return Mathf.Abs(from.y - to.y) <= FloorTolerance;
        }
    }
}
