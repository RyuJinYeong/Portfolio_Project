using System.Collections.Generic;
using UnityEngine;

public static class MercenaryGenerator
{
    private const int BaseRecruitmentRefreshCost = 500;
    private const int UnknownOriginId = 1006;

    public static float CalculateValue(CharacterData character)
    {
        if (character == null)
            return 0f;

        float levelValue = 2.75f * Mathf.Max(0, character.Level - 1);
        int traitValue = CalculateTraitValue(character);
        int skillValue = CountAdditionalSkills(character) * 4;

        return Mathf.Max(0f, levelValue + traitValue + skillValue);
    }

    public static int CalculateContractFee(CharacterData character)
    {
        int baseContractFee = Mathf.RoundToInt(CalculateValue(character) * 60f);
        return baseContractFee + CalculateEquipmentValue(character);
    }

    public static int CalculateBaseSortiePay(CharacterData character)
    {
        return Mathf.RoundToInt(CalculateValue(character) * 15f);
    }

    public static int CalculateSortiePay(CharacterData character)
    {
        if (character == null)
            return 0;

        float belongingRate = Mathf.Clamp01(character.Belonging / 100f);
        return Mathf.RoundToInt(CalculateBaseSortiePay(character) * (1f - belongingRate));
    }

    public static int GetRecruitmentRefreshCost(PlayerData playerData)
    {
        int refreshCount = playerData != null
            ? Mathf.Clamp(playerData.recruitmentRefreshCount, 0, 20)
            : 0;
        long cost = (long)BaseRecruitmentRefreshCost << refreshCount;
        return cost > int.MaxValue ? int.MaxValue : (int)cost;
    }

    public static void ResetRecruitmentRefreshCost(PlayerData playerData)
    {
        if (playerData != null)
            playerData.recruitmentRefreshCount = 0;
    }

    public static void RefreshRecruitmentCandidates(
        PlayerData playerData,
        bool preserveReserved = true)
    {
        if (playerData == null || GameDataRegistry.Instance == null)
            return;

        List<MercenaryDefinitionSO> definitions =
            GameDataRegistry.Instance.GetMercenaryDefinitions();

        if (definitions == null || definitions.Count == 0)
            return;

        List<MercenaryDefinitionSO> candidates = new List<MercenaryDefinitionSO>();

        foreach (MercenaryDefinitionSO definition in definitions)
        {
            if (definition != null)
                candidates.Add(definition);
        }

        if (candidates.Count == 0)
            return;

        CharacterData reservedCandidate = null;

        if (preserveReserved &&
            playerData.recruitmentCandidates != null &&
            !string.IsNullOrEmpty(playerData.reservedRecruitmentCandidateId))
        {
            reservedCandidate = playerData.recruitmentCandidates.Find(candidate =>
                candidate != null &&
                candidate.ID == playerData.reservedRecruitmentCandidateId);
        }

        if (reservedCandidate == null)
            playerData.reservedRecruitmentCandidateId = null;

        if (playerData.recruitmentCandidates == null)
            playerData.recruitmentCandidates = new List<CharacterData>();
        else
        {
            ReleaseCandidateEquipment(
                playerData.recruitmentCandidates,
                reservedCandidate != null ? reservedCandidate.ID : null);
            playerData.recruitmentCandidates.Clear();
        }

        if (reservedCandidate != null)
        {
            playerData.recruitmentCandidates.Add(reservedCandidate);
            candidates.RemoveAll(definition =>
                definition != null &&
                definition.baseOrigin != null &&
                definition.baseOrigin.id == reservedCandidate.originId);
        }

        playerData.recruitmentCandidatesInitialized = true;

        System.Random random = new System.Random(System.Guid.NewGuid().GetHashCode());
        int remainingOfferCount = Mathf.Max(
            0,
            GameDataRegistry.Instance.recruitmentOfferCount -
            playerData.recruitmentCandidates.Count);
        int offerCount = Mathf.Min(remainingOfferCount, candidates.Count);

        for (int i = 0; i < offerCount; i++)
        {
            int index = random.Next(0, candidates.Count);
            MercenaryDefinitionSO definition = candidates[index];
            candidates.RemoveAt(index);

            CharacterData character = Generate(definition, playerData.level);

            if (character != null)
                playerData.recruitmentCandidates.Add(character);
        }
    }

    public static int CountAdditionalSkills(CharacterData character)
    {
        if (character == null || character.Skills == null)
            return 0;

        OriginDefinitionSO origin = GameDataRegistry.Instance.GetOrigin(character.originId);
        HashSet<int> initialSkillUids = origin != null && origin.initialSkillUids != null
            ? new HashSet<int>(origin.initialSkillUids)
            : new HashSet<int>();

        int count = 0;

        foreach (SkillRuntimeData runtime in character.Skills)
        {
            if (runtime != null && !initialSkillUids.Contains(runtime.skillUid))
                count++;
        }

        return count;
    }

