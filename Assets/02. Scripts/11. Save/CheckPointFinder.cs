using UnityEngine;

namespace Arcanist
{
    public static class CheckpointFinder
    {
        public static Checkpoint Find(string roomId, string checkpointId)
        {
            Checkpoint[] checkpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);

            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint.IsMatch(roomId, checkpointId))
                    return checkpoint;
            }

            return null;
        }
    }
}
