using System;
using System.Collections.Generic;
using UnityEngine;

public enum SharedInventoryType
{
    CompanyStorage,
    ExpeditionStorage
}

public static class SharedInventoryUtility
{
    public const int InitialCompanyStorageCapacity = 100;

    public static event Action InventoryChanged;
    public static event Action<CharacterManager> EquipmentChanged;
    public static event Action<CharacterManager> CharacterChanged;

    public static List<InventorySlotData> GetStorage(
        PlayerData playerData,
        SharedInventoryType inventoryType)
    {
        if (playerData == null)
            return null;

        if (inventoryType == SharedInventoryType.CompanyStorage)
        {
            if (playerData.accountStorage == null)
                playerData.accountStorage = new List<InventorySlotData>();

            return playerData.accountStorage;
        }

        if (playerData.expeditionStorage == null)
            playerData.expeditionStorage = new List<InventorySlotData>();

        return playerData.expeditionStorage;
    }

    public static int GetStorageCapacity(
        PlayerData playerData,
        SharedInventoryType inventoryType)
    {
        return inventoryType == SharedInventoryType.CompanyStorage
            ? InitialCompanyStorageCapacity
            : int.MaxValue;
    }

    public static bool AddItem(
        List<InventorySlotData> storage,
        int itemUid,
        int count = 1,
        string equipmentInstanceId = null)
    {
        if (!AddItemInternal(storage, itemUid, count, equipmentInstanceId))
            return false;

        InventoryChanged?.Invoke();
        return true;
    }

    public static bool RemoveItem(
        List<InventorySlotData> storage,
        InventorySlotData slot,
        int count = 1)
    {
        if (!RemoveItemInternal(storage, slot, count))
            return false;

        InventoryChanged?.Invoke();
        return true;
    }

    public static bool MoveItem(
        PlayerData playerData,
        SharedInventoryType sourceType,
        SharedInventoryType destinationType,
        InventorySlotData sourceSlot,
        int count = 1)
    {
        if (playerData == null || sourceType == destinationType || sourceSlot == null)
            return false;

        List<InventorySlotData> source = GetStorage(playerData, sourceType);
        List<InventorySlotData> destination = GetStorage(playerData, destinationType);

        if (source == null || destination == null || !source.Contains(sourceSlot))
            return false;

        int moveCount = sourceSlot.IsGeneratedEquipment()
            ? 1
            : Mathf.Clamp(count, 1, sourceSlot.count);

        int itemUid = sourceSlot.itemUid;
        string instanceId = sourceSlot.equipmentInstanceId;

        if (!AddItemInternal(destination, itemUid, moveCount, instanceId))
            return false;

        if (!RemoveItemInternal(source, sourceSlot, moveCount))
        {
            RemoveMatchingItemInternal(destination, itemUid, moveCount, instanceId);
            return false;
        }

        InventoryChanged?.Invoke();
        return true;
    }

    public static bool EquipFromStorage(
        CharacterManager manager,
        List<InventorySlotData> storage,
        InventorySlotData sourceSlot,
        int slotIndex = 1)
    {
        if (manager == null || manager.character == null || storage == null || sourceSlot == null)
            return false;

        if (!storage.Contains(sourceSlot) || sourceSlot.count <= 0 || GameDataRegistry.Instance == null)
            return false;

        EquipmentDefinitionSO equipment = GameDataRegistry.Instance.GetEquipment(sourceSlot.itemUid);

        if (equipment == null || !CanEquip(manager.character, equipment))
            return false;

        InventorySlotData equipped = GetEquippedSlot(
            manager.character.EquipmentSlots,
            equipment.equipType,
            slotIndex);

        int newItemUid = sourceSlot.itemUid;
        string newInstanceId = sourceSlot.equipmentInstanceId;

        if (!RemoveItemInternal(storage, sourceSlot, 1))
            return false;

        if (equipped != null && equipped.itemUid > 0 &&
            !AddItemInternal(
                storage,
                equipped.itemUid,
                1,
                equipped.equipmentInstanceId))
        {
            AddItemInternal(storage, newItemUid, 1, newInstanceId);
            return false;
        }

        SetEquippedSlot(
            manager.character.EquipmentSlots,
            equipment.equipType,
            slotIndex,
            newItemUid,
            newInstanceId);

        RefreshEquipment(manager);

        InventoryChanged?.Invoke();
        EquipmentChanged?.Invoke(manager);
        CharacterChanged?.Invoke(manager);
        return true;
    }

