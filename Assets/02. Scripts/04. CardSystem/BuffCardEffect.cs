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

        public override void Execute(CardEffectContext context)
        {
            if (context.Player == null || context.Card == null) return;

            BuffController buffController = context.Player.GetComponent<BuffController>();
            if (buffController == null) return;

            string buffId = context.Card.CardId;
            Sprite icon = context.Card.Icon;

            for (int i = 0; i < modifiers.Count; i++)
            {
                BuffModifier modifier = modifiers[i];

                switch (modifier.EffectType)
                {
                    case BuffEffectType.DamageIncrease:
                        buffController.ApplyDamageBuff(buffId, icon, modifier.Value, duration);
                        break;

                    case BuffEffectType.TakenDamageDecrease:
                        buffController.ApplyTakenDamageDecreaseBuff(buffId, icon, modifier.Value, duration);
                        break;

                    case BuffEffectType.TakenDamageIncrease:
                        buffController.ApplyTakenDamageIncreaseBuff(buffId, icon, modifier.Value, duration);
                        break;

                    case BuffEffectType.CardGaugeChargeSpeedIncrease:
                        buffController.ApplyCardGaugeChargeSpeedBuff(buffId, icon, modifier.Value, duration);
                        break;
                }
            }
        }
    }
}
