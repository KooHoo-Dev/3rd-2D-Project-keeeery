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

        public void Initialize(RoomController controller)
        {
            Debug.Log($"[Exit] Initialize -> {controller.name}", controller);
            roomController = controller;
        }

        public void SetLocked(bool locked)
        {
            Debug.Log($"[Exit] SetLocked({locked})");
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
            Debug.Log($"[Exit] inside={isPlayerInside}, locked={isLocked}");
            if (!isPlayerInside || isLocked)
            {
                return;
            }

            if (SimpleInput.GetKeyDown(Key.UpArrow))
            {
                Debug.Log("[Exit] TryExitRoom 호출");
                roomController?.TryExitRoom();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[Exit] Enter {other.name}, tag={other.tag}");
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
