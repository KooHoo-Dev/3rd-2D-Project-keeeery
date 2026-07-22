using UnityEngine;

namespace Arcanist
{
    public class Boss : Enemy
    {
        private BossController bossController;
        [SerializeField] private HealthBarUI healthBarUI;

        public override bool CanReceiveKnockback => false;
        public override bool CanReceiveStun => false;

        protected override void Awake()
        {
            base.Awake();
            bossController = GetComponent<BossController>();

            if (healthBarUI != null)
                healthBarUI.Initialize(Stat.Hp, Stat.MaxHp);
        }

        public override void TakeDamage(int damage)
        {
            int beforeHp = Stat.Hp;

            if (Stat.ApplyDamage(damage))
            {
                if (healthBarUI != null)
                    healthBarUI.ApplyDamage(beforeHp, Stat.Hp, Stat.MaxHp);

                bossController.Dead();
                return;
            }

            if (healthBarUI != null)
                healthBarUI.ApplyDamage(beforeHp, Stat.Hp, Stat.MaxHp);

            PlayHitEffect();
        }

        public override void TakeHeal(int heal)
        {
            if (heal <= 0)
                return;

            Stat.ApplyHeal(heal);

            if (healthBarUI != null)
                healthBarUI.ApplyHeal(Stat.Hp, Stat.MaxHp);
        }
    }
}
