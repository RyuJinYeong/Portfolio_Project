using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenceSkill : DefensiveSkill // 물리 방어 스킬
{
    public DefenceSkill()
        : base("방어",1 , 0.5, SkillAttribute.None)
    {
        StaminaCost = 1;
        IconAddress = "Assets/Icons/Skills/Defence.png";
    }
}
