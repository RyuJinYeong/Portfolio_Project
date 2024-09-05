using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenceSkill : DefensiveSkill, IPhysicalSkill // 물리 방어 스킬
{
    public int EnduCost { get; private set; }

    public DefenceSkill()
        : base("방어",1 , 0.5, SkillAttribute.None)
    {
        EnduCost = 1;
        iconAddress = "Assets/Icons/Skills/Defence.asset";
    }
}
