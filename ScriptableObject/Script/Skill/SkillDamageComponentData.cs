using System;

[Serializable]
public class SkillDamageComponentData
{
    public SkillType damageType = SkillType.Physical;
    public SkillAttribute attribute = SkillAttribute.None;

    public float damageMultiplier = 1f;
    public int hitCount = 1;
}