    public static bool UnequipToStorage(
        CharacterManager manager,
        List<InventorySlotData> storage,
        EquipmentType equipmentType,
        int slotIndex = 1)
    {
        if (manager == null || manager.character == null || storage == null)
            return false;

        InventorySlotData equipped = GetEquippedSlot(
            manager.character.EquipmentSlots,
            equipmentType,
            slotIndex);

        if (equipped == null || equipped.itemUid <= 0)
            return false;

        if (!AddItemInternal(
                storage,
                equipped.itemUid,
                1,
                equipped.equipmentInstanceId))
        {
            return false;
        }

        SetEquippedSlot(
            manager.character.EquipmentSlots,
            equipmentType,
            slotIndex,
            0,
            null);

        RefreshEquipment(manager);

        InventoryChanged?.Invoke();
        EquipmentChanged?.Invoke(manager);
        CharacterChanged?.Invoke(manager);
        return true;
    }

    public static bool UseItem(
        CharacterManager manager,
        List<InventorySlotData> storage,
        InventorySlotData sourceSlot)
    {
        if (manager == null || manager.character == null || storage == null || sourceSlot == null)
            return false;

        if (!storage.Contains(sourceSlot) || sourceSlot.count <= 0 || GameDataRegistry.Instance == null)
            return false;

        ConsumableDefinitionSO consumable =
            GameDataRegistry.Instance.GetItem(sourceSlot.itemUid) as ConsumableDefinitionSO;

        if (consumable == null)
            return false;

        CharacterData character = manager.character;
        bool applied = false;

        switch (consumable.consumableType)
        {
            case ConsumableType.HealHp:
            {
                int before = character.CurrentHp;
                character.CurrentHp = Mathf.Clamp(
                    character.CurrentHp + Mathf.Max(0, consumable.hpAmount),
                    0,
                    character.FinalStats.MaxHp);
                applied = character.CurrentHp > before;
                break;
            }

            case ConsumableType.RecoverStamina:
            {
                int before = character.CurrentStamina;
                character.CurrentStamina = Mathf.Clamp(
                    character.CurrentStamina + Mathf.Max(0, consumable.staminaAmount),
                    0,
                    character.FinalStats.MaxStamina);
                applied = character.CurrentStamina > before;
                break;
            }

            case ConsumableType.RecoverMentality:
            {
                int before = character.CurrentMentality;
                character.CurrentMentality = Mathf.Clamp(
                    character.CurrentMentality + Mathf.Max(0, consumable.mentalityAmount),
                    0,
                    character.FinalStats.MaxMentality);
                applied = character.CurrentMentality > before;
                break;
            }

            case ConsumableType.LearnSkill:
                applied = SkillManager.AddSkill(character, consumable.skillUid);
                break;

            case ConsumableType.GainTrait:
            {
                TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(consumable.traitId);

                if (trait == null)
                    break;

                List<InventorySlotData> equippedBefore =
                    GetAllEquippedSlots(character.EquipmentSlots);

                int before = GetTraitPoint(character, consumable.traitId);
                TraitManager.AddTrait(manager, consumable.traitId);
                applied = GetTraitPoint(character, consumable.traitId) > before;

                if (applied)
                {
                    ReturnRemovedEquipments(
                        storage,
                        equippedBefore,
                        GetAllEquippedSlots(character.EquipmentSlots));
                }

                break;
            }
        }

        if (!applied || !RemoveItemInternal(storage, sourceSlot, 1))
            return false;

        InventoryChanged?.Invoke();
        CharacterChanged?.Invoke(manager);
        return true;
    }

    public static void SaveChanges(CharacterManager manager = null)
    {
        if (PlayerManager.Instance == null)
            return;

        PlayerManager.Instance.SavePlayerDataToPlayFab();

        if (manager != null &&
            manager.character != null &&
            !string.IsNullOrEmpty(manager.character.ID))
        {
            PlayerManager.Instance.SaveCharacter(manager.character);
        }
    }

