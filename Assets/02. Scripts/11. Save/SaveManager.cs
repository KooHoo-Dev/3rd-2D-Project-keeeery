using System.IO;
using UnityEngine;

namespace Arcanist
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance
        {
            get
            {
                if (instance == null)
                    CreateInstance();

                return instance;
            }
        }

        private static SaveManager instance;
        private const string SAVE_FILE_NAME = "save_data.json";
        private SaveData currentData;
        private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        public SaveData CurrentData => currentData;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            LoadOrCreate();
        }

        private static void CreateInstance()
        {
            GameObject saveManagerObject = new GameObject("SaveManager");
            instance = saveManagerObject.AddComponent<SaveManager>();
        }

        public bool HasSaveData()
        {
            return File.Exists(SavePath);
        }

        public bool HasCheckpoint()
        {
            return currentData != null
                && currentData.checkpoint != null
                && !string.IsNullOrEmpty(currentData.checkpoint.sceneName)
                && !string.IsNullOrEmpty(currentData.checkpoint.checkpointId);
        }

        public void LoadOrCreate()
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                currentData = JsonUtility.FromJson<SaveData>(json);

                Debug.Log($"[Save] Loaded: {SavePath}");
                return;
            }

            currentData = new SaveData();

            Debug.Log($"[Save] Created New Save Data: {SavePath}");
        }

        public void Save()
        {
            if (currentData == null) currentData = new SaveData();

            string json = JsonUtility.ToJson(currentData, true);
            File.WriteAllText(SavePath, json);

            Debug.Log($"[Save] Saved: {SavePath}");
        }

        public void SaveCheckpoint(string sceneName, string roomId, string checkpointId, int currentHp, bool respawnPlayerOnly)
        {
            if (currentData == null) currentData = new SaveData();

            currentData.checkpoint.sceneName = sceneName;
            currentData.checkpoint.roomId = roomId;
            currentData.checkpoint.checkpointId = checkpointId;
            currentData.checkpoint.currentHp = currentHp;
            
            currentData.checkpoint.respawnPlayerOnly = respawnPlayerOnly;

            Save();
        }

        public void SaveInventory(InventorySaveData inventoryData)
        {
            if (currentData == null) currentData = new SaveData();

            currentData.inventory = inventoryData;

            Save();
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log($"[Save] Deleted: {SavePath}");
            }

            currentData = new SaveData();
        }

        public bool TryGetCheckpoint(out CheckpointSaveData checkpointData)
        {
            checkpointData = null;

            if (HasCheckpoint() == false) return false;

            checkpointData = currentData.checkpoint;
            return true;
        }
    }
}