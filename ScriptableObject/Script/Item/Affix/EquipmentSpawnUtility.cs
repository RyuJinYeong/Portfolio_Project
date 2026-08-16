using UnityEngine;

public static class EquipmentSpawnUtility
{
    public static GeneratedEquipmentData GenerateEquipment(EquipmentSpawnData spawnData)
    {
        if (spawnData == null)
            return null;

        if (spawnData.equipmentUid <= 0)
            return null;

        EquipmentDefinitionSO equipment = GameDataRegistry.Instance.GetEquipment(spawnData.equipmentUid);

        if (equipment == null)
            return null;

        EquipmentRarity rarity = RollRarity(spawnData);

        return EquipmentGenerator.Generate(equipment, rarity);
    }

    public static EquipmentRarity RollRarity(EquipmentSpawnData spawnData)
    {
        if (spawnData == null)
            return EquipmentRarity.Common;

        if (spawnData.rarityRollMode == EquipmentRarityRollMode.Fixed)
            return spawnData.fixedRarity;

        if (spawnData.rarityWeights == null || spawnData.rarityWeights.Count == 0)
            return EquipmentRarity.Common;

        int totalWeight = 0;

        foreach (EquipmentRarityWeightData weightData in spawnData.rarityWeights)
        {
            if (weightData == null)
                continue;

            if (weightData.weight <= 0)
                continue;

            totalWeight += weightData.weight;
        }

        if (totalWeight <= 0)
            return EquipmentRarity.Common;

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        foreach (EquipmentRarityWeightData weightData in spawnData.rarityWeights)
        {
            if (weightData == null || weightData.weight <= 0)
                continue;

            current += weightData.weight;

            if (roll < current)
                return weightData.rarity;
        }

        return EquipmentRarity.Common;
    }
}