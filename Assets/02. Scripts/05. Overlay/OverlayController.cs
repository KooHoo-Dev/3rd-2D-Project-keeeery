using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
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
        [SerializeField] private GameObject inventoryWindow;
        [SerializeField] private GameObject rewardWindow;
        [SerializeField] private GameObject deathWindow;

        [Header("Light")]
        [SerializeField] private Light2D globalLight;
        [SerializeField] private float normalLightIntensity = 1.0f;
        [SerializeField] private float menuLightIntensity = 0.2f;
        [SerializeField] private float deathLightIntensity = 0.0f;

        [Header("Fade")]
        [SerializeField] private float menuFadeDuration = 0.25f;
        [SerializeField] private float deathFadeDuration = 2.0f;
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private float respawnFadeDuration = 0.35f;
        [SerializeField] private float respawnBlackHoldDuration = 0.25f;
        [SerializeField] private float respawnCameraWaitTimeout = 1.0f;

        [Header("DeathWindow")]
        [SerializeField] private Transform promptTargetPivot;
        [SerializeField] private PlayerController playerController;

        [Header("Reward")]
        [SerializeField] private RewardController rewardController;

        [Header("Camera Controller")]
        [SerializeField] private PlayerCameraController playerCameraController;  
        private RectTransform reviveInputPrompt;

        public enum OverlayState
        {
            None,
            Pause,
            Option,
            Inventory,
            Reward,
            Death
        }

        public OverlayState CurrentState { get; private set; }

        private Coroutine fadeRoutine;
        private bool isRespawning;

        private void Awake()
        {
            CurrentState = OverlayState.None;

            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();

            if (playerCameraController == null)
                playerCameraController = FindFirstObjectByType<PlayerCameraController>();

            if (rewardController == null)
                rewardController = FindFirstObjectByType<RewardController>(FindObjectsInactive.Include);

            reviveInputPrompt = deathWindow.transform.Find("ReviveInputPrompt").GetComponent<RectTransform>();

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

        public void ShowInventory()
        {
            CurrentState = OverlayState.Inventory;
            SimpleInput.IsKeyBoardInputBlocked = true;
            Time.timeScale = 0.0f;
            ShowWindow(inventoryWindow, menuLightIntensity, menuFadeDuration);
        }

        public void ShowReward()
        {
            CurrentState = OverlayState.Reward;
            SimpleInput.IsKeyBoardInputBlocked = true;
            Time.timeScale = 0.0f;

            ShowWindow(rewardWindow, menuLightIntensity, menuFadeDuration);

            if (rewardController != null)
                rewardController.OpenReward();
        }

        public void ShowDeathWindow()
        {
            Vector3 promptPosition = promptTargetPivot.position;
            promptPosition.x += 0.05f * playerController.FacingDirection;
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

        public IEnumerator RespawnFadeRoutine(Action onBlackScreen)
        {
            CurrentState = OverlayState.Death;
            SimpleInput.IsKeyBoardInputBlocked = true;
            Time.timeScale = 1.0f;
            overlayRoot.SetActive(true);

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            fadePanel.gameObject.SetActive(true);
            fadePanel.transform.SetAsLastSibling();
            fadePanel.alpha = 0.0f;

            yield return StartCoroutine(FadeCanvasGroup(fadePanel, 0.0f, 1.0f, respawnFadeDuration));

            HideAllWindows();
            overlayGroup.alpha = 0.0f;
            overlayRoot.SetActive(false);
            globalLight.intensity = normalLightIntensity;

            onBlackScreen?.Invoke();

            float waitStartTime = Time.unscaledTime;
            while (playerCameraController != null && !playerCameraController.IsAtTarget)
            {
                if (Time.unscaledTime - waitStartTime >= respawnCameraWaitTimeout)
                    break;

                yield return null;
            }

            yield return new WaitForSecondsRealtime(respawnBlackHoldDuration);

            CurrentState = OverlayState.None;
            yield return StartCoroutine(FadeCanvasGroup(fadePanel, 1.0f, 0.0f, respawnFadeDuration));

            fadePanel.alpha = 0.0f;
            fadePanel.gameObject.SetActive(false);

            CurrentState = OverlayState.None;
            SimpleInput.IsKeyBoardInputBlocked = false;
            Time.timeScale = 1.0f;
        }

        public void ReturnToCheckpoint()
        {
            if (isRespawning)
                return;

            if (!SaveManager.Instance.TryGetCheckpoint(out CheckpointSaveData data))
            {
                Debug.LogWarning("[Checkpoint] No checkpoint data.");
                return;
            }

            StartCoroutine(ReturnToCheckpointRoutine(data));
        }

        private IEnumerator ReturnToCheckpointRoutine(CheckpointSaveData data)
        {
            isRespawning = true;

            yield return RespawnFadeRoutine(() => { });

            string targetSceneName = data.sceneName;
            bool shouldLoadScene = !string.IsNullOrEmpty(targetSceneName)
                && targetSceneName != SceneManager.GetActiveScene().name;

            if (shouldLoadScene)
            {
                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName);

                while (loadOperation != null && !loadOperation.isDone)
                    yield return null;

                yield return null;
            }

            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();

            if (playerCameraController == null)
                playerCameraController = FindFirstObjectByType<PlayerCameraController>();

            Checkpoint checkpoint = CheckpointFinder.Find(data.roomId, data.checkpointId);

            if (checkpoint == null)
            {
                Debug.LogWarning($"[Checkpoint] Checkpoint not found: {data.roomId} / {data.checkpointId}");
                isRespawning = false;
                yield break;
            }

            if (!data.respawnPlayerOnly)
            {
                RoomManager roomManager = RoomManager.Instance;

                if (roomManager == null)
                    roomManager = FindFirstObjectByType<RoomManager>();

                if (roomManager != null)
                    roomManager.ResetCurrentRoom();
                else
                    Debug.LogWarning("[Checkpoint] RoomManager is missing. Room was not reset.");
            }

            if (playerController == null)
            {
                Debug.LogWarning("[Checkpoint] PlayerController is missing.");
                isRespawning = false;
                yield break;
            }

            Vector3 respawnPosition = checkpoint.RespawnPosition;
            playerController.Respawn(respawnPosition, data.currentHp);
            Debug.Log($"[Checkpoint] Returned to {respawnPosition}");

            isRespawning = false;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            group.alpha = from;

            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(time / duration);
                t = Mathf.SmoothStep(0.0f, 1.0f, t);

                group.alpha = Mathf.Lerp(from, to, t);

                yield return null;
            }

            group.alpha = to;
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
            inventoryWindow.SetActive(false);
            rewardWindow.SetActive(false);
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
