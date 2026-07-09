using UnityEngine;

namespace Arcanist
{
    public class InitialAnimController : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (animator == null)
                return;

            if (ScreenFadeManager.Instance == null)
                return;

            if (!ScreenFadeManager.Instance.ShouldPlayInitialAnimForCurrentScene())
            {
                animator.enabled = false;
            }
        }
    }
}
