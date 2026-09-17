using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEquipmentWindowUI : MonoBehaviour
{
    private enum StatsPage
    {
        Base,
        Special,
        Traits
    }

    [Serializable]
    public class EquipmentSlotBinding
    {
        public EquipmentType equipmentType;
        [Min(1)]
        public int slotIndex = 1;
        public InventoryItemSlotUI view;
    }

    public TMP_Text characterNameText;
    public TMP_Text characterLevelText;
    public Text legacyCharacterNameText;
    public Text legacyCharacterLevelText;
    public Button closeButton;
    public Button previousCharacterButton;
    public Button nextCharacterButton;
    [Header("Stats Panel")]
    public RectTransform statsContainer;
    public GameObject statRowTemplate;
    public Button baseStatsTabButton;
    public Button specialStatsTabButton;
    public Button traitsTabButton;
    public Button storageButton;
    [SerializeField] private GameObject twoHandedBlock;
    public SharedInventoryType returnInventoryType = SharedInventoryType.ExpeditionStorage;
    public List<EquipmentSlotBinding> slots = new();

    private CharacterManager characterManager;
    private CharacterData characterData;
    private StatsPage statsPage;
    private string portraitEquipmentSignature;
    private readonly List<CharacterData> navigationCharacters = new();

    public void SetNavigationCharacters(IEnumerable<CharacterData> characters)
    {
        navigationCharacters.Clear();
        if (characters != null)
            foreach (CharacterData character in characters)
                if (character != null && !navigationCharacters.Exists(item => item.ID == character.ID))
                    navigationCharacters.Add(character);

        bool canNavigate = navigationCharacters.Count > 1 && characterData != null &&
            navigationCharacters.Exists(item => item.ID == characterData.ID);
        if (previousCharacterButton != null) previousCharacterButton.interactable = canNavigate;
        if (nextCharacterButton != null) nextCharacterButton.interactable = canNavigate;
    }

    public void PreviousCharacter() => SwitchCharacter(-1);
    public void NextCharacter() => SwitchCharacter(1);

    private void SwitchCharacter(int direction)
    {
        int index = navigationCharacters.FindIndex(item => item.ID == characterData?.ID);
        if (index < 0 || navigationCharacters.Count < 2)
            return;

        CharacterData next = navigationCharacters[
            (index + direction + navigationCharacters.Count) % navigationCharacters.Count];
        var navigation = new List<CharacterData>(navigationCharacters);
        InventoryItemTooltipUI.Instance?.Hide();
        TooltipManager.Instance?.HideTooltip();
        InventoryUIController controller = InventoryUIController.Instance;
        CharacterManager manager = CharacterPoolManager.Instance?.Get(next.ID);
        if (manager != null)
            controller.SetSelectedCharacter(manager);
        else
            controller.SetSelectedCharacterData(next);
        controller.OpenEquipment();
        SetNavigationCharacters(navigation);
    }

    private void OnEnable()
    {
        SharedInventoryUtility.EquipmentChanged += OnEquipmentChanged;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (storageButton != null)
            storageButton.onClick.AddListener(ToggleStorage);

        if (baseStatsTabButton != null)
            baseStatsTabButton.onClick.AddListener(ShowBaseStats);

        if (specialStatsTabButton != null)
            specialStatsTabButton.onClick.AddListener(ShowSpecialStats);

        if (traitsTabButton != null)
            traitsTabButton.onClick.AddListener(ShowTraits);

        UpdateStatsTabVisuals();
        Refresh();
    }

    private void OnDisable()
    {
        SharedInventoryUtility.EquipmentChanged -= OnEquipmentChanged;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (storageButton != null)
            storageButton.onClick.RemoveListener(ToggleStorage);

        if (baseStatsTabButton != null)
            baseStatsTabButton.onClick.RemoveListener(ShowBaseStats);

        if (specialStatsTabButton != null)
            specialStatsTabButton.onClick.RemoveListener(ShowSpecialStats);

        if (traitsTabButton != null)
            traitsTabButton.onClick.RemoveListener(ShowTraits);
    }

    public void Open(
        CharacterManager manager,
        SharedInventoryType inventoryType = SharedInventoryType.ExpeditionStorage)
    {
        characterManager = manager;
        characterData = manager != null ? manager.character : null;
        SetNavigationCharacters(null);
        portraitEquipmentSignature = GetPortraitEquipmentSignature(characterData);
        returnInventoryType = inventoryType;

        if (storageButton != null)
            storageButton.gameObject.SetActive(manager != null);

        gameObject.SetActive(true);
        Refresh();
    }

    public void Open(CharacterData character)
    {
        characterManager = null;
        characterData = character;
        SetNavigationCharacters(null);
        portraitEquipmentSignature = GetPortraitEquipmentSignature(characterData);

        if (storageButton != null)
            storageButton.gameObject.SetActive(false);

        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (InventoryItemTooltipUI.Instance != null)
            InventoryItemTooltipUI.Instance.Hide();

        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();

        if (InventoryUIController.Instance != null)
            InventoryUIController.Instance.CloseAttachedStorage();

        gameObject.SetActive(false);
    }

    private void ToggleStorage()
    {
        if (InventoryUIController.Instance != null)
            InventoryUIController.Instance.ToggleAccessibleStorageFromEquipment();
    }

    public void Refresh()
    {
        CharacterData character = characterData;

        if (twoHandedBlock != null)
        {
            twoHandedBlock.SetActive(character != null && !character.CanEquipSubWeapon());
        }

        if (characterNameText != null)
            characterNameText.text = character != null ? character.Name : "";

        if (legacyCharacterNameText != null)
            legacyCharacterNameText.text = character != null ? character.Name : "";

        if (characterLevelText != null)
            characterLevelText.text = character != null ? $"Lv. {character.Level}" : "";

        if (legacyCharacterLevelText != null)
            legacyCharacterLevelText.text = character != null ? $"Lv. {character.Level}" : "";

        foreach (EquipmentSlotBinding binding in slots)
        {
            if (binding == null || binding.view == null)
                continue;

            InventorySlotData slot = character != null
                ? SharedInventoryUtility.GetEquippedSlot(
                    character.EquipmentSlots,
                    binding.equipmentType,
                    binding.slotIndex)
                : null;
            bool isLockedSubWeaponSlot =
                slot == null &&
                character != null &&
                binding.equipmentType == EquipmentType.SubWeapon &&
                !character.CanEquipSubWeapon();

            EquipmentSlotBinding capturedBinding = binding;
            binding.view.Bind(
                slot,
                isLockedSubWeaponSlot
                    ? null
                    : (_, button) => OnSlotClicked(capturedBinding, button));

            if (isLockedSubWeaponSlot)
            {
                if (binding.view.nameText != null)
                    binding.view.nameText.text = "잠김";

                if (binding.view.legacyNameText != null)
                    binding.view.legacyNameText.text = "잠김";
            }
        }

        RefreshStatsPanel(character);
    }

    private void OnSlotClicked(EquipmentSlotBinding binding, int button)
    {
        if (button != (int)UnityEngine.EventSystems.PointerEventData.InputButton.Right ||
            characterManager == null)
        {
            return;
        }

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();
        List<InventorySlotData> storage =
            SharedInventoryUtility.GetStorage(playerData, returnInventoryType);

        if (SharedInventoryUtility.UnequipToStorage(
                characterManager,
                storage,
                binding.equipmentType,
                binding.slotIndex))
        {
            SharedInventoryUtility.SaveChanges(characterManager);
        }
    }

    private void OnEquipmentChanged(CharacterManager manager)
    {
        if (manager != characterManager)
            return;

        string currentSignature = GetPortraitEquipmentSignature(manager.character);
        bool portraitChanged = portraitEquipmentSignature != currentSignature;
        portraitEquipmentSignature = currentSignature;
        Refresh();

        if (portraitChanged && CharacterPoolManager.Instance != null)
        {
            CharacterPoolManager.Instance.RefreshPortrait(manager, () =>
            {
                if (UIManager.Instance != null &&
                    UIManager.Instance.characterManagementPanel != null)
                {
                    UIManager.Instance.characterManagementPanel.RefreshAll();
                }
            });
        }
    }

    private static string GetPortraitEquipmentSignature(CharacterData character)
    {
        EquipmentSlotData slots = character != null ? character.EquipmentSlots : null;

        if (slots == null)
            return string.Empty;

        return string.Join(
            "|",
            slots.helmetUid,
            slots.helmetInstanceId,
            slots.armorUid,
            slots.armorInstanceId,
            slots.glovesUid,
            slots.glovesInstanceId,
            slots.shoesUid,
            slots.shoesInstanceId,
            slots.weaponUid,
            slots.weaponInstanceId,
            slots.subWeaponUid,
            slots.subWeaponInstanceId);
    }

    private void SetStatsPage(StatsPage page)
    {
        statsPage = page;
        UpdateStatsTabVisuals();
        RefreshStatsPanel(characterData);
    }

    private void ShowBaseStats()
    {
        SetStatsPage(StatsPage.Base);
    }

    private void ShowSpecialStats()
    {
        SetStatsPage(StatsPage.Special);
    }

    private void ShowTraits()
    {
        SetStatsPage(StatsPage.Traits);
    }

    private void RefreshStatsPanel(CharacterData character)
    {
        if (statsContainer == null || statRowTemplate == null)
            return;

        for (int i = statsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = statsContainer.GetChild(i);

            if (child.gameObject != statRowTemplate)
                Destroy(child.gameObject);
        }

        if (character == null)
            return;

        switch (statsPage)
        {
            case StatsPage.Special:
                PopulateSpecialStats(character.FinalStats);
                break;

            case StatsPage.Traits:
                PopulateTraits(character);
                break;

            default:
                PopulateBaseStats(character.FinalStats);
                break;
        }
    }

    private void PopulateBaseStats(CharacterStats stats)
    {
        if (stats == null)
            return;

        AddStatRow("HP", stats.MaxHp.ToString());
        AddStatRow("LV", characterData.Level.ToString());
        AddStatRow("근력", stats.Strength.ToString());
        AddStatRow("기교", stats.Dexterity.ToString());
        AddStatRow("속도", stats.Speed.ToString());
        AddStatRow("지능", stats.Intelligence.ToString());
        AddStatRow("지혜", stats.Wisdom.ToString());
        AddStatRow("건강", stats.Health.ToString());
        AddStatRow("활력", stats.Vitality.ToString());
        AddStatRow("인내", stats.Endurance.ToString());
        AddStatRow("눈썰미", stats.Detection.ToString());
        AddStatRow("통찰력", stats.Insight.ToString());        
        AddStatRow("물리 공격력", stats.PhysicalAttack.ToString());
        AddStatRow("마법 공격력", stats.MagicalAttack.ToString());
        AddStatRow("물리 방어력", stats.PhysicalDefense.ToString());
        AddStatRow("마법 방어력", stats.MagicalDefense.ToString());
        AddStatRow("공격 속도", stats.AttackSpeed.ToString("0.##"));
        AddStatRow("시전 속도", stats.CastSpeed.ToString("0.##"));
    }

    private void PopulateSpecialStats(CharacterStats stats)
    {
        if (stats == null)
            return;

        AddStatRow("지구력", stats.MaxStamina.ToString());
        AddStatRow("정신력", stats.MaxMentality.ToString());
        AddStatRow("턴당 지구력 회복량", stats.StaminaRecovery.ToString());
        AddStatRow("턴당 정신력 회복량", stats.MentalityRecovery.ToString());
        AddStatRow("화염 저항", $"{stats.FireResistance}%");
        AddStatRow("얼음 저항", $"{stats.IceResistance}%");
        AddStatRow("번개 저항", $"{stats.LightningResistance}%");
        AddStatRow("관통 저항", $"{stats.PierceResistance}%");
        AddStatRow("참격 저항", $"{stats.SlashResistance}%");
        AddStatRow("타격 저항", $"{stats.SmashResistance}%");
        AddStatRow("화염 특화", $"{stats.FireAffinity}%");
        AddStatRow("얼음 특화", $"{stats.IceAffinity}%");
        AddStatRow("번개 특화", $"{stats.LightningAffinity}%");
        AddStatRow("관통 특화", $"{stats.PierceAffinity}%");
        AddStatRow("참격 특화", $"{stats.SlashAffinity}%");
        AddStatRow("타격 특화", $"{stats.SmashAffinity}%");
    }

    private void PopulateTraits(CharacterData character)
    {
        HashSet<int> displayedTraitIds = new HashSet<int>();

        if (character.Traits != null)
        {
            foreach (TraitRuntimeData runtime in character.Traits)
                AddTraitRow(runtime, displayedTraitIds);
        }

        if (character.EquipmentTraitRuntimes != null)
        {
            foreach (TraitRuntimeData runtime in character.EquipmentTraitRuntimes)
                AddTraitRow(runtime, displayedTraitIds);
        }
    }

    private void AddTraitRow(TraitRuntimeData runtime, HashSet<int> displayedTraitIds)
    {
        if (runtime == null || !displayedTraitIds.Add(runtime.traitId))
            return;

        TraitDefinitionSO trait = GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetTrait(runtime.traitId)
            : null;

        if (trait != null)
        {
            TraitGrade grade = TraitGradeUtility.GetGrade(runtime.point, trait);
            string color = ColorUtility.ToHtmlStringRGB(GetTraitColor(trait.polarity));
            AddStatRow(
                $"{trait.traitName} <color=#{color}>({grade})</color>",
                "",
                trait);
        }
    }

    private void AddStatRow(
        string label,
        string value,
        TraitDefinitionSO trait = null)
    {
        GameObject row = Instantiate(statRowTemplate, statsContainer);
        row.name = label;
        row.SetActive(true);

        Text labelText = FindDeepChild(row.transform, "Stat1Name")?.GetComponent<Text>();
        Text valueText = FindDeepChild(row.transform, "Value")?.GetComponent<Text>();

        if (labelText != null)
        {
            labelText.text = label;
            labelText.supportRichText = true;
            labelText.alignment = TextAnchor.MiddleLeft;
        }

        if (valueText != null)
        {
            valueText.text = value;
            valueText.alignment = TextAnchor.MiddleRight;
        }

        EquipmentInfoHoverTooltip tooltip = row.GetComponent<EquipmentInfoHoverTooltip>();

        if (trait != null)
            tooltip.BindTrait(trait);
        else
            tooltip.BindStat(label);
    }

    private void UpdateStatsTabVisuals()
    {
        SetTabColor(baseStatsTabButton, statsPage == StatsPage.Base);
        SetTabColor(specialStatsTabButton, statsPage == StatsPage.Special);
        SetTabColor(traitsTabButton, statsPage == StatsPage.Traits);
    }

    private static void SetTabColor(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.color = selected
                ? new Color(0.38f, 0.16f, 0.08f, 0.95f)
                : new Color(0.08f, 0.07f, 0.06f, 0.85f);
        }
    }

    private static Color GetTraitColor(TraitPolarity polarity)
    {
        return polarity switch
        {
            TraitPolarity.Positive => new Color(0.29f, 0.67f, 0.22f, 1f),
            TraitPolarity.Negative => new Color(0.85f, 0.24f, 0.18f, 1f),
            TraitPolarity.Mixed => new Color(0.9f, 0.67f, 0.2f, 1f),
            _ => Color.white
        };
    }


    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
                return child;

            Transform nested = FindDeepChild(child, childName);

            if (nested != null)
                return nested;
        }

        return null;
    }
}
