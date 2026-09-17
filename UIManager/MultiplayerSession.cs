using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using Steamworks;

using UnityEngine;


public partial class MultiplayerSession : MonoBehaviour
{
    public bool IsReady => ready;
    public bool CanInvite => networkManager != null &&
                             networkManager.ClientManager.Connection.IsAuthenticated &&
                             lobby.IsValid();

    public NetworkManager networkManager;
    public Multipass transport;
    public static MultiplayerSession Instance { get; private set; }
    public event Action Changed;
    public event Action OpenRequested;
    public string Address { get; set; } = "127.0.0.1";
    public string Status { get; private set; } = "방을 만들거나 참가하세요.";
    public string MembersText { get; private set; } = "방을 만들거나 참가하세요.";
    public int Capacity => capacity;
    public bool UsesSteam => transportIndex == 1;
    public string InviteCode => inviteCode;
    public bool HasOtherParticipants => members.Count > 1 ||
        (steamInitialized && lobby.IsValid() && SteamMatchmaking.GetNumLobbyMembers(lobby) > 1) ||
        (IsHost && networkManager.ServerManager.Clients.Count > 1);
    public bool Connected => roomActive && networkManager != null && networkManager.ClientManager.Connection.IsAuthenticated;
    public bool IsHost => Connected && networkManager.IsServerStarted;
    public bool Editable => !busy && !hosting && !networkManager.IsClientStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    public struct RoomMemberMessage : IBroadcast
    {
        public string version;
        public string profileId;
        public string name;
        public bool ready;
        public int revision;
    }

    public struct RoomStateMessage : IBroadcast
    {
        public int capacity;
        public string members;
        public string formation;
    }

    private const string RoomVersion = "LL-coop-friendly-7";
    private readonly Dictionary<int, RoomMemberMessage> members = new();
    private int capacity = 4;
    private int transportIndex;
    private bool busy;
    private bool hosting;
    private bool ready;
    private bool leaving;
    private bool steamInitialized;
    private bool roomActive;
    private float connectionDeadline;
    private CSteamID lobby;
    private string inviteCode;
    private QuestDef pendingQuest;
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<LobbyEnter_t> lobbyEntered;
    private Callback<GameLobbyJoinRequested_t> joinRequested;

    private void Start()
    {
        if (Instance != this) return;
        transport.SetClientTransport(0);
        networkManager.ClientManager.OnAuthenticated += OnAuthenticated;
        networkManager.ClientManager.OnClientConnectionState += OnClientState;
        networkManager.ServerManager.OnServerConnectionState += OnServerState;
        networkManager.ServerManager.OnRemoteConnectionState += OnRemoteState;
        networkManager.ServerManager.RegisterBroadcast<RoomMemberMessage>(OnMemberMessage);
        networkManager.ClientManager.RegisterBroadcast<RoomStateMessage>(OnRoomState);
        networkManager.ServerManager.RegisterBroadcast<FormationMessage>(OnFormationMessage);
        networkManager.ServerManager.RegisterBroadcast<ExpeditionCommand>(OnExpeditionCommand);
        networkManager.ClientManager.RegisterBroadcast<ExpeditionMessage>(OnExpeditionMessage);
        networkManager.ServerManager.RegisterBroadcast<BattleCommand>(OnBattleCommand);
        networkManager.ClientManager.RegisterBroadcast<BattleMessage>(OnBattleMessage);
        networkManager.ServerManager.RegisterBroadcast<FriendlyMatchCommand>(OnFriendlyMatchCommand);
        InitializeSteam();
        RefreshControls();
    }

    private void Update()
    {
        OpenFormationWhenAvailable();
        UpdateFriendlyConnection();
        UpdateExpedition();
        if (steamInitialized)
            SteamAPI.RunCallbacks();
        if (busy && Time.realtimeSinceStartup >= connectionDeadline)
        {
            LeaveRoom();
            Status = "접속 시간이 초과되었습니다. 주소와 상대의 실행 상태를 확인하세요.";
        }
    }

    public void Open()
    {
        OpenRequested?.Invoke();
        RefreshControls();
    }

    public void Close()
    {
        LeaveRoom();
        
    }

    public void ToggleMode()
    {
        if (busy || networkManager.IsClientStarted || hosting)
            return;
        transportIndex = 1 - transportIndex;
        transport.SetClientTransport(transportIndex);
        Address = transportIndex == 0 ? "127.0.0.1" : "";
        RefreshControls();
    }

