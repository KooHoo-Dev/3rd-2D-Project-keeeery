using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utilities;

namespace Arcanist
{
    public class ScreenFadeManager : MonoBehaviour
    {
        public static ScreenFadeManager Instance
        {
            get
            {
                if (instance == null)
                    CreateInstance();

                return instance;
            }
        }

        private static ScreenFadeManager instance;

        [Header("Fade")]
        [SerializeField] private int sortingOrder = 10000;
        [SerializeField] private Color fadeColor = Color.black;

        [Header("Initial Animation")]
        [SerializeField] private bool skipInitialAnimWhenCheckpointExists = true;
        [SerializeField] private string[] alwaysPlayInitialAnimSceneNames = { "Stage01_BossRoom" };

        private Canvas fadeCanvas;
        private CanvasGroup fadeGroup;
        private Image fadeImage;
        private Coroutine fadeRoutine;

        public bool IsBlack => fadeGroup != null && fadeGroup.alpha >= 0.99f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            InitializeFadeCanvas();
        }

        private static void CreateInstance()
        {
            GameObject managerObject = new GameObject("ScreenFadeManager");
            instance = managerObject.AddComponent<ScreenFadeManager>();
            DontDestroyOnLoad(managerObject);
        }

        public Coroutine FadeToBlack(float duration)
        {
            return StartManagedFade(0.0f, 1.0f, duration, true, false);
        }

        public Coroutine FadeFromBlack(float duration)
        {
            return StartManagedFade(1.0f, 0.0f, duration, false, true);
        }

        public IEnumerator FadeToBlackRoutine(float duration)
        {
            yield return StartManagedFade(0.0f, 1.0f, duration, true, false);
        }

        public IEnumerator FadeFromBlackRoutine(float duration)
        {
            yield return FadeFromBlackWhenSceneLoadedRoutine(duration, null);
        }

        public IEnumerator FadeFromBlackWhenSceneLoadedRoutine(float duration, string expectedSceneName)
        {
            yield return WaitUntilSceneLoaded(expectedSceneName);
            yield return StartManagedFade(1.0f, 0.0f, duration, false, true);
        }

        public Coroutine LoadSceneWithFade(string sceneName, float fadeDuration)
        {
            return StartCoroutine(LoadSceneWithFadeRoutine(sceneName, fadeDuration));
        }

        private IEnumerator LoadSceneWithFadeRoutine(string sceneName, float fadeDuration)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[ScreenFade] Scene name is empty.");
                yield break;
            }

            yield return FadeToBlackRoutine(fadeDuration);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

            while (loadOperation is { isDone: false })
                yield return null;

            yield return FadeFromBlackWhenSceneLoadedRoutine(fadeDuration, sceneName);
        }

        public bool ShouldPlayInitialAnimForCurrentScene()
        {
            return ShouldPlayInitialAnim(SceneManager.GetActiveScene().name);
        }

        private bool ShouldPlayInitialAnim(string sceneName)
        {
            if (IsAlwaysPlayInitialAnimScene(sceneName))
                return true;

            if (!skipInitialAnimWhenCheckpointExists)
                return true;

            return SaveManager.Instance == null || !SaveManager.Instance.HasCheckpoint();
        }

        private bool IsAlwaysPlayInitialAnimScene(string sceneName)
        {
            if (alwaysPlayInitialAnimSceneNames == null)
                return false;

            for (int i = 0; i < alwaysPlayInitialAnimSceneNames.Length; i++)
            {
                string alwaysPlaySceneName = alwaysPlayInitialAnimSceneNames[i];

                if (string.IsNullOrEmpty(alwaysPlaySceneName))
                    continue;

                if (string.Equals(alwaysPlaySceneName, sceneName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public void SetBlackImmediate()
        {
            InitializeFadeCanvas();
            StopFadeRoutine();
            fadeCanvas.gameObject.SetActive(true);
            fadeGroup.alpha = 1.0f;
            fadeGroup.blocksRaycasts = true;
        }

        public void SetClearImmediate()
        {
            InitializeFadeCanvas();
            StopFadeRoutine();
            fadeGroup.alpha = 0.0f;
            fadeGroup.blocksRaycasts = false;
            fadeCanvas.gameObject.SetActive(false);
        }

        private Coroutine StartManagedFade(float from, float to, float duration, bool blockInput, bool hideWhenComplete)
        {
            InitializeFadeCanvas();
            StopFadeRoutine();
            fadeRoutine = StartCoroutine(FadeRoutine(from, to, duration, blockInput, hideWhenComplete));
            return fadeRoutine;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration, bool blockInput, bool hideWhenComplete)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.sortingOrder = sortingOrder;
            fadeGroup.blocksRaycasts = true;

            if (blockInput)
                SimpleInput.IsKeyBoardInputBlocked = true;

            float elapsedTime = 0.0f;
            float safeDuration = Mathf.Max(0.0001f, duration);

            while (elapsedTime < safeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(from, to, elapsedTime / safeDuration);
                yield return null;
            }

            fadeGroup.alpha = to;
            fadeGroup.blocksRaycasts = to > 0.01f;

            if (hideWhenComplete && to <= 0.01f)
                fadeCanvas.gameObject.SetActive(false);

            if (!fadeGroup.blocksRaycasts)
                SimpleInput.IsKeyBoardInputBlocked = false;

            fadeRoutine = null;
        }

        private IEnumerator WaitUntilSceneLoaded(string expectedSceneName)
        {
            while (SceneManager.GetActiveScene().isLoaded == false)
                yield return null;

            if (string.IsNullOrEmpty(expectedSceneName))
                yield break;

            while (SceneManager.GetActiveScene().name != expectedSceneName)
                yield return null;

            while (SceneManager.GetActiveScene().isLoaded == false)
                yield return null;

            yield return null;
        }

        private void InitializeFadeCanvas()
        {
            if (fadeCanvas != null && fadeGroup != null && fadeImage != null)
                return;

            fadeCanvas = GetComponentInChildren<Canvas>(true);

            if (fadeCanvas == null)
            {
                GameObject canvasObject = new GameObject("ScreenFadeCanvas");
                canvasObject.transform.SetParent(transform, false);
                fadeCanvas = canvasObject.AddComponent<Canvas>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = sortingOrder;

            fadeGroup = fadeCanvas.GetComponent<CanvasGroup>();

            if (fadeGroup == null)
                fadeGroup = fadeCanvas.gameObject.AddComponent<CanvasGroup>();

            fadeImage = fadeCanvas.GetComponentInChildren<Image>(true);

            if (fadeImage == null)
            {
                GameObject imageObject = new GameObject("FadeImage");
                imageObject.transform.SetParent(fadeCanvas.transform, false);
                fadeImage = imageObject.AddComponent<Image>();
            }

            fadeImage.color = fadeColor;
            fadeImage.raycastTarget = true;

            RectTransform fadeRect = fadeImage.rectTransform;
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;

            fadeGroup.alpha = 0.0f;
            fadeGroup.blocksRaycasts = false;
            fadeCanvas.gameObject.SetActive(false);
        }

        private void StopFadeRoutine()
        {
            if (fadeRoutine == null)
                return;

            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }
}
