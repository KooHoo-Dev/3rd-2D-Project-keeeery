using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class CardEffectPoolItem : MonoBehaviour
    {
        [Header("Auto Despawn")]
        [SerializeField] private bool autoDespawn = true;
        [SerializeField] private bool useAnimatorClipLength = true;
        [SerializeField] private float fallbackLifeTime = 2.0f;

        private Coroutine lifeCoroutine;
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnDisable()
        {
            if (lifeCoroutine == null)
                return;

            StopCoroutine(lifeCoroutine);
            lifeCoroutine = null;
        }

        public void RestartLifeTimer()
        {
            if (autoDespawn == false)
                return;

            if (lifeCoroutine != null)
                StopCoroutine(lifeCoroutine);

            lifeCoroutine = StartCoroutine(LifeTimerCoroutine());
        }

        public void DespawnSelf()
        {
            CardEffectSpawner.Despawn(gameObject);
        }

        private IEnumerator LifeTimerCoroutine()
        {
            float lifeTime = ResolveLifeTime();
            yield return new WaitForSeconds(lifeTime);
            DespawnSelf();
        }

        private float ResolveLifeTime()
        {
            if (useAnimatorClipLength && animator != null && animator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

                if (clips.Length > 0)
                {
                    float speed = Mathf.Max(0.01f, animator.speed);
                    return clips[0].length / speed;
                }
            }

            return Mathf.Max(0.01f, fallbackLifeTime);
        }
    }
}
