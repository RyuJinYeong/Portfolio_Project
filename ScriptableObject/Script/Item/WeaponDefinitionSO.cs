using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Equipment/Weapon")]
public class WeaponDefinitionSO : EquipmentDefinitionSO
{
    public WeaponCategory weaponCategory;
    public WeaponType weaponType;

    public List<WeaponTag> weaponTags = new();
    public List<SkillAttribute> attributes = new();
}
