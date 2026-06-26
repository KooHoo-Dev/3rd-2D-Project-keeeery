using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    public class HitBox : MonoBehaviour
    {
        [field: SerializeField] public AttackInfo AttackInfo { get; private set; }
        [SerializeField] private ParryLightEffect parryLightEffect;
        [SerializeField] private ScreenEffectController screenEffectController;
        [SerializeField] private GameObject hitFxPrefab;
        [SerializeField] private bool playHitFxOnlyByPlayer = true;
        [SerializeField] private float hitFxLocalYOffset = 0.0f;

        private CombatEntity Owner { get; set; }

        private HashSet<HurtBox> checkList = new HashSet<HurtBox>();

        private void Awake()
        {
            Owner = GetComponentInParent<CombatEntity>();
            
            if(Owner == null)
            {
                Debug.LogError($"{name} : 부모 계층에서 CombatEntity를 찾지 못했습니다. 오브젝트를 삭제합니다");
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
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

            PlayerController playerController = receiver.GetComponent<PlayerController>();

            if (playerController != null)
            {
                Debug.Log($"[Parry] Player hit detected. IsParrying: {playerController.IsParrying}, WindowOpen: {playerController.IsParryWindowOpen}");

                if (playerController.IsParryWindowOpen)
                {
                    Debug.Log("[Parry] Success");
                    playerController.SuccessParry();
                    parryLightEffect.Play();
                    screenEffectController.PlayParryGlow();
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
            PlayHitFx(sender, @event.Position);
        }

        private void PlayHitFx(CombatEntity sender, Vector3 position)
        {
            if (hitFxPrefab == null) return;

            bool isPlayerAttack = sender.GetComponent<PlayerController>() != null;

            if (playHitFxOnlyByPlayer && !isPlayerAttack) return;

            Quaternion rotation = Quaternion.Euler(0.0f, 0.0f, Random.Range(0.0f, 360.0f));
            Vector3 offset = new Vector3(0.0f, hitFxLocalYOffset, 0.0f);
            GameObject fx = Instantiate(hitFxPrefab, position + offset, rotation);

            float direction = sender.transform.lossyScale.x >= 0.0f ? 1.0f : -1.0f;
            fx.transform.localScale = new Vector3(direction, 1.0f, 1.0f);
        }
    }
}
