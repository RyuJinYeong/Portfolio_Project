using System.Collections.Generic;
using UnityEngine;

public static class StatusEffectCalculator
{
    public static int GetStatusApplyChance(
        CharacterData caster,
        CharacterData target,
        SkillDefinitionSO skill,
        StatusEffectApplyData effect)
    {
        if (caster == null ||
            target == null ||
            effect == null)
        {
            return 0;
        }

        if (caster.FinalStats == null)
            return 0;

        StatusEffectDefinitionSO definition =
            GameDataRegistry.Instance.GetStatusEffect(
                effect.statusEffectId);

        if (definition == null)
            return 0;

        float attackerPower = EvaluateStatWeights(
            caster.FinalStats,
            definition.attackerStatWeights);

        int statusResistance = 0;

        if (target.FinalSpecialStats != null)
        {
            statusResistance =
                target.FinalSpecialStats.StatusResistance;
        }

        int skillAccuracyBonus =
            skill != null
                ? skill.statusAccuracyBonus
                : 0;

        float finalChance =
            effect.baseChance +
            skillAccuracyBonus +
            attackerPower -
            statusResistance;

        return Mathf.Clamp(
            Mathf.RoundToInt(finalChance),
            0,
            100);
    }

    public static bool RollStatusApply(
        CharacterData caster,
        CharacterData target,
        SkillDefinitionSO skill,
        StatusEffectApplyData effect,
        int roll)
    {
        int chance = GetStatusApplyChance(
            caster,
            target,
            skill,
            effect);

        return roll <= chance;
    }

    private static float EvaluateStatWeights(
        CharacterStats stats,
        List<StatusStatWeightData> weights)
    {
        if (stats == null || weights == null)
            return 0f;

        float result = 0f;

        foreach (StatusStatWeightData data in weights)
        {
            if (data == null)
                continue;

            result +=
                GetStatValue(stats, data.stat) *
                data.weight;
        }

        return result;
    }

    private static int GetStatValue(
        CharacterStats stats,
        StatRequirementType stat)
    {
        return stat switch
        {
            StatRequirementType.Strength =>
                stats.Strength,

            StatRequirementType.Dexterity =>
                stats.Dexterity,

            StatRequirementType.Speed =>
                stats.Speed,

            StatRequirementType.Intelligence =>
                stats.Intelligence,

            StatRequirementType.Wisdom =>
                stats.Wisdom,

            StatRequirementType.Health =>
                stats.Health,

            StatRequirementType.Vitality =>
                stats.Vitality,

            StatRequirementType.Endurance =>
                stats.Endurance,

            StatRequirementType.Detection =>
                stats.Detection,

            StatRequirementType.Insight =>
                stats.Insight,

            _ => 0
        };
    }
}