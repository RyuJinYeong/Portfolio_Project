using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TraitRequirementCondition : ISkillAcquisitionCondition // 스킬 습득 조건 - 특성
{
    private TraitBase requiredTrait; // 요구 특성

    public TraitRequirementCondition(TraitBase requiredTrait) // 생성자
    {
        this.requiredTrait = requiredTrait;
    }

    public bool CanAcquireSkill(CharacterData character) // 캐릭터의 습득 조건 만족 여부 판별
    {
        return character.Traits.Contains(requiredTrait);
    }
}