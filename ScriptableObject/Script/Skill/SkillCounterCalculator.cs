using UnityEngine;

public static class SkillCounterCalculator
{
    public static int GetCounterSuccessChance(
        CharacterData attacker,
        CharacterData counterUser,
        SkillDefinitionSO attackSkill,
        SkillDefinitionSO counterSkill,
        bool isProtectingOther)
    {
        if (attacker == null || counterUser == null)
            return 0;

        if (attackSkill == null || counterSkill == null)
            return 0;

        if (!CanUseCounterAgainst(
            attackSkill,
            counterSkill,
            isProtectingOther))
        {
            return 0;
        }

        float attackPower =
            GetSkillPower(attacker, attackSkill);

        float counterPower =
            GetSkillPower(counterUser, counterSkill);

        if (attackPower <= 0f)
            attackPower = 1f;

        float powerRatio =
            counterPower / attackPower;

        int chance = 50;

        chance += Mathf.RoundToInt(
            (powerRatio - 1f) * 30f);

        chance += GetStyleCounterBonus(
            counterUser,
            counterSkill);

        return chance;
    }

    public static float GetCounterPowerRatio(
        CharacterData attacker,
        CharacterData counterUser,
        SkillDefinitionSO attackSkill,
        SkillDefinitionSO counterSkill)
    {
        float attackPower = GetSkillPower(attacker, attackSkill);
        float counterPower = GetSkillPower(counterUser, counterSkill);

        if (attackPower <= 0f)
            return 1f;

        return counterPower / attackPower;
    }

    public static float GetCounterSuccessEffectScale(float counterPowerRatio)
    {
        return Mathf.Clamp(counterPowerRatio, 0.5f, 1.5f);
    }

    public static bool CanUseCounterAgainst(
    SkillDefinitionSO attackSkill,
    SkillDefinitionSO counterSkill,
    bool isProtectingOther)
    {
        if (attackSkill == null || counterSkill == null)
            return false;

        if (!counterSkill.isCounterSkill)
            return false;

        switch (counterSkill.counterActionType)
        {
            case CounterActionType.Guard:
                return true;

            case CounterActionType.Evade:
                return !isProtectingOther;

            case CounterActionType.Parry:
                return true;

            case CounterActionType.Break:
                return attackSkill.isRangedSkill &&
                       counterSkill.isRangedSkill;

            default:
                return false;
        }
    }

    private static float GetGreatSuccessRatio(CounterActionType actionType)
    {
        switch (actionType)
        {
            case CounterActionType.Guard:
                return 0.20f;

            case CounterActionType.Evade:
                return 0.20f;

            case CounterActionType.Parry:
                return 0.15f;

            case CounterActionType.Break:
                return 0.15f;

            default:
                return 0f;
        }
    }

    private static int GetStyleCounterBonus(CharacterData counterUser, SkillDefinitionSO counterSkill)
    {
        if (counterUser == null || counterUser.FinalStats == null || counterSkill == null)
            return 0;

        CharacterStats stats = counterUser.FinalStats;

        int statValue = counterSkill.style switch
        {
            SkillStyle.Strength => stats.Strength,
            SkillStyle.Dexterity => stats.Dexterity,
            SkillStyle.Speed => stats.Speed,
            _ => 0
        };

        return Mathf.RoundToInt(statValue * 0.5f);
    }

    private static float GetSkillPower(CharacterData character, SkillDefinitionSO skill)
    {
        if (character == null || character.FinalStats == null || skill == null)
            return 1f;

        CharacterStats stats = character.FinalStats;

        int basePower = skill.type switch
        {
            SkillType.Magical => stats.MagicalAttack,
            SkillType.Physical => stats.PhysicalAttack,
            SkillType.Mixed => Mathf.Max(stats.PhysicalAttack, stats.MagicalAttack),
            _ => stats.PhysicalAttack
        };

        float multiplier = skill.GetTotalDamageMultiplier();

        if (skill.isCounterSkill)
            multiplier = 1f;

        if (multiplier <= 0f)
            multiplier = 1f;

        return Mathf.Max(1f, basePower * multiplier);
    }
}