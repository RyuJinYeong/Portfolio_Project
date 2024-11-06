using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity;

public class CombatHandler : MonoBehaviour
{
    public CharacterManager characterManager;
    private List<(SkillBase skill, CharacterManager target)> skillQueue;
    private List<(SkillBase skill, CharacterManager target)> counterSkillQueue;
    private List<SynergyEffect> activeSynergyEffects; // 시너지 효과 리스트
    private SynergyManager synergyManager;

    private Coroutine turnTimerCoroutine;

    public void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
        skillQueue = new List<(SkillBase skill, CharacterManager target)>();
        counterSkillQueue = new List<(SkillBase skill, CharacterManager target)>();
        activeSynergyEffects = new List<SynergyEffect>();
        synergyManager = new SynergyManager();
    }

    // 턴 시작 메서드
    public void StartTurn(System.Action onTurnEnd)
    {
        characterManager.isPlayerTurn = true;
        turnTimerCoroutine = StartCoroutine(TurnTimer(onTurnEnd));

        UIManager.Instance.characterTargeting.SelectCharacter(characterManager);
            //.DisplayCharacterInfo(characterManager); // UI 정보 표시
    }

    // 턴 타이머
    private IEnumerator TurnTimer(System.Action onTurnEnd)
    {
        yield return new WaitForSeconds(60.0f); // 60초 제한 시간

        if (characterManager.isPlayerTurn)
        {
            ExecuteSkillQueue(onTurnEnd); // 턴 종료 전 스킬 큐 실행            
            onTurnEnd();
        }
    }

    // 스킬 선택 및 큐에 추가 (리소스 소모 적용 및 경합 상태 처리)
    public void SelectSkill(SkillBase skill, CharacterManager target)
    {
        if (characterManager.isInMeleeCombat && skill.IsRangedSkill)
        {
            Debug.Log("경합 상태에서는 원거리 스킬을 사용할 수 없습니다.");
            return;
        }

        // 스킬 사용 시 리소스 체크 및 차감
        if (characterManager.character.FinalStats.CurrentStamina < skill.StaminaCost || characterManager.character.FinalStats.CurrentMentality < skill.MentalCost)
        {
            Debug.Log("리소스가 부족하여 스킬을 사용할 수 없습니다.");
            return;
        }

        // 리소스 차감
        characterManager.character.FinalStats.CurrentStamina -= skill.StaminaCost;
        characterManager.character.FinalStats.CurrentMentality -= skill.MentalCost;

        // 근거리 스킬 사용 시 경합 상태로 전환
        if (!skill.IsRangedSkill)
        {
            characterManager.isInMeleeCombat = true;
            characterManager.meleeTarget = target;
        }

        skillQueue.Add((skill, target));
        Debug.Log($"Skill {skill.name} added to queue. Stamina: {characterManager.character.FinalStats.CurrentStamina}, Mentality: {characterManager.character.FinalStats.CurrentMentality}");

        // 스킬 선택 후, 적용 가능한 시너지 효과 확인
        List<SynergyEffect> newSynergyEffects = synergyManager.GetActiveSynergies(skillQueue.Select(s => s.skill).ToList());

        // 선택된 스킬이 추가된 후 시너지 조건 체크
        foreach (var effect in newSynergyEffects)
        {
            if (!activeSynergyEffects.Contains(effect))
            {
                activeSynergyEffects.Add(effect); // 시너지 효과 리스트에 추가
            }
        }

        // UI 업데이트: 활성화된 시너지 표시
        //UIManager.Instance.UpdateSynergyUI(newSynergyEffects.ToList(), synergyManager.synergyRules);
    }

    // 플레이어가 대응 스킬 선택 후 큐에 추가
    public void SelectCounterSkill(SkillBase skill, CharacterManager target)
    {
        counterSkillQueue.Add((skill, target));
        Debug.Log($"Counter Skill {skill.name} added to queue.");
    }

    #region 경합 상태에서 적 처치 시 스킬 취소 및 리소스 반환

    // 경합 중 적 처치 시 스킬 큐 취소 및 리소스 반환
    public void HandleEnemyDefeated()
    {
        if (characterManager.isInMeleeCombat && characterManager.meleeTarget != null && !characterManager.meleeTarget.character.IsAlive)
        {
            Debug.Log($"{characterManager.meleeTarget.character.Name} 처치 성공. 스킬 큐 취소 및 리소스 반환.");
            CancelRemainingSkills();
        }

        // 경합 상태 해제
        characterManager.isInMeleeCombat = false;
        characterManager.meleeTarget = null;
    }

    // 남은 스킬 취소 및 리소스 반환
    private void CancelRemainingSkills()
    {
        foreach (var item in skillQueue)
        {
            SkillBase skill = item.skill;
            // 리소스 일부 반환 (예: 50%) - 해당 필드도 변수화시켜서 관리 시 반환 값에 변주를 줄 수 있으니 필요시 추후 개선필요
            characterManager.character.FinalStats.CurrentStamina += (int)(skill.StaminaCost * 0.5f);
            characterManager.character.FinalStats.CurrentMentality += (int)(skill.MentalCost * 0.5f);
        }

        skillQueue.Clear();
        Debug.Log($"Remaining skills canceled. Stamina: {characterManager.character.FinalStats.CurrentStamina}, Mentality: {characterManager.character.FinalStats.CurrentMentality}");
    }

    // 적 사망 체크
    private void CheckForEnemyDefeated(CharacterManager target)
    {
        if (!target.character.IsAlive)
        {
            HandleEnemyDefeated();
        }
    }

    // 스킬 큐 순차 실행
    private void ExecuteSkillQueue(System.Action onTurnEnd)
    {
        StopCoroutine(turnTimerCoroutine);
        if (skillQueue.Count > 0)
        {
            StartCoroutine(ExecuteSkills(onTurnEnd));
        }
        else
        {
            Debug.Log("선택된 스킬이 없습니다. 턴 종료");
            onTurnEnd();
        }
    }

    // 스킬, 대응 스킬 큐 객체 순차 접근 및 사용
    private IEnumerator ExecuteSkills(System.Action onTurnEnd)
    {
        foreach (var item in skillQueue)
        {
            SkillBase skill = item.skill;
            CharacterManager target = item.target;

            // 경합 상태에서 원거리 스킬 사용 불가
            if (characterManager.isInMeleeCombat && skill.IsRangedSkill)
            {
                Debug.Log("경합 상태에서는 원거리 스킬을 사용할 수 없습니다.");
                continue;
            }

            UseSkill(skill, target);
            yield return new WaitForSeconds(1.0f); // 스킬 간 대기 시간
        }

        // 대응 스킬 실행
        foreach (var item in counterSkillQueue)
        {
            SkillBase counterSkill = item.skill;
            CharacterManager counterTarget = item.target;
            UseSkill(counterSkill, counterTarget);
            yield return new WaitForSeconds(1.0f); // 대응 스킬 간 대기 시간
        }

        // 스킬 큐 및 대응 스킬 큐 비우기
        skillQueue.Clear();
        counterSkillQueue.Clear();
        onTurnEnd();
    }

    // 스킬 사용 메서드
    public void UseSkill(SkillBase skill, CharacterManager target)
    {
        // 스킬 발동 시 시너지 효과 적용
        foreach (var synergyEffect in activeSynergyEffects)
        {
            synergyEffect.OnApply(characterManager); // 스킬 발동 시 버프 효과 적용
        }

        Debug.Log($"Using skill: {skill.name} on {target.character.Name}");
        float damageMultiplier = skill.DamageMultiplier;
        int damage = 0;

        if (skill.IsOffHand)
        {
            damageMultiplier *= 0.9f;  // 보조 무기 패널티 적용
        }

        switch (skill.Type)
        {
            case SkillType.Physical:
                damage = Mathf.FloorToInt(characterManager.character.FinalStats.PhysicalAttack * damageMultiplier * characterManager.character.PhysicalDamageMultiplier);
                break;
            case SkillType.Magical:
                damage = Mathf.FloorToInt(characterManager.character.FinalStats.MagicalAttack * damageMultiplier * characterManager.character.MagicalDamageMultiplier);
                break;
        }

        int finalDamage = target.TakeDamage(damage, skill.Type, skill.Attribute);

        if (skill.IsEvolvableSkill)
        {
            skill.OnSkillUsed(finalDamage, characterManager.character, target.character);
        }

        // 상대가 사망했는지 확인 후 처리
        if (!target.character.IsAlive)
        {
            HandleEnemyDefeated();
        }

        // 시너지 효과 해제
        EndSynergyEffects();
    }

    // 시너지 체크
    private void CheckSynergies()
    {
        List<SynergyEffect> newSynergyEffects = synergyManager.GetActiveSynergies(skillQueue.Select(s => s.skill).ToList());

        foreach (var effect in newSynergyEffects)
        {
            if (!activeSynergyEffects.Contains(effect))
            {
                activeSynergyEffects.Add(effect);
            }
        }
    }

    // 시너지 효과 만료 처리
    private void EndSynergyEffects()
    {
        foreach (var effect in activeSynergyEffects)
        {
            effect.OnExpire(characterManager); // 각 시너지 효과를 해제
        }
        activeSynergyEffects.Clear(); // 리스트 비우기
    }

    #endregion


    #region AI 행동 처리 메서드

    // AI 턴 처리
    public IEnumerator HandleAITurn(System.Action onTurnEnd)
    {
        // AI 로직 구현 필요
        yield return new WaitForSeconds(1.0f); // AI 대기 시간
        UseSkill(AIChooseSkill(), FindTargetForAI());
        onTurnEnd();  // 턴 종료 콜백 호출
    }

    // AI 사용스킬 지정 메서드
    private SkillBase AIChooseSkill()
    {
        switch (characterManager.character.personality) // AI 행동양식 제어
        {
            case Personality.Simple:
                break;
            case Personality.Aggressive:
                break;
            case Personality.Cunning:
                break;
            case Personality.Cautious:
                break;
        }

        // 간단한 로직으로 AI가 사용할 스킬 선택
        return characterManager.character.Skills[0];
    }

    // AI 공격대상 지정
    private CharacterManager FindTargetForAI()
    {
        // 로직으로 AI가 공격할 대상 선택
        switch (characterManager.character.personality) // 성향 별 AI 행동양식 제어
        {
            case Personality.Simple:
                break;
            case Personality.Aggressive:
                break;
            case Personality.Cunning:
                break;
            case Personality.Cautious:
                break;
        }

        return characterManager; // 기본적으로 자신을 반환하거나, GameManager에서 적을 가져오도록 구현 가능
    }

    public List<(SkillBase skill, CharacterManager target)> GetSkillQueue()
    {
        return skillQueue;
    }

    #endregion
}
