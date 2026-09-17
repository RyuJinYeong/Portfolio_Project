using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public partial class MultiplayerSession
{
    public enum SharedBattlePhase { Loading, Attack, Counter, Resolving, Finished }

    [Serializable]
    public class BattleQueueEntry
    {
        public int skillId;
        public string userId;
        public string targetId;
        public string protectedId;
        public int incomingId;
        public int order;
        public bool concealed;
        public bool concealResolved;
        public RevealLevel reveal;
        public bool consumed;
    }

    [Serializable]
    public class BattleCharacterState
    {
        public CharacterSaveDTO data;
        public int monsterRoleId;
        public bool grantsExperience;
        public List<StatusEffectRuntimeData> effects;
        public CharacterStats temporaryStats;
        public bool isFront;
        public bool playerTurn;
        public bool extraTurn;
        public bool melee;
        public string meleeTarget;
        public List<BattleQueueEntry> attacks;
        public List<BattleQueueEntry> counters;
    }

    [Serializable]
    public class SharedBattleState
    {
        public int nodeId;
        public int revision;
        public SharedBattlePhase phase;
        public string attacker;
        public string defender;
        public string protectedTarget;
        public List<string> turnOrder = new();
        public List<string> readyPlayers = new();
        public List<BattleCharacterState> characters = new();
    }

    [Serializable]
    public class BattlePresentationResult
    {
        public string resolvedTarget;
        public bool cancelled;
        public bool protectionSucceeded;
        public bool protectionAttempted;
        public Dictionary<string, int> counters = new();
    }

    [Serializable]
    public class BattlePopup
    {
        public string targetId;
        public int damage;
        public string text;
    }

    [Serializable]
    private class BattlePacket
    {
        public string kind;
        public int actionId;
        public SharedBattleState state;
        public BattleQueueEntry action;
        public BattlePresentationResult result;
        public bool repeated;
        public List<BattlePopup> popups;
    }

    public struct BattleMessage : IBroadcast { public string expeditionId; public int nodeId; public string json; }
    public struct BattleCommand : IBroadcast
    {
        public string expeditionId;
        public int nodeId;
        public int revision;
        public string action;
        public string characterId;
        public string targetId;
        public int skillId;
        public int index;
        public bool concealed;
    }

    public SharedBattleState Battle { get; private set; }
    public bool IsSharedBattle => InExpedition && Expedition?.phase == ExpeditionPhase.Battle;
    public bool IsBattleReplica => IsSharedBattle && !IsHost;
    public bool ApplyingBattleCommand { get; private set; }
    public event Action BattleChanged;
    private SharedBattlePhase serverBattlePhase;
    private int battleRevision;
    private int battleActionId;
    private int waitingBattleAction;
    private int replicaActionId;
    private readonly HashSet<string> battleReady = new();
    private readonly HashSet<string> presentationAcks = new();
    private readonly List<BattlePopup> battlePopups = new();
    private readonly Queue<BattlePacket> replicaPackets = new();
    private readonly Dictionary<int, BattlePacket> replicaImpacts = new();
    private bool collectingBattlePopups;
    private Coroutine replicaRoutine;
    private float battleWaitDeadline;
    private bool battleCommandPending;
    private string selectedBattleActor;
    private string targetingBattleDefender;

    public CharacterManager FindBattleCharacter(string id) => string.IsNullOrEmpty(id) ? null :
        GameManager.Instance.GetAllCharacters().Find(cm => cm != null && cm.character.ID == id);

    public bool OwnsBattleCharacter(CharacterManager character) => character != null &&
        Expedition.party.Exists(slot => slot.character.id == character.character.ID && slot.ownerId == LocalProfileId);

    public bool CanControlBattleCharacter(CharacterManager character)
    {
        if (FriendlyReconnectPending) return false;
        if (!IsSharedBattle) return true;
        if (Battle == null || battleCommandPending || !OwnsBattleCharacter(character) || !character.character.IsAlive) return false;
        return Battle.phase == SharedBattlePhase.Attack && Battle.attacker == character.character.ID ||
            Battle.phase == SharedBattlePhase.Counter && IsCounterParticipant(character) && !Battle.readyPlayers.Contains(LocalProfileId);
    }

    private bool IsCounterParticipant(CharacterManager character) => character != null && character.character.IsAlive &&
        (!IsFriendlyMatch || (TurnManager.Instance.currentCharacter != null &&
        character.character.IsMine != TurnManager.Instance.currentCharacter.character.IsMine));

    public bool RouteBattleCommand(string action, CharacterManager character, SkillDefinitionSO skill = null,
        CharacterManager target = null, int index = -1, bool concealed = false)
    {
        if (!IsSharedBattle || ApplyingBattleCommand || (!IsFriendlyMatch && IsHost && character != null && !character.character.IsMine)) return false;
        if (FriendlyReconnectPending || Battle == null || battleCommandPending || (character != null && !CanControlBattleCharacter(character))) return true;
        battleCommandPending = true;
        networkManager.ClientManager.Broadcast(new BattleCommand
        {
            expeditionId = Expedition.id, nodeId = Battle.nodeId, revision = Battle.revision,
            action = action, characterId = character?.character.ID, targetId = target?.character.ID,
            skillId = skill != null ? skill.uid : 0, index = index, concealed = concealed
        });
        RefreshBattleControls();
        return true;
    }

    private BattleQueueEntry CaptureBattleQueue(SkillQueueData entry) => new()
    {
        skillId = entry.skill != null ? entry.skill.uid : 0, userId = entry.user?.character.ID,
        targetId = entry.target?.character.ID, protectedId = entry.protectedTarget?.character.ID,
        incomingId = entry.incomingSkill != null ? entry.incomingSkill.uid : 0, order = entry.order,
        concealed = entry.isConcealed, concealResolved = entry.concealResolved, reveal = entry.revealLevel,
        consumed = entry.resourcesConsumed
    };

    private SkillQueueData RestoreBattleQueue(BattleQueueEntry entry)
    {
        var registry = GameDataRegistry.Instance;
        return new SkillQueueData(registry.GetSkill(entry.skillId), FindBattleCharacter(entry.userId),
            FindBattleCharacter(entry.targetId), entry.concealed, entry.order, entry.consumed)
        {
            protectedTarget = FindBattleCharacter(entry.protectedId), incomingSkill = registry.GetSkill(entry.incomingId),
            concealResolved = entry.concealResolved, revealLevel = entry.reveal
        };
    }

    private SharedBattleState CaptureBattleState()
    {
        var tm = TurnManager.Instance;
        return new SharedBattleState
        {
            nodeId = serverExpedition.runtime.currentRouteNodeId, revision = ++battleRevision, phase = serverBattlePhase,
            attacker = tm.currentCharacter?.character.ID, defender = tm.defenseCharacter?.character.ID,
            protectedTarget = tm.defenseTarget?.character.ID, turnOrder = tm.GetSharedTurnOrder(), readyPlayers = battleReady.ToList(),
            characters = GameManager.Instance.GetAllCharacters().Where(cm => cm != null).Select(cm => new BattleCharacterState
            {
                data = SaveMapper.ToDto(cm.character), monsterRoleId = cm.character.monsterRoleId,
                grantsExperience = cm.character.GrantsExperience, effects = cm.character.StatusEffects,
                temporaryStats = cm.character.tempStats, isFront = cm.isFront, playerTurn = cm.isPlayerTurn,
                extraTurn = cm.hasExtraTurn, melee = cm.isInMeleeCombat, meleeTarget = cm.meleeTarget?.character.ID,
                attacks = cm.GetSkillQueue().Select(CaptureBattleQueue).ToList(),
                counters = cm.combatHandler.GetCounterSkillQueue().Select(CaptureBattleQueue).ToList()
            }).ToList()
        };
    }

    public void PublishBattleState(SharedBattlePhase phase)
    {
        if (!IsSharedBattle || !IsHost) return;
        serverBattlePhase = phase;
        if (phase != SharedBattlePhase.Counter) battleReady.Clear();
        SendBattlePacket(new BattlePacket { kind = "state", state = CaptureBattleState() });
    }

    private void SendBattlePacket(BattlePacket packet)
    {
        if (serverExpedition.friendly != null && packet.state != null)
            serverExpedition.friendly.battle = packet.state;
        networkManager.ServerManager.Broadcast(new BattleMessage
        {
            expeditionId = serverExpedition.id, nodeId = serverExpedition.runtime.currentRouteNodeId,
            json = JsonConvert.SerializeObject(packet)
        });
    }

    private void OnBattleMessage(BattleMessage message, Channel channel)
    {
        if (!IsSharedBattle || message.expeditionId != Expedition.id || message.nodeId != Expedition.runtime.currentRouteNodeId) return;
        var packet = JsonConvert.DeserializeObject<BattlePacket>(message.json);
        if (packet.kind == "loadCheck") { NotifySharedBattleLoaded(); return; }
        if (!IsHost && packet.kind == "resync")
        {
            if (replicaRoutine != null) StopCoroutine(replicaRoutine);
            replicaRoutine = null;
            replicaPackets.Clear();
            replicaImpacts.Clear();
            ApplyBattleState(packet.state);
            SendBattleAcknowledgement("presented", packet.actionId);
            return;
        }
        if (IsHost)
        {
            if (packet.state != null && (Battle == null || packet.state.revision > Battle.revision)) Battle = packet.state;
            battleCommandPending = false;
            RefreshBattleControls();
            return;
        }
        if (packet.kind == "impact") replicaImpacts[packet.actionId] = packet;
        else replicaPackets.Enqueue(packet);
        if (replicaRoutine == null) replicaRoutine = StartCoroutine(PlayReplicaPackets());
    }

    private IEnumerator PlayReplicaPackets()
    {
        yield return null;
        while (replicaPackets.Count > 0 && IsBattleReplica)
        {
            var packet = replicaPackets.Dequeue();
            if (packet.kind == "state") ApplyBattleState(packet.state);
            else if (packet.kind == "statusDamage")
            {
                ApplyBattleState(packet.state);
                foreach (var popup in packet.popups)
                {
                    var target = FindBattleCharacter(popup.targetId);
                    if (target == null) continue;
                    UIManager.Instance?.ShowDamage(popup.damage, target.transform.position);
                    if (target.character.IsAlive) target.battlePresentationHandler?.PlayHit();
                    else target.battlePresentationHandler?.PlayDeath();
                }
                yield return new WaitForSeconds(1.5f);
                SendBattleAcknowledgement("presented", packet.actionId);
            }
            else if (packet.kind == "prepare")
            {
                replicaActionId = packet.actionId;
                var action = RestoreBattleQueue(packet.action);
                action.user.combatHandler.ApplySharedPresentation(packet.result);
                yield return BattlePresentationDirector.Instance.PlaySkill(action, () =>
                {
                    var impact = replicaImpacts[packet.actionId];
                    ApplyBattleState(impact.state);
                    action.user.combatHandler.ApplySharedPresentation(impact.result);
                    foreach (var popup in impact.popups)
                    {
                        var target = FindBattleCharacter(popup.targetId);
                        if (target == null) continue;
                        if (popup.text != null) UIManager.Instance.ShowCombatResult(popup.text, target.transform.position);
                        else BattlePresentationDirector.Instance.TryQueueDamagePopup(popup.damage, target.transform.position);
                    }
                    replicaImpacts.Remove(packet.actionId);
                }, packet.repeated);
                SendBattleAcknowledgement("presented", packet.actionId);
            }
            else if (packet.kind == "home")
            {
                yield return BattlePresentationDirector.Instance.ReturnCharactersHome(GameManager.Instance.GetAllCharacters());
                SendBattleAcknowledgement("presented", packet.actionId);
            }
        }
        replicaRoutine = null;
    }

    private void ApplyBattleState(SharedBattleState state)
    {
        if (state == null || (Battle != null && state.revision <= Battle.revision)) return;
        Battle = state;
        battleCommandPending = false;
        foreach (var saved in state.characters)
        {
            var manager = FindBattleCharacter(saved.data.id);
            if (manager == null) { SendExpeditionCommand("reject"); return; }
            var portrait = manager.character.Portrait;
            manager.character = SaveMapper.FromDto(saved.data);
            manager.character.monsterRoleId = saved.monsterRoleId;
            manager.character.GrantsExperience = saved.grantsExperience;
            EquipmentManager.UpdateAvailableAttributes(manager.character);
            EquipmentManager.UpdateSkillAvailability(manager.character);
            manager.character.StatusEffects = saved.effects;
            manager.character.tempStats = saved.temporaryStats;
            manager.character.UpdateFinalStats();
            manager.character.CurrentHp = saved.data.currentHp;
            manager.character.CurrentStamina = saved.data.currentStamina;
            manager.character.CurrentMentality = saved.data.currentMentality;
            manager.character.Portrait = portrait;
            manager.isFront = saved.isFront;
            manager.isPlayerTurn = saved.playerTurn;
            manager.hasExtraTurn = saved.extraTurn;
            manager.isInMeleeCombat = saved.melee;
            manager.meleeTarget = FindBattleCharacter(saved.meleeTarget);
            manager.combatHandler.isDefenseCharacter = state.defender == saved.data.id;
            manager.combatHandler.isDefenseTarget = state.protectedTarget == saved.data.id;
            var attacks = manager.GetSkillQueue();
            attacks.Clear(); attacks.AddRange(saved.attacks.Select(RestoreBattleQueue));
            var counters = manager.combatHandler.GetCounterSkillQueue();
            counters.Clear(); counters.AddRange(saved.counters.Select(RestoreBattleQueue));
        }
        TurnManager.Instance.ApplySharedTurnState(state);
        foreach (var manager in GameManager.Instance.GetAllCharacters())
        {
            manager.UpdateCharacterUI();
            manager.characterUIHandler.UpdateCounterSkillQueueUI(manager.combatHandler.GetCounterSkillQueue(), manager);
        }
        var attacker = TurnManager.Instance.currentCharacter;
        if (attacker != null)
            foreach (var target in attacker.GetSkillQueue().Select(entry => entry.target).Distinct())
                target?.characterUIHandler?.UpdateSkillQueueUI(attacker.GetSkillQueue(), attacker);
        UIManager.Instance.characterTargeting.RefreshConfirmedTargetLines();
        RefreshBattleControls();
    }

    public void RefreshBattleControls()
    {
        if (!IsSharedBattle || Battle == null) return;
        var tm = TurnManager.Instance;
        var ui = UIManager.Instance;
        bool attacking = Battle.phase == SharedBattlePhase.Attack && OwnsBattleCharacter(tm.currentCharacter);
        ui.turnEndButton.gameObject.SetActive(attacking);
        ui.turnEndButton.interactable = attacking && !battleCommandPending;
        ui.turnEndButton.onClick.RemoveAllListeners();
        ui.turnEndButton.onClick.AddListener(() => RouteBattleCommand("attackDone", tm.currentCharacter));
        bool countering = Battle.phase == SharedBattlePhase.Counter && Expedition.party.Exists(slot =>
            slot.ownerId == LocalProfileId && IsCounterParticipant(FindBattleCharacter(slot.character.id)));
        ui.counterTurnEndButton.gameObject.SetActive(countering);
        ui.counterTurnEndButton.interactable = countering && !battleCommandPending && !Battle.readyPlayers.Contains(LocalProfileId);
        ui.counterTurnEndButton.onClick.RemoveAllListeners();
        ui.counterTurnEndButton.onClick.AddListener(() => RouteBattleCommand("counterDone", null));
        ui.turnTimerText.gameObject.SetActive(false);
        foreach (var character in GameManager.Instance.GetAllCharacters())
        {
            var go = character.characterUIHandler?.CounterButton;
            if (go == null) continue;
            bool available = countering && Battle.defender == null && CanControlBattleCharacter(character);
            go.SetActive(available);
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                ui.PlayBattleButtonClickSound();
                RouteBattleCommand("defender", character);
            });
        }
        if (attacking && selectedBattleActor != Battle.attacker)
        {
            selectedBattleActor = Battle.attacker;
            ui.characterTargeting.SelectCharacter(tm.currentCharacter);
        }
        if (!attacking) selectedBattleActor = null;
        if (countering && tm.defenseCharacter != null && OwnsBattleCharacter(tm.defenseCharacter))
        {
            if (tm.defenseTarget == null && targetingBattleDefender != Battle.defender)
            {
                targetingBattleDefender = Battle.defender;
                ui.characterTargeting.StartDefenseCharacterTargeting(tm.defenseCharacter);
            }
            if (tm.defenseTarget != null) ui.UpdateCounterSkillPanel(tm.defenseCharacter, tm.defenseTarget);
        }
        if (Battle.defender == null) targetingBattleDefender = null;
        if (Battle.phase == SharedBattlePhase.Resolving)
        {
            ui.characterTargeting.StopTargetingAndClearConfirmedLines();
            ui.ClearCounterSkillPanel();
        }
        foreach (var character in GameManager.Instance.GetAllCharacters()) ui.UpdateSkillTransparency(character);
        BattleChanged?.Invoke();
    }

    private void OnBattleCommand(NetworkConnection connection, BattleCommand command, Channel channel)
    {
        if (!IsSharedBattle || !IsHost || command.expeditionId != serverExpedition.id ||
            command.nodeId != serverExpedition.runtime.currentRouteNodeId || !members.TryGetValue(connection.ClientId, out var member)) return;
        if (command.action == "loaded")
        {
            if (serverBattlePhase != SharedBattlePhase.Loading) return;
            presentationAcks.Add(member.profileId);
            if (!FriendlyReconnectPending && serverExpedition.players.All(player => presentationAcks.Contains(player.ownerId)))
            {
                battleWaitDeadline = 0;
                presentationAcks.Clear();
                TurnManager.Instance.InitializeTurnOrder();
            }
            return;
        }
        if (command.action == "presented")
        {
            if (command.index == waitingBattleAction) presentationAcks.Add(member.profileId);
            return;
        }
        if (FriendlyReconnectPending) return;
        if (command.revision != battleRevision)
        {
            SendBattlePacket(new BattlePacket { kind = "state", state = CaptureBattleState() });
            return;
        }
        var tm = TurnManager.Instance;
        var character = FindBattleCharacter(command.characterId);
        var target = FindBattleCharacter(command.targetId);
        var skill = GameDataRegistry.Instance.GetSkill(command.skillId);
        bool owned = character != null && character.character.IsAlive && serverExpedition.party.Exists(slot =>
            slot.ownerId == member.profileId && slot.character.id == command.characterId);
        bool attackPhase = serverBattlePhase == SharedBattlePhase.Attack && owned && tm.currentCharacter == character;
        bool counterPhase = serverBattlePhase == SharedBattlePhase.Counter && owned && IsCounterParticipant(character);
        bool skillOwned = skill != null && character != null && character.character.Skills.Exists(runtime =>
            runtime.skillUid == skill.uid && runtime.canUse);
        ApplyingBattleCommand = true;
        try
        {
            if (attackPhase)
            {
                if (command.action == "attack" && skillOwned && !skill.isCounterSkill && target != null &&
                    target.character.IsAlive && target.character.IsMine != character.character.IsMine)
                    character.combatHandler.SelectSkill(skill, target, command.concealed);
                else if (command.action == "removeAttack") character.combatHandler.RemoveSkillFromQueue(command.index);
                else if (command.action == "replaceAttack" && skillOwned) character.combatHandler.ReplaceSkillInQueue(command.index, skill, command.concealed);
                else if (command.action == "attackDone") { tm.StartCounterTurn(); return; }
            }
            if (counterPhase)
            {
                bool hasSlot = command.index >= 0 && command.index < character.combatHandler.GetCounterSkillQueue().Count;
                if (command.action == "defender" && tm.defenseCharacter == null)
                {
                    tm.defenseCharacter = character;
                    character.combatHandler.SetDefenseCharacter(character);
                }
                else if (command.action == "protect" && tm.defenseCharacter == character && tm.defenseTarget == null &&
                    target != null && target.character.IsAlive && target.character.IsMine == character.character.IsMine)
                    character.combatHandler.SetDefenseTarget(target);
                else if (command.action == "cancelDefender" && tm.defenseCharacter == character && tm.defenseTarget == null)
                    tm.CancelDefenseCharacterSelection();
                else if (command.action == "cycle" && hasSlot) character.combatHandler.CycleBasicCounterSkill(command.index);
                else if (command.action == "clearCounter" && hasSlot && tm.defenseCharacter == character)
                    character.combatHandler.ClearCounterSkillAt(command.index);
                else if (command.action == "counter" && hasSlot && skillOwned && skill.isCounterSkill)
                    character.combatHandler.SetOrResetCounterSkill(command.index, skill);
                else if (command.action == "appendCounter" && skillOwned && skill.isCounterSkill &&
                    tm.defenseCharacter == character && target == tm.defenseTarget && target != null)
                    character.combatHandler.SelectCounterSkill(skill, target);
                battleReady.Clear();
            }
            if (command.action == "counterDone" && serverBattlePhase == SharedBattlePhase.Counter &&
                serverExpedition.party.Exists(slot => slot.ownerId == member.profileId && IsCounterParticipant(FindBattleCharacter(slot.character.id))))
            {
                battleReady.Add(member.profileId);
                if (serverExpedition.party.Where(slot => IsCounterParticipant(FindBattleCharacter(slot.character.id)))
                    .All(slot => battleReady.Contains(slot.ownerId)))
                {
                    tm.FinishSharedCounterTurn();
                    return;
                }
            }
        }
        finally { ApplyingBattleCommand = false; }
        SendBattlePacket(new BattlePacket { kind = "state", state = CaptureBattleState() });
    }

    public void NotifySharedBattleLoaded()
    {
        if (BattlePresentationDirector.Instance == null || GameManager.Instance.GetAllCharacters().Any(cm =>
            cm.battlePresentationHandler == null || cm.combatHandler == null))
        {
            Debug.LogError("공동 전투 참가자의 전투 연출 컴포넌트가 누락되었습니다.");
            SendExpeditionCommand("reject");
            return;
        }
        BattlePresentationDirector.Instance.PrepareBattle(GameManager.Instance.GetAllCharacters());
        StartCoroutine(PrepareReplicaBattleWeapons());
    }

    private IEnumerator PrepareReplicaBattleWeapons()
    {
        if (!IsHost)
            yield return TurnManager.Instance.PrepareBattleWeapons(false);

        SendBattleAcknowledgement("loaded", 0);
    }

    private void SendBattleAcknowledgement(string action, int id)
    {
        if (!IsSharedBattle) return;
        networkManager.ClientManager.Broadcast(new BattleCommand
        { expeditionId = Expedition.id, nodeId = Expedition.runtime.currentRouteNodeId, action = action, index = id });
    }

    private BattlePresentationResult CapturePresentation(CombatHandler handler) => new()
    {
        resolvedTarget = handler.LastResolvedTarget?.character.ID, cancelled = handler.LastSkillWasCancelled,
        protectionSucceeded = handler.LastProtectionSucceeded, protectionAttempted = handler.LastProtectionAttempted,
        counters = handler.LastSuccessfulCounters.ToDictionary(pair => pair.Key.character.ID, pair => pair.Value.uid)
    };

    public void BroadcastSkillPreparation(SkillQueueData action, bool repeated)
    {
        if (!IsSharedBattle || !IsHost) return;
        battleActionId++;
        waitingBattleAction = battleActionId;
        presentationAcks.Clear();
        battlePopups.Clear();
        collectingBattlePopups = true;
        battleWaitDeadline = Time.realtimeSinceStartup + 90f;
        SendBattlePacket(new BattlePacket
        { kind = "prepare", actionId = battleActionId, action = CaptureBattleQueue(action), result = CapturePresentation(action.user.combatHandler), repeated = repeated });
    }

    public void BroadcastSkillImpact(SkillQueueData action)
    {
        if (!IsSharedBattle || !IsHost) return;
        collectingBattlePopups = false;
        SendBattlePacket(new BattlePacket
        { kind = "impact", actionId = battleActionId, state = CaptureBattleState(), result = CapturePresentation(action.user.combatHandler), popups = battlePopups.ToList() });
    }

    public void CaptureBattlePopup(int damage, string text, Vector3 position)
    {
        if (!IsSharedBattle || !IsHost || !collectingBattlePopups) return;
        var target = GameManager.Instance.GetAllCharacters().OrderBy(cm => (cm.transform.position - position).sqrMagnitude).FirstOrDefault();
        if (target != null) battlePopups.Add(new BattlePopup { targetId = target.character.ID, damage = damage, text = text });
    }

    public IEnumerator WaitForReplicaImpact()
    {
        if (!IsBattleReplica) yield break;
        float until = Time.realtimeSinceStartup + 90f;
        while (IsBattleReplica && !replicaImpacts.ContainsKey(replicaActionId) && Time.realtimeSinceStartup < until)
        {
            if (FriendlyReconnectPending) until += Time.unscaledDeltaTime;
            yield return null;
        }
        if (IsBattleReplica && !replicaImpacts.ContainsKey(replicaActionId))
        {
            SendExpeditionCommand("reject");
            while (IsBattleReplica) yield return null;
        }
    }

    public IEnumerator WaitForBattlePresentation()
    {
        if (!IsSharedBattle || !IsHost) yield break;
        float until = Time.realtimeSinceStartup + 90f;
        while (IsSharedBattle && serverExpedition.players.Any(player => player.ownerId != LocalProfileId && !presentationAcks.Contains(player.ownerId)) &&
            Time.realtimeSinceStartup < until)
        {
            if (FriendlyReconnectPending) until += Time.unscaledDeltaTime;
            yield return null;
        }
        if (IsSharedBattle && serverExpedition.players.Any(player => player.ownerId != LocalProfileId && !presentationAcks.Contains(player.ownerId)))
            AbortExpedition("전투 연출 응답 시간이 초과되었습니다.");
        battleWaitDeadline = 0;
    }

    public void BroadcastStatusDamage(CharacterManager target, int damage)
    {
        if (!IsSharedBattle || !IsHost) return;
        waitingBattleAction = ++battleActionId;
        presentationAcks.Clear();
        battleWaitDeadline = Time.realtimeSinceStartup + 90f;
        SendBattlePacket(new BattlePacket
        {
            kind = "statusDamage", actionId = battleActionId, state = CaptureBattleState(),
            popups = new List<BattlePopup> { new BattlePopup { targetId = target.character.ID, damage = damage } }
        });
    }

    public void BroadcastBattleHome()
    {
        if (!IsSharedBattle || !IsHost) return;
        waitingBattleAction = ++battleActionId;
        presentationAcks.Clear();
        battleWaitDeadline = Time.realtimeSinceStartup + 90f;
        SendBattlePacket(new BattlePacket { kind = "home", actionId = battleActionId });
    }

    private void ResetSharedBattle()
    {
        Battle = null;
        serverBattlePhase = SharedBattlePhase.Loading;
        battleRevision = battleActionId = waitingBattleAction = replicaActionId = 0;
        battleReady.Clear(); presentationAcks.Clear(); replicaPackets.Clear(); replicaImpacts.Clear(); battlePopups.Clear();
        if (replicaRoutine != null) StopCoroutine(replicaRoutine);
        replicaRoutine = null;
        collectingBattlePopups = ApplyingBattleCommand = battleCommandPending = false;
        selectedBattleActor = targetingBattleDefender = null;
        battleWaitDeadline = 0;
        BattleChanged?.Invoke();
    }

    public void FinishSharedBattle(bool won)
    {
        if (!IsSharedBattle || !IsHost) return;
        PublishBattleState(SharedBattlePhase.Finished);
        if (IsFriendlyMatch) { FinishFriendlyBattle(won ? 0 : 1); return; }
        if (!won) { AbortExpedition("원정대가 전멸했습니다."); return; }
        var characters = GameManager.Instance.GetAllCharacters();
        int experience = ProgressionRules.SettleVictoryExperience(characters);
        int gold = ProgressionRules.SettleVictoryGold(characters);
        var loot = ProgressionRules.RollVictoryLoot(characters);
        var player = PlayerManager.Instance.GetCurrentPlayerData();
        player.gold -= gold;
        for (int i = 0; i < serverExpedition.party.Count; i++)
        {
            string owner = serverExpedition.party[i].ownerId;
            serverExpedition.battleGold.TryGetValue(owner, out int previous);
            serverExpedition.battleGold[owner] = previous + gold / serverExpedition.party.Count +
                (i < gold % serverExpedition.party.Count ? 1 : 0);
        }
        foreach (var item in loot)
        {
            EquipmentInstanceRepository.PromoteRuntimeToPlayer(item.equipmentInstanceId);
        }
        serverExpedition.pendingLoot.AddRange(loot);
        QuestManager.Instance.CompleteCurrentRouteNode();
        serverExpedition.questCompleted = QuestManager.Instance.active == null;
        CaptureExpeditionCharacters();
        sharedResolution = null;
        serverExpedition.phase = ExpeditionPhase.Result;
        serverExpedition.afterRewards = "battleContinue";
        serverExpedition.rewardSummary = $"전투 승리\n경험치 {experience} · 골드 {gold} · 전리품 {loot.Count}개\n골드는 투입 용병 수에 따라 분배됩니다.";
        BeginSharedNodeRewards();
    }
}
