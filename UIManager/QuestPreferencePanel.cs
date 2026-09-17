using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPreferencePanel : MonoBehaviour
{
    [Header("Issuers")]
    public Toggle warriorGuildToggle;
    public Toggle knightOrderToggle;
    public Toggle hunterGuildToggle;
    public Toggle mageTowerToggle;
    public Toggle adventurerGuildToggle;

    [Header("Tiers")]
    public Toggle tier1Toggle;
    public Toggle tier2Toggle;
    public Toggle tier3Toggle;
    public Toggle tier4Toggle;
    public Toggle tier5Toggle;

    [Header("Buttons")]
    public Button clearButton;
    public Button defaultButton;
    public Button applyButton;
    public Button closeButton;

    [Header("Message")]
    public TextMeshProUGUI issuerLimitText;

    private readonly List<Toggle> issuerToggles = new();
    private readonly List<Toggle> tierToggles = new();
    private bool binding;

    private void Awake()
    {
        issuerToggles.Add(warriorGuildToggle);
        issuerToggles.Add(knightOrderToggle);
        issuerToggles.Add(hunterGuildToggle);
        issuerToggles.Add(mageTowerToggle);
        issuerToggles.Add(adventurerGuildToggle);

        tierToggles.Add(tier1Toggle);
        tierToggles.Add(tier2Toggle);
        tierToggles.Add(tier3Toggle);
        tierToggles.Add(tier4Toggle);
        tierToggles.Add(tier5Toggle);

        foreach (Toggle toggle in issuerToggles)
        {
            if (toggle != null)
                toggle.onValueChanged.AddListener(OnIssuerToggleChanged);
        }

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearAll);

        if (defaultButton != null)
            defaultButton.onClick.AddListener(ClearAll);

        if (applyButton != null)
            applyButton.onClick.AddListener(Apply);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        LoadCurrentPreferences();
    }

    private void OnDestroy()
    {
        foreach (Toggle toggle in issuerToggles)
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnIssuerToggleChanged);
        }

        if (clearButton != null)
            clearButton.onClick.RemoveListener(ClearAll);

        if (defaultButton != null)
            defaultButton.onClick.RemoveListener(ClearAll);

        if (applyButton != null)
            applyButton.onClick.RemoveListener(Apply);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    private void LoadCurrentPreferences()
    {
        QuestManager manager = QuestManager.Instance;

        if (manager == null)
            return;

        binding = true;

        SetToggle(warriorGuildToggle, manager.preferredIssuers.Contains(QuestIssuer.WarriorGuild));
        SetToggle(knightOrderToggle, manager.preferredIssuers.Contains(QuestIssuer.SwordDojo));
        SetToggle(hunterGuildToggle, manager.preferredIssuers.Contains(QuestIssuer.HunterGuild));
        SetToggle(mageTowerToggle, manager.preferredIssuers.Contains(QuestIssuer.MageTower));
        SetToggle(adventurerGuildToggle, manager.preferredIssuers.Contains(QuestIssuer.TownCouncil));

        SetToggle(tier1Toggle, manager.preferredQuestTiers.Contains(1));
        SetToggle(tier2Toggle, manager.preferredQuestTiers.Contains(2));
        SetToggle(tier3Toggle, manager.preferredQuestTiers.Contains(3));
        SetToggle(tier4Toggle, manager.preferredQuestTiers.Contains(4));
        SetToggle(tier5Toggle, manager.preferredQuestTiers.Contains(5));

        int maximumTier = manager.GetMaximumAvailableQuestTier();

        for (int i = 0; i < tierToggles.Count; i++)
        {
            if (tierToggles[i] != null)
                tierToggles[i].interactable = i + 1 <= maximumTier;
        }

        binding = false;
        UpdateIssuerLimitState();
    }

    private void OnIssuerToggleChanged(bool value)
    {
        if (!binding)
            UpdateIssuerLimitState();
    }

    private void UpdateIssuerLimitState()
    {
        int selectedCount = 0;

        foreach (Toggle toggle in issuerToggles)
        {
            if (toggle != null && toggle.isOn)
                selectedCount++;
        }

        bool limitReached = selectedCount >= QuestManager.MaxPreferredIssuerCount;

        foreach (Toggle toggle in issuerToggles)
        {
            if (toggle != null)
                toggle.interactable = toggle.isOn || !limitReached;
        }

        if (issuerLimitText != null)
        {
            issuerLimitText.text =
                $"선호 의뢰주체 {selectedCount} / {QuestManager.MaxPreferredIssuerCount}";
        }
    }

    private void ClearAll()
    {
        binding = true;

        foreach (Toggle toggle in issuerToggles)
        {
            if (toggle != null)
                toggle.isOn = false;
        }

        foreach (Toggle toggle in tierToggles)
        {
            if (toggle != null)
                toggle.isOn = false;
        }

        binding = false;
        UpdateIssuerLimitState();
    }

    private void Apply()
    {
        QuestManager manager = QuestManager.Instance;

        if (manager == null)
            return;

        List<QuestIssuer> issuers = new();
        AddIssuerIfSelected(issuers, warriorGuildToggle, QuestIssuer.WarriorGuild);
        AddIssuerIfSelected(issuers, knightOrderToggle, QuestIssuer.SwordDojo);
        AddIssuerIfSelected(issuers, hunterGuildToggle, QuestIssuer.HunterGuild);
        AddIssuerIfSelected(issuers, mageTowerToggle, QuestIssuer.MageTower);
        AddIssuerIfSelected(issuers, adventurerGuildToggle, QuestIssuer.TownCouncil);

        List<int> tiers = new();
        AddTierIfSelected(tiers, tier1Toggle, 1);
        AddTierIfSelected(tiers, tier2Toggle, 2);
        AddTierIfSelected(tiers, tier3Toggle, 3);
        AddTierIfSelected(tiers, tier4Toggle, 4);
        AddTierIfSelected(tiers, tier5Toggle, 5);

        manager.SetBoardPreferences(issuers, tiers);

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.SavePlayerDataToPlayFab();

        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void SetToggle(Toggle toggle, bool value)
    {
        if (toggle != null)
            toggle.isOn = value;
    }

    private void AddIssuerIfSelected(
        List<QuestIssuer> issuers,
        Toggle toggle,
        QuestIssuer issuer)
    {
        if (toggle != null && toggle.isOn)
            issuers.Add(issuer);
    }

    private void AddTierIfSelected(
        List<int> tiers,
        Toggle toggle,
        int tier)
    {
        if (toggle != null && toggle.isOn && toggle.interactable)
            tiers.Add(tier);
    }
}
