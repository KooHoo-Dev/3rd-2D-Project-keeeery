using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class HitBox : MonoBehaviour
    {
        [field: SerializeField] public AttackInfo AttackInfo { get; private set; }
        [SerializeField] private bool explodeProjectileOnHit;
        [SerializeField] private ProjectileController projectileController;

        private CombatEntity Owner { get; set; }

        private readonly HashSet<HurtBox> checkList = new HashSet<HurtBox>();

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
        }

        public void Initialize(CombatEntity owner)
        {
            Owner = owner;
            checkList.Clear();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log($"[HitBox] {name} collided with {collision.name}");

            HurtBox hurtBox = CombatSystem.Instance.GetHurtBoxOrNull(collision);
            
            if (hurtBox == null)
            {
                Debug.LogWarning($"[HitBox] {collision.name} is not registered as HurtBox");
                return;
            }

            CombatEntity sender = Owner;
            CombatEntity receiver = hurtBox.Owner;

            if (sender == null || receiver == null)
                return;
            if (sender == receiver)
                return;

            PlayerController playerController = receiver.GetComponent<PlayerController>();

            if (playerController != null)
            {
                Debug.Log($"[Parry] Player hit detected. IsParrying: {playerController.IsParrying}, WindowOpen: {playerController.IsParryWindowOpen}");

                if (playerController.IsParryWindowOpen)
                {
                    Debug.Log("[Parry] Success");
                    playerController.SuccessParry();
                    return;
                }
            }

            if (checkList.Contains(hurtBox)) return;

            checkList.Add(hurtBox);

            int damage = AttackInfo.RollDamage();

            CombatEvent @event;
            @event.EventType = CombatEventType.DamageEvent;
            @event.Amount = damage;
            @event.Position = collision.ClosestPoint(sender.transform.position);

            CombatSystem.Instance.To(sender, receiver, @event);
            hurtBox.PlayHitFx(@event.Position);

            if (explodeProjectileOnHit && projectileController != null)
                projectileController.Explode();
        }
    }
}
