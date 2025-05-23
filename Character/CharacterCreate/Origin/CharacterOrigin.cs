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
                    new List<SkillBase>
                    {
                        ItemManager.itemDic[3000] as SkillBase,  // 휘두르기
                        ItemManager.itemDic[3002] as SkillBase,  // 찌르기
                        ItemManager.itemDic[3004] as SkillBase,  // 사격
                        ItemManager.itemDic[3006] as SkillBase,  // 베기
                        ItemManager.itemDic[3009] as SkillBase  // 방어
                    },
                    ItemManager.itemDic[3009] as SkillBase
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
                    new List<SkillBase>
                    {
                        ItemManager.itemDic[3000] as SkillBase,  // 휘두르기
                        ItemManager.itemDic[3002] as SkillBase,  // 찌르기
                        ItemManager.itemDic[3004] as SkillBase,  // 사격
                        ItemManager.itemDic[3006] as SkillBase,  // 베기
                        ItemManager.itemDic[3009] as SkillBase  // 방어
                    },
                    ItemManager.itemDic[3009] as SkillBase
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
                    new List<SkillBase>
                    {
                        ItemManager.itemDic[3000] as SkillBase,  // 휘두르기
                        ItemManager.itemDic[3002] as SkillBase,  // 찌르기
                        ItemManager.itemDic[3004] as SkillBase,  // 사격
                        ItemManager.itemDic[3006] as SkillBase,  // 베기
                        ItemManager.itemDic[3009] as SkillBase  // 방어
                    },
                    ItemManager.itemDic[3009] as SkillBase
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
                    new List<SkillBase>
                    {
                        ItemManager.itemDic[3000] as SkillBase,  // 휘두르기
                        ItemManager.itemDic[3002] as SkillBase,  // 찌르기
                        ItemManager.itemDic[3004] as SkillBase,  // 사격
                        ItemManager.itemDic[3006] as SkillBase,  // 베기
                        ItemManager.itemDic[3009] as SkillBase  // 방어
                    },
                    ItemManager.itemDic[3009] as SkillBase
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
                    null ,
                    new List<TraitBase> { new DeftnessTrait() },
                    new List<SkillBase>
                    {
                        ItemManager.itemDic[3000] as SkillBase,  // 휘두르기
                        ItemManager.itemDic[3002] as SkillBase,  // 찌르기
                        ItemManager.itemDic[3004] as SkillBase,  // 사격
                        ItemManager.itemDic[3006] as SkillBase,  // 베기
                        ItemManager.itemDic[3009] as SkillBase  // 방어
                    },
                    ItemManager.itemDic[3009] as SkillBase
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
                    new List<SkillBase>
                    {
                        ItemManager.itemDic[3000] as SkillBase,  // 휘두르기
                        ItemManager.itemDic[3002] as SkillBase,  // 찌르기
                        ItemManager.itemDic[3004] as SkillBase,  // 사격
                        ItemManager.itemDic[3006] as SkillBase,  // 베기
                        ItemManager.itemDic[3008] as SkillBase,  // 마력탄
                        ItemManager.itemDic[3009] as SkillBase,  // 방어
                        ItemManager.itemDic[3010] as SkillBase   // 마력방패
                    },
                    ItemManager.itemDic[3010] as SkillBase   // 마력방패
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
                    new List<SkillBase>
                    {
                        ItemManager.itemDic[3000] as SkillBase,  // 휘두르기
                        ItemManager.itemDic[3002] as SkillBase,  // 찌르기
                        ItemManager.itemDic[3004] as SkillBase,  // 사격
                        ItemManager.itemDic[3006] as SkillBase,  // 베기
                        ItemManager.itemDic[3008] as SkillBase,  // 마력탄
                        ItemManager.itemDic[3009] as SkillBase,  // 방어
                        ItemManager.itemDic[3010] as SkillBase   // 마력방패
                    },
                    ItemManager.itemDic[3009] as SkillBase  // 방어
                )
            },
            {
                "TestOrigin",
                new CharacterData(
                    Origin.wandering_knight, "TestOrigin",
                    new CharacterStats(10, 10, 10, 10, 10, 10, 10),
                    null,
                    ItemManager.itemDic[1001] as Armor,  // Leather Armor
                    ItemManager.itemDic[1002] as Armor,  // Leather Gloves
                    ItemManager.itemDic[1003] as Armor,  // Leather Boots
                    ItemManager.itemDic[1004] as Weapon,  // Old Sword
                    ItemManager.itemDic[1005] as Weapon,  // Round Shield                    
                    new List<TraitBase> { new DurableTrait() },
                    new List<SkillBase>
                    {
                        ItemManager.itemDic[3000] as SkillBase,  // 휘두르기
                        ItemManager.itemDic[3002] as SkillBase,  // 찌르기
                        ItemManager.itemDic[3004] as SkillBase,  // 사격
                        ItemManager.itemDic[3006] as SkillBase,  // 베기
                        ItemManager.itemDic[3008] as SkillBase,  // 마력탄
                        ItemManager.itemDic[3009] as SkillBase,  // 방어
                        ItemManager.itemDic[3010] as SkillBase   // 마력방패
                    },
                    ItemManager.itemDic[3009] as SkillBase  // 방어
                )
            }
        };
    }
}
