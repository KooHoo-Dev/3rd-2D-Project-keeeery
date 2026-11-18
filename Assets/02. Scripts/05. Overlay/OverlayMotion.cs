using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Arcanist
{
    internal sealed class OverlayMotion
    {
        private readonly GameObject overlayRoot;
        private readonly CanvasGroup overlayGroup;
        private readonly Light2D globalLight;
        private readonly CanvasGroup fadePanel;
        private readonly PlayerCameraController playerCameraController;
        private readonly float normalLightIntensity;
        private readonly float respawnFadeDuration;
        private readonly float respawnBlackHoldDuration;
        private readonly float respawnCameraWaitTimeout;

        public OverlayMotion(
            GameObject overlayRoot,
            CanvasGroup overlayGroup,
            Light2D globalLight,
            CanvasGroup fadePanel,
            PlayerCameraController playerCameraController,
            float normalLightIntensity,
            float respawnFadeDuration,
            float respawnBlackHoldDuration,
            float respawnCameraWaitTimeout)
        {
            this.overlayRoot = overlayRoot;
            this.overlayGroup = overlayGroup;
            this.globalLight = globalLight;
            this.fadePanel = fadePanel;
            this.playerCameraController = playerCameraController;
            this.normalLightIntensity = normalLightIntensity;
            this.respawnFadeDuration = respawnFadeDuration;
            this.respawnBlackHoldDuration = respawnBlackHoldDuration;
            this.respawnCameraWaitTimeout = respawnCameraWaitTimeout;
        }

        public IEnumerator Fade(
            float targetAlpha,
            float targetLightIntensity,
            float duration,
            bool hideAfterFade,
            Action hideWindows)
        {
            float startAlpha = overlayGroup.alpha;
            float startLightIntensity = globalLight.intensity;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0.0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = SmoothStep(elapsed / safeDuration);
                overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time);
                globalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, time);
                yield return null;
            }

            overlayGroup.alpha = targetAlpha;
            globalLight.intensity = targetLightIntensity;

            if (hideAfterFade)
            {
                hideWindows?.Invoke();
                overlayRoot.SetActive(false);
            }
        }

        public IEnumerator PlayDeath(
            float deathLightIntensity,
            float deathFadeDuration,
            float overlayFadeDuration)
        {
            yield return FadeLight(deathLightIntensity, deathFadeDuration);
            yield return FadeCanvasGroup(overlayGroup, overlayGroup.alpha, 1.0f, overlayFadeDuration);
        }

        public IEnumerator PlayRespawn(Action onBlackScreen, Action hideWindows)
        {
            fadePanel.gameObject.SetActive(true);
            fadePanel.transform.SetAsLastSibling();
            fadePanel.alpha = 0.0f;

            yield return FadeCanvasGroup(fadePanel, 0.0f, 1.0f, respawnFadeDuration);

            ResetOverlay(hideWindows);
            onBlackScreen?.Invoke();

            float waitStartTime = Time.unscaledTime;
            while (playerCameraController != null && playerCameraController.IsAtTarget == false)
            {
                if (Time.unscaledTime - waitStartTime >= respawnCameraWaitTimeout)
                    break;

                yield return null;
            }

            yield return new WaitForSecondsRealtime(respawnBlackHoldDuration);
            yield return FadeCanvasGroup(fadePanel, 1.0f, 0.0f, respawnFadeDuration);

            fadePanel.alpha = 0.0f;
            fadePanel.gameObject.SetActive(false);
        }

        public void ResetOverlay(Action hideWindows)
        {
            hideWindows?.Invoke();
            overlayGroup.alpha = 0.0f;
            overlayRoot.SetActive(false);
            globalLight.intensity = normalLightIntensity;
        }

        private IEnumerator FadeLight(float targetIntensity, float duration)
        {
            float startIntensity = globalLight.intensity;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0.0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float time = SmoothStep(elapsed / safeDuration);
                globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, time);
                yield return null;
            }

            globalLight.intensity = targetIntensity;
        }

        private static IEnumerator FadeCanvasGroup(
            CanvasGroup group,
            float from,
            float to,
            float duration)
        {
            group.alpha = from;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0.0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, SmoothStep(elapsed / safeDuration));
                yield return null;
            }

            group.alpha = to;
        }

        private static float SmoothStep(float value)
        {
            return Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(value));
        }
    }
}
