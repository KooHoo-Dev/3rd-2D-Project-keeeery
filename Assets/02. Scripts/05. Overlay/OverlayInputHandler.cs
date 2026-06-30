using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utilities;

namespace Arcanist
{
    public class OverlayInputHandler : MonoBehaviour
    {
        private const int MIN_DECK_CARD_COUNT = 5;

        private OverlayController overlayController;
        private PlayerInventory playerInventory;
        private InventoryWindow inventoryWindow;

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
                    SceneManager.LoadScene("Combat");

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
            }
        }

        private void ToggleInventory()
        {
            ResolveReferences();

            if (overlayController.CurrentState == OverlayController.OverlayState.None)
                overlayController.ShowInventory();
                
            if (IsInventoryOpen)
            {
                TryCloseInventory();
                return;
            }
        }

        private void TryCloseInventory()
        {
            if (CanCloseInventory())
            {
                overlayController.Hide();
                return;
            }

            if (inventoryWindow != null)
                inventoryWindow.PlayMinimumDeckWarningFeedback();
        }

        private bool CanCloseInventory()
        {
            if (playerInventory == null)
                return true;

            return playerInventory.DeckCards.Count >= MIN_DECK_CARD_COUNT;
        }

        private void ResolveReferences()
        {
            if (playerInventory == null)
                playerInventory = PlayerInventory.Instance != null ? PlayerInventory.Instance : FindFirstObjectByType<PlayerInventory>();

            if (inventoryWindow == null)
                inventoryWindow = FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
        }
    }
}
