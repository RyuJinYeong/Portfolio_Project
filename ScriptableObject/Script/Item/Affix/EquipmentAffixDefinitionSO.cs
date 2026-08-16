using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Equipment Affix")]
public class EquipmentAffixDefinitionSO : ScriptableObject
{
    public int id;

    [Header("Name")]
    public string affixName;

    [Header("Type")]
    public EquipmentAffixType affixType;
    public EquipmentRarity minRarity = EquipmentRarity.Uncommon;

    [Header("Allowed Equipment")]
    public List<EquipmentType> allowedEquipTypes = new();
    public List<WeaponType> allowedWeaponTypes = new();

    [Header("Tier 1 Stat Range")]
    public CharacterStats minStatModifiers = new CharacterStats();
    public CharacterStats maxStatModifiers = new CharacterStats();

    public CharacterSpecialStats minSpecialStatModifiers = new CharacterSpecialStats();
    public CharacterSpecialStats maxSpecialStatModifiers = new CharacterSpecialStats();

    [Header("Special Trait")]
    public List<int> grantedTraitIds = new();

    [Header("Random Weight")]
    public int weight = 100;

    public bool CanApplyTo(EquipmentDefinitionSO equipment, EquipmentRarity rarity)
    {
        if (equipment == null)
            return false;

        if (rarity < minRarity)
            return false;

        if (allowedEquipTypes != null && allowedEquipTypes.Count > 0)
        {
            if (!allowedEquipTypes.Contains(equipment.equipType))
                return false;
        }

        if (equipment is WeaponDefinitionSO weapon)
        {
            if (allowedWeaponTypes != null && allowedWeaponTypes.Count > 0)
            {
                if (!allowedWeaponTypes.Contains(weapon.weaponType))
                    return false;
            }
        }

        return true;
    }
}