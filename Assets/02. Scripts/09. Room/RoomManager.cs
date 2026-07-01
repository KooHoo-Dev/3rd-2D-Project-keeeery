using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class RoomManager : MonoBehaviour
    {
        [Header("Room")]
        [SerializeField] private Transform roomRoot;
        [SerializeField] private Vector3 roomSpawnPosition = Vector3.zero;
        [SerializeField] private bool alignRoomToPlayerSpawnPoint = true;
        [SerializeField] private List<GameObject> roomPrefabs = new List<GameObject>();

        [Header("Player")]
        [SerializeField] private Transform player;
        [SerializeField] private string playerSpawnPointName = "PlayerSpawnPoint";

        private GameObject currentRoom;
        private int currentRoomIndex = -1;

        private void Start()
        {
            LoadRoom(0);
        }

        public void LoadNextRoom()
        {
            LoadRoom(currentRoomIndex + 1);
        }

        private void LoadRoom(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= roomPrefabs.Count)
            {
                return;
            }

            if (roomPrefabs[roomIndex] == null)
            {
                Debug.LogWarning($"[RoomManager] Room prefab is missing at index {roomIndex}.", this);
                return;
            }

            if (currentRoom != null)
            {
                Destroy(currentRoom);
            }

            if (roomRoot == null)
            {
                roomRoot = transform;
            }

            currentRoomIndex = roomIndex;
            currentRoom = Instantiate(roomPrefabs[currentRoomIndex], roomRoot, false);
            currentRoom.transform.localPosition = roomSpawnPosition;

            AlignRoomToPlayerSpawnPoint();
            InitializeRoomController();
            MovePlayerToSpawnPoint();
        }

        private void AlignRoomToPlayerSpawnPoint()
        {
            if (!alignRoomToPlayerSpawnPoint || player == null || currentRoom == null)
            {
                return;
            }

            Transform spawnPoint = FindChildByName(currentRoom.transform, playerSpawnPointName);

            if (spawnPoint == null)
            {
                Debug.LogWarning($"[RoomManager] Player spawn point not found: {playerSpawnPointName}", currentRoom);
                return;
            }

            Vector3 offset = player.position - spawnPoint.position;
            offset.z = 0.0f;
            currentRoom.transform.position += offset;
        }

        private void InitializeRoomController()
        {
            RoomController roomController = currentRoom.GetComponentInChildren<RoomController>();

            if (roomController != null)
            {
                roomController.Initialize(this);
            }
        }

        private void MovePlayerToSpawnPoint()
        {
            if (player == null || currentRoom == null)
            {
                return;
            }

            Transform spawnPoint = FindChildByName(currentRoom.transform, playerSpawnPointName);

            if (spawnPoint == null)
            {
                Debug.LogWarning($"[RoomManager] Player spawn point not found: {playerSpawnPointName}", currentRoom);
                return;
            }

            player.position = spawnPoint.position;
        }

        private Transform FindChildByName(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildByName(root.GetChild(i), childName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
