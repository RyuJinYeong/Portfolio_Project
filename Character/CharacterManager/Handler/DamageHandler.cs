using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageHandler
{
    public int TakeDamage(CharacterData character, int damage, SkillType damageType, SkillAttribute damageAttribute)
    {
        //저항력, 방어력, 속성 특화 계산
        int reducedDamage = CalculateDamage(damage, character.FinalStats, damageType, damageAttribute);

        //방어도 적용 - 마법 방어도가 우선 적용
        int remainingDamage = ApplyArmor(character, reducedDamage);

        // 남은 데미지를 체력에서 차감
        character.CurrentHp -= remainingDamage;

        if (character.CurrentHp <= 0)
        {
            character.CurrentHp = 0;
            character.IsAlive = false;
            // 캐릭터 사망 처리 로직 추가
        }

        return reducedDamage; // 최종 데미지 반환
    }

    // 방어도 적용 메서드
    private int ApplyArmor(CharacterData character, int damage)
    {
        int remainingDamage = damage;

        // 마법 방어도 적용 - 물리 방어도 보다 먼저 적용
        if (character.MagicalArmor > 0)
        {
            if (remainingDamage <= character.MagicalArmor)
            {
                character.MagicalArmor -= remainingDamage;
                return 0;
            }
            else
            {
                remainingDamage -= character.MagicalArmor;
                character.MagicalArmor = 0;
            }
        }

        // 물리 방어도 적용
        if (character.PhysicalArmor > 0)
        {
            if (remainingDamage <= character.PhysicalArmor)
            {
                character.PhysicalArmor -= remainingDamage;
                return 0;
            }
            else
            {
                remainingDamage -= character.PhysicalArmor;
                character.PhysicalArmor = 0;
            }
        }

        return remainingDamage;
    }

    public int CalculateDamage(int baseDamage, CharacterStats stats, SkillType type, SkillAttribute attribute)
    {
        int resistance = 0;
        int affinityBonus = 0;  // 속성 특화로 인한 추가 데미지

        // 속성 저항 적용
        switch (attribute)
        {
            case SkillAttribute.Fire:
                resistance = stats.FireResistance;
                affinityBonus = stats.FireAffinity;
                break;
            case SkillAttribute.Ice:
                resistance = stats.IceResistance;
                affinityBonus = stats.IceAffinity;
                break;
            case SkillAttribute.Lightning:
                resistance = stats.LightningResistance;
                affinityBonus = stats.LightningAffinity;
                break;
            case SkillAttribute.Slash:
                resistance = stats.SlashResistance;
                affinityBonus = stats.SlashAffinity;
                break;
            case SkillAttribute.Pierce:
                resistance = stats.PierceResistance;
                affinityBonus = stats.PierceAffinity;
                break;
            case SkillAttribute.Smash:
                resistance = stats.SmashResistance;
                affinityBonus = stats.SmashAffinity;
                break;
                // 필요에 따라 속성 추가...
        }

        // 속성 저항에 따른 데미지 감소
        int reducedDamage = baseDamage * (100 - resistance) / 100;

        // 속성 특화에 따른 추가 데미지 적용
        reducedDamage += reducedDamage * affinityBonus / 100; // 속성 특화에 따른 데미지 배율 적용

        // 물리/마법 방어에 따른 데미지 감소
        switch (type)
        {
            case SkillType.Physical:
                reducedDamage -= stats.PhysicalDefense;
                break;
            case SkillType.Magical:
                reducedDamage -= stats.MagicalDefense;
                break;
        }

        return reducedDamage < 0 ? 0 : reducedDamage; // 데미지는 0보다 작아질 수 없습니다.
    }
}