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

    private OriginDefinitionSO selectedOrigin;
    private readonly List<OriginDefinitionSO> originList = new();

    private void Awake()
    {
        traitSelectionUI = GetComponent<TraitSelectionUI>();

        LoadOrigins();
        PopulateDropdown();

        if (originDropdown != null)
        {
            originDropdown.onValueChanged.RemoveAllListeners();
            originDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(originDropdown); });

            if (originList.Count > 0)
            {
                originDropdown.value = 0;
                selectedOrigin = originList[0];
            }
        }

        if (CustomInfo != null)
            CustomInfo.isMale = true;

        ResetCharacterCreationState();
        OpenCustomizationPanel();
    }

    public CharacterManager GetCharacterManager()
    {
        if (CustomInfo == null)
            return characterManager;

        if (CustomInfo.isMale)
        {
            if (CustomInfo.character_M != null)
                return CustomInfo.character_M.GetComponent<CharacterManager>();
        }

        if (CustomInfo.character_F != null)
            return CustomInfo.character_F.GetComponent<CharacterManager>();

        return characterManager;
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
        if (CustomInfo != null)
            CustomInfo.ResetCustomization();

        if (characterNameInput != null)
            characterNameInput.text = "";

        if (originList.Count > 0)
        {
            selectedOrigin = originList[0];

            if (originDropdown != null)
                originDropdown.value = 0;
        }

        ApplyTraitPointBySelectedOrigin();

        if (traitSelectionUI != null)
        {
            traitSelectionUI.ResetTraitSelection();
            traitSelectionUI.SetTraitPoints(traitPoint);
        }

        RefreshCharacterBySelectedOrigin(false);
        UpdateSelectedOriginInfo();
        UIupdate();
    }

    #endregion

    #region Origin / Setup

    private void LoadOrigins()
    {
        originList.Clear();

        if (GameDataRegistry.Instance == null)
        {
            Debug.LogError("GameDataRegistry.Instance가 없습니다. 씬에 GameDataRegistry 오브젝트가 필요합니다.");
            return;
        }

        List<OriginDefinitionSO> origins = GameDataRegistry.Instance.GetAllOrigins();

        if (origins == null)
            return;

        foreach (OriginDefinitionSO origin in origins)
        {
            if (origin == null)
                continue;

            originList.Add(origin);
        }
    }

    private void PopulateDropdown()
    {
        if (originDropdown == null)
            return;

        originDropdown.ClearOptions();

        List<string> options = new List<string>();

        foreach (OriginDefinitionSO origin in originList)
        {
            if (origin == null)
                continue;

            options.Add(origin.originName);
        }

        originDropdown.AddOptions(options);
    }

    private void OnDropdownValueChanged(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        int index = dropdown.value;

        if (index < 0 || index >= originList.Count)
            return;

        selectedOrigin = originList[index];

        ApplyTraitPointBySelectedOrigin();

        if (traitSelectionUI != null)
        {
            traitSelectionUI.ResetTraitSelection();
            traitSelectionUI.SetTraitPoints(traitPoint);
        }

        UpdateSelectedOriginInfo();
        RefreshCharacterBySelectedOrigin(false);
    }

    private void ApplyTraitPointBySelectedOrigin()
    {
        traitPoint = selectedOrigin != null ? selectedOrigin.traitPoint : 5;

        if (traitPointText != null)
            traitPointText.text = traitPoint.ToString();
    }

    private void RefreshCharacterBySelectedOrigin(bool resetTraitSelection)
    {
        if (selectedOrigin == null)
            return;

        characterManager = GetCharacterManager();

        if (characterManager == null)
            return;

        CharacterData newCharacter = selectedOrigin.CreateCharacterData();

        if (newCharacter == null)
            return;

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

        CharacterData character = characterManager.character;

        CharacterCustomization customization = characterManager.GetComponent<CharacterCustomization>();
        if (customization != null)
        {
            customization.ApplyCustomization(character);
            customization.UpdateEquipmentAppearance(character);
        }

        EquipmentManager.RebuildEquipmentStats(character);
        EquipmentManager.UpdateAvailableAttributes(character);
        EquipmentManager.UpdateSkillAvailability(character);

        character.RemoveAllTraits(characterManager);
        character.ApplyAllTraits(characterManager);
        character.UpdateFinalStats();

        UpdateSelectedOriginInfo();
    }

    public void UIupdate()
    {
        if (traitSelectionUI != null)
            traitPoint = traitSelectionUI.availableTraitPoints;

        UpdateTraitListUI();

        if (traitPointText != null)
            traitPointText.text = traitPoint.ToString();

        UpdateStatsUI();
    }

    private void UpdateTraitListUI()
    {
        if (baseTraits == null)
            return;

        for (int i = 0; i < baseTraits.Length; i++)
            baseTraits[i].text = "";

        if (characterManager == null ||
            characterManager.character == null ||
            characterManager.character.Traits == null)
        {
            return;
        }

        List<TraitRuntimeData> traits = characterManager.character.Traits;

        for (int i = 0; i < traits.Count && i < baseTraits.Length; i++)
        {
            TraitRuntimeData runtime = traits[i];

            if (runtime == null)
                continue;

            TraitDefinitionSO def = GameDataRegistry.Instance.GetTrait(runtime.traitId);

            if (def == null)
                continue;

            TraitGrade currentGrade = TraitGradeUtility.GetGrade(runtime.point);
            baseTraits[i].text = $"{def.traitName} ({currentGrade})";
        }
    }

    private void UpdateSelectedOriginInfo()
    {
        if (selectedOriginInfo == null)
            return;

        if (selectedOrigin == null)
        {
            selectedOriginInfo.text = "";
            ClearOriginImage();
            return;
        }

        selectedOriginInfo.text = $"{selectedOrigin.originName}";

        UpdateOriginImage();
    }

    private void UpdateOriginImage()
    {
        if (originImage == null)
            return;

        originImage.sprite = null;

        if (selectedOrigin == null || selectedOrigin.icon == null)
            return;

        Texture2D icon = selectedOrigin.icon;

        originImage.sprite = Sprite.Create(
            icon,
            new Rect(0, 0, icon.width, icon.height),
            new Vector2(0.5f, 0.5f));
    }

    private void ClearOriginImage()
    {
        if (originImage != null)
            originImage.sprite = null;
    }

    private void UpdateStatsUI()
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

        if (playerData != null)
            playerData.currentStage = "Town";

        GameManager.Instance.LoadGameScene("Town");
    }

    public void OnCreateCharacterButtonPressed()
    {
        if (characterNameInput == null || string.IsNullOrEmpty(characterNameInput.text))
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

        CharacterData character = characterManager.character;

        if (CustomInfo != null)
        {
            character.customizationData = CustomInfo.customizationInfo;
            character.customizationData.IsMale = CustomInfo.isMale;
            character.customizationData.SyncGenderFromBool();
        }

        character.Name = characterNameInput.text;
        character.IsMine = true;

        FinalizeCharacterBeforeSave(character);

        PlayerManager.Instance.CreateCharacter(character);
        PlayerManager.Instance.AddCharacterID(character.ID);
        PlayerManager.Instance.SaveCharacterPosition(character.ID, false);

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();

        if (playerData != null)
        {
            if (playerData.activeCharacterIds == null)
                playerData.activeCharacterIds = new List<string>();

            playerData.activeCharacterIds.Add(character.ID);
            playerData.currentStage = "Town";
        }

        Debug.Log("Character created and saved!");

        GameManager.Instance.LoadGameScene("Town");
    }

    private void FinalizeCharacterBeforeSave(CharacterData character)
    {
        if (character == null)
            return;

        EquipmentManager.RebuildEquipmentStats(character);
        EquipmentManager.UpdateAvailableAttributes(character);
        EquipmentManager.UpdateSkillAvailability(character);

        character.RemoveAllTraits(characterManager);
        character.ApplyAllTraits(characterManager);
        character.UpdateFinalStats();

        character.CurrentHp = character.FinalStats.MaxHp;
        character.CurrentStamina = character.FinalStats.MaxStamina;
        character.CurrentMentality = character.FinalStats.MaxMentality;
    }

    #endregion
}