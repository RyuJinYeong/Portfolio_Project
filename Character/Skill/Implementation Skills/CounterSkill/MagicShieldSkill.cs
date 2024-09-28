using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicShieldSkill : DefensiveSkill // 마법 방어 스킬
{
    public MagicShieldSkill()
        : base("마력방패", 1,0.5, SkillAttribute.Magic)
    {
        MentalCost = 1;
        IconAddress = "Assets/Icons/Skills/MagicShield.png";
    }
}
