using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Arcanist
{
    public class RoomController : MonoBehaviour
    {
        private enum RoomState
        {
            Ready,
            Combat,
            Reward,
            Cleared
        }

        [Header("Room")]
        [SerializeField] private string roomId = "Room_01";

        [Header("References")]
        [SerializeField] private EnemyRoomController enemyRoomController;
        [SerializeField] private RoomExitController roomExitController;
        [SerializeField] private OverlayController overlayController;
        [SerializeField] private BossController[] bossControllers;

        [Header("Events")]
        [SerializeField] private UnityEvent onRoomStarted;
        [SerializeField] private UnityEvent onRoomCleared;
        [SerializeField] private UnityEvent onExitRequested;

        private RoomManager roomManager;
        private RoomState currentState = RoomState.Ready;
        private bool startCleared;
        private bool startRewardObtained;

        public string RoomId => roomId;
        public bool IsRoomCleared => currentState is RoomState.Reward or RoomState.Cleared;
        public bool IsRewardObtained => currentState == RoomState.Cleared;
        public bool CanExit => currentState == RoomState.Cleared;
        public bool CanOpenReward => currentState == RoomState.Reward;

        public void Initialize(RoomManager manager, bool startCleared, bool rewardObtained = false)
        {
            roomManager = manager;
            this.startCleared = startCleared;
            startRewardObtained = rewardObtained;
        }

        private void Awake()
        {
            if (enemyRoomController == null)
                enemyRoomController = GetComponentInChildren<EnemyRoomController>();

            if (roomExitController == null)
                roomExitController = GetComponentInChildren<RoomExitController>();

            if (overlayController == null)
                overlayController = FindFirstObjectByType<OverlayController>();

            if (bossControllers == null || bossControllers.Length == 0)
                bossControllers = FindObjectsByType<BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (enemyRoomController != null)
                enemyRoomController.Initialize(this);

            if (roomExitController != null)
            {
                roomExitController.Initialize(this);
                roomExitController.SetLocked(true);
            }
        }

        private void Start()
        {
            StartRoom();
        }

        private void Update()
        {
            if (currentState != RoomState.Combat)
                return;

            if (HasBossEncounter() == false)
                return;

            if (AreAllBossesDefeated())
                OnAllEnemiesDefeated();
        }
        private bool HasBossEncounter()
        {
            return bossControllers is { Length: > 0 };
        }

        private bool AreAllBossesDefeated()
        {
            for (int i = 0; i < bossControllers.Length; i++)
            {
                BossController bossController = bossControllers[i];

                if (bossController != null && bossController.IsDead == false)
                    return false;
            }

            return true;
        }

        private void StartRoom()
        {
            bool hasBossEncounter = HasBossEncounter();
            bool hasActiveEnemyRoom = enemyRoomController != null && enemyRoomController.gameObject.activeInHierarchy;

            if (startCleared)
            {
                currentState = startRewardObtained ? RoomState.Cleared : RoomState.Reward;
                roomExitController?.SetLocked(false);

                if (startRewardObtained)
                    onRoomCleared?.Invoke();

                return;
            }

            if (!hasActiveEnemyRoom && !hasBossEncounter)
            {
                currentState = RoomState.Cleared;
                roomExitController?.SetLocked(false);
                onRoomCleared?.Invoke();
                return;
            }

            currentState = RoomState.Combat;
            roomExitController?.SetLocked(true);

            if (hasActiveEnemyRoom)
                enemyRoomController.SpawnEnemies();

            onRoomStarted?.Invoke();
        }

        public void ResetRoomForRespawn()
        {
            bool hasBossEncounter = HasBossEncounter();
            bool hasActiveEnemyRoom = enemyRoomController != null && enemyRoomController.gameObject.activeInHierarchy;

            if (!hasActiveEnemyRoom && !hasBossEncounter)
            {
                currentState = RoomState.Cleared;
                roomExitController?.SetLocked(false);
                return;
            }

            currentState = RoomState.Combat;
            roomExitController?.SetLocked(true);

            if (hasActiveEnemyRoom)
                enemyRoomController.ResetEnemiesForRespawn();
        }

        public void OnAllEnemiesDefeated()
        {
            Debug.Log("[Room] OnAllEnemiesDefeated");
            if (currentState != RoomState.Combat) return;

            currentState = RoomState.Reward;
            roomExitController?.SetLocked(false);
        }

        public void CompleteReward()
        {
            if (currentState != RoomState.Reward) return;

            currentState = RoomState.Cleared;
            roomExitController?.SetLocked(false);

            SaveManager.Instance?.MarkCheckpointRewardObtained(
                SceneManager.GetActiveScene().name,
                roomId
            );
        }

        public void TryExitRoom()
        {
            if (CanOpenReward)
            {
                if (overlayController == null)
                    overlayController = FindFirstObjectByType<OverlayController>();

                if (overlayController == null) return;

                overlayController.ShowReward();
                onRoomCleared?.Invoke();
                return;
            }

            if (CanExit == false) return;

            onExitRequested?.Invoke();
            roomManager?.LoadNextRoomWithFade();
        }
    }
}
