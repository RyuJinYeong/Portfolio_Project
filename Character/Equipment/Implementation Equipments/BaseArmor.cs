//±âº» ¹æ¾î±¸
[System.Serializable]
public class LeatherArmor: Armor
{

    public LeatherArmor()
        : base("°¡Á×°©¿Ê", ArmorCategory.LightArmor, EquipmentType.Armor, new CharacterStats { PhysicalDefense = 3, MagicalDefense = 3 })
    {
    }

    public override void Equip(CharacterData character)
    {
        character.ModifiedStats += StatModifiers;
    }

    public override void Unequip(CharacterData character)
    {
        character.ModifiedStats -= StatModifiers;
    }
}
[System.Serializable]
public class LeatherGloves : Armor
{

    public LeatherGloves()
        : base("°¡Á×Àå°©", ArmorCategory.LightArmor, EquipmentType.Armor, new CharacterStats { PhysicalDefense = 1, MagicalDefense = 1 })
    {
    }

    public override void Equip(CharacterData character)
    {
        character.ModifiedStats += StatModifiers;
    }

    public override void Unequip(CharacterData character)
    {
        character.ModifiedStats -= StatModifiers;
    }
}
[System.Serializable]
public class LeatherBoots : Armor
{

    public LeatherBoots()
        : base("°¡Á×ºÎÃ÷", ArmorCategory.LightArmor, EquipmentType.Armor, new CharacterStats { PhysicalDefense = 1, MagicalDefense = 1 })
    {
    }

    public override void Equip(CharacterData character)
    {
        character.ModifiedStats += StatModifiers;
    }

    public override void Unequip(CharacterData character)
    {
        character.ModifiedStats -= StatModifiers;
    }
}