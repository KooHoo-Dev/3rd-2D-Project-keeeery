using UnityEngine;

namespace Arcanist
{
    public class EnemyPatrolState : EnemyStateBase
    {
        public EnemyPatrolState(EnemyController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.StopMove();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            if (Owner.HasTargetInTraceRange() && Owner.HasTargetOnSameFloor())
            {
                if (Owner.HasTargetInAttackRange())
                {
                    Owner.StopMove();
                    Owner.LookAt(Owner.Target.position);

                    if (Owner.CanAttack())
                    {
                        Owner.MarkAttackUsed();
                        Owner.TriggerAttackAnimation();
                    }

                    return;
                }

                Owner.MoveTo(Owner.Target.position);
                return;
            }

            if (Owner.IsNearGoal(Owner.GoalPoint))
                Owner.GoalPoint = Owner.IsNearGoal(Owner.PointB) ? Owner.PointA : Owner.PointB;

            Owner.MoveTo(Owner.GoalPoint);
        }

        public override void Exit()
        {
            Owner.StopMove();
        }
    }
}