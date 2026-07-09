using System;
using System.Collections.Generic;

namespace Arcanist
{
    [Serializable]
    public class SaveData
    {
        public CheckpointSaveData checkpoint = new();
        public InventorySaveData inventory = new();
    }

    [Serializable]
    public class CheckpointSaveData
    {
        public string sceneName;
        public string roomId;
        public string checkpointId;
        public int currentHp;
        public bool respawnPlayerOnly;
        public bool roomCleared;
    }

    [Serializable]
    public class InventorySaveData
    {
        public List<string> ownedCardIds = new();
        public List<string> deckCardIds = new();
        public List<string> relicIds = new();
    }
}