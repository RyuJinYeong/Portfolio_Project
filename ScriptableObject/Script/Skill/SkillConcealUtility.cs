using System.Collections.Generic;
using UnityEngine;

public static class SkillConcealUtility
{
    public const float ConcealCostRate = 0.3f;
    public const int ConcealCountPenalty = 10;
    public const int FullRevealCounterBonus = 15;

    public static int GetAdditionalStaminaCost(SkillDefinitionSO skill)
    {
        if (skill == null)
            return 0;

        return Mathf.CeilToInt(skill.staminaCost * ConcealCostRate);
    }

    public static int GetAdditionalMentalCost(SkillDefinitionSO skill)
    {
        if (skill == null)
            return 0;

        int staminaAdd = GetAdditionalStaminaCost(skill);
        int mentalAdd = Mathf.CeilToInt(skill.mentalCost * ConcealCostRate);

        if (staminaAdd <= 0 && mentalAdd <= 0)
            mentalAdd = 1;

        return mentalAdd;
    }

    public static int GetPerceptionScore(CharacterData character)
    {
        if (character == null || character.FinalStats == null)
            return 0;

        int high = Mathf.Max(character.FinalStats.Detection, character.FinalStats.Insight);
        int low = Mathf.Min(character.FinalStats.Detection, character.FinalStats.Insight);

        return high + low / 2;
    }

    public static CharacterData GetBestPerceiver(List<CharacterData> defenders)
    {
        if (defenders == null || defenders.Count == 0)
            return null;

        CharacterData best = null;
        int bestScore = int.MinValue;

        foreach (var character in defenders)
        {
            int score = GetPerceptionScore(character);

            if (score > bestScore)
            {
                best = character;
                bestScore = score;
            }
        }

        return best;
    }

    public static int GetConcealDifficulty(
        CharacterData attacker,
        SkillDefinitionSO skill,
        int concealedSkillCount)
    {
        if (attacker == null || skill == null)
            return 0;

        int styleStat = SkillStyleUtility.GetStyleStat(attacker, skill.style);
        int countPenalty = Mathf.Max(0, concealedSkillCount - 1) * ConcealCountPenalty;

        return Mathf.Max(0, styleStat - countPenalty);
    }

    public static int GetRevealChance(
        CharacterData perceiver,
        CharacterData attacker,
        SkillDefinitionSO skill,
        int concealedSkillCount)
    {
        int perceptionScore = GetPerceptionScore(perceiver);
        int concealDifficulty = GetConcealDifficulty(attacker, skill, concealedSkillCount);

        int diff = perceptionScore - concealDifficulty;
        int chance = 50 + diff * 3;

        return Mathf.Clamp(chance, 10, 90);
    }

    public static RevealLevel GetRevealLevelByRoll(int revealChance, int roll)
    {
        revealChance = Mathf.Clamp(revealChance, 10, 90);
        roll = Mathf.Clamp(roll, 1, 100);

        if (roll <= revealChance / 2)
            return RevealLevel.Full;

        if (roll <= revealChance)
            return RevealLevel.Partial;

        return RevealLevel.None;
    }
}