using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterManager : MonoBehaviour
{
    public CharacterData character;
    private DamageHandler damageHandler;
    private StatHandler statHandler;

    public Transform characterPool;

    public bool isFront; // 캐릭터의 전열 여부를 나타내는 불린형 필드
    private bool isPlayerTurn; // 플레이어 턴 여부 확인

    public CharacterManager(CharacterData characterData)
    {
        character = characterData;
        damageHandler = new DamageHandler();
        statHandler = new StatHandler();
    }

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

    public void UpdateCharacterUI()
    {
        // UI 업데이트 로직을 여기에 추가 - 세부 구현 필요 or UIManager와 기능 통합 필요
    }

    #region Turn and Battle Management

    // 턴 시작 메서드
    public void StartTurn(System.Action onTurnEnd)
    {
        if (IsPlayerControlled())
        {
            isPlayerTurn = true;
            ShowPlayerControlUI(onTurnEnd);
        }
        else
        {
            isPlayerTurn = false;
            StartCoroutine(HandleAITurn(onTurnEnd));
        }
    }

    // 플레이어가 조작할 수 있는 UI 활성화 메서드
    private void ShowPlayerControlUI(System.Action onTurnEnd)
    {
        // UI를 활성화하고 플레이어가 스킬을 선택하거나 행동할 수 있도록 처리
        // 예시: UIManager.Instance.ShowSkillSelection(this, onTurnEnd);
        UIManager.Instance.DisplayCharacterInfo(this); // 예시용 구문 onTurnEnd 관련 처리 필요
    }

    // AI 턴을 처리하는 코루틴
    private IEnumerator HandleAITurn(System.Action onTurnEnd)
    {
        // AI 로직 구현 (간단한 예시)
        yield return new WaitForSeconds(1.0f); // AI가 생각하는 시간을 기다림
        UseSkill(AIChooseSkill(), FindTargetForAI());
        onTurnEnd();  // 턴 종료 콜백 호출
    }

    // AI가 사용할 스킬을 선택하는 메서드
    private SkillBase AIChooseSkill()
    {
        // 간단한 로직으로 AI가 사용할 스킬 선택
        return character.Skills[0];
    }

    // AI가 공격할 대상을 선택하는 메서드
    private CharacterManager FindTargetForAI()
    {
        // 간단한 로직으로 AI가 공격할 대상 선택
        return this; //GameManager.Instance.GetOpponent(this);
    }

    // 리소스 회복 메서드 (지구력, 정신력 등)
    private void RecoverResources()
    {
        character.FinalStats.CurrentStamina = Mathf.Min(character.FinalStats.MaxStamina,
            character.FinalStats.CurrentStamina + character.FinalStats.StaminaRecovery);

        character.FinalStats.CurrentMentality = Mathf.Min(character.FinalStats.MaxMentality,
            character.FinalStats.CurrentMentality + character.FinalStats.MentalityRecovery);

        UpdateCharacterUI();  // 리소스 회복 후 UI 업데이트
    }

    // 상태이상 적용 메서드
    private void ApplyStatusEffects()
    {
        /*
        foreach (var statusEffect in character.StatusEffects.ToList())
        {
            statusEffect.ApplyEffect(character);

            if (statusEffect.IsExpired())
            {
                character.StatusEffects.Remove(statusEffect);
            }
        }
        */

        UpdateCharacterUI();  // 상태이상 적용 후 UI 업데이트
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

    // 데미지를 받는 메서드
    public int TakeDamage(int damage, SkillType damageType, SkillAttribute damageAttribute)
    {
        return damageHandler.TakeDamage(character, damage, damageType, damageAttribute);
    }

    #endregion

    #region Character Management

    // 스탯 포인트를 투자하는 메서드
    public void InvestStatPoint(string statName, int points)
    {
        statHandler.InvestStatPoint(character, statName, points);
    }

    // 플레이어가 조작할 수 있는지 여부를 판단하는 메서드
    private bool IsPlayerControlled()
    {
        // 예시: PlayerManager에서 플레이어가 조작하는 캐릭터인지 확인
        return this.character.IsMine; //PlayerManager.Instance.IsPlayerCharacter(this);
    }

    #endregion
}