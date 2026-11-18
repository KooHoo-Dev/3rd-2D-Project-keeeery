using UnityEngine;

namespace Arcanist
{
    public interface ICombatTarget
    {
        Transform TargetPoint { get; }
        EnemyGrade Grade { get; }
        bool IsDead { get; }
        bool IsTargetable { get; }
    }
}
