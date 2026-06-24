using Utilities;
using UnityEngine;

namespace Arcanist
{
    public class DebugFloatingText : MonoBehaviour
    {
        public CombatEntity entity;

        void Update()
        {
            if(SimpleInput.GetKeyDown(UnityEngine.InputSystem.Key.Space))
            {
                CombatEvent @event;
                @event.EventType = CombatEventType.HealEvent;
                @event.Amount = 10;
                @event.Position = entity.transform.position;

                CombatSystem.Instance.To(entity, entity, @event);
            }
        }
    }
}
