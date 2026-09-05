using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestEncounterDialog : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public RectTransform choiceContainer;
    public Button choiceButtonTemplate;

    private readonly List<Button> spawnedButtons = new();
    private QuestEncounterDefinitionSO encounter;
    private bool insightSucceeded;

    private void Awake()
    {
        if (choiceButtonTemplate != null)
            choiceButtonTemplate.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StageManager.Instance?.DeactivateEncounterPresentation();
    }

    public void OpenCurrentEncounter()
    {
        encounter = QuestManager.Instance != null
            ? QuestManager.Instance.GetOrAssignCurrentEncounter()
            : null;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (encounter == null)
        {
            ShowFallback();
            return;
        }

        StageManager.Instance?.ActivateEncounterPresentation(encounter);

        if (titleText != null)
            titleText.text = encounter.title;

        insightSucceeded = QuestManager.Instance != null &&
                           QuestManager.Instance.ResolveCurrentEncounterInsight(encounter);

        if (encounter.insight != null && encounter.insight.enabled)
            PlayerManager.Instance?.SavePlayerDataToPlayFab();

        SetEncounterDescription();

        ShowChoices();
    }

    private void ShowChoices()
    {
        ClearButtons();

        if (encounter == null || encounter.choices == null || encounter.choices.Count == 0)
        {
            AddButton("계속", CompleteWithoutEffect);
            return;
        }

        for (int i = 0; i < encounter.choices.Count; i++)
        {
            QuestEncounterChoiceData choice = encounter.choices[i];

            if (choice == null)
                continue;

            int displayIndex = i + 1;
            string checkText = string.Empty;

            if (insightSucceeded && choice.abilityCheck != null && choice.abilityCheck.enabled)
            {
                string statName = QuestEncounterResolver.GetCheckStatName(choice.abilityCheck.stat);
                checkText = choice.requiresCharacter
                    ? $" [{statName} 판정]"
                    : $" [{statName} {QuestEncounterResolver.GetSuccessChance(choice, null):0}%]";
            }

            AddButton(
                $"{displayIndex}. {choice.text}{checkText}",
                () => SelectChoice(choice));
        }
    }

    private void SelectChoice(QuestEncounterChoiceData choice)
    {
        if (choice == null)
            return;

        if (choice.requiresCharacter)
        {
            ShowCharacterChoices(choice);
            return;
        }

        ResolveChoice(choice, null);
    }

    private void ShowCharacterChoices(QuestEncounterChoiceData choice)
    {
        ClearButtons();

        if (bodyText != null)
            bodyText.text = $"{GetEncounterDescription()}\n\n누가 ‘{choice.text}’ 선택을 실행합니까?";

        List<CharacterManager> party = GetPartyCharacters(choice);

        if (party.Count == 0)
        {
            if (bodyText != null)
            {
                bodyText.text =
                    $"{GetEncounterDescription()}\n\n" +
                    "현재 퀘스트 티어에서는 이 선택을 실행할 수 있는 캐릭터가 없습니다.";
            }

            AddButton("돌아가기", () =>
            {
                SetEncounterDescription();
                ShowChoices();
            });
            return;
        }

        for (int i = 0; i < party.Count; i++)
        {
            CharacterManager manager = party[i];
            int displayIndex = i + 1;
            string chanceText = insightSucceeded && choice.abilityCheck != null &&
                                choice.abilityCheck.enabled
                ? $" ({QuestEncounterResolver.GetSuccessChance(choice, manager):0}%)"
                : string.Empty;
            AddButton(
                $"{displayIndex}. {manager.character.Name}{chanceText}",
                () => ResolveChoice(choice, manager));
        }

        AddButton("돌아가기", () =>
        {
            SetEncounterDescription();

            ShowChoices();
        });
    }

    private void ResolveChoice(
        QuestEncounterChoiceData choice,
        CharacterManager selectedCharacter)
    {
        QuestEncounterResolution resolution = QuestEncounterResolver.Resolve(
            choice,
            selectedCharacter);

        if (bodyText != null)
            bodyText.text = resolution.message;

        ClearButtons();
        AddButton("계속", () => FinishResolution(resolution));
    }

    private void FinishResolution(QuestEncounterResolution resolution)
    {
        ClearButtons();
        gameObject.SetActive(false);

        if (resolution != null && resolution.startsBattle)
        {
            QuestManager.Instance?.BeginCurrentEncounterBattle(resolution.monsterRoleIds);
            PlayerManager.Instance?.SavePlayerDataToPlayFab();
            GameManager.Instance?.StartEncounterBattle(resolution.monsterRoleIds);
            return;
        }

        QuestManager.Instance?.ResolveCurrentEncounterNode();
        PlayerManager.Instance?.SavePlayerDataToPlayFab();
        UIManager.Instance?.OpenQuestNodeMap();
    }

    private void CompleteWithoutEffect()
    {
        FinishResolution(new QuestEncounterResolution
        {
            message = "특별한 일은 일어나지 않았습니다."
        });
    }

    private void ShowFallback()
    {
        QuestRouteNode node = QuestManager.Instance != null
            ? QuestManager.Instance.GetCurrentRouteNode()
            : null;

        if (titleText != null)
            titleText.text = node != null && node.type == QuestRouteNodeType.Rest
                ? "안전 구역"
                : "주변 탐색";

        if (bodyText != null)
            bodyText.text = node != null && node.type == QuestRouteNodeType.Rest
                ? "잠시 숨을 돌릴 수 있는 안전한 장소입니다."
                : "주변을 살펴보았지만 특별한 것은 발견하지 못했습니다.";

        ClearButtons();
        AddButton("계속", CompleteWithoutEffect);
    }

    private List<CharacterManager> GetPartyCharacters(QuestEncounterChoiceData choice)
    {
        List<CharacterManager> result = new List<CharacterManager>();

        if (GameManager.Instance == null)
            return result;

        foreach (CharacterManager manager in GameManager.Instance.GetAllCharacters())
        {
            if (manager != null && manager.character != null && manager.character.IsMine &&
                QuestEncounterResolver.CanSelectCharacter(choice, manager))
            {
                result.Add(manager);
            }
        }

        return result;
    }

    private void SetEncounterDescription()
    {
        if (bodyText != null)
            bodyText.text = GetEncounterDescription();
    }

    private string GetEncounterDescription()
    {
        string description = encounter != null ? encounter.description : string.Empty;

        if (!insightSucceeded || encounter == null || encounter.insight == null ||
            string.IsNullOrEmpty(encounter.insight.revealedText))
        {
            return description;
        }

        return $"{description}\n\n<color=#D8B56A>간파:</color> {encounter.insight.revealedText}";
    }

    private void AddButton(string text, Action onClick)
    {
        if (choiceButtonTemplate == null || choiceContainer == null)
            return;

        Button button = Instantiate(choiceButtonTemplate, choiceContainer);
        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
            label.text = text;

        spawnedButtons.Add(button);
        int columnCount = 1;
        int rowCount = spawnedButtons.Count;
        float rowHeight = Mathf.Clamp(
            (choiceContainer.rect.height - (rowCount - 1) * 6f) / Mathf.Max(1, rowCount),
            30f,
            52f);

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            RectTransform rect = spawnedButtons[i].transform as RectTransform;

            if (rect == null)
                continue;

            int column = i % columnCount;
            int row = i / columnCount;
            float columnWidth = 1f / columnCount;

            rect.anchorMin = new Vector2(column * columnWidth, 1f);
            rect.anchorMax = new Vector2((column + 1) * columnWidth, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -row * (rowHeight + 6f));
            rect.sizeDelta = new Vector2(-8f, rowHeight);
        }
    }

    private void ClearButtons()
    {
        foreach (Button button in spawnedButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        spawnedButtons.Clear();
    }
}
