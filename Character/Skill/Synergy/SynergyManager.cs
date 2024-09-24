using System;
using System.Collections.Generic;
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
public class SynergyEffect
{
    public float DamageMultiplier { get; set; } = 1.0f;  // 데미지 배율
    public SkillAttribute? AffectedAttribute { get; set; }  // 영향을 받는 속성 (옵션)
    // 필요한 추가 효과 필드 구현
}

public class SynergyManager
{
    private List<SynergyRule> synergyRules;

    public SynergyManager()
    {
        synergyRules = new List<SynergyRule>();

        // 오름차순 데미지 증가 시너지
        synergyRules.Add(new SynergyRule(
            "오름차순 데미지 증가",
            skills => skills.Count >= 3 && IsAscendingCost(skills),
            skills => CalculateAscendingDamageBonus(skills)
        ));

        // 동일 속성 스킬 데미지 증가 시너지
        synergyRules.Add(new SynergyRule(
            "동일 속성 스킬 데미지 증가",
            skills => skills.Count >= 3 && IsSameAttribute(skills),
            skills => CalculateAttributeDamageBonus(skills)
        ));
    }
    
    // 시너지 조건을 실시간으로 체크하여 반환
    public List<string> GetActiveSynergies(List<SkillBase> skillQueue)
    {
        List<string> activeSynergies = new List<string>();
        foreach (var rule in synergyRules)
        {
            if (rule.Condition(skillQueue))
            {
                activeSynergies.Add(rule.Name);  // 발동된 시너지 이름 추가
            }
        }
        return activeSynergies;
    }

    // 시너지가 활성화된 경우의 효과를 리스트로 반환
    public List<SynergyEffect> ApplySynergies(List<SkillBase> skillQueue)
    {
        List<SynergyEffect> activeSynergies = new List<SynergyEffect>();

        foreach (var rule in synergyRules)
        {
            if (rule.Condition(skillQueue))
            {
                activeSynergies.Add(rule.Effect(skillQueue));
            }
        }

        return activeSynergies;
    }

    // SynergyManager에서 시너지로 인한 데미지 배수를 계산하는 메서드
    public float CalculateSynergyDamageMultiplier(List<SkillBase> skillQueue, SkillBase currentSkill)
    {
        float totalMultiplier = 1.0f;  // 기본 배수는 1.0 (변화 없음)

        // 현재 큐에 있는 스킬들을 기반으로 시너지 효과 계산
        foreach (var effect in activeSynergyEffects)
        {
            if (effect.AffectedAttribute == null || effect.AffectedAttribute == currentSkill.Attribute)
            {
                totalMultiplier *= effect.DamageMultiplier;  // 시너지로 인한 데미지 배수 적용
            }
        }

        return totalMultiplier;
    }

    private bool IsAscendingCost(List<SkillBase> skills)
    {
        for (int i = 1; i < skills.Count; i++)
        {
            // 지구력 + 정신력 소모 합산
            int currentCost = skills[i].StaminaCost + skills[i].MentalCost;
            int previousCost = skills[i - 1].StaminaCost + skills[i - 1].MentalCost;

            // 코스트가 이전 스킬보다 작으면 오름차순 아님
            if (currentCost < previousCost)
                return false;
        }
        return true;
    }

    private bool IsSameAttribute(List<SkillBase> skills)
    {
        return skills.All(skill => skill.Attribute == skills[0].Attribute);
    }

    private SynergyEffect CalculateAscendingDamageBonus(List<SkillBase> skills)
    {
        float bonusPercentage = 0.05f;  // 매 스킬마다 5%씩 증가
        float totalMultiplier = 1.0f + bonusPercentage * (skills.Count - 1);

        return new SynergyEffect
        {
            DamageMultiplier = totalMultiplier
        };
    }

    private SynergyEffect CalculateAttributeDamageBonus(List<SkillBase> skills)
    {
        return new SynergyEffect
        {
            DamageMultiplier = 1.1f,  // 10% 데미지 증가
            AffectedAttribute = skills[0].Attribute  // 해당 속성
        };
    }
}
