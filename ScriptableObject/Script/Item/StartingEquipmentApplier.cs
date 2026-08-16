using System.Collections.Generic;

public static class StartingEquipmentApplier
{
    public static void ApplyStartingEquipmentSlots(CharacterData character, EquipmentSpawnSlotSet slotSet)
    {
        if (character == null || slotSet == null)
            return;

        if (character.EquipmentSlots == null)
            character.EquipmentSlots = new EquipmentSlotData();

        ApplyOne(character, slotSet.helmet);
        ApplyOne(character, slotSet.armor);
        ApplyOne(character, slotSet.gloves);
        ApplyOne(character, slotSet.shoes);

        ApplyOne(character, slotSet.ring1);
        ApplyOne(character, slotSet.ring2);
        ApplyOne(character, slotSet.necklace);

        ApplyOne(character, slotSet.weapon);
        ApplyOne(character, slotSet.subWeapon);

        RecalculateCharacter(character);
    }

    public static void ApplyStartingEquipments(CharacterData character, List<EquipmentSpawnData> equipments)
    {
        if (character == null || equipments == null)
            return;

        if (character.EquipmentSlots == null)
            character.EquipmentSlots = new EquipmentSlotData();

        foreach (EquipmentSpawnData spawnData in equipments)
            ApplyOne(character, spawnData);

        RecalculateCharacter(character);
    }

    private static void ApplyOne(CharacterData character, EquipmentSpawnData spawnData)
    {
        if (character == null || spawnData == null)
            return;

        if (!spawnData.equipOnSpawn)
            return;

        if (spawnData.equipmentUid <= 0)
            return;

        GeneratedEquipmentData generated = EquipmentSpawnUtility.GenerateEquipment(spawnData);

        if (generated == null)
            return;

        EquipmentDefinitionSO definition = GameDataRegistry.Instance.GetEquipment(generated.definitionUid);

        if (definition == null)
            return;

        EquipmentInstanceRepository.AddToPlayer(generated);

        EquipmentManager.SetEquipmentSlot(
            character.EquipmentSlots,
            definition.equipType,
            definition.uid,
            generated.instanceId);
    }

    private static void RecalculateCharacter(CharacterData character)
    {
        EquipmentManager.RebuildEquipmentStats(character);
        EquipmentManager.UpdateAvailableAttributes(character);
        EquipmentManager.UpdateSkillAvailability(character);

        character.RemoveAllTraits(null);
        character.ApplyAllTraits(null);
        character.UpdateFinalStats();
    }
}