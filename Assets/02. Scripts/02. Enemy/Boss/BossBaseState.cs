using UnityEngine;

namespace Arcanist
{
    public abstract class BossBaseState : MonoBehaviour
    {
        protected BossController BossController { get; private set; }

        public virtual void Initialize(BossController controller)
        {
            BossController = controller;
        }

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
    }

    public abstract class BossPatternState : BossBaseState
    {
    }
}