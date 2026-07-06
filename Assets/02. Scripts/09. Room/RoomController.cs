using UnityEngine;
using UnityEngine.Events;

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

        public bool CanExit => currentState == RoomState.Cleared;
        public bool CanOpenReward => currentState == RoomState.Reward;

        public void Initialize(RoomManager manager)
        {
            roomManager = manager;
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
            return bossControllers != null && bossControllers.Length > 0;
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
            currentState = RoomState.Combat;
            roomExitController?.SetLocked(true);
            enemyRoomController?.SpawnEnemies();
            onRoomStarted?.Invoke();
        }

        public void ResetRoomForRespawn()
        {
            currentState = RoomState.Combat;
            roomExitController?.SetLocked(true);
            enemyRoomController?.ResetEnemiesForRespawn();
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
            roomManager?.LoadNextRoom();
        }
    }
}
