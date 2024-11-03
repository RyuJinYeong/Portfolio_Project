using SoftKitty.InventoryEngine;
using System.Collections.Generic;

public static class CharacterOrigin
{
    public static Dictionary<string, CharacterData> GetOriginData()
    {
        return new Dictionary<string, CharacterData>
        {
            {
                "방랑기사",
                new CharacterData(
                    Origin.wandering_knight, "방랑기사",
                    new CharacterStats(9, 2, 5, 2, 2, 10, 5),
                    null,
                    ItemManager.itemDic[1001] as Armor,  // Leather Armor
                    ItemManager.itemDic[1002] as Armor,  // Leather Gloves
                    ItemManager.itemDic[1003] as Armor,  // Leather Boots
                    ItemManager.itemDic[1004] as Weapon,  // Old Sword
                    ItemManager.itemDic[1005] as Weapon,  // Round Shield                    
                    new List<TraitBase> { new DurableTrait() },
                    new List<SkillBase> { new StingSkill(), new CutSkill(), new DefenceSkill() }
                )
            },
            {
                "검사",
                new CharacterData(
                    Origin.swordsman, "검사",
                    new CharacterStats(2, 12, 8, 1, 2, 5, 5),
                    null,
                    ItemManager.itemDic[1001] as Armor,
                    ItemManager.itemDic[1002] as Armor,
                    ItemManager.itemDic[1003] as Armor,
                    ItemManager.itemDic[1004] as Weapon,
                    null,
                    new List<TraitBase> { new SwordMasteryTrait() },
                    new List<SkillBase> { new StingSkill(), new CutSkill(), new DefenceSkill() }
                )
            },
            {
                "야만인",
                new CharacterData(
                    Origin.barbarian, "야만인",
                    new CharacterStats(14, 1, 6, 1, 1, 7, 5),
                    null,
                    ItemManager.itemDic[1001] as Armor,
                    ItemManager.itemDic[1002] as Armor,
                    ItemManager.itemDic[1003] as Armor,
                    ItemManager.itemDic[1009] as Weapon,  // Rusty Sledgehammer
                    null,
                    new List<TraitBase> { new BarbarianPowerTrait() },
                    new List<SkillBase> { new SwingSkill(), new DefenceSkill() }
                )
            },
            {
                "사냥꾼",
                new CharacterData(
                    Origin.hunter, "사냥꾼",
                    new CharacterStats(3, 12, 7, 2, 2, 4, 5),
                    null,
                    ItemManager.itemDic[1001] as Armor,
                    ItemManager.itemDic[1002] as Armor,
                    ItemManager.itemDic[1003] as Armor,
                    ItemManager.itemDic[1008] as Weapon,  // Shortbow
                    null,
                    new List<TraitBase> { new BowMasteryTrait() },
                    new List<SkillBase> { new SwingSkill(), new ShootSkill(), new DefenceSkill() }
                )
            },
            {
                "도적",
                new CharacterData(
                    Origin.rogue, "도적",
                    new CharacterStats(2, 10, 10, 1, 2, 5, 5),
                    null,
                    ItemManager.itemDic[1001] as Armor,  // Leather Armor
                    ItemManager.itemDic[1002] as Armor,  // Leather Gloves
                    ItemManager.itemDic[1003] as Armor,  // Leather Boots
                    ItemManager.itemDic[1006] as Weapon,  // Dagger
                    ItemManager.itemDic[1006] as Weapon,  // Another Dagger
                    new List<TraitBase> { new DeftnessTrait() },
                    new List<SkillBase> { new StingSkill(), new CutSkill(), new DefenceSkill() }
                )
            },
            {
                "마법사",
                new CharacterData(
                    Origin.wizard, "마법사",
                    new CharacterStats(2, 2, 5, 11, 5, 5, 5),
                    null,
                    ItemManager.itemDic[1001] as Armor,  // Leather Armor
                    ItemManager.itemDic[1002] as Armor,  // Leather Gloves
                    ItemManager.itemDic[1003] as Armor,  // Leather Boots
                    ItemManager.itemDic[1007] as Weapon,  // Quarterstaff
                    null,
                    new List<TraitBase> { new BasicElementalAptitudeTrait() },
                    new List<SkillBase> { new SwingSkill(), new MagicBulletSkill(), new MagicShieldSkill(), new DefenceSkill() }
                )
            },
            {
                "출신불명",
                new CharacterData(
                    Origin.unknown, "출신불명",
                    new CharacterStats(5, 5, 5, 5, 5, 5, 5),
                    null,
                    ItemManager.itemDic[1001] as Armor,  // Leather Armor
                    ItemManager.itemDic[1002] as Armor,  // Leather Gloves
                    ItemManager.itemDic[1003] as Armor,  // Leather Boots
                    ItemManager.itemDic[1004] as Weapon,  // Old Sword
                    null,
                    new List<TraitBase> { },
                    new List<SkillBase> { new StingSkill(), new CutSkill(), new MagicBulletSkill(), new DefenceSkill(), new MagicShieldSkill() }
                )
            }
        };
    }
}