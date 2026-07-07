using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utilities;

namespace Arcanist
{
    public class MainMenuOverlayController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string startSceneName = "Intro";

        [Header("Option")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private GameObject optionWindow;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private float fadeDuration = 0.2f;

        [Header("Main Menu Fade")]
        [SerializeField] private SpriteRenderer titleRenderer;
        [SerializeField] private Animator titleAnimator;
        [SerializeField] private CanvasGroup menuGroup;

        private Coroutine fadeRoutine;
        private bool isOptionOpen;

        private void Awake()
        {
            InitializeClosedState();
        }

        private void Update()
        {
            if (isOptionOpen && SimpleInput.GetKeyDown(Key.Escape))
                CloseOption();
        }

        public void StartGame()
        {
            SceneManager.LoadScene(startSceneName);
        }

        public void OpenOption()
        {
            if (isOptionOpen)
                return;

            SetOverlayRootActive(true);
            isOptionOpen = true;

            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0.0f;
                overlayGroup.blocksRaycasts = true;
                overlayGroup.interactable = true;
            }

            if (optionWindow != null)
                optionWindow.SetActive(true);

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeOptionRoutine(true));
        }

        public void CloseOption()
        {
            if (!isOptionOpen)
                return;

            isOptionOpen = false;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(CloseOptionRoutine());
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        private IEnumerator CloseOptionRoutine()
        {
            yield return FadeOptionRoutine(false);

            if (optionWindow != null)
                optionWindow.SetActive(false);

            if (overlayGroup != null)
            {
                overlayGroup.blocksRaycasts = false;
                overlayGroup.interactable = false;
            }

            SetOverlayRootActive(false);
        }

        private IEnumerator FadeOptionRoutine(bool open)
        {
            float overlayStartAlpha = overlayGroup != null ? overlayGroup.alpha : 0.0f;
            float overlayTargetAlpha = open ? 1.0f : 0.0f;

            float titleStartAlpha = titleRenderer != null ? titleRenderer.color.a : 1.0f;
            float titleTargetAlpha = open ? 0.0f : 1.0f;

            float menuStartAlpha = menuGroup != null ? menuGroup.alpha : 1.0f;
            float menuTargetAlpha = open ? 0.0f : 1.0f;

            SetCanvasGroupInput(menuGroup, !open);

            SetTitleAnimatorEnabled(false);

            float time = 0.0f;

            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / fadeDuration);

                SetCanvasGroupAlpha(overlayGroup, Mathf.Lerp(overlayStartAlpha, overlayTargetAlpha, t));
                SetSpriteAlpha(titleRenderer, Mathf.Lerp(titleStartAlpha, titleTargetAlpha, t));
                SetCanvasGroupAlpha(menuGroup, Mathf.Lerp(menuStartAlpha, menuTargetAlpha, t));

                yield return null;
            }

            SetCanvasGroupAlpha(overlayGroup, overlayTargetAlpha);
            SetSpriteAlpha(titleRenderer, titleTargetAlpha);
            SetCanvasGroupAlpha(menuGroup, menuTargetAlpha);

            SetCanvasGroupInput(menuGroup, !open);

            if (!open)
                SetTitleAnimatorEnabled(true);

            fadeRoutine = null;
        }

        private void InitializeClosedState()
        {
            isOptionOpen = false;

            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0.0f;
                overlayGroup.blocksRaycasts = false;
                overlayGroup.interactable = false;
            }

            if (optionWindow != null)
                optionWindow.SetActive(false);

            SetSpriteAlpha(titleRenderer, 1.0f);
            SetTitleAnimatorEnabled(true);
            SetCanvasGroupAlpha(menuGroup, 1.0f);
            SetCanvasGroupInput(menuGroup, true);

            SetOverlayRootActive(false);
        }

        private void SetTitleAnimatorEnabled(bool enabled)
        {
            if (titleAnimator == null)
                return;

            titleAnimator.enabled = enabled;
        }

        private void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
                return;

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        private void SetCanvasGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group == null)
                return;

            group.alpha = alpha;
        }

        private void SetCanvasGroupInput(CanvasGroup group, bool enabled)
        {
            if (group == null)
                return;

            group.blocksRaycasts = enabled;
            group.interactable = enabled;
        }

        private void SetOverlayRootActive(bool active)
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(active);
                return;
            }

            gameObject.SetActive(active);
        }
    }
}