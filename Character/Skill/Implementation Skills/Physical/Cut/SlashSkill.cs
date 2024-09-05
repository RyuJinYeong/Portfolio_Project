using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashSkill : PhysicalAttackSkill // 참격
{
    public SlashSkill()
        : base("참격", 3, 1.2, 1, SkillAttribute.Slash)
    {

    }
}