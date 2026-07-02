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

        private void StartRoom()
        {
            currentState = RoomState.Combat;

            if (roomExitController != null)
                roomExitController.SetLocked(true);

            if (enemyRoomController != null)
                enemyRoomController.SpawnEnemies();

            onRoomStarted?.Invoke();
        }

        public void OnAllEnemiesDefeated()
        {
            Debug.Log("[Room] OnAllEnemiesDefeated");
            if (currentState != RoomState.Combat) return;

            currentState = RoomState.Reward;

            if (roomExitController != null)
                roomExitController.SetLocked(false);
        }

        public void CompleteReward()
        {
            if (currentState != RoomState.Reward) return;

            currentState = RoomState.Cleared;

            if (roomExitController != null)
                roomExitController.SetLocked(false);
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

            if (!CanExit) return;

            onExitRequested?.Invoke();
            roomManager?.LoadNextRoom();
        }
    }
}
