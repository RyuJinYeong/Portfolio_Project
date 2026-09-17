using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;
using Newtonsoft.Json;
using UnityEngine;

public partial class MultiplayerSession
{
    public enum ExpeditionPhase { Preparing, LoadingParty, Map, LoadingNode, Encounter, Result, Battle, Ended }

    [Serializable]
    public class ExpeditionState
    {
        public string id;
        public int revision;
        public ExpeditionPhase phase;
        public int diceSequence;
        public ActiveQuestRuntime runtime;
        public List<FormationSlot> party = new();
        public List<FormationMember> players = new();
        public List<SharedMonster> monsters = new();
        public List<InventorySlotData> inventory = new();
        public List<GeneratedEquipmentData> equipment = new();
        public SharedDecision decision;
        public string text;
        public Dictionary<string, int> battleGold = new();
        public List<InventorySlotData> pendingLoot = new();
        public List<SharedLoot> claimedLoot = new();
        public bool questCompleted;
        public string afterRewards;
        public string rewardSummary;
        public QuestEncounterResolution resolution;
        public bool finished;
        public bool settlementPrepared;
        public bool resuming;
        public Dictionary<string, int> sortiePay = new();
        public FriendlyMatchSettings friendly;
    }

    [Serializable]
    public class SharedLoot
    {
        public string ownerId;
        public InventorySlotData item;
    }

    [Serializable]
    public class SharedMonster
    {
        public CharacterSaveDTO character;
        public int roleId;
        public bool isFront;
        public bool grantsExperience;
        public List<GeneratedEquipmentData> equipment = new();
    }

    public struct ExpeditionCommand : IBroadcast
    {
        public string expeditionId;
        public int revision;
        public string action;
        public int nodeId;
        public string value;
        public string characterId;
    }

    public struct ExpeditionMessage : IBroadcast { public string json; }

    public ExpeditionState Expedition { get; private set; }
    public bool InExpedition => beforeExpedition != null;
    public event Action ExpeditionChanged;
    private ExpeditionState serverExpedition;
    private PlayerSaveDTO beforeExpedition;
    private readonly Dictionary<string, CharacterSaveDTO> beforeCharacters = new();
    private readonly HashSet<string> expeditionAcks = new();
    private readonly List<string> remotePoolIds = new();
    private bool previousSaveSuppression;
    private float expeditionDeadline;
    private float decisionDeadline;
    private string handledExpeditionId;
    private int handledPhase = -1;
    private int handledNode = -1;
    private QuestEncounterResolution sharedResolution;
    private int appliedBattleGold;

    private List<GeneratedEquipmentData> GetFormationEquipment(CharacterData character)
    {
        var slots = character.EquipmentSlots;
        var ids = new[] { slots.helmetInstanceId, slots.armorInstanceId, slots.glovesInstanceId,
            slots.shoesInstanceId, slots.ring1InstanceId, slots.ring2InstanceId, slots.necklaceInstanceId,
            slots.weaponInstanceId, slots.subWeaponInstanceId };
        return ids.Where(id => !string.IsNullOrEmpty(id)).Distinct().Select(EquipmentInstanceRepository.Get)
            .Where(item => item != null).ToList();
    }

