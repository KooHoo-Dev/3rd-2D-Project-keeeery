using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utilities;

namespace Arcanist
{
    public class OverlayInputHandler : MonoBehaviour
    {
        private OverlayController overlayController;

        private void Awake()
        {
            overlayController = GetComponent<OverlayController>();
        }

        private void Update()
        {
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

            if (SimpleInput.GetKeyDown(Key.Escape) == false) return;

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
                    overlayController.Hide();
                    break;

                case OverlayController.OverlayState.Death:
                    break;
            }
        }

        private void ToggleInventory()
        {
            switch (overlayController.CurrentState)
            {
                case OverlayController.OverlayState.None:
                    overlayController.ShowInventory();
                    break;

                case OverlayController.OverlayState.Inventory:
                    overlayController.Hide();
                    break;
            }
        }
    }
}
