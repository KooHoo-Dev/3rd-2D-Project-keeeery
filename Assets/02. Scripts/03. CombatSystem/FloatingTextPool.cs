using UnityEngine;
using Utilities;

namespace Arcanist
{
    public class FloatingTextPool : ComponentPool<FloatingText>
    {
        public FloatingTextPool(FloatingText original, Transform parent = null) : base(original, parent)
        {
        }
    }
}
