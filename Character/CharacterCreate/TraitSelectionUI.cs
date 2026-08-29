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

        if (GameDataRegistry.Instance == null)
            return;

        List<TraitDefinitionSO> traits = GameDataRegistry.Instance.GetAllTraits();

        if (traits == null)
            return;

        foreach (TraitDefinitionSO trait in traits)
        {
            if (trait == null || IsCurrentOriginTrait(trait.id))
                continue;

            availableTraits.Add(trait);
        }

        availableTraits.Sort(CompareTraits);
    }

    private bool IsCurrentOriginTrait(int id)
    {
        if (characterCreation == null ||
            characterCreation.characterManager == null ||
            characterCreation.characterManager.character == null)
            return false;

        return characterCreation.characterManager.character.HasCharacterTrait(id);
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

        RefreshTraitButtonInteractability();
    }

    private void RefreshTraitButtonInteractability()
    {
        foreach (Transform child in leftPanelContent)
        {
            TraitButton traitButton = child.GetComponent<TraitButton>();

            if (traitButton == null ||
                traitButton.trait == null ||
                traitButton.button == null)
            {
                continue;
            }

            int traitCost = DetermineTraitCost(traitButton.trait);
            traitButton.button.interactable = traitCost <= 0 || availableTraitPoints >= traitCost;
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
                SortPanelByTraitOrder(rightPanelContent);

                TraitManager.AddTrait(characterCreation.characterManager, trait.id);

                availableTraitPoints -= traitCost;

                UpdateUI();
                RefreshTraitButtonInteractability();
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
                SortPanelByTraitOrder(leftPanelContent);

                TraitManager.RemoveTrait(characterCreation.characterManager, trait.id);

                availableTraitPoints += traitCost;

                UpdateUI();
                RefreshTraitButtonInteractability();
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
        RefreshTraitButtonInteractability();
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

    private void SortPanelByTraitOrder(Transform panel)
    {
        List<TraitButton> buttons = new();

        foreach (Transform child in panel)
        {
            TraitButton traitButton = child.GetComponent<TraitButton>();

            if (traitButton != null && traitButton.trait != null)
                buttons.Add(traitButton);
        }

        buttons.Sort((left, right) => CompareTraits(left.trait, right.trait));

        for (int i = 0; i < buttons.Count; i++)
            buttons[i].transform.SetSiblingIndex(i);
    }

    private int CompareTraits(TraitDefinitionSO left, TraitDefinitionSO right)
    {
        int polarityComparison = GetPolarityOrder(left.polarity).CompareTo(GetPolarityOrder(right.polarity));

        if (polarityComparison != 0)
            return polarityComparison;

        int gradeComparison = right.defaultAcquireGrade.CompareTo(left.defaultAcquireGrade);

        if (gradeComparison != 0)
            return gradeComparison;

        return left.id.CompareTo(right.id);
    }

    private int GetPolarityOrder(TraitPolarity polarity)
    {
        switch (polarity)
        {
            case TraitPolarity.Positive:
                return 0;

            case TraitPolarity.Mixed:
                return 1;

            case TraitPolarity.Negative:
                return 2;

            default:
                return 0;
        }
    }
}
