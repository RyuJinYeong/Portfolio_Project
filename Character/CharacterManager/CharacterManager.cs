using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CharacterManager : MonoBehaviour
{
    public CharacterData character;
    private DamageHandler damageHandler;
    private StatHandler statHandler;

    public Transform characterPool;

    public bool isFront; // 캐릭터의 전열 여부를 나타내는 불린형 필드
    public bool isPlayerTurn; // 플레이어 턴 여부 확인

    public List<(SkillBase skill, CharacterManager target)> skillQueue = new List<(SkillBase skill, CharacterManager target)>();  // 시전할 스킬 큐
    public List<(SkillBase skill, CharacterManager target)> counterSkillQueue = new List<(SkillBase skill, CharacterManager target)>();  // 시전할 카운터 스킬 큐
    private Coroutine turnTimerCoroutine;

    public CharacterManager(CharacterData characterData)
    {
        character = characterData;
        damageHandler = new DamageHandler();
        statHandler = new StatHandler();
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

    private GameObject InstantiateCharacter(CharacterData characterData) // - 세부 기능 구현 필요
    {
        // 캐릭터를 풀에서 가져오거나 새로 생성
        GameObject characterInstance = Instantiate(GameManager.Instance.characterPrefab, characterPool);
        characterInstance.SetActive(false);
        return characterInstance;
    }

    public void ApplyCustomization(GameObject characterInstance, CharacterData characterData)
    {
        if(characterData.customizationData.IsMale)
            characterInstance.GetComponent<CharacterCustomization>().SetBodyType("Male");
        else
            characterInstance.GetComponent<CharacterCustomization>().SetBodyType("Female");

        characterInstance.GetComponent<CharacterCustomization>().SetHairColor(characterData.customizationData.HairColor);
        characterInstance.GetComponent<CharacterCustomization>().SetHairStyle(characterData.customizationData.HairStyle);
        characterInstance.GetComponent<CharacterCustomization>().SetSkinTone(characterData.customizationData.SkinTone);
        characterInstance.GetComponent<CharacterCustomization>().SetBodyType(characterData.customizationData.BodyType);

        // 캐릭터 오브젝트의 Skinned Mesh Renderer의 속성값도 지정해줘야함.

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

        // 추가적인 초기화 작업 (UI 업데이트, 스탯 적용 등)
        UpdateCharacterUI();
    }

    #endregion

    public void UpdateCharacterUI()
    {
        // UI 업데이트 로직을 여기에 추가 - 세부 구현 필요 or UIManager와 기능 통합 필요
    }

    #region 턴 관리 및 스킬 선택

    // 턴 시작 메서드
    public void StartTurn(System.Action onTurnEnd)
    {
        // 턴이 시작되면 선택된 캐릭터의 외곽선을 활성화하여 강조 표시 - 구현 필요
        //this.gameObject.GetComponent<OutlineEffect>().EnableOutline();
        isPlayerTurn = true;

        // 제한 시간 내에 턴을 종료하지 않으면 자동 종료
        turnTimerCoroutine = StartCoroutine(TurnTimer(onTurnEnd));

        ShowPlayerControlUI(onTurnEnd);
    }

    // 턴타이머
    private IEnumerator TurnTimer(System.Action onTurnEnd)
    {
        yield return new WaitForSeconds(60.0f); // 60초 제한 시간

        if (isPlayerTurn)
        {
            ExecuteSkillQueue(onTurnEnd); // 턴 종료 전 스킬 큐 실행            
            onTurnEnd();
        }
    }

    // 플레이어가 조작할 수 있는 UI 활성화 메서드
    private void ShowPlayerControlUI(System.Action onTurnEnd)
    {
        UIManager.Instance.DisplayCharacterInfo(this); // UI 정보 표시

        // 여기서 턴 종료 버튼 클릭 시 큐에 쌓인 스킬 발동
        //UIManager.Instance.SetEndTurnCallback(() => ExecuteSkillQueue(onTurnEnd)); // UIManager에서 턴 종료 버튼 콜백 메서드 구현 필요
    }

    // 스킬 선택 및 큐에 추가
    public void SelectSkill(SkillBase skill, CharacterManager target)
    {
        // 스킬을 리스트에 추가
        skillQueue.Add((skill, target));
        Debug.Log($"Skill {skill.SkillName} 큐에 추가");
    }

    // 플레이어가 대응 스킬 선택 후 큐에 추가
    public void SelectCounterSkill(SkillBase skill, CharacterManager target)
    {
        counterSkillQueue.Add((skill, target));
        Debug.Log($"Counter Skill {skill.SkillName} added to queue.");
    }

    // 스킬 큐 실행
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

    // 스킬 및 대응 스킬 실행
    private IEnumerator ExecuteSkills(System.Action onTurnEnd)
    {
        foreach (var item in skillQueue)
        {
            SkillBase skill = item.skill;
            CharacterManager target = item.target;
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

        return this; //GameManager.Instance.GetOpponent(this);
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

    // 스킬 사용 메서드
    public void UseSkill(SkillBase skill, CharacterManager target)
    {
        Debug.Log("Using skill: " + skill.SkillName);
        float damageMultiplier = skill.DamageMultiplier;
        int damage = 0;

        if (skill.IsOffHand)
        {
            damageMultiplier *= 0.9f;
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
    }


    #endregion

    #region Character Management

    // 데미지를 받는 메서드
    public int TakeDamage(int damage, SkillType damageType, SkillAttribute damageAttribute)
    {
        return damageHandler.TakeDamage(character, damage, damageType, damageAttribute);
    }

    // 스탯 포인트를 투자하는 메서드
    public void InvestStatPoint(string statName, int points)
    {
        statHandler.InvestStatPoint(character, statName, points);
    }

    #endregion
}