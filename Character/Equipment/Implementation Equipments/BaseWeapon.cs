//±âº» ¹«±â
using System.Collections.Generic;
using System.Linq;

public class OldSword : Weapon
{
    public OldSword()
        : base("³°Àº Àå°Ë", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.OffhandWeapon },WeaponType.Sword, new List<SkillAttribute> { SkillAttribute.Slash, SkillAttribute.Pierce }, new CharacterStats { PhysicalAttack = 15, WeaponAttackSpeedMultiplier = 1.0})
    {
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

public class RoundShield: Weapon
{
    public RoundShield()
        : base("¶ó¿îµå½Çµå", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.OffhandWeapon }, WeaponType.Shiled, new List<SkillAttribute> { SkillAttribute.Smash }, new CharacterStats { PhysicalAttack = 1, PhysicalDefense = 1, WeaponAttackSpeedMultiplier = 1.0 })
    {

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

public class Dagger : Weapon
{
    public Dagger()
        : base("´Ü°Ë", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.OffhandWeapon }, WeaponType.Sword, new List<SkillAttribute> { SkillAttribute.Slash, SkillAttribute.Pierce }, new CharacterStats { PhysicalAttack = 5, WeaponAttackSpeedMultiplier = 1.2 })
    {

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

public class Quarterstaff : Weapon
{
    public Quarterstaff()
        : base("ÄõÅÍ½ºÅÂÇÁ", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.MagicWeapon }, WeaponType.Staff ,new List<SkillAttribute> { SkillAttribute.Smash, SkillAttribute.Pierce, SkillAttribute.Magic }, new CharacterStats { PhysicalAttack = 10, MagicalAttack = 10, WeaponAttackSpeedMultiplier = 1.0, WeaponCastSpeedMultiplier = 1.0 })
    {

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

public class Shortbow : Weapon
{
    public Shortbow()
        : base("´Ü±Ã", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.TwoHanded },WeaponType.Bow, new List<SkillAttribute> { SkillAttribute.Smash, SkillAttribute.Pierce }, new CharacterStats { PhysicalAttack = 10, WeaponAttackSpeedMultiplier = 1.2 })
    {

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

public class RustySledgeHammer : Weapon
{
    public RustySledgeHammer()
    : base("³ì½¼ ´ëÇü ¸ÁÄ¡", WeaponCategory.HeavyWeapon, new List<WeaponTag> { WeaponTag.TwoHanded },WeaponType.BluntWeapon, new List<SkillAttribute> { SkillAttribute.Smash }, new CharacterStats { PhysicalAttack = 20, WeaponAttackSpeedMultiplier = 0.6 })
    {

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

