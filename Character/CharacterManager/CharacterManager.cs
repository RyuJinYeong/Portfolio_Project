using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public CharacterData character = new CharacterData();
    private DamageHandler damageHandler = new DamageHandler();
    private StatHandler statHandler = new StatHandler();
    public CombatHandler combatHandler;
    public CharacterUIHandler characterUIHandler;

    public Transform characterPool;
    public bool isFront; // 캐릭터의 전열 여부를 나타내는 불린형 필드
    public bool isPlayerTurn; // 플레이어 턴 여부 확인

    public bool isInMeleeCombat = false;  // 경합 상태 여부
    public CharacterManager meleeTarget = null;  // 경합 중 타겟

    void Awake()
    {
        combatHandler = gameObject.AddComponent<CombatHandler>();
    }
    /*
    private void Update()
    {
        UpdateCharacterUI();
        characterUIHandler.FaceCamera(); // UI 작동 테스트용
    }*/

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

    private GameObject InstantiateCharacter(CharacterData characterData)
    {
        GameObject characterInstance = new();
        // 캐릭터를 풀에서 가져오거나 새로 생성
        if (characterData.customizationData.IsMale)
            characterInstance = Instantiate(GameManager.Instance.characterPrefab_M, characterPool);
        else
            characterInstance = Instantiate(GameManager.Instance.characterPrefab_F, characterPool);

        characterInstance.SetActive(false);

        return characterInstance;
    }

    public void ApplyCustomization(GameObject characterInstance, CharacterData characterData)
    {
        if (characterData.customizationData.IsMale)
            characterInstance.GetComponent<CharacterCustomization>().SetBeard(characterData.customizationData.BeardType);

        characterInstance.GetComponent<CharacterCustomization>().SetHairStyle(characterData.customizationData.HairType);
        characterInstance.GetComponent<CharacterCustomization>().SetEyebrows(characterData.customizationData.EyebrowsType);
        characterInstance.GetComponent<CharacterCustomization>().SetEyes(characterData.customizationData.EyeType);
        characterInstance.GetComponent<CharacterCustomization>().SetMouth(characterData.customizationData.MouthType);

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
        characterUIHandler.UpdateUI();
    }

    #region 전투 관련 로직 위임

    // 전투 핸들러에 턴 시작 전달
    public void StartTurn(System.Action onTurnEnd)
    {
        combatHandler.StartTurn(onTurnEnd);
    }

    // 스킬 선택 시 CombatHandler로 전달
    public void SelectSkill(SkillBase skill, CharacterManager target)
    {
        combatHandler.SelectSkill(skill, target);
    }

    // 대응 스킬 선택 시 CombatHandler로 전달
    public void SelectCounterSkill(SkillBase skill, CharacterManager target)
    {
        combatHandler.SelectCounterSkill(skill, target);
    }

    // AI 턴 처리
    public IEnumerator HandleAITurn(System.Action onTurnEnd)
    {
        yield return combatHandler.HandleAITurn(onTurnEnd);
    }

    // 데미지 처리
    public int TakeDamage(int damage, SkillType damageType, SkillAttribute damageAttribute)
    {
        return damageHandler.TakeDamage(character, damage, damageType, damageAttribute);
    }

    //스킬큐 Getter 구현 - 명시적 접근제어
    public List<(SkillBase skill, CharacterManager target)> GetSkillQueue()
    {
        return combatHandler.GetSkillQueue();
    }

    #endregion

    #region Character Management

    // 스탯 포인트 투자
    public void InvestStatPoint(string statName, int points)
    {
        statHandler.InvestStatPoint(this, statName, points);
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
