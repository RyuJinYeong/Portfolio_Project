using System;
using System.Collections.Generic;

[Serializable]
public class EquipmentAffixRollData
{
    public int affixId;
    public EquipmentAffixType affixType;
    public string affixName;

    public CharacterStats rolledStatModifiers = new CharacterStats();
    public CharacterSpecialStats rolledSpecialStatModifiers = new CharacterSpecialStats();

    public List<int> grantedTraitIds = new();
}