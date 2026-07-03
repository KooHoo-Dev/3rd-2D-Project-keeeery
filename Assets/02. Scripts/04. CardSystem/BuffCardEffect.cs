using UnityEngine;

namespace Arcanist
{
    [CreateAssetMenu(menuName = "Arcanist/Card Effects/Player Buff")]
    public class BuffCardEffect : CardEffect
    {
        [SerializeField] private float damageMultiplier = 1.3f;
        [SerializeField] private float duration = 10.0f;

        public override void Execute(CardEffectContext context)
        {
            if (context.Player == null) return;

            BuffController buffController = context.Player.GetComponent<BuffController>();

            if (buffController == null) return;

            buffController.ApplyDamageBuff(
                context.Card.CardId, 
                context.Card.Icon, 
                damageMultiplier, 
                duration);
        }
    }
}
