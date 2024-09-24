using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public CharacterData character;
    private DamageHandler damageHandler;
    private StatHandler statHandler;

    private SynergyManager synergyManager;
    private List<SynergyEffect> activeSynergyEffects; // 활성화 되어있는 시너지 효과 리스트

    public Transform characterPool;

    public bool isFront; // 캐릭터의 전열 여부를 나타내는 불린형 필드
    public bool isPlayerTurn; // 플레이어 턴 여부 확인

    public bool isInMeleeCombat = false;  // 경합 상태 여부
    public CharacterManager meleeTarget = null;  // 경합 중 타겟

    public List<(SkillBase skill, CharacterManager target)> skillQueue = new List<(SkillBase skill, CharacterManager target)>();  // 시전할 스킬 큐
    public List<(SkillBase skill, CharacterManager target)> counterSkillQueue = new List<(SkillBase skill, CharacterManager target)>();  // 시전할 카운터 스킬 큐
    private Coroutine turnTimerCoroutine;
    public void Start()
    {
        damageHandler = new DamageHandler();
        statHandler = new StatHandler();
        synergyManager = new SynergyManager();
        activeSynergyEffects = new List<SynergyEffect>(); // 시너지 효과 저장 리스트
    }

    public CharacterManager(CharacterData characterData)
    {
        character = characterData;
        damageHandler = new DamageHandler();
        statHandler = new StatHandler();
        synergyManager = new SynergyManager();
        activeSynergyEffects = new List<SynergyEffect>();
    }

    #region 캐릭터 데이터 초기화, 스폰관련 로직 - 세부 기능 구현 필요

    // 캐릭터 프리로드 및 초상화 촬영
    public void LoadCharacters(List<CharacterData> characters)
    {
        foreach (var characterData in characters)
        {
            GameObject characterInstance = InstantiateCharacter(characterData);

            // 커스터마이징 적용 && 초상화 촬영
            ApplyCustomization(characterInstance, characterData);
        }
    }

    private GameObject InstantiateCharacter(CharacterData characterData)
    {
        // 캐릭터를 풀에서 가져오거나 새로 생성
        GameObject characterInstance = Instantiate(GameManager.Instance.characterPrefab, characterPool);
        characterInstance.SetActive(false);
        return characterInstance;
    }

    public void ApplyCustomization(GameObject characterInstance, CharacterData characterData)
    {
        if (characterData.customizationData.IsMale)
            characterInstance.GetComponent<CharacterCustomization>().SetBodyType("Male");
        else
            characterInstance.GetComponent<CharacterCustomization>().SetBodyType("Female");

        characterInstance.GetComponent<CharacterCustomization>().SetHairColor(characterData.customizationData.HairColor);
        characterInstance.GetComponent<CharacterCustomization>().SetHairStyle(characterData.customizationData.HairStyle);
        characterInstance.GetComponent<CharacterCustomization>().SetSkinTone(characterData.customizationData.SkinTone);
        characterInstance.GetComponent<CharacterCustomization>().SetBodyType(characterData.customizationData.BodyType);

        characterData.Portrait = characterInstance.GetComponent<CharacterCustomization>().CapturePortrait(); // 초상화 촬영
    }

    // 캐릭터 데이터 초기화 메서드
    public void InitializeCharacter(CharacterData characterData)
    {
        character = characterData;
        damageHandler = new DamageHandler();
        statHandler = new StatHandler();
        characterData.UpdateFinalStats(); // 캐릭터 스탯 초기화
        characterData.InitializeSkills(); // 스킬 아이콘 초기화
        EquipmentManager.UpdateAvailableAttributes(characterData); // 캐릭터 장비 세부속성 초기화
        EquipmentManager.UpdateSkillAvailability(characterData); // 장비 세부 속성에 따른 사용 가능 스킬 초기화

        UpdateCharacterUI();
    }

    #endregion

    public void UpdateCharacterUI()
    {
        // UI 업데이트 로직 (필요 시 추가)
    }

    #region 턴 관리 및 스킬 선택

    // 플레이어가 조작할 수 있는 UI 활성화 메서드
    private void ShowPlayerControlUI(System.Action onTurnEnd)
    {
        UIManager.Instance.DisplayCharacterInfo(this); // UI 정보 표시
    }

    // 턴 시작 메서드
    public void StartTurn(System.Action onTurnEnd)
    {
        isPlayerTurn = true;
        turnTimerCoroutine = StartCoroutine(TurnTimer(onTurnEnd));
        ShowPlayerControlUI(onTurnEnd);
    }

    // 턴 타이머
    private IEnumerator TurnTimer(System.Action onTurnEnd)
    {
        yield return new WaitForSeconds(60.0f); // 60초 제한 시간

        if (isPlayerTurn)
        {
            ExecuteSkillQueue(onTurnEnd); // 턴 종료 전 스킬 큐 실행            
            onTurnEnd();
        }
    }

    // 스킬 선택 및 큐에 추가 (리소스 소모 적용 및 경합 상태 처리)
    public void SelectSkill(SkillBase skill, CharacterManager target)
    {
        if (isInMeleeCombat && skill.IsRangedSkill)
        {
            Debug.Log("경합 상태에서는 원거리 스킬을 사용할 수 없습니다.");
            return;
        }

        // 스킬 사용 시 리소스 체크 및 차감
        if (character.FinalStats.CurrentStamina < skill.StaminaCost || character.FinalStats.CurrentMentality < skill.MentalCost)
        {
            Debug.Log("리소스가 부족하여 스킬을 사용할 수 없습니다.");
            return;
        }

        // 리소스 차감
        character.FinalStats.CurrentStamina -= skill.StaminaCost;
        character.FinalStats.CurrentMentality -= skill.MentalCost;

        // 근거리 스킬 사용 시 경합 상태로 전환
        if (!skill.IsRangedSkill)
        {
            isInMeleeCombat = true;
            meleeTarget = target;
        }

        skillQueue.Add((skill, target));
        Debug.Log($"Skill {skill.SkillName} added to queue. Stamina: {character.FinalStats.CurrentStamina}, Mentality: {character.FinalStats.CurrentMentality}");

        // 선택된 스킬이 추가된 후 시너지 조건 체크
        List<string> activeSynergies = synergyManager.GetActiveSynergies(skillQueue.Select(s => s.skill).ToList());

        // 활성화된 시너지를 UI에 표시
        UIManager.Instance.UpdateSynergyUI(activeSynergies);

        // 시너지 효과 적용 (큐 전체에 대해 중복 가능)
        activeSynergyEffects = synergyManager.ApplySynergies(skillQueue.Select(s => s.skill).ToList());
    }

    // 플레이어가 대응 스킬 선택 후 큐에 추가
    public void SelectCounterSkill(SkillBase skill, CharacterManager target)
    {
        counterSkillQueue.Add((skill, target));
        Debug.Log($"Counter Skill {skill.SkillName} added to queue.");
    }

    #endregion

    #region 경합 상태에서 적 처치 시 스킬 취소 및 리소스 반환

    // 경합 중 적 처치 시 스킬 큐 취소 및 리소스 반환
    public void HandleEnemyDefeated()
    {
        if (isInMeleeCombat && meleeTarget != null && !meleeTarget.character.IsAlive)
        {
            Debug.Log($"{meleeTarget.character.Name} 처치 성공. 스킬 큐 취소 및 리소스 반환.");
            CancelRemainingSkills();
        }

        // 경합 상태 해제
        isInMeleeCombat = false;
        meleeTarget = null;
    }

    // 남은 스킬 취소 및 리소스 반환
    private void CancelRemainingSkills()
    {
        foreach (var item in skillQueue)
        {
            SkillBase skill = item.skill;
            // 리소스 일부 반환 (예: 50%) - 해당 필드도 변수화시켜서 관리 시 반환 값에 변주를 줄 수 있으니 필요시 추후 개선필요
            character.FinalStats.CurrentStamina += (int)(skill.StaminaCost * 0.5f);
            character.FinalStats.CurrentMentality += (int)(skill.MentalCost * 0.5f);
        }

        skillQueue.Clear();
        Debug.Log($"Remaining skills canceled. Stamina: {character.FinalStats.CurrentStamina}, Mentality: {character.FinalStats.CurrentMentality}");
    }

    #endregion

    #region 스킬 사용 메서드

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
            if (isInMeleeCombat && skill.IsRangedSkill)
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
        Debug.Log($"Using skill: {skill.SkillName} on {target.character.Name}");
        float damageMultiplier = skill.DamageMultiplier;
        int damage = 0;
        
        if (skill.IsOffHand)
        {
            damageMultiplier *= 0.9f;  // 보조 무기 패널티 적용
        }

        switch (skill.Type)
        {
            case SkillType.Physical:
                damage = Mathf.FloorToInt(character.FinalStats.PhysicalAttack * damageMultiplier);
                break;
            case SkillType.Magical:
                damage = Mathf.FloorToInt(character.FinalStats.MagicalAttack * damageMultiplier);
                break;
        }

        int finalDamage = target.TakeDamage(damage, skill.Type, skill.Attribute);

        if (skill is IEvolvableSkill evolvableSkill)
        {
            evolvableSkill.OnSkillUsed(finalDamage, character, target.character);
        }

        // 상대가 사망했는지 확인 후 처리
        if (!target.character.IsAlive)
        {
            HandleEnemyDefeated();
        }
    }

    #endregion


    #region AI 행동 처리 메서드

    // AI 턴 처리
    private IEnumerator HandleAITurn(System.Action onTurnEnd)
    {
        // AI 로직 구현 필요
        yield return new WaitForSeconds(1.0f); // AI 대기 시간
        UseSkill(AIChooseSkill(), FindTargetForAI());
        onTurnEnd();  // 턴 종료 콜백 호출
    }

    // AI 사용스킬 지정 메서드
    private SkillBase AIChooseSkill()
    {
        switch (character.personality) // AI 행동양식 제어
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
        return character.Skills[0];
    }

    // AI 공격대상 지정
    private CharacterManager FindTargetForAI()
    {
        // 로직으로 AI가 공격할 대상 선택
        switch (character.personality) // 성향 별 AI 행동양식 제어
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

        return this; // 기본적으로 자신을 반환하거나, GameManager에서 적을 가져오도록 구현 가능
    }

    #endregion

    #region Character Management

    // 데미지 받기
    public int TakeDamage(int damage, SkillType damageType, SkillAttribute damageAttribute)
    {
        return damageHandler.TakeDamage(character, damage, damageType, damageAttribute);
    }

    // 스탯 포인트 투자
    public void InvestStatPoint(string statName, int points)
    {
        statHandler.InvestStatPoint(character, statName, points);
    }

    // 리소스 회복 메서드 (지구력, 정신력 등)
    public void RecoverResources()
    {
        character.FinalStats.CurrentStamina = Mathf.Min(character.FinalStats.MaxStamina,
            character.FinalStats.CurrentStamina + character.FinalStats.StaminaRecovery);

        character.FinalStats.CurrentMentality = Mathf.Min(character.FinalStats.MaxMentality,
            character.FinalStats.CurrentMentality + character.FinalStats.MentalityRecovery);

        UpdateCharacterUI();  // 리소스 회복 후 UI 업데이트
    }

    #endregion
}
