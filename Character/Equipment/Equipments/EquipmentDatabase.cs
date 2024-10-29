using SoftKitty.InventoryEngine;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public static class EquipmentDatabase
{
    public static void InitializeDatabase()
    {
        List<SoftKitty.InventoryEngine.Item> equipmentList = new List<SoftKitty.InventoryEngine.Item>(); // 지역 변수로 아이템 리스트 생성

        // 기본 방어구 추가
        Armor leatherArmor = new Armor("Leather Armor", ArmorCategory.LightArmor, EquipmentType.Armor, new CharacterStats { PhysicalDefense = 5, MagicalDefense = 3 });
        leatherArmor.uid = 1001;
        leatherArmor.description = "A simple leather armor offering basic protection.";
        leatherArmor.icon = Resources.Load<Texture2D>("Icons/LeatherArmor");
        leatherArmor.price = 150;
        leatherArmor.weight = 2.5f;
        leatherArmor.actions.Add("equip");
        leatherArmor.tags.Add("Torso");
        equipmentList.Add(leatherArmor);

        Armor leatherGloves = new Armor("Leather Gloves", ArmorCategory.LightArmor, EquipmentType.Gloves, new CharacterStats { PhysicalDefense = 1, MagicalDefense = 1 });
        leatherGloves.uid = 1002;
        leatherGloves.description = "Leather gloves providing minimal protection.";
        leatherGloves.icon = Resources.Load<Texture2D>("Icons/LeatherGloves");
        leatherGloves.price = 50;
        leatherGloves.weight = 0.5f;
        leatherGloves.actions.Add("equip");
        leatherGloves.tags.Add("Gauntlet");
        equipmentList.Add(leatherGloves);

        Armor leatherBoots = new Armor("Leather Boots", ArmorCategory.LightArmor, EquipmentType.Shoes, new CharacterStats { PhysicalDefense = 1, MagicalDefense = 1 });
        leatherBoots.uid = 1003;
        leatherBoots.description = "Light leather boots for basic foot protection.";
        leatherBoots.icon = Resources.Load<Texture2D>("Icons/LeatherBoots");
        leatherBoots.price = 70;
        leatherBoots.weight = 0.8f;
        leatherBoots.actions.Add("equip");
        leatherBoots.tags.Add("Boots");
        equipmentList.Add(leatherBoots);

        // 기본 무기들 추가
        Weapon oldSword = new Weapon("Old Sword", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.OffhandWeapon }, WeaponType.LongSword, new List<SkillAttribute> { SkillAttribute.Slash, SkillAttribute.Pierce }, new CharacterStats { PhysicalAttack = 15, WeaponAttackSpeedMultiplier = 1.0f });
        oldSword.uid = 1004;
        oldSword.description = "An old sword with low attack power.";
        oldSword.icon = Resources.Load<Texture2D>("Icons/OldSword");
        oldSword.price = 100;
        oldSword.weight = 3.0f;
        oldSword.actions.Add("equip");
        oldSword.tags.Add("MainHand");
        equipmentList.Add(oldSword);

        Weapon roundShield = new Weapon("Round Shield", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.OffhandWeapon }, WeaponType.Shield, new List<SkillAttribute> { SkillAttribute.Smash }, new CharacterStats { PhysicalAttack = 1, PhysicalDefense = 1, WeaponAttackSpeedMultiplier = 1.0f });
        roundShield.uid = 1005;
        roundShield.description = "A round shield for basic defense.";
        roundShield.icon = Resources.Load<Texture2D>("Icons/RoundShield");
        roundShield.price = 120;
        roundShield.weight = 4.0f;
        roundShield.actions.Add("equip");
        roundShield.tags.Add("OffHand");
        equipmentList.Add(roundShield);

        Weapon dagger = new Weapon("Dagger", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.OffhandWeapon }, WeaponType.Dagger, new List<SkillAttribute> { SkillAttribute.Slash, SkillAttribute.Pierce }, new CharacterStats { PhysicalAttack = 5, WeaponAttackSpeedMultiplier = 1.2f });
        dagger.uid = 1006;
        dagger.description = "A small dagger suitable for quick attacks.";
        dagger.icon = Resources.Load<Texture2D>("Icons/Dagger");
        dagger.price = 80;
        dagger.weight = 1.0f;
        dagger.actions.Add("equip");
        dagger.tags.Add("MainHand");
        equipmentList.Add(dagger);

        Weapon quarterstaff = new Weapon("Quarterstaff", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.MagicWeapon }, WeaponType.Staff, new List<SkillAttribute> { SkillAttribute.Smash, SkillAttribute.Magic }, new CharacterStats { PhysicalAttack = 10, MagicalAttack = 10, WeaponAttackSpeedMultiplier = 1.0f, WeaponCastSpeedMultiplier = 1.0f });
        quarterstaff.uid = 1007;
        quarterstaff.description = "A staff often used by spellcasters.";
        quarterstaff.icon = Resources.Load<Texture2D>("Icons/Quarterstaff");
        quarterstaff.price = 200;
        quarterstaff.weight = 2.0f;
        quarterstaff.actions.Add("equip");
        quarterstaff.tags.Add("MainHand");
        equipmentList.Add(quarterstaff);

        Weapon shortbow = new Weapon("Shortbow", WeaponCategory.LightWeapon, new List<WeaponTag> { WeaponTag.TwoHanded }, WeaponType.Bow, new List<SkillAttribute> { SkillAttribute.Smash, SkillAttribute.Pierce }, new CharacterStats { PhysicalAttack = 10, WeaponAttackSpeedMultiplier = 1.2f });
        shortbow.uid = 1008;
        shortbow.description = "A shortbow suited for quick ranged attacks.";
        shortbow.icon = Resources.Load<Texture2D>("Icons/Shortbow");
        shortbow.price = 150;
        shortbow.weight = 2.5f;
        shortbow.actions.Add("equip");
        shortbow.tags.Add("MainHand");
        shortbow.tags.Add("TwoHanded");
        equipmentList.Add(shortbow);

        Weapon rustySledgeHammer = new Weapon("Rusty Sledgehammer", WeaponCategory.HeavyWeapon, new List<WeaponTag> { WeaponTag.TwoHanded }, WeaponType.Hammer, new List<SkillAttribute> { SkillAttribute.Smash }, new CharacterStats { PhysicalAttack = 20, WeaponAttackSpeedMultiplier = 0.6f });
        rustySledgeHammer.uid = 1009;
        rustySledgeHammer.description = "A large, heavy sledgehammer with low speed.";
        rustySledgeHammer.icon = Resources.Load<Texture2D>("Icons/RustySledgeHammer");
        rustySledgeHammer.price = 250;
        rustySledgeHammer.weight = 5.0f;
        rustySledgeHammer.actions.Add("equip");
        rustySledgeHammer.tags.Add("MainHand");
        rustySledgeHammer.tags.Add("TwoHanded");
        equipmentList.Add(rustySledgeHammer);


        // 모든 장비를 itemDic에 추가
        foreach (var item in equipmentList)
        {
            /*if (item is Weapon weapon)
            {
                Debug.Log("WeaponCategory = " + weapon.WeaponCategory);
                Debug.Log("WeaponType = " + weapon.WeaponType);
            }*/
            ItemManager.itemDic.Add(item.uid, item);
        }
        /*
        if (ItemManager.itemDic[1004] is Weapon wea)
        {
            Debug.Log("WeaponCategory = " + wea.WeaponCategory);
            Debug.Log("WeaponType = " + wea.WeaponType);
        }*/
    }
}
