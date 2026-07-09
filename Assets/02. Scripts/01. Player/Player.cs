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
        public static Player LocalPlayer { get; private set; }

        public override BaseStat BaseStat => Stat;

        [field: SerializeField] public PlayerStat Stat { get; private set; }

        [Header("HP")]
        [SerializeField] private HealthBarUI healthBarUI;

        [Header("Stamina")]
        [SerializeField] private Transform staminaBar;
        [SerializeField] private float maxStaminaBarScaleX = 2.5f;
        [SerializeField] private float maxStamina = 100.0f;

        [Header("Cooldown UI")]
        [SerializeField] private GameObject parryCooldownRoot;
        [SerializeField] private Image parryCooldownFill;

        public AttackInfo AttackInfo;
        public int CurrentHp => Stat.Hp;

        private static readonly int HIT_EFFECT_BLEND = Shader.PropertyToID("_HitEffectBlend");

        [SerializeField] private float hitEffectDuration = 0.2f;
        private const float HIT_EFFECT_VALUE = 0.75f;

        private SpriteRenderer spriteRenderer;
        private Material instanceMaterial;
        private float currentStamina;

        private PlayerController PlayerController { get; set; }

        private void Awake()
        {
            LocalPlayer = this;
            Stat.ResetToFull();
            currentStamina = maxStamina;
            healthBarUI?.Initialize(Stat.Hp, Stat.MaxHp);
            UpdateStaminaBar();

            parryCooldownRoot?.SetActive(false);
            parryCooldownFill.fillAmount = 0.0f;

            PlayerController = GetComponent<PlayerController>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            instanceMaterial = spriteRenderer.material;
        }

        public override void TakeDamage(int damage)
        {
            damage = CalculateFinalTakenDamage(damage);

            int beforeHp = Stat.Hp;

            if (Stat.ApplyDamage(damage))
            {
                if (TryConsumeReviveOnce())
                    return;

                if (healthBarUI != null)
                    healthBarUI.ApplyDamage(beforeHp, Stat.Hp, Stat.MaxHp);

                PlayerController.Dead();
                return;
            }

            PlayerController.Hurt();

            if (healthBarUI != null)
                healthBarUI.ApplyDamage(beforeHp, Stat.Hp, Stat.MaxHp);

            StartCoroutine(HitEffectCoroutine());
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
            if (parryCooldownFill == null) return;

            if (cooldownTime <= 0.0f || remainingTime <= 0.0f)
            {
                parryCooldownFill.fillAmount = 0.0f;
                parryCooldownRoot?.SetActive(false);

                return;
            }

            if (parryCooldownRoot != null && parryCooldownRoot.activeSelf == false)
                parryCooldownRoot.SetActive(true);

            float remainingRatio = Mathf.Clamp01(remainingTime / cooldownTime);

            parryCooldownFill.fillAmount = 1.0f - remainingRatio;
        }

        private bool TryConsumeReviveOnce()
        {
            if (TryGetComponent(out BuffController buffController) == false)
                return false;

            return buffController.TryConsumeReviveOnce(this);
        }

        private int CalculateFinalTakenDamage(int damage)
        {
            if (TryGetComponent(out BuffController buffController))
                damage = buffController.ModifyTakenDamage(damage);

            return Mathf.Max(0, damage);
        }

        public void RestoreHealthTo(int hp)
        {
            int targetHp = Mathf.Clamp(hp, 1, Stat.MaxHp);

            Stat.ResetToFull();

            int damageAmount = Stat.MaxHp - targetHp;
            if (damageAmount > 0)
                Stat.ApplyDamage(damageAmount);

            if (healthBarUI != null)
                healthBarUI.Initialize(Stat.Hp, Stat.MaxHp);
        }
        
        public override void TakeHeal(int heal)
        {
            if (heal <= 0) return;

            Stat.ApplyHeal(heal);

            if (healthBarUI != null)
                healthBarUI.ApplyHeal(Stat.Hp, Stat.MaxHp);
        }
    }
}
