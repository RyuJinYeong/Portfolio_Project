using System;
using System.Collections.Generic;

[Serializable]
public class EquipmentSpawnData
{
    public int equipmentUid;

    public EquipmentRarityRollMode rarityRollMode = EquipmentRarityRollMode.Fixed;
    public EquipmentRarity fixedRarity = EquipmentRarity.Common;

    public List<EquipmentRarityWeightData> rarityWeights = new();

    public bool equipOnSpawn = true;
}

[Serializable]
public class EquipmentSpawnSlotSet
{
    public EquipmentSpawnData helmet = new EquipmentSpawnData();
    public EquipmentSpawnData armor = new EquipmentSpawnData();
    public EquipmentSpawnData gloves = new EquipmentSpawnData();
    public EquipmentSpawnData shoes = new EquipmentSpawnData();

    public EquipmentSpawnData ring1 = new EquipmentSpawnData();
    public EquipmentSpawnData ring2 = new EquipmentSpawnData();
    public EquipmentSpawnData necklace = new EquipmentSpawnData();

    public EquipmentSpawnData weapon = new EquipmentSpawnData();
    public EquipmentSpawnData subWeapon = new EquipmentSpawnData();
}