using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcanist
{
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [Header("Room")]
        [SerializeField] private Transform roomRoot;
        [SerializeField] private Vector3 roomSpawnPosition = Vector3.zero;
        [SerializeField] private bool alignRoomToPlayerSpawnPoint = true;
        [SerializeField] private List<GameObject> roomPrefabs = new List<GameObject>();

        [Header("Player")]
        [SerializeField] private Transform player;
        [SerializeField] private string playerSpawnPointName = "PlayerSpawnPoint";

        [Header("Transition")]
        [SerializeField] private float fadeOutDuration = 0.35f;
        [SerializeField] private float fadeInDuration = 0.35f;
        [SerializeField] private float blackScreenHoldDuration = 0.08f;

        [Header("Next Scene")]
        [SerializeField] private string nextSceneName;

        private GameObject currentRoom;
        private int currentRoomIndex = -1;
        private bool isTransitioning;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            LoadRoom(0);
        }

        public void LoadNextRoom()
        {
            LoadRoom(currentRoomIndex + 1);
        }

        public void LoadNextRoomWithFade()
        {
            if (isTransitioning)
                return;

            StartCoroutine(LoadNextRoomWithFadeRoutine());
        }

        public bool TryLoadRoomById(string roomId)
        {
            return TryLoadRoomById(roomId, false);
        }

        public bool TryLoadRoomById(string roomId, bool skipEnemySpawn)
        {
            if (string.IsNullOrEmpty(roomId))
                return false;

            int roomIndex = FindRoomIndexById(roomId);

            if (roomIndex < 0)
            {
                Debug.LogWarning($"[RoomManager] Room id not found: {roomId}", this);
                return false;
            }

            LoadRoom(roomIndex, skipEnemySpawn);
            return true;
        }

        private int FindRoomIndexById(string roomId)
        {
            for (int i = 0; i < roomPrefabs.Count; i++)
            {
                GameObject roomPrefab = roomPrefabs[i];

                if (roomPrefab == null)
                    continue;

                RoomController roomController = roomPrefab.GetComponentInChildren<RoomController>(true);

                if (roomController != null && roomController.RoomId == roomId)
                    return i;
            }

            return -1;
        }

        private bool HasNextRoom()
        {
            return currentRoomIndex + 1 < roomPrefabs.Count;
        }

        private IEnumerator LoadNextRoomWithFadeRoutine()
        {
            isTransitioning = true;

            if (HasNextRoom())
            {
                yield return ScreenFadeManager.Instance.FadeToBlackRoutine(fadeOutDuration);
                yield return new WaitForSecondsRealtime(blackScreenHoldDuration);

                LoadRoom(currentRoomIndex + 1);

                yield return ScreenFadeManager.Instance.FadeFromBlackRoutine(fadeInDuration);
                isTransitioning = false;
                yield break;
            }

            LoadNextScene();
        }

        private void LoadNextScene()
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                ScreenFadeManager.Instance.LoadSceneWithFade(nextSceneName, fadeOutDuration);
                return;
            }

            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
                ScreenFadeManager.Instance.LoadSceneWithFade(nextScenePath, fadeOutDuration);
                return;
            }

            Debug.LogWarning("[RoomManager] Next room and next scene are both missing.", this);
            StartCoroutine(ScreenFadeManager.Instance.FadeFromBlackRoutine(fadeInDuration));
            isTransitioning = false;
        }

        public void ResetCurrentRoom()
        {
            Debug.Log($"[RoomManager] ResetCurrentRoom currentIndex={currentRoomIndex}");

            RoomController roomController = currentRoom != null
                ? currentRoom.GetComponentInChildren<RoomController>()
                : null;

            if (roomController != null)
            {
                roomController.ResetRoomForRespawn();
                return;
            }

            if (currentRoomIndex < 0)
            {
                LoadRoom(0);
                return;
            }

            LoadRoom(currentRoomIndex);
        }

        private void LoadRoom(int roomIndex)
        {
            LoadRoom(roomIndex, false);
        }

        private void LoadRoom(int roomIndex, bool skipEnemySpawn)
        {
            if (roomIndex < 0 || roomIndex >= roomPrefabs.Count)
                return;

            if (roomPrefabs[roomIndex] == null)
            {
                Debug.LogWarning($"[RoomManager] Room prefab is missing at index {roomIndex}.", this);
                return;
            }

            if (currentRoom != null)
                Destroy(currentRoom);

            if (roomRoot == null)
                roomRoot = transform;

            currentRoomIndex = roomIndex;
            currentRoom = Instantiate(roomPrefabs[currentRoomIndex], roomRoot, false);
            currentRoom.transform.localPosition = roomSpawnPosition;

            AlignRoomToPlayerSpawnPoint();

            if (skipEnemySpawn)
                DisableEnemyRoomController();

            InitializeRoomController();
            MovePlayerToSpawnPoint();
        }

        private void AlignRoomToPlayerSpawnPoint()
        {
            if (alignRoomToPlayerSpawnPoint == false || player == null || currentRoom == null)
                return;

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

        private void DisableEnemyRoomController()
        {
            if (currentRoom == null)
                return;

            EnemyRoomController enemyRoomController = currentRoom.GetComponentInChildren<EnemyRoomController>(true);

            if (enemyRoomController == null)
                return;

            enemyRoomController.gameObject.SetActive(false);
            Debug.Log("[RoomManager] Enemy room controller disabled for cleared checkpoint room.", currentRoom);
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
