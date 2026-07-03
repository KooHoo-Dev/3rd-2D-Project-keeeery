using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public enum BuffEffectType
    {
        DamageIncrease,
        TakenDamageDecrease,
        TakenDamageIncrease,
        CardGaugeChargeSpeedIncrease,
        HealRatio,
        ReviveOnce,
    }

    [System.Serializable]
    public class BuffModifier
    {
        [SerializeField] private BuffEffectType effectType = BuffEffectType.DamageIncrease;
        [SerializeField] private float value = 1.0f;

        public BuffEffectType EffectType => effectType;
        public float Value => value;
    }

    [CreateAssetMenu(menuName = "Arcanist/Card Effects/Player Buff")]
    public class BuffCardEffect : CardEffect
    {
        [Header("Buff")]
        [SerializeField] private float duration = 10.0f;
        [SerializeField] private List<BuffModifier> modifiers = new List<BuffModifier>();

        public bool HasModifier(BuffEffectType effectType)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] != null && modifiers[i].EffectType == effectType)
                    return true;
            }

            return false;
        }
        
        public override void Execute(CardEffectContext context)
        {
            if (context.Player == null || context.Card == null) return;

            BuffController buffController = context.Player.GetComponent<BuffController>();
            Player player = context.Player.GetComponent<Player>();

            string buffId = context.Card.CardId;
            Sprite icon = context.Card.Icon;

            for (int i = 0; i < modifiers.Count; i++)
            {
                BuffModifier modifier = modifiers[i];

                switch (modifier.EffectType)
                {
                    case BuffEffectType.DamageIncrease:
                        if (buffController != null)
                            buffController.ApplyDamageBuff(buffId, icon, modifier.Value, duration);
                        break;

                    case BuffEffectType.TakenDamageDecrease:
                        if (buffController != null)
                            buffController.ApplyTakenDamageDecreaseBuff(buffId, icon, modifier.Value, duration);
                        break;

                    case BuffEffectType.TakenDamageIncrease:
                        if (buffController != null)
                            buffController.ApplyTakenDamageIncreaseBuff(buffId, icon, modifier.Value, duration);
                        break;

                    case BuffEffectType.CardGaugeChargeSpeedIncrease:
                        if (buffController != null)
                            buffController.ApplyCardGaugeChargeSpeedBuff(buffId, icon, modifier.Value, duration);
                        break;

                    case BuffEffectType.HealRatio:
                        if (player != null)
                        {
                            int healAmount = Mathf.RoundToInt(player.Stat.MaxHp * modifier.Value);
                            player.TakeHeal(healAmount);
                        }
                        break;

                    case BuffEffectType.ReviveOnce:
                        if (buffController != null)
                            buffController.ApplyReviveOnceBuff(buffId, icon, modifier.Value, duration);
                        break;
                }
            }
        }
    }
}
