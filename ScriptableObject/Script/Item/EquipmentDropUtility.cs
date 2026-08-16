using System.Collections.Generic;

public static class EquipmentDropUtility
{
    public static List<GeneratedEquipmentData> GetDroppableEquipments(CharacterData character)
    {
        List<GeneratedEquipmentData> result = new List<GeneratedEquipmentData>();

        if (character == null)
            return result;

        TryAddDroppable(result, character.EquipmentSlots.helmetInstanceId);
        TryAddDroppable(result, character.EquipmentSlots.armorInstanceId);
        TryAddDroppable(result, character.EquipmentSlots.glovesInstanceId);
        TryAddDroppable(result, character.EquipmentSlots.shoesInstanceId);

        TryAddDroppable(result, character.EquipmentSlots.ring1InstanceId);
        TryAddDroppable(result, character.EquipmentSlots.ring2InstanceId);
        TryAddDroppable(result, character.EquipmentSlots.necklaceInstanceId);

        TryAddDroppable(result, character.EquipmentSlots.weaponInstanceId);
        TryAddDroppable(result, character.EquipmentSlots.subWeaponInstanceId);

        return result;
    }

    private static void TryAddDroppable(List<GeneratedEquipmentData> list, string instanceId)
    {
        if (list == null)
            return;

        if (string.IsNullOrEmpty(instanceId))
            return;

        GeneratedEquipmentData generated = EquipmentInstanceRepository.Get(instanceId);

        if (generated == null)
            return;

        EquipmentDefinitionSO definition = GameDataRegistry.Instance.GetEquipment(generated.definitionUid);

        if (definition == null)
            return;

        if (!definition.canDrop)
            return;

        list.Add(generated);
    }

    public static List<GeneratedEquipmentData> RollDroppedEquipments(CharacterData character, int dropChancePercent)
    {
        List<GeneratedEquipmentData> result = new List<GeneratedEquipmentData>();

        List<GeneratedEquipmentData> candidates = GetDroppableEquipments(character);

        foreach (GeneratedEquipmentData equipment in candidates)
        {
            if (equipment == null)
                continue;

            int roll = UnityEngine.Random.Range(0, 100);

            if (roll < dropChancePercent)
                result.Add(equipment);
        }

        return result;
    }
}