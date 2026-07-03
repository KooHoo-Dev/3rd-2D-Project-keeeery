using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Arcanist
{
    public class ScreenEffectController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image vignetteImage;

        [Header("Colors")]
        [SerializeField] private Color parryColor = new Color(0.62f, 0.31f, 0.93f, 1f);
        [SerializeField] private Color damageColor = new Color(1f, 0.23f, 0.19f, 1f);
        [SerializeField] private Color reviveColor = Color.white;

        [Header("Parry Glow")]
        [SerializeField] private float parryMaxAlpha = 0.35f;
        [SerializeField] private float parryFadeInDuration = 0.05f;
        [SerializeField] private float parryFadeOutDuration = 0.25f;

        [Header("Damage Glow")]
        [SerializeField] private float damageMaxAlpha = 0.5f;
        [SerializeField] private float damageFadeInDuration = 0.03f;
        [SerializeField] private float damageFadeOutDuration = 0.18f;

        [Header("Revive Glow")]
        [SerializeField] private float reviveMaxAlpha = 0.6f;
        [SerializeField] private float reviveFadeInDuration = 0.08f;
        [SerializeField] private float reviveHoldDuration = 0.1f;

        private Coroutine glowRoutine;

        private void Awake()
        {
            SetVignetteAlpha(0f);
        }

        public void PlayParryGlow()
        {
            PlayGlow(parryColor, parryMaxAlpha, parryFadeInDuration, parryFadeOutDuration);
        }

        public void PlayDamageGlow()
        {
            PlayGlow(damageColor, damageMaxAlpha, damageFadeInDuration, damageFadeOutDuration);
        }

        public void PlayReviveGlow(float fadeOutDuration)
        {
            PlayGlow(reviveColor, reviveMaxAlpha, reviveFadeInDuration, Mathf.Max(0.0f, fadeOutDuration), reviveHoldDuration);
        }

        private void PlayGlow(Color color, float maxAlpha, float fadeInDuration, float fadeOutDuration)
        {
            PlayGlow(color, maxAlpha, fadeInDuration, fadeOutDuration, 0.0f);
        }

        private void PlayGlow(Color color, float maxAlpha, float fadeInDuration, float fadeOutDuration, float holdDuration)
        {
            if (vignetteImage == null) return;

            if (glowRoutine != null) StopCoroutine(glowRoutine);

            glowRoutine = StartCoroutine(GlowRoutine(color, maxAlpha, fadeInDuration, fadeOutDuration, holdDuration));
        }

        private IEnumerator GlowRoutine(Color color, float maxAlpha, float fadeInDuration, float fadeOutDuration, float holdDuration)
        {
            vignetteImage.gameObject.SetActive(true);

            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                float normalizedTime = fadeInDuration <= 0.0f ? 1.0f : Mathf.Clamp01(t / fadeInDuration);
                SetVignetteColor(color, Mathf.Lerp(0f, maxAlpha, normalizedTime));
                yield return null;
            }

            if (holdDuration > 0.0f)
                yield return new WaitForSeconds(holdDuration);

            t = 0f;

            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                float normalizedTime = fadeOutDuration <= 0.0f ? 1.0f : Mathf.Clamp01(t / fadeOutDuration);
                SetVignetteColor(color, Mathf.Lerp(maxAlpha, 0f, normalizedTime));
                yield return null;
            }

            SetVignetteAlpha(0f);
            glowRoutine = null;
        }

        private void SetVignetteColor(Color color, float alpha)
        {
            color.a = alpha;
            vignetteImage.color = color;
        }

        private void SetVignetteAlpha(float alpha)
        {
            if (vignetteImage == null) return;

            Color color = vignetteImage.color;
            color.a = alpha;
            vignetteImage.color = color;
        }
    }
}
