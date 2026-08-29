using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Monster Base")]
public class MonsterBaseSO : ScriptableObject
{
    public int id;
    public string monsterName;

    [TextArea]
    public string description;

    [Header("Visual")]
    public GameObject modelPrefab;

    [Header("Stage Tags")]
    public List<string> tags = new();

    [Header("Base Stats")]
    public CharacterStats baseStats = new CharacterStats();
    public CharacterSpecialStats baseSpecialStats = new CharacterSpecialStats();

    [Header("Level Growth Weights")]
    public List<StatGrowthWeightData> levelUpStatWeights = new();

    [Header("Base Traits")]
    public List<int> baseTraitIds = new();
}
