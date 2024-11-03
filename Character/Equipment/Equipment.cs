using SoftKitty.InventoryEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Equipment : Item // 장착 가능한 장비에 대한 추상 클래스
{
    public EquipmentType EquipType { get; set; } // 장비 장착 위치    
    public CharacterStats StatModifiers { get; set; } // 해당 부위 장비로 증가한 스탯량

    // 기본 생성자
    public Equipment(string name, CharacterStats statModifiers, Item baseitem)
    {
        type = 1;
        base.name = name;
        StatModifiers = statModifiers;
    }

    //장비 장착 위치를 받는 생성자
    public Equipment(string name, EquipmentType equipType, CharacterStats statModifiers)
    {
        base.name = name;
        EquipType = equipType;
        StatModifiers = statModifiers;
    }

    public Equipment()
    {

    }

    public virtual void Equip(CharacterData character){}
    public virtual void Unequip(CharacterData character){}

    //Equipment에서는 Item.Copy 가상 메서드를 재정의 X
}