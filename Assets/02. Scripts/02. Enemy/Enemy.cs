using DG.Tweening;
using UnityEngine;

namespace Arcanist
{
    public abstract class Enemy : CombatEntity
    {
        private static readonly int HIT_EFFECT_BLEND = Shader.PropertyToID("_HitEffectBlend");

        [SerializeField] private float hitEffectDuration = 0.2f;
        private const float HIT_EFFECT_VALUE = 0.75f;

        private SpriteRenderer SpriteRenderer { get; set; }
        private Material instanceMaterial { get; set; }
        private Tween hitEffectTween;

        [field:SerializeField] public BaseStat Stat { get; private set; }
        private EnemyController EnemyController { get; set; }

        protected virtual void Awake()
        {
            SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            instanceMaterial = SpriteRenderer.material;

            EnemyController = GetComponent<EnemyController>();
            Stat.ResetToFull();
        }

        protected virtual void Start()
        {

        }

        protected virtual void OnDestroy()
        {
            hitEffectTween?.Kill();

            if (instanceMaterial != null) Destroy(instanceMaterial);
        }

        public override void TakeDamage(int damage)
        {
            if (Stat.ApplyDamage(damage))
            {
                EnemyController.Dead();
                return;
            }
            
            EnemyController.Hurt();

            PlayHitEffect();
        }

        protected void PlayHitEffect()
        {
            if (instanceMaterial == null)
                return;

            hitEffectTween?.Kill();
            instanceMaterial.SetFloat(HIT_EFFECT_BLEND, HIT_EFFECT_VALUE);
            hitEffectTween = DOTween.To(
                    () => instanceMaterial.GetFloat(HIT_EFFECT_BLEND),
                    value => instanceMaterial.SetFloat(HIT_EFFECT_BLEND, value),
                    0.0f,
                    Mathf.Max(0.01f, hitEffectDuration))
                .SetEase(Ease.Linear)
                .OnComplete(() => hitEffectTween = null);
        }
    }
}
