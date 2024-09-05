using System;
using System.Collections.Generic;
using UnityEngine;
/*
public enum ReactSkill // 대응스킬
{
dodge, // 회피
}

public enum Skill // 일반스킬
{
fake_attack // 속임수 공격
}
*/


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
}