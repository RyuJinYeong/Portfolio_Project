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
    public int availableTraitPoints = 5;

    private CharacterCreation characterCreation; // CharacterCreation 참조

    private List<TraitBase> availableTraits = new List<TraitBase>
    {
        new OneArmedTrait(),
        new DunceTrait(),
        new KeenEyeTrait(),
        new SwiftMovementTrait(),
        new KeenInsightTrait()
        //new BasicElementalAptitudeTrait() - 특성이 추가되자마자 적성으로 변환되어서 Remove가 수행이 안되는 오류 발생
        // 나머지 특성들 추가
    };

    private void Start()
    {
        characterCreation = this.GetComponent<CharacterCreation>(); // CharacterCreation 스크립트 참조
        PopulateLeftPanel();
        UpdateUI();
    }

    private void InitializeTraitPoints()
    {
        availableTraitPoints = characterCreation.traitPoint; // CharacterCreation에서 특성 포인트 가져오기
    }

    void PopulateLeftPanel()
    {
        foreach (TraitBase trait in availableTraits)
        {
            int traitCost = DetermineTraitCost(trait);
            GameObject button = Instantiate(traitButtonPrefab, leftPanelContent);
            TraitButton traitButton = button.GetComponent<TraitButton>();
            traitButton.Initialize(trait, this, false, traitCost);
        }
    }

    public int DetermineTraitCost(TraitBase trait)
    {
        // 특성 비용을 결정하는 하드코딩된 로직
        if (trait.Name == "외팔") return -20;
        else if (trait.Name == "둔재") return -20;
        // 나머지 특성 비용 설정
        return 5; // 기본 비용
    }

    public void MoveTraitToRight(TraitBase trait)
    {
        int traitCost = DetermineTraitCost(trait);

        if (availableTraitPoints >= traitCost)
        {
            foreach (Transform child in leftPanelContent)
            {
                TraitButton button = child.GetComponent<TraitButton>();
                if (button != null && button.trait == trait)
                {
                    Destroy(child.gameObject);
                    GameObject newButton = Instantiate(traitButtonPrefab, rightPanelContent);
                    TraitButton newTraitButton = newButton.GetComponent<TraitButton>();
                    newTraitButton.Initialize(trait, this, true, traitCost);
                    TraitManager.AddTrait(characterCreation.selectOrigin, trait);
                    availableTraitPoints -= traitCost;
                    UpdateUI();
                    characterCreation.UIupdate();
                    break;
                }
            }
        }
    }

    public void MoveTraitToLeft(TraitBase trait)
    {
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
                TraitManager.RemoveTrait(characterCreation.selectOrigin, trait);
                
                availableTraitPoints += traitCost;
                UpdateUI();
                characterCreation.UIupdate();
                break;
            }
        }
    }

    public void UpdateUI()
    {
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
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in rightPanelContent)
        {
            Destroy(child.gameObject);
        }
        PopulateLeftPanel();
        UpdateUI();
    }
}
