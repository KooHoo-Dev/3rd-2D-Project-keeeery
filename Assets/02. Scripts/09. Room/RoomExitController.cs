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

        [Header("Inventory")]
        [SerializeField] private OverlayController overlayController;
        [SerializeField] private InventoryWindow inventoryWindow;

        private RoomController roomController;
        private readonly HashSet<Collider2D> playerCollidersInside = new HashSet<Collider2D>();
        private bool isPlayerInside;
        private bool isLocked = true;
        private bool isInteractionConsumed;
        private Coroutine hidePromptRoutine;

        public bool IsPlayerInside => isPlayerInside && !isLocked;

        public void Initialize(RoomController controller)
        {
            roomController = controller;

            if (obelisk == null)
                obelisk = GetComponentInChildren<Obelisk>(true);

            if (inputPromptController == null)
                inputPromptController = FindFirstObjectByType<InputPromptController>();

            if (overlayController == null)
                overlayController = FindFirstObjectByType<OverlayController>();

            if (inventoryWindow == null)
                inventoryWindow = FindFirstObjectByType<InventoryWindow>();

            RefreshInputPrompt();
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            isInteractionConsumed = false;
            isPlayerInside = playerCollidersInside.Count > 0;

            obelisk?.SetActiveState(!isLocked);
            RefreshInputPrompt();
        }

        private void Update()
        {
            if (!isPlayerInside || isLocked || isInteractionConsumed)
                return;

            if (SimpleInput.GetKeyDown(Key.UpArrow))
            {
                isInteractionConsumed = true;
                RefreshInputPrompt();
                TryUseExit();
                return;
            }

            if (SimpleInput.GetKeyDown(Key.DownArrow))
                TryOpenDeckEditInventory();
        }

        public void TryOpenDeckEditInventory()
        {
            if (overlayController == null)
                overlayController = FindFirstObjectByType<OverlayController>(FindObjectsInactive.Include);

            if (inventoryWindow == null)
                inventoryWindow = FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);

            if (overlayController == null)
                return;

            if (overlayController.CurrentState != OverlayController.OverlayState.None)
                return;

            if (inventoryWindow != null)
                inventoryWindow.SetDeckEditMode(true);

            overlayController.ShowInventory();
        }

        private void TryUseExit()
        {
            if (roomController == null)
                return;

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

            bool shouldShow = isPlayerInside && !isLocked && !isInteractionConsumed;

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

            if (!isPlayerInside)
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
