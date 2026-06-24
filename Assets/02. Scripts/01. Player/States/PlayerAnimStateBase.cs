using UnityEngine;

namespace Arcanist
{
    public abstract class PlayerAnimStateBase
    {        
        public const float INPUT_RESET_TIME = 0.35f;
        public const float COMBO_INPUT_END_TIME = 0.75f;

        protected PlayerController Owner { get; }
        protected Animator Animator => Owner.Animator;

        protected PlayerAnimStateBase(PlayerController owner)
        {
            Owner = owner;
        }

        public virtual void Enter() { }

        public abstract void UpdateState(AnimatorStateInfo stateInfo);
        
        public virtual void Exit() { }
    }
}
