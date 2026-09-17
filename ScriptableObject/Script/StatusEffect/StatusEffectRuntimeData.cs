using System;
using System.Collections.Generic;

[Serializable]
public class StatusEffectRuntimeData
{
    public int statusEffectId;
    public int stack;
    public string sourceCharacterId;
    public int remainingRounds;
    public List<StatusEffectStackData> damageStacks = new();
}

[Serializable]
public class StatusEffectStackData
{
    public int stack;
    public int damagePerStack;
    public string sourceCharacterId;
}
