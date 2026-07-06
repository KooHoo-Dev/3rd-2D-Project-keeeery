using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class GolemMeleeState : BossPatternState
    {
        [SerializeField] private Collider2D meleeHitBox;
        [SerializeField] private float duration = 1.2f;

        private Coroutine coroutine;

        protected override void OnEnable()
        {
            if (BossController == null)
                return;

            meleeHitBox.enabled = false;
            BossController.Animator.Play("Boss_Golem_Attack");

            coroutine = StartCoroutine(Coroutine());
        }

        protected override void OnDisable()
        {
            if (BossController == null)
                return;
                
            if (coroutine != null)
                StopCoroutine(coroutine);

            meleeHitBox.enabled = false;
        }

        private IEnumerator Coroutine()
        {
            yield return new WaitForSeconds(duration);
            BossController.ChangeState(typeof(GolemIdleState));
        }
    }
}