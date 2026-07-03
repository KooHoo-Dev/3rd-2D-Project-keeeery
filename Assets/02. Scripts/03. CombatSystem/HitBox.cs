using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class HitBox : MonoBehaviour
    {
        [field: SerializeField] public AttackInfo AttackInfo { get; private set; }
        [SerializeField] private bool explodeProjectileOnHit;
        [SerializeField] private ProjectileController projectileController;

        [Header("Multi Hit")]
        [SerializeField] private bool allowMultiHit;
        [SerializeField] private float multiHitInterval = 0.25f;

        private CombatEntity Owner { get; set; }

        private readonly HashSet<HurtBox> checkList = new HashSet<HurtBox>();
        private readonly Dictionary<HurtBox, float> lastHitTimes = new Dictionary<HurtBox, float>();

        private void Awake()
        {
            Owner = GetComponentInParent<CombatEntity>();

            if (projectileController == null)
                projectileController = GetComponentInParent<ProjectileController>();

            // if (Owner == null && projectileController == null)
            // {
            //     Debug.LogError($"{name} : 부모 계층에서 CombatEntity 또는 ProjectileController를 찾지 못했습니다. 오브젝트를 삭제합니다");
            //     Destroy(gameObject);
            // }
        }

        private void OnEnable()
        {
            checkList.Clear();
            lastHitTimes.Clear();
        }

        public void Initialize(CombatEntity owner)
        {
            Owner = owner;
            checkList.Clear();
            lastHitTimes.Clear();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            TryHit(collision);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (allowMultiHit == false)
                return;

            TryHit(collision);
        }

        private void TryHit(Collider2D collision)
        {
            HurtBox hurtBox = CombatSystem.Instance.GetHurtBoxOrNull(collision);

            if (hurtBox == null) return;

            CombatEntity sender = Owner;
            CombatEntity receiver = hurtBox.Owner;

            if (sender == null || receiver == null) return;

            if (sender == receiver) return;

            EnemyController enemy = receiver.GetComponent<EnemyController>();

            if (enemy != null && enemy.IsDead) return;

            PlayerController playerController = receiver.GetComponent<PlayerController>();

            if (playerController != null && playerController.IsParryWindowOpen)
            {
                playerController.SuccessParry();
                return;
            }

            if (CanHit(hurtBox) == false) return;

            RegisterHit(hurtBox);

            int damage = AttackInfo.RollDamage();

            CombatEvent @event;
            @event.EventType = CombatEventType.DamageEvent;
            @event.Amount = damage;
            @event.Position = collision.ClosestPoint(sender.transform.position);

            hurtBox.PlayHitFx(@event.Position);
            CombatSystem.Instance.To(sender, receiver, @event);

            if (explodeProjectileOnHit && projectileController != null)
                projectileController.Explode();
        }

        private bool CanHit(HurtBox hurtBox)
        {
            if (allowMultiHit == false)
                return checkList.Contains(hurtBox) == false;

            if (lastHitTimes.TryGetValue(hurtBox, out float lastHitTime) == false)
                return true;

            return Time.time - lastHitTime >= multiHitInterval;
        }

        private void RegisterHit(HurtBox hurtBox)
        {
            if (allowMultiHit)
            {
                lastHitTimes[hurtBox] = Time.time;
                return;
            }

            checkList.Add(hurtBox);
        }
    }
}
