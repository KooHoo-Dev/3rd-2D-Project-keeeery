using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Arcanist
{
    public class CombatSystem : SingletonBase<CombatSystem>
    {
        private readonly List<ICombatObserver> observerList = new();

        // protected override void OnInitialize()
        // {
        //     base.OnInitialize();
        // }

        public void Subscribe(ICombatObserver observer)
        {
            if (observerList.Contains(observer)) return;
            observerList.Add(observer);
        }

        public void UnSubscribe(ICombatObserver observer)
        {
            if (observerList.Contains(observer) == false) return;
            observerList.Remove(observer);
        }

        public void To(CombatEntity sender, CombatEntity receiver, CombatEvent @event)
        {
            if (sender == null || receiver == null)
            {
                Debug.LogWarning($"CombatSystem ::: " +
                    $"sender : {sender != null}, receiver : {receiver != null}");
                return;
            }

            // Debug.Log($"{sender.name}이 {receiver.name}에게 {@event.EventType.ToString()}" +
            //     $", {@event.Amount}을 전달!");

            switch (@event.EventType)
            {
                case CombatEventType.DamageEvent:
                    HandleDamageEvent(sender, receiver, @event);
                    break;
                case CombatEventType.HealEvent:
                    HandleHealEvent(sender, receiver, @event);
                    break;
            }
        }

        private void HandleDamageEvent(CombatEntity sender, CombatEntity receiver, CombatEvent @event)
        {
            receiver.TakeDamage(@event.Amount);

            for (int i = 0; i < observerList.Count; ++i)
                observerList[i].OnDamageTaken(sender, receiver, @event);
        }

        private void HandleHealEvent(CombatEntity sender, CombatEntity receiver, CombatEvent @event)
        {
            for (int i = 0; i < observerList.Count; ++i)
                observerList[i].OnHealTaken(sender, receiver, @event);
        }

        private Dictionary<Collider2D, HurtBox> HurtBoxDic { get; set; } = new();

        public void AddHurtBox(Collider2D collider, HurtBox hurtBox)
        {
            HurtBoxDic.TryAdd(collider, hurtBox);
        }

        public void RemoveHurtBox(Collider2D collider)
        {
            if (HurtBoxDic.Remove(collider) == false) return;
        }

        public HurtBox GetHurtBoxOrNull(Collider2D collider)
        {
            if (collider == null)
                return null;

            if (HurtBoxDic.TryGetValue(collider, out HurtBox reval))
                return reval;

            HurtBox hurtBox = collider.GetComponentInParent<HurtBox>();
            if (hurtBox != null)
                return hurtBox;

            CombatEntity entity = collider.GetComponentInParent<CombatEntity>();
            if (entity == null)
                return null;

            return entity.GetComponentInChildren<HurtBox>();
        }
    }
}
