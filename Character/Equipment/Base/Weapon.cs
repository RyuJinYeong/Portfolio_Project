using SoftKitty.InventoryEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : Equipment
{
    public WeaponCategory WeaponCategory { get; set; } // 무기 유형 지정 - 중량, 경량무기 구분
    public List<WeaponTag> WeaponTags { get; set; } // 무기 태그 지정 - 양손 무기 여부, 보조무기 여부, 마법 무기 여부    
    public List<SkillAttribute> Attribute { get; set; } // 세부 속성 - 공격 특화 속성 지정, 해당 속성에 맞는 공격시 보너스 데미지 추가
    public WeaponType WeaponType { get; set; } // 무기 타입 - 둔기, 도검류 등등..

    public Weapon(string name, WeaponCategory weaponCategory, List<WeaponTag> Tag, WeaponType weatype, List<SkillAttribute> attribute, CharacterStats statModifiers)
        : base(name, EquipmentType.Weapon, statModifiers)
    {
        WeaponType = weatype;
        WeaponCategory = weaponCategory;
        WeaponTags = Tag; // tags가 null일 경우 새로운 빈 리스트로 초기화
        Attribute = attribute;
    }

    public Weapon(){}

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
        Weapon _newItem = new Weapon(this.name, this.WeaponCategory, new List<WeaponTag>(this.WeaponTags), this.WeaponType, new List<SkillAttribute>(this.Attribute), this.StatModifiers.Copy());

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