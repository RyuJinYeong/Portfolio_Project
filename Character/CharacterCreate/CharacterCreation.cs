using SoftKitty.InventoryEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreation : MonoBehaviour
{
    [Header("Step Panels")]
    public GameObject customizationPanel;
    public GameObject characterSetupPanel;

    [Header("Camera Focus Keys")]
    public string customizationCameraKey = "CharacterCloseUp";
    public string setupCameraKey = "CharacterFullBody";

    [Header("Setup UI")]
    public TMP_InputField characterNameInput;
    public TMP_Dropdown originDropdown;
    public TextMeshProUGUI selectedOriginInfo;
    public TextMeshProUGUI[] baseStats;
    public TextMeshProUGUI[] baseTraits;
    public TextMeshProUGUI traitPointText;
    public GameObject ConfirmWindow;
    public Image originImage;

    [Header("References")]
    public CharacterManager characterManager;
    public CharacterCustomizationUI CustomInfo;
    public TraitSelectionUI traitSelectionUI;

    [Header("Trait Point")]
    public int traitPoint = 5;

    private string selectedOrigin;
    private Dictionary<string, CharacterData> originDataDictionary;

    public void Awake()
    {
        DatabaseBootstrapper.EnsureInitialized();
        ResetCharacterCreationState();

        originDataDictionary = CharacterOrigin.GetOriginData();
        traitSelectionUI = GetComponent<TraitSelectionUI>();

        PopulateDropdown();

        originDropdown.onValueChanged.RemoveAllListeners();
        originDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(originDropdown); });

        originDropdown.value = 0;
        selectedOrigin = originDropdown.options[0].text;

        if (CustomInfo != null)
            CustomInfo.isMale = true;

        ApplyTraitPointByOriginIndex(originDropdown.value);

        if (traitSelectionUI != null)
            traitSelectionUI.SetTraitPoints(traitPoint);

        UpdateSelectedOriginInfo();
        OpenCustomizationPanel();
    }

    public CharacterManager GetCharacterManager()
    {
        if (CustomInfo != null && CustomInfo.isMale)
            return CustomInfo.character_M.GetComponent<CharacterManager>();

        return CustomInfo.character_F.GetComponent<CharacterManager>();
    }

    #region Step Flow
    public void OpenCustomizationPanel()
    {
        if (customizationPanel != null)
            customizationPanel.SetActive(true);

        if (characterSetupPanel != null)
            characterSetupPanel.SetActive(false);
    }

    public void OpenCharacterSetupPanel()
    {
        if (customizationPanel != null)
            customizationPanel.SetActive(false);

        if (characterSetupPanel != null)
            characterSetupPanel.SetActive(true);

        RefreshCharacterBySelectedOrigin(true);
    }

    public void OnClickBackToLobby()
    {
        ResetCharacterCreationState();
        gameObject.SetActive(false);
    }
    private void ResetCharacterCreationState()
    {
        // 1. 커스터마이징 데이터 초기화
        if (CustomInfo != null)
        {
            CustomInfo.ResetCustomization();
        }

        // 2. 이름 입력 초기화
        if (characterNameInput != null)
        {
            characterNameInput.text = "";
        }

        // 3. 출신지 드롭다운 초기화
        if (originDropdown != null && originDropdown.options.Count > 0)
        {
            originDropdown.value = 0;
            selectedOrigin = originDropdown.options[0].text;
        }

        // 4. 특성 선택 초기화
        ApplyTraitPointByOriginIndex(originDropdown != null ? originDropdown.value : 0);

        if (traitSelectionUI != null)
        {
            traitSelectionUI.ResetTraitSelection();
            traitSelectionUI.SetTraitPoints(traitPoint);
        }

        // 5. 생성창 첫 화면으로 복귀
        OpenCustomizationPanel();

        // 6. 프리뷰 캐릭터도 기본 출신지 기준으로 다시 세팅
        RefreshCharacterBySelectedOrigin(false);
    }

    #endregion

    #region Origin / Setup

    void PopulateDropdown()
    {
        originDropdown.ClearOptions();

        List<string> options = new List<string>(originDataDictionary.Keys);
        originDropdown.AddOptions(options);
    }

    void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        int index = dropdown.value;
        selectedOrigin = originDropdown.options[index].text;

        ApplyTraitPointByOriginIndex(index);

        if (traitSelectionUI != null)
        {
            traitSelectionUI.ResetTraitSelection();
            traitSelectionUI.SetTraitPoints(traitPoint);
        }

        UpdateSelectedOriginInfo();
        RefreshCharacterBySelectedOrigin(false);
    }

    private void ApplyTraitPointByOriginIndex(int index)
    {
        traitPoint = index == 6 ? 10 : 5;

        if (traitPointText != null)
            traitPointText.text = traitPoint.ToString();
    }

    private void RefreshCharacterBySelectedOrigin(bool resetTraitSelection)
    {
        if (string.IsNullOrEmpty(selectedOrigin))
            return;

        if (!originDataDictionary.TryGetValue(selectedOrigin, out CharacterData originData))
            return;

        characterManager = GetCharacterManager();
        if (characterManager == null)
            return;

        CharacterData newCharacter = DeepCopy.DeepCopyCharacter(originData);

        if (CustomInfo != null)
        {
            newCharacter.customizationData = CustomInfo.customizationInfo;
            newCharacter.customizationData.IsMale = CustomInfo.isMale;
            newCharacter.customizationData.SyncGenderFromBool();
        }

        characterManager.character = newCharacter;

        ApplyPreviewCharacterState();

        if (resetTraitSelection && traitSelectionUI != null)
        {
            traitSelectionUI.ResetTraitSelection();
            traitSelectionUI.SetTraitPoints(traitPoint);
        }

        UIupdate();
    }

    private void ApplyPreviewCharacterState()
    {
        if (characterManager == null || characterManager.character == null)
            return;

        var customization = characterManager.GetComponent<CharacterCustomization>();
        if (customization != null)
        {
            customization.ApplyCustomization(characterManager.character);
            customization.UpdateEquipmentAppearance(characterManager.character);
        }

        // 기존 마법사 랜덤 원소 적성 처리 유지.
        // 단, 나중에는 생성 확정 시점으로 옮기는 것을 추천.
        if (selectedOrigin == "마법사" &&
            characterManager.character.Traits != null &&
            characterManager.character.Traits.Count > 0 &&
            characterManager.character.Traits[0] is BasicElementalAptitudeTrait)
        {
            characterManager.character.Traits[0].ApplyTrait(characterManager);
        }

        characterManager.character.ApplyAllTraits(characterManager);
        characterManager.character.UpdateFinalStats();

        UpdateSelectedOriginInfo();
    }

    public void UIupdate()
    {
        if (traitSelectionUI != null)
            traitPoint = traitSelectionUI.availableTraitPoints;

        if (baseTraits != null)
        {
            for (int i = 0; i < baseTraits.Length; i++)
                baseTraits[i].text = "";

            if (characterManager != null && characterManager.character.Traits != null)
            {
                for (int i = 0; i < characterManager.character.Traits.Count && i < baseTraits.Length; i++)
                    baseTraits[i].text = characterManager.character.Traits[i].Name;
            }
        }

        if (traitPointText != null)
            traitPointText.text = traitPoint.ToString();

        UpdateStatsUI();
    }

    void UpdateSelectedOriginInfo()
    {
        if (selectedOriginInfo != null && !string.IsNullOrEmpty(selectedOrigin))
            selectedOriginInfo.text = $"{selectedOrigin}\n";
    }

    void UpdateStatsUI()
    {
        if (characterManager == null || characterManager.character == null)
            return;

        CharacterStats stats = characterManager.character.FinalStats;
        if (stats == null || baseStats == null || baseStats.Length < 10)
            return;

        baseStats[0].text = stats.Strength.ToString();
        baseStats[1].text = stats.Dexterity.ToString();
        baseStats[2].text = stats.Speed.ToString();
        baseStats[3].text = stats.Intelligence.ToString();
        baseStats[4].text = stats.Wisdom.ToString();
        baseStats[5].text = stats.Health.ToString();
        baseStats[6].text = stats.Detection.ToString();
        baseStats[7].text = stats.Insight.ToString();
        baseStats[8].text = stats.MaxHp.ToString();
        baseStats[9].text = stats.Endurance.ToString();
    }

    #endregion

    #region Create Character

    public void OnGameStartButtonPressed()
    {
        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();

        playerData.currentStage = "Town";

        GameManager.Instance.LoadGameScene("Town");
    }

    public void OnCreateCharacterButtonPressed()
    {
        if (string.IsNullOrEmpty(characterNameInput.text))
        {
            Debug.LogWarning("Character name is required!");

            if (ConfirmWindow != null)
                ConfirmWindow.SetActive(true);

            return;
        }

        characterManager = GetCharacterManager();

        if (characterManager == null || characterManager.character == null)
        {
            Debug.LogError("CharacterManager 또는 CharacterData가 없습니다.");
            return;
        }

        characterManager.character.customizationData = CustomInfo.customizationInfo;
        characterManager.character.customizationData.IsMale = CustomInfo.isMale;
        characterManager.character.customizationData.SyncGenderFromBool();

        BindHoldersToCharacterData();
        SeedOriginEquipmentsIntoHolders();

        UpdateEquipmentEffect();

        characterManager.character.Name = characterNameInput.text;
        characterManager.character.IsMine = true;
        characterManager.character.UpdateFinalStats();

        characterManager.character.CurrentHp = characterManager.character.FinalStats.MaxHp;
        characterManager.character.CurrentStamina = characterManager.character.FinalStats.MaxStamina;
        characterManager.character.CurrentMentality = characterManager.character.FinalStats.MaxMentality;

        PlayerManager._instance.CreateCharacter(characterManager.character);
        PlayerManager._instance.AddCharacterID(characterManager.character.ID);
        PlayerManager._instance.SaveCharacterPosition(characterManager.character.ID, false);

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();

        if (playerData.activeCharacterIds == null)
            playerData.activeCharacterIds = new List<string>();

        playerData.activeCharacterIds.Add(characterManager.character.ID);
        playerData.currentStage = "Town";

        Debug.Log("Character created and saved!");

        GameManager.Instance.LoadGameScene("Town");
    }

    public void UpdateEquipmentEffect()
    {
        List<Equipment> equips = (List<Equipment>)characterManager.character.GetEquipments();

        foreach (Equipment equip in equips)
        {
            if (equip != null)
                equip.Equip(characterManager.character);
        }
    }

    void BindHoldersToCharacterData()
    {
        var inv = default(InventoryHolder);
        var eq = default(InventoryHolder);

        var holders = characterManager.GetComponents<InventoryHolder>();
        foreach (var h in holders)
        {
            if (h.Type == InventoryHolder.HolderType.PlayerInventory)
                inv = h;
            else if (h.Type == InventoryHolder.HolderType.PlayerEquipment)
                eq = h;
        }

        characterManager.character.CharacterInventory = inv;
        characterManager.character.CharacterEquipment = eq;
    }

    void SeedOriginEquipmentsIntoHolders()
    {
        var eqHolder = characterManager.character.CharacterEquipment;
        if (eqHolder == null) return;

        var equips = (List<Equipment>)characterManager.character.GetEquipments();
        if (equips == null) return;

        foreach (var equipment in equips)
        {
            if (equipment == null) continue;

            eqHolder.AddItem(equipment, 1);

            var changed = new Dictionary<Item, int> { { equipment, 1 } };
            eqHolder.ItemChanged(changed);
        }
    }

    #endregion
}