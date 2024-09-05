using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PierceSkill : PhysicalAttackSkill // 관통
{
    public PierceSkill()
        : base("관통", 3, 1.4, 1, SkillAttribute.Pierce)
    {

    }
}