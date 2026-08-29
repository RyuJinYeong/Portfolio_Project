using System.Collections.Generic;
using UnityEngine;

public static class MercenaryGenerator
{
    public static CharacterData Generate(MercenaryDefinitionSO template, int accountLevel)
    {
        if (template == null || template.baseOrigin == null)
            return null;

        System.Random random = new System.Random(System.Guid.NewGuid().GetHashCode());
        CharacterData character = template.baseOrigin.CreateRuntimeCharacterData();

        character.ID = System.Guid.NewGuid().ToString();
        character.customizationData = template.identityPool.CreateCustomization(random);
        character.Name = template.identityPool.PickName(
            character.customizationData.IsMale,
            string.IsNullOrEmpty(template.templateName)
                ? $"{template.baseOrigin.originName} 용병"
                : template.templateName,
            random);

        int minimumLevelOffset = Mathf.Min(template.minLevelOffset, template.maxLevelOffset);
        int maximumLevelOffset = Mathf.Max(template.minLevelOffset, template.maxLevelOffset);

        character.Level = Mathf.Max(
            1,
            accountLevel + random.Next(minimumLevelOffset, maximumLevelOffset + 1));

        ApplyFixedTraits(character, template.fixedTraitIds);
        ApplyFixedSkills(character, template.fixedSkillUids);

        TraitGenerationUtility.Apply(
            character,
            template.traitGeneration,
            0,
            random);

        ApplyRandomSkills(character, template.randomSkillPool, template.randomSkillCount, random);

        character.RemoveAllTraits(null);
        character.ApplyAllTraits(null);
        character.UpdateFinalStats();

        LevelGrowthUtility.ApplyAutomaticGrowth(
            character,
            character.Level,
            template.baseOrigin.levelUpStatWeights,
            random);

        RuntimeEquipmentApplier.ApplyRuntimeEquipments(character, template.fixedEquipments);
        ApplyRandomEquipments(
            character,
            template.randomEquipmentPool,
            template.randomEquipmentCount,
            random);

        character.RemoveAllTraits(null);
        character.ApplyAllTraits(null);
        character.UpdateFinalStats();

        character.CurrentHp = character.FinalStats.MaxHp;
        character.CurrentStamina = character.FinalStats.MaxStamina;
        character.CurrentMentality = character.FinalStats.MaxMentality;

        return character;
    }

    private static void ApplyFixedTraits(CharacterData character, List<int> traitIds)
    {
        if (character == null || traitIds == null)
            return;

        foreach (int traitId in traitIds)
        {
            TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(traitId);

            if (trait == null)
                continue;

            TraitGradeUtility.AddTrait(character.Traits, trait, trait.defaultAcquireGrade);
        }
    }

    private static void ApplyFixedSkills(CharacterData character, List<int> skillUids)
    {
        if (character == null || skillUids == null)
            return;

        foreach (int skillUid in skillUids)
        {
            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(skillUid);

            if (skill == null)
                continue;

            if (HasSkill(character, skillUid))
                continue;

            character.Skills.Add(SkillRuntimeFactory.Create(skill));
        }
    }

    private static void ApplyRandomSkills(
        CharacterData character,
        List<int> pool,
        int count,
        System.Random random)
    {
        if (character == null || pool == null || count <= 0)
            return;

        List<int> candidates = new List<int>(pool);

        for (int i = 0; i < count; i++)
        {
            if (candidates.Count == 0)
                return;

            int index = random.Next(0, candidates.Count);
            int skillUid = candidates[index];
            candidates.RemoveAt(index);

            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(skillUid);

            if (skill == null)
                continue;

            if (HasSkill(character, skillUid))
                continue;

            character.Skills.Add(SkillRuntimeFactory.Create(skill));
        }
    }

    private static void ApplyRandomEquipments(
        CharacterData character,
        List<EquipmentSpawnData> pool,
        int count,
        System.Random random)
    {
        if (character == null || pool == null || count <= 0)
            return;

        List<EquipmentSpawnData> candidates = new List<EquipmentSpawnData>(pool);

        for (int i = 0; i < count; i++)
        {
            if (candidates.Count == 0)
                return;

            int index = random.Next(0, candidates.Count);
            EquipmentSpawnData spawnData = candidates[index];
            candidates.RemoveAt(index);

            RuntimeEquipmentApplier.ApplyRuntimeEquipments(character, new List<EquipmentSpawnData> { spawnData });
        }
    }

    private static bool HasSkill(CharacterData character, int skillUid)
    {
        if (character == null || character.Skills == null)
            return false;

        return character.Skills.Exists(s => s != null && s.skillUid == skillUid);
    }
}