    private static int CalculateTraitValue(CharacterData character)
    {
        if (character.Traits == null)
            return 0;

        int value = 0;

        foreach (TraitRuntimeData runtime in character.Traits)
        {
            if (runtime == null)
                continue;

            TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(runtime.traitId);

            if (trait == null)
                continue;

            value += TraitGradeUtility.GetSignedValue(runtime.point, trait.polarity);
        }

        return value;
    }

    private static int CalculateEquipmentValue(CharacterData character)
    {
        EquipmentSlotData slots = character != null ? character.EquipmentSlots : null;

        if (slots == null)
            return 0;

        int value = 0;
        value += GetEquipmentValue(slots.helmetUid, slots.helmetInstanceId);
        value += GetEquipmentValue(slots.armorUid, slots.armorInstanceId);
        value += GetEquipmentValue(slots.glovesUid, slots.glovesInstanceId);
        value += GetEquipmentValue(slots.shoesUid, slots.shoesInstanceId);
        value += GetEquipmentValue(slots.ring1Uid, slots.ring1InstanceId);
        value += GetEquipmentValue(slots.ring2Uid, slots.ring2InstanceId);
        value += GetEquipmentValue(slots.necklaceUid, slots.necklaceInstanceId);
        value += GetEquipmentValue(slots.weaponUid, slots.weaponInstanceId);
        value += GetEquipmentValue(slots.subWeaponUid, slots.subWeaponInstanceId);
        return value;
    }

    private static int GetEquipmentValue(int itemUid, string instanceId)
    {
        if (itemUid <= 0)
            return 0;

        return SharedInventoryUtility.GetSalePrice(
            new InventorySlotData
            {
                itemUid = itemUid,
                count = 1,
                equipmentInstanceId = instanceId
            });
    }

    private static void ReleaseCandidateEquipment(
        List<CharacterData> candidates,
        string preservedCandidateId)
    {
        foreach (CharacterData candidate in candidates)
        {
            if (candidate != null && candidate.ID == preservedCandidateId)
                continue;

            EquipmentSlotData slots = candidate != null ? candidate.EquipmentSlots : null;

            if (slots == null)
                continue;

            EquipmentInstanceRepository.RemoveRuntime(slots.helmetInstanceId);
            EquipmentInstanceRepository.RemoveRuntime(slots.armorInstanceId);
            EquipmentInstanceRepository.RemoveRuntime(slots.glovesInstanceId);
            EquipmentInstanceRepository.RemoveRuntime(slots.shoesInstanceId);
            EquipmentInstanceRepository.RemoveRuntime(slots.ring1InstanceId);
            EquipmentInstanceRepository.RemoveRuntime(slots.ring2InstanceId);
            EquipmentInstanceRepository.RemoveRuntime(slots.necklaceInstanceId);
            EquipmentInstanceRepository.RemoveRuntime(slots.weaponInstanceId);
            EquipmentInstanceRepository.RemoveRuntime(slots.subWeaponInstanceId);
        }
    }

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

        int minimumBelonging = Mathf.Min(
            template.minInitialBelonging,
            template.maxInitialBelonging);
        int maximumBelonging = Mathf.Max(
            template.minInitialBelonging,
            template.maxInitialBelonging);
        character.Belonging = random.Next(
            Mathf.Clamp(minimumBelonging, 0, 100),
            Mathf.Clamp(maximumBelonging, 0, 100) + 1);

        int minimumLevelOffset = Mathf.Min(template.minLevelOffset, template.maxLevelOffset);
        int maximumLevelOffset = Mathf.Max(template.minLevelOffset, template.maxLevelOffset);

        character.Level = Mathf.Max(
            1,
            accountLevel + random.Next(minimumLevelOffset, maximumLevelOffset + 1));

        ApplyFixedTraits(character, template.fixedTraitIds);
        ApplyFixedSkills(character, template.fixedSkillUids);

        ApplyGuaranteedUnknownOriginTrait(character, random);

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

    private static void ApplyGuaranteedUnknownOriginTrait(
        CharacterData character,
        System.Random random)
    {
        if (character == null || character.originId != UnknownOriginId ||
            GameDataRegistry.Instance == null)
        {
            return;
        }

        List<TraitDefinitionSO> candidates = GameDataRegistry.Instance.GetAllTraits()
            .FindAll(trait =>
                trait != null &&
                trait.polarity == TraitPolarity.Positive &&
                trait.defaultAcquireGrade == TraitGrade.C &&
                !character.HasCharacterTrait(trait.id));

        if (candidates.Count == 0)
            return;

        TraitDefinitionSO selected = candidates[random.Next(0, candidates.Count)];
        TraitGradeUtility.AddTrait(character.Traits, selected, TraitGrade.C);
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
