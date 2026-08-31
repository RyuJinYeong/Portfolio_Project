using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TownCharacterManagementPanel : MonoBehaviour
{
    [Header("Character List")]
    public RectTransform content;
    public GameObject cardPrefab;

    [Header("Selected Character")]
    public RawImage selectedPortrait;
    public TMP_Text selectedNameText;
    public TMP_Text selectedOriginText;
    public TMP_Text selectedLevelText;
    public TMP_Text selectedResourceText;
    public TMP_Text selectedStatsText;
    public TMP_Text selectedTraitsText;
    public TMP_Text selectedSkillsText;

    [Header("Roster Summary")]
    public TMP_Text skillSummaryText;
    public TMP_Text traitSummaryText;
    public TMP_Text statSummaryText;
    public Button skillSummaryButton;
    public Button traitSummaryButton;
    public Button statSummaryButton;

    [Header("Actions")]
    public Button overviewButton;
    public Button equipmentButton;
    public Button skillsButton;
    public Button storageButton;
    public Button equipmentWindowButton;
    public Button skillWindowButton;
    public Button closeButton;

    [Header("Detail Sections")]
    public RectTransform statsPreview;
    public RectTransform traitsPreview;
    public RectTransform skillsPreview;
    public TownCharacterStatsDetailUI statsDetailUI;
    public RectTransform traitCardContent;
    public TownTraitPreviewCardUI traitCardTemplate;
    public RectTransform skillCardContent;
    public TownSkillPreviewCardUI skillCardTemplate;

    public InventoryUIController inventoryUIController;
    [FormerlySerializedAs("townCoordinator")]
    public UIManager uiManager;

    public CharacterManager SelectedCharacter { get; private set; }
    public CharacterData SelectedCharacterData { get; private set; }
    public Action closeRequested;

    private readonly List<CharacterInfoPanel> infoPanels = new();
    private readonly Dictionary<Image, Color> panelColors = new();
    private readonly Dictionary<CharacterInfoPanel, CharacterManager> panelCharacters = new();
    private readonly Dictionary<CharacterInfoPanel, CharacterData> panelData = new();
    private readonly List<TownTraitPreviewCardUI> traitCards = new();
    private readonly List<TownSkillPreviewCardUI> skillCards = new();
    private readonly Dictionary<RectTransform, RectTransformLayout> statsRectLayouts = new();
    private readonly Dictionary<RectTransform, float> statRowHeights = new();
    private DetailSection selectedDetailSection = DetailSection.Stats;
    private Action<CharacterData> externalSelectionChanged;
    private bool showingExternalCharacters;

    private struct RectTransformLayout
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;

        public RectTransformLayout(RectTransform rectTransform)
        {
            anchorMin = rectTransform.anchorMin;
            anchorMax = rectTransform.anchorMax;
            anchoredPosition = rectTransform.anchoredPosition;
            sizeDelta = rectTransform.sizeDelta;
        }
    }

    private enum DetailSection
    {
        Stats,
        Traits,
        Skills
    }

    private void OnEnable()
    {
        WireButtons(true);
        SelectStatsDetail();
    }

    private void OnDisable()
    {
        WireButtons(false);
        Clear();
    }

    public void OpenAndBuild()
    {
        gameObject.SetActive(true);

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        int requiredCount = playerData?.characterIds?.Count ?? 0;
        int pooledCount = CharacterPoolManager.Instance != null
            ? CharacterPoolManager.Instance.Pool.Count
            : 0;

        if (CharacterPoolManager.Instance == null)
            return;

        if (pooledCount < requiredCount)
        {
            CharacterPoolManager.Instance.BuildPoolFromPlayerData(RebuildFromPool);
            return;
        }

        RebuildFromPool();
    }

    public void RebuildFromPool()
    {
        Clear();

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData?.characterIds == null ||
            playerData.characterIds.Count == 0 ||
            content == null ||
            cardPrefab == null ||
            CharacterPoolManager.Instance == null)
        {
            RefreshSelectedCharacter();
            return;
        }

        CharacterManager firstCharacter = null;

        foreach (string id in playerData.characterIds)
        {
            CharacterManager characterManager = CharacterPoolManager.Instance.Get(id);

            if (characterManager == null)
                continue;

            if (firstCharacter == null)
                firstCharacter = characterManager;

            CreateCharacterCard(characterManager.character, characterManager);
        }

        SelectCharacter(firstCharacter);
    }

    public void BuildCharacters(
        IReadOnlyList<CharacterData> characters,
        Action<CharacterData> onSelectionChanged)
    {
        Clear();
        showingExternalCharacters = true;
        externalSelectionChanged = onSelectionChanged;

        if (characters == null || content == null || cardPrefab == null)
        {
            RefreshSelectedCharacter();
            return;
        }

        CharacterData firstCharacter = null;

        foreach (CharacterData character in characters)
        {
            if (character == null)
                continue;

            if (firstCharacter == null)
                firstCharacter = character;

            CreateCharacterCard(character, null);
        }

        SelectCharacter(firstCharacter);
    }

    public void SelectCharacter(CharacterManager characterManager)
    {
        SelectedCharacter = characterManager;
        SelectCharacter(characterManager != null ? characterManager.character : null);

        if (inventoryUIController != null)
            inventoryUIController.SetSelectedCharacter(characterManager);
    }

    private void SelectCharacter(CharacterData character)
    {
        SelectedCharacterData = character;

        foreach (CharacterInfoPanel panel in infoPanels)
        {
            if (panel == null)
                continue;

            Image image = panel.GetComponent<Image>();

            bool selected = GetPanelData(panel) == character;

            if (image != null && panelColors.TryGetValue(image, out Color normalColor))
            {
                image.color = selected
                    ? new Color(0.72f, 0.52f, 0.2f, normalColor.a)
                    : normalColor;
            }

            Transform descriptionBar = panel.transform.Find("DescriptionBar");
            Image descriptionImage = descriptionBar != null ? descriptionBar.GetComponent<Image>() : null;

            if (descriptionImage != null && panelColors.TryGetValue(descriptionImage, out Color descriptionColor))
            {
                descriptionImage.color = selected
                    ? new Color(0.72f, 0.52f, 0.2f, descriptionColor.a)
                    : descriptionColor;
            }
        }

        RefreshSelectedCharacter();
        externalSelectionChanged?.Invoke(character);
    }

    public void RefreshAll()
    {
        foreach (CharacterInfoPanel panel in infoPanels)
        {
            if (panel != null)
                panel.Refresh();
        }

        RefreshSelectedCharacter();
    }

    public void OpenSelectedEquipment()
    {
        if (SelectedCharacter != null)
            uiManager?.OpenCharacterEquipment(SelectedCharacter);
    }

    public void OpenSelectedSkills()
    {
        if (SelectedCharacter != null)
            uiManager?.OpenCharacterSkills(SelectedCharacter);
    }

    public void OpenStorage()
    {
        uiManager?.OpenCompanyStorage();
    }

    public void ClosePanel()
    {
        if (closeRequested != null)
            closeRequested.Invoke();
        else if (uiManager != null)
            uiManager.CloseCharacterManagement();
        else
            gameObject.SetActive(false);
    }

    private void CreateCharacterCard(
        CharacterData character,
        CharacterManager characterManager)
    {
        if (character == null)
            return;

        GameObject cardObject = Instantiate(cardPrefab, content);
        CharacterInfoPanel panel = cardObject.GetComponent<CharacterInfoPanel>();

        if (panel == null)
        {
            Destroy(cardObject);
            return;
        }

        if (characterManager != null)
        {
            panel.Bind(characterManager);
            RewireCardButtons(panel, characterManager);
            panelCharacters[panel] = characterManager;
        }
        else
        {
            panel.Bind(character);
        }

        WireCardSelection(cardObject, character);

        Image panelImage = cardObject.GetComponent<Image>();

        if (panelImage != null)
            panelColors[panelImage] = panelImage.color;

        Transform descriptionBar = cardObject.transform.Find("DescriptionBar");
        Image descriptionImage = descriptionBar != null ? descriptionBar.GetComponent<Image>() : null;

        if (descriptionImage != null)
            panelColors[descriptionImage] = descriptionImage.color;

        panelData[panel] = character;
        infoPanels.Add(panel);
    }

    private void RewireCardButtons(
        CharacterInfoPanel panel,
        CharacterManager characterManager)
    {
        if (panel.btnEquipment != null)
        {
            panel.btnEquipment.onClick.RemoveAllListeners();
            panel.btnEquipment.onClick.AddListener(() =>
                uiManager?.OpenCharacterEquipment(characterManager));
        }

        if (panel.btnInventory != null)
        {
            panel.btnInventory.onClick.RemoveAllListeners();
            panel.btnInventory.onClick.AddListener(() =>
            {
                inventoryUIController?.SetSelectedCharacter(characterManager);
                uiManager?.OpenCompanyStorage();
            });
        }

        if (panel.btnSkills != null)
        {
            panel.btnSkills.onClick.RemoveAllListeners();
            panel.btnSkills.onClick.AddListener(() =>
                uiManager?.OpenCharacterSkills(characterManager));
        }
    }

    private void WireCardSelection(
        GameObject cardObject,
        CharacterData character)
    {
        Button selectButton = cardObject.GetComponent<Button>();

        if (selectButton == null)
            selectButton = cardObject.AddComponent<Button>();

        selectButton.targetGraphic = cardObject.GetComponent<Graphic>();
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => SelectCharacter(character));
    }

    private CharacterData GetPanelData(CharacterInfoPanel panel)
    {
        return panel != null && panelData.TryGetValue(panel, out CharacterData character)
            ? character
            : null;
    }

    private void RefreshSelectedCharacter()
    {
        CharacterData character = SelectedCharacterData;

        if (selectedPortrait != null)
        {
            selectedPortrait.texture = character != null ? character.Portrait : null;
            selectedPortrait.enabled = character != null && character.Portrait != null;
        }

        SetText(selectedNameText, character != null ? character.Name : "캐릭터를 선택하세요");
        SetText(selectedOriginText, character != null ? character.originName : "");
        SetText(selectedLevelText, character != null ? $"레벨 {character.Level}" : "");

        CharacterStats stats = character != null ? character.FinalStats : null;

        string resourceText = stats != null
            ? $"체력  {character.CurrentHp} / {stats.MaxHp}    " +
              $"지구력  {character.CurrentStamina} / {stats.MaxStamina}    " +
              $"정신력  {character.CurrentMentality} / {stats.MaxMentality}"
            : "";

        if (character != null && showingExternalCharacters)
            resourceText += $"    소속감  {character.Belonging}";

        SetText(selectedResourceText, resourceText);

        SetText(skillSummaryText, BuildSkillSummaryText(character));
        SetText(traitSummaryText, BuildTraitSummaryText(character));
        SetText(statSummaryText, BuildStatSummaryText(stats));
        statsDetailUI?.Refresh(stats);
        RebuildTraitCards(character);
        RebuildSkillCards(character);
    }

    private void RebuildTraitCards(CharacterData character)
    {
        ClearPreviewCards(traitCards);

        if (character?.Traits == null ||
            traitCardContent == null ||
            traitCardTemplate == null)
        {
            return;
        }

        foreach (TraitRuntimeData runtime in character.Traits)
        {
            TraitDefinitionSO trait = GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetTrait(runtime.traitId)
                : null;

            if (trait == null)
                continue;

            TownTraitPreviewCardUI card = Instantiate(traitCardTemplate, traitCardContent);
            card.gameObject.SetActive(true);
            card.Bind(runtime, trait);
            traitCards.Add(card);
        }
    }

    private void RebuildSkillCards(CharacterData character)
    {
        ClearPreviewCards(skillCards);

        if (character?.Skills == null ||
            skillCardContent == null ||
            skillCardTemplate == null)
        {
            return;
        }

        foreach (SkillRuntimeData runtime in character.Skills)
        {
            SkillDefinitionSO skill = GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetSkill(runtime.skillUid)
                : null;

            if (skill == null)
                continue;

            TownSkillPreviewCardUI card = Instantiate(skillCardTemplate, skillCardContent);
            card.gameObject.SetActive(true);
            card.Bind(skill);
            skillCards.Add(card);
        }
    }

    private void WireButtons(bool add)
    {
        WireButton(overviewButton, SelectStatsDetail, add);
        WireButton(equipmentButton, SelectTraitsDetail, add);
        WireButton(skillsButton, SelectSkillsDetail, add);
        WireButton(equipmentWindowButton, OpenSelectedEquipment, add);
        WireButton(skillWindowButton, OpenSelectedSkills, add);
        WireButton(storageButton, OpenStorage, add);
        WireButton(statSummaryButton, SelectStatsDetail, add);
        WireButton(traitSummaryButton, SelectTraitsDetail, add);
        WireButton(skillSummaryButton, SelectSkillsDetail, add);
        WireButton(closeButton, ClosePanel, add);
    }

    public void SelectStatsDetail()
    {
        SetDetailSection(DetailSection.Stats);
    }

    public void SelectTraitsDetail()
    {
        SetDetailSection(DetailSection.Traits);
    }

    public void SelectSkillsDetail()
    {
        SetDetailSection(DetailSection.Skills);
    }

    private void SetDetailSection(DetailSection section)
    {
        selectedDetailSection = section;

        const float gap = 0.01f;
        float statsWidth = selectedDetailSection == DetailSection.Stats ? 0.40f : 0.216f;
        float traitsWidth = selectedDetailSection == DetailSection.Traits
            ? 0.50f
            : selectedDetailSection == DetailSection.Stats ? 0.29f : 0.264f;
        float skillsWidth = selectedDetailSection == DetailSection.Skills
            ? 0.50f
            : selectedDetailSection == DetailSection.Stats ? 0.29f : 0.264f;
        float currentX = 0f;

        SetDetailPanelLayout(statsPreview, ref currentX, statsWidth, gap);
        SetDetailPanelLayout(traitsPreview, ref currentX, traitsWidth, gap);
        SetDetailPanelLayout(skillsPreview, ref currentX, skillsWidth, 0f);
        UpdateStatsPreviewColumns(selectedDetailSection != DetailSection.Stats);

        SetTabVisual(overviewButton, selectedDetailSection == DetailSection.Stats);
        SetTabVisual(equipmentButton, selectedDetailSection == DetailSection.Traits);
        SetTabVisual(skillsButton, selectedDetailSection == DetailSection.Skills);
        UpdatePreviewGridColumns();
    }

    private void UpdateStatsPreviewColumns(bool useSingleColumn)
    {
        if (statsPreview == null)
            return;

        ScrollRect scrollRect = statsPreview.GetComponentInChildren<ScrollRect>(true);

        if (scrollRect == null || scrollRect.content == null)
            return;

        RectTransform statsContent = scrollRect.content;
        statsContent.anchorMin = new Vector2(0f, statsContent.anchorMin.y);
        statsContent.anchorMax = new Vector2(1f, statsContent.anchorMax.y);
        statsContent.anchoredPosition = new Vector2(0f, statsContent.anchoredPosition.y);
        statsContent.sizeDelta = new Vector2(0f, statsContent.sizeDelta.y);

        for (int rowIndex = 0; rowIndex < statsContent.childCount; rowIndex++)
        {
            RectTransform row = statsContent.GetChild(rowIndex) as RectTransform;

            if (row == null)
                continue;

            List<RectTransform> statItems = new();
            RectTransform columnSeparator = null;

            for (int childIndex = 0; childIndex < row.childCount; childIndex++)
            {
                RectTransform child = row.GetChild(childIndex) as RectTransform;

                if (child == null)
                    continue;

                if (child.GetComponent<TownCharacterStatValueUI>() != null)
                    statItems.Add(child);
                else if (child.name == "ColumnSeparator")
                    columnSeparator = child;
            }

            if (statItems.Count == 0)
                continue;

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();

            if (!statRowHeights.TryGetValue(row, out float normalHeight))
            {
                normalHeight = rowLayout != null && rowLayout.preferredHeight > 0f
                    ? rowLayout.preferredHeight
                    : row.sizeDelta.y;
                statRowHeights.Add(row, normalHeight);
            }

            float rowHeight = useSingleColumn ? normalHeight * statItems.Count : normalHeight;
            row.sizeDelta = new Vector2(row.sizeDelta.x, rowHeight);

            if (rowLayout != null)
            {
                rowLayout.minHeight = rowHeight;
                rowLayout.preferredHeight = rowHeight;
            }

            for (int statIndex = 0; statIndex < statItems.Count; statIndex++)
            {
                RectTransform statItem = statItems[statIndex];

                if (!statsRectLayouts.TryGetValue(statItem, out RectTransformLayout normalLayout))
                {
                    normalLayout = new RectTransformLayout(statItem);
                    statsRectLayouts.Add(statItem, normalLayout);
                }

                if (useSingleColumn)
                {
                    float anchorMaxY = 1f - (float)statIndex / statItems.Count;
                    float anchorMinY = 1f - (float)(statIndex + 1) / statItems.Count;
                    statItem.anchorMin = new Vector2(0f, anchorMinY);
                    statItem.anchorMax = new Vector2(1f, anchorMaxY);
                    statItem.anchoredPosition = Vector2.zero;
                    statItem.sizeDelta = Vector2.zero;
                }
                else
                {
                    ApplyRectTransformLayout(statItem, normalLayout);
                }
            }

            if (columnSeparator != null)
            {
                if (!statsRectLayouts.TryGetValue(columnSeparator, out RectTransformLayout separatorLayout))
                {
                    separatorLayout = new RectTransformLayout(columnSeparator);
                    statsRectLayouts.Add(columnSeparator, separatorLayout);
                }

                columnSeparator.gameObject.SetActive(!useSingleColumn || statItems.Count > 1);

                if (useSingleColumn && statItems.Count > 1)
                {
                    columnSeparator.anchorMin = new Vector2(0.02f, 0.5f);
                    columnSeparator.anchorMax = new Vector2(0.98f, 0.5f);
                    columnSeparator.anchoredPosition = Vector2.zero;
                    columnSeparator.sizeDelta = new Vector2(0f, 1f);
                }
                else
                {
                    ApplyRectTransformLayout(columnSeparator, separatorLayout);
                }
            }
        }

        scrollRect.verticalNormalizedPosition = 1f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(statsContent);
    }

    private static void ApplyRectTransformLayout(
        RectTransform rectTransform,
        RectTransformLayout layout)
    {
        rectTransform.anchorMin = layout.anchorMin;
        rectTransform.anchorMax = layout.anchorMax;
        rectTransform.anchoredPosition = layout.anchoredPosition;
        rectTransform.sizeDelta = layout.sizeDelta;
    }

    private void UpdatePreviewGridColumns()
    {
        bool useSingleTraitColumn = selectedDetailSection != DetailSection.Traits;
        bool useSingleSkillColumn = selectedDetailSection != DetailSection.Skills;
        UpdatePreviewGrid(traitCardContent, useSingleTraitColumn);
        UpdatePreviewGrid(skillCardContent, useSingleSkillColumn);
    }

    private static void UpdatePreviewGrid(RectTransform content, bool useSingleColumn)
    {
        if (content == null)
            return;

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();

        if (grid == null)
            return;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();

        if (fitter != null)
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = useSingleColumn ? 1 : 2;

        if (useSingleColumn)
            FitSingleColumnGrid(content, grid);
        else
            FitTwoColumnGrid(content, grid);

        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        ScrollRect scrollRect = content.GetComponentInParent<ScrollRect>();

        if (scrollRect != null)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalNormalizedPosition = 1f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private static void FitSingleColumnGrid(RectTransform content, GridLayoutGroup grid)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float availableWidth = content.rect.width - grid.padding.horizontal;

        if (availableWidth > 0f)
            grid.cellSize = new Vector2(availableWidth, grid.cellSize.y);
    }

    private static void FitTwoColumnGrid(RectTransform content, GridLayoutGroup grid)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float availableWidth = content.rect.width - grid.padding.horizontal - grid.spacing.x;

        if (availableWidth > 0f)
            grid.cellSize = new Vector2(availableWidth * 0.5f, grid.cellSize.y);
    }

    private static void SetDetailPanelLayout(
        RectTransform panel,
        ref float currentX,
        float width,
        float gap)
    {
        if (panel != null)
        {
            panel.anchorMin = new Vector2(currentX, 0f);
            panel.anchorMax = new Vector2(currentX + width, 1f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = Vector2.zero;
        }

        currentX += width + gap;
    }

    private static void SetTabVisual(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null)
            return;

        button.targetGraphic.color = selected
            ? new Color(1f, 0.7f, 0.5f, 1f)
            : new Color(1f, 1f, 1f, 1f);
    }

    private static void WireButton(
        Button button,
        UnityEngine.Events.UnityAction action,
        bool add)
    {
        if (button == null)
            return;

        if (add)
            button.onClick.AddListener(action);
        else
            button.onClick.RemoveListener(action);
    }

    private static string BuildSkillSummaryText(CharacterData character)
    {
        if (character == null || character.Skills == null)
            return "0개 보유";

        int normalCount = 0;
        int counterCount = 0;

        foreach (SkillRuntimeData runtime in character.Skills)
        {
            SkillDefinitionSO skill = GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetSkill(runtime.skillUid)
                : null;

            if (skill == null)
                continue;

            if (skill.isCounterSkill)
                counterCount++;
            else
                normalCount++;
        }

        return
            $"<size=26><b>{normalCount + counterCount}</b></size>  개 습득\n" +
            $"<size=13>• 일반 스킬  {normalCount}\n" +
            $"• 대응 스킬  {counterCount}</size>";
    }

    private static string BuildTraitSummaryText(CharacterData character)
    {
        if (character == null || character.Traits == null)
            return "0개 보유";

        int positiveCount = 0;
        int negativeCount = 0;
        int mixedCount = 0;

        foreach (TraitRuntimeData runtime in character.Traits)
        {
            TraitDefinitionSO trait = GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetTrait(runtime.traitId)
                : null;

            if (trait == null)
                continue;

            switch (trait.polarity)
            {
                case TraitPolarity.Positive:
                    positiveCount++;
                    break;
                case TraitPolarity.Negative:
                    negativeCount++;
                    break;
                case TraitPolarity.Mixed:
                    mixedCount++;
                    break;
            }
        }

        return
            $"<size=26><b>{positiveCount + negativeCount + mixedCount}</b></size>  개 보유\n" +
            $"<size=13>• 긍정 특성  {positiveCount}\n" +
            $"• 부정/혼합  {negativeCount + mixedCount}</size>";
    }

    private static string BuildStatSummaryText(CharacterStats stats)
    {
        if (stats == null)
            return "능력치 없음";

        int mainStats =
            stats.Strength + stats.Dexterity + stats.Speed +
            stats.Intelligence + stats.Wisdom + stats.Health;
        int supportStats =
            stats.Vitality + stats.Endurance + stats.Detection + stats.Insight;

        return
            $"<size=26><b>{mainStats + supportStats}</b></size>  총 합계\n" +
            $"<size=13>• 주요 능력치  {mainStats}\n" +
            $"• 보조 능력치  {supportStats}</size>";
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private static void ClearPreviewCards<T>(List<T> cards)
        where T : Component
    {
        foreach (T card in cards)
        {
            if (card == null)
                continue;

            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }

        cards.Clear();
    }

    private void Clear()
    {
        if (content != null)
        {
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }

        infoPanels.Clear();
        panelColors.Clear();
        panelCharacters.Clear();
        panelData.Clear();
        ClearPreviewCards(traitCards);
        ClearPreviewCards(skillCards);
        SelectedCharacter = null;
        SelectedCharacterData = null;
        externalSelectionChanged = null;
        showingExternalCharacters = false;
    }
}
