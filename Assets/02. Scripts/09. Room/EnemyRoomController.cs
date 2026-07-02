using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class EnemyRoomController : MonoBehaviour
    {
        [Header("Normal Enemy")]
        [SerializeField] private List<GameObject> normalEnemyPrefabs = new List<GameObject>();
        [SerializeField] private List<Transform> normalSpawnPoints = new List<Transform>();
        [SerializeField] private int normalEnemyCount = 3;

        [Header("Spawn")]
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";

        private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
        private RoomController roomController;
        private bool isCleared;

        public void Initialize(RoomController controller)
        {
            roomController = controller;
        }

        public void SpawnEnemies()
        {
            ClearSpawnedEnemies();
            isCleared = false;

            if (enemyRoot == null) enemyRoot = transform;

            SpawnNormalEnemies();
            CheckRoomClear();
        }

        private void SpawnNormalEnemies()
        {
            if (normalEnemyPrefabs.Count == 0 || normalSpawnPoints.Count == 0)
                return;

            int spawnCount = Mathf.Min(normalEnemyCount, normalSpawnPoints.Count);
            List<Transform> availableSpawnPoints = new List<Transform>(normalSpawnPoints);

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject enemyPrefab = GetRandomNormalEnemyPrefab();
                Transform spawnPoint = GetRandomSpawnPoint(availableSpawnPoints);

                if (enemyPrefab == null || spawnPoint == null) continue;

                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation, enemyRoot);
                InitializeSpawnedEnemy(enemy);
                spawnedEnemies.Add(enemy);
            }
        }

        private void InitializeSpawnedEnemy(GameObject enemy)
        {
            if (enemy == null) return;

            AssignTarget(enemy);
            EnablePhysics(enemy);
        }

        private void AssignTarget(GameObject enemy)
        {
            Transform resolvedTarget = GetTarget();

            if (resolvedTarget == null)
            {
                Debug.LogWarning("[EnemyRoomController] Target is missing. Enemy target was not assigned.", enemy);
                return;
            }

            enemy.SendMessage("SetTarget", resolvedTarget, SendMessageOptions.DontRequireReceiver);
        }

        private Transform GetTarget()
        {
            if (target != null) return target;

            GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);

            if (targetObject == null) return null;

            target = targetObject.transform;
            return target;
        }

        private void EnablePhysics(GameObject enemy)
        {
            Rigidbody2D rigidbody = enemy.GetComponentInChildren<Rigidbody2D>();

            if (rigidbody != null)
            {
                rigidbody.bodyType = RigidbodyType2D.Dynamic;
                rigidbody.simulated = true;
                rigidbody.linearVelocity = Vector2.zero;
                rigidbody.WakeUp();
            }

            Collider2D[] colliders = enemy.GetComponentsInChildren<Collider2D>(true);

            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = true;
        }

        private GameObject GetRandomNormalEnemyPrefab()
        {
            int randomIndex = Random.Range(0, normalEnemyPrefabs.Count);
            return normalEnemyPrefabs[randomIndex];
        }

        private Transform GetRandomSpawnPoint(List<Transform> availableSpawnPoints)
        {
            if (availableSpawnPoints.Count == 0) return null;

            int randomIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[randomIndex];
            availableSpawnPoints.RemoveAt(randomIndex);
            return spawnPoint;
        }

        private void Update()
        {
            if (isCleared) return;

            CheckRoomClear();
        }

        private void CheckRoomClear()
        {
            if (spawnedEnemies.Count == 0)
            {
                isCleared = true;
                roomController?.OnAllEnemiesDefeated();
                return;
            }

            for (int i = 0; i < spawnedEnemies.Count; i++)
            {
                GameObject enemyObject = spawnedEnemies[i];

                if (enemyObject == null) continue;

                EnemyController enemy = enemyObject.GetComponent<EnemyController>();

                if (enemy == null || !enemy.isDead) return;
            }

            isCleared = true;
            roomController?.OnAllEnemiesDefeated();
        }

        private void ClearSpawnedEnemies()
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
                if (spawnedEnemies[i] != null)
                    Destroy(spawnedEnemies[i]);

            spawnedEnemies.Clear();
        }
    }
}
