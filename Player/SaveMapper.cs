using System.Collections.Generic;
using UnityEngine;

public static class SaveMapper
{
    // ---------- Player ----------
    public static PlayerSaveDTO ToDto(PlayerData src)
    {
        if (src == null)
            return null;

        PlayerSaveDTO dto = new PlayerSaveDTO
        {
            playerName = src.playerName,
            level = src.level,
            gold = src.gold,
            currentStage = src.currentStage,

            accountStorage = CloneInventorySlots(src.accountStorage),
            expeditionStorage = CloneInventorySlots(src.expeditionStorage),
            lostExpeditionInventories = CloneLostExpeditionInventories(
                src.lostExpeditionInventories),

            generatedEquipments = src.generatedEquipments != null
                ? new List<GeneratedEquipmentData>(src.generatedEquipments)
                : new List<GeneratedEquipmentData>(),

            characterIds = src.characterIds != null
                ? new List<string>(src.characterIds)
                : new List<string>(),

            missingCharacterIds = src.missingCharacterIds != null
                ? new List<string>(src.missingCharacterIds)
                : new List<string>(),

            revivalRequiredCharacterIds = src.revivalRequiredCharacterIds != null
                ? new List<string>(src.revivalRequiredCharacterIds)
                : new List<string>(),

            activeCharacterIds = src.activeCharacterIds != null
                ? new List<string>(src.activeCharacterIds)
                : new List<string>(),

            recruitmentCandidates = ToCharacterDtos(src.recruitmentCandidates),
            recruitmentCandidatesInitialized = src.recruitmentCandidatesInitialized,

            positions = ClonePositions(src.positions),

            questState = ToDto(QuestManager.Instance)
        };

        return dto;
    }

    public static PlayerData FromDto(PlayerSaveDTO dto)
    {
        if (dto == null)
            return null;

        PlayerData pd = new PlayerData
        {
            playerName = dto.playerName,
            level = dto.level,
            gold = dto.gold,
            currentStage = string.IsNullOrEmpty(dto.currentStage) ? "Town" : dto.currentStage,

            accountStorage = CloneInventorySlots(dto.accountStorage),
            expeditionStorage = CloneInventorySlots(dto.expeditionStorage),
            lostExpeditionInventories = CloneLostExpeditionInventories(
                dto.lostExpeditionInventories),

            generatedEquipments = dto.generatedEquipments != null
                ? new List<GeneratedEquipmentData>(dto.generatedEquipments)
                : new List<GeneratedEquipmentData>(),

            characterIds = dto.characterIds != null
                ? new List<string>(dto.characterIds)
                : new List<string>(),

            missingCharacterIds = dto.missingCharacterIds != null
                ? new List<string>(dto.missingCharacterIds)
                : new List<string>(),

            revivalRequiredCharacterIds = dto.revivalRequiredCharacterIds != null
                ? new List<string>(dto.revivalRequiredCharacterIds)
                : new List<string>(),

            activeCharacterIds = dto.activeCharacterIds != null
                ? new List<string>(dto.activeCharacterIds)
                : new List<string>(),

            recruitmentCandidates = FromCharacterDtos(dto.recruitmentCandidates),
            recruitmentCandidatesInitialized = dto.recruitmentCandidatesInitialized,

            positions = ClonePositions(dto.positions)
        };

        FromDto(dto.questState);

        return pd;
    }

    private static List<CharacterSaveDTO> ToCharacterDtos(List<CharacterData> source)
    {
        List<CharacterSaveDTO> result = new List<CharacterSaveDTO>();

        if (source == null)
            return result;

        foreach (CharacterData character in source)
        {
            CharacterSaveDTO dto = ToDto(character);

            if (dto != null)
                result.Add(dto);
        }

        return result;
    }

    private static List<CharacterData> FromCharacterDtos(List<CharacterSaveDTO> source)
    {
        List<CharacterData> result = new List<CharacterData>();

        if (source == null)
            return result;

        foreach (CharacterSaveDTO dto in source)
        {
            CharacterData character = FromDto(dto);

            if (character != null)
                result.Add(character);
        }

        return result;
    }

