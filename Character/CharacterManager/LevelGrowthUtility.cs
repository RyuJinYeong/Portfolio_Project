using System;
using System.Collections.Generic;

[Serializable]
public class StatGrowthWeightData
{
    public StatRequirementType stat;
    public int weight = 1;
}

[Serializable]
public class LevelUpStatChoice
{
    public StatRequirementType stat;
    public int amount;
}

public static class LevelGrowthUtility
{
    private static readonly StatRequirementType[] DefaultStats =
    {
        StatRequirementType.Strength,
        StatRequirementType.Dexterity,
        StatRequirementType.Speed,
        StatRequirementType.Intelligence,
        StatRequirementType.Wisdom,
        StatRequirementType.Health,
        StatRequirementType.Vitality
    };

    public static void GetGrowthRange(
        CharacterData character,
        out int minAmount,
        out int maxAmount)
    {
        minAmount = 1;
        maxAmount = 3;

        if (character != null && character.FinalSpecialStats != null)
        {
            minAmount += character.FinalSpecialStats.MinLevelUpStatGainBonus;
            maxAmount += character.FinalSpecialStats.MaxLevelUpStatGainBonus;
        }

        minAmount = Math.Max(0, minAmount);
        maxAmount = Math.Max(minAmount, maxAmount);
    }

    public static List<LevelUpStatChoice> CreateChoices(
        int minAmount,
        int maxAmount,
        System.Random random)
    {
        List<StatRequirementType> candidates = new List<StatRequirementType>(DefaultStats);
        List<LevelUpStatChoice> result = new List<LevelUpStatChoice>();

        int minimum = Math.Max(0, minAmount);
        int maximum = Math.Max(minimum, maxAmount);

        while (result.Count < 3 && candidates.Count > 0)
        {
            StatRequirementType stat = candidates[random.Next(0, candidates.Count)];
            candidates.Remove(stat);

            result.Add(new LevelUpStatChoice
            {
                stat = stat,
                amount = random.Next(minimum, maximum + 1)
            });
        }

        return result;
    }

    public static void ApplyAutomaticGrowth(
        CharacterData character,
        int targetLevel,
        List<StatGrowthWeightData> weights,
        System.Random random)
    {
        if (character == null || character.OriginBaseStats == null)
            return;

        GetGrowthRange(character, out int minAmount, out int maxAmount);

        for (int level = 2; level <= Math.Max(1, targetLevel); level++)
        {
            List<LevelUpStatChoice> choices = CreateChoices(
                minAmount,
                maxAmount,
                random);

            LevelUpStatChoice selected = PickAutomaticChoice(choices, weights, random);

            if (selected != null)
                AddStat(character.OriginBaseStats, selected.stat, selected.amount);
        }

        character.BaseStats = character.OriginBaseStats.Copy();
    }

    public static bool ApplyPendingChoice(
        CharacterManager manager,
        LevelUpStatChoice choice)
    {
        CharacterData character = manager != null ? manager.character : null;

        if (character == null || character.OriginBaseStats == null ||
            character.PendingLevelUps <= 0 || choice == null ||
            Array.IndexOf(DefaultStats, choice.stat) < 0)
        {
            return false;
        }

        int previousMaxHp = character.FinalStats != null
            ? character.FinalStats.MaxHp
            : character.CurrentHp;

        AddStat(character.OriginBaseStats, choice.stat, choice.amount);
        character.OriginBaseStats.ClampNonNegative();
        character.PendingLevelUps--;

        character.RemoveAllTraits(manager);
        character.ApplyAllTraits(manager);
        character.UpdateFinalStats();

        int increasedMaxHp = Math.Max(0, character.FinalStats.MaxHp - previousMaxHp);
        character.CurrentHp = Math.Min(
            character.FinalStats.MaxHp,
            character.CurrentHp + increasedMaxHp);
        manager.UpdateCharacterUI();

        return true;
    }

    private static LevelUpStatChoice PickAutomaticChoice(
        List<LevelUpStatChoice> choices,
        List<StatGrowthWeightData> weights,
        System.Random random)
    {
        if (choices == null || choices.Count == 0)
            return null;

        int bestScore = int.MinValue;
        List<LevelUpStatChoice> bestChoices = new List<LevelUpStatChoice>();

        foreach (LevelUpStatChoice choice in choices)
        {
            int score = GetWeight(choice.stat, weights) * choice.amount;

            if (score > bestScore)
            {
                bestScore = score;
                bestChoices.Clear();
                bestChoices.Add(choice);
            }
            else if (score == bestScore)
            {
                bestChoices.Add(choice);
            }
        }

        return bestChoices[random.Next(0, bestChoices.Count)];
    }

    private static int GetWeight(StatRequirementType stat, List<StatGrowthWeightData> weights)
    {
        if (weights != null)
        {
            StatGrowthWeightData data = weights.Find(x => x != null && x.stat == stat);

            if (data != null && data.weight > 0)
                return data.weight;
        }

        return 1;
    }

    public static void AddStat(CharacterStats stats, StatRequirementType stat, int amount)
    {
        switch (stat)
        {
            case StatRequirementType.Strength: stats.Strength += amount; break;
            case StatRequirementType.Dexterity: stats.Dexterity += amount; break;
            case StatRequirementType.Speed: stats.Speed += amount; break;
            case StatRequirementType.Intelligence: stats.Intelligence += amount; break;
            case StatRequirementType.Wisdom: stats.Wisdom += amount; break;
            case StatRequirementType.Health: stats.Health += amount; break;
            case StatRequirementType.Vitality: stats.Vitality += amount; break;
            case StatRequirementType.Endurance: stats.Endurance += amount; break;
            case StatRequirementType.Detection: stats.Detection += amount; break;
            case StatRequirementType.Insight: stats.Insight += amount; break;
        }
    }
}