    public static InventorySlotData GetEquippedSlot(
        EquipmentSlotData slots,
        EquipmentType equipmentType,
        int slotIndex = 1)
    {
        if (slots == null)
            return null;

        int itemUid = 0;
        string instanceId = null;

        switch (equipmentType)
        {
            case EquipmentType.Helmet:
                itemUid = slots.helmetUid;
                instanceId = slots.helmetInstanceId;
                break;

            case EquipmentType.Armor:
                itemUid = slots.armorUid;
                instanceId = slots.armorInstanceId;
                break;

            case EquipmentType.Gloves:
                itemUid = slots.glovesUid;
                instanceId = slots.glovesInstanceId;
                break;

            case EquipmentType.Shoes:
                itemUid = slots.shoesUid;
                instanceId = slots.shoesInstanceId;
                break;

            case EquipmentType.Ring:
                if (slotIndex == 2)
                {
                    itemUid = slots.ring2Uid;
                    instanceId = slots.ring2InstanceId;
                }
                else
                {
                    itemUid = slots.ring1Uid;
                    instanceId = slots.ring1InstanceId;
                }
                break;

            case EquipmentType.Necklace:
                itemUid = slots.necklaceUid;
                instanceId = slots.necklaceInstanceId;
                break;

            case EquipmentType.Weapon:
                itemUid = slots.weaponUid;
                instanceId = slots.weaponInstanceId;
                break;

            case EquipmentType.SubWeapon:
                itemUid = slots.subWeaponUid;
                instanceId = slots.subWeaponInstanceId;
                break;
        }

        if (itemUid <= 0)
            return null;

        return new InventorySlotData
        {
            itemUid = itemUid,
            count = 1,
            equipmentInstanceId = instanceId
        };
    }

