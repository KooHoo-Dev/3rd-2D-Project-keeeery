using System.Collections;
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

            StartCoroutine(HitEffectCoroutine());
        }

        protected IEnumerator HitEffectCoroutine()
        {
            float waitTime = 0.0f;

            while(true)
            {
                if (waitTime > hitEffectDuration) break;

                float blendValue = Mathf.Lerp(HIT_EFFECT_VALUE, 0.0f, waitTime / hitEffectDuration);
                
                instanceMaterial.SetFloat(HIT_EFFECT_BLEND, blendValue);

                yield return null; // 한프레임 대기
                waitTime += Time.deltaTime; // 대기한만큼 시간 더해줌
            }

            instanceMaterial.SetFloat(HIT_EFFECT_BLEND, 0.0f);
        }
    }
}