    public void DepartExpedition()
    {
        if (serverExpedition != null || !CanDepart(out string reason)) return;
        if (members.Count < 2 || networkManager.ServerManager.Clients.Count != members.Count ||
            (steamInitialized && lobby.IsValid() && Steamworks.SteamMatchmaking.GetNumLobbyMembers(lobby) != members.Count))
        {
            Status = "접속 중인 참가자의 입장이 완료되면 출발할 수 있습니다.";
            RefreshControls();
            return;
        }
        if (serverFormation.slots.Select(slot => slot.character.id).Distinct().Count() != serverFormation.slots.Count)
            return;
        serverExpedition = resumeCheckpoint != null ? JsonConvert.DeserializeObject<ExpeditionState>(JsonConvert.SerializeObject(resumeCheckpoint)) : new ExpeditionState
        {
            id = Guid.NewGuid().ToString("N"), phase = ExpeditionPhase.Preparing,
            party = JsonConvert.DeserializeObject<List<FormationSlot>>(JsonConvert.SerializeObject(serverFormation.slots)),
            players = JsonConvert.DeserializeObject<List<FormationMember>>(JsonConvert.SerializeObject(serverFormation.members)),
            friendly = serverFormation.friendly == null ? null : JsonConvert.DeserializeObject<FriendlyMatchSettings>(JsonConvert.SerializeObject(serverFormation.friendly)),
            text = "참가자의 편성과 출전 수당을 확인하고 있습니다."
        };
        if (resumeCheckpoint != null)
        {
            serverExpedition.resuming = true;
            serverExpedition.phase = ExpeditionPhase.Preparing;
            serverExpedition.decision = null;
            serverExpedition.revision++;
        }
        if (steamInitialized && lobby.IsValid()) Steamworks.SteamMatchmaking.SetLobbyJoinable(lobby, serverExpedition.friendly != null);
        expeditionDeadline = Time.realtimeSinceStartup + 90f;
        expeditionAcks.Clear();
        PublishExpedition();
    }

    private void PublishExpedition()
    {
        serverExpedition.revision++;
        Status = serverExpedition.text;
        networkManager.ServerManager.Broadcast(new ExpeditionMessage { json = JsonConvert.SerializeObject(serverExpedition) });
    }

