using UnityEngine;

namespace Arcanist
{
    public class AttackEndState : PlayerAnimStateBase
    {
        public AttackEndState(PlayerController owner) : base(owner) 
        {
            
        }

        public override void Enter()
        {

        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            Owner.HandleMovementWithoutAnimation(0.35f);
            
            if (stateInfo.normalizedTime < INPUT_RESET_TIME)
            {
                Animator.SetBool(PlayerController.IS_ATTACK, false);
            }
        }
    }
}
