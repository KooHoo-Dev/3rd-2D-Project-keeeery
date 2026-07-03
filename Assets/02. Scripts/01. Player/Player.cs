using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Arcanist
{
    [System.Serializable]
    public class PlayerStat : BaseStat
    {
        
    }

    public class Player : CombatEntity
    {
        public static Player LocalPlayer { get; set; }

        public override BaseStat BaseStat => Stat;

        [field: SerializeField] public PlayerStat Stat { get; private set; }

        [SerializeField] private Transform hpBar;
        [SerializeField] private Transform hpDelayBar;
        [SerializeField] private float maxHpBarScaleX = 2.5f;
        [SerializeField] private float hpDelayStartTime = 0.4f;
        [SerializeField] private float hpDelaySpeed = 3.0f;

        [Header("Stamina")]
        [SerializeField] private Transform staminaBar;
        [SerializeField] private float maxStaminaBarScaleX = 2.5f;
        [SerializeField] private float maxStamina = 100.0f;

        [Header("Cooldown UI")]
        [SerializeField] private Image parryCooldownFill;

        public AttackInfo AttackInfo;

        private static readonly int HIT_EFFECT_BLEND = Shader.PropertyToID("_HitEffectBlend");

        [SerializeField] private float hitEffectDuration = 0.2f;
        private const float HIT_EFFECT_VALUE = 0.75f;

        private SpriteRenderer spriteRenderer;
        private Material instanceMaterial;
        private Coroutine hpDelayCoroutine;
        private float currentStamina;

        protected PlayerController PlayerController { get; private set; }

        private void Awake()
        {
            LocalPlayer = this;
            Stat.ResetToFull();
            currentStamina = maxStamina;
            UpdateHpBar();
            UpdateStaminaBar();

            if (hpDelayBar != null)
            {
                Vector3 scale = hpDelayBar.localScale;
                scale.x = maxHpBarScaleX;
                hpDelayBar.localScale = scale;
            }

            parryCooldownFill.fillAmount = 0.0f;

            PlayerController = GetComponent<PlayerController>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            instanceMaterial = spriteRenderer.material;
        }

        public override void TakeDamage(int damage)
        {
            damage = CalculateFinalTakenDamage(damage);

            float beforeHpRatio = (float)Stat.Hp / Stat.MaxHp;

            if (Stat.ApplyDamage(damage))
            {
                UpdateHpBar();
                PlayDamageBarEffect(beforeHpRatio, 0.0f);
                PlayerController.Dead();
                return;
            }

            float afterHpRatio = (float)Stat.Hp / Stat.MaxHp;

            PlayerController.Hurt();
            UpdateHpBar();
            PlayDamageBarEffect(beforeHpRatio, afterHpRatio);

            StartCoroutine(HitEffectCoroutine());
        }
        
        private void PlayDamageBarEffect(float beforeRatio, float afterRatio)
        {
            if (hpDelayBar == null)
                return;

            if (hpDelayCoroutine != null)
                StopCoroutine(hpDelayCoroutine);

            Vector3 scale = hpDelayBar.localScale;
            scale.x = beforeRatio * maxHpBarScaleX;
            hpDelayBar.localScale = scale;

            hpDelayCoroutine = StartCoroutine(DelayHpBarCoroutine(afterRatio));
        }

        private IEnumerator DelayHpBarCoroutine(float targetRatio)
        {
            yield return new WaitForSeconds(hpDelayStartTime);

            float targetScale = targetRatio * maxHpBarScaleX;

            while (hpDelayBar.localScale.x > targetScale)
            {
                Vector3 scale = hpDelayBar.localScale;
                scale.x = Mathf.MoveTowards(scale.x, targetScale, hpDelaySpeed * Time.deltaTime);
                hpDelayBar.localScale = scale;

                yield return null;
            }
        }

        private IEnumerator HitEffectCoroutine()
        {
            float waitTime = 0.0f;

            while (true)
            {
                if (waitTime > hitEffectDuration) break;

                float blendValue = Mathf.Lerp(HIT_EFFECT_VALUE, 0.0f, waitTime / hitEffectDuration);
                instanceMaterial.SetFloat(HIT_EFFECT_BLEND, blendValue);

                yield return null;
                waitTime += Time.deltaTime;
            }

            instanceMaterial.SetFloat(HIT_EFFECT_BLEND, 0.0f);
        }

        private void UpdateHpBar()
        {
            if (Stat.Hp <= 0)
            {
                Vector3 zeroScale = hpBar.localScale;
                zeroScale.x = 0.0f;
                hpBar.localScale = zeroScale;
                return;
            }

            float hpRatio = (float)Stat.Hp / Stat.MaxHp;
            float scaleX = hpRatio * maxHpBarScaleX;

            Vector3 scale = hpBar.localScale;
            scale.x = scaleX;
            hpBar.localScale = scale;
        }

        public bool HasStamina(float amount)
        {
            return currentStamina > 0.0f && currentStamina >= amount;
        }

        public bool TryUseStamina(float amount)
        {
            if (currentStamina <= 0.0f || currentStamina < amount)
            {
                currentStamina = 0.0f;
                UpdateStaminaBar();
                return false;
            }

            currentStamina = Mathf.Max(0.0f, currentStamina - amount);
            UpdateStaminaBar();

            return currentStamina > 0.0f;
        }

        public void RecoverStamina(float amount)
        {
            if (currentStamina >= maxStamina)
                return;

            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            UpdateStaminaBar();
        }

        private void UpdateStaminaBar()
        {
            if (staminaBar == null)
                return;

            float staminaRatio = maxStamina <= 0.0f
                ? 0.0f
                : Mathf.Clamp01(currentStamina / maxStamina);

            Vector3 scale = staminaBar.localScale;
            scale.x = staminaRatio * maxStaminaBarScaleX;
            staminaBar.localScale = scale;
        }

        public void UpdateParryCooldownUI(float remainingTime, float cooldownTime)
        {
            if (parryCooldownFill == null)
                return;

            parryCooldownFill.fillAmount = cooldownTime <= 0.0f
                ? 0.0f
                : Mathf.Clamp01(remainingTime / cooldownTime);
        }

        private int CalculateFinalTakenDamage(int damage)
        {
            if (TryGetComponent(out BuffController buffController))
                damage = buffController.ModifyTakenDamage(damage);

            return Mathf.Max(0, damage);
        }

        public override void TakeHeal(int heal)
        {
            
        }
    }
}

