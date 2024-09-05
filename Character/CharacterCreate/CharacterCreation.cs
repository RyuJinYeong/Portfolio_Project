using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using PlayFab.ClientModels;
using PlayFab;
using UnityEngine.SceneManagement;

public class CharacterCreation : MonoBehaviour
{
    public TMP_InputField characterNameInput;
    public TMP_Dropdown originDropdown; // Dropdown UI 요소    
    public TextMeshProUGUI selectedOriginInfo; // 선택된 출신지 정보 표시할 UI 텍스트
    public TextMeshProUGUI[] baseStats;
    public TextMeshProUGUI[] baseTraits;
    public CharacterData selectOrigin;
    public int traitPoint = 5;
    public TextMeshProUGUI traitPointText;
    public GameObject ConfirmWindow;
    public CharacterCustomizationUI CustomInfo;
    public Image originImage;

    private string selectedOrigin; // 현재 선택된 출신지
    private Dictionary<string, CharacterData> originDataDictionary; // 출신지 데이터 딕셔너리

    public TraitSelectionUI traitSelectionUI; // TraitSelectionUI 참조

    void Start()
    {        
        originDataDictionary = CharacterOrigin.GetOriginData();
        PopulateDropdown();
        originDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(originDropdown); });
        originDropdown.value = 0; // 기본 선택된 드롭다운 목록을 0번으로 설정
        selectedOrigin = originDropdown.options[0].text;
        UpdateSelectedOriginInfo();

        selectOrigin = originDataDictionary.GetValueOrDefault(selectedOrigin);

        traitSelectionUI = GetComponent<TraitSelectionUI>(); // TraitSelectionUI 스크립트 참조
        traitSelectionUI.SetTraitPoints(traitPoint); // 특성포인트 초기화

        ChangeUIValue();
    }

    void PopulateDropdown()
    {
        List<string> options = new List<string>(originDataDictionary.Keys);
        originDropdown.AddOptions(options);
    }

    void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        selectOrigin.RemoveAllTraits();
        originDataDictionary = CharacterOrigin.GetOriginData();
        
        int index = dropdown.value;
        selectedOrigin = originDropdown.options[index].text;
        UpdateSelectedOriginInfo();
        ChangeUIValue();
        if (index == 6) // 7번째 드롭다운 목록을 선택할때만 10포인트
            traitPoint = 10;
        else
            traitPoint = 5;
        traitPointText.text = traitPoint.ToString();
        
        // TraitSelectionUI 초기화
        traitSelectionUI.ResetTraitSelection();
        traitSelectionUI.SetTraitPoints(traitPoint);
        //traitSelectionUI.SetCharacterData(selectOrigin);
    }

    void ChangeUIValue() // 선택된 드롭다운 목록에 해당되는 출신지 정보로 UI 정보 초기화
    {
        if (originDataDictionary.TryGetValue(selectedOrigin, out CharacterData originData))
        {
            // selectOrigin 초기화
            selectOrigin = originData;
            if(selectedOrigin == "마법사") // 랜덤 기능이 들어간 특성을 미리 활성화시키기 위해서 따로 특정지어 특성을 선택 단계에서 적용
            {
                selectOrigin.Traits[0].ApplyTrait(selectOrigin);
            }
            selectOrigin.ApplyAllTraits();
            selectOrigin.UpdateFinalStats();

            UIupdate();
        }
    }

    public void UIupdate()
    {
        traitPoint = traitSelectionUI.availableTraitPoints;

        // UI 업데이트
        for (int i = 0; i < 12; i++)
            baseTraits[i].text = "";
        for (int i = 0; i < selectOrigin.Traits.Count; i++)
            baseTraits[i].text = selectOrigin.Traits[i].Name.ToString();

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
        baseStats[0].text = selectOrigin.FinalStats.Strength.ToString();
        baseStats[1].text = selectOrigin.FinalStats.Dexterity.ToString();
        baseStats[2].text = selectOrigin.FinalStats.Speed.ToString();
        baseStats[3].text = selectOrigin.FinalStats.Intelligence.ToString();
        baseStats[4].text = selectOrigin.FinalStats.Wisdom.ToString();
        baseStats[5].text = selectOrigin.FinalStats.Health.ToString();
        baseStats[6].text = selectOrigin.FinalStats.Detection.ToString();
        baseStats[7].text = selectOrigin.FinalStats.Insight.ToString();
        baseStats[8].text = selectOrigin.FinalStats.MaxHp.ToString();
        baseStats[9].text = selectOrigin.FinalStats.Endurance.ToString();
    }

    #region 캐릭터생성,플레이팹 데이터 저장 구현부

    public void OnCreateCharacterButtonPressed()
    {
        if (string.IsNullOrEmpty(characterNameInput.text))
        {
            Debug.LogWarning("Character name is required!");
            ConfirmWindow.SetActive(true);
            return;
        }

        selectOrigin.Name = characterNameInput.text;
        selectOrigin.customizationData = CustomInfo.customizationInfo; // 커스터마이징 정보 저장
        selectOrigin.Portrait = CustomInfo.characterCustom.CapturePortrait(); // 초상화 촬영용 렌더카메라로 초상화 촬영 후 Sprite로 변환하여 캐릭터 데이터에 저장
        selectOrigin.IsMine = true; //캐릭터 소유권 지정

        PlayerManager._instance.CreateCharacter(selectOrigin);
        PlayerManager._instance.AddCharacterID(selectOrigin.ID);
        PlayerManager._instance.SaveCharacterPosition(selectOrigin.ID, false); // 후열로 기본 설정
        selectOrigin.InitializeSkills();

        
        if (PlayerManager.Instance.GetCurrentPlayerData().activeCharacterIds == null)
        {
            PlayerManager.Instance.GetCurrentPlayerData().activeCharacterIds = new List<string>();
        }
        PlayerManager.Instance.GetCurrentPlayerData().activeCharacterIds.Add(selectOrigin.ID);

        Debug.Log("Character created and saved!");

        // 튜토리얼 모드로 전환
        GameManager.Instance.LoadGameScene("Tutorial");
    }
    #endregion
}
