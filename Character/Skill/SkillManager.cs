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
}