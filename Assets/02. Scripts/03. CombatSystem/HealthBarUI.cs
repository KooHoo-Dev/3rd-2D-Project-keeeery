using DG.Tweening;
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
        private Sequence hpDelaySequence;
        private Tween healFlashTween;

        private void Awake()
        {
            if (hpBarImage != null)
                defaultHpBarColor = hpBarImage.color;
        }

        private void OnDisable()
        {
            KillTweens();
        }

        public void Initialize(int currentHp, int maxHp)
        {
            KillTweens();
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
            hpDelaySequence?.Kill();
            hpDelaySequence = null;
            UpdateHpBar(currentHp, maxHp);
            UpdateDelayBar(currentHp, maxHp);
            PlayHealEffect();
        }

        public void UpdateHpBar(int currentHp, int maxHp)
        {
            if (hpBar == null) return;

            float hpRatio = GetHpRatio(currentHp, maxHp);

            Vector3 scale = hpBar.localScale;
            scale.x = hpRatio * maxHpBarScaleX;
            hpBar.localScale = scale;
        }

        private void UpdateDelayBar(int currentHp, int maxHp)
        {
            if (hpDelayBar == null) return;

            float hpRatio = GetHpRatio(currentHp, maxHp);

            Vector3 scale = hpDelayBar.localScale;
            scale.x = hpRatio * maxHpBarScaleX;
            hpDelayBar.localScale = scale;
        }

        private void PlayDamageBarEffect(float beforeRatio, float afterRatio)
        {
            if (hpDelayBar == null) return;

            hpDelaySequence?.Kill();

            Vector3 scale = hpDelayBar.localScale;
            scale.x = beforeRatio * maxHpBarScaleX;
            hpDelayBar.localScale = scale;

            float targetScale = afterRatio * maxHpBarScaleX;
            float distance = Mathf.Abs(scale.x - targetScale);
            float duration = hpDelaySpeed <= 0.0f
                ? 0.01f
                : Mathf.Max(0.01f, distance / hpDelaySpeed);

            hpDelaySequence = DOTween.Sequence()
                .AppendInterval(Mathf.Max(0.0f, hpDelayStartTime))
                .Append(hpDelayBar.DOScaleX(targetScale, duration).SetEase(Ease.Linear))
                .OnComplete(() => hpDelaySequence = null);
        }

        private void PlayHealEffect()
        {
            if (hpBarImage == null) return;

            healFlashTween?.Kill();
            hpBarImage.color = healColor;
            healFlashTween = hpBarImage
                .DOColor(defaultHpBarColor, Mathf.Max(0.01f, healFlashDuration))
                .SetEase(Ease.Linear)
                .OnComplete(() => healFlashTween = null);
        }

        private void KillTweens()
        {
            hpDelaySequence?.Kill();
            healFlashTween?.Kill();
            hpDelaySequence = null;
            healFlashTween = null;

            if (hpBarImage != null)
                hpBarImage.color = defaultHpBarColor;
        }

        private float GetHpRatio(int currentHp, int maxHp)
        {
            if (maxHp <= 0) return 0.0f;

            return Mathf.Clamp01((float)currentHp / maxHp);
        }
    }
}
