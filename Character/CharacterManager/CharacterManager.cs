using SoftKitty.InventoryEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SoftKitty.InventoryEngine.InventoryHolder;

public class CharacterManager : MonoBehaviour
{
    public CharacterData character = new CharacterData();
    private DamageHandler damageHandler = new DamageHandler();
    private StatHandler statHandler = new StatHandler();
    public CombatHandler combatHandler;
    public CharacterUIHandler characterUIHandler;
    public BattlePresentationHandler battlePresentationHandler;

    public Transform characterPool;
    public bool isFront; // 캐릭터의 전열 여부를 나타내는 불린형 필드
    public bool isPlayerTurn; // 플레이어 턴 여부 확인
    public bool hasExtraTurn;

    public bool isInMeleeCombat = false;  // 경합 상태 여부
    public CharacterManager meleeTarget = null;  // 경합 중 타겟

    void Awake()
    {
        combatHandler = gameObject.AddComponent<CombatHandler>();
        battlePresentationHandler = GetComponent<BattlePresentationHandler>();

        if (battlePresentationHandler == null)
            battlePresentationHandler = gameObject.AddComponent<BattlePresentationHandler>();
    }

    public void Start() // UI 작동 테스트
    {
        if (characterUIHandler != null)
        {
            UpdateCharacterUI();
            characterUIHandler.FaceCamera();
        }
    }

    public CharacterManager(CharacterData characterData)
    {
        character = characterData;
        damageHandler = new DamageHandler();
        statHandler = new StatHandler();
    }

    #region 캐릭터 데이터 초기화, 스폰관련 로직 - 세부 기능 구현 필요

    // 캐릭터 데이터 초기화 메서드
    public void InitializeCharacter(CharacterData characterData, Camera portraitCamera = null, RenderTexture portraitRenderTexture = null)
    {
        character = characterData;
        damageHandler = new DamageHandler();
        statHandler = new StatHandler();

        /*
        // 3) 장비 효과 적용(EquipmentHolder의 장비를 실제 캐릭터에 Equip) ( 중복 적용으로 인해 주석처리 )
        if (character.CharacterEquipment != null)
        {
            var stacks = character.CharacterEquipment.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (!stacks[i].isEmpty() && stacks[i].Item is Equipment eq)
                    eq.Equip(character);
            }
        }

        
        //character.ApplyAllTraits(this);               // 필요 시*/

        // 4) 장비/특성 반영 후 계산
        EquipmentManager.UpdateAvailableAttributes(character);
        EquipmentManager.UpdateSkillAvailability(character);
        character.UpdateFinalStats();

        // 5) 외형 / 초상화 / 애니메이터 처리
        CharacterCustomization customization = GetComponent<CharacterCustomization>();
        
        if (customization != null)
        {
            customization.ApplyCustomization(character);
            customization.UpdateEquipmentAppearance(character);
        }

        battlePresentationHandler?.BindVisual(transform);

        // 6) UI 갱신
        UpdateCharacterUI();
    }

    public void SetDefaultCounterSkill(SkillDefinitionSO skill)
    {
        if (skill == null)
            return;

        if (skill.staminaCost + skill.mentalCost <= 1)
        {
            character.DefaultCounterSkill = skill.uid;
        }
        else
        {
            Debug.Log("기본 대응 스킬로 설정할 수 없습니다. (코스트 초과)");
        }
    }

    #endregion

    public void UpdateCharacterUI()
    {
        bool isAlive = character != null && character.IsAlive;

        Collider targetingCollider = GetComponent<Collider>();
        if (targetingCollider != null)
            targetingCollider.enabled = isAlive;

        if (characterUIHandler != null)
        {
            if (characterUIHandler.StatusCanvas != null)
                characterUIHandler.StatusCanvas.SetActive(isAlive);

            if (isAlive)
                characterUIHandler.UpdateUI();
        }

        UIManager.Instance?.RefreshCharacterInfo(this);
    }

    #region 전투 관련 로직 위임

    // 전투 핸들러에 턴 시작 전달
    public void StartTurn(System.Action onTurnEnd)
    {
        combatHandler.StartTurn(onTurnEnd);
    }

    // 스킬 선택 시 CombatHandler로 전달
    public void SelectSkill(SkillDefinitionSO skill, CharacterManager target, bool isConcealed = false)
    {
        combatHandler.SelectSkill(skill, target, isConcealed);
    }

    // 대응 스킬 선택 시 CombatHandler로 전달
    public void SelectCounterSkill(SkillDefinitionSO skill, CharacterManager target)
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
        return TakeDamage(damage, damageType, damageAttribute, false);
    }

    public int TakeDamage(
        int damage,
        SkillType damageType,
        SkillAttribute damageAttribute,
        bool suppressZeroDamagePopup)
    {
        int finalDamage = damageHandler.TakeDamage(character, damage, damageType, damageAttribute);

        if (!suppressZeroDamagePopup || finalDamage > 0)
        {
            BattlePresentationDirector director = BattlePresentationDirector.Instance;

            if (director == null ||
                !director.TryQueueDamagePopup(finalDamage, transform.position))
            {
                UIManager.Instance?.ShowDamage(finalDamage, transform.position);
            }
        }

        UpdateCharacterUI();
        return finalDamage;
    }

    //스킬큐 Getter 구현 - 명시적 접근제어
    public List<SkillQueueData> GetSkillQueue()
    {
        return combatHandler.GetSkillQueue();
    }

    #endregion

    #region Character Management

    // 리소스 회복 메서드 (지구력, 정신력 등)
    public void RecoverResources()
    {
        character.CurrentStamina = Mathf.Clamp(
            character.CurrentStamina + character.FinalStats.StaminaRecovery,
            0,
            character.FinalStats.MaxStamina);
        character.CurrentMentality = Mathf.Clamp(
            character.CurrentMentality + character.FinalStats.MentalityRecovery,
            0,
            character.FinalStats.MaxMentality);

        UpdateCharacterUI();  // 리소스 회복 후 UI 업데이트
    }

    public void RecoverOnKill(CharacterData defeated, bool wasAlive)
    {
        if (!wasAlive || defeated == null || defeated.IsAlive || defeated.IsMine == character.IsMine ||
            !character.IsAlive || character.CurrentHp <= 0) return;

        CharacterSpecialStats bonus = character.FinalSpecialStats;
        character.CurrentHp = Mathf.Clamp(character.CurrentHp + bonus.KillHpRecovery, 0, character.FinalStats.MaxHp);
        character.CurrentStamina = Mathf.Clamp(character.CurrentStamina + bonus.KillStaminaRecovery, 0, character.FinalStats.MaxStamina);
        character.CurrentMentality = Mathf.Clamp(character.CurrentMentality + bonus.KillMentalityRecovery, 0, character.FinalStats.MaxMentality);
        UpdateCharacterUI();
    }

    #endregion
}
