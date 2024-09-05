using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MagicSword : Weapon, IHasSkill // 스킬 달린 장비 견본 코드
{
    public List<SkillBase> Skills { get; set; }    

    public MagicSword(WeaponCategory category, List<WeaponTag> tags, List<SkillAttribute> attributes, CharacterStats statModifiers)
        : base("스킬예시용무기", category, tags,WeaponType.Sword, attributes, statModifiers)
    {
        Skills[0] = new FireballSkill();
    }


    public override void Equip(CharacterData character)
    {
        base.Equip(character);
        foreach (SkillBase skill in Skills) // foreach로 List속 객체를 하나씩 추출해 캐릭터에 추가
        {
            if (!character.Skills.Contains(skill)) // 이미 캐릭터에 존재하는 스킬일 경우 추가X
            {
                character.EquipmentSkills.Add(skill);
            }
        }
    }

    public override void Unequip(CharacterData character)
    {
        base.Unequip(character);
        foreach (SkillBase skill in Skills) // foreach로 List속 객체를 하나씩 추출해 캐릭터에 추가, 중복 객체 식별 필요
        {
            if (character.EquipmentSkills.Contains(skill) && character.IsSkillUnique(skill, this)) // 다른 장비와 중복시 제거x
            {
                character.EquipmentSkills.Remove(skill);
            }
        }
    }
}

public class EnchantedArmor : Armor, IHasTrait // 특성 달린 장비 견본 코드
{
    public List<TraitBase> Traits { get; set; }

    public EnchantedArmor(ArmorCategory armorCategory, CharacterStats statModifiers)
        : base("특성예시용방어구", armorCategory, EquipmentType.Armor, statModifiers)
    {
        Traits[0] = new DurableTrait();
    }

    public override void Equip(CharacterData character)
    {
        base.Equip(character);
        foreach (TraitBase trait in Traits) // foreach로 List속 객체를 하나씩 추출해 캐릭터에 추가
        {
            if (!character.Traits.Contains(trait)) // 이미 존재하는 특성일 경우 추가 X
            {
                character.EquipmentTraits.Add(trait);
            }
        }
    }

    public override void Unequip(CharacterData character)
    {
        base.Unequip(character);
        foreach (TraitBase trait in Traits) // foreach로 List속 객체를 하나씩 추출해 캐릭터에 추가, 중복 객체 식별 필요
        {
            if (character.EquipmentTraits.Contains(trait) && character.IsTraitUnique(trait, this))  // 다른 장비와 중복시 제거x
            {
                character.EquipmentTraits.Remove(trait);
            }
        }
    }
}
