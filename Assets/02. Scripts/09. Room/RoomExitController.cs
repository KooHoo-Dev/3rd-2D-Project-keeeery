using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Arcanist
{
    public class RoomExitController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private Obelisk obelisk;

        [Header("Input Prompt")]
        [SerializeField] private InputPromptController inputPromptController;

        private RoomController roomController;
        private readonly HashSet<Collider2D> playerCollidersInside = new HashSet<Collider2D>();
        private bool isPlayerInside;
        private bool isLocked = true;
        private bool isInteractionConsumed;
        private bool isUsingExit;
        private Coroutine hidePromptRoutine;

        public bool IsPlayerInside => isPlayerInside && !isLocked;

        public void Initialize(RoomController controller)
        {
            roomController = controller;

            if (obelisk == null)
                obelisk = GetComponentInChildren<Obelisk>(true);

            if (inputPromptController == null)
                inputPromptController = FindFirstObjectByType<InputPromptController>();

            RefreshInputPrompt();
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            isInteractionConsumed = false;
            isUsingExit = false;
            isPlayerInside = playerCollidersInside.Count > 0;

            obelisk?.SetActiveState(!isLocked);
            RefreshInputPrompt();
        }

        private void Update()
        {
            if (!isPlayerInside || isLocked || isInteractionConsumed || isUsingExit)
                return;

            if (IsOverlayOpen())
                return;

            if (SimpleInput.GetKeyDown(Key.UpArrow))
            {
                isInteractionConsumed = true;
                RefreshInputPrompt();
                TryUseExit();
                return;
            }

            if (SimpleInput.GetKeyDown(Key.DownArrow))
                obelisk?.TryOpenDeckEditInventory();
        }

        private bool IsOverlayOpen()
        {
            OverlayController currentOverlayController = FindFirstObjectByType<OverlayController>(FindObjectsInactive.Include);
            return currentOverlayController != null && currentOverlayController.CurrentState != OverlayController.OverlayState.None;
        }

        private void TryUseExit()
        {
            if (isUsingExit)
                return;

            if (roomController == null)
                return;

            isUsingExit = true;
            inputPromptController?.Hide();

            if (roomController.CanExit && obelisk != null)
            {
                obelisk.PlayBreak(() => roomController.TryExitRoom());
                return;
            }

            roomController.TryExitRoom();
        }

        private void RefreshInputPrompt()
        {
            if (inputPromptController == null)
                return;

            bool shouldShow = isPlayerInside && !isLocked && !isInteractionConsumed && !isUsingExit && !IsOverlayOpen();

            if (!shouldShow)
            {
                inputPromptController.Hide();
                return;
            }

            if (roomController != null && roomController.CanExit)
                inputPromptController.ShowNextRoomPrompt();
            else
                inputPromptController.ShowRewardPrompt();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {

            if (!IsPlayerCollider(other)) return;

            CancelHidePromptDelay();
            playerCollidersInside.Add(other);
            isPlayerInside = true;
            RefreshInputPrompt();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
                return;

            playerCollidersInside.Remove(other);
            isPlayerInside = playerCollidersInside.Count > 0;

            if (!isPlayerInside && !isUsingExit)
                isInteractionConsumed = false;

            if (isPlayerInside) RefreshInputPrompt();
            else StartHidePromptDelay();
        }

        private void OnDisable()
        {
            CancelHidePromptDelay();
            playerCollidersInside.Clear();
            isPlayerInside = false;
            isInteractionConsumed = false;
            isUsingExit = false;
            inputPromptController?.Hide();
        }

        private void StartHidePromptDelay()
        {
            CancelHidePromptDelay();
            hidePromptRoutine = StartCoroutine(HidePromptDelayRoutine());
        }

        private IEnumerator HidePromptDelayRoutine()
        {
            yield return new WaitForSecondsRealtime(0.08f);

            hidePromptRoutine = null;

            RefreshInputPrompt();
        }

        private void CancelHidePromptDelay()
        {
            if (hidePromptRoutine == null)
                return;

            StopCoroutine(hidePromptRoutine);
            hidePromptRoutine = null;
        }

        private bool IsPlayerCollider(Collider2D other)
        {
            if (other.CompareTag("Player"))
                return true;

            return other.GetComponentInParent<PlayerController>() != null;
        }
    }
}