    private static List<PositionEntry> ClonePositions(List<PositionEntry> source)
    {
        List<PositionEntry> result = new List<PositionEntry>();

        if (source == null)
            return result;

        foreach (PositionEntry position in source)
        {
            if (position == null || string.IsNullOrEmpty(position.characterId))
                continue;

            result.Add(new PositionEntry
            {
                characterId = position.characterId,
                isFront = position.isFront
            });
        }

        return result;
    }

    private static List<InventorySlotData> CloneInventorySlots(List<InventorySlotData> source)
    {
        List<InventorySlotData> result = new List<InventorySlotData>();

        if (source == null)
            return result;

        foreach (InventorySlotData slot in source)
        {
            if (slot == null)
                continue;

            if (slot.itemUid <= 0 || slot.count <= 0)
                continue;

            result.Add(new InventorySlotData
            {
                itemUid = slot.itemUid,
                count = slot.count,
                equipmentInstanceId = slot.equipmentInstanceId,
                essenceQuestId = slot.essenceQuestId,
                essenceMonsterRoleId = slot.essenceMonsterRoleId,
                essenceMonsterName = slot.essenceMonsterName,
                essenceTraits = CloneEssenceTraits(slot.essenceTraits)
            });
        }

        return result;
    }

    private static List<LostExpeditionInventoryData> CloneLostExpeditionInventories(
        List<LostExpeditionInventoryData> source)
    {
        List<LostExpeditionInventoryData> result = new();

        if (source == null)
            return result;

        foreach (LostExpeditionInventoryData inventory in source)
        {
            if (inventory == null || string.IsNullOrEmpty(inventory.sourceQuestId))
                continue;

            result.Add(new LostExpeditionInventoryData
            {
                sourceQuestId = inventory.sourceQuestId,
                characterIds = inventory.characterIds != null
                    ? new List<string>(inventory.characterIds)
                    : new List<string>(),
                items = CloneInventorySlots(inventory.items)
            });
        }

        return result;
    }

    private static List<MonsterEssenceTraitData> CloneEssenceTraits(
        List<MonsterEssenceTraitData> source)
    {
        List<MonsterEssenceTraitData> result = new List<MonsterEssenceTraitData>();

        if (source == null)
            return result;

        foreach (MonsterEssenceTraitData trait in source)
        {
            if (trait == null || trait.traitId <= 0)
                continue;

            result.Add(new MonsterEssenceTraitData
            {
                traitId = trait.traitId,
                point = trait.point
            });
        }

        return result;
    }

    // ---------- Quest ----------
    public static QuestStateDTO ToDto(QuestManager qm)
    {
        if (qm == null)
            return null;

        return new QuestStateDTO
        {
            board = qm.board != null
                ? new List<QuestBoardEntry>(qm.board)
                : new List<QuestBoardEntry>(),

            preferredIssuers = qm.preferredIssuers != null
                ? new List<QuestIssuer>(qm.preferredIssuers)
                : new List<QuestIssuer>(),

            preferredQuestTiers = qm.preferredQuestTiers != null
                ? new List<int>(qm.preferredQuestTiers)
                : new List<int>(),

            active = qm.active == null
                ? null
                : new ActiveQuestRuntime
                {
                    def = qm.active.def,
                    status = qm.active.status,
                    leaderCharacterId = qm.active.leaderCharacterId,
                    partyCharacterIds = new List<string>(qm.active.partyCharacterIds ?? new List<string>()),
                    stageKey = qm.active.stageKey,
                    stageNodeIndex = qm.active.stageNodeIndex,
                    currentRouteNodeId = qm.active.currentRouteNodeId,
                    routeNodes = CloneRouteNodes(qm.active.routeNodes),
                    encounterEffects = CloneEncounterEffects(qm.active.encounterEffects),
                    retreated = qm.active.retreated
                },

            completed = qm.completed != null
                ? new List<CompletedQuestEntry>(qm.completed)
                : new List<CompletedQuestEntry>()
        };
    }

