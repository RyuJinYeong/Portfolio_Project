using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Monster Base")]
public class MonsterBaseSO : ScriptableObject
{
    public int id;
    public string monsterName;

    [TextArea]
    public string description;

    public CharacterStats baseStats = new CharacterStats();
    public CharacterSpecialStats baseSpecialStats = new CharacterSpecialStats();

    public List<int> baseTraitIds = new();
}