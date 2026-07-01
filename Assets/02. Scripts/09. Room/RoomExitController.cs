using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Arcanist
{
    public class RoomExitController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Key interactKey = Key.UpArrow;

        [Header("State")]
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private GameObject unlockedVisual;

        private RoomController roomController;
        private bool isPlayerInside;
        private bool isLocked = true;

        public void Initialize(RoomController controller)
        {
            roomController = controller;
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;

            if (lockedVisual != null)
            {
                lockedVisual.SetActive(isLocked);
            }

            if (unlockedVisual != null)
            {
                unlockedVisual.SetActive(!isLocked);
            }
        }

        private void Update()
        {
            if (!isPlayerInside || isLocked)
            {
                return;
            }

            if (SimpleInput.GetKeyDown(interactKey))
            {
                roomController?.TryExitRoom();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInside = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInside = false;
            }
        }
    }
}
