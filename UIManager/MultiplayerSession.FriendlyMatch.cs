using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;
using Newtonsoft.Json;
using UnityEngine;

public partial class MultiplayerSession
{
    [Serializable]
    public class FriendlyParticipant
    {
        public string ownerId;
        public int team;
        public bool locked;
        public bool accepted;
        public List<InventorySlotData> stake = new();
        public List<GeneratedEquipmentData> equipment = new();
    }

    [Serializable]
    public class FriendlyMatchSettings
    {
        public int teamSize = 3;
        public bool multiplePlayersPerTeam;
        public List<FriendlyParticipant> participants = new();
        public int winningTeam = -1;
        public Dictionary<string, long> disconnected = new();
        public SharedBattleState battle;
        public bool resultReady;
        public bool returnToTown;
        public bool rematch;
        public List<string> rematchVotes = new();
    }

    public struct FriendlyMatchCommand : IBroadcast
    {
        public int revision;
        public string action;
        public int value;
        public string json;
    }

    public bool IsFriendlyRoom => Formation.friendly != null;
    public bool IsFriendlyMatch => InExpedition && Expedition?.friendly != null;
    public bool FriendlyReconnectPending => IsFriendlyMatch &&
        (friendlyReconnectDeadline > 0 || Expedition.friendly.disconnected.Count > 0);
    private float friendlyReconnectDeadline;
    private float friendlyRetryAt;
    private float friendlyPreviousTimeScale = 1f;
    private bool friendlyPaused;
    private string friendlyHostAddress;
    private int friendlyMigrationWinner = -1;
    private float friendlyMigrationDeadline;
    private const float FriendlyReconnectSeconds = 120f;
    public int FriendlyTeam(string ownerId) =>
        (Expedition?.friendly ?? Formation.friendly)?.participants.Find(p => p.ownerId == ownerId)?.team ?? -1;

    public void OpenFriendlyRoom()
    {
        if (InExpedition || HasSavedExpedition || (!Editable && !IsHost)) return;
        if (Connected)
        {
            if (serverFormation.friendly == null)
            {
                serverFormation.friendly = new FriendlyMatchSettings();
                serverFormation.slots.Clear();
                serverFormation.quest = new QuestDef { id = "friendly", title = "친선전", stageKey = "Town" };
                InvalidateReady();
                capacity = 10;
                transport.SetMaximumClients(capacity, transportIndex);
                if (steamInitialized && lobby.IsValid()) Steamworks.SteamMatchmaking.SetLobbyMemberLimit(lobby, capacity);
                PublishMembers();
            }
        }
        else
        {
            serverFormation.friendly = new FriendlyMatchSettings();
            pendingQuest = new QuestDef { id = "friendly", title = "친선전", stageKey = "Town" };
            transportIndex = 1;
            capacity = 10;
            HostRoom();
        }
    }

    public void SendFriendlyCommand(string action, int value = 0, List<InventorySlotData> stake = null)
    {
        if (!Connected || !IsFriendlyRoom || InExpedition) return;
        if ((action == "lock" || action == "accept") && !ValidateFriendlyStake(out string reason))
        {
            Status = reason;
            RefreshControls();
            return;
        }
        networkManager.ClientManager.Broadcast(new FriendlyMatchCommand
        {
            revision = Formation.revision, action = action, value = value,
            json = stake == null ? null : JsonConvert.SerializeObject(new FriendlyParticipant
            {
                stake = stake,
                equipment = stake.Where(item => item.IsGeneratedEquipment())
                    .Select(item => EquipmentInstanceRepository.Get(item.equipmentInstanceId)).ToList()
            })
        });
    }