    private void OnExpeditionMessage(ExpeditionMessage message, Channel channel)
    {
        if (!Connected) return;
        var state = JsonConvert.DeserializeObject<ExpeditionState>(message.json);
        if (state == null || (Expedition?.id == state.id && state.revision <= Expedition.revision)) return;
        Expedition = state;
        if (state.friendly != null)
        {
            friendlyReconnectDeadline = 0;
            SetFriendlyPause(state.friendly.winningTeam < 0 && state.friendly.disconnected.Count > 0);
        }
        Status = state.text;
        if (state.phase == ExpeditionPhase.Ended)
        {
            if (state.friendly?.rematch == true && PlayerManager.Instance.HasSharedReceipt(state.id))
            {
                bool host = IsHost;
                var settings = new FriendlyMatchSettings
                {
                    teamSize = state.friendly.teamSize,
                    multiplePlayersPerTeam = state.friendly.multiplePlayersPerTeam,
                    participants = state.friendly.participants.Select(p => new FriendlyParticipant
                    { ownerId = p.ownerId, team = p.team }).ToList()
                };
                int revision = Formation.revision + 1;
                RestoreBeforeExpedition();
                openedQuestId = null;
                ready = false;
                Formation = new FormationState
                {
                    revision = revision, friendly = settings,
                    quest = new QuestDef { id = "friendly", title = "친선전", stageKey = "Town" }
                };
                if (host)
                {
                    serverFormation = Formation;
                    InvalidateReady();
                    PublishMembers();
                }
                FormationChanged?.Invoke();
                RefreshControls();
                return;
            }
            if (state.friendly != null && state.runtime == null)
                PlayerManager.Instance.SaveSharedCheckpoint(null);

            if (state.friendly == null &&
                state.text.StartsWith("원정대가 전멸했습니다.") &&
                UIManager.Instance != null &&
                UIManager.Instance.OpenBattleDefeat(() =>
                {
                    LeaveRoom();
                    Status = state.text;
                    RefreshControls();
                }))
            {
                RefreshControls();
                return;
            }

            LeaveRoom();
            Status = state.text;
            RefreshControls();
            return;
        }

        bool friendlyRejoin = state.friendly != null && beforeExpedition == null &&
            PlayerManager.Instance.SharedCheckpoint?.id == state.id && state.phase != ExpeditionPhase.Preparing;
        if ((state.phase == ExpeditionPhase.Preparing || friendlyRejoin) && beforeExpedition == null)
        {
            bool currentParty = state.party.Where(slot => slot.ownerId == LocalProfileId).All(slot =>
            {
                var character = CharacterPoolManager.Instance.Get(slot.character.id)?.character;
                return character != null && JsonConvert.SerializeObject(slot.character) ==
                    JsonConvert.SerializeObject(SaveMapper.ToDto(character));
            });
            if (!friendlyRejoin && ((!state.resuming && (!CanReady(out _) || !currentParty)) || QuestManager.Instance.active != null ||
                (state.resuming && !CanReady(out _))))
            {
                SendExpeditionCommand("reject", "편성 또는 보관 원정이 일치하지 않습니다.");
                return;
            }
            var player = PlayerManager.Instance.GetCurrentPlayerData();
            beforeExpedition = JsonConvert.DeserializeObject<PlayerSaveDTO>(JsonConvert.SerializeObject(SaveMapper.ToDto(player)));
            beforeCharacters.Clear();
            appliedBattleGold = 0;
            foreach (var manager in CharacterPoolManager.Instance.All())
                if (player.characterIds.Contains(manager.character.ID))
                    beforeCharacters[manager.character.ID] = JsonConvert.DeserializeObject<CharacterSaveDTO>(
                        JsonConvert.SerializeObject(SaveMapper.ToDto(manager.character)));
            previousSaveSuppression = PlayerManager.Instance.suppressRemotePersistence;
            PlayerManager.Instance.suppressRemotePersistence = true;
            UIManager.Instance.CloseManagedWindows();
            if (!friendlyRejoin) SendExpeditionCommand("ready");
        }

        if (state.runtime != null && InExpedition)
        {
            QuestManager.Instance.active = state.questCompleted && !state.resuming ? null : IsHost ? serverExpedition.runtime : state.runtime;
            var player = PlayerManager.Instance.GetCurrentPlayerData();
            player.activeCharacterIds = state.party.Select(slot => slot.character.id).ToList();
            state.battleGold.TryGetValue(LocalProfileId, out int earnedGold);
            player.gold += earnedGold - appliedBattleGold;
            appliedBattleGold = earnedGold;
            player.currentStage = state.runtime.stageKey;
            foreach (var slot in state.party)
            {
                player.SetPosition(slot.character.id, slot.isFront);
                foreach (var item in slot.equipment) EquipmentInstanceRepository.AddRuntime(item);
            }
            foreach (var item in state.equipment) EquipmentInstanceRepository.AddRuntime(item);
            player.expeditionStorage = state.inventory;
        }

        int node = state.runtime?.currentRouteNodeId ?? -1;
        bool newPhase = handledExpeditionId != state.id || handledPhase != (int)state.phase || handledNode != node;
        handledExpeditionId = state.id;
        handledPhase = (int)state.phase;
        handledNode = node;
        if (newPhase && InExpedition)
        {
            switch (state.phase)
            {
                case ExpeditionPhase.LoadingParty:
                    var player = PlayerManager.Instance.GetCurrentPlayerData();
                    state.sortiePay.TryGetValue(LocalProfileId, out int sortiePay);
                    player.gold -= sortiePay;
                    StartCoroutine(LoadExpeditionPortraits(state.id));
                    break;
                case ExpeditionPhase.Map:
                    ApplyExpeditionCharacters();
                    UIManager.Instance.OpenQuestNodeMap();
                    break;
                case ExpeditionPhase.LoadingNode:
                    ResetSharedBattle();
                    UIManager.Instance.questNodeMapPanel.Close();
                    GameManager.Instance.EnterQuestRouteNode(QuestManager.Instance.GetCurrentRouteNode());
                    break;
                case ExpeditionPhase.Encounter:
                    ApplyExpeditionCharacters();
                    var encounter = QuestManager.Instance.GetOrAssignCurrentEncounter();
                    if (encounter != null) StageManager.Instance.ActivateEncounterPresentation(encounter);
                    break;
                case ExpeditionPhase.Result:
                    if (Battle != null) TurnManager.Instance.StopSharedBattle();
                    if (state.friendly == null) ApplyExpeditionCharacters();
                    break;
                case ExpeditionPhase.Battle:
                    if (state.friendly != null) GameManager.Instance.SpawnFriendlyBattle(state.party);
                    else if (!IsHost) GameManager.Instance.SpawnSharedBattle(state.monsters);
                    NotifySharedBattleLoaded();
                    if (friendlyRejoin && state.friendly.battle != null) ApplyBattleState(state.friendly.battle);
                    break;
            }
        }
        if (InExpedition && !newPhase && state.phase == ExpeditionPhase.Result && state.friendly == null) ApplyExpeditionCharacters();
        if (InExpedition) SaveExpeditionProgress(state);
        if (InExpedition && state.settlementPrepared && !state.finished &&
            PlayerManager.Instance.SharedCheckpoint?.revision == state.revision)
            SendExpeditionCommand("settlementReady");
        if (InExpedition && state.finished && (PlayerManager.Instance.HasSharedReceipt(state.id) ||
            (PlayerManager.Instance.SharedCheckpoint?.id == state.id && PlayerManager.Instance.SharedCheckpoint.revision == state.revision)))
        {
            if (CommitSharedRewards(state))
            {
                if (state.friendly != null)
                    UIManager.Instance.GetComponent<FriendlyMatchLobbyUI>().ShowResult(state);
                else SendExpeditionCommand("settled");
            }
            else Status = "보상 저장이 보류되었습니다. 창고와 디스크의 여유 공간을 확보한 뒤 보관 원정에서 다시 수령하세요.";
        }
        ExpeditionChanged?.Invoke();
        RefreshControls();
    }

