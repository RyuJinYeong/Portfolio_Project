using System;
using System.Collections.Generic;

public static class MonsterGenerator
{
    public static CharacterData Generate(MonsterRoleSO role, int questLevel, int randomSeed = 0)
    {
        if (role == null || role.baseMonster == null)
            return null;

        System.Random random = randomSeed == 0
            ? new System.Random(Guid.NewGuid().GetHashCode())
            : new System.Random(randomSeed);

        CharacterData character = new CharacterData();

        character.ID = Guid.NewGuid().ToString();
        character.Name = BuildMonsterName(role);
        character.originId = role.baseMonster.id;
        character.originName = role.baseMonster.monsterName;
        character.monsterRoleId = role.id;

        character.Type = role.characterType;
        character.personality = role.personality;

        character.OriginBaseStats = role.baseMonster.baseStats != null
            ? role.baseMonster.baseStats.Copy()
            : new CharacterStats();

        character.OriginSpecialStats = role.baseMonster.baseSpecialStats != null
            ? role.baseMonster.baseSpecialStats.Copy()
            : new CharacterSpecialStats();

        if (role.roleStatBonus != null)
            character.OriginBaseStats += role.roleStatBonus;

        if (role.roleSpecialStatBonus != null)
            character.OriginSpecialStats += role.roleSpecialStatBonus;

        character.BaseStats = character.OriginBaseStats.Copy();
        character.BaseSpecialStats = character.OriginSpecialStats.Copy();

        character.ModifiedStats = new CharacterStats();
        character.ModifiedSpecialStats = new CharacterSpecialStats();

        character.FinalStats = new CharacterStats();
        character.FinalSpecialStats = new CharacterSpecialStats();

        character.Traits = new List<TraitRuntimeData>();
        character.EquipmentTraitRuntimes = new List<TraitRuntimeData>();
        character.Skills = new List<SkillRuntimeData>();
        character.StatusEffects = new List<StatusEffectRuntimeData>();
        character.AvailableAttributes = new List<SkillAttribute>();
        character.EquipmentSlots = new EquipmentSlotData();

        int questTierMinimumLevel = (Math.Max(1, questLevel) - 1) / 10 * 10 + 1;
        int questTierMaximumLevel = questTierMinimumLevel + 9;

        character.Level = Math.Min(
            questTierMaximumLevel,
            Math.Max(questTierMinimumLevel, questLevel + role.levelBonus));
        character.IsAlive = true;
        character.IsMine = false;

        ApplyTraits(character, role.baseMonster.baseTraitIds);
        ApplyTraits(character, role.traitIds);

        int monsterTier = (character.Level - 1) / 10 + 1;
        TraitGenerationData traitGeneration = TraitGenerationUtility.GetForTier(
            role.traitsByTier,
            monsterTier);

        int minimumTraitCount =
            role.characterType == CharacterType.Elite || role.characterType == CharacterType.Boss
                ? 3
                : 0;

        TraitGenerationUtility.Apply(
            character,
            traitGeneration,
            minimumTraitCount,
            random);

        character.RemoveAllTraits(null);
        character.ApplyAllTraits(null);
        character.UpdateFinalStats();

        List<StatGrowthWeightData> growthWeights =
            role.levelUpStatWeights != null && role.levelUpStatWeights.Count > 0
                ? role.levelUpStatWeights
                : role.baseMonster.levelUpStatWeights;

        LevelGrowthUtility.ApplyAutomaticGrowth(
            character,
            character.Level,
            growthWeights,
            random);

        ApplySkills(character, role.skillUids);
        AssignDefaultCounterSkill(character, role.defaultCounterSkillUids, random);

        List<EquipmentSpawnData> equipments = new List<EquipmentSpawnData>();

        if (role.equipments != null)
            equipments.AddRange(role.equipments);

        equipments.AddRange(PickRandomEquipments(role, equipments, random));
        RuntimeEquipmentApplier.ApplyRuntimeEquipments(character, equipments);

        character.RemoveAllTraits(null);
        character.ApplyAllTraits(null);
        character.UpdateFinalStats();

        character.CurrentHp = character.FinalStats.MaxHp;
        character.CurrentStamina = character.FinalStats.MaxStamina;
        character.CurrentMentality = character.FinalStats.MaxMentality;

        return character;
    }