    private void OnFriendlyMatchCommand(NetworkConnection connection, FriendlyMatchCommand command, Channel channel)
    {
        var settings = serverFormation.friendly;
        if (settings == null || serverExpedition != null ||
            !members.TryGetValue(connection.ClientId, out var member)) return;
        if (command.revision != serverFormation.revision && command.action != "lock" && command.action != "accept")
        {
            PublishMembers();
            return;
        }
        var participant = settings.participants.Find(p => p.ownerId == member.profileId);
        if (participant == null) return;
        if (command.action == "size" || command.action == "teamMode")
        {
            if (member.profileId != LocalProfileId) return;
            if (command.action == "size") settings.teamSize = Mathf.Clamp(command.value, 1, 5);
            else settings.multiplePlayersPerTeam = command.value != 0;
            serverFormation.slots.Clear();
            InvalidateReady();
        }
        else if (command.action == "team")
        {
            if (command.value < 0 || command.value > 1 || participant.locked) return;
            if (!settings.multiplePlayersPerTeam && settings.participants.Any(p =>
                p.ownerId != participant.ownerId && p.team == command.value)) return;
            participant.team = command.value;
            serverFormation.slots.RemoveAll(slot => slot.ownerId == participant.ownerId);
            InvalidateReady();
        }
        else if (command.action == "stake")
        {
            if (participant.locked || string.IsNullOrEmpty(command.json) || command.json.Length > 131072) return;
            FriendlyParticipant offer;
            try { offer = JsonConvert.DeserializeObject<FriendlyParticipant>(command.json); }
            catch (JsonException) { return; }
            if (offer?.stake == null || offer.equipment == null || offer.stake.Count > 100 ||
                offer.stake.Any(item => item == null || item.count <= 0 || GameDataRegistry.Instance.GetItem(item.itemUid) == null) ||
                offer.equipment.Any(item => item == null || string.IsNullOrEmpty(item.instanceId))) return;
            participant.stake = offer.stake;
            participant.equipment = offer.equipment;
            InvalidateReady();
        }
        else if (command.action == "lock")
        {
            if (!participant.locked && !serverFormation.slots.Any(slot => slot.ownerId == participant.ownerId)) return;
            bool locked = !participant.locked;
            foreach (var p in settings.participants) p.accepted = false;
            participant.locked = locked;
            serverFormation.revision++;
        }
        else if (command.action == "accept")
        {
            if (!settings.participants.All(p => p.locked) ||
                !FriendlyFormationComplete(serverFormation, out _)) return;
            participant.accepted = !participant.accepted;
            serverFormation.revision++;
        }
        else return;
        PublishMembers();
    }

    private bool FriendlyFormationComplete(FormationState formation, out string reason)
    {
        reason = "";
        var settings = formation.friendly;
        if (settings == null) { reason = "친선전 방이 아닙니다."; return false; }
        for (int team = 0; team < 2; team++)
        {
            var owners = settings.participants.Where(p => p.team == team).Select(p => p.ownerId).ToList();
            if (owners.Count == 0 || (!settings.multiplePlayersPerTeam && owners.Count != 1))
                reason = $"{team + 1}팀의 참가자 구성을 확인하세요.";
            else if (formation.slots.Count(slot => owners.Contains(slot.ownerId)) != settings.teamSize)
                reason = $"각 팀을 정확히 {settings.teamSize}명으로 편성하세요.";
        }
        if (settings.participants.Any(p => !formation.slots.Any(slot => slot.ownerId == p.ownerId)))
            reason = "모든 참가자가 본인의 용병을 한 명 이상 편성해야 합니다.";
        return reason.Length == 0;
    }

    private bool ValidateFriendlyStake(out string reason)
    {
        reason = "";
        var offer = Formation.friendly?.participants.Find(p => p.ownerId == LocalProfileId);
        if (offer == null) { reason = "친선전 참가 정보를 기다리고 있습니다."; return false; }
        var storage = JsonConvert.DeserializeObject<List<InventorySlotData>>(JsonConvert.SerializeObject(
            PlayerManager.Instance.GetCurrentPlayerData().accountStorage));
        foreach (var item in offer.stake)
        {
            var found = storage.Find(slot => slot.count >= item.count && FriendlyItemsMatch(slot, item));
            if (found == null) { reason = "내기 아이템이 창고의 현재 상태와 다릅니다. 다시 등록하세요."; return false; }
            found.count -= item.count;
        }
        return true;
    }

