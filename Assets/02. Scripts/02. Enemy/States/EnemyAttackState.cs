using UnityEngine;

namespace Arcanist
{
    public class EnemyAttackState : EnemyStateBase
    {
        private float attackTimer;
        public EnemyAttackState(EnemyController owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.StopMove();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            Owner.StopMove();

            if (Owner.Target != null)
                Owner.LookAt(Owner.Target.position);
        }

        public override void Exit()
        {
            Owner.StopMove();
        }
    }
}
