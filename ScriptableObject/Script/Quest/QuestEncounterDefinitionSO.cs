using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestEncounterEffectType
{
    None,
    RandomPermanentStat,
    RecoverResources,
    TemporaryStatModifier,
    RandomEquipment,
    StartBattle,
    RandomRegisteredItem
}

public enum QuestEncounterTargetScope
{
    None,
    SelectedCharacter,
    WholeParty
}

public enum QuestEncounterCheckStat
{
    None,
    Strength,
    Dexterity,
    Speed,
    Intelligence,
    Wisdom,
    Health,
    Vitality,
    Endurance,
    Detection,
    Insight,
    PhysicalAttack,
    MagicalAttack,
    HigherPhysicalOrMagicalAttack
}

[Serializable]
public class QuestEncounterInsightData
{
    public bool enabled;
    [Range(0f, 100f)] public float baseChancePercent = 5f;
    [Min(0f)] public float chancePerDetection = 0.5f;
    [Min(0f)] public float chancePerInsight = 0.5f;
    [Min(0f)] public float tierPenaltyPercent = 3f;
    [Min(0f)] public float difficultyPenaltyPercent = 5f;
    [Range(0f, 100f)] public float maxChancePercent = 35f;

    [TextArea]
    public string revealedText;
}

[Serializable]
public class QuestEncounterAbilityCheckData
{
    public bool enabled;
    public QuestEncounterCheckStat stat;
    [Range(0f, 100f)] public float baseSuccessChancePercent = 50f;
    [Min(0f)] public float chancePerStatPoint = 2f;
    [Min(0f)] public float tierPenaltyPercent = 5f;
    [Min(0f)] public float difficultyPenaltyPercent = 10f;
    [Range(0f, 100f)] public float minimumSuccessChancePercent = 5f;
    [Range(0f, 100f)] public float maximumSuccessChancePercent = 95f;
}

[Serializable]
public class QuestEncounterOutcomeData
{
    [Min(1)] public int weight = 1;

    [TextArea]
    public string resultText;

    public QuestEncounterEffectType effectType;
    public QuestEncounterTargetScope targetScope;

    [Header("Permanent Stat Change")]
    public List<StatRequirementType> candidateStats = new();
    public int minAmount;
    public int maxAmount;

    [Header("Temporary Expedition Effect")]
    public string temporaryEffectName;
    [Min(1)] public int durationRooms = 1;
    public CharacterStats temporaryStatModifiers = new();

    [Header("Equipment Reward")]
    [Min(1)] public int equipmentTier = 1;
    public EquipmentRarity maxEquipmentRarity = EquipmentRarity.Common;

    [Header("Registered Item Reward")]
    public List<int> itemUids = new();
    [Min(1)] public int itemCount = 1;

    [Header("Battle")]
    public List<int> monsterRoleIds = new();
}

[Serializable]
public class QuestEncounterChoiceData
{
    public string text;
    public bool requiresCharacter;
    [Range(0f, 100f)] public float randomSkillChancePercent;
    public QuestEncounterAbilityCheckData abilityCheck = new();
    public List<QuestEncounterOutcomeData> outcomes = new();
    public List<QuestEncounterOutcomeData> failureOutcomes = new();
}

[CreateAssetMenu(menuName = "GameData/Quest Encounter")]
public class QuestEncounterDefinitionSO : ScriptableObject
{
    public string encounterId;
    public QuestRouteNodeType nodeType = QuestRouteNodeType.RandomEncounter;
    [Min(1)] public int weight = 1;

    [Header("Stage Availability")]
    public bool availableInAllStages;
    public List<string> stageKeys = new();

    [Header("Dialogue")]
    public string title;

    [TextArea(3, 8)]
    public string description;

    [Header("3D Presentation")]
    [Tooltip("특정 스테이지용 인카운트에서 사용할 전용 배경 맵 프리팹입니다.")]
    public GameObject encounterMapPrefab;

    [Tooltip("인카운트 배경 위에 배치할 상호작용 소품 프리팹입니다.")]
    public GameObject encounterPropsPrefab;

    [Header("Detection / Insight Reveal")]
    public QuestEncounterInsightData insight = new();

    public List<QuestEncounterChoiceData> choices = new();

    public bool CanAppear(string stageKey, QuestRouteNodeType routeNodeType)
    {
        if (nodeType != routeNodeType || weight <= 0)
            return false;

        if (availableInAllStages)
            return true;

        return !string.IsNullOrEmpty(stageKey) &&
               stageKeys != null &&
               stageKeys.Contains(stageKey);
    }
}
