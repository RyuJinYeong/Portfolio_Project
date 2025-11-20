using UnityEngine;


#region Positive Traits

// 철벽: 모든 방어력 +6 * Level (기존 +3의 2배)
public class DurableTrait : TraitBase
{
    public override string Name => "철벽";
    public override bool IsPercentage => false;
    public override int Id => 1000;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m)
    {
        int delta = 6 * Level;
        m.character.BaseStats.PhysicalDefense += delta;
        m.character.BaseStats.MagicalDefense += delta;
    }
    public override void RemoveTrait(CharacterManager m)
    {
        int delta = 6 * Level;
        m.character.BaseStats.PhysicalDefense -= delta;
        m.character.BaseStats.MagicalDefense -= delta;
    }
}

// 야만인의 힘: 힘 +10 * Level
public class BarbarianPowerTrait : TraitBase
{
    public override string Name => "야만인의 힘";
    public override bool IsPercentage => false;
    public override int Id => 1001;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m) { m.character.BaseStats.Strength += 10 * Level; }
    public override void RemoveTrait(CharacterManager m) { m.character.BaseStats.Strength -= 10 * Level; }
}

// 손재주: 기교 +10 * Level
public class DeftnessTrait : TraitBase
{
    public override string Name => "손재주";
    public override bool IsPercentage => false;
    public override int Id => 1002;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m) { m.character.BaseStats.Dexterity += 10 * Level; }
    public override void RemoveTrait(CharacterManager m) { m.character.BaseStats.Dexterity -= 10 * Level; }
}

// 신속한 몸놀림: 속도 +10 * Level
public class SwiftMovementTrait : TraitBase
{
    public override string Name => "신속한 몸놀림";
    public override bool IsPercentage => false;
    public override int Id => 1003;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m) { m.character.BaseStats.Speed += 10 * Level; }
    public override void RemoveTrait(CharacterManager m) { m.character.BaseStats.Speed -= 10 * Level; }
}

// 날카로운 눈썰미: 눈썰미 +10 * Level
public class KeenEyeTrait : TraitBase
{
    public override string Name => "날카로운 눈썰미";
    public override bool IsPercentage => false;
    public override int Id => 1004;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m) { m.character.BaseStats.Detection += 10 * Level; }
    public override void RemoveTrait(CharacterManager m) { m.character.BaseStats.Detection -= 10 * Level; }
}

// 예리한 통찰력: 통찰 +10 * Level
public class KeenInsightTrait : TraitBase
{
    public override string Name => "예리한 통찰력";
    public override bool IsPercentage => false;
    public override int Id => 1005;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m) { m.character.BaseStats.Insight += 10 * Level; }
    public override void RemoveTrait(CharacterManager m) { m.character.BaseStats.Insight -= 10 * Level; }
}

// 검술 숙련: (조건) 물리공격 +20 * Level
public class SwordMasteryTrait : TraitBase
{
    public override string Name => "검술 숙련";
    public override bool IsPercentage => false;
    public override int Id => 1006;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m)
    {
        if (m.character.Weapon is Weapon w && w.WeaponType == WeaponType.LongSword)
            m.character.ModifiedStats.PhysicalAttack += (uint)(20 * Level);
    }
    public override void RemoveTrait(CharacterManager m)
    {
        if (m.character.Weapon is Weapon w && w.WeaponType == WeaponType.LongSword)
            m.character.ModifiedStats.PhysicalAttack -= (uint)(20 * Level);
    }
}

// 궁술 숙련: (조건) 물리공격 +20 * Level
public class BowMasteryTrait : TraitBase
{
    public override string Name => "궁술 숙련";
    public override bool IsPercentage => false;
    public override int Id => 1007;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m)
    {
        if (m.character.Weapon is Weapon w && w.WeaponType == WeaponType.Bow)
            m.character.ModifiedStats.PhysicalAttack += (uint)(20 * Level);
    }
    public override void RemoveTrait(CharacterManager m)
    {
        if (m.character.Weapon is Weapon w && w.WeaponType == WeaponType.Bow)
            m.character.ModifiedStats.PhysicalAttack -= (uint)(20 * Level);
    }
}

// 원소 적성(불): 스킬 전제 + 저항 +10 * Level
public class FireElementalAptitudeTrait : TraitBase
{
    public override string Name => "화염 원소적성";
    public override bool IsPercentage => false;
    public override int Id => 1010;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m)
    {
        m.character.ModifiedStats.FireResistance += 10 * Level;
    }
    public override void RemoveTrait(CharacterManager m)
    {
        m.character.ModifiedStats.FireResistance -= 10 * Level;
    }
}

public class WaterElementalAptitudeTrait : TraitBase
{
    public override string Name => "물 원소적성";
    public override bool IsPercentage => false;
    public override int Id => 1011;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m) { m.character.ModifiedStats.WaterResistance += 10 * Level; }
    public override void RemoveTrait(CharacterManager m) { m.character.ModifiedStats.WaterResistance -= 10 * Level; }
}

