using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum SharedInventoryType
{
    CompanyStorage,
    ExpeditionStorage
}

public static class SharedInventoryUtility
{
    public const int InitialCompanyStorageCapacity = 100;
    public const int ExpeditionStorageCapacity = 20;
    public const int MonsterEssenceItemUid = 20001;
    public const int FadedEssenceItemUid = 20002;
    public const int SkillBookItemUid = 20003;

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
            : ExpeditionStorageCapacity;
    }

    public static bool AddItem(
        List<InventorySlotData> storage,
        int itemUid,
        int count = 1,
        string equipmentInstanceId = null,
        int skillBookSkillUid = 0)
    {
        if (!AddItemInternal(
                storage,
                itemUid,
                count,
                equipmentInstanceId,
                skillBookSkillUid))
            return false;

        InventoryChanged?.Invoke();
        return true;
    }

    public static bool AddMonsterEssence(
        List<InventorySlotData> storage,
        string questId,
        CharacterData monster)
    {
        InventorySlotData essence = CreateMonsterEssence(questId, monster);

        return essence != null && AddInventorySlot(storage, essence);
    }

    public static InventorySlotData CreateMonsterEssence(
        string questId,
        CharacterData monster)
    {
        if (string.IsNullOrEmpty(questId) ||
            monster == null ||
            GameDataRegistry.Instance == null ||
            GameDataRegistry.Instance.GetItem(MonsterEssenceItemUid) == null)
        {
            return null;
        }

        List<MonsterEssenceTraitData> traits = new List<MonsterEssenceTraitData>();

        foreach (TraitRuntimeData runtime in monster.GetAllTraitRuntimes())
        {
            if (runtime == null || runtime.traitId <= 0)
                continue;

            MonsterEssenceTraitData existing = traits.Find(
                trait => trait.traitId == runtime.traitId);

            if (existing == null)
            {
                traits.Add(new MonsterEssenceTraitData
                {
                    traitId = runtime.traitId,
                    point = Mathf.Max(1, runtime.point)
                });
            }
            else
            {
                existing.point = Mathf.Max(existing.point, runtime.point);
            }
        }

        return new InventorySlotData
        {
            itemUid = MonsterEssenceItemUid,
            count = 1,
            essenceQuestId = questId,
            essenceMonsterRoleId = monster.monsterRoleId,
            essenceMonsterName = monster.Name,
            essenceTraits = traits
        };
    }

    public static bool AddInventorySlot(
        List<InventorySlotData> storage,
        InventorySlotData source)
    {
        if (storage == null || source == null || source.itemUid <= 0 || source.count <= 0)
            return false;

        if (!source.IsMonsterEssence())
        {
            return AddItem(
                storage,
                source.itemUid,
                source.count,
                source.equipmentInstanceId,
                source.skillBookSkillUid);
        }

        if (GameDataRegistry.Instance == null ||
            GameDataRegistry.Instance.GetItem(source.itemUid) is not ItemDefinitionSO item ||
            !HasCapacityFor(storage, item, source.count, null))
        {
            return false;
        }

        List<MonsterEssenceTraitData> traits = new List<MonsterEssenceTraitData>();
        List<MonsterEssenceTraitAppraisalData> appraisals =
            CloneEssenceTraitAppraisals(source.essenceTraitAppraisals);

        if (source.essenceTraits != null)
        {
            foreach (MonsterEssenceTraitData trait in source.essenceTraits)
            {
                if (trait == null)
                    continue;

                traits.Add(new MonsterEssenceTraitData
                {
                    traitId = trait.traitId,
                    point = trait.point
                });
            }
        }

        storage.Add(new InventorySlotData
        {
            itemUid = source.itemUid,
            count = source.count,
            essenceQuestId = source.essenceQuestId,
            essenceMonsterRoleId = source.essenceMonsterRoleId,
            essenceMonsterName = source.essenceMonsterName,
            essenceTraits = traits,
            essenceAppraised = source.essenceAppraised,
            essenceTraitCountRevealed = source.essenceTraitCountRevealed,
            essenceTraitAppraisals = appraisals,
            skillBookSkillUid = source.skillBookSkillUid
        });

        InventoryChanged?.Invoke();
        return true;
    }

    public static void ArchiveDefeatedExpedition(
        PlayerData playerData,
        string sourceQuestId,
        List<string> characterIds)
    {
        if (playerData == null || string.IsNullOrEmpty(sourceQuestId))
            return;

        if (playerData.lostExpeditionInventories == null)
            playerData.lostExpeditionInventories = new List<LostExpeditionInventoryData>();

        LostExpeditionInventoryData lost = playerData.lostExpeditionInventories.Find(
            inventory => inventory != null && inventory.sourceQuestId == sourceQuestId);

        if (lost == null)
        {
            lost = new LostExpeditionInventoryData
            {
                sourceQuestId = sourceQuestId
            };
            playerData.lostExpeditionInventories.Add(lost);
        }

        lost.characterIds = characterIds != null
            ? new List<string>(characterIds)
            : new List<string>();
        lost.items = CloneInventorySlots(playerData.expeditionStorage);

        if (playerData.expeditionStorage == null)
            playerData.expeditionStorage = new List<InventorySlotData>();
        else
            playerData.expeditionStorage.Clear();

        InventoryChanged?.Invoke();
    }

    public static int RecoverDefeatedExpedition(
        PlayerData playerData,
        string sourceQuestId)
    {
        if (playerData == null ||
            string.IsNullOrEmpty(sourceQuestId) ||
            playerData.lostExpeditionInventories == null)
        {
            return 0;
        }

        LostExpeditionInventoryData lost = playerData.lostExpeditionInventories.Find(
            inventory => inventory != null && inventory.sourceQuestId == sourceQuestId);

        if (lost == null)
            return 0;

        if (playerData.expeditionStorage == null)
            playerData.expeditionStorage = new List<InventorySlotData>();

        int recoveredCount = 0;
        List<InventorySlotData> remaining = new();

        foreach (InventorySlotData slot in lost.items ?? new List<InventorySlotData>())
        {
            if (AddInventorySlot(playerData.expeditionStorage, slot))
                recoveredCount += Mathf.Max(1, slot.count);
            else
                remaining.Add(CloneInventorySlot(slot));
        }

        if (remaining.Count == 0)
            playerData.lostExpeditionInventories.Remove(lost);
        else
            lost.items = remaining;

        return recoveredCount;
    }

    public static int DiscardDefeatedExpedition(
        PlayerData playerData,
        string sourceQuestId)
    {
        if (playerData == null ||
            string.IsNullOrEmpty(sourceQuestId) ||
            playerData.lostExpeditionInventories == null)
        {
            return 0;
        }

        LostExpeditionInventoryData lost = playerData.lostExpeditionInventories.Find(
            inventory => inventory != null && inventory.sourceQuestId == sourceQuestId);

        if (lost == null)
            return 0;

        int discardedCount = 0;

        foreach (InventorySlotData slot in lost.items ?? new List<InventorySlotData>())
        {
            if (slot == null)
                continue;

            discardedCount += Mathf.Max(1, slot.count);

            if (slot.IsGeneratedEquipment())
                EquipmentInstanceRepository.RemoveFromPlayer(slot.equipmentInstanceId);
        }

        playerData.lostExpeditionInventories.Remove(lost);
        InventoryChanged?.Invoke();
        return discardedCount;
    }

    public static int TransferExpeditionToCompany(PlayerData playerData)
    {
        if (playerData?.expeditionStorage == null)
            return 0;

        if (playerData.accountStorage == null)
            playerData.accountStorage = new List<InventorySlotData>();

        int movedCount = 0;
        InventorySlotData[] slots = playerData.expeditionStorage.ToArray();

        foreach (InventorySlotData slot in slots)
        {
            if (slot == null || !AddInventorySlot(playerData.accountStorage, slot))
                continue;

            movedCount += Mathf.Max(1, slot.count);
            RemoveItemInternal(
                playerData.expeditionStorage,
                slot,
                Mathf.Max(1, slot.count));
        }

        if (movedCount > 0)
            InventoryChanged?.Invoke();

        return movedCount;
    }

    public static bool DiscardInventorySlot(
        List<InventorySlotData> storage,
        InventorySlotData slot)
    {
        ItemDefinitionSO item = slot != null && GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(slot.itemUid)
            : null;

        if (storage == null || slot == null || item == null || !item.deletable)
            return false;

        int count = slot.IsGeneratedEquipment() ? 1 : Mathf.Max(1, slot.count);
        string equipmentInstanceId = slot.equipmentInstanceId;

        if (!RemoveItemInternal(storage, slot, count))
            return false;

        if (!string.IsNullOrEmpty(equipmentInstanceId))
            EquipmentInstanceRepository.RemoveFromPlayer(equipmentInstanceId);

        InventoryChanged?.Invoke();
        return true;
    }

    public static bool SellInventorySlot(
        PlayerData playerData,
        List<InventorySlotData> storage,
        InventorySlotData slot)
    {
        ItemDefinitionSO item = slot != null && GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(slot.itemUid)
            : null;

        if (playerData == null || storage == null || slot == null ||
            item == null || !item.tradeable)
        {
            return false;
        }

        int count = slot.IsGeneratedEquipment() ? 1 : Mathf.Max(1, slot.count);
        string equipmentInstanceId = slot.equipmentInstanceId;
        int salePrice = GetSalePrice(slot);

        if (!RemoveItemInternal(storage, slot, count))
            return false;

        playerData.gold += salePrice;

        if (!string.IsNullOrEmpty(equipmentInstanceId))
            EquipmentInstanceRepository.RemoveFromPlayer(equipmentInstanceId);

        InventoryChanged?.Invoke();
        return true;
    }

    public static int GetSalePrice(InventorySlotData slot)
    {
        ItemDefinitionSO item = slot != null && GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(slot.itemUid)
            : null;

        if (slot == null || item == null)
            return 0;

        int count = slot.IsGeneratedEquipment() ? 1 : Mathf.Max(1, slot.count);
        int basePrice = Mathf.Max(0, item.price) * count;

        if (!slot.IsGeneratedEquipment())
            return basePrice;

        GeneratedEquipmentData equipment =
            EquipmentInstanceRepository.Get(slot.equipmentInstanceId);

        if (equipment == null)
            return basePrice;

        float tierMultiplier = 1f + Mathf.Max(0, equipment.tier - 1) * 0.5f;
        float rarityMultiplier = equipment.rarity switch
        {
            EquipmentRarity.Uncommon => 1.25f,
            EquipmentRarity.Rare => 1.6f,
            EquipmentRarity.Epic => 2.1f,
            EquipmentRarity.Legendary => 3f,
            _ => 1f
        };
        int adjustedPrice = Mathf.RoundToInt(basePrice * tierMultiplier * rarityMultiplier);

        return Mathf.Max(basePrice, Mathf.RoundToInt(adjustedPrice / 5f) * 5);
    }

    public static InventorySlotData CloneInventorySlot(InventorySlotData source)
    {
        if (source == null)
            return null;

        List<MonsterEssenceTraitData> traits = new();
        List<MonsterEssenceTraitAppraisalData> appraisals =
            CloneEssenceTraitAppraisals(source.essenceTraitAppraisals);

        foreach (MonsterEssenceTraitData trait in
                 source.essenceTraits ?? new List<MonsterEssenceTraitData>())
        {
            if (trait == null)
                continue;

            traits.Add(new MonsterEssenceTraitData
            {
                traitId = trait.traitId,
                point = trait.point
            });
        }

        return new InventorySlotData
        {
            itemUid = source.itemUid,
            count = source.count,
            equipmentInstanceId = source.equipmentInstanceId,
            essenceQuestId = source.essenceQuestId,
            essenceMonsterRoleId = source.essenceMonsterRoleId,
            essenceMonsterName = source.essenceMonsterName,
            essenceTraits = traits,
            essenceAppraised = source.essenceAppraised,
            essenceTraitCountRevealed = source.essenceTraitCountRevealed,
            essenceTraitAppraisals = appraisals,
            skillBookSkillUid = source.skillBookSkillUid
        };
    }

    public static bool AppraiseMonsterEssence(
        CharacterManager manager,
        List<InventorySlotData> storage,
        InventorySlotData sourceSlot)
    {
        if (manager == null ||
            manager.character == null ||
            manager.character.FinalStats == null ||
            !manager.character.IsAlive ||
            storage == null ||
            sourceSlot == null ||
            !storage.Contains(sourceSlot) ||
            !sourceSlot.IsMonsterEssence() ||
            sourceSlot.essenceAppraised)
        {
            return false;
        }

        int perception = Mathf.Max(
            manager.character.FinalStats.Detection,
            manager.character.FinalStats.Insight);
        sourceSlot.essenceAppraised = true;
        sourceSlot.essenceTraitCountRevealed = RollAppraisal(
            GetAppraisalChance(45, perception));
        sourceSlot.essenceTraitAppraisals =
            new List<MonsterEssenceTraitAppraisalData>();

        if (sourceSlot.essenceTraitCountRevealed)
        {
            foreach (MonsterEssenceTraitData trait in
                     sourceSlot.essenceTraits ?? new List<MonsterEssenceTraitData>())
            {
                if (trait == null)
                    continue;

                MonsterEssenceAppraisalRevealLevel revealLevel =
                    MonsterEssenceAppraisalRevealLevel.None;

                if (RollAppraisal(GetAppraisalChance(35, perception)))
                {
                    revealLevel = MonsterEssenceAppraisalRevealLevel.Polarity;

                    if (RollAppraisal(GetAppraisalChance(25, perception)))
                    {
                        revealLevel = MonsterEssenceAppraisalRevealLevel.Grade;

                        if (RollAppraisal(GetAppraisalChance(15, perception)))
                            revealLevel = MonsterEssenceAppraisalRevealLevel.Details;
                    }
                }

                sourceSlot.essenceTraitAppraisals.Add(
                    new MonsterEssenceTraitAppraisalData
                    {
                        traitId = trait.traitId,
                        revealLevel = revealLevel
                    });
            }
        }

        InventoryChanged?.Invoke();
        return true;
    }

    public static string GetMonsterEssenceAppraisalText(InventorySlotData slot)
    {
        if (slot == null || !slot.IsMonsterEssence() || !slot.essenceAppraised)
            return string.Empty;

        if (!slot.essenceTraitCountRevealed)
            return "아무것도 간파하지 못했습니다.";

        List<MonsterEssenceTraitData> traits =
            slot.essenceTraits ?? new List<MonsterEssenceTraitData>();
        StringBuilder builder = new StringBuilder();
        builder.Append($"특성 수: {traits.Count}");

        for (int i = 0; i < traits.Count; i++)
        {
            MonsterEssenceTraitData essenceTrait = traits[i];
            MonsterEssenceTraitAppraisalData appraisal =
                slot.essenceTraitAppraisals?.Find(entry =>
                    entry != null &&
                    essenceTrait != null &&
                    entry.traitId == essenceTrait.traitId);
            MonsterEssenceAppraisalRevealLevel revealLevel = appraisal != null
                ? appraisal.revealLevel
                : MonsterEssenceAppraisalRevealLevel.None;
            TraitDefinitionSO trait = essenceTrait != null && GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetTrait(essenceTrait.traitId)
                : null;

            builder.Append($"\n{i + 1}. ");

            if (trait == null || revealLevel == MonsterEssenceAppraisalRevealLevel.None)
            {
                builder.Append("???");
                continue;
            }

            builder.Append(GetTraitPolarityText(trait.polarity));

            if (revealLevel < MonsterEssenceAppraisalRevealLevel.Grade)
            {
                builder.Append(" · ???");
                continue;
            }

            TraitRuntimeData runtime = new TraitRuntimeData
            {
                traitId = essenceTrait.traitId,
                point = Mathf.Max(1, essenceTrait.point)
            };
            TraitGrade grade = TraitGradeUtility.GetGrade(runtime.point);
            builder.Append($" · {grade}");

            if (revealLevel < MonsterEssenceAppraisalRevealLevel.Details)
            {
                builder.Append(" · ???");
                continue;
            }

            builder.Append($" {trait.traitName}");
            string details = TownTraitPreviewCardUI.BuildDescription(runtime, trait);

            if (!string.IsNullOrWhiteSpace(details))
                builder.Append($"\n   {details.Replace("\n", "\n   ")}");
        }

        return builder.ToString();
    }

    private static int GetAppraisalChance(int baseChance, int perception)
    {
        return Mathf.Clamp(
            Mathf.RoundToInt(baseChance + Mathf.Max(0, perception) * 0.75f),
            5,
            95);
    }

    private static bool RollAppraisal(int chance)
    {
        return UnityEngine.Random.Range(1, 101) <= chance;
    }

    private static string GetTraitPolarityText(TraitPolarity polarity)
    {
        return polarity switch
        {
            TraitPolarity.Positive => "긍정",
            TraitPolarity.Negative => "부정",
            TraitPolarity.Mixed => "혼합",
            _ => "???"
        };
    }

    private static List<MonsterEssenceTraitAppraisalData> CloneEssenceTraitAppraisals(
        List<MonsterEssenceTraitAppraisalData> source)
    {
        List<MonsterEssenceTraitAppraisalData> result = new();

        foreach (MonsterEssenceTraitAppraisalData appraisal in
                 source ?? new List<MonsterEssenceTraitAppraisalData>())
        {
            if (appraisal == null)
                continue;

            result.Add(new MonsterEssenceTraitAppraisalData
            {
                traitId = appraisal.traitId,
                revealLevel = appraisal.revealLevel
            });
        }

        return result;
    }

    private static List<InventorySlotData> CloneInventorySlots(
        List<InventorySlotData> source)
    {
        List<InventorySlotData> result = new();

        foreach (InventorySlotData slot in source ?? new List<InventorySlotData>())
        {
            InventorySlotData clone = CloneInventorySlot(slot);

            if (clone != null && clone.itemUid > 0 && clone.count > 0)
                result.Add(clone);
        }

        return result;
    }

    public static int ExpireMonsterEssences(
        List<InventorySlotData> storage,
        string questId)
    {
        if (storage == null ||
            string.IsNullOrEmpty(questId) ||
            GameDataRegistry.Instance == null ||
            GameDataRegistry.Instance.GetItem(FadedEssenceItemUid) == null)
        {
            return 0;
        }

        int changedCount = 0;

        foreach (InventorySlotData slot in storage)
        {
            if (slot == null || slot.essenceQuestId != questId)
                continue;

            slot.itemUid = FadedEssenceItemUid;
            slot.equipmentInstanceId = null;
            slot.essenceQuestId = null;
            slot.essenceMonsterRoleId = 0;
            slot.essenceMonsterName = null;
            slot.essenceTraits?.Clear();
            slot.essenceAppraised = false;
            slot.essenceTraitCountRevealed = false;
            slot.essenceTraitAppraisals?.Clear();
            changedCount += Mathf.Max(1, slot.count);
        }

        if (changedCount > 0)
            InventoryChanged?.Invoke();

        return changedCount;
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

        if (sourceSlot.IsMonsterEssence())
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
        int skillBookSkillUid = sourceSlot.skillBookSkillUid;

        if (!AddItemInternal(
                destination,
                itemUid,
                moveCount,
                instanceId,
                skillBookSkillUid))
            return false;

        if (!RemoveItemInternal(source, sourceSlot, moveCount))
        {
            RemoveMatchingItemInternal(
                destination,
                itemUid,
                moveCount,
                instanceId,
                skillBookSkillUid);
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
            {
                int skillUid = sourceSlot.IsSkillBook()
                    ? sourceSlot.skillBookSkillUid
                    : consumable.skillUid;
                applied = SkillManager.AddSkill(character, skillUid);
                break;
            }

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


            case ConsumableType.MonsterEssence:
                applied = UseMonsterEssence(manager, storage, sourceSlot, consumable);
                break;
        }

        if (!applied || !RemoveItemInternal(storage, sourceSlot, 1))
            return false;

        manager.UpdateCharacterUI();
        InventoryChanged?.Invoke();
        CharacterChanged?.Invoke(manager);
        return true;
    }

    private static bool UseMonsterEssence(
        CharacterManager manager,
        List<InventorySlotData> storage,
        InventorySlotData sourceSlot,
        ConsumableDefinitionSO consumable)
    {
        ActiveQuestRuntime active = QuestManager.Instance != null
            ? QuestManager.Instance.active
            : null;

        if (!sourceSlot.IsMonsterEssence() ||
            active?.def == null ||
            active.def.id != sourceSlot.essenceQuestId ||
            sourceSlot.essenceTraits == null)
        {
            return false;
        }

        CharacterData character = manager.character;
        List<MonsterEssenceTraitData> candidates = new List<MonsterEssenceTraitData>();
        List<int> weights = new List<int>();
        int totalWeight = 0;

        foreach (MonsterEssenceTraitData essenceTrait in sourceSlot.essenceTraits)
        {
            TraitDefinitionSO trait = essenceTrait != null && GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetTrait(essenceTrait.traitId)
                : null;

            if (trait == null)
                continue;

            TraitRuntimeData owned = character.Traits?.Find(
                runtime => runtime != null && runtime.traitId == trait.id);

            if (owned != null &&
                (!trait.canGradeUp || owned.point >= TraitGradeUtility.MaxPoint))
            {
                continue;
            }

            TraitGrade grade = TraitGradeUtility.GetGrade(
                Mathf.Max(1, essenceTrait.point));
            int weight = Mathf.Max(0, consumable.GetMonsterEssenceTraitWeight(grade));

            weight *= trait.polarity == TraitPolarity.Negative ? 1 : 2;

            if (weight <= 0)
                continue;

            candidates.Add(essenceTrait);
            weights.Add(weight);
            totalWeight += weight;
        }

        if (candidates.Count == 0 || totalWeight <= 0)
            return false;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int selectedIndex = 0;

        for (int i = 0; i < weights.Count; i++)
        {
            if (roll < weights[i])
            {
                selectedIndex = i;
                break;
            }

            roll -= weights[i];
        }

        MonsterEssenceTraitData selected = candidates[selectedIndex];
        List<InventorySlotData> equippedBefore =
            GetAllEquippedSlots(character.EquipmentSlots);
        int before = GetTraitPoint(character, selected.traitId);

        TraitManager.AddTrait(
            manager,
            selected.traitId,
            TraitGradeUtility.GetGrade(Mathf.Max(1, selected.point)));

        bool applied = GetTraitPoint(character, selected.traitId) > before;

        if (applied)
        {
            TraitDefinitionSO acquiredTrait = GameDataRegistry.Instance.GetTrait(
                selected.traitId);
            TraitRuntimeData acquiredRuntime = character.Traits?.Find(
                runtime => runtime != null && runtime.traitId == selected.traitId);

            if (acquiredTrait != null && acquiredRuntime != null)
            {
                UIManager.Instance?.ShowTraitAcquisition(
                    acquiredTrait,
                    TraitGradeUtility.GetGrade(acquiredRuntime.point));
            }

            ReturnRemovedEquipments(
                storage,
                equippedBefore,
                GetAllEquippedSlots(character.EquipmentSlots));
        }

        return applied;
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

        if (equipment.equipType == EquipmentType.SubWeapon &&
            !character.CanEquipSubWeapon())
        {
            return false;
        }

        if (equipment is WeaponDefinitionSO weapon)
        {
            if (equipment.equipType == EquipmentType.Weapon &&
                !character.CanEquipMainWeapon(weapon))
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
        string equipmentInstanceId,
        int skillBookSkillUid = 0)
    {
        if (storage == null || count <= 0 || GameDataRegistry.Instance == null)
            return false;

        ItemDefinitionSO item = GameDataRegistry.Instance.GetItem(itemUid);

        if (item == null)
            return false;

        if (!HasCapacityFor(storage, item, count, equipmentInstanceId))
            return false;

        if (skillBookSkillUid > 0)
        {
            if (itemUid != SkillBookItemUid || count != 1)
                return false;

            storage.Add(new InventorySlotData
            {
                itemUid = itemUid,
                count = 1,
                skillBookSkillUid = skillBookSkillUid
            });

            return true;
        }

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

        if (playerData == null)
            return true;

        int capacity;

        if (ReferenceEquals(storage, playerData.accountStorage))
            capacity = InitialCompanyStorageCapacity;
        else if (ReferenceEquals(storage, playerData.expeditionStorage))
            capacity = ExpeditionStorageCapacity;
        else
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

        return occupiedSlots + requiredSlots <= capacity;
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
        string equipmentInstanceId,
        int skillBookSkillUid = 0)
    {
        if (storage == null || count <= 0)
            return;

        for (int i = storage.Count - 1; i >= 0 && count > 0; i--)
        {
            InventorySlotData slot = storage[i];

            if (slot == null ||
                slot.itemUid != itemUid ||
                slot.equipmentInstanceId != equipmentInstanceId ||
                slot.skillBookSkillUid != skillBookSkillUid)
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
