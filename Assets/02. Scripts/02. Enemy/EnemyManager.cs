using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public enum EnemyGrade
    {
        Normal = 0,
        Elite = 1,
        Boss = 2
    }

    public class EnemyManager : MonoBehaviour
    {
        private static readonly List<EnemyController> activeEnemies = new();
        private static readonly List<BossController> activeBosses = new();

        public static IReadOnlyList<EnemyController> ActiveEnemies => activeEnemies;
        public static IReadOnlyList<BossController> ActiveBosses => activeBosses;

        public static void Register(EnemyController enemy)
        {
            if (enemy == null)
                return;

            if (activeEnemies.Contains(enemy))
                return;

            activeEnemies.Add(enemy);
        }

        public static void Unregister(EnemyController enemy)
        {
            if (enemy == null)
                return;

            activeEnemies.Remove(enemy);
        }

        public static void Register(BossController boss)
        {
            if (boss == null)
                return;

            if (activeBosses.Contains(boss))
                return;

            activeBosses.Add(boss);
        }

        public static void Unregister(BossController boss)
        {
            if (boss == null)
                return;

            activeBosses.Remove(boss);
        }

        public Transform FindBestTarget(Vector3 origin)
        {
            EnemyController bestEnemy = null;
            int bestGrade = int.MinValue;
            float bestSqrDistance = float.MaxValue;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyController candidate = activeEnemies[i];

                if (candidate == null || candidate.isActiveAndEnabled == false || candidate.IsDead)
                    continue;

                Transform targetPoint = candidate.TargetPoint;

                if (targetPoint == null)
                    continue;

                int grade = (int)candidate.Grade;
                float sqrDistance = (targetPoint.position - origin).sqrMagnitude;

                if (grade < bestGrade)
                    continue;

                if (grade == bestGrade && sqrDistance >= bestSqrDistance)
                    continue;

                bestEnemy = candidate;
                bestGrade = grade;
                bestSqrDistance = sqrDistance;
            }

            Transform bestBossTarget = FindBestBossTarget(origin, bestGrade, bestSqrDistance);

            if (bestBossTarget != null)
                return bestBossTarget;

            return bestEnemy != null ? bestEnemy.TargetPoint : null;
        }

        private Transform FindBestBossTarget(Vector3 origin, int currentBestGrade, float currentBestSqrDistance)
        {
            BossController bestBoss = null;
            int bestGrade = currentBestGrade;
            float bestSqrDistance = currentBestSqrDistance;

            for (int i = 0; i < activeBosses.Count; i++)
            {
                BossController candidate = activeBosses[i];

                if (candidate == null || candidate.isActiveAndEnabled == false || candidate.IsDead)
                    continue;

                Transform targetPoint = candidate.TargetPoint;

                if (targetPoint == null)
                    continue;

                int grade = (int)candidate.Grade;
                float sqrDistance = (targetPoint.position - origin).sqrMagnitude;

                if (grade < bestGrade)
                    continue;

                if (grade == bestGrade && sqrDistance >= bestSqrDistance)
                    continue;

                bestBoss = candidate;
                bestGrade = grade;
                bestSqrDistance = sqrDistance;
            }

            return bestBoss != null ? bestBoss.TargetPoint : null;
        }
    }
}