public class EarthElementalAptitudeTrait : TraitBase
{
    public override string Name => "땅 원소적성";
    public override bool IsPercentage => false;
    public override int Id => 1012;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m) { m.character.ModifiedStats.EarthResistance += 10 * Level; }
    public override void RemoveTrait(CharacterManager m) { m.character.ModifiedStats.EarthResistance -= 10 * Level; }
}

public class WindElementalAptitudeTrait : TraitBase
{
    public override string Name => "바람 원소적성";
    public override bool IsPercentage => false;
    public override int Id => 1013;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; set { } }

    public override void ApplyTrait(CharacterManager m) { m.character.ModifiedStats.WindResistance += 10 * Level; }
    public override void RemoveTrait(CharacterManager m) { m.character.ModifiedStats.WindResistance -= 10 * Level; }
}

public class BasicElementalAptitudeTrait : TraitBase
{
    public override string Name => "기초 원소적성";
    public override bool IsPercentage => false;
    public override int Id => 1014;
    public override TraitGrade Grade => TraitGrade.C;
    public override TraitPolarity Polarity { get => TraitPolarity.Positive; }

    public override void ApplyTrait(CharacterManager manager)
    {
        TraitBase[] pool = new TraitBase[]
        {
            new FireElementalAptitudeTrait(),  // Id 1010
            new WaterElementalAptitudeTrait(), // Id 1011
            new EarthElementalAptitudeTrait(), // Id 1012
            new WindElementalAptitudeTrait()   // Id 1013
        };

        int idx = Random.Range(0, pool.Length);
        var selected = pool[idx];

        manager.character.Traits.Remove(this); // 추가할 원소 적성 임의선택 후 자신은 제거
        TraitManager.AddTrait(manager, selected);
    }

    public override void RemoveTrait(CharacterManager manager)
    {

    }
}

#endregion

#region Negative Traits
// ===================== 부정 (5000+) =====================

// 외팔: 근력을 1/(2^Level)로 축소(강한 스택 페널티) + 장착 제한 유지
public class OneArmedTrait : TraitBase
{
    public override string Name => "외팔";
    public override bool IsPercentage => true;
    public override int Id => 5000;
    public override TraitGrade Grade => TraitGrade.A;
    public override TraitPolarity Polarity { get => TraitPolarity.Negative; set { } }

    private int strDelta = 0;

    public override void ApplyTrait(CharacterManager m)
    {
        var s = m.character.BaseStats;

        // Level 미사용 정책이라도 1로 고정 방어 (레벨 0이 들어와도 반감되도록)
        int lv = Level <= 0 ? 1 : Level;
        int divisor = 1 << Mathf.Clamp(lv, 1, 10); // 2^Level

        int before = s.Strength;
        int after = Mathf.Max(0, before / divisor);

        strDelta = after - before; // 음수
        s.Strength += strDelta;    // after가 됨

        // 착용 불가 정리
        if (m.character.Weapon is Weapon w && !m.character.CanEquipMainWeapon(w))
            EquipmentManager.Unequip(m, EquipmentType.Weapon, 1, suppressTraitRecalc: true);
        if (m.character.SubWeapon != null && !m.character.CanEquipSubWeapon())
            EquipmentManager.Unequip(m, EquipmentType.SubWeapon, 1, suppressTraitRecalc: true);
    }

    public override void RemoveTrait(CharacterManager m)
    {
        m.character.BaseStats.Strength -= strDelta;
        strDelta = 0;
    }
}

// 둔재: 기교/지능/지혜를 1/(2^Level)로 축소
public class DunceTrait : TraitBase
{
    public override string Name => "둔재";
    public override bool IsPercentage => true;
    public override int Id => 5001;
    public override TraitGrade Grade => TraitGrade.A;
    public override TraitPolarity Polarity { get => TraitPolarity.Negative; set { } }

    private (int dx, int wi, int it) delta;

    public override void ApplyTrait(CharacterManager m)
    {
        var s = m.character.BaseStats;
        int divisor = 1 << Mathf.Clamp(Level, 1, 10); // 2^Level

        int ndx = Mathf.Max(0, s.Dexterity / divisor);
        int nwi = Mathf.Max(0, s.Wisdom / divisor);
        int nit = Mathf.Max(0, s.Intelligence / divisor);

        delta.dx = ndx - s.Dexterity;
        delta.wi = nwi - s.Wisdom;
        delta.it = nit - s.Intelligence;

        s.Dexterity += delta.dx;
        s.Wisdom += delta.wi;
        s.Intelligence += delta.it;
        Debug.Log($"둔재 적용: {delta}");
    }
    public override void RemoveTrait(CharacterManager m)
    {
        var s = m.character.BaseStats;
        s.Dexterity -= delta.dx;
        s.Wisdom -= delta.wi;
        s.Intelligence -= delta.it;
        delta = default;
        Debug.Log($"둔재 적용 해제: {(-delta.dx, -delta.wi, -delta.it)}");
    }
}


#endregion