    public static void FromDto(QuestStateDTO dto)
    {
        QuestManager qm = QuestManager.Instance;

        if (qm == null)
        {
            Debug.LogWarning("[SaveMapper] QuestManager.Instance is null. Quest state not applied.");
            return;
        }

        if (dto == null)
        {
            qm.board = new List<QuestBoardEntry>();
            qm.SetBoardPreferences(null, null);
            qm.active = null;
            qm.completed = new List<CompletedQuestEntry>();
            qm.GenerateBoardIfEmpty();
            return;
        }

        qm.board = dto.board != null
            ? new List<QuestBoardEntry>(dto.board)
            : new List<QuestBoardEntry>();

        qm.SetBoardPreferences(
            dto.preferredIssuers,
            dto.preferredQuestTiers);

        qm.active = dto.active == null
            ? null
            : new ActiveQuestRuntime
            {
                def = dto.active.def,
                status = dto.active.status,
                leaderCharacterId = dto.active.leaderCharacterId,
                partyCharacterIds = new List<string>(dto.active.partyCharacterIds ?? new List<string>()),
                stageKey = dto.active.stageKey,
                stageNodeIndex = dto.active.stageNodeIndex,
                currentRouteNodeId = dto.active.currentRouteNodeId,
                routeNodes = CloneRouteNodes(dto.active.routeNodes),
                encounterEffects = CloneEncounterEffects(dto.active.encounterEffects),
                retreated = dto.active.retreated
            };

        qm.completed = dto.completed != null
            ? new List<CompletedQuestEntry>(dto.completed)
            : new List<CompletedQuestEntry>();

        qm.GenerateBoardIfEmpty();
    }

    private static List<QuestRouteNode> CloneRouteNodes(List<QuestRouteNode> source)
    {
        List<QuestRouteNode> result = new List<QuestRouteNode>();

        if (source == null)
            return result;

        foreach (QuestRouteNode node in source)
        {
            if (node == null)
                continue;

            result.Add(new QuestRouteNode
            {
                id = node.id,
                depth = node.depth,
                column = node.column,
                type = node.type,
                mapPrefabIndex = node.mapPrefabIndex,
                nextNodeIds = node.nextNodeIds != null
                    ? new List<int>(node.nextNodeIds)
                    : new List<int>(),
                cleared = node.cleared,
                encounterId = node.encounterId,
                encounterResolved = node.encounterResolved,
                encounterMonsterRoleIds = node.encounterMonsterRoleIds != null
                    ? new List<int>(node.encounterMonsterRoleIds)
                    : new List<int>(),
                encounterInsightRolled = node.encounterInsightRolled,
                encounterInsightSucceeded = node.encounterInsightSucceeded
            });
        }

        return result;
    }


    private static List<QuestEncounterRuntimeEffect> CloneEncounterEffects(
        List<QuestEncounterRuntimeEffect> source)
    {
        List<QuestEncounterRuntimeEffect> result = new List<QuestEncounterRuntimeEffect>();

        if (source == null)
            return result;

        foreach (QuestEncounterRuntimeEffect effect in source)
        {
            if (effect == null)
                continue;

            result.Add(new QuestEncounterRuntimeEffect
            {
                characterId = effect.characterId,
                effectName = effect.effectName,
                remainingRooms = effect.remainingRooms,
                sourceNodeId = effect.sourceNodeId,
                statModifiers = effect.statModifiers != null
                    ? effect.statModifiers.Copy()
                    : new CharacterStats()
            });
        }

        return result;
    }

