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

        public void ResetEnemiesForRespawn()
        {
            if (enemyRoot == null)
                enemyRoot = transform;

            ClearAllEnemiesInRoom();
            isCleared = false;
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
            Rigidbody2D rBody = enemy.GetComponentInChildren<Rigidbody2D>();

            if (rBody != null)
            {
                rBody.bodyType = RigidbodyType2D.Dynamic;
                rBody.simulated = true;
                rBody.linearVelocity = Vector2.zero;
                rBody.WakeUp();
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

        private void ClearAllEnemiesInRoom()
        {
            Debug.Log($"[EnemyRoom] ClearAllEnemiesInRoom - childCount={enemyRoot.childCount}, spawned={spawnedEnemies.Count}");

            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] == null)
                    continue;

                spawnedEnemies[i].SetActive(false);
                Destroy(spawnedEnemies[i]);
            }

            EnemyController[] enemies = enemyRoot.GetComponentsInChildren<EnemyController>(true);

            for (int i = enemies.Length - 1; i >= 0; i--)
            {
                if (enemies[i] == null) continue;

                GameObject enemyObject = enemies[i].gameObject;
                enemyObject.SetActive(false);
                Destroy(enemyObject);
            }

            spawnedEnemies.Clear();
        }

        private void ClearSpawnedEnemies()
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] == null) continue;

                spawnedEnemies[i].SetActive(false);
                Destroy(spawnedEnemies[i]);
            }

            spawnedEnemies.Clear();
        }
    }
}
