using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utilities;

namespace Arcanist
{
    public class OverlayInputHandler : MonoBehaviour
    {
        private const int MIN_DECK_CARD_COUNT = 5;

        private PlayerController player;
        private OverlayController overlayController;
        private PlayerInventory playerInventory;
        private InventoryWindow inventoryWindow;
        private bool isRespawning;

        // [Header("Reward Test")]
        // [SerializeField] private RectTransform rewardContainer;

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
                if (!isRespawning && SimpleInput.GetKeyDown(Key.UpArrow))
                {
                    TryRespawnFromCheckpoint();
                }

                return;
            }

            // 보상 화면 테스트용 - 실제 보상 흐름은 RoomExitController -> RoomController.TryExitRoom()에서 처리
            // if (SimpleInput.GetKeyDown(Key.R))
            // {
            //     ToggleReward();
            //     return;
            // }

            if (SimpleInput.GetKeyDown(Key.I))
            {
                ToggleInventory();
                return;
            }

            if (SimpleInput.GetKeyDown(Key.Escape))
                TogglePause();
        }

        private void TryRespawnFromCheckpoint()
        {
            ResolveReferences();

            if (player == null)
            {
                Debug.LogWarning("[Respawn] PlayerController is missing.");
                return;
            }
            if (!SaveManager.Instance.TryGetCheckpoint(out CheckpointSaveData data))
            {
                Debug.LogWarning("[Respawn] No checkpoint data.");
                return;
            }

            Checkpoint checkpoint = CheckpointFinder.Find(data.roomId, data.checkpointId);

            if (checkpoint == null)
            {
                Debug.LogWarning($"[Respawn] Checkpoint not found: {data.roomId} / {data.checkpointId}");
                return;
            }

            StartCoroutine(RespawnRoutine(checkpoint, data));
        }

        private IEnumerator RespawnRoutine(Checkpoint checkpoint, CheckpointSaveData data)
        {
            isRespawning = true;

            yield return overlayController.RespawnFadeRoutine(() =>
            {
                player.Respawn(checkpoint.RespawnPosition, data.currentHp);
                Debug.Log($"[Respawn] Moved to {checkpoint.RespawnPosition}");
            });

            isRespawning = false;
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
                overlayController.ShowInventory();
        }

        // 보상 화면 테스트용 - 실제 보상 흐름에서는 사용하지 않음
        // public void ToggleReward()
        // {
        //     if (overlayController.CurrentState == OverlayController.OverlayState.None)
        //     {
        //         ClearRewardContainer();
        //         overlayController.ShowReward();
        //     }
        // }

        // 보상 화면 테스트용 - 실제 보상 흐름에서는 사용하지 않음
        // private void ClearRewardContainer()
        // {
        //     if (rewardContainer == null)
        //         return;
        //
        //     for (int i = rewardContainer.childCount - 1; i >= 0; i--)
        //     {
        //         Transform child = rewardContainer.GetChild(i);
        //
        //         if (child == null)
        //             continue;
        //
        //         GameObject childObject = child.gameObject;
        //         child.SetParent(null, false);
        //         childObject.SetActive(false);
        //         Destroy(childObject);
        //     }
        // }

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
            if (player == null)
                player = FindFirstObjectByType<PlayerController>();
            if (playerInventory == null)
                playerInventory = PlayerInventory.Instance != null ? PlayerInventory.Instance : FindFirstObjectByType<PlayerInventory>();

            if (inventoryWindow == null)
                inventoryWindow = FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
        }
    }
}
