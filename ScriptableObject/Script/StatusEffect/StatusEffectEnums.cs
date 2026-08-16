
public enum StatusDurationType
{
    QueueTurn,   // 현재 전투턴/스킬큐 안에서만 지속
    FullTurn     // 여러 턴 지속
}

public enum StatusStackType
{
    None,
    Stack,
    RefreshDuration
}

public enum StatusEffectType
{
    None,

    TurnDamage,
    DamageTakenPerHit,

    CounterSuccessPenalty,
    PhysicalCounterPenalty,
    MagicalCounterPenalty,

    CancelNextCounter
}