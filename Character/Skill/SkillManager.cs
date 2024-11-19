using System;
using System.Collections.Generic;
using UnityEngine;

// 스킬을 관리하는 클래스
public static class SkillManager
{
    public static void AddSkill(CharacterData character, SkillBase skill)
    {
        if (!character.Skills.Contains(skill))
        {
            character.Skills.Add(skill);
            skill.LoadIcon();
        }
        else
        {
            Debug.Log("이미 존재하는 스킬 습득 시도");
        }
    }

    public static void RemoveSkill(CharacterData character, SkillBase skill)
    {
        if (character.Skills.Contains(skill))
        {
            character.Skills.Remove(skill);
        }
        else
        {
            Debug.Log("존재하지 않는 스킬 삭제 시도");
        }
    }

    public static void SetDefaultCounterSkill(CharacterManager manager, SkillBase skill)
    {
        if(skill.StaminaCost + skill.MentalCost == 1)
        {
            manager.character.DefaultCounterSkill = skill;
        }
        else
        {
            Debug.Log("기본 대응 스킬로 설정할 수 없습니다. (코스트 초과)");
        }
    }
}