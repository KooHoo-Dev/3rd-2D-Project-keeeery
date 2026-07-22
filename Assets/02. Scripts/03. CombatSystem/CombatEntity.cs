using UnityEngine;

namespace Arcanist
{
    public abstract class CombatEntity : MonoBehaviour
    {
        [field: SerializeField] public Transform HeadUpPivot { get; private set; }
        [field: SerializeField] public virtual BaseStat BaseStat { get; }

        public virtual bool CanReceiveKnockback => true;
        public virtual bool CanReceiveStun => true;

        public abstract void TakeDamage(int damage);

        public abstract void TakeHeal(int heal);

        public virtual int CalculateFinalDamage(int damage)
        {
            return damage;
        }
    }
}