    private static void SetEquippedSlot(
        EquipmentSlotData slots,
        EquipmentType equipmentType,
        int slotIndex,
        int itemUid,
        string instanceId)
    {
        if (slots == null)
            return;

        switch (equipmentType)
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
                if (slotIndex == 2)
                {
                    slots.ring2Uid = itemUid;
                    slots.ring2InstanceId = instanceId;
                }
                else
                {
                    slots.ring1Uid = itemUid;
                    slots.ring1InstanceId = instanceId;
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

    private static void RefreshEquipment(CharacterManager manager)
    {
        CharacterData character = manager.character;

        EquipmentManager.RebuildEquipmentStats(character);
        EquipmentManager.UpdateAvailableAttributes(character);
        EquipmentManager.UpdateSkillAvailability(character);

        character.RemoveAllTraits(manager);
        character.ApplyAllTraits(manager);
        character.UpdateFinalStats();

        CharacterCustomization customization = manager.GetComponent<CharacterCustomization>();

        if (customization != null)
            customization.UpdateEquipmentAppearance(character);
    }

    private static bool CanEquip(CharacterData character, EquipmentDefinitionSO equipment)
    {
        if (character == null || equipment == null)
            return false;

        if (equipment is WeaponDefinitionSO weapon)
        {
            if (equipment.equipType == EquipmentType.Weapon &&
                !character.CanEquipMainWeapon(weapon))
            {
                return false;
            }

            if (equipment.equipType == EquipmentType.SubWeapon &&
                !character.CanEquipSubWeapon())
            {
                return false;
            }
        }

        return true;
    }

    private static int GetTraitPoint(CharacterData character, int traitId)
    {
        if (character == null || character.Traits == null)
            return 0;

        TraitRuntimeData runtime = character.Traits.Find(t => t != null && t.traitId == traitId);
        return runtime != null ? runtime.point : 0;
    }

    private static List<InventorySlotData> GetAllEquippedSlots(EquipmentSlotData slots)
    {
        List<InventorySlotData> result = new List<InventorySlotData>();

        AddEquippedSlot(result, GetEquippedSlot(slots, EquipmentType.Helmet));
        AddEquippedSlot(result, GetEquippedSlot(slots, EquipmentType.Armor));
        AddEquippedSlot(result, GetEquippedSlot(slots, EquipmentType.Gloves));
        AddEquippedSlot(result, GetEquippedSlot(slots, EquipmentType.Shoes));
        AddEquippedSlot(result, GetEquippedSlot(slots, EquipmentType.Ring, 1));
        AddEquippedSlot(result, GetEquippedSlot(slots, EquipmentType.Ring, 2));
        AddEquippedSlot(result, GetEquippedSlot(slots, EquipmentType.Necklace));
        AddEquippedSlot(result, GetEquippedSlot(slots, EquipmentType.Weapon));
        AddEquippedSlot(result, GetEquippedSlot(slots, EquipmentType.SubWeapon));

        return result;
    }

    private static void AddEquippedSlot(
        List<InventorySlotData> result,
        InventorySlotData slot)
    {
        if (slot != null)
            result.Add(slot);
    }

    private static void ReturnRemovedEquipments(
        List<InventorySlotData> storage,
        List<InventorySlotData> before,
        List<InventorySlotData> after)
    {
        if (storage == null || before == null)
            return;

        List<InventorySlotData> remaining = after != null
            ? new List<InventorySlotData>(after)
            : new List<InventorySlotData>();

        foreach (InventorySlotData previous in before)
        {
            int index = remaining.FindIndex(current =>
                current != null &&
                current.itemUid == previous.itemUid &&
                current.equipmentInstanceId == previous.equipmentInstanceId);

            if (index >= 0)
            {
                remaining.RemoveAt(index);
                continue;
            }

            AddItemInternal(
                storage,
                previous.itemUid,
                1,
                previous.equipmentInstanceId);
        }
    }

    private static bool AddItemInternal(
        List<InventorySlotData> storage,
        int itemUid,
        int count,
        string equipmentInstanceId)
    {
        if (storage == null || count <= 0 || GameDataRegistry.Instance == null)
            return false;

        ItemDefinitionSO item = GameDataRegistry.Instance.GetItem(itemUid);

        if (item == null)
            return false;

        if (!HasCapacityFor(storage, item, count, equipmentInstanceId))
            return false;

        if (!string.IsNullOrEmpty(equipmentInstanceId))
        {
            if (count != 1)
                return false;

            if (storage.Exists(slot =>
                    slot != null &&
                    slot.equipmentInstanceId == equipmentInstanceId))
            {
                return false;
            }

            storage.Add(new InventorySlotData
            {
                itemUid = itemUid,
                count = 1,
                equipmentInstanceId = equipmentInstanceId
            });

            return true;
        }

        int remaining = count;
        int maxStack = Mathf.Max(1, item.maxStack);

        if (maxStack > 1)
        {
            foreach (InventorySlotData slot in storage)
            {
                if (slot == null || slot.itemUid != itemUid || slot.IsGeneratedEquipment())
                    continue;

                int add = Mathf.Min(Mathf.Max(0, maxStack - slot.count), remaining);
                slot.count += add;
                remaining -= add;

                if (remaining <= 0)
                    break;
            }
        }

        while (remaining > 0)
        {
            int add = Mathf.Min(maxStack, remaining);

            storage.Add(new InventorySlotData
            {
                itemUid = itemUid,
                count = add,
                equipmentInstanceId = null
            });

            remaining -= add;
        }

        return true;
    }

    private static bool HasCapacityFor(
        List<InventorySlotData> storage,
        ItemDefinitionSO item,
        int count,
        string equipmentInstanceId)
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData == null || !ReferenceEquals(storage, playerData.accountStorage))
            return true;

        int occupiedSlots = 0;

        foreach (InventorySlotData slot in storage)
        {
            if (slot != null && slot.itemUid > 0 && slot.count > 0)
                occupiedSlots++;
        }

        int requiredSlots;

        if (!string.IsNullOrEmpty(equipmentInstanceId))
        {
            requiredSlots = 1;
        }
        else
        {
            int remaining = count;
            int maxStack = Mathf.Max(1, item.maxStack);

            if (maxStack > 1)
            {
                foreach (InventorySlotData slot in storage)
                {
                    if (slot == null ||
                        slot.itemUid != item.uid ||
                        slot.IsGeneratedEquipment())
                    {
                        continue;
                    }

                    remaining -= Mathf.Max(0, maxStack - slot.count);

                    if (remaining <= 0)
                        return true;
                }
            }

            requiredSlots = Mathf.CeilToInt(Mathf.Max(0, remaining) / (float)maxStack);
        }

        return occupiedSlots + requiredSlots <= InitialCompanyStorageCapacity;
    }

    private static bool RemoveItemInternal(
        List<InventorySlotData> storage,
        InventorySlotData slot,
        int count)
    {
        if (storage == null || slot == null || count <= 0 || !storage.Contains(slot))
            return false;

        int removeCount = slot.IsGeneratedEquipment()
            ? 1
            : Mathf.Clamp(count, 1, slot.count);

        slot.count -= removeCount;

        if (slot.count <= 0)
            storage.Remove(slot);

        return true;
    }

    private static void RemoveMatchingItemInternal(
        List<InventorySlotData> storage,
        int itemUid,
        int count,
        string equipmentInstanceId)
    {
        if (storage == null || count <= 0)
            return;

        for (int i = storage.Count - 1; i >= 0 && count > 0; i--)
        {
            InventorySlotData slot = storage[i];

            if (slot == null ||
                slot.itemUid != itemUid ||
                slot.equipmentInstanceId != equipmentInstanceId)
            {
                continue;
            }

            int remove = Mathf.Min(count, slot.count);
            slot.count -= remove;
            count -= remove;

            if (slot.count <= 0)
                storage.RemoveAt(i);
        }
    }
}
