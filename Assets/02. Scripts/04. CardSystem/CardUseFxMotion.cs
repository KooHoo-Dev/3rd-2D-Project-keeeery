using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    internal readonly struct CardUseFxSettings
    {
        public readonly float AbsorbDuration;
        public readonly float StartScale;
        public readonly float EndScale;

        public CardUseFxSettings(float absorbDuration, float startScale, float endScale)
        {
            AbsorbDuration = absorbDuration;
            StartScale = startScale;
            EndScale = endScale;
        }
    }

    internal sealed class CardUseFxMotion
    {
        private readonly MonoBehaviour coroutineHost;
        private readonly GameObject prefab;
        private readonly Transform parentOverride;
        private readonly Transform absorbTarget;
        private readonly Camera absorbTargetCamera;
        private readonly CardUseFxSettings settings;
        private readonly List<GameObject> activeEffects = new();

        public CardUseFxMotion(
            MonoBehaviour coroutineHost,
            GameObject prefab,
            Transform parentOverride,
            Transform absorbTarget,
            Camera absorbTargetCamera,
            CardUseFxSettings settings)
        {
            this.coroutineHost = coroutineHost;
            this.prefab = prefab;
            this.parentOverride = parentOverride;
            this.absorbTarget = absorbTarget;
            this.absorbTargetCamera = absorbTargetCamera;
            this.settings = settings;
        }

        public void Spawn(RectTransform card)
        {
            if (prefab == null || card == null)
                return;

            Transform parent = parentOverride != null ? parentOverride : coroutineHost.transform;
            GameObject fx = Object.Instantiate(prefab, parent);
            activeEffects.Add(fx);
            fx.transform.rotation = Quaternion.identity;
            fx.transform.localScale = Vector3.one * settings.StartScale;
            fx.GetComponent<ParticleSystem>()?.Play(true);

            RectTransform fxRect = fx.GetComponent<RectTransform>();
            RectTransform parentRect = parent as RectTransform;

            if (fxRect != null && parentRect != null)
            {
                Vector2 startPoint = UiWorldToLocalPoint(parentRect, card.position);
                fxRect.anchoredPosition = startPoint;
                fxRect.localRotation = Quaternion.identity;
                coroutineHost.StartCoroutine(PlayUiAbsorb(fxRect, parentRect, startPoint));
                return;
            }

            fx.transform.position = card.position;
            coroutineHost.StartCoroutine(PlayWorldAbsorb(fx.transform));
        }

        public void Clear()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i] != null)
                    Object.Destroy(activeEffects[i]);
            }

            activeEffects.Clear();
        }

        private IEnumerator PlayUiAbsorb(RectTransform fx, RectTransform parent, Vector2 startPoint)
        {
            float elapsed = 0.0f;

            while (elapsed < settings.AbsorbDuration)
            {
                if (fx == null)
                    yield break;

                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / settings.AbsorbDuration);
                fx.anchoredPosition = Vector2.LerpUnclamped(
                    startPoint,
                    GetAbsorbTargetPoint(parent),
                    CardMotionEase.InCubic(normalized));
                fx.localScale = Vector3.one * Mathf.Lerp(
                    settings.StartScale,
                    settings.EndScale,
                    CardMotionEase.OutCubic(normalized));
                yield return null;
            }

            if (fx != null)
            {
                activeEffects.Remove(fx.gameObject);
                Object.Destroy(fx.gameObject);
            }
        }

        private IEnumerator PlayWorldAbsorb(Transform fx)
        {
            float elapsed = 0.0f;
            Vector3 startPosition = fx.position;

            while (elapsed < settings.AbsorbDuration)
            {
                if (fx == null)
                    yield break;

                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / settings.AbsorbDuration);
                Vector3 targetPosition = absorbTarget != null ? absorbTarget.position : startPosition;
                fx.position = Vector3.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    CardMotionEase.InCubic(normalized));
                fx.localScale = Vector3.one * Mathf.Lerp(
                    settings.StartScale,
                    settings.EndScale,
                    CardMotionEase.OutCubic(normalized));
                yield return null;
            }

            if (fx != null)
            {
                activeEffects.Remove(fx.gameObject);
                Object.Destroy(fx.gameObject);
            }
        }

        private Vector2 GetAbsorbTargetPoint(RectTransform parent)
        {
            if (absorbTarget == null)
                return Vector2.zero;
            if (absorbTarget is RectTransform targetRect)
                return UiWorldToLocalPoint(parent, targetRect.position);

            Camera worldCamera = absorbTargetCamera != null ? absorbTargetCamera : Camera.main;
            if (worldCamera == null)
                return Vector2.zero;

            Camera uiCamera = GetCanvasCamera(parent);
            Vector2 screenPoint = worldCamera.WorldToScreenPoint(absorbTarget.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                screenPoint,
                uiCamera,
                out Vector2 localPoint);
            return localPoint;
        }

        private static Vector2 UiWorldToLocalPoint(RectTransform parent, Vector3 worldPosition)
        {
            Camera uiCamera = GetCanvasCamera(parent);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                screenPoint,
                uiCamera,
                out Vector2 localPoint);
            return localPoint;
        }

        private static Camera GetCanvasCamera(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }
    }
}
