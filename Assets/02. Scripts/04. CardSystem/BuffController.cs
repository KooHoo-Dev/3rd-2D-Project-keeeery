using UnityEngine;

namespace Arcanist
{
    public class BuffController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private BuffViewController buffViewController;
        [SerializeField] private ScreenEffectController screenEffectController;

        private float outgoingDamageMultiplier = 1.0f;
        private float outgoingDamageBuffEndTime;
        private float incomingDamageMultiplier = 1.0f;
        private float incomingDamageBuffEndTime;
        private float cardGaugeChargeMultiplier = 1.0f;
        private float cardGaugeSpeedBuffEndTime;
        private bool hasReviveOnceBuff;
        private string reviveBuffId;
        private Sprite reviveBuffIcon;
        private float reviveHealRatio;
        private float reviveInvincibleDuration;

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

        public void ApplyReviveOnceBuff(string buffId, Sprite icon, float healRatio, float duration)
        {
            hasReviveOnceBuff = true;
            reviveBuffId = buffId;
            reviveBuffIcon = icon;
            reviveHealRatio = Mathf.Clamp01(healRatio);
            reviveInvincibleDuration = Mathf.Max(0.0f, duration);

            buffViewController?.ShowBuff(buffId, icon, float.MaxValue);
        }

        public bool TryConsumeReviveOnce(Player player)
        {
            if (hasReviveOnceBuff == false || player == null)
                return false;

            string consumedBuffId = reviveBuffId;
            Sprite consumedBuffIcon = reviveBuffIcon;
            float consumedInvincibleDuration = reviveInvincibleDuration;
            int healAmount = Mathf.RoundToInt(player.Stat.MaxHp * reviveHealRatio);

            ClearReviveOnceBuff(false);
            player.TakeHeal(healAmount);

            if (consumedInvincibleDuration > 0.0f)
            {
                incomingDamageMultiplier = 0.0f;
                incomingDamageBuffEndTime = Time.time + consumedInvincibleDuration;
                ShowBuffUI(consumedBuffId, consumedBuffIcon, incomingDamageBuffEndTime);
                screenEffectController?.PlayReviveGlow(consumedInvincibleDuration);
            }
            else if (string.IsNullOrEmpty(consumedBuffId) == false)
            {
                buffViewController?.HideBuff(consumedBuffId);
            }

            return true;
        }

        private void ClearReviveOnceBuff(bool hideUI = true)
        {
            hasReviveOnceBuff = false;

            if (hideUI && string.IsNullOrEmpty(reviveBuffId) == false)
                buffViewController?.HideBuff(reviveBuffId);

            reviveBuffId = null;
            reviveBuffIcon = null;
            reviveHealRatio = 0.0f;
            reviveInvincibleDuration = 0.0f;
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
            float remainingDuration = endTime < 0.0f
                ? float.MaxValue
                : Mathf.Max(0.01f, endTime - Time.time);

            buffViewController?.ShowBuff(buffId, icon, remainingDuration);
        }    
    }
}