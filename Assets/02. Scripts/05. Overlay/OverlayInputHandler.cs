using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utilities;

namespace Arcanist
{
    public class OverlayInputHandler : MonoBehaviour
    {

        private PlayerController player;
        private OverlayController overlayController;
        private PlayerInventory playerInventory;
        private InventoryWindow inventoryWindow;
        private RoomExitController roomExitController;

        public PlayerInventory PlayerInventory => playerInventory;
        public InventoryWindow InventoryWindow => inventoryWindow;

        private bool IsInventoryOpen =>
            overlayController != null && overlayController.CurrentState == OverlayController.OverlayState.Inventory;

        private void Awake()
        {
            overlayController = GetComponent<OverlayController>();
            ResolveReferences();
        }

        private void Update()
        {
            if (overlayController == null)
                return;

            if (overlayController.CurrentState == OverlayController.OverlayState.Death)
            {
                if (SimpleInput.GetKeyDown(Key.UpArrow))
                    overlayController.ReturnToCheckpoint();

                return;
            }

            if (SimpleInput.GetKeyDown(Key.I))
            {
                ToggleInventory();
                return;
            }

            if (SimpleInput.GetKeyDown(Key.Escape))
                TogglePause();
        }

        private void TogglePause()
        {
            switch (overlayController.CurrentState)
            {
                case OverlayController.OverlayState.None:
                    overlayController.ShowPause();
                    break;

                case OverlayController.OverlayState.Pause:
                    overlayController.Hide();
                    break;

                case OverlayController.OverlayState.Option:
                    overlayController.ShowPause();
                    break;

                case OverlayController.OverlayState.Inventory:
                    ToggleInventory();
                    break;

                case OverlayController.OverlayState.Reward:
                    break;
            }
        }

        private void ToggleInventory()
        {
            ResolveReferences();

            if (IsInventoryOpen)
            {
                TryCloseInventory();
                return;
            }

            if (overlayController.CurrentState == OverlayController.OverlayState.None)
            {
                bool canEditDeck = roomExitController != null && roomExitController.IsPlayerInside;

                inventoryWindow?.SetDeckEditMode(canEditDeck);
                overlayController.ShowInventory();
            }
        }

        private void TryCloseInventory()
        {
            if (CanCloseInventory())
            {
                overlayController.Hide();
                return;
            }

            inventoryWindow?.PlayMinimumDeckWarningFeedback();
        }

        private bool CanCloseInventory()
        {
            if (inventoryWindow != null && !inventoryWindow.CanEditDeck)
                return true;

            if (playerInventory == null)
                return true;

            return playerInventory.CanCompleteDeckEdit();
        }

        private void ResolveReferences()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerController>();

            if (playerInventory == null)
                playerInventory = PlayerInventory.Instance != null ? PlayerInventory.Instance : FindFirstObjectByType<PlayerInventory>();

            if (inventoryWindow == null)
                inventoryWindow = FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);

            if (roomExitController == null)
                roomExitController = FindFirstObjectByType<RoomExitController>();
        }
    }
}
