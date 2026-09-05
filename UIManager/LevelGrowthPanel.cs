using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelGrowthPanel : MonoBehaviour
{
    [Header("Layout")]
    public TMP_Text titleText;
    public TMP_Text remainingText;
    public RectTransform[] choiceSlots = new RectTransform[3];
    public GameObject choiceCardPrefab;

    [Header("Stat Frames")]
    public Sprite commonFrame;
    public Sprite epicFrame;
    public Sprite goodFrame;
    public Sprite legendaryFrame;
    public Sprite rareFrame;

    private readonly Queue<CharacterManager> pendingCharacters = new();
    private readonly List<GameObject> spawnedCards = new();
    private readonly System.Random random = new();

    private CharacterManager currentManager;
    private Action onCompleted;
    private bool choiceLocked;

    public bool IsOpen => gameObject.activeInHierarchy;

    public bool Open(IEnumerable<CharacterManager> characters, Action completed = null)
    {
        pendingCharacters.Clear();

        if (characters != null)
        {
            foreach (CharacterManager manager in characters)
            {
                if (manager != null && manager.character != null &&
                    manager.character.IsMine && manager.character.PendingLevelUps > 0)
                {
                    pendingCharacters.Enqueue(manager);
                }
            }
        }

        if (pendingCharacters.Count == 0)
            return false;

        onCompleted = completed;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        ShowNextCharacter();
        return true;
    }

    private void ShowNextCharacter()
    {
        ClearCards();
        currentManager = null;

        while (pendingCharacters.Count > 0)
        {
            CharacterManager candidate = pendingCharacters.Dequeue();

            if (candidate != null && candidate.character != null &&
                candidate.character.PendingLevelUps > 0)
            {
                currentManager = candidate;
                break;
            }
        }

        if (currentManager == null)
        {
            Complete();
            return;
        }

        ShowChoices();
    }

    private void ShowChoices()
    {
        ClearCards();
        choiceLocked = false;

        CharacterData character = currentManager.character;
        LevelGrowthUtility.GetGrowthRange(
            character,
            out int minAmount,
            out int maxAmount);

        List<LevelUpStatChoice> choices = LevelGrowthUtility.CreateChoices(
            minAmount,
            maxAmount,
            random);

        if (titleText != null)
            titleText.text = $"{character.Name} 레벨업";

        if (remainingText != null)
            remainingText.text = $"남은 성장 선택  {character.PendingLevelUps}회";

        for (int i = 0; i < choices.Count && i < choiceSlots.Length; i++)
            CreateChoiceCard(choiceSlots[i], choices[i]);
    }

    private void CreateChoiceCard(RectTransform slot, LevelUpStatChoice choice)
    {
        if (slot == null || choiceCardPrefab == null)
            return;

        GameObject card = Instantiate(choiceCardPrefab, slot, false);
        RectTransform cardRect = card.transform as RectTransform;

        if (cardRect != null)
        {
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = Vector2.zero;
            cardRect.localScale = Vector3.one;
        }

        SetText(card.transform, "Text Title", GetStatName(choice.stat));
        SetText(card.transform, "Text Lvl", $"+{choice.amount}");
        SetText(card.transform, "Text Description", GetStatDescription(choice.stat));

        Transform flag = card.transform.Find("Flag");
        Image flagImage = flag != null ? flag.GetComponent<Image>() : null;

        if (flagImage != null)
            flagImage.sprite = GetFrame(choice.stat);

        Button button = card.GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(() => SelectChoice(choice));
        else
            Debug.LogError("성장 선택 카드에 Button 컴포넌트가 없습니다.", card);

        spawnedCards.Add(card);
    }

    private void SelectChoice(LevelUpStatChoice choice)
    {
        if (choiceLocked || currentManager == null)
            return;

        choiceLocked = true;
        SetCardsInteractable(false);

        if (!LevelGrowthUtility.ApplyPendingChoice(currentManager, choice))
        {
            choiceLocked = false;
            SetCardsInteractable(true);
            return;
        }

        CharacterData character = currentManager.character;

        if (currentManager.characterUIHandler != null)
            currentManager.UpdateCharacterUI();

        PlayerManager.Instance?.SaveCharacter(character);

        if (character.PendingLevelUps > 0)
            ShowChoices();
        else
            ShowNextCharacter();
    }

    private void SetCardsInteractable(bool interactable)
    {
        foreach (GameObject card in spawnedCards)
        {
            Button button = card != null ? card.GetComponent<Button>() : null;

            if (button != null)
                button.interactable = interactable;
        }
    }

    private void Complete()
    {
        ClearCards();
        currentManager = null;
        gameObject.SetActive(false);

        Action callback = onCompleted;
        onCompleted = null;
        callback?.Invoke();
    }

    private void ClearCards()
    {
        foreach (GameObject card in spawnedCards)
        {
            if (card != null)
                Destroy(card);
        }

        spawnedCards.Clear();
    }

    private static void SetText(Transform root, string childName, string value)
    {
        Transform child = root.Find(childName);
        TMP_Text text = child != null ? child.GetComponent<TMP_Text>() : null;

        if (text != null)
            text.text = value;
    }

    private Sprite GetFrame(StatRequirementType stat)
    {
        switch (stat)
        {
            case StatRequirementType.Strength:
            case StatRequirementType.Dexterity:
                return epicFrame;
            case StatRequirementType.Intelligence:
                return goodFrame;
            case StatRequirementType.Health:
            case StatRequirementType.Vitality:
                return legendaryFrame;
            case StatRequirementType.Wisdom:
                return rareFrame;
            default:
                return commonFrame;
        }
    }

    private static string GetStatName(StatRequirementType stat)
    {
        switch (stat)
        {
            case StatRequirementType.Strength: return "근력";
            case StatRequirementType.Dexterity: return "기량";
            case StatRequirementType.Speed: return "속도";
            case StatRequirementType.Health: return "건강";
            case StatRequirementType.Vitality: return "활력";
            case StatRequirementType.Intelligence: return "지능";
            case StatRequirementType.Wisdom: return "지혜";
            default: return stat.ToString();
        }
    }

    private static string GetStatDescription(StatRequirementType stat)
    {
        switch (stat)
        {
            case StatRequirementType.Strength:
                return "무기를 통한 물리 피해와 근력 기반 판정에 영향을 줍니다.";
            case StatRequirementType.Dexterity:
                return "무기 활용과 탐지 능력, 기량 기반 판정에 영향을 줍니다.";
            case StatRequirementType.Speed:
                return "행동 순서와 공격 속도, 지구력 최대치에 영향을 줍니다.";
            case StatRequirementType.Health:
                return "최대 체력과 상태이상 저항에 영향을 줍니다.";
            case StatRequirementType.Vitality:
                return "최대 체력과 지구력 회복에 영향을 줍니다.";
            case StatRequirementType.Intelligence:
                return "마법 피해와 정신력 회복, 통찰 판정에 영향을 줍니다.";
            case StatRequirementType.Wisdom:
                return "최대 정신력과 통찰 능력에 영향을 줍니다.";
            default:
                return "캐릭터의 기본 능력치를 증가시킵니다.";
        }
    }
}