    private IEnumerator LoadExpeditionPortraits(string expeditionId)
    {
        foreach (var slot in Expedition.party)
        {
            if (!InExpedition || Expedition.id != expeditionId) yield break;
            if (CharacterPoolManager.Instance.Get(slot.character.id) != null) continue;
            remotePoolIds.Add(slot.character.id);
            bool loaded = false;
            CharacterPoolManager.Instance.AddCharacterToPool(SaveMapper.FromDto(slot.character), () =>
            {
                loaded = true;
                if (!InExpedition && CharacterPoolManager.Instance._pool.Remove(slot.character.id, out var abandoned))
                    Destroy(abandoned.gameObject);
            });
            while (!loaded && InExpedition) yield return null;
            if (!InExpedition)
            {
                if (CharacterPoolManager.Instance._pool.Remove(slot.character.id, out var abandoned)) Destroy(abandoned.gameObject);
                yield break;
            }
        }
        SendExpeditionCommand("ready");
    }

    public CharacterData LoadExpeditionCharacter(string id)
    {
        var slot = Expedition?.party.Find(item => item.character.id == id);
        return slot != null ? SaveMapper.FromDto(slot.character) : null;
    }

    private void ApplyExpeditionCharacters()
    {
        foreach (var slot in Expedition.party)
        {
            var pool = CharacterPoolManager.Instance.Get(slot.character.id);
            var managers = GameManager.Instance.GetAllCharacters().Where(cm => cm != null && cm.character.ID == slot.character.id).ToList();
            if (pool != null) managers.Add(pool);
            foreach (var manager in managers.Distinct())
            {
                Texture2D portrait = manager.character.Portrait;
                manager.character = SaveMapper.FromDto(slot.character);
                manager.character.Portrait = portrait;
                manager.UpdateCharacterUI();
            }
        }
    }

    private void CaptureExpeditionCharacters()
    {
        foreach (var slot in serverExpedition.party)
        {
            var manager = GameManager.Instance.GetAllCharacters().Find(cm => cm != null && cm.character.ID == slot.character.id);
            if (manager != null) slot.character = SaveMapper.ToDto(manager.character);
        }
        var player = PlayerManager.Instance.GetCurrentPlayerData();
        serverExpedition.inventory = player.expeditionStorage;
        foreach (var item in player.generatedEquipments.Where(item => !beforeExpedition.generatedEquipments
            .Exists(old => old.instanceId == item.instanceId)))
        {
            serverExpedition.equipment.RemoveAll(old => old.instanceId == item.instanceId);
            serverExpedition.equipment.Add(item);
        }
    }

