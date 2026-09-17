using System.Collections.Generic;
using UnityEngine;

public static class EquipmentManager
{
    public static void Equip(CharacterManager manager, InventorySlotData slot)
    {
        if (manager == null || manager.character == null || slot == null)
            return;

        Equip(manager, slot.itemUid, slot.equipmentInstanceId);
    }

    public static void Equip(CharacterManager manager, int itemUid, string instanceId = null)
    {
        if (manager == null || manager.character == null)
            return;

        EquipmentDefinitionSO equipment = GameDataRegistry.Instance.GetEquipment(itemUid);

        if (equipment == null)
            return;

        CharacterData character = manager.character;

        if (equipment.equipType == EquipmentType.SubWeapon &&
            !character.CanEquipSubWeapon())
        {
            Debug.Log("장착 불가능한 보조무기입니다.");
            return;
        }

        if (equipment is WeaponDefinitionSO weapon)
        {
            if (character.HasTraitFlag(TraitSpecialFlag.FearOfBlades) &&
                weapon.weaponType != WeaponType.Mace && weapon.weaponType != WeaponType.Hammer &&
                weapon.weaponType != WeaponType.Shield && weapon.weaponType != WeaponType.Staff) return;
            if (equipment.equipType == EquipmentType.Weapon && !character.CanEquipMainWeapon(weapon))
            {
                Debug.Log("장착 불가능한 주무기입니다.");
                return;
            }

        }

        SetEquipmentSlot(character.EquipmentSlots, equipment.equipType, itemUid, instanceId);

        RecalculateAfterEquipmentChanged(manager);
    }

    public static void Unequip(CharacterManager manager, EquipmentType equipType, int slotIndex = 1)
    {
        if (manager == null || manager.character == null)
            return;

        ClearSlot(manager.character.EquipmentSlots, equipType, slotIndex);

        RecalculateAfterEquipmentChanged(manager);
    }

    private static void RecalculateAfterEquipmentChanged(CharacterManager manager)
    {
        CharacterData character = manager.character;

        RebuildEquipmentStats(character);
        UpdateAvailableAttributes(character);
        UpdateSkillAvailability(character);

        character.RemoveAllTraits(manager);
        character.ApplyAllTraits(manager);
        character.UpdateFinalStats();

        CharacterCustomization customization = manager.GetComponent<CharacterCustomization>();

        if (customization != null)
            customization.UpdateEquipmentAppearance(character);
    }

    public static void SetEquipmentSlot(
    EquipmentSlotData slots,
    EquipmentType equipType,
    int itemUid,
    string instanceId)
    {
        if (slots == null)
            return;

        switch (equipType)
        {
            case EquipmentType.Helmet:
                slots.helmetUid = itemUid;
                slots.helmetInstanceId = instanceId;
                break;

            case EquipmentType.Armor:
                slots.armorUid = itemUid;
                slots.armorInstanceId = instanceId;
                break;

            case EquipmentType.Gloves:
                slots.glovesUid = itemUid;
                slots.glovesInstanceId = instanceId;
                break;

            case EquipmentType.Shoes:
                slots.shoesUid = itemUid;
                slots.shoesInstanceId = instanceId;
                break;

            case EquipmentType.Ring:
                if (slots.ring1Uid == 0)
                {
                    slots.ring1Uid = itemUid;
                    slots.ring1InstanceId = instanceId;
                }
                else
                {
                    slots.ring2Uid = itemUid;
                    slots.ring2InstanceId = instanceId;
                }
                break;

            case EquipmentType.Necklace:
                slots.necklaceUid = itemUid;
                slots.necklaceInstanceId = instanceId;
                break;

            case EquipmentType.Weapon:
                slots.weaponUid = itemUid;
                slots.weaponInstanceId = instanceId;
                break;

            case EquipmentType.SubWeapon:
                slots.subWeaponUid = itemUid;
                slots.subWeaponInstanceId = instanceId;
                break;
        }
    }

