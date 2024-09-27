using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CutSkill : PhysicalAttackSkill, IEvolvableSkill // 베기
{
    public int SkillUseCount { get; set; }
    public int SkillKillCount { get; set; }
    public int SkillDamageCount { get; set; }

    public int EvolUseCount => 100; // 진화에 필요한 사용 횟수
    public int EvolKillCount => 10; // 진화에 필요한 처치 횟수
    public int EvolDamageCount => 1000; // 진화에 필요한 데미지


    public CutSkill()
        : base("베기", 1,1, 1, SkillAttribute.Slash)
    {   
        SkillUseCount = 0;
        SkillKillCount = 0;
        SkillDamageCount = 0;
        IconAddress = "Assets/Icons/Skills/Cut.asset";        
    }

    // 스킬 사용 시 호출할 수 있는 메서드
    public void OnSkillUsed(int damage, CharacterData user, CharacterData target)
    {
        SkillUseCount++;
        SkillDamageCount += damage;

        if (target.FinalStats.CurrentHp <= 0) // 타겟이 죽었을 경우
        {
            SkillKillCount++;
        }

        CheckEvolution(user);
    }

    public void CheckEvolution(CharacterData user)
    {
        if (SkillUseCount >= EvolUseCount && SkillKillCount >= EvolKillCount && SkillDamageCount >= EvolDamageCount)
        {
            EvolveSkill(user);
        }
    }

    public void EvolveSkill(CharacterData user)
    {
        // 스킬 진화 로직
        SkillManager.RemoveSkill(user, this);
        SkillManager.AddSkill(user, new SlashSkill()); // 진화된 스킬로 대체
    }
}