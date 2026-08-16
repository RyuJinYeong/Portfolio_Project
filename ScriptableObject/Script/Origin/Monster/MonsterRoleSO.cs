using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Monster Role")]
public class MonsterRoleSO : ScriptableObject
{
    public int id;
    public string roleName;

    public MonsterBaseSO baseMonster;

    public CharacterType characterType = CharacterType.Normal;
    public Personality personality = Personality.Aggressive;

    public CharacterStats roleStatBonus = new CharacterStats();
    public CharacterSpecialStats roleSpecialStatBonus = new CharacterSpecialStats();

    public List<int> skillUids = new();
    public List<int> traitIds = new();

    public List<EquipmentSpawnData> equipments = new();

    public int levelBonus;
}