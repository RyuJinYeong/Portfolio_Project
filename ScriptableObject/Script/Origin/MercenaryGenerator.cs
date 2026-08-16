using System.Collections.Generic;
using UnityEngine;

public static class MercenaryGenerator
{
    public static CharacterData Generate(MercenaryDefinitionSO template, int accountLevel)
    {
        if (template == null || template.baseOrigin == null)
            return null;

        CharacterData character = template.baseOrigin.CreateRuntimeCharacterData();

        character.Level = Mathf.Max(1, accountLevel + Random.Range(template.minLevelOffset, template.maxLevelOffset + 1));

        ApplyFixedTraits(character, template.fixedTraitIds);
        ApplyFixedSkills(character, template.fixedSkillUids);

        ApplyRandomTraits(character, template.randomTraitPool, template.randomTraitCount);
        ApplyRandomSkills(character, template.randomSkillPool, template.randomSkillCount);

        RuntimeEquipmentApplier.ApplyRuntimeEquipments(character, template.fixedEquipments);
        ApplyRandomEquipments(character, template.randomEquipmentPool, template.randomEquipmentCount);

        character.RemoveAllTraits(null);
        character.ApplyAllTraits(null);
        character.UpdateFinalStats();

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

    private static void ApplyRandomTraits(CharacterData character, List<int> pool, int count)
    {
        if (character == null || pool == null || count <= 0)
            return;

        List<int> candidates = new List<int>(pool);

        for (int i = 0; i < count; i++)
        {
            if (candidates.Count == 0)
                return;

            int index = Random.Range(0, candidates.Count);
            int traitId = candidates[index];
            candidates.RemoveAt(index);

            TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(traitId);

            if (trait == null)
                continue;

            TraitGradeUtility.AddTrait(character.Traits, trait, trait.defaultAcquireGrade);
        }
    }

    private static void ApplyRandomSkills(CharacterData character, List<int> pool, int count)
    {
        if (character == null || pool == null || count <= 0)
            return;

        List<int> candidates = new List<int>(pool);

        for (int i = 0; i < count; i++)
        {
            if (candidates.Count == 0)
                return;

            int index = Random.Range(0, candidates.Count);
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

    private static void ApplyRandomEquipments(CharacterData character, List<EquipmentSpawnData> pool, int count)
    {
        if (character == null || pool == null || count <= 0)
            return;

        List<EquipmentSpawnData> candidates = new List<EquipmentSpawnData>(pool);

        for (int i = 0; i < count; i++)
        {
            if (candidates.Count == 0)
                return;

            int index = Random.Range(0, candidates.Count);
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