using UnityEngine;

namespace Arcanist
{
    public class BossAnimationEventReceiver : MonoBehaviour
    {
        [Header("Melee")]
        [SerializeField] private Collider2D meleeHitBox;

        [Header("Laser")]
        [SerializeField] private GameObject laserObject;

        [Header("Arm Shoot")]
        [SerializeField] private GolemShootState armShootState;

        private void Awake()
        {
            DisableMeleeHitBox();
            StopLaser();
        }

        public void EnableMeleeHitBox()
        {
            if (meleeHitBox != null)
                meleeHitBox.enabled = true;
        }

        public void DisableMeleeHitBox()
        {
            if (meleeHitBox != null)
                meleeHitBox.enabled = false;
        }

        public void FireLaser()
        {
            if (laserObject != null)
                laserObject.SetActive(true);
        }

        public void StopLaser()
        {
            if (laserObject != null)
                laserObject.SetActive(false);
        }

        public void ShootArm()
        {
            if (armShootState != null)
                armShootState.ShootArm();
        }
    }
}