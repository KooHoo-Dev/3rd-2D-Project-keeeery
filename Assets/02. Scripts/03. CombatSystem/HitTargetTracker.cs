using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    internal sealed class HitTargetTracker
    {
        private readonly bool allowMultiHit;
        private readonly float multiHitInterval;
        private readonly HashSet<HurtBox> hitTargets = new();
        private readonly Dictionary<HurtBox, float> lastHitTimes = new();

        public HitTargetTracker(bool allowMultiHit, float multiHitInterval)
        {
            this.allowMultiHit = allowMultiHit;
            this.multiHitInterval = Mathf.Max(0.0f, multiHitInterval);
        }

        public bool TryRegister(HurtBox hurtBox)
        {
            if (hurtBox == null)
                return false;

            if (allowMultiHit == false)
                return hitTargets.Add(hurtBox);

            if (lastHitTimes.TryGetValue(hurtBox, out float lastHitTime)
                && Time.time - lastHitTime < multiHitInterval)
                return false;

            lastHitTimes[hurtBox] = Time.time;
            return true;
        }

        public void Reset()
        {
            hitTargets.Clear();
            lastHitTimes.Clear();
        }
    }
}