    // ---------- Character ----------
    public static CharacterSaveDTO ToDto(CharacterData c)
    {
        if (c == null)
            return null;

        CharacterSaveDTO dto = new CharacterSaveDTO
        {
            id = c.ID,
            name = c.Name,
            type = (int)c.Type,

            originId = c.originId,
            originName = c.originName,

            personality = (int)c.personality,
            belonging = c.Belonging,
            morale = c.Morale,

            isMale = c.customizationData?.IsMale ?? true,
            genderId = c.customizationData?.GenderId ?? 1,
            faceTypeId = c.customizationData?.FaceTypeId ?? 1,
            hairStyleId = c.customizationData?.HairStyleId ?? 1,
            hairColorId = c.customizationData?.HairColorId ?? 1,
            skinColorId = c.customizationData?.SkinColorId ?? 1,
            eyeColorId = c.customizationData?.EyeColorId ?? 1,
            facialHairId = c.customizationData?.FacialHairId ?? 0,
            bustSizeId = c.customizationData?.BustSizeId ?? 2,

            level = c.Level,
            exp = c.Exp,
            pendingLevelUps = c.PendingLevelUps,
            currentHp = c.CurrentHp,
            currentStamina = c.CurrentStamina,
            currentMentality = c.CurrentMentality,

            originBaseStats = c.OriginBaseStats?.Copy(),
            originSpecialStats = c.OriginSpecialStats?.Copy(),

            modifiedStats = c.ModifiedStats?.Copy(),
            modifiedSpecialStats = c.ModifiedSpecialStats?.Copy(),

            equipmentSlots = CloneEquipmentSlots(c.EquipmentSlots),

            physicalDamageMultiplier = c.PhysicalDamageMultiplier,
            magicalDamageMultiplier = c.MagicalDamageMultiplier,
            attackSpeedMultiplier = c.AttackSpeedMultiplier,
            castSpeedMultiplier = c.CastSpeedMultiplier,

            isAlive = c.IsAlive,
            isMine = c.IsMine,

            physicalArmor = c.PhysicalArmor,
            magicalArmor = c.MagicalArmor,

            defaultCounterSkillUid = c.DefaultCounterSkill
        };

        if (c.Skills != null)
        {
            foreach (SkillRuntimeData s in c.Skills)
            {
                if (s == null)
                    continue;

                dto.skills.Add(new SkillSaveDTO
                {
                    uid = s.skillUid,
                    useCount = s.useCount,
                    killCount = s.killCount,
                    damageCount = s.damageCount,
                    quickSlot = s.quickSlot,
                    canUse = s.canUse,
                    useOffHand = s.useOffHand
                });
            }
        }

        if (c.Traits != null)
        {
            foreach (TraitRuntimeData t in c.Traits)
            {
                if (t == null)
                    continue;

                dto.traits.Add(new TraitSaveDTO
                {
                    id = t.traitId,
                    point = t.point
                });
            }
        }

        return dto;
    }

