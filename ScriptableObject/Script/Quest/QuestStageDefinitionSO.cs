using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Quest Stage")]
public class QuestStageDefinitionSO : ScriptableObject
{
    public string stageKey;
    public string stageName;

    [TextArea]
    public string description;

    [Header("Map Prefabs")]
    public List<GameObject> mapPrefabs = new();

    [Header("Monster Tags")]
    public List<string> monsterTags = new();

    [Header("Stage Status Effects")]
    public List<int> statusEffectIds = new();
}
