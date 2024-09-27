using SoftKitty.InventoryEngine;

public abstract class PhysicalAttackSkill : SkillBase
{
    protected PhysicalAttackSkill(string name, float damageMultiplier, double activationSpeed, int staminaCost, SkillAttribute attribute)
    {
        StaminaCost = staminaCost;
        base.name = name;
        DamageMultiplier = damageMultiplier;
        ActivationSpeed = activationSpeed;
        Type = SkillType.Physical;
        Attribute = attribute;
        CanUse = false;
    }
}

public abstract class BowAttackSkill : SkillBase
{
    protected BowAttackSkill(string name, float damageMultiplier, double activationSpeed, int staminaCost, SkillAttribute attribute)
    {
        base.name = name;
        ActivationSpeed = activationSpeed;
        DamageMultiplier = damageMultiplier;
        Type = SkillType.Physical;
        StaminaCost = staminaCost;
        Attribute = attribute;
        IsBowSkill = true;
        IsRangedSkill = true;
        CanUse = false;
    }
}

public abstract class MagicalAttackSkill : SkillBase
{
    protected MagicalAttackSkill(string name, float damageMultiplier, double activationSpeed, int mentalCost, SkillAttribute attribute)
    {
        base.name = name;
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
        base.name = name;
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
        base.name = name;
        ActivationSpeed = activationSpeed;
        DamageMultiplier = damageMultiplier;
        IsCounterSkill = true;
        Attribute = attribute;
        CanUse = true;
    }
}