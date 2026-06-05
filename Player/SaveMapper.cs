using Newtonsoft.Json;
using SoftKitty.InventoryEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SaveMapper
{
    // ---------- Player ----------
    public static PlayerSaveDTO ToDto(PlayerData src)
    {
        var dto = new PlayerSaveDTO
        {
            playerName = src.playerName,
            level = src.level,
            gold = src.gold,
            currentStage = src.currentStage,
            characterIds = new List<string>(src.characterIds),
            activeCharacterIds = new List<string>(src.activeCharacterIds),
            positions = src.characterPositionMapping
                            .Select(kv => new PositionEntry { characterId = kv.Key, isFront = kv.Value })
                            .ToList(),
            questState = ToDto(QuestManager.Instance)
        };
        return dto;
    }

    public static PlayerData FromDto(PlayerSaveDTO dto)
    {
        var pd = new PlayerData
        {
            playerName = dto.playerName,
            level = dto.level,
            gold = dto.gold,
            currentStage = dto.currentStage,
            characterIds = new List<string>(dto.characterIds),
            activeCharacterIds = new List<string>(dto.activeCharacterIds),
            positions = dto.positions ?? new List<PositionEntry>()
        };

        // 런타임 캐시 복원
        pd.characterPositionMapping.Clear();
        foreach (var p in pd.positions)
            pd.characterPositionMapping[p.characterId] = p.isFront;

        FromDto(dto.questState);

        return pd;
    }

    // ---------- Quest ----------
    public static QuestStateDTO ToDto(QuestManager qm)
    {
        if (qm == null) return null;

        return new QuestStateDTO
        {
            board = qm.board != null ? new List<QuestBoardEntry>(qm.board) : new List<QuestBoardEntry>(),
            active = qm.active == null ? null : new ActiveQuestRuntime
            {
                def = qm.active.def,
                status = qm.active.status,
                leaderCharacterId = qm.active.leaderCharacterId,
                partyCharacterIds = new List<string>(qm.active.partyCharacterIds ?? new List<string>()),
                stageKey = qm.active.stageKey,
                stageNodeIndex = qm.active.stageNodeIndex,
                retreated = qm.active.retreated
            },
            completed = qm.completed != null ? new List<CompletedQuestEntry>(qm.completed) : new List<CompletedQuestEntry>()
        };
    }

    public static void FromDto(QuestStateDTO dto)
    {
        var qm = QuestManager.Instance;
        if (qm == null)
        {
            Debug.LogWarning("[SaveMapper] QuestManager.Instance is null. Quest state not applied.");
            return;
        }

        if (dto == null)
        {
            // 저장 데이터에 퀘스트가 없으면 보드 생성
            qm.board = new List<QuestBoardEntry>();
            qm.active = null;
            qm.completed = new List<CompletedQuestEntry>();
            qm.GenerateBoardIfEmpty(8);
            return;
        }

        qm.board = dto.board != null ? new List<QuestBoardEntry>(dto.board) : new List<QuestBoardEntry>();
        qm.active = dto.active == null ? null : new ActiveQuestRuntime
        {
            def = dto.active.def,
            status = dto.active.status,
            leaderCharacterId = dto.active.leaderCharacterId,
            partyCharacterIds = new List<string>(dto.active.partyCharacterIds ?? new List<string>()),
            stageKey = dto.active.stageKey,
            stageNodeIndex = dto.active.stageNodeIndex,
            retreated = dto.active.retreated
        };
        qm.completed = dto.completed != null ? new List<CompletedQuestEntry>(dto.completed) : new List<CompletedQuestEntry>();

        // 보드가 비었으면 기본 생성
        qm.GenerateBoardIfEmpty(8);
    }

    // ---------- Character ----------
    public static CharacterSaveDTO ToDto(CharacterData c)
    {
        var dto = new CharacterSaveDTO
        {
            id = c.ID,
            name = c.Name,
            type = (int)(c.Type),

            // 출신
            origin = (int)(c.origin),
            originName = c.originName,

            // 성향/심리
            personality = (int)(c.personality),
            belonging = c.Belonging,
            morale = c.Morale,

            // 커스터마이징
            isMale = c.customizationData?.IsMale ?? true,
            genderId = c.customizationData?.GenderId ?? 1,
            faceTypeId = c.customizationData?.FaceTypeId ?? 1,
            hairStyleId = c.customizationData?.HairStyleId ?? 1,
            hairColorId = c.customizationData?.HairColorId ?? 1,
            skinColorId = c.customizationData?.SkinColorId ?? 1,
            eyeColorId = c.customizationData?.EyeColorId ?? 1,
            facialHairId = c.customizationData?.FacialHairId ?? 0,
            bustSizeId = c.customizationData?.BustSizeId ?? 2,

            // ★ 런타임 값은 CharacterData에서
            level = c.Level,
            exp = c.Exp,
            currentHp = c.CurrentHp,
            currentStamina = c.CurrentStamina,
            currentMentality = c.CurrentMentality,

            // 원천 값만 저장(계산 결과는 로드 시 재계산)
            baseStats = c.BaseStats?.Copy(),
            modifiedStats = c.ModifiedStats?.Copy(),

            helmetUid = (c.Helmet as Item)?.uid ?? 0,
            armorUid = (c.Armor as Item)?.uid ?? 0,
            glovesUid = (c.Gloves as Item)?.uid ?? 0,
            shoesUid = (c.Shoes as Item)?.uid ?? 0,
            capeUid = (c.Cape as Item)?.uid ?? 0,
            ring1Uid = (c.Ring1 as Item)?.uid ?? 0,
            ring2Uid = (c.Ring2 as Item)?.uid ?? 0,
            necklaceUid = (c.Necklace as Item)?.uid ?? 0,
            weaponUid = (c.Weapon as Item)?.uid ?? 0,
            subWeaponUid = (c.SubWeapon as Item)?.uid ?? 0,

            // 배율
            physicalDamageMultiplier = c.PhysicalDamageMultiplier,
            magicalDamageMultiplier = c.MagicalDamageMultiplier,
            attackSpeedMultiplier = c.AttackSpeedMultiplier,
            castSpeedMultiplier = c.CastSpeedMultiplier,

            // 상태/진영
            isAlive = c.IsAlive,
            isMine = c.IsMine,

            // 방어도
            physicalArmor = c.PhysicalArmor,
            magicalArmor = c.MagicalArmor,

            defaultCounterSkillUid = c.DefaultCounterSkill?.uid ?? 0,
        };

        dto.inventoryJson = InventorySerializer.ExportJson(c.CharacterInventory) ?? "";
        dto.equipmentJson = InventorySerializer.ExportJson(c.CharacterEquipment) ?? "";

        // 스킬
        if (c.Skills != null)
        {
            foreach (var s in c.Skills)
            {
                dto.skills.Add(new SkillSaveDTO
                {
                    uid = s.uid,
                    useCount = s.SkillUseCount,
                    killCount = s.SkillKillCount,
                    damageCount = s.SkillDamageCount,
                    quickSlot = s.QuickSlot
                });
            }
        }
        
        // 특성
        if (c.Traits != null)
        {
            foreach (var t in c.Traits)
                dto.traits.Add(new TraitSaveDTO
                {
                    id = t.Id,
                    level = (t.Level <= 0 ? 1 : t.Level)
                });
        }
        
        // 장비로 얻은 스킬/특성
        if (c.EquipmentSkills != null)
            foreach (var s in c.EquipmentSkills)
                dto.equipmentSkills.Add(new SkillSaveDTO
                {
                    uid = s.uid,
                    useCount = s.SkillUseCount,
                    killCount = s.SkillKillCount,
                    damageCount = s.SkillDamageCount,
                    quickSlot = s.QuickSlot
                });
        
        if (c.EquipmentTraits != null)
        {
            foreach (var t in c.EquipmentTraits)
                dto.equipmentTraits.Add(new TraitSaveDTO
                {
                    id = t.Id,
                    level = (t.Level <= 0 ? 1 : t.Level)
                });
        }
        


        // 무기 세부 속성
        if (c.AvailableAttributes != null)
        {
            foreach (var a in c.AvailableAttributes)
                dto.availableAttributes.Add((int)a);
        }

        return dto;
    }
    public static CharacterData FromDto(CharacterSaveDTO dto)
    {
        var c = new CharacterData
        {
            ID = dto.id,
            Name = dto.name,
            Type = (CharacterType)dto.type,

            origin = (Origin)dto.origin,
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

            BaseStats = dto.baseStats?.Copy() ?? new CharacterStats(),
            ModifiedStats = dto.modifiedStats?.Copy() ?? new CharacterStats(),
            FinalStats = new CharacterStats(),

            Traits = new List<TraitBase>(),
            EquipmentTraits = new List<TraitBase>(),
            Skills = new List<SkillBase>(),
            EquipmentSkills = new List<SkillBase>(),
            StatusEffects = new List<StatusEffect>(),
            AvailableAttributes = new List<SkillAttribute>(),

            // ★ 런타임 값은 CharacterData로
            Level = dto.level,
            Exp = dto.exp,
            CurrentHp = dto.currentHp,
            CurrentStamina = dto.currentStamina,
            CurrentMentality = dto.currentMentality
        };

        // 배율/상태
        c.PhysicalDamageMultiplier = dto.physicalDamageMultiplier > 0f ? dto.physicalDamageMultiplier : 1f;
        c.MagicalDamageMultiplier = dto.magicalDamageMultiplier > 0f ? dto.magicalDamageMultiplier : 1f;
        c.AttackSpeedMultiplier = dto.attackSpeedMultiplier > 0f ? dto.attackSpeedMultiplier : 1f;
        c.CastSpeedMultiplier = dto.castSpeedMultiplier > 0f ? dto.castSpeedMultiplier : 1f;

        c.IsAlive = dto.isAlive;
        c.IsMine = dto.isMine;
        c.PhysicalArmor = dto.physicalArmor;
        c.MagicalArmor = dto.magicalArmor;

        // 스냅샷만 캐시 (★ 여기선 Import하지 않음. 프리팹 홀더 바인딩 후 Import)
        c.InventoryJsonSnapshot = dto.inventoryJson;
        c.EquipmentJsonSnapshot = dto.equipmentJson;

        // 장비 복원
        c.Helmet = ItemDbCopyAs<Equipment>(dto.helmetUid);
        c.Armor = ItemDbCopyAs<Equipment>(dto.armorUid);
        c.Gloves = ItemDbCopyAs<Equipment>(dto.glovesUid);
        c.Shoes = ItemDbCopyAs<Equipment>(dto.shoesUid);
        c.Cape = ItemDbCopyAs<Equipment>(dto.capeUid);
        c.Ring1 = ItemDbCopyAs<Equipment>(dto.ring1Uid);
        c.Ring2 = ItemDbCopyAs<Equipment>(dto.ring2Uid);
        c.Necklace = ItemDbCopyAs<Equipment>(dto.necklaceUid);
        c.Weapon = ItemDbCopyAs<Equipment>(dto.weaponUid);
        c.SubWeapon = ItemDbCopyAs<Equipment>(dto.subWeaponUid);

        // ★ 여기 두 줄은 제거하세요 (홀더 미바인딩 시 NRE/중복 Import)
        // InventorySerializer.ImportJson(c.CharacterInventory, dto.inventoryJson);
        // InventorySerializer.ImportJson(c.CharacterEquipment, dto.equipmentJson);

        // 스킬 복원
        if (dto.skills != null)
        {
            foreach (var s in dto.skills)
            {
                var skill = ItemDbCopyAs<SkillBase>(s.uid);
                if (skill == null) continue;
                skill.SkillUseCount = s.useCount;
                skill.SkillKillCount = s.killCount;
                skill.SkillDamageCount = s.damageCount;
                skill.QuickSlot = s.quickSlot;
                c.Skills.Add(skill);
            }
        }

        // 특성 복원
        if (dto.traits != null)
        {
            foreach (var t in dto.traits)
            {
                var trait = TraitDatabase.Create(t.id);
                if (trait == null) continue;
                trait.Level = (t.level <= 0 ? 1 : t.level);
                c.Traits.Add(trait);
            }
        }

        // 장비로 얻는 스킬/특성
        if (dto.equipmentSkills != null)
        {
            foreach (var s in dto.equipmentSkills)
            {
                var skill = ItemDbCopyAs<SkillBase>(s.uid);
                if (skill == null) continue;
                skill.SkillUseCount = s.useCount;
                skill.SkillKillCount = s.killCount;
                skill.SkillDamageCount = s.damageCount;
                skill.QuickSlot = s.quickSlot;
                c.EquipmentSkills.Add(skill);
            }
        }
        if (dto.equipmentTraits != null)
        {
            foreach (var t in dto.equipmentTraits)
            {
                var trait = TraitDatabase.Create(t.id);
                if (trait == null) continue;
                trait.Level = (t.level <= 0 ? 1 : t.level);
                c.EquipmentTraits.Add(trait);
            }
        }

        if (dto.availableAttributes != null)
            foreach (var a in dto.availableAttributes)
                c.AvailableAttributes.Add((SkillAttribute)a);

        c.DefaultCounterSkill = ItemDbCopyAs<SkillBase>(dto.defaultCounterSkillUid);

        // ★ 계산 먼저
        c.UpdateFinalStats();

        // ★ 계산 후 현재치 상한 보정
        c.ClampRuntimeResources();

        return c;
    }


    private static T ItemDbCopyAs<T>(int uid) where T : class
    {
        if (uid == 0) return null;
        if (!ItemManager.itemDic.TryGetValue(uid, out var item)) return null;
        return item.Copy() as T;
    }
}