    public void NotifyExpeditionNodeReady() => SendExpeditionCommand("ready");

    private void SendExpeditionCommand(string action, string value = null, string characterId = null)
    {
        if (!Connected || Expedition == null) return;
        networkManager.ClientManager.Broadcast(new ExpeditionCommand
        {
            expeditionId = Expedition.id, revision = Expedition.decision?.id ?? (int)Expedition.phase,
            nodeId = Expedition.runtime?.currentRouteNodeId ?? -1,
            action = action, value = value, characterId = characterId
        });
    }

    private void OnExpeditionCommand(NetworkConnection connection, ExpeditionCommand command, Channel channel)
    {
        if (serverExpedition == null || serverExpedition.id != command.expeditionId ||
            serverExpedition.phase == ExpeditionPhase.Ended ||
            !members.TryGetValue(connection.ClientId, out var member)) return;
        if (command.action == "settlementReady" && serverExpedition.settlementPrepared && !serverExpedition.finished)
        {
            expeditionAcks.Add(member.profileId);
            if (serverExpedition.players.All(player => expeditionAcks.Contains(player.ownerId) ||
                (serverExpedition.friendly != null && !members.Values.Any(m => m.profileId == player.ownerId))))
            {
                expeditionAcks.Clear();
                serverExpedition.finished = true;
                if (serverExpedition.friendly != null) expeditionDeadline = 0;
                PublishExpedition();
            }
            return;
        }
        if (command.action == "settled" && serverExpedition.finished)
        {
            expeditionAcks.Add(member.profileId);
            if (serverExpedition.friendly != null)
            {
                ResolveFriendlyResult();
                return;
            }
            if (serverExpedition.players.All(player => expeditionAcks.Contains(player.ownerId) ||
                (serverExpedition.friendly != null && !members.Values.Any(m => m.profileId == player.ownerId))))
            {
                expeditionDeadline = 0;
                serverExpedition.phase = ExpeditionPhase.Ended;
                serverExpedition.text = serverExpedition.friendly == null ? "원정을 완료하고 각자의 보상을 저장했습니다." :
                    serverExpedition.rewardSummary + " 내기 정산을 완료했습니다.";
                PublishExpedition();
            }
            return;
        }
        if ((command.action == "friendlyRematch" || command.action == "friendlyTown") &&
            serverExpedition.friendly != null && serverExpedition.finished && expeditionAcks.Contains(member.profileId))
        {
            var settings = serverExpedition.friendly;
            if (command.action == "friendlyTown") settings.returnToTown = true;
            else if (!settings.rematchVotes.Contains(member.profileId)) settings.rematchVotes.Add(member.profileId);
            ResolveFriendlyResult();
            return;
        }
        if (command.action == "leave" || command.action == "reject")
        {
            if (serverExpedition.friendly != null && serverExpedition.phase != ExpeditionPhase.Preparing)
            {
                FinishFriendlyBattle(1 - FriendlyTeam(member.profileId));
                return;
            }
            AbortExpedition(command.action == "leave" ? "참가자가 공동 원정을 종료했습니다." : "출발 조건이 변경되어 원정을 취소했습니다.");
            return;
        }
        if (command.action == "ready")
        {
            if (command.revision != (int)serverExpedition.phase ||
                command.nodeId != (serverExpedition.runtime?.currentRouteNodeId ?? -1) ||
                (serverExpedition.phase != ExpeditionPhase.Preparing && serverExpedition.phase != ExpeditionPhase.LoadingParty &&
                 serverExpedition.phase != ExpeditionPhase.LoadingNode)) return;
            expeditionAcks.Add(member.profileId);
            if (!serverExpedition.players.All(player => expeditionAcks.Contains(player.ownerId))) return;
            expeditionAcks.Clear();
            expeditionDeadline = 0;
            if (serverExpedition.phase == ExpeditionPhase.Preparing)
            {
                if (serverExpedition.friendly != null) { BeginFriendlyBattle(); return; }
                if (!serverExpedition.resuming && !QuestManager.Instance.Accept(serverFormation.quest.id, serverExpedition.party[0].character.id,
                    serverExpedition.party.Select(slot => slot.character.id).ToList()))
                {
                    AbortExpedition("의뢰를 수락할 수 없어 출발을 취소했습니다.");
                    return;
                }
                if (!serverExpedition.resuming)
                {
                    serverExpedition.runtime = QuestManager.Instance.active;
                    foreach (var slot in serverExpedition.party) slot.character.lastStandUsed = false;
                }
                foreach (var slot in serverExpedition.resuming ? new List<FormationSlot>() : serverExpedition.party)
                {
                    int pay = MercenaryGenerator.CalculateSortiePay(SaveMapper.FromDto(slot.character));
                    serverExpedition.sortiePay.TryGetValue(slot.ownerId, out int total);
                    serverExpedition.sortiePay[slot.ownerId] = total + pay;
                }
                serverExpedition.phase = ExpeditionPhase.LoadingParty;
                serverExpedition.text = "원정대 캐릭터와 초상화를 불러오고 있습니다.";
                expeditionDeadline = Time.realtimeSinceStartup + 90f;
                PublishExpedition();
            }
            else if (serverExpedition.phase == ExpeditionPhase.LoadingParty)
            {
                if (serverExpedition.friendly != null)
                {
                    ResetSharedBattle();
                    serverExpedition.phase = ExpeditionPhase.Battle;
                    battleWaitDeadline = Time.realtimeSinceStartup + 90f;
                    PublishExpedition();
                    return;
                }
                if (!serverExpedition.resuming) BeginRouteDecision();
                else
                {
                    serverExpedition.phase = ExpeditionPhase.LoadingNode;
                    expeditionDeadline = Time.realtimeSinceStartup + 90f;
                    PublishExpedition();
                }
            }
            else
            {
                if (serverExpedition.resuming) { ResumeLoadedNode(); return; }
                var node = QuestManager.Instance.GetCurrentRouteNode();
                if (node.type == QuestRouteNodeType.Rest || node.type == QuestRouteNodeType.RandomEncounter) BeginEncounterDecision();
                else
                {
                    GameManager.Instance.PrepareSharedQuestBattle();
                    if (serverExpedition.phase == ExpeditionPhase.LoadingNode)
                        AbortExpedition("현재 노드의 전투 데이터를 불러올 수 없습니다.");
                }
            }
            return;
        }
        HandleDecisionCommand(member.profileId, command);
    }