    public void CycleCapacity()
    {
        if (busy || networkManager.IsClientStarted || hosting)
            return;
        capacity = capacity == 4 ? 2 : capacity + 1;
        RefreshControls();
    }

    public void OpenQuestRoom(QuestDef quest)
    {
        if (quest == null) return;
        if (Connected)
        {
            SelectQuest(quest);
            return;
        }
        if (!Editable) return;
        pendingQuest = quest;
        transportIndex = 1;
        capacity = 4;
        HostRoom();
        if (!busy && !Connected)
            Status = "혼자 출발할 수 있습니다. 초대 연결 실패: " + Status;
        RefreshControls();
    }

    public void JoinByCode(string code)
    {
        if (!Editable)
        {
            Status = "현재 편성창에서 나간 후 다른 의뢰에 참가하세요.";
            return;
        }
        transportIndex = 1;
        Address = code;
        JoinRoom();
    }

    private bool PrepareConnection()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.active != null)
        {
            Status = "진행 중인 원정을 마친 후 멀티플레이 방에 참가하세요.";
            return false;
        }
        if (string.IsNullOrEmpty(LocalProfileId))
        {
            Status = "플레이어 데이터를 먼저 불러오세요.";
            return false;
        }
        if (busy || networkManager.IsClientStarted || hosting)
            return false;
        if (transportIndex == 1 && !InitializeSteam())
            return false;
        transport.SetClientTransport(transportIndex);
        transport.SetMaximumClients(capacity, transportIndex);
        busy = true;
        connectionDeadline = Time.realtimeSinceStartup + 30f;
        Status = "접속을 준비하는 중입니다.";
        RefreshControls();
        return true;
    }

    public void HostRoom()
    {
        if (!PrepareConnection())
            return;
        hosting = true;
        if (transportIndex == 1)
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, capacity);
        else if (!transport.StartConnection(true, transportIndex))
        {
            LeaveRoom();
            Status = "방을 열지 못했습니다. UDP 7770 포트 사용 여부를 확인하세요.";
        }
    }

    public void JoinRoom()
    {
        string address = Address.Trim();
        if (address.Length == 0)
        {
            Status = "호스트 IP 또는 Steam 초대 코드를 입력하세요.";
            return;
        }
        ulong lobbyId = 0;
        if (transportIndex == 1 &&
            (!ulong.TryParse(address, out lobbyId) || !new CSteamID(lobbyId).IsLobby()))
        {
            Status = "유효한 Steam 초대 코드를 입력하세요.";
            return;
        }
        if (!PrepareConnection())
            return;
        if (transportIndex == 1)
            SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
        else if (!networkManager.ClientManager.StartConnection(address))
        {
            LeaveRoom();
            Status = "접속을 시작하지 못했습니다.";
        }
    }

    private bool InitializeSteam()
    {
        if (steamInitialized)
            return true;
        try
        {
            steamInitialized = SteamAPI.Init();
        }
        catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException)
        {
            Debug.LogError($"Steam initialization failed: {e.Message}");
        }
        if (!steamInitialized)
        {
            Status = "Steam을 실행한 뒤 다시 시도하세요. steam_appid.txt도 필요합니다.";
            return false;
        }
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        return true;
    }

    private void OnLobbyCreated(LobbyCreated_t result)
    {
        if (!hosting || !busy)
        {
            if (result.m_eResult == EResult.k_EResultOK)
                SteamMatchmaking.LeaveLobby(new CSteamID(result.m_ulSteamIDLobby));
            return;
        }
        if (result.m_eResult != EResult.k_EResultOK)
        {
            LeaveRoom();
            Status = $"Steam 방 생성 실패: {result.m_eResult}";
            return;
        }
        lobby = new CSteamID(result.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(lobby, "ll_version", RoomVersion);
        SteamMatchmaking.SetLobbyData(lobby, "ll_host", SteamUser.GetSteamID().m_SteamID.ToString());
        inviteCode = lobby.m_SteamID.ToString();
        if (!transport.StartConnection(true, transportIndex))
        {
            LeaveRoom();
            Status = "Steam 호스트 연결을 시작하지 못했습니다.";
        }
    }

    private void OnLobbyEntered(LobbyEnter_t result)
    {
        if (hosting)
            return;
        var enteredLobby = new CSteamID(result.m_ulSteamIDLobby);
        if (!busy)
        {
            SteamMatchmaking.LeaveLobby(enteredLobby);
            return;
        }
        if (result.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            LeaveRoom();
            Status = "방에 입장하지 못했습니다. 초대 코드와 남은 자리를 확인하세요.";
            return;
        }
        lobby = enteredLobby;
        string host = SteamMatchmaking.GetLobbyData(lobby, "ll_host");
        if (SteamMatchmaking.GetLobbyData(lobby, "ll_version") != RoomVersion || !ulong.TryParse(host, out _))
        {
            LeaveRoom();
            Status = "현재 게임 버전의 방이 아닙니다.";
            return;
        }
        inviteCode = lobby.m_SteamID.ToString();
        if (!networkManager.ClientManager.StartConnection(host))
        {
            LeaveRoom();
            Status = "호스트 연결을 시작하지 못했습니다.";
        }
    }

    private void OnJoinRequested(GameLobbyJoinRequested_t request)
    {
        Open();
        if (busy || networkManager.IsClientStarted || hosting)
        {
            Status = "현재 방에서 나온 후 새 초대를 수락하세요.";
            return;
        }
        transportIndex = 1;
        Address = request.m_steamIDLobby.m_SteamID.ToString();
        JoinRoom();
    }

    private void OnServerState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started && hosting)
        {
            string host = transportIndex == 0 ? "127.0.0.1" : SteamUser.GetSteamID().m_SteamID.ToString();
            if (!networkManager.ClientManager.StartConnection(host))
            {
                LeaveRoom();
                Status = "호스트의 로컬 참가 연결에 실패했습니다.";
            }
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped && hosting && !leaving)
        {
            LeaveRoom();
            Status = "호스트 연결이 종료되었습니다.";
        }
    }

    private void OnAuthenticated()
    {
        busy = false;
        roomActive = true;
        ready = false;
        friendlyHostAddress = transportIndex == 1 && lobby.IsValid()
            ? SteamMatchmaking.GetLobbyData(lobby, "ll_host") : Address;
        if (IsFriendlyMatch) { SendMember(); return; }
        if (hosting && pendingQuest != null) SelectQuest(pendingQuest);
        SendMember();
        Status = "접속 완료. 참가자 목록과 준비 상태가 실시간으로 공유됩니다.";
        RefreshControls();
    }

    public void ToggleReady()
    {
        SetReady(!ready);
    }

    public void SetReady(bool value)
    {
        if (!networkManager.ClientManager.Connection.IsAuthenticated || ready == value)
            return;

        if (value && !CanReady(out string reason))
        {
            Status = reason;
            RefreshControls();
            return;
        }

        ready = value;
        SendMember();
        RefreshControls();
    }

    private void SendMember()
    {
        string name = transportIndex == 1 ? SteamFriends.GetPersonaName() : Environment.MachineName;
        networkManager.ClientManager.Broadcast(new RoomMemberMessage
        {
            version = RoomVersion, profileId = LocalProfileId, name = name, ready = ready, revision = Formation.revision
        });
    }

    private void OnMemberMessage(NetworkConnection connection, RoomMemberMessage message, Channel channel)
    {
        if (serverExpedition != null)
        {
            if (serverExpedition.friendly != null && message.version == RoomVersion &&
                serverExpedition.players.Exists(p => p.ownerId == message.profileId) &&
                !members.Any(p => p.Key != connection.ClientId && p.Value.profileId == message.profileId))
            {
                members[connection.ClientId] = message;
                serverExpedition.friendly.disconnected.Remove(message.profileId);
                SetFriendlyPause(serverExpedition.friendly.winningTeam < 0 && serverExpedition.friendly.disconnected.Count > 0);
                PublishMembers();
                PublishExpedition();
                if (IsSharedBattle && serverExpedition.friendly.battle != null)
                {
                    var snapshot = friendlyMigrationWinner >= 0 ? serverExpedition.friendly.battle : CaptureBattleState();
                    connection.Broadcast(new BattleMessage
                    {
                        expeditionId = serverExpedition.id, nodeId = serverExpedition.runtime.currentRouteNodeId,
                        json = Newtonsoft.Json.JsonConvert.SerializeObject(new BattlePacket
                        { kind = "resync", actionId = waitingBattleAction, state = snapshot })
                    });
                    presentationAcks.Add(message.profileId);
                }
                else if (IsSharedBattle)
                {
                    connection.Broadcast(new BattleMessage
                    {
                        expeditionId = serverExpedition.id, nodeId = serverExpedition.runtime.currentRouteNodeId,
                        json = Newtonsoft.Json.JsonConvert.SerializeObject(new BattlePacket { kind = "loadCheck" })
                    });
                }
                return;
            }
            if (!members.ContainsKey(connection.ClientId)) connection.Disconnect(true);
            return;
        }
        if (message.version != RoomVersion || string.IsNullOrEmpty(message.profileId) || message.profileId.Length > 64)
        {
            connection.Disconnect(true);
            return;
        }
        if (resumeCheckpoint != null && !resumeCheckpoint.players.Exists(player => player.ownerId == message.profileId))
        {
            connection.Disconnect(true);
            return;
        }
        foreach (var member in members)
        {
            if (member.Key != connection.ClientId && member.Value.profileId == message.profileId)
            {
                connection.Disconnect(true);
                return;
            }
        }
        if (members.TryGetValue(connection.ClientId, out var previous) && previous.profileId != message.profileId)
        {
            connection.Disconnect(true);
            return;
        }
        if (!members.ContainsKey(connection.ClientId) && members.Count >= capacity)
        {
            connection.Disconnect(true);
            return;
        }
        message.name = (message.name ?? "참가자").Replace('\n', ' ').Replace('\r', ' ').Replace('<', ' ').Replace('>', ' ');
        if (message.name.Length > 32)
            message.name = message.name.Substring(0, 32);
        message.ready = message.ready && message.revision == serverFormation.revision &&
                        serverFormation.slots.Exists(slot => slot.ownerId == message.profileId);
        if (!members.ContainsKey(connection.ClientId)) InvalidateReady();
        members[connection.ClientId] = message;
        PublishMembers();
    }

    private void OnRemoteState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped && members.TryGetValue(connection.ClientId, out var member) && !leaving)
        {
            if (serverExpedition != null)
            {
                if (serverExpedition.friendly != null)
                {
                    members.Remove(connection.ClientId);
                    if (serverExpedition.finished)
                    {
                        serverExpedition.friendly.returnToTown = true;
                        ResolveFriendlyResult();
                        return;
                    }
                    serverExpedition.friendly.disconnected[member.profileId] =
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)FriendlyReconnectSeconds;
                    serverExpedition.text = "연결이 끊긴 참가자를 120초 동안 기다립니다. 미복귀 시 해당 팀은 몰수패합니다.";
                    if (serverExpedition.friendly.winningTeam < 0) SetFriendlyPause(true);
                    PublishExpedition();
                    return;
                }
                AbortExpedition("참가자의 연결이 끊어져 공동 원정을 종료합니다.");
                return;
            }
            members.Remove(connection.ClientId);
            if (resumeCheckpoint == null) serverFormation.slots.RemoveAll(slot => slot.ownerId == member.profileId);
            InvalidateReady();
            PublishMembers();
        }
    }

    private void PublishMembers()
    {
        if (serverFormation.friendly != null && serverExpedition == null)
        {
            var participants = serverFormation.friendly.participants;
            participants.RemoveAll(p => !members.Values.Any(member => member.profileId == p.ownerId));
            foreach (var member in members.Values)
                if (!participants.Exists(p => p.ownerId == member.profileId))
                    participants.Add(new FriendlyParticipant
                    {
                        ownerId = member.profileId,
                        team = participants.Count(p => p.team == 0) <= participants.Count(p => p.team == 1) ? 0 : 1
                    });
        }
        var text = new StringBuilder();
        int readyCount = 0;
        foreach (var member in members)
        {
            if (member.Value.ready)
                readyCount++;
            text.AppendLine($"{member.Value.name}    {(member.Value.ready ? "준비 완료" : "대기 중")}");
        }
        text.Append($"\n참가 {members.Count}/{capacity}명 · 준비 {readyCount}/{members.Count}명");
        serverFormation.members.Clear();
        foreach (var member in members)
            serverFormation.members.Add(new FormationMember
            {
                ownerId = member.Value.profileId,
                name = member.Value.name,
                ready = member.Value.ready,
                host = member.Value.profileId == LocalProfileId
            });
        networkManager.ServerManager.Broadcast(new RoomStateMessage
        {
            capacity = capacity, members = text.ToString(),
            formation = Newtonsoft.Json.JsonConvert.SerializeObject(serverFormation)
        });
    }

    private void OnRoomState(RoomStateMessage message, Channel channel)
    {
        capacity = message.capacity;
        MembersText = message.members;
        ApplyFormation(message.formation);
        RefreshControls();
    }

    private void OnClientState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped && !leaving)
        {
            if (IsFriendlyMatch && Expedition.friendly.winningTeam < 0)
            {
                if (friendlyReconnectDeadline <= 0)
                    friendlyReconnectDeadline = Time.realtimeSinceStartup + FriendlyReconnectSeconds;
                Status = "친선전 연결 복구 중 · 120초 안에 복귀하지 못하면 몰수패합니다.";
                SetFriendlyPause(true);
                RefreshControls();
                return;
            }
            LeaveRoom();
            Status = "접속이 종료되었습니다. 방장이 나갔거나 연결이 끊어졌습니다.";
        }
    }

    public void InviteFriend()
    {
        if (!steamInitialized || !lobby.IsValid())
        {
            Status = "Steam 방 연결이 완료된 뒤 친구를 초대할 수 있습니다.";
            RefreshControls();
            return;
        }

        if (!SteamUtils.IsOverlayEnabled())
        {
            if (!string.IsNullOrEmpty(inviteCode))
                GUIUtility.systemCopyBuffer = inviteCode;

            Status = "Steam 오버레이를 열 수 없어 초대 코드를 클립보드에 복사했습니다.";
            RefreshControls();
            return;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(lobby);
        Status = "Steam 친구 초대창을 열었습니다.";
        RefreshControls();
    }

    public void CopyInviteCode()
    {
        if (!string.IsNullOrEmpty(inviteCode))
        {
            GUIUtility.systemCopyBuffer = inviteCode;
            Status = "Steam 초대 코드를 복사했습니다.";
        }
    }

    public void LeaveRoom()
    {
        if (leaving)
            return;
        leaving = true;
        friendlyMigrationWinner = -1;
        friendlyReconnectDeadline = 0;
        SetFriendlyPause(false);
        RestoreBeforeExpedition();
        roomActive = false;
        busy = false;
        pendingQuest = null;
        if (steamInitialized && lobby.IsValid())
        {
            if (hosting) SteamMatchmaking.SetLobbyJoinable(lobby, false);
            SteamMatchmaking.LeaveLobby(lobby);
        }
        lobby = default;
        inviteCode = null;
        networkManager.ClientManager.StopConnection();
        if (hosting)
            transport.StopConnection(true, transportIndex);
        hosting = false;
        ready = false;
        members.Clear();
        serverFormation = new FormationState();
        resumeCheckpoint = null;
        Formation = new FormationState();
        openedQuestId = null;
        MembersText = "방을 만들거나 참가하세요.";
        Status = "방에서 나왔습니다.";
        leaving = false;
        FormationChanged?.Invoke();
        RefreshControls();
    }

    private void RefreshControls() => Changed?.Invoke();

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (networkManager == null)
            return;
        networkManager.ClientManager.OnAuthenticated -= OnAuthenticated;
        networkManager.ClientManager.OnClientConnectionState -= OnClientState;
        networkManager.ServerManager.OnServerConnectionState -= OnServerState;
        networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteState;
        networkManager.ServerManager.UnregisterBroadcast<RoomMemberMessage>(OnMemberMessage);
        networkManager.ClientManager.UnregisterBroadcast<RoomStateMessage>(OnRoomState);
        networkManager.ServerManager.UnregisterBroadcast<FormationMessage>(OnFormationMessage);
        networkManager.ServerManager.UnregisterBroadcast<ExpeditionCommand>(OnExpeditionCommand);
        networkManager.ClientManager.UnregisterBroadcast<ExpeditionMessage>(OnExpeditionMessage);
        networkManager.ServerManager.UnregisterBroadcast<BattleCommand>(OnBattleCommand);
        networkManager.ClientManager.UnregisterBroadcast<BattleMessage>(OnBattleMessage);
        networkManager.ServerManager.UnregisterBroadcast<FriendlyMatchCommand>(OnFriendlyMatchCommand);
        if (steamInitialized)
        {
            if (lobby.IsValid())
                SteamMatchmaking.LeaveLobby(lobby);
            lobbyCreated.Dispose();
            lobbyEntered.Dispose();
            joinRequested.Dispose();
            SteamAPI.Shutdown();
        }
    }
}
