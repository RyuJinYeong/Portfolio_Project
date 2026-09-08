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
    private bool reservationPending;

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
            insufficientGoldConfirmButton.onClick.RemoveListener(OnInsufficientGoldConfirmed);
            insufficientGoldConfirmButton.onClick.AddListener(OnInsufficientGoldConfirmed);
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
            insufficientGoldConfirmButton.onClick.RemoveListener(OnInsufficientGoldConfirmed);
    }

    private void BuildCandidates()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        SetText(currentGoldText, playerData != null ? $"{playerData.gold:N0}" : "-");
        UpdateRefreshButton(playerData);

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

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        int contractFee = MercenaryGenerator.CalculateContractFee(character);
        int sortiePay = MercenaryGenerator.CalculateSortiePay(character);

        SetText(
            contractFeeText,
            character != null
                ? $"{contractFee:N0}" +
                  (playerData != null &&
                   character.ID == playerData.reservedRecruitmentCandidateId
                      ? "  (예약 중)"
                      : string.Empty)
                : "-");
        SetText(
            sortiePayText,
            character != null ? $"{sortiePay:N0}" : "-");

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
        reservationPending = !hireConfirmationPending;

        if (!hireConfirmationPending)
        {
            SetText(
                insufficientGoldMessageText,
                "골드가 부족합니다.\n이 용병을 예약하시겠습니까?\n예약은 1명만 유지됩니다.");
            SetButtonText(insufficientGoldConfirmButton, "예약");

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
        int refreshCost = MercenaryGenerator.GetRecruitmentRefreshCost(playerData);

        if (playerData.gold < refreshCost)
        {
            SetText(
                insufficientGoldMessageText,
                $"새로고침 비용 {refreshCost:N0} G가 필요합니다.");
            SetButtonText(insufficientGoldConfirmButton, "확인");

            if (insufficientGoldDialog != null)
                insufficientGoldDialog.SetActive(true);

            return;
        }

        playerData.gold -= refreshCost;
        playerData.recruitmentRefreshCount++;
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
        reservationPending = false;

        if (hireDialog != null)
            hireDialog.SetActive(false);

        if (insufficientGoldDialog != null)
            insufficientGoldDialog.SetActive(false);
    }

    private void OnInsufficientGoldConfirmed()
    {
        if (reservationPending && selectedCharacter != null)
        {
            PlayerData playerData = PlayerManager.Instance != null
                ? PlayerManager.Instance.GetCurrentPlayerData()
                : null;

            if (playerData != null)
            {
                playerData.reservedRecruitmentCandidateId = selectedCharacter.ID;
                PlayerManager.Instance.SavePlayerDataToPlayFab();
            }
        }

        HideHireDialog();
        SelectCharacter(selectedCharacter);
        characterView?.RefreshAll();
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
        if (playerData.reservedRecruitmentCandidateId == selectedCharacter.ID)
            playerData.reservedRecruitmentCandidateId = null;

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

    private void UpdateRefreshButton(PlayerData playerData)
    {
        int cost = MercenaryGenerator.GetRecruitmentRefreshCost(playerData);
        SetButtonText(refreshButton, $"새로고침  {cost:N0} G");
    }

    private static void SetButtonText(Button button, string value)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
            label.text = value;
    }
}
