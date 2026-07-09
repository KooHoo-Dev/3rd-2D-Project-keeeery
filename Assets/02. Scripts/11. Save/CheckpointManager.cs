using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace Arcanist
{
    public class CheckPointManager : MonoBehaviour
    {
        public static CheckPointManager Instance
        {
            get
            {
                if (instance == null)
                    CreateInstance();

                return instance;
            }
        }

        private static CheckPointManager instance;
        private bool isRespawning;

        private const float RESPAWN_FADE_DURATION = 0.35f;
        private const float RESPAWN_BLACK_HOLD_DURATION = 0.25f;
        private const float RESPAWN_CAMERA_WAIT_TIMEOUT = 1.0f;
        private const float CHECKPOINT_SAVE_BLOCK_DURATION = 3.0f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private static void CreateInstance()
        {
            GameObject managerObject = new GameObject("CheckpointManager");
            instance = managerObject.AddComponent<CheckPointManager>();
        }

        public void ReturnToCheckpoint()
        {
            Debug.Log("[Checkpoint] ReturnToCheckpoint Called");

            if (isRespawning)
            {
                Debug.LogWarning("[Checkpoint] Return blocked: already respawning.");
                return;
            }

            SaveManager saveManager = SaveManager.Instance;

            if (saveManager == null)
            {
                Debug.LogWarning("[Checkpoint] Return failed: SaveManager is null.");
                return;
            }

            if (saveManager.CurrentData == null || saveManager.CurrentData.checkpoint == null)
                Debug.LogWarning("[Checkpoint] CurrentData or checkpoint data is null before TryGetCheckpoint.");
            else
                Debug.Log($"[Checkpoint] Current Save Before Return: {saveManager.CurrentData.checkpoint.sceneName} / {saveManager.CurrentData.checkpoint.roomId} / {saveManager.CurrentData.checkpoint.checkpointId} / HP {saveManager.CurrentData.checkpoint.currentHp} / PlayerOnly {saveManager.CurrentData.checkpoint.respawnPlayerOnly}");

            if (!saveManager.TryGetCheckpoint(out CheckpointSaveData data))
            {
                Debug.LogWarning("[Checkpoint] No checkpoint data.");
                return;
            }

            CheckpointSaveData snapshot = new CheckpointSaveData
            {
                sceneName = data.sceneName,
                roomId = data.roomId,
                checkpointId = data.checkpointId,
                currentHp = data.currentHp,
                respawnPlayerOnly = data.respawnPlayerOnly,
                roomCleared = data.roomCleared
            };

            Debug.Log($"[Checkpoint] TryGetCheckpoint Result: {snapshot.sceneName} / {snapshot.roomId} / {snapshot.checkpointId} / HP {snapshot.currentHp} / PlayerOnly {snapshot.respawnPlayerOnly} / RoomCleared {snapshot.roomCleared}");
            StartCoroutine(ReturnToCheckpointRoutine(snapshot));
        }

        private IEnumerator ReturnToCheckpointRoutine(CheckpointSaveData data)
        {
            Debug.Log("[Checkpoint] Routine Start");
            isRespawning = true;
            SimpleInput.IsKeyBoardInputBlocked = true;
            Time.timeScale = 1.0f;

            ScreenFadeManager screenFadeManager = ScreenFadeManager.Instance;
            yield return screenFadeManager.FadeToBlackRoutine(RESPAWN_FADE_DURATION);

            string targetSceneName = data.sceneName;
            bool shouldLoadScene = !string.IsNullOrEmpty(targetSceneName)
                && targetSceneName != SceneManager.GetActiveScene().name;

            Debug.Log($"[Checkpoint] Scene Check: current={SceneManager.GetActiveScene().name}, target={targetSceneName}, shouldLoad={shouldLoadScene}");

            if (shouldLoadScene)
            {
                Checkpoint.BlockSaveForSeconds(CHECKPOINT_SAVE_BLOCK_DURATION);
                Debug.Log($"[Checkpoint] Scene Load Start: {targetSceneName}");

                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName);

                while (loadOperation is { isDone: false })
                    yield return null;

                yield return null;
                Debug.Log($"[Checkpoint] Scene Load Complete: {SceneManager.GetActiveScene().name}");
            }

            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            PlayerCameraController playerCameraController = FindFirstObjectByType<PlayerCameraController>();
            RoomManager roomManager = RoomManager.Instance;

            if (roomManager == null)
                roomManager = FindFirstObjectByType<RoomManager>();

            Debug.Log($"[Checkpoint] References: player={(playerController != null ? playerController.name : "null")}, camera={(playerCameraController != null ? playerCameraController.name : "null")}, roomManager={(roomManager != null ? roomManager.name : "null")}");
            Debug.Log($"[Checkpoint] Restore Target: {data.sceneName} / {data.roomId} / {data.checkpointId} / HP {data.currentHp} / PlayerOnly {data.respawnPlayerOnly} / RoomCleared {data.roomCleared}");

            if (roomManager != null)
            {
                bool skipEnemySpawn = data.respawnPlayerOnly && data.roomCleared;
                bool loadedRoom = roomManager.TryLoadRoomById(data.roomId, skipEnemySpawn);
                Debug.Log($"[Checkpoint] Load Room By Id Result: {data.roomId} / {loadedRoom} / SkipEnemySpawn {skipEnemySpawn}");
                yield return null;
            }

            Checkpoint checkpoint = CheckpointFinder.Find(data.roomId, data.checkpointId);

            if (checkpoint == null)
            {
                Debug.LogWarning($"[Checkpoint] Checkpoint not found: {data.roomId} / {data.checkpointId}");
                yield return FinishCheckpointReturn(data.sceneName);
                yield break;
            }

            Debug.Log($"[Checkpoint] Found Target: {checkpoint.RoomId} / {checkpoint.CheckpointId} / {checkpoint.RespawnPosition}");

            if (playerController == null)
            {
                Debug.LogWarning("[Checkpoint] PlayerController is missing.");
                yield return FinishCheckpointReturn(data.sceneName);
                yield break;
            }

            Vector3 respawnPosition = checkpoint.RespawnPosition;
            playerController.Respawn(respawnPosition, data.currentHp);
            Debug.Log($"[Checkpoint] Returned to {data.sceneName} / {data.roomId} / {data.checkpointId} / {respawnPosition}");

            float waitStartTime = Time.unscaledTime;
            while (playerCameraController != null && !playerCameraController.IsAtTarget)
            {
                if (Time.unscaledTime - waitStartTime >= RESPAWN_CAMERA_WAIT_TIMEOUT)
                    break;

                yield return null;
            }

            yield return new WaitForSecondsRealtime(RESPAWN_BLACK_HOLD_DURATION);
            yield return FinishCheckpointReturn(data.sceneName);
        }

        private IEnumerator FinishCheckpointReturn(string expectedSceneName)
        {
            Debug.Log("[Checkpoint] Finish Fade Start");
            yield return ScreenFadeManager.Instance.FadeFromBlackWhenSceneLoadedRoutine(RESPAWN_FADE_DURATION, expectedSceneName);

            SimpleInput.IsKeyBoardInputBlocked = false;
            Time.timeScale = 1.0f;
            isRespawning = false;
        }
    }
}