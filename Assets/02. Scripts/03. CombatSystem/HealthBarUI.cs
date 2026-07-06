using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Arcanist
{
    public class HealthBarUI : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private Transform hpBar;
        [SerializeField] private Transform hpDelayBar;
        [SerializeField] private float maxHpBarScaleX = 2.5f;
        [SerializeField] private float hpDelayStartTime = 0.4f;
        [SerializeField] private float hpDelaySpeed = 3.0f;

        [Header("Heal")]
        [SerializeField] private Image hpBarImage;
        [SerializeField] private Color healColor = Color.green;
        [SerializeField] private float healFlashDuration = 0.5f;

        private Color defaultHpBarColor;
        private Coroutine hpDelayCoroutine;
        private Coroutine healFlashCoroutine;

        private void Awake()
        {
            if (hpBarImage != null)
                defaultHpBarColor = hpBarImage.color;
        }

        public void Initialize(int currentHp, int maxHp)
        {
            UpdateHpBar(currentHp, maxHp);
            UpdateDelayBar(currentHp, maxHp);
        }

        public void ApplyDamage(int beforeHp, int afterHp, int maxHp)
        {
            float beforeRatio = GetHpRatio(beforeHp, maxHp);
            float afterRatio = GetHpRatio(afterHp, maxHp);

            UpdateHpBar(afterHp, maxHp);
            PlayDamageBarEffect(beforeRatio, afterRatio);
        }

        public void ApplyHeal(int currentHp, int maxHp)
        {
            UpdateHpBar(currentHp, maxHp);
            UpdateDelayBar(currentHp, maxHp);
            PlayHealEffect();
        }

        public void UpdateHpBar(int currentHp, int maxHp)
        {
            if (hpBar == null)
                return;

            float hpRatio = GetHpRatio(currentHp, maxHp);

            Vector3 scale = hpBar.localScale;
            scale.x = hpRatio * maxHpBarScaleX;
            hpBar.localScale = scale;
        }

        private void UpdateDelayBar(int currentHp, int maxHp)
        {
            if (hpDelayBar == null)
                return;

            float hpRatio = GetHpRatio(currentHp, maxHp);

            Vector3 scale = hpDelayBar.localScale;
            scale.x = hpRatio * maxHpBarScaleX;
            hpDelayBar.localScale = scale;
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

            while (hpDelayBar != null && hpDelayBar.localScale.x > targetScale)
            {
                Vector3 scale = hpDelayBar.localScale;
                scale.x = Mathf.MoveTowards(scale.x, targetScale, hpDelaySpeed * Time.deltaTime);
                hpDelayBar.localScale = scale;

                yield return null;
            }

            hpDelayCoroutine = null;
        }

        private void PlayHealEffect()
        {
            if (hpBarImage == null)
                return;

            if (healFlashCoroutine != null)
                StopCoroutine(healFlashCoroutine);

            healFlashCoroutine = StartCoroutine(HealBarFlashCoroutine());
        }

        private IEnumerator HealBarFlashCoroutine()
        {
            hpBarImage.color = healColor;

            float elapsedTime = 0.0f;

            while (elapsedTime < healFlashDuration)
            {
                float normalizedTime = healFlashDuration <= 0.0f
                    ? 1.0f
                    : elapsedTime / healFlashDuration;

                hpBarImage.color = Color.Lerp(healColor, defaultHpBarColor, normalizedTime);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            hpBarImage.color = defaultHpBarColor;
            healFlashCoroutine = null;
        }

        private float GetHpRatio(int currentHp, int maxHp)
        {
            if (maxHp <= 0)
                return 0.0f;

            return Mathf.Clamp01((float)currentHp / maxHp);
        }
    }
}