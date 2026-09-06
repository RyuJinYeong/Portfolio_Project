using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecruitPanel : MonoBehaviour
{
    [Header("Character View")]
    public TownCharacterManagementPanel characterView;

    [Header("Recruitment")]
    public TMP_Text contractFeeText;
    public TMP_Text sortiePayText;
    public TMP_Text currentGoldText;
    public Button hireButton;
    public Button refreshButton;

    [Header("Hire Dialog")]
    public GameObject hireDialog;
    public GameObject hireDialogGoldRow;
    public TMP_Text hireDialogGoldText;
    public TMP_Text hireDialogMessageText;
    public Button hireDialogConfirmButton;
    public Button hireDialogCancelButton;
    public GameObject insufficientGoldDialog;
    public TMP_Text insufficientGoldMessageText;
    public Button insufficientGoldConfirmButton;

    private CharacterData selectedCharacter;
    private bool hireConfirmationPending;

    private void Awake()
    {
        if (characterView != null)
            characterView.closeRequested = Close;

        HideHireDialog();
    }

    private void OnEnable()
    {
        if (hireButton != null)
        {
            hireButton.onClick.RemoveListener(OnHireButtonClicked);
            hireButton.onClick.AddListener(OnHireButtonClicked);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(OnRefreshButtonClicked);
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }

        if (hireDialogConfirmButton != null)
        {
            hireDialogConfirmButton.onClick.RemoveListener(OnHireDialogConfirmed);
            hireDialogConfirmButton.onClick.AddListener(OnHireDialogConfirmed);
        }

        if (hireDialogCancelButton != null)
        {
            hireDialogCancelButton.onClick.RemoveListener(HideHireDialog);
            hireDialogCancelButton.onClick.AddListener(HideHireDialog);
        }

        if (insufficientGoldConfirmButton != null)
        {
            insufficientGoldConfirmButton.onClick.RemoveListener(HideHireDialog);
            insufficientGoldConfirmButton.onClick.AddListener(HideHireDialog);
        }

        BuildCandidates();
    }

    private void OnDisable()
    {
        if (hireButton != null)
            hireButton.onClick.RemoveListener(OnHireButtonClicked);

        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(OnRefreshButtonClicked);

        if (hireDialogConfirmButton != null)
            hireDialogConfirmButton.onClick.RemoveListener(OnHireDialogConfirmed);

        if (hireDialogCancelButton != null)
            hireDialogCancelButton.onClick.RemoveListener(HideHireDialog);

        if (insufficientGoldConfirmButton != null)
            insufficientGoldConfirmButton.onClick.RemoveListener(HideHireDialog);
    }

    private void BuildCandidates()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        SetText(currentGoldText, playerData != null ? $"{playerData.gold:N0}" : "-");

        if (playerData == null || characterView == null)
            return;

        if (playerData.recruitmentCandidates == null)
            playerData.recruitmentCandidates = new System.Collections.Generic.List<CharacterData>();

        bool hasLegacyCandidateNames = playerData.recruitmentCandidates.Exists(
            candidate => candidate != null &&
                         !string.IsNullOrEmpty(candidate.Name) &&
                         candidate.Name.EndsWith(" 용병"));

        if (!playerData.recruitmentCandidatesInitialized || hasLegacyCandidateNames)
        {
            MercenaryGenerator.RefreshRecruitmentCandidates(playerData);
            PlayerManager.Instance.SavePlayerDataToPlayFab();
        }

        characterView.BuildCharacters(
            playerData.recruitmentCandidates,
            SelectCharacter);

        CaptureMissingPortraits(playerData);
    }

    private void CaptureMissingPortraits(PlayerData playerData)
    {
        if (CharacterPoolManager.Instance == null ||
            playerData?.recruitmentCandidates == null)
        {
            return;
        }

        foreach (CharacterData character in playerData.recruitmentCandidates)
        {
            if (character == null || character.Portrait != null)
                continue;

            CharacterPoolManager.Instance.CapturePortrait(character, () =>
                characterView?.RefreshAll());
        }
    }

    private void SelectCharacter(CharacterData character)
    {
        selectedCharacter = character;

        int contractFee = MercenaryGenerator.CalculateContractFee(character);
        int sortiePay = MercenaryGenerator.CalculateSortiePay(character);

        SetText(
            contractFeeText,
            character != null ? $"{contractFee:N0}" : "-");
        SetText(
            sortiePayText,
            character != null ? $"{sortiePay:N0}" : "-");

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (hireButton != null)
        {
            hireButton.interactable = character != null &&
                                      playerData != null;
        }
    }

    private void OnHireButtonClicked()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (selectedCharacter == null || playerData == null)
            return;

        int contractFee = MercenaryGenerator.CalculateContractFee(selectedCharacter);
        hireConfirmationPending = playerData.gold >= contractFee;

        if (!hireConfirmationPending)
        {
            SetText(insufficientGoldMessageText, "골드가 부족합니다.");

            if (insufficientGoldDialog != null)
                insufficientGoldDialog.SetActive(true);

            return;
        }

        if (hireDialogGoldRow != null)
            hireDialogGoldRow.SetActive(true);

        SetText(hireDialogGoldText, $"{playerData.gold:N0}");
        SetText(hireDialogMessageText, "고용하시겠습니까?");

        if (hireDialog != null)
            hireDialog.SetActive(true);
    }

    private void OnRefreshButtonClicked()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData == null)
            return;

        HideHireDialog();
        selectedCharacter = null;
        MercenaryGenerator.RefreshRecruitmentCandidates(playerData);
        PlayerManager.Instance.SavePlayerDataToPlayFab();
        BuildCandidates();
    }

    private void OnHireDialogConfirmed()
    {
        if (!hireConfirmationPending)
        {
            HideHireDialog();
            return;
        }

        HireSelected();
    }

    private void HideHireDialog()
    {
        hireConfirmationPending = false;

        if (hireDialog != null)
            hireDialog.SetActive(false);

        if (insufficientGoldDialog != null)
            insufficientGoldDialog.SetActive(false);
    }

    private void HireSelected()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (selectedCharacter == null ||
            playerData?.recruitmentCandidates == null)
        {
            return;
        }

        int contractFee = MercenaryGenerator.CalculateContractFee(selectedCharacter);

        if (playerData.gold < contractFee ||
            !playerData.recruitmentCandidates.Remove(selectedCharacter))
        {
            return;
        }

        playerData.gold -= contractFee;
        SetText(currentGoldText, $"{playerData.gold:N0}");
        selectedCharacter.IsMine = true;

        PromoteEquippedItems(selectedCharacter.EquipmentSlots);

        PlayerManager.Instance.CreateCharacter(selectedCharacter);
        playerData.characterIds.Add(selectedCharacter.ID);
        playerData.SetPosition(selectedCharacter.ID, false);

        CharacterPoolManager.Instance?.AddCharacterToPool(selectedCharacter);
        PlayerManager.Instance.SavePlayerDataToPlayFab();

        HideHireDialog();
        selectedCharacter = null;
        characterView.BuildCharacters(
            playerData.recruitmentCandidates,
            SelectCharacter);
    }

    public void Close()
    {
        HideHireDialog();
        gameObject.SetActive(false);
        UIManager.Instance?.ReturnToTownCameraWhenIdle();
    }

    private static void PromoteEquippedItems(EquipmentSlotData slots)
    {
        if (slots == null)
            return;

        EquipmentInstanceRepository.PromoteRuntimeToPlayer(slots.helmetInstanceId);
        EquipmentInstanceRepository.PromoteRuntimeToPlayer(slots.armorInstanceId);
        EquipmentInstanceRepository.PromoteRuntimeToPlayer(slots.glovesInstanceId);
        EquipmentInstanceRepository.PromoteRuntimeToPlayer(slots.shoesInstanceId);
        EquipmentInstanceRepository.PromoteRuntimeToPlayer(slots.ring1InstanceId);
        EquipmentInstanceRepository.PromoteRuntimeToPlayer(slots.ring2InstanceId);
        EquipmentInstanceRepository.PromoteRuntimeToPlayer(slots.necklaceInstanceId);
        EquipmentInstanceRepository.PromoteRuntimeToPlayer(slots.weaponInstanceId);
        EquipmentInstanceRepository.PromoteRuntimeToPlayer(slots.subWeaponInstanceId);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
