using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TraitGenerationData
{
    [Min(1)]
    public int tier = 1;

    public List<int> fixedTraitPool = new();

    [Min(0)]
    public int fixedTraitCount;

    [Range(0, 100)]
    public int additionalTraitChance;

    [Range(0, 100)]
    public int additionalTraitChanceDecrease = 10;

    [Min(0)]
    public int maxTraitCount;

    [Min(0)]
    public int traitGradeBonus;
}

public static class TraitGenerationUtility
{
    public static TraitGenerationData GetForTier(
        List<TraitGenerationData> dataByTier,
        int tier)
    {
        if (dataByTier == null)
            return null;

        TraitGenerationData result = null;

        foreach (TraitGenerationData data in dataByTier)
        {
            if (data == null || data.tier > tier)
                continue;

            if (result == null || data.tier > result.tier)
                result = data;
        }

        return result;
    }

    public static void Apply(
        CharacterData character,
        TraitGenerationData data,
        int minimumTotalTraitCount,
        System.Random random)
    {
        if (character == null || data == null || random == null)
            return;

        int fixedCount = Math.Max(
            data.fixedTraitCount,
            Math.Max(0, minimumTotalTraitCount - character.Traits.Count));

        AddFromPool(
            character,
            data.fixedTraitPool,
            fixedCount,
            data.traitGradeBonus,
            random);

        if (character.Traits.Count < minimumTotalTraitCount)
        {
            AddFromDefinitions(
                character,
                GameDataRegistry.Instance.GetAllTraits(),
                minimumTotalTraitCount - character.Traits.Count,
                data.traitGradeBonus,
                random);
        }

        int maxTraitCount = Math.Max(
            minimumTotalTraitCount,
            Math.Max(data.maxTraitCount, character.Traits.Count));

        int chance = Mathf.Clamp(data.additionalTraitChance, 0, 100);
        int chanceDecrease = Mathf.Clamp(data.additionalTraitChanceDecrease, 0, 100);

        while (character.Traits.Count < maxTraitCount && chance > 0)
        {
            if (random.Next(0, 100) >= chance)
                break;

            if (!AddOneRandomTraitFromDefinitions(
                    character,
                    GameDataRegistry.Instance.GetAllTraits(),
                    data.traitGradeBonus,
                    random))
            {
                break;
            }

            chance = Math.Max(0, chance - chanceDecrease);
        }
    }

    private static void AddFromPool(
        CharacterData character,
        List<int> traitPool,
        int count,
        int gradeBonus,
        System.Random random)
    {
        if (traitPool == null || count <= 0)
            return;

        List<TraitDefinitionSO> candidates = new List<TraitDefinitionSO>();

        foreach (int traitId in traitPool)
        {
            TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(traitId);

            if (trait == null || character.HasCharacterTrait(traitId) || candidates.Contains(trait))
                continue;

            candidates.Add(trait);
        }

        AddFromDefinitions(character, candidates, count, gradeBonus, random);
    }

    private static void AddFromDefinitions(
        CharacterData character,
        List<TraitDefinitionSO> definitions,
        int count,
        int gradeBonus,
        System.Random random)
    {
        if (definitions == null || count <= 0)
            return;

        List<TraitDefinitionSO> candidates = GetUnownedTraits(character, definitions);

        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            TraitDefinitionSO trait = TakeWeightedRandomTrait(candidates, random);

            AddTrait(character, trait, gradeBonus);
        }
    }

    private static bool AddOneRandomTraitFromDefinitions(
        CharacterData character,
        List<TraitDefinitionSO> definitions,
        int gradeBonus,
        System.Random random)
    {
        if (definitions == null)
            return false;

        List<TraitDefinitionSO> candidates = GetUnownedTraits(character, definitions);

        if (candidates.Count == 0)
            return false;

        TraitDefinitionSO trait = TakeWeightedRandomTrait(candidates, random);
        TraitGrade grade = RollAdditionalTraitGrade(random, gradeBonus);
        TraitGradeUtility.AddTrait(character.Traits, trait, grade);
        return true;
    }

    private static TraitDefinitionSO TakeWeightedRandomTrait(
        List<TraitDefinitionSO> candidates,
        System.Random random)
    {
        int totalWeight = 0;

        foreach (TraitDefinitionSO trait in candidates)
            totalWeight += 64 >> (int)trait.defaultAcquireGrade;

        int roll = random.Next(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            TraitDefinitionSO trait = candidates[i];
            roll -= 64 >> (int)trait.defaultAcquireGrade;

            if (roll >= 0)
                continue;

            candidates.RemoveAt(i);
            return trait;
        }

        TraitDefinitionSO fallback = candidates[candidates.Count - 1];
        candidates.RemoveAt(candidates.Count - 1);
        return fallback;
    }

    private static TraitGrade RollAdditionalTraitGrade(
        System.Random random,
        int gradeBonus)
    {
        int roll = random.Next(0, 100);
        TraitGrade grade;

        if (roll < 28)
            grade = TraitGrade.F;
        else if (roll < 52)
            grade = TraitGrade.E;
        else if (roll < 72)
            grade = TraitGrade.D;
        else if (roll < 86)
            grade = TraitGrade.C;
        else if (roll < 94)
            grade = TraitGrade.B;
        else if (roll < 98)
            grade = TraitGrade.A;
        else
            grade = TraitGrade.S;

        int adjustedGrade = Mathf.Clamp(
            (int)grade + Math.Max(0, gradeBonus),
            (int)TraitGrade.F,
            (int)TraitGrade.S);

        return (TraitGrade)adjustedGrade;
    }

    private static List<TraitDefinitionSO> GetUnownedTraits(
        CharacterData character,
        List<TraitDefinitionSO> definitions)
    {
        List<TraitDefinitionSO> result = new List<TraitDefinitionSO>();

        foreach (TraitDefinitionSO trait in definitions)
        {
            if (trait == null || character.HasCharacterTrait(trait.id) || result.Contains(trait))
                continue;

            result.Add(trait);
        }

        return result;
    }

    private static void AddTrait(
        CharacterData character,
        TraitDefinitionSO trait,
        int gradeBonus)
    {
        int grade = Mathf.Clamp(
            (int)trait.defaultAcquireGrade + Math.Max(0, gradeBonus),
            (int)TraitGrade.F,
            (int)TraitGrade.S);

        TraitGradeUtility.AddTrait(
            character.Traits,
            trait,
            (TraitGrade)grade);
    }
}
