using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcanist
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint")]
        [SerializeField] private string checkpointId = "CP_01";
        [SerializeField] private string roomId = "Room_01";
        [SerializeField] private Transform respawnPoint;

        [Header("Option")]
        [SerializeField] private bool saveOnTriggerEnter = true;
        [SerializeField] private bool saveOnce = true;
        [SerializeField] private bool respawnPlayerOnly;

        private static float saveBlockEndTime;

        private bool isSaved;
        private bool savedRoomCleared;

        public string CheckpointId => checkpointId;
        public string RoomId => roomId;
        public bool RespawnPlayerOnly => respawnPlayerOnly;
        public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;

        public static void BlockSaveForSeconds(float duration)
        {
            saveBlockEndTime = Mathf.Max(saveBlockEndTime, Time.time + Mathf.Max(0.0f, duration));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!saveOnTriggerEnter)
                return;

            if (Time.time < saveBlockEndTime)
            {
                Debug.Log($"[Checkpoint] Save Blocked: {roomId} / {checkpointId}");
                return;
            }

            if (saveOnce && isSaved && savedRoomCleared)
                return;

            if (!other.TryGetComponent(out PlayerController player))
                return;

            Save(player);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!saveOnTriggerEnter)
                return;

            if (Time.time < saveBlockEndTime)
                return;

            if (savedRoomCleared)
                return;

            if (!IsCurrentRoomCleared())
                return;

            if (!other.TryGetComponent(out PlayerController player))
                return;

            Save(player);
        }

        private void Save(PlayerController player)
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogWarning("[Checkpoint] SaveManager is missing.");
                return;
            }

            int currentHp = GetPlayerCurrentHp(player);
            string sceneName = SceneManager.GetActiveScene().name;

            bool roomCleared = IsCurrentRoomCleared();

            SaveManager.Instance.SaveCheckpoint(
                sceneName,
                roomId,
                checkpointId,
                currentHp,
                respawnPlayerOnly,
                roomCleared
            );

            isSaved = true;
            savedRoomCleared = roomCleared;

            Debug.Log($"[Checkpoint] Saved: {sceneName} / {roomId} / {checkpointId} / HP {currentHp} / PlayerOnly {respawnPlayerOnly} / RoomCleared {roomCleared}");
        }

        private bool IsCurrentRoomCleared()
        {
            RoomController roomController = GetComponentInParent<RoomController>();

            if (roomController == null)
            {
                Transform roomRoot = transform.root;

                if (roomRoot != null)
                    roomController = roomRoot.GetComponentInChildren<RoomController>(true);
            }

            if (roomController == null || roomController.RoomId != roomId)
            {
                RoomController[] roomControllers = FindObjectsByType<RoomController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < roomControllers.Length; i++)
                {
                    RoomController candidate = roomControllers[i];

                    if (candidate != null && candidate.RoomId == roomId)
                    {
                        roomController = candidate;
                        break;
                    }
                }
            }

            return roomController != null && roomController.IsRoomCleared;
        }

        private int GetPlayerCurrentHp(PlayerController player)
        {
            if (player.TryGetComponent(out Player playerStatus))
                return playerStatus.CurrentHp;

            if (Player.LocalPlayer != null)
                return Player.LocalPlayer.CurrentHp;

            return 1;
        }

        public bool IsMatch(string targetRoomId, string targetCheckpointId)
        {
            return roomId == targetRoomId && checkpointId == targetCheckpointId;
        }
    }
}