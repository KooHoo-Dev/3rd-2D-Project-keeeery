using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class BuffController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private BuffViewController buffViewController;
        [SerializeField] private ScreenEffectController screenEffectController;

        private readonly List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
        private bool hasReviveOnceBuff;
        private string reviveBuffId;
        private Sprite reviveBuffIcon;
        private float reviveHealRatio;
        private float reviveInvincibleDuration;

        public void ApplyDamageBuff(string buffId, Sprite icon, float multiplier, float duration)
        {
            float endTime = Time.time + Mathf.Max(0.0f, duration);
            AddOrRefreshBuff(buffId, ActiveBuffType.OutgoingDamageIncrease, multiplier, endTime);

            ShowBuffUI(buffId, icon, endTime);
        }

        public void ApplyTakenDamageDecreaseBuff(string buffId, Sprite icon, float reductionRatio, float duration)
        {
            float endTime = Time.time + Mathf.Max(0.0f, duration);
            AddOrRefreshBuff(buffId, ActiveBuffType.IncomingDamageReduction, reductionRatio, endTime);

            ShowBuffUI(buffId, icon, endTime);
        }

        public void ApplyTakenDamageIncreaseBuff(string buffId, Sprite icon, float multiplier, float duration)
        {
            float endTime = Time.time + Mathf.Max(0.0f, duration);
            AddOrRefreshBuff(buffId, ActiveBuffType.IncomingDamageIncrease, multiplier, endTime);

            ShowBuffUI(buffId, icon, endTime);
        }

        public void ApplyCardGaugeChargeSpeedBuff(string buffId, Sprite icon, float multiplier, float duration)
        {
            float endTime = Time.time + Mathf.Max(0.0f, duration);
            AddOrRefreshBuff(buffId, ActiveBuffType.CardGaugeSpeedIncrease, multiplier, endTime);

            ShowBuffUI(buffId, icon, endTime);
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
                float endTime = Time.time + consumedInvincibleDuration;
                AddOrRefreshBuff(consumedBuffId, ActiveBuffType.Invincibility, 1.0f, endTime);
                ShowBuffUI(consumedBuffId, consumedBuffIcon, endTime);
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
            RemoveExpiredBuffs();

            float increaseRatio = 0.0f;
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                ActiveBuff buff = activeBuffs[i];
                if (buff.Type == ActiveBuffType.OutgoingDamageIncrease)
                    increaseRatio += Mathf.Max(0.0f, buff.Value - 1.0f);
            }

            return Mathf.RoundToInt(damage * (1.0f + increaseRatio));
        }

        public int ModifyTakenDamage(int damage)
        {
            RemoveExpiredBuffs();

            float increaseRatio = 0.0f;
            float reductionMultiplier = 1.0f;

            for (int i = 0; i < activeBuffs.Count; i++)
            {
                ActiveBuff buff = activeBuffs[i];
                switch (buff.Type)
                {
                    case ActiveBuffType.Invincibility:
                        return 0;

                    case ActiveBuffType.IncomingDamageIncrease:
                        increaseRatio += Mathf.Max(0.0f, buff.Value - 1.0f);
                        break;

                    case ActiveBuffType.IncomingDamageReduction:
                        reductionMultiplier *= 1.0f - Mathf.Clamp01(buff.Value);
                        break;
                }
            }

            float increasedDamage = damage * (1.0f + increaseRatio);
            return Mathf.RoundToInt(increasedDamage * reductionMultiplier);
        }

        public float ModifyCardGaugeChargeTime(float seconds)
        {
            RemoveExpiredBuffs();

            float increaseRatio = 0.0f;
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                ActiveBuff buff = activeBuffs[i];
                if (buff.Type == ActiveBuffType.CardGaugeSpeedIncrease)
                    increaseRatio += Mathf.Max(0.0f, buff.Value - 1.0f);
            }

            return seconds / (1.0f + increaseRatio);
        }

        private void AddOrRefreshBuff(string buffId, ActiveBuffType type, float value, float endTime)
        {
            RemoveExpiredBuffs();

            for (int i = 0; i < activeBuffs.Count; i++)
            {
                ActiveBuff buff = activeBuffs[i];
                if (buff.Matches(buffId, type))
                {
                    buff.Refresh(value, endTime);
                    return;
                }
            }

            activeBuffs.Add(new ActiveBuff(buffId, type, value, endTime));
        }

        private void RemoveExpiredBuffs()
        {
            float currentTime = Time.time;
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (activeBuffs[i].IsExpired(currentTime))
                    activeBuffs.RemoveAt(i);
            }
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
