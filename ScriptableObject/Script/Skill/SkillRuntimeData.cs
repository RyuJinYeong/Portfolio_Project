using System;

[Serializable]
public class SkillRuntimeData
{
    public int skillUid;

    public int useCount;
    public int killCount;
    public int damageCount;

    public bool quickSlot = false;
    public bool canUse = false;

    public bool useOffHand;
}
public static class SkillRuntimeFactory
{
    public static SkillRuntimeData Create(SkillDefinitionSO def)
    {
        if (def == null)
            return null;

        return new SkillRuntimeData
        {
            skillUid = def.uid,
            useCount = 0,
            killCount = 0,
            damageCount = 0,
            quickSlot = false,
            canUse = false,
            useOffHand = false
        };
    }
}