    private void BeginFriendlyBattle()
    {
        serverExpedition.runtime = new ActiveQuestRuntime
        {
            def = serverFormation.quest, status = QuestStatus.Active, stageKey = "Town",
            currentRouteNodeId = 0,
            routeNodes = new List<QuestRouteNode> { new() { id = 0, type = QuestRouteNodeType.Battle } }
        };
        foreach (var slot in serverExpedition.party)
        {
            var character = SaveMapper.FromDto(slot.character);
            character.IsMine = serverExpedition.friendly.participants.Find(p => p.ownerId == slot.ownerId).team == 0;
            character.IsAlive = true;
            character.CurrentHp = character.FinalStats.MaxHp;
            character.CurrentStamina = character.FinalStats.MaxStamina;
            character.CurrentMentality = character.FinalStats.MaxMentality;
            slot.character = SaveMapper.ToDto(character);
        }
        serverExpedition.phase = ExpeditionPhase.LoadingParty;
        serverExpedition.text = "친선전 캐릭터를 준비하고 있습니다.";
        expeditionDeadline = Time.realtimeSinceStartup + 90f;
        PublishExpedition();
    }

    private void FinishFriendlyBattle(int winner)
    {
        if (serverExpedition.friendly.winningTeam >= 0) return;
        SetFriendlyPause(false);
        TurnManager.Instance.StopSharedBattle();
        battleWaitDeadline = expeditionDeadline = 0;
        serverExpedition.friendly.winningTeam = winner;
        serverExpedition.phase = ExpeditionPhase.Result;
        serverExpedition.afterRewards = "friendlyComplete";
        serverExpedition.rewardSummary = $"{winner + 1}팀 승리 · 참가 용병은 회복됩니다.";
        var owners = serverExpedition.friendly.participants.Where(p => p.team == winner).ToList();
        foreach (var p in serverExpedition.friendly.participants)
        {
            if (p.team == winner)
                foreach (var item in p.stake)
                    serverExpedition.claimedLoot.Add(new SharedLoot { ownerId = p.ownerId, item = item });
            else if (owners.Count == 1)
                foreach (var item in p.stake)
                    serverExpedition.claimedLoot.Add(new SharedLoot { ownerId = owners[0].ownerId, item = item });
            else serverExpedition.pendingLoot.AddRange(p.stake);
            serverExpedition.equipment.AddRange(p.equipment);
        }
        StartCoroutine(ShowFriendlyRewardsAfterDeath(serverExpedition.id));
    }

    private System.Collections.IEnumerator ShowFriendlyRewardsAfterDeath(string expeditionId)
    {
        yield return new WaitForSecondsRealtime(1.5f);
        if (IsHost && serverExpedition?.id == expeditionId)
            BeginSharedNodeRewards();
    }

    private bool CommitFriendlyRewards(ExpeditionState state)
    {
        var player = SaveMapper.FromDto(JsonConvert.DeserializeObject<PlayerSaveDTO>(JsonConvert.SerializeObject(beforeExpedition)));
        var offer = state.friendly.participants.Find(p => p.ownerId == LocalProfileId);
        foreach (var item in offer.stake)
        {
            var found = player.accountStorage.Find(slot => slot.count >= item.count && FriendlyItemsMatch(slot, item));
            if (found == null) return false;
            found.count -= item.count;
            if (found.count == 0) player.accountStorage.Remove(found);
        }
        foreach (var reward in state.claimedLoot.Where(item => item.ownerId == LocalProfileId))
        {
            var copy = JsonConvert.DeserializeObject<InventorySlotData>(JsonConvert.SerializeObject(reward.item));
            if (!SharedInventoryUtility.AddInventorySlot(player.accountStorage, copy)) return false;
            var equipment = state.equipment.Find(item => item.instanceId == copy.equipmentInstanceId);
            if (equipment != null && !player.generatedEquipments.Exists(item => item.instanceId == equipment.instanceId))
                player.generatedEquipments.Add(equipment);
        }
        var characters = new List<CharacterSaveDTO>();
        foreach (var slot in state.party.Where(slot => slot.ownerId == LocalProfileId))
        {
            var character = SaveMapper.FromDto(beforeCharacters[slot.character.id]);
            character.IsAlive = true;
            character.CurrentHp = character.FinalStats.MaxHp;
            character.CurrentStamina = character.FinalStats.MaxStamina;
            character.CurrentMentality = character.FinalStats.MaxMentality;
            characters.Add(SaveMapper.ToDto(character));
        }
        return PlayerManager.Instance.CommitSharedResult(state.id, SaveMapper.ToDto(player), characters);
    }

