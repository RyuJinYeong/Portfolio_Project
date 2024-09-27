using System.Collections.Generic;
using System;
using System.Linq;
public class SynergyRule
{
    public string Name { get; private set; }  // 시너지 이름
    public Func<List<SkillBase>, bool> Condition { get; private set; }  // 시너지 발동 조건
    public Func<List<SkillBase>, SynergyEffect> Effect { get; private set; }  // 시너지 효과 반환

    public SynergyRule(string name, Func<List<SkillBase>, bool> condition, Func<List<SkillBase>, SynergyEffect> effect)
    {
        Name = name;
        Condition = condition;
        Effect = effect;
    }
}

public class SynergyManager
{
    public List<SynergyRule> synergyRules;

    public SynergyManager()
    {
        synergyRules = new List<SynergyRule>();

        // 오름차순 데미지 증가 시너지
        synergyRules.Add(new SynergyRule(
            "오름차순 데미지 증가",
            skills => skills.Count >= 3 && IsAscendingCost(skills),
            skills => new SynergyEffect(physicalDamageBonus: 0.05f * skills.Count, magicalDamageBonus: 0.05f * skills.Count, attackSpeedBonus: 0.0f, castSpeedBonus: 0.0f, ignoreArmor: false)
        ));

        // 동일 속성 스킬 데미지 증가 시너지
        synergyRules.Add(new SynergyRule(
            "동일 속성 스킬 데미지 증가",
            skills => skills.Count >= 3 && IsSameAttribute(skills),
            skills => new SynergyEffect(physicalDamageBonus: 0.1f, magicalDamageBonus: 0.1f, attackSpeedBonus: 0.0f, castSpeedBonus: 0.0f, ignoreArmor: false)
        ));
    }

    // 시너지 조건을 실시간으로 체크하여 활성화된 시너지를 반환
    public List<SynergyEffect> GetActiveSynergies(List<SkillBase> skillQueue)
    {
        List<SynergyEffect> activeSynergies = new List<SynergyEffect>();

        foreach (var rule in synergyRules)
        {
            if (rule.Condition(skillQueue))
            {
                activeSynergies.Add(rule.Effect(skillQueue));  // 해당 시너지 효과를 리스트에 추가
            }
        }

        return activeSynergies;
    }

    private bool IsAscendingCost(List<SkillBase> skills)
    {
        for (int i = 1; i < skills.Count; i++)
        {
            int currentCost = skills[i].StaminaCost + skills[i].MentalCost;
            int previousCost = skills[i - 1].StaminaCost + skills[i - 1].MentalCost;

            if (currentCost < previousCost)
                return false;
        }
        return true;
    }

    private bool IsSameAttribute(List<SkillBase> skills)
    {
        return skills.All(skill => skill.Attribute == skills[0].Attribute);
    }
}
