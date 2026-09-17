using System.Collections.Generic;

public class EquipmentRuntimeData
{
    public EquipmentDefinitionSO definition;
    public GeneratedEquipmentData generated;

    public int uid;
    public string instanceId;

    public string displayName;
    public int tier;
    public EquipmentRarity rarity;

    public EquipmentType equipType;

    public CharacterStats statModifiers = new CharacterStats();
    public CharacterSpecialStats specialStatModifiers = new CharacterSpecialStats();

    public List<int> grantedTraitIds = new();

    public string visualKey;
}