    public void ChooseFriendlyResult(bool rematch)
    {
        if (!IsFriendlyMatch || !Expedition.finished || !PlayerManager.Instance.HasSharedReceipt(Expedition.id)) return;
        SendExpeditionCommand(rematch ? "friendlyRematch" : "friendlyTown");
    }

    public void ConfirmFriendlyLoot(string expeditionId)
    {
        if (IsFriendlyMatch && Expedition.id == expeditionId && PlayerManager.Instance.HasSharedReceipt(expeditionId))
            SendExpeditionCommand("settled");
    }

    private void ResolveFriendlyResult()
    {
        var state = serverExpedition;
        var settings = state.friendly;
        if (!state.players.All(player => expeditionAcks.Contains(player.ownerId) ||
            !members.Values.Any(member => member.profileId == player.ownerId))) return;
        expeditionDeadline = 0;
        settings.resultReady = true;
        bool everyoneConnected = state.players.All(player => members.Values.Any(member => member.profileId == player.ownerId));
        if (settings.returnToTown || !everyoneConnected || state.players.All(player => settings.rematchVotes.Contains(player.ownerId)))
        {
            settings.rematch = !settings.returnToTown && everyoneConnected;
            state.phase = ExpeditionPhase.Ended;
        }
        PublishExpedition();
    }

    public bool IsFriendlyStakeLocked(InventorySlotData slot)
    {
        if (slot == null) return false;
        var settings = Expedition?.friendly ?? PlayerManager.Instance.SharedCheckpoint?.friendly ?? Formation.friendly;
        var own = settings?.participants.Find(p => p.ownerId == LocalProfileId);
        return own?.locked == true && own.stake.Any(item => FriendlyItemsMatch(slot, item));
    }

    private static bool FriendlyItemsMatch(InventorySlotData left, InventorySlotData right)
    {
        var a = Newtonsoft.Json.Linq.JObject.FromObject(left);
        var b = Newtonsoft.Json.Linq.JObject.FromObject(right);
        a.Remove("count"); b.Remove("count");
        return Newtonsoft.Json.Linq.JToken.DeepEquals(a, b);
    }

    private void SetFriendlyPause(bool value)
    {
        if (friendlyPaused == value) return;
        if (value) { friendlyPreviousTimeScale = Time.timeScale; Time.timeScale = 0f; }
        else Time.timeScale = friendlyPreviousTimeScale;
        friendlyPaused = value;
        RefreshBattleControls();
    }

