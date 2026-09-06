using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyFormationPanel : MonoBehaviour
{
    private const int MaximumPartySize = 4;

    [Header("Quest")]
    public TMP_Text questTitleText;
    public TMP_Text questInfoText;

    [Header("Party")]
    public PartyFormationSlotUI[] partySlots;
    public TMP_Text totalSortiePayText;
    public TMP_Text partyInfoText;
    public TMP_Text statusText;

    [Header("Roster")]
    public RectTransform rosterContent;
    public PartyFormationRosterItemUI rosterItemPrefab;

    [Header("Multiplayer Reserve")]
    public GameObject multiplayerLobbySection;
    public TMP_Text multiplayerStatusText;
    public TMP_Text lobbyChatLogText;
    public TMP_InputField lobbyChatInput;
    public Button lobbyChatSendButton;

    [Header("Actions")]
    public Button cancelButton;
    public Button departButton;

    private readonly List<PartyMemberSelection> selectedMembers = new();
    private readonly List<PartyFormationRosterItemUI> rosterItems = new();
    private QuestDef selectedQuest;

    private class PartyMemberSelection
    {
        public CharacterData character;
        public bool isFront;
    }

    private void OnEnable()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        if (departButton != null)
            departButton.onClick.AddListener(Depart);
    }

    private void OnDisable()
    {
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Cancel);

        if (departButton != null)
            departButton.onClick.RemoveListener(Depart);

        ClearRosterItems();
    }

    public void Open(QuestDef quest)
    {
        selectedQuest = quest;
        selectedMembers.Clear();
        gameObject.SetActive(true);

        if (multiplayerStatusText != null)
            multiplayerStatusText.text = "싱글플레이 로비\n호스트  나\n참가자 슬롯  연동 예정";

        if (lobbyChatLogText != null)
            lobbyChatLogText.text = "멀티플레이 로비 채팅이 여기에 표시됩니다.";

        RefreshQuestInfo();
        RefreshParty();
        BuildRoster();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void BuildRoster()
    {
        ClearRosterItems();

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData?.characterIds == null ||
            CharacterPoolManager.Instance == null)
        {
            SetStatus("보유 캐릭터 정보를 불러올 수 없습니다.");
            return;
        }

        if (!CharacterPoolManager.Instance.IsBuilt)
        {
            SetStatus("캐릭터 정보를 불러오는 중입니다.");
            CharacterPoolManager.Instance.BuildPoolFromPlayerData(BuildRoster);
            return;
        }

        foreach (string characterId in playerData.characterIds)
        {
            CharacterManager manager = CharacterPoolManager.Instance.Get(characterId);
            CharacterData character = manager != null ? manager.character : null;

            if (character == null ||
                !character.IsAlive ||
                rosterContent == null ||
                rosterItemPrefab == null)
                continue;

            PartyFormationRosterItemUI item = Instantiate(rosterItemPrefab, rosterContent);
            item.gameObject.SetActive(true);
            item.Bind(character, IsSelected(character), ToggleCharacter);
            rosterItems.Add(item);
        }

        SetStatus("");
    }

    private void ToggleCharacter(CharacterData character)
    {
        if (character == null || selectedQuest == null)
            return;

        int index = selectedMembers.FindIndex(member =>
            member.character != null && member.character.ID == character.ID);

        if (index >= 0)
        {
            selectedMembers.RemoveAt(index);
        }
        else
        {
            int maximum = Mathf.Min(MaximumPartySize, partySlots.Length);

            if (selectedMembers.Count >= maximum)
            {
                SetStatus($"최대 {maximum}명까지 편성할 수 있습니다.");
                return;
            }

            int frontCapacity = Mathf.CeilToInt(maximum * 0.5f);
            selectedMembers.Add(new PartyMemberSelection
            {
                character = character,
                isFront = selectedMembers.Count < frontCapacity
            });
        }

        SetStatus("");
        RefreshParty();
        RefreshRosterSelection();
    }

    private void RemoveMember(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= selectedMembers.Count)
            return;

        selectedMembers.RemoveAt(slotIndex);
        RefreshParty();
        RefreshRosterSelection();
    }

    private void ToggleRow(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= selectedMembers.Count)
            return;

        selectedMembers[slotIndex].isFront = !selectedMembers[slotIndex].isFront;
        RefreshParty();
    }

    private void RefreshQuestInfo()
    {
        if (selectedQuest == null)
            return;

        QuestStageDefinitionSO stage = GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetQuestStage(selectedQuest.stageKey)
            : null;

        if (questTitleText != null)
            questTitleText.text = selectedQuest.isRescueQuest
                ? $"[구출] {selectedQuest.title}"
                : selectedQuest.title;

        if (questInfoText != null)
        {
            string difficulty = selectedQuest.difficulty switch
            {
                QuestDifficulty.Easy => "쉬움",
                QuestDifficulty.Normal => "보통",
                QuestDifficulty.Hard => "어려움",
                _ => selectedQuest.difficulty.ToString()
            };

            string rescueObjective = selectedQuest.isRescueQuest
                ? $"목표  실종된 원정대원 {selectedQuest.rescueCharacterIds?.Count ?? 0}명 구출\n"
                : "";

            questInfoText.text =
                rescueObjective +
                $"T{selectedQuest.tier} · {difficulty}    " +
                $"스테이지  {(stage != null ? stage.stageName : selectedQuest.stageKey)}\n" +
                $"권장 레벨  Lv. {selectedQuest.recommendedLevel}    " +
                $"권장 인원  {selectedQuest.recommendedPartySize}명    " +
                $"최대 인원  {MaximumPartySize}명\n" +
                $"주요 보상  {QuestPanelItem.GetRewardText(selectedQuest)}";
        }
    }

    private void RefreshParty()
    {
        int maximum = selectedQuest != null
            ? Mathf.Min(MaximumPartySize, partySlots.Length)
            : 0;

        for (int i = 0; i < partySlots.Length; i++)
        {
            PartyFormationSlotUI slot = partySlots[i];

            if (slot == null)
                continue;

            bool available = i < maximum;
            slot.gameObject.SetActive(available);

            if (!available)
                continue;

            PartyMemberSelection member = i < selectedMembers.Count
                ? selectedMembers[i]
                : null;

            slot.Bind(
                i,
                member != null ? member.character : null,
                member != null && member.isFront,
                RemoveMember,
                ToggleRow);
        }

        int totalSortiePay = CalculateTotalSortiePay();
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (totalSortiePayText != null)
            totalSortiePayText.text = $"총 출전 수당  {totalSortiePay:N0} G";

        if (partyInfoText != null)
        {
            partyInfoText.text =
                $"현재 파티 인원  {selectedMembers.Count} / {maximum}명\n" +
                $"권장 파티 인원  {selectedQuest?.recommendedPartySize ?? 0}명\n" +
                $"계정 보유 골드  {(playerData != null ? playerData.gold : 0):N0} G\n" +
                $"파티장  {(selectedMembers.Count > 0 ? selectedMembers[0].character.Name : "-")}";
        }

        if (departButton != null)
            departButton.interactable = selectedQuest != null && selectedMembers.Count > 0;
    }

    private void RefreshRosterSelection()
    {
        for (int i = 0; i < rosterItems.Count; i++)
        {
            PartyFormationRosterItemUI item = rosterItems[i];

            if (item == null)
                continue;

            CharacterData character = item.Character;
            item.Bind(character, IsSelected(character), ToggleCharacter);
        }
    }

    private bool IsSelected(CharacterData character)
    {
        return character != null && selectedMembers.Exists(member =>
            member.character != null && member.character.ID == character.ID);
    }

    private int CalculateTotalSortiePay()
    {
        int total = 0;

        foreach (PartyMemberSelection member in selectedMembers)
        {
            if (member?.character != null)
                total += MercenaryGenerator.CalculateSortiePay(member.character);
        }

        return total;
    }

    private void Depart()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (selectedQuest == null || playerData == null || selectedMembers.Count == 0)
            return;

        int totalSortiePay = CalculateTotalSortiePay();

        if (playerData.gold < totalSortiePay)
        {
            SetStatus("출전 수당을 지급할 골드가 부족합니다.");
            return;
        }

        List<string> partyIds = new();

        foreach (PartyMemberSelection member in selectedMembers)
            partyIds.Add(member.character.ID);

        string leaderId = partyIds[0];

        if (!QuestManager.Instance.Accept(selectedQuest.id, leaderId, partyIds))
        {
            SetStatus("현재 다른 퀘스트가 진행 중이거나 의뢰를 수락할 수 없습니다.");
            return;
        }

        playerData.gold -= totalSortiePay;
        playerData.activeCharacterIds = new List<string>(partyIds);
        playerData.currentStage = selectedQuest.stageKey;

        foreach (PartyMemberSelection member in selectedMembers)
            playerData.SetPosition(member.character.ID, member.isFront);

        PlayerManager.Instance.SavePlayerDataToPlayFab();
        Close();
        UIManager.Instance?.OpenQuestNodeMap();
    }

    private void Cancel()
    {
        UIManager.Instance?.ReturnToQuestBoardFromPartyFormation();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void ClearRosterItems()
    {
        foreach (PartyFormationRosterItemUI item in rosterItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        rosterItems.Clear();
    }
}
