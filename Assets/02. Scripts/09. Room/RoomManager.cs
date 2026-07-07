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
        [SerializeField] private CanvasGroup transitionFadeGroup;
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
            InitializeTransitionFadeGroup();
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

        public bool HasNextRoom()
        {
            return currentRoomIndex + 1 < roomPrefabs.Count;
        }

        private IEnumerator LoadNextRoomWithFadeRoutine()
        {
            isTransitioning = true;

            yield return FadeTransition(1.0f, fadeOutDuration);
            yield return new WaitForSecondsRealtime(blackScreenHoldDuration);

            if (HasNextRoom())
            {
                LoadRoom(currentRoomIndex + 1);
                yield return FadeTransition(0.0f, fadeInDuration);
                isTransitioning = false;
                yield break;
            }

            LoadNextScene();
        }

        private void LoadNextScene()
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
                return;
            }

            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
                return;
            }

            Debug.LogWarning("[RoomManager] Next room and next scene are both missing.", this);
            StartCoroutine(FadeTransition(0.0f, fadeInDuration));
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

        private IEnumerator FadeTransition(float targetAlpha, float duration)
        {
            if (transitionFadeGroup == null)
                yield break;

            transitionFadeGroup.blocksRaycasts = true;
            transitionFadeGroup.interactable = true;

            float startAlpha = transitionFadeGroup.alpha;
            float elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = duration <= 0.0f ? 1.0f : elapsedTime / duration;
                transitionFadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            transitionFadeGroup.alpha = targetAlpha;

            bool isVisible = transitionFadeGroup.alpha > 0.0f;
            transitionFadeGroup.blocksRaycasts = isVisible;
            transitionFadeGroup.interactable = isVisible;
        }

        private void InitializeTransitionFadeGroup()
        {
            if (transitionFadeGroup == null)
                return;

            transitionFadeGroup.alpha = 0.0f;
            transitionFadeGroup.blocksRaycasts = false;
            transitionFadeGroup.interactable = false;
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