    private static string BuildMonsterName(MonsterRoleSO role)
    {
        if (string.IsNullOrEmpty(role.roleName))
            return role.baseMonster.monsterName;

        return $"{role.baseMonster.monsterName} {role.roleName}";
    }

    private static List<EquipmentSpawnData> PickRandomEquipments(
        MonsterRoleSO role,
        List<EquipmentSpawnData> fixedEquipments,
        System.Random random)
    {
        List<EquipmentSpawnData> result = new List<EquipmentSpawnData>();

        if (role.randomEquipmentPool == null || role.randomEquipmentPool.Count == 0)
            return result;

        int minimum = Math.Max(0, role.minRandomEquipmentCount);
        int maximum = Math.Max(minimum, role.maxRandomEquipmentCount);
        int count = random.Next(minimum, maximum + 1);

        List<EquipmentSpawnData> candidates = new List<EquipmentSpawnData>(role.randomEquipmentPool);
        HashSet<EquipmentType> usedSlots = GetUsedEquipmentSlots(fixedEquipments);

        while (result.Count < count && candidates.Count > 0)
        {
            int index = random.Next(0, candidates.Count);
            EquipmentSpawnData spawnData = candidates[index];
            candidates.RemoveAt(index);

            EquipmentDefinitionSO equipment = spawnData != null && GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetEquipment(spawnData.equipmentUid)
                : null;

            if (equipment == null || usedSlots.Contains(equipment.equipType))
                continue;

            usedSlots.Add(equipment.equipType);
            result.Add(spawnData);
        }

        return result;
    }

    private static HashSet<EquipmentType> GetUsedEquipmentSlots(List<EquipmentSpawnData> equipments)
    {
        HashSet<EquipmentType> result = new HashSet<EquipmentType>();

        if (equipments == null || GameDataRegistry.Instance == null)
            return result;

        foreach (EquipmentSpawnData spawnData in equipments)
        {
            if (spawnData == null || !spawnData.equipOnSpawn)
                continue;

            EquipmentDefinitionSO equipment = GameDataRegistry.Instance.GetEquipment(spawnData.equipmentUid);

            if (equipment != null)
                result.Add(equipment.equipType);
        }

        return result;
    }

    private static void AssignDefaultCounterSkill(
        CharacterData character,
        List<int> counterSkillUids,
        System.Random random)
    {
        if (counterSkillUids == null || counterSkillUids.Count == 0)
            return;

        List<int> candidates = counterSkillUids.FindAll(uid =>
        {
            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(uid);
            return skill != null && skill.isCounterSkill;
        });

        if (candidates.Count == 0)
            return;

        int selectedUid = candidates[random.Next(0, candidates.Count)];
        character.DefaultCounterSkill = selectedUid;

        if (!HasSkill(character, selectedUid))
            ApplySkill(character, selectedUid);
    }

    private static void ApplyTraits(CharacterData character, List<int> traitIds)
    {
        if (character == null || traitIds == null)
            return;

        foreach (int traitId in traitIds)
            ApplyTrait(character, traitId);
    }

    private static void ApplyTrait(CharacterData character, int traitId)
    {
        TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(traitId);

        if (trait == null || character.HasCharacterTrait(traitId))
            return;

        TraitGradeUtility.AddTrait(character.Traits, trait, trait.defaultAcquireGrade);
    }

    private static void ApplySkills(CharacterData character, List<int> skillUids)
    {
        if (character == null || skillUids == null)
            return;

        foreach (int skillUid in skillUids)
            ApplySkill(character, skillUid);
    }

    private static void ApplySkill(CharacterData character, int skillUid)
    {
        SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(skillUid);

        if (skill == null || HasSkill(character, skillUid))
            return;

        character.Skills.Add(SkillRuntimeFactory.Create(skill));
    }

    private static bool HasSkill(CharacterData character, int skillUid)
    {
        return character.Skills.Exists(skill => skill != null && skill.skillUid == skillUid);
    }
}
