using UnityEngine;

namespace Arcanist
{
    public class ParryState : PlayerAnimStateBase
    {
        private const float PARRY_WINDOW = 0.8f;

        public ParryState(PlayerController owner) : base(owner)
        {
            
        }

        public override void Enter()
        {
            Owner.IsParrying = true;
            Owner.OpenParryWindow();
            Owner.StopMovement();
        }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            if (stateInfo.normalizedTime >= PARRY_WINDOW)
            {
                Owner.CloseParryWindow();
            }
        }

        public override void Exit()
        {
            Owner.IsParrying = false;
            Owner.CloseParryWindow();
        }
    }
}