public abstract class PhysicalAttackSkill : SkillBase, IPhysicalSkill
{
    public int EnduCost { get; private set; }

    protected PhysicalAttackSkill(string name, float damageMultiplier, double activationSpeed, int enduCost, SkillAttribute attribute)
    {
        SkillName = name;
        DamageMultiplier = damageMultiplier;
        ActivationSpeed = activationSpeed;
        Type = SkillType.Physical;
        EnduCost = enduCost;
        Attribute = attribute;
        CanUse = false;
    }
}

public abstract class BowAttackSkill : SkillBase, IPhysicalSkill
{
    public int EnduCost { get; private set; }

    protected BowAttackSkill(string name, float damageMultiplier, double activationSpeed, int enduCost, SkillAttribute attribute)
    {
        SkillName = name;
        ActivationSpeed = activationSpeed;
        DamageMultiplier = damageMultiplier;
        Type = SkillType.Physical;
        EnduCost = enduCost;
        Attribute = attribute;
        IsBowSkill = true;
        IsRangedSkill = true;
        CanUse = false;
    }
}

public abstract class MagicalAttackSkill : SkillBase, IMagicalSkill
{
    public int MentalCost { get; private set; }

    protected MagicalAttackSkill(string name, float damageMultiplier, double activationSpeed, int mentalCost, SkillAttribute attribute)
    {
        SkillName = name;
        ActivationSpeed = activationSpeed;
        DamageMultiplier = damageMultiplier;
        Type = SkillType.Magical;
        MentalCost = mentalCost;
        Attribute = attribute;
        CanUse = true;
    }
}

public abstract class BuffSkill : SkillBase, IBuffSkill
{
    protected BuffSkill(string name, double activationSpeed, bool isCounterSkill, SkillAttribute attribute)
    {
        SkillName = name;
        ActivationSpeed = activationSpeed;
        IsCounterSkill = isCounterSkill;
        Attribute = attribute;
        CanUse = true;
    }

    public abstract void ApplyBuff(CharacterData user, CharacterData target);
}

public abstract class DefensiveSkill : SkillBase, IDefensiveSkill
{
    protected DefensiveSkill(string name, float damageMultiplier, double activationSpeed, SkillAttribute attribute)
    {
        SkillName = name;
        ActivationSpeed = activationSpeed;
        DamageMultiplier = damageMultiplier;
        IsCounterSkill = true;
        Attribute = attribute;

        CanUse = true;
    }
}