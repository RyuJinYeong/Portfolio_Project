using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity;
using UnityEngine.TextCore.Text;

public class CombatHandler : MonoBehaviour
{
    public CharacterManager characterManager;
    private List<(SkillBase skill, CharacterManager target)> skillQueue;
    private List<(SkillBase skill, CharacterManager target)> counterSkillQueue;
    private List<SynergyEffect> activeSynergyEffects; // 시너지 효과 리스트
    private SynergyManager synergyManager;

    private Coroutine turnTimerCoroutine;
    private float totalDuration = 60.0f; // 턴 제한 시간

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
        // 캐릭터 타입에 따라 AI 여부 판단
        if (characterManager.character.Type != CharacterType.Character) // 플레이어가 조종하는 캐릭터가 아닐 경우
        {
            Debug.Log("AI 턴 시작");
            turnTimerCoroutine = StartCoroutine(TurnTimer(onTurnEnd)); // AI 턴 타이머 시작
            characterManager.UpdateCharacterUI();
            StartCoroutine(HandleAITurn(onTurnEnd));
        }
        else
        {
            Debug.Log("플레이어 턴 시작");
            turnTimerCoroutine = StartCoroutine(TurnTimer(onTurnEnd)); // 플레이어 턴 타이머 시작
            characterManager.UpdateCharacterUI();

            UIManager.Instance.characterTargeting.SelectCharacter(characterManager);
        }
    }



    // 턴 타이머
    private IEnumerator TurnTimer(System.Action onTurnEnd)
    {
        float timeRemaining = totalDuration;

        while (timeRemaining > 0)
        {
            UIManager.Instance.UpdateTurnTimer(timeRemaining);

            yield return null; // 한 프레임 기다림
            timeRemaining -= Time.deltaTime; // 남은 시간 감소
        }

        if (characterManager.isPlayerTurn)
        {
            ExecuteSkillQueue(onTurnEnd); // 턴 종료 전 스킬 큐 실행     
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

        // 리소스 차감
        if (!ConsumeResources(skill)) return;

        // 근거리 스킬 사용 시 경합 상태로 전환
        if (!skill.IsRangedSkill)
        {
            characterManager.isInMeleeCombat = true;
            characterManager.meleeTarget = target;
        }

        skillQueue.Add((skill, target));
        int order = skillQueue.Count;
        Debug.Log($"Skill {skill.name} added to queue. CurrentResources - Stamina: {characterManager.character.FinalStats.CurrentStamina}, Mentality: {characterManager.character.FinalStats.CurrentMentality}");

        // 스킬 큐 UI에 추가 (캐릭터 UI 핸들러 사용)
        target.characterUIHandler.AddSkillToQueue(skill, skillQueue.Count, characterManager); // 시전자 전달

        //시전자 UI 갱신        
        characterManager.UpdateCharacterUI();

        // 스킬 사용 가능 여부 업데이트
        UIManager.Instance.UpdateSkillTransparency(characterManager);

        UpdateSynergies();

        // UI 업데이트: 활성화된 시너지 표시
        //UIManager.Instance.UpdateSynergyUI(newSynergyEffects.ToList(), synergyManager.synergyRules);
    }


    // 스킬 큐에서 스킬 제거 및 리소스 반환
    public void RemoveSkillFromQueue(SkillBase skill)
    {
        // 스킬 큐에서 해당 스킬을 찾기
        var skillEntry = skillQueue.FirstOrDefault(s => s.skill == skill);
        if (skillEntry.skill != null)
        {
            skillQueue.Remove(skillEntry);
            characterManager.character.FinalStats.CurrentStamina += (int)(skill.StaminaCost);
            characterManager.character.FinalStats.CurrentMentality += (int)(skill.MentalCost);

            // UI 갱신
            characterManager.UpdateCharacterUI();
            // 스킬 사용 가능 여부 업데이트
            UIManager.Instance.UpdateSkillTransparency(characterManager);

            // 타겟의 상단 패널 UI에서 해당 스킬 제거
            skillEntry.target.characterUIHandler.RemoveSkillFromQueue(skill);     
            Debug.Log($"Skill {skill.name} removed from queue. Resources refunded: Stamina: {(int)(skill.StaminaCost)}, Mentality: {(int)(skill.MentalCost)}");            
        }

        // 경합 상태 해제 로직 추가
        // 남은 스킬 큐를 확인하여 근거리 스킬이 있는지 검사
        bool hasMeleeSkill = skillQueue.Any(s => !s.skill.IsRangedSkill);

        // 근거리 스킬이 더 이상 없다면 경합 상태를 해제
        if (!hasMeleeSkill && characterManager.isInMeleeCombat)
        {
            characterManager.isInMeleeCombat = false;
            characterManager.meleeTarget = null;
            Debug.Log("경합 상태 해제");
        }
    }


    // 리소스 소비 로직
    private bool ConsumeResources(SkillBase skill)
    {
        if (characterManager.character.FinalStats.CurrentStamina < skill.StaminaCost || characterManager.character.FinalStats.CurrentMentality < skill.MentalCost)
        {
            Debug.Log("리소스가 부족하여 스킬을 사용할 수 없습니다.");
            return false;
        }

        characterManager.character.FinalStats.CurrentStamina -= skill.StaminaCost;
        characterManager.character.FinalStats.CurrentMentality -= skill.MentalCost;
        characterManager.UpdateCharacterUI();
        return true;
    }

    // 플레이어가 대응 스킬 선택 후 큐에 추가
    public void SelectCounterSkill(SkillBase skill, CharacterManager target)
    {
        counterSkillQueue.Add((skill, target));
        Debug.Log($"Counter Skill {skill.name} added to queue.");
    }

    #region 경합 상태에서 적 처치 시 스킬 취소 및 리소스 반환

    // 경합 중 적 처치 시 스킬 큐 취소 및 리소스 반환
    public bool HandleEnemyDefeated()
    {
        if (characterManager.isInMeleeCombat && characterManager.meleeTarget != null && !characterManager.meleeTarget.character.IsAlive)
        {
            Debug.Log($"{characterManager.meleeTarget.character.Name} 처치 성공. 스킬 큐 취소 및 리소스 반환.");
            CancelRemainingSkills();
            // 경합 상태 해제
            characterManager.isInMeleeCombat = false;
            characterManager.meleeTarget = null;
            return true;
        }
        else
        {
            return false;
        }
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

            // 타겟의 상단 패널 UI에서 해당 스킬 제거
            item.target.characterUIHandler.RemoveSkillFromQueue(skill);
        }

        skillQueue.Clear();
        characterManager.UpdateCharacterUI();
        Debug.Log($"Remaining skills canceled. Stamina: {characterManager.character.FinalStats.CurrentStamina}, Mentality: {characterManager.character.FinalStats.CurrentMentality}");
    }

    // 스킬 큐 순차 실행
    public void ExecuteSkillQueue(System.Action onTurnEnd)
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
    public IEnumerator ExecuteSkills(System.Action onTurnEnd)
    {
        foreach (var item in skillQueue)
        {
            SkillBase skill = item.skill;
            CharacterManager target = item.target;

            UseSkill(skill, target);

            // 타겟의 상단 패널에서 스킬 큐 UI 제거
            target.characterUIHandler.RemoveSkillFromQueue(skill);
            target.UpdateCharacterUI();

            // 상대가 사망했는지 확인 후 처리
            if (!target.character.IsAlive && !skill.IsRangedSkill)
            {
                if (HandleEnemyDefeated()) // 경합 상태에서 타겟 사망 시 반복문 종료
                {
                    StartTurn(onTurnEnd);
                    yield break;
                }
            }

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
    public void AutoCounterAllies(System.Action onTurnEnd)
    {
        // 전체 캐릭터 리스트에서 아군 캐릭터 찾기
        List<CharacterManager> allCharacters = GameManager.Instance.GetAllCharacters();
        List<CharacterManager> allyCharacters = allCharacters.Where(character => character.character.IsMine == true && character.character.IsAlive).ToList();

        foreach (var ally in allyCharacters)
        {
            if (ally.character.DefaultCounterSkill != null && ally.character.IsAlive)
            {
                Debug.Log($"{ally.character.Name} 자동 대응 스킬 사용 : {ally.character.DefaultCounterSkill.name}");
                UseSkill(ally.character.DefaultCounterSkill, ally); // 본인을 타겟으로 기본 대응
            }
        }

        // 대응 스킬 실행 이후 턴 종료 처리
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

        Debug.Log($"{characterManager.character.Name} - {skill.name} 스킬 사용 -> {target.character.Name}");
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

        // 시너지 효과 해제
        EndSynergyEffects();
    }

    // 시너지 체크 로직
    private void UpdateSynergies()
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
        Debug.Log("AI 턴 시작");

        yield return new WaitForSeconds(2.0f); // AI 대기 시간 (AI 턴 대기시간 연출)

        // 스킬 및 타겟 선택
        SkillBase selectedSkill = AIChooseSkill();
        CharacterManager selectedTarget = FindTargetForAI();

        if (selectedSkill != null && selectedTarget != null)
        {
            // 스킬을 선택하여 스킬 큐에 추가
            SelectSkill(selectedSkill, selectedTarget);
        }

        yield return new WaitForSeconds(2.0f);
        ExecuteSkillQueue(onTurnEnd); // 턴 종료 전 스킬 큐 실행
    }

    // AI 사용스킬 지정 메서드
    private SkillBase AIChooseSkill()
    {
        SkillBase selectedSkill = null;
        List<SkillBase> availableSkills = characterManager.character.Skills
            .Where(skill => characterManager.character.FinalStats.CurrentStamina >= skill.StaminaCost &&
                            characterManager.character.FinalStats.CurrentMentality >= skill.MentalCost)
            .ToList();

        if (availableSkills.Count == 0)
        {
            Debug.Log("사용 가능한 스킬이 없습니다.");
            return null;
        }

        // 랜덤성 부여 (성향에 반하는 행동을 할 확률)
        float personalityDeviationChance = 0.15f; // 15% 확률로 성향과 상관없는 랜덤한 행동
        bool deviateFromPersonality = UnityEngine.Random.value < personalityDeviationChance;

        switch (characterManager.character.personality)
        {
            case Personality.Simple:
                selectedSkill = availableSkills.OrderBy(skill => skill.StaminaCost + skill.MentalCost).FirstOrDefault();
                if (deviateFromPersonality)
                {
                    selectedSkill = availableSkills.OrderByDescending(skill => skill.DamageMultiplier).FirstOrDefault();
                }
                break;

            case Personality.Aggressive:
                selectedSkill = availableSkills.OrderByDescending(skill => skill.DamageMultiplier).FirstOrDefault();
                if (deviateFromPersonality)
                {
                    selectedSkill = availableSkills.OrderBy(skill => skill.StaminaCost + skill.MentalCost).FirstOrDefault();
                }
                break;

            case Personality.Cunning:
                selectedSkill = availableSkills.FirstOrDefault(skill => skill.IsStatusEffectSkill);
                if (selectedSkill == null)
                {
                    selectedSkill = availableSkills.OrderByDescending(skill => skill.DamageMultiplier).FirstOrDefault();
                }
                if (deviateFromPersonality)
                {
                    selectedSkill = availableSkills.OrderBy(skill => skill.StaminaCost).FirstOrDefault();
                }
                break;

            case Personality.Cautious:
                selectedSkill = availableSkills.OrderBy(skill => skill.StaminaCost + skill.MentalCost).FirstOrDefault();
                if (deviateFromPersonality)
                {
                    selectedSkill = availableSkills.OrderByDescending(skill => skill.DamageMultiplier).FirstOrDefault();
                }
                break;

            default:
                selectedSkill = availableSkills[0];
                break;
        }

        return selectedSkill;
    }

    // AI 공격대상 지정
    private CharacterManager FindTargetForAI()
    {
        CharacterManager selectedTarget = null;

        // 전체 캐릭터 리스트에서 적 캐릭터 찾기
        List<CharacterManager> allCharacters = GameManager.Instance.GetAllCharacters();
        List<CharacterManager> enemies = allCharacters
            .Where(character => character.character.IsMine != characterManager.character.IsMine && character.character.IsAlive)
            .ToList();


        if (enemies.Count == 0)
        {
            Debug.Log("공격할 대상이 없습니다.");
            return null;
        }

        // 성향에 따른 타겟 선택
        float personalityDeviationChance = 0.15f; // 성향과 반대되는 행동을 할 확률
        bool deviateFromPersonality = UnityEngine.Random.value < personalityDeviationChance;

        switch (characterManager.character.personality)
        {
            case Personality.Simple:
                selectedTarget = enemies.FirstOrDefault();
                if (deviateFromPersonality)
                {
                    selectedTarget = enemies.OrderBy(enemy => enemy.character.FinalStats.CurrentHp).FirstOrDefault();
                }
                break;

            case Personality.Aggressive:
                selectedTarget = enemies.OrderBy(enemy => enemy.character.FinalStats.CurrentHp).FirstOrDefault();
                if (deviateFromPersonality)
                {
                    selectedTarget = enemies.FirstOrDefault();
                }
                break;

            case Personality.Cunning:
                selectedTarget = enemies.OrderBy(enemy => enemy.character.FinalStats.CurrentHp).FirstOrDefault();
                if (deviateFromPersonality)
                {
                    selectedTarget = enemies.OrderByDescending(enemy => enemy.character.FinalStats.PhysicalDefense).FirstOrDefault();
                }
                break;

            case Personality.Cautious:
                selectedTarget = enemies.Where(enemy => enemy.character.StatusEffects.Count > 0)
                    .OrderBy(enemy => enemy.character.FinalStats.CurrentHp).FirstOrDefault();
                if (selectedTarget == null || deviateFromPersonality)
                {
                    selectedTarget = enemies.OrderByDescending(enemy => enemy.character.FinalStats.CurrentHp).LastOrDefault();
                }
                break;

            default:
                selectedTarget = enemies[0];
                break;
        }

        return selectedTarget;
    }

    // 대응 스킬 지정 메서드
    private SkillBase AIChooseCounterSkill(SkillBase incomingSkill)
    {
        // 대응 스킬 로직 구현 (AI가 특정 스킬에 대응하는 방법)
        SkillBase counterSkill = characterManager.character.Skills
            .FirstOrDefault(skill => skill.IsCounterSkill && characterManager.character.FinalStats.CurrentStamina >= skill.StaminaCost);

        return counterSkill;
    }

    #endregion



    public List<(SkillBase skill, CharacterManager target)> GetSkillQueue()
    {
        return skillQueue;
    }
}
