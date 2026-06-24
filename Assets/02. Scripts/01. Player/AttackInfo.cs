using UnityEngine;

namespace Arcanist
{
    public enum AttackKey
    {
        None = 0, Combo1, Combo2, Combo3
    }
    [System.Serializable]
    public struct AttackInfo
    {
        public AttackKey Key;
        public int MinDamage;
        public int MaxDamage;
        public AnimationCurve damageCurve;
        
        public int RollDamage()
        {
            float randomRoll = Random.Range(0f, 1f);
            float evaluation = damageCurve.Evaluate(randomRoll);
            float finalDamage = Mathf.Lerp(MinDamage, MaxDamage, evaluation);

            return Mathf.RoundToInt(finalDamage);
        }
    }
}
