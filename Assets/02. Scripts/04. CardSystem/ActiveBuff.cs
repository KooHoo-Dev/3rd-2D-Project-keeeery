namespace Arcanist
{
    public enum ActiveBuffType
    {
        OutgoingDamageIncrease,
        IncomingDamageIncrease,
        IncomingDamageReduction,
        CardGaugeSpeedIncrease,
        Invincibility,
    }

    /// <summary>
    /// ScriptableObject의 버프 정의가 플레이 중 적용된 상태를 나타냅니다.
    /// </summary>
    public sealed class ActiveBuff
    {
        public string BuffId { get; }
        public ActiveBuffType Type { get; }
        public float Value { get; private set; }
        public float EndTime { get; private set; }

        public ActiveBuff(string buffId, ActiveBuffType type, float value, float endTime)
        {
            BuffId = buffId;
            Type = type;
            Refresh(value, endTime);
        }

        public bool IsExpired(float currentTime)
        {
            return currentTime > EndTime;
        }

        public bool Matches(string buffId, ActiveBuffType type)
        {
            return BuffId == buffId && Type == type;
        }

        public void Refresh(float value, float endTime)
        {
            Value = value;
            EndTime = endTime;
        }
    }
}
