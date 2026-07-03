using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Arcanist
{
    public class CardEffectSpawner : MonoBehaviour
    {
        private static CardEffectSpawner instance;

        private readonly Dictionary<GameObject, ComponentPool<Transform>> pools = new Dictionary<GameObject, ComponentPool<Transform>>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            CardEffectSpawner spawner = GetOrCreateInstance();
            Transform spawned = spawner.GetFromPool(prefab);
            spawned.SetPositionAndRotation(position, rotation);
            spawned.gameObject.SetActive(true);

            CardEffectPoolItem poolItem = spawned.GetComponent<CardEffectPoolItem>();

            if (poolItem != null)
                poolItem.RestartLifeTimer();

            return spawned.gameObject;
        }

        public static void Despawn(GameObject effect)
        {
            if (effect == null)
                return;

            effect.SetActive(false);
        }

        private Transform GetFromPool(GameObject prefab)
        {
            if (pools.TryGetValue(prefab, out ComponentPool<Transform> pool) == false)
            {
                pool = new ComponentPool<Transform>(prefab.transform, transform);
                pools.Add(prefab, pool);
            }

            return pool.Get();
        }

        private static CardEffectSpawner GetOrCreateInstance()
        {
            if (instance != null)
                return instance;

            CardEffectSpawner found = FindFirstObjectByType<CardEffectSpawner>();

            if (found != null)
            {
                instance = found;
                return instance;
            }

            GameObject spawnerObject = new GameObject(nameof(CardEffectSpawner));
            instance = spawnerObject.AddComponent<CardEffectSpawner>();
            return instance;
        }
    }
}
