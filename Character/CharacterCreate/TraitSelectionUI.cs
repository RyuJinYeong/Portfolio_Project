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

    // 출신지 "기본 특성"은 UI에서 제외
    private static readonly int[] EXCLUDE_ORIGIN_BASE_IDS = { 1000, 1001, 1002, 1006, 1007 };
    // 카탈로그: 원소 적성 4종 + 일반 긍정(신속/눈썰미/통찰) + 부정(외팔/둔재)
    private static readonly int[] CATALOG_IDS = {
        1010, 1011, 1012, 1013,   // 원소 적성: 화염/물/땅/바람
        1003, 1004, 1005,         // 신속/눈썰미/통찰
        5000, 5001                // 외팔/둔재
    };

    // DB에서 만든 선택 가능 특성들
    private readonly List<TraitBase> availableTraits = new();

    private void Awake()
    {
        characterCreation = this.GetComponent<CharacterCreation>(); // CharacterCreation 스크립트 참조
        InitializeTraitPoints();

        BuildAvailableTraitCatalog();
        PopulateLeftPanel();
        UpdateUI();
    }

    private void InitializeTraitPoints()
    {
        if (characterCreation != null)
            availableTraitPoints = characterCreation.traitPoint; // CharacterCreation에서 특성 포인트 가져오기
    }

    // DB 기반 카탈로그 구성 (기본 특성 제외)
    private void BuildAvailableTraitCatalog()
    {
        availableTraits.Clear();
        foreach (var id in CATALOG_IDS)
        {
            bool excluded = false;
            for (int i = 0; i < EXCLUDE_ORIGIN_BASE_IDS.Length; i++)
                if (EXCLUDE_ORIGIN_BASE_IDS[i] == id) { excluded = true; break; }
            if (excluded) continue;

            var t = TraitDatabase.Create(id);
            if (t != null) availableTraits.Add(t);
        }
    }

    void PopulateLeftPanel()
    {
        foreach (TraitBase trait in availableTraits)
        {
            int traitCost = DetermineTraitCost(trait); // 등급/극성 기반
            GameObject button = Instantiate(traitButtonPrefab, leftPanelContent);
            TraitButton traitButton = button.GetComponent<TraitButton>();
            traitButton.Initialize(trait, this, false, traitCost);
        }
    }

    // 등급/극성 기반 비용 계산
    public int DetermineTraitCost(TraitBase trait)
    {
        int baseCost = TraitEconomy.BaseCostByGrade(trait.Grade);
        switch (trait.Polarity)
        {
            case TraitPolarity.Positive: return baseCost;
            case TraitPolarity.Negative: return -baseCost;
            case TraitPolarity.Mixed: return 0;
            default: return baseCost;
        }
    }

    public void MoveTraitToRight(TraitBase trait)
    {
        int traitCost = DetermineTraitCost(trait);

        // 긍정(양수)은 포인트 부족 시 불가 / 부정(음수)은 환급이므로 허용
        if (traitCost > 0 && availableTraitPoints < traitCost) return;

        foreach (Transform child in leftPanelContent)
        {
            TraitButton button = child.GetComponent<TraitButton>();
            if (button != null && button.trait == trait)
            {
                // UI 이동: 좌 → 우
                Destroy(child.gameObject);
                GameObject newButton = Instantiate(traitButtonPrefab, rightPanelContent);
                TraitButton newTraitButton = newButton.GetComponent<TraitButton>();
                newTraitButton.Initialize(trait, this, true, traitCost);

                // 즉시 적용 (레벨 고려 X, 단일 습득)
                TraitManager.AddTrait(characterCreation.characterManager, trait);

                // 포인트 반영 (양수면 차감, 음수면 환급)
                availableTraitPoints -= traitCost;

                UpdateUI();
                characterCreation.UIupdate();
                break;
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
                // UI 이동: 우 → 좌
                Destroy(child.gameObject);
                GameObject newButton = Instantiate(traitButtonPrefab, leftPanelContent);
                TraitButton newTraitButton = newButton.GetComponent<TraitButton>();
                newTraitButton.Initialize(trait, this, false, traitCost);

                // 즉시 제거
                TraitManager.RemoveTrait(characterCreation.characterManager, trait);

                // 포인트 환원(긍정) / 차감(부정)
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
        foreach (Transform child in leftPanelContent) Destroy(child.gameObject);
        foreach (Transform child in rightPanelContent) Destroy(child.gameObject);

        BuildAvailableTraitCatalog();
        PopulateLeftPanel();
        UpdateUI();
    }
}
