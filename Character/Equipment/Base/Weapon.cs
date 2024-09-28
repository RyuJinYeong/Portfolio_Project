using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : Equipment
{
    public WeaponCategory WeaponCategory { get; } // 무기 유형 지정 - 중량, 경량무기 구분
    public List<WeaponTag> Tags { get; } // 무기 태그 지정 - 양손 무기 여부, 보조무기 여부, 마법 무기 여부    
    public List<SkillAttribute> Attribute { get; protected set; } // 세부 속성 - 공격 특화 속성 지정, 해당 속성에 맞는 공격시 보너스 데미지 추가
    public WeaponType WeaponType { get;} // 무기 타입 - 둔기, 도검류 등등..

    public Weapon(string name, WeaponCategory weaponCategory, List<WeaponTag> tags, WeaponType type, List<SkillAttribute> attribute, CharacterStats statModifiers)
        : base(name, EquipmentType.Weapon, statModifiers)
    {
        WeaponType = type;
        WeaponCategory = weaponCategory;
        Tags = tags;
        Attribute = attribute;
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