    public static CharacterData FromDto(CharacterSaveDTO dto)
    {
        if (dto == null)
            return null;

        CharacterData c = new CharacterData
        {
            ID = dto.id,
            Name = dto.name,
            Type = (CharacterType)dto.type,

            originId = dto.originId,
            originName = dto.originName,

            personality = (Personality)dto.personality,
            Belonging = dto.belonging,
            Morale = dto.morale,

            customizationData = new CustomizationData
            {
                IsMale = dto.isMale,
                GenderId = dto.genderId <= 0 ? (dto.isMale ? 1 : 2) : dto.genderId,
                FaceTypeId = dto.faceTypeId <= 0 ? 1 : dto.faceTypeId,
                HairStyleId = dto.hairStyleId <= 0 ? 1 : dto.hairStyleId,
                HairColorId = dto.hairColorId <= 0 ? 1 : dto.hairColorId,
                SkinColorId = dto.skinColorId <= 0 ? 1 : dto.skinColorId,
                EyeColorId = dto.eyeColorId <= 0 ? 1 : dto.eyeColorId,
                FacialHairId = dto.facialHairId,
                BustSizeId = dto.bustSizeId <= 0 ? 2 : dto.bustSizeId
            },

            OriginBaseStats = dto.originBaseStats?.Copy() ?? new CharacterStats(),
            OriginSpecialStats = dto.originSpecialStats?.Copy() ?? new CharacterSpecialStats(),

            BaseStats = dto.originBaseStats?.Copy() ?? new CharacterStats(),
            BaseSpecialStats = dto.originSpecialStats?.Copy() ?? new CharacterSpecialStats(),

            ModifiedStats = dto.modifiedStats?.Copy() ?? new CharacterStats(),
            ModifiedSpecialStats = dto.modifiedSpecialStats?.Copy() ?? new CharacterSpecialStats(),

            FinalStats = new CharacterStats(),
            FinalSpecialStats = new CharacterSpecialStats(),

            Traits = new List<TraitRuntimeData>(),
            EquipmentTraitRuntimes = new List<TraitRuntimeData>(),
            Skills = new List<SkillRuntimeData>(),
            StatusEffects = new List<StatusEffectRuntimeData>(),
            AvailableAttributes = new List<SkillAttribute>(),

            EquipmentSlots = CloneEquipmentSlots(dto.equipmentSlots),

            Level = dto.level,
            Exp = dto.exp,
            PendingLevelUps = dto.pendingLevelUps,
            CurrentHp = dto.currentHp,
            CurrentStamina = dto.currentStamina,
            CurrentMentality = dto.currentMentality
        };

        c.PhysicalDamageMultiplier = dto.physicalDamageMultiplier > 0f ? dto.physicalDamageMultiplier : 1f;
        c.MagicalDamageMultiplier = dto.magicalDamageMultiplier > 0f ? dto.magicalDamageMultiplier : 1f;
        c.AttackSpeedMultiplier = dto.attackSpeedMultiplier > 0f ? dto.attackSpeedMultiplier : 1f;
        c.CastSpeedMultiplier = dto.castSpeedMultiplier > 0f ? dto.castSpeedMultiplier : 1f;

        c.IsAlive = dto.isAlive;
        c.IsMine = dto.isMine;
        c.PhysicalArmor = dto.physicalArmor;
        c.MagicalArmor = dto.magicalArmor;

        if (dto.skills != null)
        {
            foreach (SkillSaveDTO s in dto.skills)
            {
                SkillDefinitionSO def = GameDataRegistry.Instance.GetSkill(s.uid);

                if (def == null)
                    continue;

                c.Skills.Add(new SkillRuntimeData
                {
                    skillUid = s.uid,
                    useCount = s.useCount,
                    killCount = s.killCount,
                    damageCount = s.damageCount,
                    quickSlot = s.quickSlot,
                    canUse = s.canUse,
                    useOffHand = s.useOffHand
                });
            }
        }

        if (dto.traits != null)
        {
            foreach (TraitSaveDTO t in dto.traits)
            {
                TraitDefinitionSO def = GameDataRegistry.Instance.GetTrait(t.id);

                if (def == null)
                    continue;

                c.Traits.Add(new TraitRuntimeData
                {
                    traitId = t.id,
                    point = Mathf.Clamp(t.point <= 0 ? 1 : t.point, 1, TraitGradeUtility.MaxPoint)
                });
            }
        }

        c.DefaultCounterSkill = dto.defaultCounterSkillUid;

        EquipmentManager.RebuildEquipmentStats(c);
        EquipmentManager.UpdateAvailableAttributes(c);
        EquipmentManager.UpdateSkillAvailability(c);

        c.RemoveAllTraits(null);
        c.ApplyAllTraits(null);
        c.UpdateFinalStats();
        c.ClampRuntimeResources();

        return c;
    }

    private static EquipmentSlotData CloneEquipmentSlots(EquipmentSlotData source)
    {
        if (source == null)
            return new EquipmentSlotData();

        return new EquipmentSlotData
        {
            helmetUid = source.helmetUid,
            helmetInstanceId = source.helmetInstanceId,

            armorUid = source.armorUid,
            armorInstanceId = source.armorInstanceId,

            glovesUid = source.glovesUid,
            glovesInstanceId = source.glovesInstanceId,

            shoesUid = source.shoesUid,
            shoesInstanceId = source.shoesInstanceId,

            ring1Uid = source.ring1Uid,
            ring1InstanceId = source.ring1InstanceId,

            ring2Uid = source.ring2Uid,
            ring2InstanceId = source.ring2InstanceId,

            necklaceUid = source.necklaceUid,
            necklaceInstanceId = source.necklaceInstanceId,

            weaponUid = source.weaponUid,
            weaponInstanceId = source.weaponInstanceId,

            subWeaponUid = source.subWeaponUid,
            subWeaponInstanceId = source.subWeaponInstanceId
        };
    }
}
