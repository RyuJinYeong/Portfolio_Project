using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Equipment : Root // 장착 가능한 장비에 대한 추상 클래스
{
    public EquipmentType EquipType { get; protected set; } // 장비 장착 위치    
    public CharacterStats StatModifiers { get; protected set; } // 해당 부위 장비로 증가한 스탯량
    public int Price { get; set; }
    public bool IsStackable { get; protected set; }

    // 기본 생성자
    protected Equipment(string name, CharacterStats statModifiers)
    {
        Name = name;
        StatModifiers = statModifiers;
    }

    //장비 장착 위치를 받는 생성자
    protected Equipment(string name, EquipmentType equipType, CharacterStats statModifiers)
    {
        Name = name;
        EquipType = equipType;
        IsStackable = false;
        StatModifiers = statModifiers;
        ObjectType = ObjectType.Equipment;
    }

    public abstract void Equip(CharacterData character);
    public abstract void Unequip(CharacterData character);
}