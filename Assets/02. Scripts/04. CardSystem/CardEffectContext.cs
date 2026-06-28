using UnityEngine;

namespace Arcanist
{
    public readonly struct CardEffectContext
    {
        public CardData Card { get; }
        public PlayerController Player { get; }
        public Transform CastPoint { get; }
        public int FacingDirection { get; }
        public bool HasTarget { get; }
        public Transform Target { get; }
        public Vector3 TargetPosition { get; }

        public CardEffectContext(CardData card, PlayerController player, Transform castPoint, int facingDirection, Transform target)
        {
            Card = card;
            Player = player;
            CastPoint = castPoint;
            FacingDirection = facingDirection;
            Target = target;
            HasTarget = target != null;
            TargetPosition = target != null ? target.position : Vector3.zero;
        }
    }
}
