using System;
using System.Collections.Generic;

[Serializable]
public class SkillEquipmentRequirementData
{
    public bool requireMainHandEmpty;
    public bool requireSubHandEmpty;
    public bool requireAnyHandEmpty;

    public bool requireShield;

    public List<WeaponType> allowedMainWeaponTypes = new();
    public List<WeaponType> allowedSubWeaponTypes = new();
    public List<WeaponType> allowedAnyWeaponTypes = new();

    public bool HasAnyRequirement()
    {
        return requireMainHandEmpty ||
               requireSubHandEmpty ||
               requireAnyHandEmpty ||
               requireShield ||
               allowedMainWeaponTypes.Count > 0 ||
               allowedSubWeaponTypes.Count > 0 ||
               allowedAnyWeaponTypes.Count > 0;
    }
}