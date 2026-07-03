using UnityEngine;

namespace Arcanist
{
    public class BuffController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private BuffViewController buffViewController;

        private float outgoingDamageMultiplier = 1.0f;
        private float outgoingDamageBuffEndTime;
        private float incomingDamageMultiplier = 1.0f;
        private float incomingDamageBuffEndTime;
        private float cardGaugeChargeMultiplier = 1.0f;
        private float cardGaugeSpeedBuffEndTime;

        public void ApplyDamageBuff(string buffId, Sprite icon, float multiplier, float duration)
        {
            outgoingDamageMultiplier = Mathf.Max(outgoingDamageMultiplier, multiplier);
            outgoingDamageBuffEndTime = Mathf.Max(outgoingDamageBuffEndTime, Time.time + duration);

            ShowBuffUI(buffId, icon, outgoingDamageBuffEndTime);
        }

        public void ApplyTakenDamageDecreaseBuff(string buffId, Sprite icon, float reductionRatio, float duration)
        {
            float multiplier = Mathf.Clamp01(1.0f - reductionRatio);
            incomingDamageMultiplier = Mathf.Min(incomingDamageMultiplier, multiplier);
            incomingDamageBuffEndTime = Mathf.Max(incomingDamageBuffEndTime, Time.time + duration);

            ShowBuffUI(buffId, icon, incomingDamageBuffEndTime);
        }

        public void ApplyTakenDamageIncreaseBuff(string buffId, Sprite icon, float multiplier, float duration)
        {
            incomingDamageMultiplier = Mathf.Max(incomingDamageMultiplier, multiplier);
            incomingDamageBuffEndTime = Mathf.Max(incomingDamageBuffEndTime, Time.time + duration);

            ShowBuffUI(buffId, icon, incomingDamageBuffEndTime);
        }

        public void ApplyCardGaugeChargeSpeedBuff(string buffId, Sprite icon, float multiplier, float duration)
        {
            cardGaugeChargeMultiplier = Mathf.Max(cardGaugeChargeMultiplier, multiplier);
            cardGaugeSpeedBuffEndTime = Mathf.Max(cardGaugeSpeedBuffEndTime, Time.time + duration);

            ShowBuffUI(buffId, icon, cardGaugeSpeedBuffEndTime);
        }

        public int ModifyDamage(int damage)
        {
            if (Time.time > outgoingDamageBuffEndTime)
                outgoingDamageMultiplier = 1.0f;

            return Mathf.RoundToInt(damage * outgoingDamageMultiplier);
        }

        public int ModifyTakenDamage(int damage)
        {
            if (Time.time > incomingDamageBuffEndTime)
                incomingDamageMultiplier = 1.0f;

            return Mathf.RoundToInt(damage * incomingDamageMultiplier);
        }

        public float ModifyCardGaugeChargeTime(float seconds)
        {
            if (Time.time > cardGaugeSpeedBuffEndTime)
                cardGaugeChargeMultiplier = 1.0f;

            return seconds / cardGaugeChargeMultiplier;
        }

        private void ShowBuffUI(string buffId, Sprite icon, float endTime)
        {
            float remainingDuration = Mathf.Max(0.01f, endTime - Time.time);
            buffViewController?.ShowBuff(buffId, icon, remainingDuration);
        }    
    }
}
       