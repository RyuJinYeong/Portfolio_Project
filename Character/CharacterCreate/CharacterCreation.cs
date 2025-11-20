using PlayFab;
using PlayFab.ClientModels;
using SoftKitty.InventoryEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterCreation : MonoBehaviour
{
    public TMP_InputField characterNameInput;
    public TMP_Dropdown originDropdown;
    public TextMeshProUGUI selectedOriginInfo;
    public TextMeshProUGUI[] baseStats;
    public TextMeshProUGUI[] baseTraits;
    public CharacterManager characterManager; 
    public int traitPoint = 5;
    public TextMeshProUGUI traitPointText;
    public GameObject ConfirmWindow;
    public CharacterCustomizationUI CustomInfo;
    public Image originImage;

    private string selectedOrigin;
    private Dictionary<string, CharacterData> originDataDictionary;

    public TraitSelectionUI traitSelectionUI;

    public void Awake()
    {
        DatabaseBootstrapper.EnsureInitialized();

        originDataDictionary = CharacterOrigin.GetOriginData();
        PopulateDropdown();
        originDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(originDropdown); });
        originDropdown.value = 0;
        selectedOrigin = originDropdown.options[0].text;
        UpdateSelectedOriginInfo();

        CustomInfo.isMale = true;

        // CharacterManager 초기화
        characterManager = GetCharacterManager();
        characterManager.character = DeepCopy.DeepCopyCharacter(originDataDictionary.GetValueOrDefault(selectedOrigin));
        characterManager.GetComponent<CharacterCustomization>().UpdateWeaponAppearance(characterManager.character);

        traitSelectionUI = GetComponent<TraitSelectionUI>();
        traitSelectionUI.SetTraitPoints(traitPoint);

        ChangeUIValue();
    }

    public CharacterManager GetCharacterManager()
    {
        if (CustomInfo.isMale)
        {
            return CustomInfo.character_M.GetComponent<CharacterManager>();
        }
        else
        {
            return CustomInfo.character_F.GetComponent<CharacterManager>();
        }
    }

    void PopulateDropdown()
    {
        List<string> options = new List<string>(originDataDictionary.Keys);
        originDropdown.AddOptions(options);
    }

    void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        //characterManager.character.RemoveAllTraits(); // Trait 초기화
        originDataDictionary = CharacterOrigin.GetOriginData();

        int index = dropdown.value;
        selectedOrigin = originDropdown.options[index].text;
        UpdateSelectedOriginInfo();
        ChangeUIValue();

        if (index == 6)
            traitPoint = 10;
        else
            traitPoint = 5;

        traitPointText.text = traitPoint.ToString();

        traitSelectionUI.ResetTraitSelection();
        traitSelectionUI.SetTraitPoints(traitPoint);

        // 출신지 변경에 따른 캐릭터 모델 업데이트 + 직렬화를 이용한 깊은 복사 진행
        characterManager.character = DeepCopy.DeepCopyCharacter(originDataDictionary.GetValueOrDefault(selectedOrigin));
        characterManager.GetComponent<CharacterCustomization>().UpdateWeaponAppearance(characterManager.character);
    }

    void ChangeUIValue()
    {
        characterManager.character.RemoveAllTraits(characterManager);
        originDataDictionary = CharacterOrigin.GetOriginData();

        if (originDataDictionary.TryGetValue(selectedOrigin, out CharacterData originData))
        {
            characterManager.character = originData;

            if (selectedOrigin == "마법사") // 랜덤 기능이 들어간 특성을 미리 활성화시키기 위해서 따로 특정지어 특성을 선택 단계에서 적용
            {
                characterManager.character.Traits[0].ApplyTrait(characterManager);
            }

            characterManager.character.ApplyAllTraits(characterManager);
            characterManager.character.UpdateFinalStats();

            UIupdate();
        }
    }

    public void UIupdate()
    {
        traitPoint = traitSelectionUI.availableTraitPoints;

        for (int i = 0; i < 12; i++)
            baseTraits[i].text = "";
        for (int i = 0; i < characterManager.character.Traits.Count; i++)
            baseTraits[i].text = characterManager.character.Traits[i].Name.ToString();

        traitPointText.text = traitPoint.ToString();
        UpdateStatsUI();
    }

    void UpdateSelectedOriginInfo()
    {
        if (!string.IsNullOrEmpty(selectedOrigin))
        {
            selectedOriginInfo.text = $"{selectedOrigin}\n";
        }
    }

    void UpdateStatsUI()
    {
        baseStats[0].text = characterManager.character.FinalStats.Strength.ToString();
        baseStats[1].text = characterManager.character.FinalStats.Dexterity.ToString();
        baseStats[2].text = characterManager.character.FinalStats.Speed.ToString();
        baseStats[3].text = characterManager.character.FinalStats.Intelligence.ToString();
        baseStats[4].text = characterManager.character.FinalStats.Wisdom.ToString();
        baseStats[5].text = characterManager.character.FinalStats.Health.ToString();
        baseStats[6].text = characterManager.character.FinalStats.Detection.ToString();
        baseStats[7].text = characterManager.character.FinalStats.Insight.ToString();
        baseStats[8].text = characterManager.character.FinalStats.MaxHp.ToString();
        baseStats[9].text = characterManager.character.FinalStats.Endurance.ToString();
    }

    public void UpdateEquipmentEffect()
    {
        List<Equipment> equips = (List<Equipment>)characterManager.character.GetEquipments();

        foreach (Equipment equip in equips)
        {
            if (equip is not null)
            {
                equip.Equip(characterManager.character);
            }
        }
    }

    public void OnCreateCharacterButtonPressed()
    {
        if (string.IsNullOrEmpty(characterNameInput.text))
        {
            Debug.LogWarning("Character name is required!");
            ConfirmWindow.SetActive(true);
            return;
        }

        // ① 프리뷰 오브젝트에 붙은 홀더를 캐릭터데이터에 연결
        BindHoldersToCharacterData();

        // ② 오리진 장비를 실제 '장비창 홀더'에 한 번 집어넣음
        SeedOriginEquipmentsIntoHolders();

        // ③ 이후엔 기존 흐름 그대로: 장비 효과 적용 + 커스텀/초상화/스탯 갱신 → 저장 
        UpdateEquipmentEffect();

        characterManager.character.Name = characterNameInput.text;
        characterManager.character.customizationData = CustomInfo.customizationInfo; // 커스터마이징 정보 저장
        //characterManager.character.Portrait = CustomInfo.characterCustom.CapturePortrait(); // 초상화 촬영용 렌더카메라로 초상화 촬영 후 Sprite로 변환하여 캐릭터 데이터에 저장
        characterManager.character.IsMine = true; //캐릭터 소유권 지정
        characterManager.character.UpdateFinalStats();
        characterManager.character.CurrentHp = characterManager.character.FinalStats.MaxHp;

        PlayerManager._instance.CreateCharacter(characterManager.character);
        PlayerManager._instance.AddCharacterID(characterManager.character.ID);
        PlayerManager._instance.SaveCharacterPosition(characterManager.character.ID, false); // 후열로 기본 설정


        if (PlayerManager.Instance.GetCurrentPlayerData().activeCharacterIds == null)
        {
            PlayerManager.Instance.GetCurrentPlayerData().activeCharacterIds = new List<string>();
        }
        PlayerManager.Instance.GetCurrentPlayerData().activeCharacterIds.Add(characterManager.character.ID);

        Debug.Log("Character created and saved!");

        PlayerManager.Instance.GetCurrentPlayerData().currentStage = "Town";

        // 마을 씬으로 전환
        GameManager.Instance.LoadGameScene("Town");
    }

    public void OnClickPlayButton()
    {
        GameManager.Instance.LoadGameScene(PlayerManager.Instance.GetCurrentPlayerData().currentStage);
    }

    // 프리뷰 캐릭터(남/여)의 InventoryHolder를 캐릭터데이터에 연결
    void BindHoldersToCharacterData()
    {
        var inv = default(InventoryHolder);
        var eq = default(InventoryHolder);

        var holders = characterManager.GetComponents<InventoryHolder>();
        foreach (var h in holders)
        {
            if (h.Type == InventoryHolder.HolderType.PlayerInventory) inv = h;
            else if (h.Type == InventoryHolder.HolderType.PlayerEquipment) eq = h;
        }

        characterManager.character.CharacterInventory = inv;
        characterManager.character.CharacterEquipment = eq;
    }

    // 오리진 장비(Equipment 필드)에 들어있는 것들을 실제 장비창 홀더에 넣어두기
    void SeedOriginEquipmentsIntoHolders()
    {
        var eqHolder = characterManager.character.CharacterEquipment;
        if (eqHolder == null) return;

        var equips = (List<Equipment>)characterManager.character.GetEquipments();
        if (equips == null) return;

        foreach (var equipment in equips)
        {
            if (equipment == null) continue;
            // 장비창에 실제 스택으로 추가
            var res = eqHolder.AddItem(equipment, 1);

            // 에셋 쪽 UI/링크 갱신
            var changed = new Dictionary<Item, int> { { equipment, 1 } };
            eqHolder.ItemChanged(changed);
        }
    }
}