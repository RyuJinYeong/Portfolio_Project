using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using PlayFab.ClientModels;
using PlayFab;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal.Internal;

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

    void Awake()
    {
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

        // 출신지 변경에 따른 외형 업데이트 - 직렬화를 이용한 깊은 복사 진행
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

        UpdateEquipmentEffect();

        characterManager.character.Name = characterNameInput.text;
        characterManager.character.customizationData = CustomInfo.customizationInfo; // 커스터마이징 정보 저장
        characterManager.character.Portrait = CustomInfo.characterCustom.CapturePortrait(); // 초상화 촬영용 렌더카메라로 초상화 촬영 후 Sprite로 변환하여 캐릭터 데이터에 저장
        characterManager.character.IsMine = true; //캐릭터 소유권 지정
        characterManager.character.UpdateFinalStats();
        characterManager.character.FinalStats.CurrentHp = characterManager.character.FinalStats.MaxHp;

        PlayerManager._instance.CreateCharacter(characterManager.character);
        PlayerManager._instance.AddCharacterID(characterManager.character.ID);
        PlayerManager._instance.SaveCharacterPosition(characterManager.character.ID, false); // 후열로 기본 설정
        characterManager.character.InitializeSkills();


        if (PlayerManager.Instance.GetCurrentPlayerData().activeCharacterIds == null)
        {
            PlayerManager.Instance.GetCurrentPlayerData().activeCharacterIds = new List<string>();
        }
        PlayerManager.Instance.GetCurrentPlayerData().activeCharacterIds.Add(characterManager.character.ID);

        Debug.Log("Character created and saved!");

        // 튜토리얼 씬으로 전환
        GameManager.Instance.LoadGameScene("Tutorial");
    }
}