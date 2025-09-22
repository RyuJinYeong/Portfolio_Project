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
                            .ToList()
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

        return pd;
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
            hairType = c.customizationData?.HairType ?? 0,
            eyebrowsType = c.customizationData?.EyebrowsType ?? 0,
            eyeType = c.customizationData?.EyeType ?? 0,
            mouthType = c.customizationData?.MouthType ?? 0,
            beardType = c.customizationData?.BeardType ?? 0,
            hairColor = c.customizationData?.HairColor ?? 0,
            skinTone = c.customizationData?.SkinTone ?? 0,

            // 레벨 / 현재 수치
            level = c.FinalStats?.Lv ?? 1,
            exp = c.FinalStats?.Exp ?? 0,
            currentHp = c.FinalStats?.CurrentHp ?? 0,
            currentStamina = c.FinalStats?.CurrentStamina ?? 0,
            currentMentality = c.FinalStats?.CurrentMentality ?? 0,

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
                HairType = dto.hairType,
                EyebrowsType = dto.eyebrowsType,
                EyeType = dto.eyeType,
                MouthType = dto.mouthType,
                BeardType = dto.beardType,
                HairColor = dto.hairColor,
                SkinTone = dto.skinTone
            },

            // 원천 스탯 복원
            BaseStats = dto.baseStats?.Copy() ?? new CharacterStats(),
            ModifiedStats = dto.modifiedStats?.Copy() ?? new CharacterStats(),
            FinalStats = new CharacterStats(),

            Traits = new List<TraitBase>(),
            EquipmentTraits = new List<TraitBase>(),
            Skills = new List<SkillBase>(),
            EquipmentSkills = new List<SkillBase>(),
            StatusEffects = new List<StatusEffect>(),
            AvailableAttributes = new List<SkillAttribute>()
        };

        // --- 런타임 배율/상태/방어도(기본값 가드) ---
        c.PhysicalDamageMultiplier = dto.physicalDamageMultiplier > 0f ? dto.physicalDamageMultiplier : 1f;
        c.MagicalDamageMultiplier = dto.magicalDamageMultiplier > 0f ? dto.magicalDamageMultiplier : 1f;
        c.AttackSpeedMultiplier = dto.attackSpeedMultiplier > 0f ? dto.attackSpeedMultiplier : 1f;
        c.CastSpeedMultiplier = dto.castSpeedMultiplier > 0f ? dto.castSpeedMultiplier : 1f;

        c.IsAlive = dto.isAlive;
        c.IsMine = dto.isMine;
        c.PhysicalArmor = dto.physicalArmor;
        c.MagicalArmor = dto.magicalArmor;

        c.FinalStats.Lv = dto.level;
        c.FinalStats.Exp = dto.exp;
        c.FinalStats.CurrentHp = dto.currentHp;
        c.FinalStats.CurrentStamina = dto.currentStamina;
        c.FinalStats.CurrentMentality = dto.currentMentality;

        // 인벤토리/장비 JSON은 스폰된 프리팹의 홀더에 Import할 것이므로 일단 캐시
        c.InventoryJsonSnapshot = dto.inventoryJson;
        c.EquipmentJsonSnapshot = dto.equipmentJson;

        // 장비 복원(UID → DB → Copy())
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

        // 인벤토리 복원
        InventorySerializer.ImportJson(c.CharacterInventory, dto.inventoryJson);
        InventorySerializer.ImportJson(c.CharacterEquipment, dto.equipmentJson);

        // 스킬 복원(UID → DB → Copy() + 카운터 반영)
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

        // --- 특성 복원 ---
        if (dto.traits != null)
        {
            foreach (var t in dto.traits)
            {
                var trait = TraitRegistry.Create(t.id);
                if (trait == null) continue;
                trait.Level = (t.level <= 0 ? 1 : t.level);
                c.Traits.Add(trait);
            }
        }

        // --- 장비로 얻는 스킬/특성 복원 ---
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
                var trait = TraitRegistry.Create(t.id);
                if (trait == null) continue;
                trait.Level = (t.level <= 0 ? 1 : t.level);
                c.EquipmentTraits.Add(trait);
            }
        }

        // --- 무기 세부 속성 ---
        if (dto.availableAttributes != null)
        {
            foreach (var a in dto.availableAttributes)
                c.AvailableAttributes.Add((SkillAttribute)a);
        }

        // 기본 대응 스킬
        c.DefaultCounterSkill = ItemDbCopyAs<SkillBase>(dto.defaultCounterSkillUid);

        // 최종 스탯 계산
        c.UpdateFinalStats();

        return c;
    }

    private static T ItemDbCopyAs<T>(int uid) where T : class
    {
        if (uid == 0) return null;
        if (!ItemManager.itemDic.TryGetValue(uid, out var item)) return null;
        return item.Copy() as T;
    }
}
