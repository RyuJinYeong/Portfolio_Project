using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Mercenary Definition")]
public class MercenaryDefinitionSO : ScriptableObject
{
    public int id;
    public string templateName;

    public OriginDefinitionSO baseOrigin;

    [Header("Fixed Additions")]
    public List<int> fixedTraitIds = new();
    public List<int> fixedSkillUids = new();
    public List<EquipmentSpawnData> fixedEquipments = new();

    [Header("Random Trait")]
    public List<int> randomTraitPool = new();
    public int randomTraitCount;

    [Header("Random Skill")]
    public List<int> randomSkillPool = new();
    public int randomSkillCount;

    [Header("Random Equipment")]
    public List<EquipmentSpawnData> randomEquipmentPool = new();
    public int randomEquipmentCount;

    [Header("Level")]
    public int minLevelOffset;
    public int maxLevelOffset = 2;
}