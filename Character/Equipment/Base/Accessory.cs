using System.Collections.Generic;
using UnityEngine;

public abstract class Accessory : Equipment
{
    protected Accessory(string name, EquipmentType equipType, CharacterStats statModifiers)
        : base(name, equipType, statModifiers)
    {

    }
}