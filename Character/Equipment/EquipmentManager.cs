using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public static class EquipmentManager
{
    public static void Equip(CharacterManager manager, Equipment equipment)
    {
        CharacterData character = manager.character;

        character.RemoveAllTraits(manager); // 캐릭터의 모든 특성 적용 해제
        switch (equipment.EquipType)
        {
            case EquipmentType.Helmet:
                if (character.Helmet != null)
                    character.Helmet.Unequip(character);
                character.Helmet = equipment;
                break;

            case EquipmentType.Armor:
                if (character.Armor != null)
                    character.Armor.Unequip(character);
                character.Armor = equipment;
                break;

            case EquipmentType.Gloves:
                if (character.Gloves != null)
                    character.Gloves.Unequip(character);
                character.Gloves = equipment;
                break;

            case EquipmentType.Shoes:
                if (character.Shoes != null)
                    character.Shoes.Unequip(character);
                character.Shoes = equipment;
                break;

            case EquipmentType.Ring:
                if (character.Ring1 == null)
                {
                    character.Ring1 = equipment;
                }
                else if (character.Ring2 == null)
                {
                    character.Ring2 = equipment;
                }
                else
                {
                    character.Ring2.Unequip(character);
                    character.Ring2 = equipment;
                }
                break;

            case EquipmentType.Earring:
                if (character.Earring1 == null)
                {
                    character.Earring1 = equipment;
                }
                else if (character.Earring2 == null)
                {
                    character.Earring2 = equipment;
                }
                else
                {
                    character.Earring2.Unequip(character);
                    character.Earring2 = equipment;
                }
                break;

            case EquipmentType.Necklace:
                if (character.Necklace != null)
                    character.Necklace.Unequip(character);
                character.Necklace = equipment;
                break;

            case EquipmentType.Weapon:
                if(equipment is Weapon weapon) // Equipment타입으로 전달 받은 장비 객체를 임의로 Weapon 형식으로 캐스팅
                {
                    if (!character.CanEquipMainWeapon(weapon)) // 장착 불가능한 무기일 경우
                    {
                        Debug.Log("장착 불가능한 주무기입니다.");
                        character.ApplyAllTraits(manager);  // 캐릭터의 모든 특성 적용
                        return;
                    }
                }

                if (character.Weapon != null)
                    character.Weapon.Unequip(character);
                character.Weapon = equipment;
                break;

            case EquipmentType.SubWeapon:
                if (!character.CanEquipSubWeapon()) // 장착 불가능한 보조 무기일 경우
                {
                    Debug.Log("장착 불가능한 보조무기입니다.");
                    character.ApplyAllTraits(manager);  // 캐릭터의 모든 특성 적용
                    return;
                }

                if (character.SubWeapon != null)
                    character.SubWeapon.Unequip(character);
                character.SubWeapon = equipment;
                break;
        }

        equipment.Equip(character); // 해당 장비 착용으로 증감된 능력치 캐릭터에 적용

        // 사용 가능한 속성 및 스킬 업데이트
        UpdateAvailableAttributes(character);
        UpdateSkillAvailability(character);

        character.ApplyAllTraits(manager);  // 캐릭터의 모든 특성 적용
        character.UpdateFinalStats();

        manager.GetComponent<CharacterCustomization>().UpdateEquipmentAppearance(manager.character);
    }

    public static void Unequip(CharacterManager manager, EquipmentType equipType, int slotIndex = 1)
    {
        CharacterData character = manager.character;
        character.RemoveAllTraits(manager); // 캐릭터의 모든 특성 적용 해제

        Equipment equipment = null;

        switch (equipType)
        {
            case EquipmentType.Helmet:
                equipment = character.Helmet;
                character.Helmet = null;
                break;

            case EquipmentType.Armor:
                equipment = character.Armor;
                character.Armor = null;
                break;

            case EquipmentType.Gloves:
                equipment = character.Gloves;
                character.Gloves = null;
                break;

            case EquipmentType.Shoes:
                equipment = character.Shoes;
                character.Shoes = null;
                break;

            case EquipmentType.Ring:
                if (slotIndex == 1)
                {
                    equipment = character.Ring1;
                    character.Ring1 = null;
                }
                else
                {
                    equipment = character.Ring2;
                    character.Ring2 = null;
                }
                break;

            case EquipmentType.Earring:
                if (slotIndex == 1)
                {
                    equipment = character.Earring1;
                    character.Earring1 = null;
                }
                else
                {
                    equipment = character.Earring2;
                    character.Earring2 = null;
                }
                break;

            case EquipmentType.Necklace:
                equipment = character.Necklace;
                character.Necklace = null;
                break;

            case EquipmentType.Weapon:
                equipment = character.Weapon;
                character.Weapon = null;
                break;

            case EquipmentType.SubWeapon:
                equipment = character.SubWeapon;
                character.SubWeapon = null;
                break;
        }
        
        if (equipment != null) // 장착 해제 요청이 들어온 부위의 장비가 제대로 장착되어있을 경우
        {
            // 사용 가능한 속성 및 스킬 업데이트
            UpdateAvailableAttributes(character);
            UpdateSkillAvailability(character);

            equipment.Unequip(character); // 해당 장비로 증감된 능력치 캐릭터에 적용 해제
            character.ApplyAllTraits(manager);   // 캐릭터의 모든 특성 적용
            character.UpdateFinalStats();
        }

        manager.GetComponent<CharacterCustomization>().UpdateEquipmentAppearance(manager.character);
    }

    //장비와 스킬 관련 속성 구분 구현부

    public static void UpdateAvailableAttributes(CharacterData character)
    {
        // 캐릭터가 장착한 주무기 및 보조무기에 따라 사용 가능한 속성 업데이트
        character.AvailableAttributes.Clear();

        if (character.Weapon is Weapon mainWeapon)
        {
            character.AvailableAttributes.AddRange(mainWeapon.Attribute);
        }

        if (character.SubWeapon is Weapon subWeapon)
        {
            character.AvailableAttributes.AddRange(subWeapon.Attribute);
        }
    }

    public static void UpdateSkillAvailability(CharacterData character)
    {
        foreach (var skill in character.Skills)
        {
            if (skill.Type == SkillType.Physical && !skill.IsCounterSkill)
            {
                skill.CanUse = character.AvailableAttributes.Contains(skill.Attribute);
            }

            skill.IsOffHand = false;

            // 보조무기 사용 여부를 고려한 스킬 사용 가능 여부 설정
            if (character.Weapon is Weapon mainWeapon)
            {
                foreach(var att in mainWeapon.Attribute)
                {
                    if (!character.AvailableAttributes.Contains(att))
                    {
                        skill.IsOffHand = true;
                    }
                }
                
            }
        }
    }
}