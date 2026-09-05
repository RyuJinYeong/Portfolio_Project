using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Monster Role")]
public class MonsterRoleSO : ScriptableObject
{
    public int id;
    public string roleName;

    public MonsterBaseSO baseMonster;

    [Header("Visual Override")]
    [Tooltip("비워두면 Base Monster의 Model Prefab을 사용합니다.")]
    public GameObject modelPrefabOverride;

    public CharacterType characterType = CharacterType.Normal;
    public Personality personality = Personality.Aggressive;

    public CharacterStats roleStatBonus = new CharacterStats();
    public CharacterSpecialStats roleSpecialStatBonus = new CharacterSpecialStats();

    [Header("Level Growth Weight Override")]
    [Tooltip("비워두면 Base Monster의 가중치를 사용합니다.")]
    public List<StatGrowthWeightData> levelUpStatWeights = new();

    public List<int> skillUids = new();

    [Header("Default Counter Skill Pool")]
    public List<int> defaultCounterSkillUids = new() { 3000, 3001, 3002 };

    [Header("Fixed Traits")]
    public List<int> traitIds = new();

    [Header("Traits By Tier")]
    public List<TraitGenerationData> traitsByTier = new();

    [Header("Fixed Equipment")]
    public List<EquipmentSpawnData> equipments = new();

    [Header("Random Equipment Pool")]
    public List<EquipmentSpawnData> randomEquipmentPool = new();
    [Min(0)]
    public int minRandomEquipmentCount;
    [Min(0)]
    public int maxRandomEquipmentCount = 1;

    [Header("Defeat Drop")]
    [Tooltip("소환 몬스터처럼 경험치를 주지 않는 역할에 체크합니다.")]
    public bool suppressExperience;
    [Range(0, 100)]
    public int traitCaptureChance;
    [Range(0, 100)]
    public int equippedItemDropChance;

    public int levelBonus;
}
