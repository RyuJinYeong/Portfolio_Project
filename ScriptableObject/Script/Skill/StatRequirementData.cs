using System;

public enum StatRequirementType
{
    Strength,
    Dexterity,
    Speed,
    Intelligence,
    Wisdom,
    Health,
    Vitality,
    Endurance,
    Detection,
    Insight
}

[Serializable]
public class StatRequirementData
{
    public StatRequirementType stat;
    public int requiredValue;
}