    public void CaptureExpeditionBattle()
    {
        if (!IsHost || serverExpedition == null) return;
        ResetSharedBattle();
        battleWaitDeadline = Time.realtimeSinceStartup + 90f;
        CaptureExpeditionCharacters();
        serverExpedition.monsters = GameManager.Instance.GetAllCharacters()
            .Where(cm => cm != null && !cm.character.IsMine)
            .Select(cm => new SharedMonster
            {
                character = SaveMapper.ToDto(cm.character), roleId = cm.character.monsterRoleId, isFront = cm.isFront,
                grantsExperience = cm.character.GrantsExperience, equipment = GetFormationEquipment(cm.character)
            }).ToList();
        serverExpedition.phase = ExpeditionPhase.Battle;
        serverExpedition.decision = null;
        serverExpedition.text = "공동 전투 준비 중 — 참가자들의 로딩을 기다립니다.";
        PublishExpedition();
    }

    public void LeaveExpedition()
    {
        if (!InExpedition) return;

        if (IsHost && serverExpedition != null)
        {
            if (serverExpedition.friendly != null && serverExpedition.phase != ExpeditionPhase.Preparing)
                FinishFriendlyBattle(1 - FriendlyTeam(LocalProfileId));
            else
                AbortExpedition("참가자가 공동 원정을 종료했습니다.");
            return;
        }

        if (Connected && Expedition != null)
            SendExpeditionCommand("leave");
        else
            LeaveRoom();
    }