    private void UpdateFriendlyConnection()
    {
        if (!IsFriendlyMatch) return;
        if (friendlyMigrationWinner >= 0)
        {
            if (IsHost && (Time.realtimeSinceStartup >= friendlyMigrationDeadline ||
                serverExpedition.friendly.participants.Where(p => p.team == friendlyMigrationWinner)
                    .All(p => members.Values.Any(m => m.profileId == p.ownerId))))
            {
                int winner = friendlyMigrationWinner;
                friendlyMigrationWinner = -1;
                FinishFriendlyBattle(winner);
            }
            return;
        }
        if (IsHost && serverExpedition.friendly.winningTeam < 0)
        {
            var absent = serverExpedition.friendly.disconnected;
            if (absent.Count > 0)
            {
                if (battleWaitDeadline > 0) battleWaitDeadline += Time.unscaledDeltaTime;
                if (expeditionDeadline > 0) expeditionDeadline += Time.unscaledDeltaTime;
                var expired = absent.FirstOrDefault(p => p.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                if (expired.Key != null)
                {
                    int loser = FriendlyTeam(expired.Key);
                    FinishFriendlyBattle(1 - loser);
                    return;
                }
            }
        }
        if (friendlyReconnectDeadline <= 0) return;
        if (Time.realtimeSinceStartup >= friendlyReconnectDeadline)
        {
            if (steamInitialized && lobby.IsValid() && ulong.TryParse(friendlyHostAddress, out ulong previousHost) &&
                Steamworks.SteamMatchmaking.GetLobbyOwner(lobby).m_SteamID != previousHost)
            {
                if (Steamworks.SteamMatchmaking.GetLobbyOwner(lobby) == Steamworks.SteamUser.GetSteamID())
                {
                    friendlyReconnectDeadline = 0;
                    friendlyMigrationWinner = 1 - FriendlyTeam(Expedition.players.First(p => p.host).ownerId);
                    friendlyMigrationDeadline = Time.realtimeSinceStartup + 30f;
                    serverExpedition = JsonConvert.DeserializeObject<ExpeditionState>(JsonConvert.SerializeObject(Expedition));
                    serverFormation = JsonConvert.DeserializeObject<FormationState>(JsonConvert.SerializeObject(Formation));
                    TurnManager.Instance.StopSharedBattle();
                    members.Clear();
                    hosting = true;
                    expeditionDeadline = battleWaitDeadline = decisionDeadline = 0;
                    Steamworks.SteamMatchmaking.SetLobbyData(lobby, "ll_host", Steamworks.SteamUser.GetSteamID().m_SteamID.ToString());
                    transport.SetMaximumClients(10, transportIndex);
                    if (!transport.StartConnection(true, transportIndex))
                    {
                        hosting = false;
                        friendlyMigrationWinner = -1;
                        friendlyReconnectDeadline = Time.realtimeSinceStartup + 5f;
                    }
                }
                else friendlyReconnectDeadline = Time.realtimeSinceStartup + 30f;
                return;
            }
            friendlyReconnectDeadline = 0;
            SetFriendlyPause(false);
            var saved = JsonConvert.DeserializeObject<ExpeditionState>(JsonConvert.SerializeObject(Expedition));
            int losingTeam = FriendlyTeam(LocalProfileId);
            ResolveOfflineFriendlyForfeit(saved, losingTeam);
            return;
        }
        if (Time.realtimeSinceStartup >= friendlyRetryAt && !networkManager.IsClientStarted)
        {
            friendlyRetryAt = Time.realtimeSinceStartup + 5f;
            if (steamInitialized && lobby.IsValid())
            {
                string currentHost = Steamworks.SteamMatchmaking.GetLobbyData(lobby, "ll_host");
                if (ulong.TryParse(currentHost, out _)) friendlyHostAddress = currentHost;
            }
            networkManager.ClientManager.StartConnection(friendlyHostAddress);
        }
    }

    private void ResolveOfflineFriendlyForfeit(ExpeditionState state, int losingTeam)
    {
        state.friendly.winningTeam = 1 - losingTeam;
        state.claimedLoot.Clear();
        state.finished = state.settlementPrepared = true;
        state.phase = ExpeditionPhase.Result;
        if (PlayerManager.Instance.SaveSharedCheckpoint(state) && CommitFriendlyRewards(state))
        {
            LeaveRoom();
            Status = "재접속 대기 시간이 만료되어 몰수패했습니다. 참가 용병은 회복되었습니다.";
        }
        else Status = "몰수패 결과 저장을 재시도해야 합니다. 보관된 친선전에서 다시 정산하세요.";
        RefreshControls();
    }
}
