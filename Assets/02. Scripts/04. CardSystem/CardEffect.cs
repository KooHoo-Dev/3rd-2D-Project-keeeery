using UnityEngine;

namespace Arcanist
{
    public abstract class CardEffect : ScriptableObject
    {
        public abstract void Execute(CardEffectContext context);
    }
}
