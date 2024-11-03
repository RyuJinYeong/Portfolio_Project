using SoftKitty.InventoryEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Armor : Equipment
{
    public ArmorCategory ArmorCategory { get; set; } // 중갑, 경갑, 의복 여부 구분.

    public Armor(string name, ArmorCategory armorCategory, EquipmentType equipType, CharacterStats statModifiers)
        : base(name, equipType, statModifiers)
    {
        ArmorCategory = armorCategory;
    }

    public Armor(){}

    public override void Equip(CharacterData character)
    {
        character.ModifiedStats += StatModifiers;
    }

    public override void Unequip(CharacterData character)
    {
        character.ModifiedStats -= StatModifiers;
    }


    public override Item Copy()
    {
        Armor _newItem = new Armor(this.name, this.ArmorCategory, this.EquipType, this.StatModifiers.Copy());

        // 부모 클래스의 필드를 복사 (가급적이면 직접 필드를 복사)
        _newItem.uid = uid;
        _newItem.name = name;
        _newItem.description = description;
        _newItem.type = type;
        _newItem.icon = icon;
        _newItem.quality = quality;
        _newItem.tradeable = tradeable;
        _newItem.deletable = deletable;
        _newItem.useable = useable;
        _newItem.consumable = consumable;
        _newItem.price = price;
        _newItem.currency = currency;
        _newItem.maxiumStack = maxiumStack;
        _newItem.upgradeLevel = upgradeLevel;
        _newItem.weight = weight;
        _newItem.dropRates = dropRates;
        _newItem.favorite = favorite;
        _newItem.attributes.Clear();
        foreach (var att in attributes)
        {
            _newItem.attributes.Add(att.Copy());
        }
        _newItem.craftMaterials.Clear();
        _newItem.craftMaterials.AddRange(craftMaterials);
        _newItem.enchantments.Clear();
        _newItem.enchantments.AddRange(enchantments);
        _newItem.actions.Clear();
        _newItem.actions.AddRange(actions);
        _newItem.tags.Clear();
        _newItem.tags.AddRange(tags);
        _newItem.fold = true;

        return _newItem;
    }
}