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

        public static IReadOnlyList<EnemyController> ActiveEnemies => activeEnemies;

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

            return bestEnemy != null ? bestEnemy.TargetPoint : null;
        }
    }
}