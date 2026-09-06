using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterSkillWindowUI : MonoBehaviour
{
    public RectTransform content;
    public SkillListItem itemPrefab;
    public TMP_Text characterNameText;
    public TMP_InputField searchInput;
    public Text legacyCharacterNameText;
    public InputField legacySearchInput;
    public Button closeButton;
    public Button[] filterButtons;
    public GameObject[] filterSelectedIndicators;

    private readonly List<SkillListItem> itemViews = new();
    private CharacterManager characterManager;
    private CharacterData characterData;
    private UnityAction[] filterActions;
    private int selectedFilter;

    private void OnEnable()
    {
        SharedInventoryUtility.CharacterChanged += OnCharacterChanged;

        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchChanged);

        if (legacySearchInput != null)
            legacySearchInput.onValueChanged.AddListener(OnSearchChanged);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (filterButtons != null)
        {
            filterActions = new UnityAction[filterButtons.Length];

            for (int i = 0; i < filterButtons.Length; i++)
            {
                if (filterButtons[i] == null)
                    continue;

                int filterIndex = i;
                filterActions[i] = () => SelectFilter(filterIndex);
                filterButtons[i].onClick.AddListener(filterActions[i]);
            }
        }

        UpdateFilterState();

        Refresh();
    }

    private void OnDisable()
    {
        SharedInventoryUtility.CharacterChanged -= OnCharacterChanged;

        if (searchInput != null)
            searchInput.onValueChanged.RemoveListener(OnSearchChanged);

        if (legacySearchInput != null)
            legacySearchInput.onValueChanged.RemoveListener(OnSearchChanged);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (filterButtons != null && filterActions != null)
        {
            int count = Mathf.Min(filterButtons.Length, filterActions.Length);

            for (int i = 0; i < count; i++)
            {
                if (filterButtons[i] != null && filterActions[i] != null)
                    filterButtons[i].onClick.RemoveListener(filterActions[i]);
            }
        }

        filterActions = null;
    }

    public void Open(CharacterManager manager)
    {
        characterManager = manager;
        characterData = manager != null ? manager.character : null;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Open(CharacterData character)
    {
        characterManager = null;
        characterData = character;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();

        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        ClearItems();

        CharacterData character = characterData;

        if (characterNameText != null)
            characterNameText.text = character != null ? character.Name : "";

        if (legacyCharacterNameText != null)
            legacyCharacterNameText.text = character != null ? character.Name : "";

        if (character == null || character.Skills == null || content == null || itemPrefab == null)
            return;

        string search = searchInput != null
            ? searchInput.text
            : legacySearchInput != null
                ? legacySearchInput.text
                : "";

        for (int i = 0; i < character.Skills.Count; i++)
        {
            SkillRuntimeData runtime = character.Skills[i];

            if (runtime == null)
                continue;

            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(runtime.skillUid);

            if (skill == null ||
                !MatchesSelectedFilter(skill) ||
                (!string.IsNullOrEmpty(search) && !skill.skillName.Contains(search)))
            {
                continue;
            }

            SkillListItem view = Instantiate(itemPrefab, content);
            view.gameObject.SetActive(true);
            view.Bind(i, skill, runtime, OnSkillClicked);
            itemViews.Add(view);
        }
    }

    private void OnSkillClicked(int index, int button)
    {
        if (button != (int)UnityEngine.EventSystems.PointerEventData.InputButton.Right ||
            characterManager == null ||
            characterManager.character == null ||
            characterManager.character.Skills == null ||
            index < 0 ||
            index >= characterManager.character.Skills.Count)
        {
            return;
        }

        SkillRuntimeData runtime = characterManager.character.Skills[index];

        if (runtime.quickSlot)
        {
            runtime.quickSlot = false;
        }
        else
        {
            if (!runtime.canUse)
                return;

            int quickSlotCount = characterManager.character.Skills.FindAll(s =>
                s != null && s.quickSlot).Count;

            if (quickSlotCount >= SkillManager.MaxQuickSlotCount)
                return;

            runtime.quickSlot = true;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHotbarSkills(characterManager);

        SharedInventoryUtility.SaveChanges(characterManager);
        Refresh();
    }

    private void OnSearchChanged(string value)
    {
        Refresh();
    }

    private void SelectFilter(int filterIndex)
    {
        selectedFilter = filterIndex;
        UpdateFilterState();
        Refresh();
    }

    private bool MatchesSelectedFilter(SkillDefinitionSO skill)
    {
        return selectedFilter switch
        {
            1 => skill.type == SkillType.Physical,
            2 => skill.type == SkillType.Magical,
            3 => skill.isCounterSkill,
            _ => true
        };
    }

    private void UpdateFilterState()
    {
        if (filterSelectedIndicators == null)
            return;

        for (int i = 0; i < filterSelectedIndicators.Length; i++)
        {
            if (filterSelectedIndicators[i] != null)
                filterSelectedIndicators[i].SetActive(i == selectedFilter);
        }
    }

    private void OnCharacterChanged(CharacterManager manager)
    {
        if (manager == characterManager)
            Refresh();
    }

    private void ClearItems()
    {
        foreach (SkillListItem view in itemViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        itemViews.Clear();
    }
}