    private void AbortExpedition(string reason)
    {
        if (serverExpedition == null) return;
        serverExpedition.phase = ExpeditionPhase.Ended;
        serverExpedition.text = reason + " 마지막 저장 지점에서 원정을 재개할 수 있습니다.";
        PublishExpedition();
    }

    private void UpdateExpedition()
    {
        if (!IsHost || serverExpedition == null || serverExpedition.phase == ExpeditionPhase.Ended) return;
        if (battleWaitDeadline > 0 && Time.realtimeSinceStartup >= battleWaitDeadline)
        {
            battleWaitDeadline = 0;
            AbortExpedition("전투 준비 응답 시간이 초과되었습니다.");
            return;
        }
        if (expeditionDeadline > 0 && Time.realtimeSinceStartup >= expeditionDeadline)
        {
            expeditionDeadline = 0;
            AbortExpedition("참가자의 로딩 응답을 기다리는 시간이 초과되었습니다.");
        }
        if (decisionDeadline > 0 && Time.realtimeSinceStartup >= decisionDeadline)
        {
            decisionDeadline = 0;
            FinishSharedDecision();
        }
    }

    private void RestoreBeforeExpedition()
    {
        if (IsFriendlyMatch) QuestManager.Instance.active = null;
        bool committed = Expedition != null && PlayerManager.Instance.HasSharedReceipt(Expedition.id);
        if (InExpedition) TurnManager.Instance?.StopSharedBattle();
        ResetSharedBattle();
        StopAllCoroutines();
        if (beforeExpedition != null)
        {
            PlayerManager.Instance.SetCurrentPlayerData(SaveMapper.FromDto(beforeExpedition));
            foreach (var saved in beforeCharacters)
            {
                var manager = CharacterPoolManager.Instance.Get(saved.Key);
                if (manager == null) continue;
                var portrait = manager.character.Portrait;
                manager.character = SaveMapper.FromDto(saved.Value);
                manager.character.Portrait = portrait;
            }
            foreach (string id in remotePoolIds)
                if (CharacterPoolManager.Instance._pool.Remove(id, out var manager)) Destroy(manager.gameObject);
            foreach (var slot in Expedition.party)
                foreach (var item in slot.equipment) EquipmentInstanceRepository.RemoveRuntime(item.instanceId);
            foreach (var item in Expedition.equipment) EquipmentInstanceRepository.RemoveRuntime(item.instanceId);
            foreach (var monster in Expedition.monsters)
                foreach (var item in monster.equipment) EquipmentInstanceRepository.RemoveRuntime(item.instanceId);
            PlayerManager.Instance.suppressRemotePersistence = previousSaveSuppression;
            beforeExpedition = null;
            if (committed)
            {
                Expedition = null;
                PlayerManager.Instance.suppressRemotePersistence = true;
                PlayerManager.Instance.LoadLocalPlayerData(null);
                foreach (var saved in beforeCharacters)
                {
                    var manager = CharacterPoolManager.Instance.Get(saved.Key);
                    if (manager == null) continue;
                    string id = saved.Key;
                    PlayerManager.Instance.LoadCharacter(id, character =>
                    {
                        if (character == null || manager == null) return;
                        var portrait = manager.character.Portrait;
                        manager.character = character;
                        manager.character.Portrait = portrait;
                    });
                }
            }
            UIManager.Instance.CloseManagedWindows();
            GameManager.Instance.RestoreTownAfterSharedPreview();
        }
        beforeCharacters.Clear();
        remotePoolIds.Clear();
        Expedition = null;
        serverExpedition = null;
        expeditionAcks.Clear();
        expeditionDeadline = decisionDeadline = 0;
        handledExpeditionId = null;
        sharedResolution = null;
        ExpeditionChanged?.Invoke();
    }
}
