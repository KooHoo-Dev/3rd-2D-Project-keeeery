using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Arcanist
{
    public class RoomExitController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private GameObject unlockedVisual;

        private RoomController roomController;
        private bool isPlayerInside;
        private bool isLocked = true;
        private bool isInteractionConsumed;

        public void Initialize(RoomController controller)
        {
            roomController = controller;
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            isInteractionConsumed = false;

            if (lockedVisual != null)
                lockedVisual.SetActive(isLocked);

            if (unlockedVisual != null)
                unlockedVisual.SetActive(!isLocked);
        }

        private void Update()
        {
            if (!isPlayerInside || isLocked || isInteractionConsumed) return;

            if (SimpleInput.GetKeyDown(Key.UpArrow))
            {
                isInteractionConsumed = true;
                roomController?.TryExitRoom();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                isPlayerInside = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInside = false;
                isInteractionConsumed = false;
            }
        }
    }
}
