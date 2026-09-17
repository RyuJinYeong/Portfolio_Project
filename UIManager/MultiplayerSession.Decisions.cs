using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class MultiplayerSession
{
    [Serializable]
    public class SharedOption
    {
        public string id;
        public string label;
    }

    [Serializable]
    public class SharedBallot
    {
        public string characterId;
        public string ownerId;
        public string optionId;
        public int roll;
        public int previousRoll;
        public bool eligible = true;
    }

    [Serializable]
    public class SharedDecision
    {
        public int id;
        public string kind;
        public int choiceIndex = -1;
        public string title;
        public List<SharedOption> options = new();
        public List<SharedBallot> ballots = new();
        public bool rolling;
        public int round = 1;
        public string winner;
        public string ownerId;
        public string characterId;
        public List<LevelUpStatChoice> growthChoices;
    }

    private int sharedDecisionId;

    public void ChooseExpeditionOption(string optionId) => SendExpeditionCommand("choose", optionId);
    public void RollExpeditionDice(string characterId) => SendExpeditionCommand("roll", null, characterId);

    private void CreateSharedDecision(string kind, string title, List<SharedOption> options, int choiceIndex = -1)
    {
        serverExpedition.decision = new SharedDecision
        {
            id = ++sharedDecisionId, kind = kind, title = title, options = options, choiceIndex = choiceIndex,
            ballots = serverExpedition.party.Where(slot => serverExpedition.friendly == null ||
                (members.Values.Any(m => m.profileId == slot.ownerId) && (kind != "loot" ||
                serverExpedition.friendly.participants.Exists(p => p.ownerId == slot.ownerId && p.team == serverExpedition.friendly.winningTeam)))).Select(slot => new SharedBallot
            { characterId = slot.character.id, ownerId = slot.ownerId }).ToList()
        };
        serverExpedition.text = "선택지를 누르세요. 의견이 같으면 진행하고, 다르면 각 캐릭터가 주사위를 굴립니다.";
        PublishExpedition();
    }

    private void BeginRouteDecision()
    {
        serverExpedition.phase = ExpeditionPhase.Map;
        var node = QuestManager.Instance.GetCurrentRouteNode();
        var options = serverExpedition.runtime.routeNodes.Where(next => node.nextNodeIds.Contains(next.id))
            .Select(next => new SharedOption { id = next.id.ToString(), label = $"{next.depth}단계 · {next.column + 1}번 경로 — {RouteNodeLabel(next.type)}" }).ToList();
        CreateSharedDecision("route", "다음 목적지 선택", options);
    }

    private static string RouteNodeLabel(QuestRouteNodeType type) => type switch
    {
        QuestRouteNodeType.Rest => "안전 구역",
        QuestRouteNodeType.RandomEncounter => "인카운트",
        QuestRouteNodeType.Elite => "정예 전투",
        QuestRouteNodeType.Boss => "보스 전투",
        _ => "전투"
    };

    private void BeginEncounterDecision()
    {
        serverExpedition.phase = ExpeditionPhase.Encounter;
        var encounter = QuestManager.Instance.GetOrAssignCurrentEncounter();
        var options = new List<SharedOption>();
        string title = "주변 탐색";
        if (encounter != null)
        {
            bool insight = QuestManager.Instance.ResolveCurrentEncounterInsight(encounter);
            title = encounter.title + "\n" + encounter.description;
            if (insight && encounter.insight != null) title += $"\n간파 ({QuestManager.Instance.GetCurrentRouteNode().encounterInsightCharacterName}): " + encounter.insight.revealedText;
            for (int i = 0; i < encounter.choices.Count; i++)
            {
                var choice = encounter.choices[i];
                if (choice == null || (choice.requiresCharacter && !GameManager.Instance.GetAllCharacters().Any(cm =>
                    cm != null && cm.character.IsMine && QuestEncounterResolver.CanSelectCharacter(choice, cm)))) continue;
                options.Add(new SharedOption { id = i.ToString(), label = choice.text });
            }
        }
        if (options.Count == 0) options.Add(new SharedOption { id = "continue", label = "계속" });
        CreateSharedDecision("encounter", title, options);
    }

    private void HandleDecisionCommand(string ownerId, ExpeditionCommand command)
    {
        var decision = serverExpedition.decision;
        if (decision == null || decision.id != command.revision || decision.winner != null) return;
        if (decision.kind == "growth")
        {
            if (command.action != "choose" || ownerId != decision.ownerId ||
                !int.TryParse(command.value, out int index) || index < 0 || index >= decision.growthChoices.Count) return;
            var manager = FindBattleCharacter(decision.characterId);
            if (!LevelGrowthUtility.ApplyPendingChoice(manager, decision.growthChoices[index])) return;
            CaptureExpeditionCharacters();
            BeginSharedNodeRewards();
            return;
        }
        if (command.action == "choose")
        {
            if (decision.rolling || !decision.options.Exists(option => option.id == command.value)) return;
            foreach (var ballot in decision.ballots.Where(ballot => ballot.ownerId == ownerId)) ballot.optionId = command.value;
            if (decision.kind == "loot")
            {
                if (decision.ballots.All(ballot => ballot.optionId != null))
                {
                    foreach (var ballot in decision.ballots) ballot.eligible = ballot.optionId == "need";
                    var bidders = decision.ballots.Where(ballot => ballot.eligible).ToList();
                    if (bidders.Count == 0) { decision.winner = "pass"; decisionDeadline = Time.realtimeSinceStartup + 0.8f; }
                    else if (bidders.Count == 1) { decision.winner = bidders[0].characterId; decisionDeadline = Time.realtimeSinceStartup + 0.8f; }
                    else { decision.rolling = true; serverExpedition.text = "입찰한 용병마다 주사위를 굴리세요. 최고값의 용병 소유자가 획득합니다."; }
                }
                PublishExpedition();
                return;
            }
            if (decision.ballots.All(ballot => ballot.optionId != null))
            {
                if (decision.ballots.Select(ballot => ballot.optionId).Distinct().Count() == 1)
                {
                    decision.winner = decision.ballots[0].optionId;
                    decisionDeadline = Time.realtimeSinceStartup + 0.8f;
                    serverExpedition.text = "모두 같은 선택을 했습니다.";
                }
                else
                {
                    decision.rolling = true;
                    serverExpedition.text = "선택이 갈렸습니다. 각 캐릭터 옆의 주사위를 굴리세요. (1~100)";
                }
            }
        }
        else if (command.action == "roll")
        {
            var ballot = decision.ballots.Find(candidate => candidate.characterId == command.characterId && candidate.ownerId == ownerId);
            if (!decision.rolling || ballot == null || !ballot.eligible || ballot.roll != 0) return;
            ballot.roll = UnityEngine.Random.Range(1, 101);
            serverExpedition.diceSequence++;
            if (decision.ballots.Where(candidate => candidate.eligible).All(candidate => candidate.roll > 0))
            {
                int highest = decision.ballots.Where(candidate => candidate.eligible).Max(candidate => candidate.roll);
                var tied = decision.ballots.Where(candidate => candidate.eligible && candidate.roll == highest).ToList();
                if (tied.Count > 1)
                {
                    foreach (var candidate in decision.ballots)
                    {
                        candidate.eligible = tied.Contains(candidate);
                        if (!candidate.eligible) continue;
                        candidate.previousRoll = candidate.roll;
                        candidate.roll = 0;
                    }
                    decision.round++;
                    decision.id = ++sharedDecisionId;
                    serverExpedition.text = $"최고값 {highest} 동점! 동점 캐릭터만 다시 굴리세요. ({decision.round}차 굴림)";
                }
                else
                {
                    decision.winner = decision.kind == "loot" ? tied[0].characterId : tied[0].optionId;
                    var character = serverExpedition.party.Find(slot => slot.character.id == tied[0].characterId).character;
                    serverExpedition.text = $"{character.name}: {highest} — " + (decision.kind == "loot" ? "아이템 획득" : decision.options.Find(option => option.id == decision.winner).label);
                    decisionDeadline = Time.realtimeSinceStartup + 2f;
                }
            }
        }
        else return;
        PublishExpedition();
    }

    private void FinishSharedDecision()
    {
        var decision = serverExpedition.decision;
        if (decision?.winner == null) return;
        switch (decision.kind)
        {
            case "loot":
                if (decision.winner != "pass")
                {
                    var winner = serverExpedition.party.Find(slot => slot.character.id == decision.winner);
                    serverExpedition.claimedLoot.Add(new SharedLoot { ownerId = winner.ownerId, item = serverExpedition.pendingLoot[0] });
                }
                serverExpedition.pendingLoot.RemoveAt(0);
                BeginSharedNodeRewards();
                break;
            case "battleContinue":
                if (!serverExpedition.questCompleted) BeginRouteDecision();
                else CompleteSharedExpedition();
                break;
            case "friendlyComplete":
                CompleteSharedExpedition();
                break;
            case "route":
                if (!int.TryParse(decision.winner, out int nodeId) || !QuestManager.Instance.SelectRouteNode(nodeId)) return;
                serverExpedition.phase = ExpeditionPhase.LoadingNode;
                serverExpedition.decision = null;
                expeditionAcks.Clear();
                expeditionDeadline = Time.realtimeSinceStartup + 90f;
                serverExpedition.text = "선택한 목적지로 함께 이동하고 있습니다.";
                PublishExpedition();
                break;
            case "encounter":
                if (decision.winner == "continue") { ResolveSharedEncounter(-1, null); break; }
                int choiceIndex = int.Parse(decision.winner);
                var choice = QuestManager.Instance.GetOrAssignCurrentEncounter().choices[choiceIndex];
                if (!choice.requiresCharacter) { ResolveSharedEncounter(choiceIndex, null); break; }
                var candidates = GameManager.Instance.GetAllCharacters().Where(cm => cm != null && cm.character.IsMine &&
                    QuestEncounterResolver.CanSelectCharacter(choice, cm)).Select(cm => new SharedOption
                    { id = cm.character.ID, label = cm.character.Name }).ToList();
                if (candidates.Count == 0) { BeginEncounterDecision(); break; }
                CreateSharedDecision("actor", $"{choice.text.TrimEnd('.', ' ', '\n')}.\n캐릭터를 선택하세요.", candidates, choiceIndex);
                break;
            case "actor":
                var actor = GameManager.Instance.GetAllCharacters().Find(cm => cm != null && cm.character.ID == decision.winner);
                var selectedChoice = QuestManager.Instance.GetOrAssignCurrentEncounter().choices[decision.choiceIndex];
                if (actor == null || !QuestEncounterResolver.CanSelectCharacter(selectedChoice, actor)) { BeginEncounterDecision(); break; }
                ResolveSharedEncounter(decision.choiceIndex, actor);
                break;
            case "continue":
                serverExpedition.decision = null;
                if (sharedResolution != null && sharedResolution.startsBattle)
                {
                    QuestManager.Instance.BeginCurrentEncounterBattle(sharedResolution.monsterRoleIds);
                    GameManager.Instance.StartEncounterBattle(sharedResolution.monsterRoleIds);
                    if (serverExpedition.phase != ExpeditionPhase.Battle)
                        AbortExpedition("인카운트 전투 데이터를 불러올 수 없습니다.");
                }
                else
                {
                    QuestManager.Instance.ResolveCurrentEncounterNode();
                    serverExpedition.questCompleted = QuestManager.Instance.active == null;
                    CaptureExpeditionCharacters();
                    serverExpedition.afterRewards = "battleContinue";
                    BeginSharedNodeRewards();
                }
                break;
        }
    }

    private void ResolveSharedEncounter(int choiceIndex, CharacterManager actor)
    {
        var player = PlayerManager.Instance.GetCurrentPlayerData();
        var inventory = player.expeditionStorage;
        player.expeditionStorage = new List<InventorySlotData>();
        try
        {
        sharedResolution = choiceIndex < 0
            ? new QuestEncounterResolution { message = "특별한 일은 일어나지 않았습니다." }
            : QuestEncounterResolver.Resolve(QuestManager.Instance.GetOrAssignCurrentEncounter().choices[choiceIndex], actor);
            serverExpedition.pendingLoot.AddRange(player.expeditionStorage);
        }
        finally { player.expeditionStorage = inventory; }
        CaptureExpeditionCharacters();
        serverExpedition.phase = ExpeditionPhase.Result;
        serverExpedition.resolution = sharedResolution;
        serverExpedition.afterRewards = "continue";
        serverExpedition.rewardSummary = sharedResolution.message;
        BeginSharedNodeRewards();
    }
}
