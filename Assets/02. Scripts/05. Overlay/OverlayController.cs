using System;
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
        private OverlayMotion overlayMotion;

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
            BuildTransition();

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
            reviveInputPrompt.gameObject.SetActive(true);
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

            fadeRoutine = StartCoroutine(RunTransition(
                overlayMotion.PlayDeath(deathLightIntensity, deathFadeDuration, menuFadeDuration)));
        }

        public void Hide()
        {
            CurrentState = OverlayState.None;
            SimpleInput.IsKeyBoardInputBlocked = false;
            Time.timeScale = 1.0f;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(RunTransition(
                overlayMotion.Fade(0.0f, normalLightIntensity, menuFadeDuration, true, HideAllWindows)));
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

            yield return overlayMotion.PlayRespawn(onBlackScreen, HideAllWindows);

            CurrentState = OverlayState.None;
            SimpleInput.IsKeyBoardInputBlocked = false;
            Time.timeScale = 1.0f;
        }

        public void ReturnToCheckpoint()
        {
            CheckPointManager.Instance.ReturnToCheckpoint(HideOverlayForRespawn);
        }

        private void HideOverlayForRespawn()
        {
            CurrentState = OverlayState.None;

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            overlayMotion.ResetOverlay(HideAllWindows);
        }

        private void ShowWindow(GameObject targetWindow, float targetLightIntensity, float duration)
        {
            overlayRoot.SetActive(true);
            HideAllWindows();
            targetWindow.SetActive(true);

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(RunTransition(
                overlayMotion.Fade(1.0f, targetLightIntensity, duration, false, HideAllWindows)));
        }

        private void InitializePauseButtons()
        {
            PauseMenuButton[] buttons = overlayRoot.GetComponentsInChildren<PauseMenuButton>(true);

            foreach (var t in buttons)
                t.Initialize(this);
        }

        private void HideAllWindows()
        {
            pauseWindow.SetActive(false);
            optionWindow.SetActive(false);
            inventoryWindow.SetActive(false);
            rewardWindow.SetActive(false);
            deathWindow.SetActive(false);
        }

        private IEnumerator RunTransition(IEnumerator routine)
        {
            yield return routine;
            fadeRoutine = null;
        }

        private void BuildTransition()
        {
            overlayMotion = new OverlayMotion(
                overlayRoot,
                overlayGroup,
                globalLight,
                fadePanel,
                playerCameraController,
                normalLightIntensity,
                respawnFadeDuration,
                respawnBlackHoldDuration,
                respawnCameraWaitTimeout);
        }
    }
}
