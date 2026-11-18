using UnityEngine;

namespace Arcanist
{
    public abstract class RoomFactory
    {
        public GameObject CreateRoom(
            GameObject roomPrefab,
            Transform parent,
            Vector3 localPosition,
            RoomManager roomManager,
            bool startCleared,
            bool rewardObtained)
        {
            GameObject room = CreateRoomInstance(roomPrefab, parent);

            if (room == null)
                return null;

            room.transform.localPosition = localPosition;
            ConfigureRoom(room, roomManager, startCleared, rewardObtained);
            return room;
        }

        protected abstract GameObject CreateRoomInstance(GameObject roomPrefab, Transform parent);

        protected virtual void ConfigureRoom(
            GameObject room,
            RoomManager roomManager,
            bool startCleared,
            bool rewardObtained)
        {
            if (startCleared)
            {
                EnemyRoomController enemyRoomController =
                    room.GetComponentInChildren<EnemyRoomController>(true);

                if (enemyRoomController != null)
                {
                    // GameObject를 끄면 같은 오브젝트에 있는 RoomController도 Start 전에 꺼져 StartRoom이 실행되지 않으므로 컴포넌트만 끈다.
                    enemyRoomController.enabled = false;
                    Debug.Log(
                        "[RoomFactory] Enemy room controller disabled for cleared checkpoint room.",
                        room);
                }
            }

            RoomController roomController = room.GetComponentInChildren<RoomController>(true);
            roomController?.Initialize(roomManager, startCleared, rewardObtained);
        }
    }

    public sealed class PrefabRoomFactory : RoomFactory
    {
        protected override GameObject CreateRoomInstance(GameObject roomPrefab, Transform parent)
        {
            return Object.Instantiate(roomPrefab, parent, false);
        }
    }
}
