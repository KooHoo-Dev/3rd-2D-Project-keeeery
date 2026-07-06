using System;
using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class Obelisk : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float breakAnimationDuration = 0.6f;

        private static readonly int ACTIVE = Animator.StringToHash("Active");
        private static readonly int BREAK = Animator.StringToHash("Break");

        private Coroutine breakRoutine;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void SetActiveState(bool active)
        {
            if (breakRoutine != null)
            {
                StopCoroutine(breakRoutine);
                breakRoutine = null;
            }

            if (animator == null)
                return;

            animator.ResetTrigger(BREAK);
            animator.SetBool(ACTIVE, active);
        }

        public void PlayBreak(Action onComplete)
        {
            if (breakRoutine != null)
                StopCoroutine(breakRoutine);

            breakRoutine = StartCoroutine(BreakRoutine(onComplete));
        }

        private IEnumerator BreakRoutine(Action onComplete)
        {
            if (animator != null)
                animator.SetTrigger(BREAK);

            if (breakAnimationDuration > 0.0f)
                yield return new WaitForSeconds(breakAnimationDuration);

            breakRoutine = null;
            onComplete?.Invoke();
        }
    }
}
