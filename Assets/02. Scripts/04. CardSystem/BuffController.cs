using UnityEngine;

namespace Arcanist
{
    public class BuffController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private BuffViewController buffViewController;

        private float damageMultiplier = 1.0f;
        private float damageBuffEndTime;

        public void ApplyDamageBuff(string buffId, Sprite icon, float multiplier, float duration)
        {
            damageMultiplier = Mathf.Max(damageMultiplier, multiplier);
            damageBuffEndTime = Mathf.Max(damageBuffEndTime, Time.time + duration);

            float remainingDuration = Mathf.Max(0.01f, damageBuffEndTime - Time.time);
            buffViewController?.ShowBuff(buffId, icon, remainingDuration);
        }

        public int ModifyDamage(int damage)
        {
            if (Time.time > damageBuffEndTime)
                damageMultiplier = 1.0f;

            return Mathf.RoundToInt(damage * damageMultiplier);
        }
    }
}
