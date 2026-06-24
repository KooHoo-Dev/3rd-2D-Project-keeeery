using UnityEngine;

namespace Arcanist
{
    public abstract class EnemyStateBase
    {
        protected EnemyController Owner { get; }
        protected Animator Animator => Owner.Animator;

        protected EnemyStateBase(EnemyController owner)
        {
            Owner = owner;
        }

        public virtual void Enter() { }
        public virtual void UpdateState(AnimatorStateInfo stateInfo) { }
        public virtual void Exit() { }
    }
}