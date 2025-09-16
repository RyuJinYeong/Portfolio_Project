using Newtonsoft.Json;
using System;
using UnityEngine;

// 특성은 BaseStat에 영향을 주도록 해서 구현

public class DurableTrait : TraitBase // 추상클래스 TraitBase를 상속받아 구현한 상세 특성 정의 클래스
{
    public override string Name => "철벽"; // 특성명
    public override bool IsPercentage => false;
    public override int Id => 1000;
    public override TraitGrade Grade => TraitGrade.C;

    public override int Level => 1;
    public override TraitPolarity Polarity => TraitPolarity.Positive;

    public override void ApplyTrait(CharacterManager manager) // 모든 방어력 3 증가
    {
        manager.character.BaseStats.PhysicalDefense += 3;
        manager.character.BaseStats.MagicalDefense += 3;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.BaseStats.PhysicalDefense -= 3;
        manager.character.BaseStats.MagicalDefense -= 3;
    }
}

public class SwordMasteryTrait : TraitBase
{
    public override string Name => "검술 숙련";
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager)
    {
        if (manager.character.Weapon is Weapon weapon && weapon.WeaponType == WeaponType.LongSword) // 주무기로 도검류 장착시 해당 장비 공격력 10 증가
        {
            manager.character.ModifiedStats.PhysicalAttack += 10; 
        }
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        if (manager.character.Weapon is Weapon weapon && weapon.WeaponType == WeaponType.LongSword) 
        {
            manager.character.ModifiedStats.PhysicalAttack -= 10;
        }
    }
}


public class BarbarianPowerTrait : TraitBase 
{
    public override string Name => "야만인의 힘"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager) // 근력 5 증가
    {
        manager.character.BaseStats.Strength += 5;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.BaseStats.Strength -= 5;
    }
}

public class BowMasteryTrait : TraitBase
{
    public override string Name => "궁술 숙련"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager)
    {
        if (manager.character.Weapon is Weapon weapon && weapon.WeaponType == WeaponType.Bow) // 주무기로 활 장착시 해당 장비 공격력 10증가
        {
            manager.character.ModifiedStats.PhysicalAttack += 10;
        }
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        if (manager.character.Weapon is Weapon weapon && weapon.WeaponType == WeaponType.Bow) 
        {
            manager.character.ModifiedStats.PhysicalAttack -= 10;
        }
    }
}

public class DeftnessTrait : TraitBase
{
    public override string Name => "손재주"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager) // 기교 5 증가
    {
        manager.character.BaseStats.Dexterity += 5;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.BaseStats.Dexterity -= 5;
    }
}

public class BasicElementalAptitudeTrait : TraitBase
{
    public override string Name => "기초 원소적성"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager)
    {
        // 랜덤하게 원소적성 선택
        TraitBase[] elementalTraits = new TraitBase[]
        {
            new FireElementalAptitudeTrait(),
            new WaterElementalAptitudeTrait(),
            new EarthElementalAptitudeTrait(),
            new WindElementalAptitudeTrait()
        };

        int randomIndex = UnityEngine.Random.Range(0, elementalTraits.Length);
        TraitBase selectedTrait = elementalTraits[randomIndex];

        // 기초원소적성 제거
        manager.character.Traits.Remove(this);

        // 선택한 원소적성 부여
        TraitManager.AddTrait(manager,selectedTrait);
    }

    public override void RemoveTrait(CharacterManager manager)
    {

    }
}

public class FireElementalAptitudeTrait : TraitBase
{
    public override string Name => "화염 원소적성"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager)
    {
        manager.character.ModifiedStats.FireResistance += 5;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.ModifiedStats.FireResistance -= 5;
    }
}

public class WaterElementalAptitudeTrait : TraitBase
{
    public override string Name => "물 원소적성"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager) // 
    {
        manager.character.ModifiedStats.WaterResistance += 5;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.ModifiedStats.WaterResistance -= 5;
    }
}

public class EarthElementalAptitudeTrait : TraitBase
{
    public override string Name => "땅 원소적성"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager) // 
    {
        manager.character.ModifiedStats.EarthResistance += 5;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.ModifiedStats.EarthResistance -= 5;
    }
}

public class WindElementalAptitudeTrait : TraitBase
{
    public override string Name => "바람 원소적성"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager) // 
    {
        manager.character.ModifiedStats.WindResistance += 5;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.ModifiedStats.WindResistance -= 5;
    }
}

public class OneArmedTrait : TraitBase
{
    public override string Name => "외팔"; // 특성명
    public override bool IsPercentage => true;

    public override void ApplyTrait(CharacterManager manager) // 기본 근력 반감, 양손무기 및 보조무기 착용 불가능 +20
    {
        manager.character.BaseStats.Strength /= 2;

        if (manager.character.Weapon is Weapon weapon)
        {
            if (!manager.character.CanEquipMainWeapon(weapon))
            {
                EquipmentManager.Unequip(manager, EquipmentType.Weapon);
            }
        }

        if (!manager.character.CanEquipSubWeapon())
        {
            EquipmentManager.Unequip(manager, EquipmentType.SubWeapon);
        }
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.BaseStats.Strength *= 2;
    }
}

public class DunceTrait : TraitBase
{
    public override string Name => "둔재"; // 특성명
    public override bool IsPercentage => true;

    public override void ApplyTrait(CharacterManager manager) // 기본 기교, 지능, 지혜 스탯 반감
    {
        manager.character.BaseStats.Dexterity /= 2;
        manager.character.BaseStats.Wisdom /= 2;
        manager.character.BaseStats.Intelligence /= 2;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.BaseStats.Dexterity *= 2;
        manager.character.BaseStats.Wisdom *= 2;
        manager.character.BaseStats.Intelligence *= 2;
    }
}

public class KeenEyeTrait : TraitBase
{
    public override string Name => "날카로운 눈썰미"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager) // 기본 눈썰미 +5 (-5)
    {
        manager.character.BaseStats.Detection += 5;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.BaseStats.Detection -= 5;
    }
}

public class KeenInsightTrait : TraitBase
{
    public override string Name => "예리한 통찰력"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager) // 기본 통찰력 +5 (-5)    
    {
        manager.character.BaseStats.Insight += 5;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.BaseStats.Insight -= 5;
    }
}

public class SwiftMovementTrait : TraitBase
{
    public override string Name => "신속한 몸놀림"; // 특성명
    public override bool IsPercentage => false;

    public override void ApplyTrait(CharacterManager manager) // 기본 속도 +5 (-5)
    {
        manager.character.BaseStats.Speed += 5;
    }

    public override void RemoveTrait(CharacterManager manager)
    {
        manager.character.BaseStats.Speed -= 5;
    }
}