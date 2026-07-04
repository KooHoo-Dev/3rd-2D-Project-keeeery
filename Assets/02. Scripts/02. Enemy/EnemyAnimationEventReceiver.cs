using UnityEngine;

namespace Arcanist
{
    public class EnemyAnimationEventReceiver : MonoBehaviour
    {
        private EnemyController enemy;

        private void Awake()
        {
            enemy = GetComponentInParent<EnemyController>();
        }

        public void FireProjectile()
        {
            enemy?.FireProjectile();
        }
    }
}