    private static void ClearSlot(EquipmentSlotData slots, EquipmentType equipType, int slotIndex)
    {
        if (slots == null)
            return;

        switch (equipType)
        {
            case EquipmentType.Helmet:
                slots.helmetUid = 0;
                slots.helmetInstanceId = null;
                break;

            case EquipmentType.Armor:
                slots.armorUid = 0;
                slots.armorInstanceId = null;
                break;

            case EquipmentType.Gloves:
                slots.glovesUid = 0;
                slots.glovesInstanceId = null;
                break;

            case EquipmentType.Shoes:
                slots.shoesUid = 0;
                slots.shoesInstanceId = null;
                break;

            case EquipmentType.Ring:
                if (slotIndex == 1)
                {
                    slots.ring1Uid = 0;
                    slots.ring1InstanceId = null;
                }
                else
                {
                    slots.ring2Uid = 0;
                    slots.ring2InstanceId = null;
                }
                break;

            case EquipmentType.Necklace:
                slots.necklaceUid = 0;
                slots.necklaceInstanceId = null;
                break;

            case EquipmentType.Weapon:
                slots.weaponUid = 0;
                slots.weaponInstanceId = null;
                break;

            case EquipmentType.SubWeapon:
                slots.subWeaponUid = 0;
                slots.subWeaponInstanceId = null;
                break;
        }
    }

    public static void RebuildEquipmentStats(CharacterData character)
    {
        if (character == null)
            return;

        character.ModifiedStats = new CharacterStats();
        character.ModifiedSpecialStats = new CharacterSpecialStats();

        if (character.EquipmentTraitRuntimes == null)
            character.EquipmentTraitRuntimes = new List<TraitRuntimeData>();
        else
            character.EquipmentTraitRuntimes.Clear();

        List<EquipmentRuntimeData> equipments = character.GetEquipmentRuntimes();

        foreach (EquipmentRuntimeData equipment in equipments)
        {
            if (equipment == null)
                continue;

            if (equipment.statModifiers != null)
                character.ModifiedStats += equipment.statModifiers;

            if (equipment.specialStatModifiers != null)
                character.ModifiedSpecialStats += equipment.specialStatModifiers;

            AddEquipmentTraits(character, equipment);
        }
    }

    private static void AddEquipmentTraits(CharacterData character, EquipmentRuntimeData equipment)
    {
        if (character == null || equipment == null || equipment.grantedTraitIds == null)
            return;

        foreach (int traitId in equipment.grantedTraitIds)
        {
            TraitDefinitionSO def = GameDataRegistry.Instance.GetTrait(traitId);

            if (def == null)
                continue;

            character.EquipmentTraitRuntimes.Add(new TraitRuntimeData
            {
                traitId = traitId,
                point = TraitGradeUtility.GetGradeValue(def.defaultAcquireGrade)
            });
        }
    }

    public static void UpdateAvailableAttributes(CharacterData character)
    {
        if (character == null)
            return;

        if (character.AvailableAttributes == null)
            character.AvailableAttributes = new List<SkillAttribute>();

        character.AvailableAttributes.Clear();

        WeaponDefinitionSO mainWeapon = character.GetMainWeapon();
        WeaponDefinitionSO subWeapon = character.GetSubWeapon();

        if (mainWeapon != null && mainWeapon.attributes != null)
            character.AvailableAttributes.AddRange(mainWeapon.attributes);

        if (subWeapon != null && subWeapon.attributes != null)
            character.AvailableAttributes.AddRange(subWeapon.attributes);

        if (character.monsterRoleId > 0 && GameDataRegistry.Instance != null)
        {
            MonsterRoleSO role = GameDataRegistry.Instance.GetMonsterRole(character.monsterRoleId);

            if (role != null && role.availableAttackAttributes != null)
            {
                foreach (SkillAttribute attribute in role.availableAttackAttributes)
                {
                    if (!character.AvailableAttributes.Contains(attribute))
                        character.AvailableAttributes.Add(attribute);
                }
            }
        }
    }

    public static void UpdateSkillAvailability(CharacterData character)
    {
        if (character == null || character.Skills == null)
            return;

        foreach (SkillRuntimeData runtime in character.Skills)
        {
            if (runtime == null)
                continue;

            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(runtime.skillUid);

            if (skill == null)
            {
                runtime.canUse = false;
                runtime.useOffHand = false;
                continue;
            }

            runtime.canUse = skill.CanBeUsedBy(character);
            runtime.useOffHand = runtime.canUse && skill.ShouldUseOffHand(character);
        }

        SkillManager.RefreshAutoQuickSlots(character);
    }
}
