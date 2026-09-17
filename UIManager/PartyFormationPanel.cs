using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
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
    public Button inviteButton;
    public Button copyCodeButton;
    public TMP_Text roomCodeText;
    public GameObject friendlyControls;
    public TMP_Text opposingTeamText;
    private bool IsFriendly => session != null && session.IsFriendlyRoom;

    private readonly List<PartyMemberSelection> selectedMembers = new();
    private readonly List<PartyFormationRosterItemUI> rosterItems = new();
    private QuestDef selectedQuest;
    private MultiplayerSession session;
    private bool awaitingFormation;
    private bool uploadLocalFormation;
    private bool leavingForSolo;

    private class PartyMemberSelection
    {
        public CharacterData character;
        public string ownerId;
        public bool isFront;
    }

    private void OnEnable()
    {
        session = MultiplayerSession.Instance;
        if (session != null)
        {
            session.FormationChanged += OnFormationChanged;
            session.Changed += RefreshRoom;
        }
        if (copyCodeButton != null) copyCodeButton.onClick.AddListener(CopyCode);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        if (departButton != null)
            departButton.onClick.AddListener(Depart);

        if (inviteButton != null)
            inviteButton.onClick.AddListener(Invite);
    }

    private void OnDisable()
    {
        if (session != null)
        {
            session.FormationChanged -= OnFormationChanged;
            session.Changed -= RefreshRoom;
        }
        if (copyCodeButton != null) copyCodeButton.onClick.RemoveListener(CopyCode);
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Cancel);

        if (departButton != null)
            departButton.onClick.RemoveListener(Depart);

        if (inviteButton != null)
            inviteButton.onClick.RemoveListener(Invite);

        ClearRosterItems();
    }

    public void Open(QuestDef quest)
    {
        selectedQuest = quest;
        selectedMembers.Clear();
        gameObject.SetActive(true);
        awaitingFormation = false;
        uploadLocalFormation = session != null && !session.Connected;
        if (session != null && session.Connected)
        {
            uploadLocalFormation = session.IsHost && session.Formation.quest?.id != quest.id;
            session.SelectQuest(quest);
            if (!uploadLocalFormation) OnFormationChanged();
        }
        else session?.OpenQuestRoom(quest);

        if (multiplayerStatusText != null)
            multiplayerStatusText.text = session != null && session.Connected ? session.MembersText : "싱글플레이";

        if (lobbyChatLogText != null)
            lobbyChatLogText.text = "멀티플레이 로비 채팅이 여기에 표시됩니다.";

        RefreshQuestInfo();
        RefreshParty();
        BuildRoster();
        RefreshRoom();
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
        if (awaitingFormation || character == null || selectedQuest == null)
            return;
        if (session != null && (session.Formation.savedExpeditionId != null || session.IsExpeditionCharacterLocked(character.ID)))
        {
            SetStatus("보관된 원정에 참여 중인 용병입니다. 해당 원정을 재개하거나 포기해 주세요.");
            return;
        }

        int index = selectedMembers.FindIndex(member =>
            member.character != null && member.character.ID == character.ID);

        if (index >= 0)
        {
            selectedMembers.RemoveAt(index);
        }
        else
        {
            int maximum = Mathf.Min(IsFriendly ? session.Formation.friendly.teamSize : MaximumPartySize, partySlots.Length);

            if (selectedMembers.Count >= maximum)
            {
                SetStatus($"최대 {maximum}명까지 편성할 수 있습니다.");
                return;
            }

            int frontCapacity = Mathf.CeilToInt(maximum * 0.5f);
            selectedMembers.Add(new PartyMemberSelection
            {
                character = character,
                ownerId = session != null && session.Connected ? session.LocalProfileId : null,
                isFront = selectedMembers.Count < frontCapacity
            });
        }

        ClearReadyAfterPartyChange();
        SetStatus("");
        RefreshParty();
        RefreshRosterSelection();
    }

    private void RemoveMember(int slotIndex)
    {
        if (session != null && session.Formation.savedExpeditionId != null) return;
        if (slotIndex < 0 || slotIndex >= selectedMembers.Count)
            return;

        if (session != null && session.Connected)
        {
            var member = selectedMembers[slotIndex];
            if (awaitingFormation || (member.ownerId != session.LocalProfileId && !session.IsHost)) return;
            awaitingFormation = true;
            session.RemoveFormationSlot(member.character.ID);
            return;
        }
        selectedMembers.RemoveAt(slotIndex);
        ClearReadyAfterPartyChange();
        RefreshParty();
        RefreshRosterSelection();
    }

    private void ToggleRow(int slotIndex)
    {
        if (session != null && session.Formation.savedExpeditionId != null) return;
        if (slotIndex < 0 || slotIndex >= selectedMembers.Count)
            return;

        if (awaitingFormation || (session != null && session.Connected && selectedMembers[slotIndex].ownerId != session.LocalProfileId)) return;
        selectedMembers[slotIndex].isFront = !selectedMembers[slotIndex].isFront;
        ClearReadyAfterPartyChange();
        RefreshParty();
    }

    private void RefreshQuestInfo()
    {
        if (questInfoText != null) questInfoText.gameObject.SetActive(!IsFriendly);
        if (friendlyControls != null) friendlyControls.SetActive(IsFriendly);
        if (IsFriendly)
        {
            questTitleText.text = "친선전 · 파티 편성";
            questInfoText.text = $"{session.Formation.friendly.teamSize} vs {session.Formation.friendly.teamSize} · " +
                (session.Formation.friendly.multiplePlayersPerTeam ? "다인 팀전" : "용병단 1 vs 1") +
                "\n마을에서 전투 · 영구 사망 없음 · 종료 후 회복\n아이템 등록 → 각자 잠금 → 모두 수락 → 호스트 시작";
            return;
        }
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
            ? Mathf.Min(IsFriendly ? session.Formation.friendly.teamSize : MaximumPartySize, partySlots.Length)
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
                ToggleRow,
                OpenMemberEquipment);
            if (member != null)
            {
                bool local = session == null || !session.Connected || member.ownerId == session.LocalProfileId;
                slot.rowButton.interactable = local && !awaitingFormation;
                slot.equipmentButton.interactable = local && !awaitingFormation;
                slot.removeButton.interactable = (local || session.IsHost) && !awaitingFormation;
                if (session != null && session.Connected)
                {
                    var owner = session.Formation.members.Find(value => value.ownerId == member.ownerId);
                    slot.characterText.text = $"Lv. {member.character.Level}  {member.character.Name}\n" +
                        $"{owner?.name ?? "참가자"} · {member.character.originName}\n" +
                        (IsFriendly ? "친선전 · 출전 수당 없음" : $"수당 {MercenaryGenerator.CalculateSortiePay(member.character):N0} G");
                    slot.characterText.color = local ? new Color(0.65f, 0.85f, 1f) : new Color(1f, 0.85f, 0.6f);
                }
                else slot.characterText.color = Color.white;
            }
        }

        int totalSortiePay = CalculateTotalSortiePay();
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (totalSortiePayText != null)
            totalSortiePayText.text = IsFriendly ? $"내 진영 · {session.FriendlyTeam(session.LocalProfileId) + 1}팀" : $"총 출전 수당  {totalSortiePay:N0} G";

        if (partyInfoText != null)
        {
            partyInfoText.text =
                $"현재 파티 인원  {selectedMembers.Count} / {maximum}명\n" +
                $"권장 파티 인원  {selectedQuest?.recommendedPartySize ?? 0}명\n" +
                $"계정 보유 골드  {(playerData != null ? playerData.gold : 0):N0} G\n" +
                $"파티장  {(selectedMembers.Count > 0 ? selectedMembers[0].character.Name : "-")}";
            if (session != null && session.Connected)
            {
                int ownedCount = selectedMembers.FindAll(member => member.ownerId == session.LocalProfileId).Count;
                partyInfoText.text = $"현재 파티 인원  {selectedMembers.Count} / {maximum}명\n" +
                    $"내 편성 인원  {ownedCount} / {selectedMembers.Count}명\n" +
                    (IsFriendly ? "친선전 · 출전 수당 없음\n" : $"내 출전 수당  {totalSortiePay:N0} G\n") +
                    $"계정 보유 골드  {(playerData != null ? playerData.gold : 0):N0} G";
            }
        }

        if (departButton != null)
            departButton.interactable = !awaitingFormation && selectedQuest != null && selectedMembers.Count > 0;

        MultiplayerSession multiplayerRoom = MultiplayerSession.Instance;

        SetButtonText(departButton, IsRemoteClient()
            ? (multiplayerRoom != null && multiplayerRoom.IsReady ? "준비 취소" : "준비")
            : "출발");
        if (IsFriendly) SetButtonText(departButton, session.IsHost ? "친선전 시작" : "내기 / 최종 수락");
        SetButtonText(cancelButton, "나가기");
        SetButtonText(inviteButton, "초대");

        if (inviteButton != null)
            inviteButton.interactable = multiplayerRoom != null && multiplayerRoom.CanInvite;
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
            if (member?.character != null && (session == null || !session.Connected || member.ownerId == session.LocalProfileId))
                total += MercenaryGenerator.CalculateSortiePay(member.character);
        }

        return total;
    }

    private void OpenMemberEquipment(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= selectedMembers.Count)
            return;

        if (session != null && session.Connected && selectedMembers[slotIndex].ownerId != session.LocalProfileId) return;
        CharacterData character = selectedMembers[slotIndex].character;
        if (character != null)
        {
            UIManager.Instance?.OpenCharacterEquipment(character);
            var characters = new List<CharacterData>();
            foreach (var member in selectedMembers)
                if (session == null || !session.Connected || member.ownerId == session.LocalProfileId)
                    characters.Add(member.character);
            InventoryUIController.Instance?.equipmentWindow?.SetNavigationCharacters(characters);
        }
    }

    private void Invite()
    {
        MultiplayerSession multiplayerRoom = MultiplayerSession.Instance;
        if (multiplayerRoom != null)
            multiplayerRoom.InviteFriend();
        else
            SetStatus("멀티플레이 방에 연결된 상태에서 초대할 수 있습니다.");
    }

    private void ClearReadyAfterPartyChange()
    {
        if (session == null || !session.Connected) return;
        var characters = new List<CharacterData>();
        var rows = new List<bool>();
        foreach (var member in selectedMembers)
        {
            if (member.ownerId != session.LocalProfileId) continue;
            characters.Add(member.character);
            rows.Add(member.isFront);
        }
        awaitingFormation = true;
        session.SubmitFormation(characters, rows);
    }

    private void OnFormationChanged()
    {
        if (leavingForSolo) return;
        awaitingFormation = false;
        if (!session.Connected)
        {
            selectedMembers.RemoveAll(member => !string.IsNullOrEmpty(member.ownerId) && member.ownerId != session.LocalProfileId);
            uploadLocalFormation = true;
            SetStatus("연결이 종료되었습니다. 본인 캐릭터로 혼자 출발할 수 있습니다.");
            RefreshParty();
            RefreshRosterSelection();
            return;
        }
        if (session.Formation.quest == null) return;
        if (uploadLocalFormation && session.IsHost)
        {
            uploadLocalFormation = false;
            foreach (var member in selectedMembers) member.ownerId = session.LocalProfileId;
            ClearReadyAfterPartyChange();
            return;
        }
        selectedQuest = session.Formation.quest;
        selectedMembers.Clear();
        foreach (var slot in session.Formation.slots)
        {
            if (IsFriendly && session.FriendlyTeam(slot.ownerId) != session.FriendlyTeam(session.LocalProfileId)) continue;
            bool local = slot.ownerId == session.LocalProfileId;
            CharacterData character = local ? CharacterPoolManager.Instance?.Get(slot.character.id)?.character : null;
            if (character == null) character = SaveMapper.FromDto(slot.character);
            selectedMembers.Add(new PartyMemberSelection { character = character, ownerId = slot.ownerId, isFront = slot.isFront });
        }
        if (multiplayerStatusText != null) multiplayerStatusText.text = session.MembersText;
        if (opposingTeamText != null)
        {
            opposingTeamText.gameObject.SetActive(IsFriendly);
            opposingTeamText.text = "상대 팀: ";
            if (IsFriendly)
                foreach (var slot in session.Formation.slots)
                    if (session.FriendlyTeam(slot.ownerId) != session.FriendlyTeam(session.LocalProfileId))
                        opposingTeamText.text += $"Lv.{slot.character.level} {slot.character.name}({(slot.isFront ? "전열" : "후열")})  ";
        }
        RefreshQuestInfo();
        RefreshParty();
        RefreshRosterSelection();
    }

    private void CopyCode() => session?.CopyInviteCode();

    private void RefreshRoom()
    {
        if (roomCodeText != null)
            roomCodeText.text = !string.IsNullOrEmpty(session?.InviteCode)
                ? $"방 코드  {session.InviteCode}" : "방 코드 없음 · 혼자 출발 가능";
        if (copyCodeButton != null) copyCodeButton.interactable = !string.IsNullOrEmpty(session?.InviteCode);
        if (inviteButton != null) inviteButton.interactable = session != null && session.CanInvite;
        if (multiplayerStatusText != null)
            multiplayerStatusText.text = session != null && session.Connected ? session.MembersText : "싱글플레이";
        if (session != null) SetStatus(session.Status);
    }
    private static void SetButtonText(Button button, string value)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = value;
    }

    private static bool IsRemoteClient()
    {
        NetworkManager networkManager = InstanceFinder.NetworkManager;
        return networkManager != null &&
               networkManager.IsClientStarted &&
               !networkManager.IsServerStarted;
    }

    private static bool IsNetworkConnectionActive()
    {
        NetworkManager networkManager = InstanceFinder.NetworkManager;
        return networkManager != null && networkManager.IsClientStarted;
    }

    private void Depart()
    {
        if (awaitingFormation) return;
        if (IsFriendly)
        {
            if (!session.IsHost)
            {
                UIManager.Instance?.GetComponent<FriendlyMatchLobbyUI>()?.Open();
                return;
            }

            if (!session.CanDepart(out string reason))
            {
                SetStatus(reason);
                if (reason == "모든 참가자가 내기를 잠그고 최종 수락해야 합니다.")
                    UIManager.Instance?.GetComponent<FriendlyMatchLobbyUI>()?.Open();
            }
            else session.DepartExpedition();
            return;
        }
        if (IsRemoteClient())
        {
            if (selectedMembers.Count == 0)
                return;

            MultiplayerSession multiplayerRoom = MultiplayerSession.Instance;
            if (multiplayerRoom == null)
            {
                SetStatus("멀티플레이 원정 세션을 찾을 수 없습니다.");
                return;
            }

            if (!multiplayerRoom.IsReady && !multiplayerRoom.CanReady(out string reason))
            {
                SetStatus(reason);
                return;
            }
            multiplayerRoom.ToggleReady();
            SetStatus(multiplayerRoom.IsReady
                ? "준비를 완료했습니다. 호스트의 출발을 기다립니다."
                : "준비를 취소했습니다.");
            RefreshParty();
            return;
        }

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (selectedQuest == null || playerData == null || selectedMembers.Count == 0)
            return;
        if (session != null && session.Formation.savedExpeditionId != null && !session.HasOtherParticipants)
        {
            SetStatus("보관 원정은 기존 참가자가 모두 접속해야 재개할 수 있습니다.");
            return;
        }

        if (session != null && session.Connected && session.HasOtherParticipants)
        {
            if (!session.CanDepart(out string reason)) SetStatus(reason);
            else { session.DepartExpedition(); SetStatus(session.Status); }
            return;
        }

        if (session != null && selectedMembers.Exists(member => session.IsExpeditionCharacterLocked(member.character.ID)))
        {
            SetStatus("보관 원정에 참가 중인 용병은 재개하거나 포기하기 전 다른 의뢰에 출전할 수 없습니다.");
            return;
        }
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

        if (session != null && !session.Editable)
        {
            if (session.HasOtherParticipants)
            {
                SetStatus("참가자가 접속 중입니다. 참가 완료 후 준비 상태를 확인하세요.");
                return;
            }
            leavingForSolo = true;
            session.LeaveRoom();
            leavingForSolo = false;
        }

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
        MultiplayerSession multiplayerRoom = MultiplayerSession.Instance;
        if (multiplayerRoom != null && !multiplayerRoom.Editable)
            multiplayerRoom.LeaveRoom();

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
