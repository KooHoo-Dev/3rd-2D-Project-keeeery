using UnityEngine;

namespace Arcanist
{
    internal sealed class EnemyStateMachine
    {
        private enum State
        {
            Idle,
            Patrol,
            Attack,
        }

        private readonly EnemyMovement movement;
        private readonly EnemyCombat combat;
        private readonly EnemySensor sensor;
        private readonly float idleWaitTime;

        private State currentState;
        private float stateElapsedTime;
        private bool isMovingToPointB;

        public Vector3 PointA { get; private set; }
        public Vector3 PointB { get; private set; }
        public Vector3 GoalPoint { get; private set; }

        public EnemyStateMachine(
            EnemyMovement movement,
            EnemyCombat combat,
            EnemySensor sensor,
            float idleWaitTime,
            Transform patrolPoint)
        {
            this.movement = movement;
            this.combat = combat;
            this.sensor = sensor;
            this.idleWaitTime = idleWaitTime;
            InitializePatrolPoints(patrolPoint);
        }

        public void Reset()
        {
            currentState = State.Idle;
            stateElapsedTime = 0.0f;
            isMovingToPointB = true;
            GoalPoint = PointB;
            movement.Stop();
        }

        public void Tick(Transform target)
        {
            stateElapsedTime += Time.deltaTime;

            switch (currentState)
            {
                case State.Idle:
                    UpdateIdle();
                    break;
                case State.Patrol:
                    UpdatePatrol();
                    break;
                case State.Attack:
                    UpdateAttack(target);
                    break;
            }
        }

        public void ReturnToIdle()
        {
            ChangeState(State.Idle);
        }

        private void UpdateIdle()
        {
            movement.Stop();

            if (sensor.CanTraceTarget)
                ChangeState(State.Attack);
            else if (stateElapsedTime >= idleWaitTime)
                ChangeState(State.Patrol);
        }

        private void UpdatePatrol()
        {
            if (sensor.CanTraceTarget)
            {
                ChangeState(State.Attack);
                return;
            }

            if (movement.IsNear(GoalPoint))
            {
                SwitchPatrolGoal();
                ChangeState(State.Idle);
                return;
            }

            movement.MoveTo(GoalPoint, Time.deltaTime);
        }

        private void UpdateAttack(Transform target)
        {
            if (sensor.CanTraceTarget == false || target == null)
            {
                ChangeState(State.Idle);
                return;
            }

            if (sensor.IsTargetInAttackRange)
            {
                movement.Stop();
                movement.LookAt(target.position);
                combat.TryAttack();
                return;
            }

            movement.MoveTo(target.position, Time.deltaTime);
        }

        private void ChangeState(State nextState)
        {
            if (currentState == nextState)
                return;

            currentState = nextState;
            stateElapsedTime = 0.0f;
            movement.Stop();
        }

        private void InitializePatrolPoints(Transform patrolPoint)
        {
            PointA = movement.Position;
            PointB = patrolPoint != null ? patrolPoint.position : PointA;
            PointB = new Vector3(PointB.x, PointA.y, PointB.z);
            GoalPoint = PointB;
            isMovingToPointB = true;
        }

        private void SwitchPatrolGoal()
        {
            isMovingToPointB = !isMovingToPointB;
            GoalPoint = isMovingToPointB ? PointB : PointA;
        }
    }
}
