using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TraitSelectionUI : MonoBehaviour
{
    public GameObject traitButtonPrefab;
    public Transform leftPanelContent;
    public Transform rightPanelContent;
    public TextMeshProUGUI traitPointsText;
    public int availableTraitPoints = 8;

    private CharacterCreation characterCreation;

    private static readonly int[] EXCLUDE_ORIGIN_BASE_IDS = { 1000, 1001, 1002, 1006, 1007 };

    private static readonly int[] CATALOG_IDS =
    {
    1010, 1011, 1012, 1013, // 盔家 利己
    1003, 1004, 1005,       // 老馆 编沥
    5000, 5001              // 何沥
    };

    private readonly List<TraitDefinitionSO> availableTraits = new();

    private void Awake()
    {
        characterCreation = GetComponent<CharacterCreation>();

        InitializeTraitPoints();
        BuildAvailableTraitCatalog();
        PopulateLeftPanel();
        UpdateUI();
    }

    private void InitializeTraitPoints()
    {
        if (characterCreation != null)
            availableTraitPoints = characterCreation.traitPoint;
    }

    private void BuildAvailableTraitCatalog()
    {
        availableTraits.Clear();

        foreach (int id in CATALOG_IDS)
        {
            if (IsExcludedOriginBaseTrait(id))
                continue;

            TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(id);

            if (trait != null)
                availableTraits.Add(trait);
        }
    }

    private bool IsExcludedOriginBaseTrait(int id)
    {
        for (int i = 0; i < EXCLUDE_ORIGIN_BASE_IDS.Length; i++)
        {
            if (EXCLUDE_ORIGIN_BASE_IDS[i] == id)
                return true;
        }

        return false;
    }

    void PopulateLeftPanel()
    {
        foreach (TraitDefinitionSO trait in availableTraits)
        {
            int traitCost = DetermineTraitCost(trait);

            GameObject button = Instantiate(traitButtonPrefab, leftPanelContent);
            TraitButton traitButton = button.GetComponent<TraitButton>();

            traitButton.Initialize(trait, this, false, traitCost);
        }
    }

    public int DetermineTraitCost(TraitDefinitionSO trait)
    {
        if (trait == null)
            return 0;

        int baseCost = TraitGradeUtility.GetGradeValue(trait.defaultAcquireGrade);

        switch (trait.polarity)
        {
            case TraitPolarity.Positive:
                return baseCost;

            case TraitPolarity.Negative:
                return -baseCost;

            case TraitPolarity.Mixed:
                return 0;

            default:
                return baseCost;
        }
    }

    public void MoveTraitToRight(TraitDefinitionSO trait)
    {
        if (trait == null || characterCreation == null)
            return;

        int traitCost = DetermineTraitCost(trait);

        if (traitCost > 0 && availableTraitPoints < traitCost)
            return;

        foreach (Transform child in leftPanelContent)
        {
            TraitButton button = child.GetComponent<TraitButton>();

            if (button != null && button.trait == trait)
            {
                Destroy(child.gameObject);

                GameObject newButton = Instantiate(traitButtonPrefab, rightPanelContent);
                TraitButton newTraitButton = newButton.GetComponent<TraitButton>();
                newTraitButton.Initialize(trait, this, true, traitCost);

                TraitManager.AddTrait(characterCreation.characterManager, trait.id);

                availableTraitPoints -= traitCost;

                UpdateUI();
                characterCreation.UIupdate();
                break;
            }
        }
    }

    public void MoveTraitToLeft(TraitDefinitionSO trait)
    {
        if (trait == null || characterCreation == null)
            return;

        int traitCost = DetermineTraitCost(trait);

        foreach (Transform child in rightPanelContent)
        {
            TraitButton button = child.GetComponent<TraitButton>();

            if (button != null && button.trait == trait)
            {
                Destroy(child.gameObject);

                GameObject newButton = Instantiate(traitButtonPrefab, leftPanelContent);
                TraitButton newTraitButton = newButton.GetComponent<TraitButton>();
                newTraitButton.Initialize(trait, this, false, traitCost);

                TraitManager.RemoveTrait(characterCreation.characterManager, trait.id);

                availableTraitPoints += traitCost;

                UpdateUI();
                characterCreation.UIupdate();
                break;
            }
        }
    }

    public void UpdateUI()
    {
        if (traitPointsText != null)
            traitPointsText.text = $"{availableTraitPoints}";
    }

    public void SetTraitPoints(int points)
    {
        availableTraitPoints = points;
        UpdateUI();
    }

    public void ResetTraitSelection()
    {
        foreach (Transform child in leftPanelContent)
            Destroy(child.gameObject);

        foreach (Transform child in rightPanelContent)
            Destroy(child.gameObject);

        BuildAvailableTraitCatalog();
        PopulateLeftPanel();
        UpdateUI();
    }
}