using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatRequirementCondition : ISkillAcquisitionCondition // 스킬 습득 조건 - 스탯
{
    public Func<CharacterStats, int> statSelector; // 요구 스탯 선택
    public int requiredValue; // 요구 스탯치

    public StatRequirementCondition(Func<CharacterStats, int> statSelector, int requiredValue)
    {
        this.statSelector = statSelector;
        this.requiredValue = requiredValue;
    }

    public bool CanAcquireSkill(CharacterData character) // 캐릭터의 습득 조건 만족 여부 판별
    {
        return statSelector(character.BaseStats) >= requiredValue; // 버프, 특성 혹은 장비로 증감한 스탯은 제외하고 캐릭터의 기본 스탯만 계산
    }
}