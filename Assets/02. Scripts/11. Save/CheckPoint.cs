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

        private bool isSaved;

        public string CheckpointId => checkpointId;
        public string RoomId => roomId;
        public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!saveOnTriggerEnter)
                return;

            if (saveOnce && isSaved)
                return;

            if (!other.TryGetComponent(out PlayerController player))
                return;

            Save(player);
        }

        public void Save(PlayerController player)
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogWarning("[Checkpoint] SaveManager is missing.");
                return;
            }

            int currentHp = GetPlayerCurrentHp(player);
            string sceneName = SceneManager.GetActiveScene().name;

            SaveManager.Instance.SaveCheckpoint(
                sceneName,
                roomId,
                checkpointId,
                currentHp
            );

            isSaved = true;

            Debug.Log($"[Checkpoint] Saved: {sceneName} / {roomId} / {checkpointId} / HP {currentHp}");
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