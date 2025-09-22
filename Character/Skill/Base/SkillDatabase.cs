using SoftKitty.InventoryEngine;
using System.Collections.Generic;
using UnityEngine;

public static class SkillDatabase
{
    private static bool _initialized;

    public static void InitializeDatabase()
    {
        if (_initialized) return;

        int uidCounter = 3000;
        List<SkillBase> skillList = new List<SkillBase>();

        // 휘두르기 스킬 추가 (진화 가능) 3000
        skillList.Add(new SkillBase(
            name: "휘두르기",
            activationSpeed: 0.8,
            damageMultiplier: 1f,
            staminaCost: 1,
            mentalCost: 0,
            type: SkillType.Physical,
            attribute: SkillAttribute.Smash,
            iconAddress: "Assets/Icons/Skills/Swing.png"
        )
        {
            uid = uidCounter++,
            IsEvolvableSkill = true,
            SkillUseCount = 0,
            SkillKillCount = 0,
            SkillDamageCount = 0,
            EvolRequiredUseCount = 100,
            EvolRequiredKillCount = 10,
            EvolRequiredDamageCount = 1000,
            QuickSlot = true
        });

        // 분쇄 스킬 추가 (진화 후 스킬) 3001
        skillList.Add(new SkillBase(
            name: "분쇄",
            activationSpeed: 1.0,
            damageMultiplier: 3f,
            staminaCost: 1,
            mentalCost: 0,
            type: SkillType.Physical,
            attribute: SkillAttribute.Smash,
            iconAddress: "Assets/Icons/Skills/Smash.png"
        )
        {
            uid = uidCounter++,
            QuickSlot = true
        });

        // 찌르기 스킬 추가 (진화 가능) 3002
        skillList.Add(new SkillBase(
            name: "찌르기",
            activationSpeed: 1.2,
            damageMultiplier: 1f,
            staminaCost: 1,
            mentalCost: 0,
            type: SkillType.Physical,
            attribute: SkillAttribute.Pierce,
            iconAddress: "Assets/Icons/Skills/Sting.png"
        )
        {
            uid = uidCounter++,
            IsEvolvableSkill = true,
            SkillUseCount = 0,
            SkillKillCount = 0,
            SkillDamageCount = 0,
            EvolRequiredUseCount = 100,
            EvolRequiredKillCount = 10,
            EvolRequiredDamageCount = 1000,
            QuickSlot = true
        });

        // 관통 스킬 추가 (진화 후 스킬) 3003
        skillList.Add(new SkillBase(
            name: "관통",
            activationSpeed: 1.4,
            damageMultiplier: 3f,
            staminaCost: 1,
            mentalCost: 0,
            type: SkillType.Physical,
            attribute: SkillAttribute.Pierce,
            iconAddress: "Assets/Icons/Skills/Pierce.png"
        )
        {
            uid = uidCounter++,
            QuickSlot = true
        });

        // 사격 스킬 추가 (진화 가능) 3004
        skillList.Add(new SkillBase(
            name: "사격",
            activationSpeed: 1.0,
            damageMultiplier: 1f,
            staminaCost: 1,
            mentalCost: 0,
            type: SkillType.Physical,
            attribute: SkillAttribute.Pierce,
            iconAddress: "Assets/Icons/Skills/Shoot.png"
        )
        {
            uid = uidCounter++,
            IsEvolvableSkill = true,
            SkillUseCount = 0,
            SkillKillCount = 0,
            SkillDamageCount = 0,
            EvolRequiredUseCount = 100,
            EvolRequiredKillCount = 10,
            EvolRequiredDamageCount = 1000,
            IsBowSkill = true,
            IsRangedSkill = true,
            QuickSlot = true
        });

        // 관통시 스킬 추가 (진화 후 스킬) 3005
        skillList.Add(new SkillBase(
            name: "관통시",
            activationSpeed: 1.2,
            damageMultiplier: 3f,
            staminaCost: 1,
            mentalCost: 0,
            type: SkillType.Physical,
            attribute: SkillAttribute.Pierce,
            iconAddress: "Assets/Icons/Skills/PierceShoot.png"
        )
        {
            uid = uidCounter++,
            IsBowSkill = true,
            IsRangedSkill = true,
            QuickSlot = true
        });

        // 물리 공격 스킬 - 베기 추가 3006
        skillList.Add(new SkillBase(
            name: "베기",
            activationSpeed: 1.0,
            damageMultiplier: 1f,
            staminaCost: 1,
            mentalCost: 0,
            type: SkillType.Physical,
            attribute: SkillAttribute.Slash,
            iconAddress: "Assets/Icons/Skills/Cut.png"
        )
        {
            uid = uidCounter++,
            IsEvolvableSkill = true,
            SkillUseCount = 0,
            SkillKillCount = 0,
            SkillDamageCount = 0,
            EvolRequiredUseCount = 100,
            EvolRequiredKillCount = 10,
            EvolRequiredDamageCount = 1000,
            QuickSlot = true
        });

        // 참격 스킬 추가 3007
        skillList.Add(new SkillBase(
            name: "참격",
            activationSpeed: 1.2,
            damageMultiplier: 3f,
            staminaCost: 1,
            mentalCost: 0,
            type: SkillType.Physical,
            attribute: SkillAttribute.Slash,
            iconAddress: "Assets/Icons/Skills/Slash.png"
        )
        {
            uid = uidCounter++,
            QuickSlot = true
        });

        // 마법 공격 스킬 - 마력탄 추가 3008
        skillList.Add(new SkillBase(
            name: "마력탄",
            activationSpeed: 0.8,
            damageMultiplier: 1f,
            staminaCost: 0,
            mentalCost: 1,
            type: SkillType.Magical,
            attribute: SkillAttribute.Magic,
            iconAddress: "Assets/Icons/Skills/magic_bolt.png"
        )
        {
            uid = uidCounter++,
            IsRangedSkill = true,
            CanUse = true,
            QuickSlot = true
        });

        // 방어 스킬 추가 3009
        skillList.Add(new SkillBase(
            name: "방어",
            activationSpeed: 0.5,
            damageMultiplier: 1f,
            staminaCost: 1,
            mentalCost: 0,
            type: SkillType.Physical,
            attribute: SkillAttribute.None,
            iconAddress: "Assets/Icons/Skills/Defence.png"
        )
        {
            uid = uidCounter++,
            IsCounterSkill = true,
            CanUse = true,
            QuickSlot = true
        });

        // 마력 방패 스킬 추가 3010
        skillList.Add(new SkillBase(
            name: "마력방패",
            activationSpeed: 0.5,
            damageMultiplier: 1f,
            staminaCost: 0,
            mentalCost: 1,
            type: SkillType.Magical,
            attribute: SkillAttribute.Magic,
            iconAddress: "Assets/Icons/Skills/MagicShield.png"
        )
        {
            uid = uidCounter++,
            IsCounterSkill = true,
            CanUse = true,
            QuickSlot = true
        });

        // 화염구 스킬 추가 - 3011
        skillList.Add(new SkillBase(
            name: "화염구",
            activationSpeed: 0.8,
            damageMultiplier: 2f,
            staminaCost: 0,
            mentalCost: 1,
            type: SkillType.Magical,
            attribute: SkillAttribute.Fire,
            iconAddress: "Assets/Icons/Skills/fireball.png"
        )
        {
            uid = uidCounter++,
            IsRangedSkill = true,
            CanUse = true,
            QuickSlot = true
        });

        // 모든 스킬을 아이템 매니저에 추가
        foreach (SkillBase skill in skillList)
        {
            ItemManager.itemDic.Add(skill.uid, skill);
        }

        _initialized = true;
        Debug.Log("스킬 DB 초기화");
    }
}
