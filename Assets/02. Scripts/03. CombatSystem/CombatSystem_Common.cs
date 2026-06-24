using UnityEngine;

namespace Arcanist
{
    public enum CombatEventType
    {
        DamageEvent, HealEvent
    }

    public struct CombatEvent
    {
        public CombatEventType EventType;
        public int Amount;
        public Vector3 Position;
    }

    // interface
    // - 클래스나 구조체가 반드시 제공해야하는 멤버(메소드, 속성, 이벤트, 인덱서)의 
    //   시그니처만 명시한 일종의 "계약"이자 "설계도"
    // - 무엇을 할 지만 정의할 뿐, 어떻게 할 지는 상속자에게 위임
    //   철저한 추상화를 위해 사용
    // - 추상 클래스와는 달리 필드가 없어서 더 유연한 상속 구조를 제공
    // - 추상 클래스는 형태가 있는 것을 사옥하여 수직적 구조를 갖게 되는 반면,
    //   인터페이스는 무형의 기능만 상속하기에 수평적 구조를 가짐
    // - C++의 다중 상속의 문제점을 해결하기 위해 제안된 개념

    public interface ICombatObserver
    {
        void OnDamageTaken(CombatEntity sender, CombatEntity receiver, CombatEvent @event);
        void OnHealTaken(CombatEntity sender, CombatEntity receiver, CombatEvent @event);
    }
}
