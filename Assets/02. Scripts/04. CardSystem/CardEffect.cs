using UnityEngine;

namespace Arcanist
{
    public abstract class CardEffect : ScriptableObject
    {
        public virtual bool RequiresTarget => false;

        public abstract void Execute(CardEffectContext context);
    }
}
