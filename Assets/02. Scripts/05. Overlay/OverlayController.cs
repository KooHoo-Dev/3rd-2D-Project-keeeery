using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Utilities;

namespace Arcanist
{
    public class OverlayController : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup overlayGroup;

        [Header("Windows")]
        [SerializeField] private GameObject pauseWindow;
        [SerializeField] private GameObject optionWindow;
        [SerializeField] private GameObject deathWindow;

        [Header("Light")]
        [SerializeField] private Light2D globalLight;
        [SerializeField] private float normalLightIntensity = 1.0f;
        [SerializeField] private float menuLightIntensity = 0.2f;
        [SerializeField] private float deathLightIntensity = 0.0f;

        [Header("Fade")]
        [SerializeField] private float menuFadeDuration = 0.25f;
        [SerializeField] private float deathFadeDuration = 2.0f;

        [Header("DeathWindow")]
        [SerializeField] private Transform promptTargetPivot;
        
        private RectTransform reviveInputPrompt;

        public enum OverlayState
        {
            None, Pause, Option, Death
        }

        public OverlayState CurrentState { get; private set; }

        private Coroutine fadeRoutine;

        private void Awake()
        {
            CurrentState = OverlayState.None;
            reviveInputPrompt = 
                deathWindow.transform.Find("ReviveInputPrompt").GetComponent<RectTransform>();

            InitializePauseButtons();
            HideAllWindows();

            overlayGroup.alpha = 0.0f;
            overlayRoot.SetActive(false);
        }

        public void ShowPause()
        {
            CurrentState = OverlayState.Pause;
            SimpleInput.IsKeyBoardInputBlocked = true;
            Time.timeScale = 0.0f;
            ShowWindow(pauseWindow, menuLightIntensity, menuFadeDuration);
        }

        public void ShowOption()
        {
            CurrentState = OverlayState.Option;
            SimpleInput.IsKeyBoardInputBlocked = true;
            ShowWindow(optionWindow, menuLightIntensity, menuFadeDuration);
        }

        public void ShowDeathWindow()
        {
            Vector3 promptPosition = promptTargetPivot.position;
            promptPosition.x -= 0.1f * PlayerController.FacingDirecton;
            reviveInputPrompt.position = promptPosition;
            deathWindow.SetActive(true);
        }

        public void ShowDeath()
        {
            StartDeathOverlay();
            ShowDeathWindow();
        }

        public void StartDeathOverlay()
        {
            CurrentState = OverlayState.Death;
            overlayRoot.SetActive(true);
            HideAllWindows();

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(DeathOverlayRoutine());
        }

        private IEnumerator DeathOverlayRoutine()
        {
            yield return StartCoroutine(FadeLightRoutine(deathLightIntensity, deathFadeDuration));
            yield return StartCoroutine(FadeOverlayRoutine(1.0f, menuFadeDuration));

            fadeRoutine = null;
        }

        private IEnumerator FadeLightRoutine(float targetLightIntensity, float duration)
        {
            float startLightIntensity = globalLight.intensity;
            float time = 0.0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(time / duration);
                t = Mathf.SmoothStep(0.0f, 1.0f, t);
                globalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);

                yield return null;
            }

            globalLight.intensity = targetLightIntensity;
        }

        private IEnumerator FadeOverlayRoutine(float targetAlpha, float duration)
        {
            float startAlpha = overlayGroup.alpha;
            float time = 0.0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(time / duration);
                t = Mathf.SmoothStep(0.0f, 1.0f, t);
                overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

                yield return null;
            }

            overlayGroup.alpha = targetAlpha;
        }

        public void Hide()
        {
            CurrentState = OverlayState.None;
            SimpleInput.IsKeyBoardInputBlocked = false;
            Time.timeScale = 1.0f;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeRoutine(0.0f, normalLightIntensity, menuFadeDuration, true));
        }

        private void ShowWindow(GameObject targetWindow, float targetLightIntensity, float duration)
        {
            overlayRoot.SetActive(true);
            HideAllWindows();
            targetWindow.SetActive(true);

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeRoutine(1.0f, targetLightIntensity, duration, false));
        }

        private void InitializePauseButtons()
        {
            PauseMenuButton[] buttons = overlayRoot.GetComponentsInChildren<PauseMenuButton>(true);

            for (int i = 0; i < buttons.Length; i++)
                buttons[i].Initialize(this);
        }

        private void HideAllWindows()
        {
            pauseWindow.SetActive(false);
            optionWindow.SetActive(false);
            deathWindow.SetActive(false);
        }

        private IEnumerator FadeRoutine(float targetAlpha, float targetLightIntensity, float duration, bool hideAfterFade)
        {
            float startAlpha = overlayGroup.alpha;
            float startLightIntensity = globalLight.intensity;
            float time = 0.0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(time / duration);
                t = Mathf.SmoothStep(0.0f, 1.0f, t);

                overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                globalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);

                yield return null;
            }

            overlayGroup.alpha = targetAlpha;
            globalLight.intensity = targetLightIntensity;

            if (hideAfterFade)
            {
                HideAllWindows();
                overlayRoot.SetActive(false);
            }

            fadeRoutine = null;
        }
    }
}
