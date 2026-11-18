using UnityEngine;

namespace Arcanist
{
    public class DestroyAfterAnimation : MonoBehaviour
    {
        [SerializeField] private float lifeTimeOverride = 0.0f;
        [SerializeField] private float fallbackLifeTime = 1.0f;

        private void Start()
        {
            if (lifeTimeOverride > 0.0f)
            {
                Destroy(gameObject, lifeTimeOverride);
                return;
            }

            Animator animator = GetComponent<Animator>();

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

                if (clips.Length > 0)
                {
                    float speed = Mathf.Max(0.01f, animator.speed);
                    Destroy(gameObject, clips[0].length / speed);
                    return;
                }
            }

            Destroy(gameObject, fallbackLifeTime);
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}