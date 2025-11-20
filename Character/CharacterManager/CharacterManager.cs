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

    public Transform characterPool;
    public bool isFront; // 캐릭터의 전열 여부를 나타내는 불린형 필드
    public bool isPlayerTurn; // 플레이어 턴 여부 확인
    public bool hasExtraTurn;

    public bool isInMeleeCombat = false;  // 경합 상태 여부
    public CharacterManager meleeTarget = null;  // 경합 중 타겟

    void Awake()
    {
        combatHandler = gameObject.AddComponent<CombatHandler>();        
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

        //characterData.Portrait = characterInstance.GetComponent<CharacterCustomization>().CapturePortrait(); // 초상화 촬영
    }

    // 캐릭터 데이터 초기화 메서드
    public void InitializeCharacter(CharacterData characterData)
    {
        character = characterData;
        damageHandler = new DamageHandler();
        statHandler = new StatHandler();

        // 1) 프리팹의 홀더 컴포넌트를 캐릭터 필드에 연결
        InventoryHolder[] holders = GetComponents<InventoryHolder>();
        foreach (var holder in holders)
        {
            if (holder.Type == InventoryHolder.HolderType.PlayerInventory)
                character.CharacterInventory = holder;
            else if (holder.Type == InventoryHolder.HolderType.PlayerEquipment)
                character.CharacterEquipment = holder;
        }

        // 2) 저장해 둔 스냅샷(JSON) → 실제 홀더로 복원 (순서: 연결 후 Import)
        if (!string.IsNullOrEmpty(character.InventoryJsonSnapshot))
            InventorySerializer.ImportJson(character.CharacterInventory, character.InventoryJsonSnapshot);

        if (!string.IsNullOrEmpty(character.EquipmentJsonSnapshot))
            InventorySerializer.ImportJson(character.CharacterEquipment, character.EquipmentJsonSnapshot);


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

        // 5) 외형 갱신(무기 등)
        GetComponent<CharacterCustomization>()?.UpdateEquipmentAppearance(character);
                
        character.Portrait = Resources.Load<Texture2D>("OriginIcon/"+character.originName);
        Debug.Log(Resources.Load<Texture2D>("OriginIcon/" + character.originName) + " " + character.originName + " 초상화 초기화");

        // 6) 스냅샷 비우기
        character.InventoryJsonSnapshot = null;
        character.EquipmentJsonSnapshot = null;

        // 7) UI 갱신
        UpdateCharacterUI();
    }

    public void SetDefaultCounterSkill(SkillBase skill)
    {
        if (skill.StaminaCost + skill.MentalCost == 1)
        {
            character.DefaultCounterSkill = skill;
        }
        else
        {
            Debug.Log("기본 대응 스킬로 설정할 수 없습니다. (코스트 초과)");
        }
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
        int finalDamage = damageHandler.TakeDamage(character, damage, damageType, damageAttribute);
        UIManager.Instance.ShowDamage(finalDamage, transform.position);
        return finalDamage;
    }

    //스킬큐 Getter 구현 - 명시적 접근제어
    public List<(SkillBase skill, CharacterManager target)> GetSkillQueue()
    {
        return combatHandler.GetSkillQueue();
    }

    #endregion

    #region Character Management

    // 리소스 회복 메서드 (지구력, 정신력 등)
    public void RecoverResources()
    {
        character.CurrentStamina += character.FinalStats.StaminaRecovery;
        character.CurrentMentality += character.FinalStats.MentalityRecovery;

        UpdateCharacterUI();  // 리소스 회복 후 UI 업데이트
    }

    #endregion
}
