using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Armor : Equipment
{
    public ArmorCategory ArmorCategory { get; private set; } // Áß°©, °æ°©, ÀÇº¹ ¿©ºÎ ±¸ºÐ.

    public Armor(string name, ArmorCategory armorCategory, EquipmentType equipType, CharacterStats statModifiers)
        : base(name, equipType, statModifiers)
    {
        ArmorCategory = armorCategory;
        Price = 100;
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