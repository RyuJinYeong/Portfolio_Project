using UnityEngine;

public static class SkillCounterCalculator
{
    private const int ProtectionTraitId = 1029;
    private const int ProtectionSuccessBonusPercent = 20;

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
            attacker,
            counterUser,
            attackSkill,
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

    public static int ApplyDefenseSkillSuccessRateBonus(
        CharacterData counterUser,
        SkillDefinitionSO counterSkill,
        int currentChance,
        bool isProtectingOther)
    {
        if (counterUser == null || counterSkill == null || currentChance <= 0)
            return currentChance;

        if (counterSkill.counterActionType != CounterActionType.Guard &&
            counterSkill.counterActionType != CounterActionType.Parry &&
            counterSkill.counterActionType != CounterActionType.Evade)
        {
            return currentChance;
        }

        int bonusPercent = counterUser.FinalSpecialStats != null
            ? counterUser.FinalSpecialStats.DefenseSkillSuccessRateBonus
            : 0;

        if (isProtectingOther && counterUser.HasTrait(ProtectionTraitId))
            bonusPercent += ProtectionSuccessBonusPercent;

        return currentChance + Mathf.RoundToInt(currentChance * bonusPercent / 100f);
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

    private static int GetStyleCounterBonus(
        CharacterData attacker,
        CharacterData counterUser,
        SkillDefinitionSO attackSkill,
        SkillDefinitionSO counterSkill)
    {
        if (attacker == null || attacker.FinalStats == null || attackSkill == null ||
            counterUser == null || counterUser.FinalStats == null || counterSkill == null)
            return 0;

        int counterStatValue = GetStyleStat(counterUser.FinalStats, counterSkill.style);
        int attackStatValue = GetStyleStat(attacker.FinalStats, attackSkill.style);

        return Mathf.RoundToInt((counterStatValue - attackStatValue) * 0.5f);
    }

    private static int GetStyleStat(CharacterStats stats, SkillStyle style)
    {
        if (stats == null)
            return 0;

        return style switch
        {
            SkillStyle.Strength => stats.Strength,
            SkillStyle.Dexterity => stats.Dexterity,
            SkillStyle.Speed => stats.Speed,
            _ => 0
        };
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
        {
            multiplier = skill.counterActionType switch
            {
                CounterActionType.Guard => skill.successArmorAttackMultiplier,
                CounterActionType.Parry => skill.successArmorAttackMultiplier,
                _ => 1f
            };
        }

        if (multiplier <= 0f)
            multiplier = 1f;

        return Mathf.Max(1f, basePower * multiplier);
    }
}
