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
        private static readonly List<ICombatTarget> activeTargets = new();

        public static IReadOnlyList<ICombatTarget> ActiveTargets => activeTargets;

        public static void Register(ICombatTarget target)
        {
            if (target == null || activeTargets.Contains(target))
                return;

            activeTargets.Add(target);
        }

        public static void Unregister(ICombatTarget target)
        {
            if (target != null)
                activeTargets.Remove(target);
        }

        public bool TryFindBestTarget(Vector3 origin, out ICombatTarget target)
        {
            target = null;
            int bestGrade = int.MinValue;
            float bestSqrDistance = float.MaxValue;

            for (int i = activeTargets.Count - 1; i >= 0; i--)
            {
                ICombatTarget candidate = activeTargets[i];

                if (candidate == null || candidate is Object unityObject && unityObject == null)
                {
                    activeTargets.RemoveAt(i);
                    continue;
                }

                if (candidate.IsTargetable == false || candidate.IsDead)
                    continue;

                Transform targetPoint = candidate.TargetPoint;
                if (targetPoint == null)
                    continue;

                int grade = (int)candidate.Grade;
                float sqrDistance = (targetPoint.position - origin).sqrMagnitude;

                if (grade < bestGrade || grade == bestGrade && sqrDistance >= bestSqrDistance)
                    continue;

                target = candidate;
                bestGrade = grade;
                bestSqrDistance = sqrDistance;
            }

            return target != null;
        }

        public Transform FindBestTarget(Vector3 origin)
        {
            return TryFindBestTarget(origin, out ICombatTarget target)
                ? target.TargetPoint
                : null;
        }
    }
}
