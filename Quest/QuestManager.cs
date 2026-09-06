using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestStatus
{
    Board,
    Reserved,
    Active,
    CompletedPendingReward,
    Rewarded
}

public enum QuestIssuer
{
    HunterGuild = 0,
    WarriorGuild = 1,
    SwordDojo = 2,
    MageTower = 3,
    TownCouncil = 4,
    ThievesGuild = 5
}

public enum QuestDifficulty
{
    Easy,
    Normal,
    Hard
}


public enum QuestRewardKind
{
    None,
    Equipment,
    Skill
}


public enum QuestEquipmentRewardFilter
{
    None,
    Any,
    WeaponType,
    ArmorCategory,
    EquipmentType
}


[Serializable]
public class QuestReward
{
    // 퀘스트 완료 시 실제 보상을 결정하는 시드
    public int randomSeed;

    public QuestRewardKind kind;
    public int tier;
    public int goldAmount;
    public bool goldClaimed;
    public bool itemsClaimed;

    public EquipmentRarity maxEquipmentRarity = EquipmentRarity.Common;
    public QuestEquipmentRewardFilter equipmentFilter;
    public WeaponType weaponType;
    public ArmorCategory armorCategory;
    public EquipmentType equipmentType;

    public SkillDiscipline skillDiscipline;

    // 완료 시 한 번만 실제 보상을 결정했는지 여부
    public bool rolled;

    // EquipmentDefinitionSO UID
    public List<int> equipmentUids = new();
    public List<EquipmentRarity> equipmentRarities = new();

    // SkillDefinitionSO UID
    public List<int> skillUids = new();
}


[Serializable]
public class MonsterSpawn
{
    // MonsterRoleSO.id
    public int monsterUid;

    public int count;
}


[Serializable]
public enum QuestRouteNodeType
{
    Start = 0,
    Battle = 1,
    Boss = 2,
    Elite = 3,
    RandomEncounter = 4,
    Rest = 5
}


[Serializable]
public class QuestRouteNode
{
    public int id;
    public int depth;
    public int column;
    public QuestRouteNodeType type;
    public int mapPrefabIndex = -1;
    public List<int> nextNodeIds = new();
    public bool cleared;
    public string encounterId;
    public bool encounterResolved;
    public List<int> encounterMonsterRoleIds = new();
    public bool encounterInsightRolled;
    public bool encounterInsightSucceeded;
}


[Serializable]
public class QuestEncounterRuntimeEffect
{
    public string characterId;
    public string effectName;
    public int remainingRooms;
    public int sourceNodeId;
    public CharacterStats statModifiers = new();
}


[Serializable]
public class QuestDef
{
    public string id;

    // 원정대 전멸 후 한 번만 발행되는 구출 의뢰 정보
    public bool isRescueQuest;
    public string sourceQuestId;
    public List<string> rescueCharacterIds = new();

    // 생성 시 사용한 랜덤 시드
    public int seed;

    // 적과 장비 보상의 기준 Tier
    public int tier;

    public QuestDifficulty difficulty;

    public string stageKey;

    // QuestStageDefinitionSO.mapPrefabs에서 선택된 맵 인덱스
    public int mapPrefabIndex = -1;

    public QuestIssuer issuer;

    // MonsterRoleSO.id
    public int bossUid;

    public string title;

    public int recommendedLevel;
    public int recommendedPartySize;
    public int maxPartySize;

    public List<MonsterSpawn> enemyPool = new();

    public QuestReward reward =
        new QuestReward();
}


[Serializable]
public class QuestBoardEntry
{
    public QuestDef def;

    public bool reserved;

    public QuestStatus status;
}


[Serializable]
public class ActiveQuestRuntime
{
    public QuestDef def;

    public QuestStatus status;

    public string leaderCharacterId;

    public List<string> partyCharacterIds =
        new();

    public string stageKey;

    public int stageNodeIndex;

    public int currentRouteNodeId;

    public List<QuestRouteNode> routeNodes =
        new();

    public List<QuestEncounterRuntimeEffect> encounterEffects =
        new();

    public bool retreated;
}


[Serializable]
public class CompletedQuestEntry
{
    public QuestDef def;

    public QuestStatus status;
}


public class QuestManager : MonoBehaviour
{
    public const int BoardQuestCount = 12;
    public const int BoardPageSize = 6;
    public const int MaxPreferredIssuerCount = 2;
    public const int PreferredIssuerWeight = 3;

    private static readonly QuestIssuer[] GeneratedIssuers =
    {
        QuestIssuer.WarriorGuild,
        QuestIssuer.SwordDojo,
        QuestIssuer.HunterGuild,
        QuestIssuer.MageTower,
        QuestIssuer.TownCouncil
    };

    public static QuestManager Instance
    {
        get;
        private set;
    }


    [Header("Quest Tier")]
    [SerializeField]
    [Min(1)]
    private int minQuestTier = 1;

    [SerializeField]
    [Min(1)]
    private int maxQuestTier = 5;

    [SerializeField]
    [Min(1)]
    private int maxUnlockedLevel = 10;


    [Header("Reward")]
    [SerializeField]
    [Range(0, 100)]
    private int equipmentRewardChance = 50;

    [SerializeField]
    [Min(1)]
    private int maxRewardSkillCost = 5;


    [Header("Enemy")]
    [SerializeField]
    [Range(0, 100)]
    private int eliteSpawnChance = 20;


    public List<QuestBoardEntry> board =
        new();

    public List<QuestIssuer> preferredIssuers =
        new();

    public List<int> preferredQuestTiers =
        new();

    public ActiveQuestRuntime active;

    public List<CompletedQuestEntry> completed =
        new();


    public event Action OnBoardChanged;
    public event Action OnPreferencesChanged;
    public event Action OnActiveChanged;
    public event Action OnCompletedChanged;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    #region Board

    public void GenerateBoardIfEmpty(
        int targetCount = BoardQuestCount)
    {
        if (board == null)
        {
            board =
                new List<QuestBoardEntry>();
        }


        foreach (QuestBoardEntry entry in board)
        {
            if (entry == null || entry.def == null)
                continue;

            entry.def.maxPartySize = 4;

            if (entry.def.issuer == QuestIssuer.ThievesGuild)
            {
                entry.def.issuer = QuestIssuer.HunterGuild;
            }
        }

        if (board.Count >= targetCount)
            return;


        while (board.Count < targetCount)
        {
            QuestDef quest =
                GenerateOneQuest(board.Count);

            if (quest == null)
                break;

            board.Add(
                new QuestBoardEntry
                {
                    def = quest,

                    reserved = false,

                    status =
                        QuestStatus.Board
                });
        }


        OnBoardChanged?.Invoke();
    }


    public void RefreshBoard(
        bool preserveReserved = true,
        int targetCount = BoardQuestCount)
    {
        if (board == null)
        {
            board =
                new List<QuestBoardEntry>();
        }


        List<QuestBoardEntry> keep =
            new List<QuestBoardEntry>();


        foreach (QuestBoardEntry entry in board)
        {
            if (entry == null || entry.def == null)
                continue;

            if (entry.def.isRescueQuest)
                keep.Add(entry);
        }


        if (preserveReserved)
        {
            foreach (QuestBoardEntry entry in board)
            {
                if (entry == null ||
                    entry.def == null ||
                    entry.def.isRescueQuest)
                {
                    continue;
                }

                if (entry.status == QuestStatus.Reserved)
                    keep.Add(entry);
            }
        }


        board.Clear();
        board.AddRange(keep);


        while (board.Count < targetCount)
        {
            QuestDef quest =
                GenerateOneQuest(board.Count);

            if (quest == null)
                break;

            board.Add(
                new QuestBoardEntry
                {
                    def = quest,

                    reserved = false,

                    status =
                        QuestStatus.Board
                });
        }


        OnBoardChanged?.Invoke();
    }


    public void SetBoardPreferences(
        List<QuestIssuer> issuers,
        List<int> questTiers)
    {
        if (preferredIssuers == null)
            preferredIssuers = new List<QuestIssuer>();

        if (preferredQuestTiers == null)
            preferredQuestTiers = new List<int>();

        preferredIssuers.Clear();


        if (issuers != null)
        {
            foreach (QuestIssuer issuer in issuers)
            {
                if (preferredIssuers.Count >= MaxPreferredIssuerCount)
                    break;

                if (!IsGeneratedIssuer(issuer) ||
                    preferredIssuers.Contains(issuer))
                {
                    continue;
                }

                preferredIssuers.Add(issuer);
            }
        }


        preferredQuestTiers.Clear();


        if (questTiers != null)
        {
            foreach (int tier in questTiers)
            {
                if (tier < minQuestTier ||
                    tier > maxQuestTier ||
                    preferredQuestTiers.Contains(tier))
                {
                    continue;
                }

                preferredQuestTiers.Add(tier);
            }
        }


        OnPreferencesChanged?.Invoke();
    }


    public int GetMaximumAvailableQuestTier()
    {
        int unlockedLevel =
            Mathf.Max(
                maxUnlockedLevel,
                1);


        int unlockedTier =
            Mathf.Max(
                (unlockedLevel - 1) / 10 + 1,
                1);


        return Mathf.Clamp(
            maxQuestTier,
            minQuestTier,
            unlockedTier);
    }


    private bool IsGeneratedIssuer(QuestIssuer issuer)
    {
        foreach (QuestIssuer generatedIssuer in GeneratedIssuers)
        {
            if (generatedIssuer == issuer)
                return true;
        }


        return false;
    }

    #endregion


    #region Accept / Reserve / Complete

    public bool CanAccept()
    {
        return active == null;
    }


    public bool AbandonActive()
    {
        if (active == null)
            return false;


        ExpireActiveQuestEssences();

        active = null;


        OnActiveChanged?.Invoke();
        return true;
    }


    public bool IssueRescueQuest(
        QuestDef failedQuest,
        List<string> missingCharacterIds)
    {
        if (failedQuest == null ||
            failedQuest.isRescueQuest ||
            missingCharacterIds == null ||
            missingCharacterIds.Count == 0)
        {
            return false;
        }

        if (board == null)
            board = new List<QuestBoardEntry>();

        foreach (QuestBoardEntry entry in board)
        {
            if (entry?.def == null || !entry.def.isRescueQuest)
                continue;

            if (entry.def.sourceQuestId == failedQuest.id)
                return false;
        }

        QuestDef rescueQuest = CloneAsRescueQuest(
            failedQuest,
            missingCharacterIds);

        board.Insert(
            0,
            new QuestBoardEntry
            {
                def = rescueQuest,
                reserved = false,
                status = QuestStatus.Board
            });

        while (board.Count > BoardQuestCount)
        {
            int removeIndex = -1;

            for (int i = board.Count - 1; i > 0; i--)
            {
                QuestBoardEntry entry = board[i];

                if (entry != null &&
                    entry.def != null &&
                    !entry.def.isRescueQuest &&
                    entry.status != QuestStatus.Reserved)
                {
                    removeIndex = i;
                    break;
                }
            }

            if (removeIndex < 0)
            {
                for (int i = board.Count - 1; i > 0; i--)
                {
                    if (board[i]?.def != null &&
                        !board[i].def.isRescueQuest)
                    {
                        removeIndex = i;
                        break;
                    }
                }
            }

            if (removeIndex < 0)
                break;

            board.RemoveAt(removeIndex);
        }

        OnBoardChanged?.Invoke();
        return true;
    }


    public bool Accept(
        string questId,
        string leaderId,
        List<string> partyIds)
    {
        if (!CanAccept() || partyIds == null || partyIds.Count == 0 || partyIds.Count > 4)
            return false;


        QuestBoardEntry entry =
            board.Find(
                b =>
                    b != null &&
                    b.def != null &&
                    b.def.id == questId &&
                    (
                        b.status ==
                        QuestStatus.Board ||

                        b.status ==
                        QuestStatus.Reserved
                    ));


        if (entry == null)
            return false;


        ExpirePendingRescueQuests(entry.def.id);


        active =
            new ActiveQuestRuntime
            {
                def =
                    entry.def,

                status =
                    QuestStatus.Active,

                leaderCharacterId =
                    leaderId,

                partyCharacterIds =
                    partyIds != null
                        ? new List<string>(partyIds)
                        : new List<string>(),

                stageKey =
                    entry.def.stageKey,

                stageNodeIndex =
                    0,

                currentRouteNodeId =
                    0,

                retreated =
                    false
            };


        BuildRoute(active);


        board.Remove(entry);


        OnBoardChanged?.Invoke();
        OnActiveChanged?.Invoke();


        return true;
    }


    private void ExpirePendingRescueQuests(string acceptedQuestId)
    {
        if (board == null)
            return;

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;
        int expiredQuestCount = 0;
        int lostCharacterCount = 0;

        for (int i = board.Count - 1; i >= 0; i--)
        {
            QuestBoardEntry rescueEntry = board[i];
            QuestDef rescueQuest = rescueEntry != null
                ? rescueEntry.def
                : null;

            if (rescueQuest == null ||
                !rescueQuest.isRescueQuest ||
                rescueQuest.id == acceptedQuestId)
            {
                continue;
            }

            if (playerData != null && rescueQuest.rescueCharacterIds != null)
            {
                foreach (string characterId in rescueQuest.rescueCharacterIds)
                {
                    if (string.IsNullOrEmpty(characterId))
                        continue;

                    playerData.characterIds?.RemoveAll(id => id == characterId);
                    playerData.activeCharacterIds?.RemoveAll(id => id == characterId);
                    playerData.missingCharacterIds?.RemoveAll(id => id == characterId);
                    playerData.revivalRequiredCharacterIds?.RemoveAll(id => id == characterId);
                    playerData.RemovePosition(characterId);
                    lostCharacterCount++;
                }
            }

            SharedInventoryUtility.DiscardDefeatedExpedition(
                playerData,
                rescueQuest.sourceQuestId);
            board.RemoveAt(i);
            expiredQuestCount++;
        }

        if (expiredQuestCount > 0)
        {
            Debug.Log(
                $"다른 의뢰 출발로 구출 의뢰 {expiredQuestCount}건이 만료되고 " +
                $"실종 원정대원 {lostCharacterCount}명이 영구 사망 처리되었습니다.");
        }
    }


    public QuestRouteNode GetCurrentRouteNode()
    {
        if (active == null || active.routeNodes == null)
            return null;

        return active.routeNodes.Find(node => node != null && node.id == active.currentRouteNodeId);
    }


    public void EnsureActiveRoute()
    {
        if (active == null || active.def == null)
            return;

        if (active.routeNodes == null || active.routeNodes.Count == 0)
            BuildRoute(active);
    }


    public bool CanSelectRouteNode(int nodeId)
    {
        if (active == null || active.status != QuestStatus.Active || active.routeNodes == null)
            return false;

        QuestRouteNode current = GetCurrentRouteNode();

        if (current == null || !current.cleared || current.nextNodeIds == null)
            return false;

        return current.nextNodeIds.Contains(nodeId);
    }


    public bool SelectRouteNode(int nodeId)
    {
        if (!CanSelectRouteNode(nodeId))
            return false;

        QuestRouteNode selected = active.routeNodes.Find(node => node != null && node.id == nodeId);

        if (selected == null)
            return false;

        active.currentRouteNodeId = selected.id;
        active.stageNodeIndex = selected.depth;
        OnActiveChanged?.Invoke();
        return true;
    }


    public QuestEncounterDefinitionSO GetOrAssignCurrentEncounter()
    {
        QuestRouteNode current = GetCurrentRouteNode();

        if (active == null || active.def == null || current == null ||
            (current.type != QuestRouteNodeType.RandomEncounter &&
             current.type != QuestRouteNodeType.Rest) ||
            GameDataRegistry.Instance == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(current.encounterId))
            return GameDataRegistry.Instance.GetQuestEncounter(current.encounterId);

        List<QuestEncounterDefinitionSO> candidates =
            GameDataRegistry.Instance.GetQuestEncounters(active.stageKey, current.type);

        int totalWeight = 0;

        foreach (QuestEncounterDefinitionSO candidate in candidates)
        {
            if (candidate != null && candidate.weight > 0)
                totalWeight += candidate.weight;
        }

        if (totalWeight <= 0)
            return null;

        int seed = active.def.seed ^ (current.id * 397) ^ ((int)current.type * 7919);
        System.Random random = new System.Random(seed);
        int roll = random.Next(0, totalWeight);
        int currentWeight = 0;

        foreach (QuestEncounterDefinitionSO candidate in candidates)
        {
            if (candidate == null || candidate.weight <= 0)
                continue;

            currentWeight += candidate.weight;

            if (roll < currentWeight)
            {
                current.encounterId = candidate.encounterId;
                OnActiveChanged?.Invoke();
                return candidate;
            }
        }

        return null;
    }


    public bool ResolveCurrentEncounterInsight(QuestEncounterDefinitionSO encounter)
    {
        QuestRouteNode current = GetCurrentRouteNode();

        if (active == null || active.def == null || current == null ||
            encounter == null || encounter.insight == null ||
            !encounter.insight.enabled)
        {
            return false;
        }

        if (current.encounterInsightRolled)
            return current.encounterInsightSucceeded;

        current.encounterInsightRolled = true;
        int bestDetection = 0;
        int bestInsight = 0;

        if (GameManager.Instance != null)
        {
            foreach (CharacterManager manager in GameManager.Instance.GetAllCharacters())
            {
                if (manager == null || manager.character == null ||
                    !manager.character.IsMine || manager.character.FinalStats == null)
                {
                    continue;
                }

                if (current.type == QuestRouteNodeType.RandomEncounter &&
                    ProgressionRules.IsOverpoweredForQuest(manager.character, active.def))
                {
                    continue;
                }

                bestDetection = Mathf.Max(bestDetection, manager.character.FinalStats.Detection);
                bestInsight = Mathf.Max(bestInsight, manager.character.FinalStats.Insight);
            }
        }

        int tier = Mathf.Max(1, active.def.tier);
        int difficultyStep = (int)active.def.difficulty;
        float chance = encounter.insight.baseChancePercent +
                       bestDetection * encounter.insight.chancePerDetection +
                       bestInsight * encounter.insight.chancePerInsight -
                       (tier - 1) * encounter.insight.tierPenaltyPercent -
                       difficultyStep * encounter.insight.difficultyPenaltyPercent;
        chance = Mathf.Clamp(chance, 0f, encounter.insight.maxChancePercent);
        current.encounterInsightSucceeded = UnityEngine.Random.Range(0f, 100f) < chance;
        OnActiveChanged?.Invoke();
        return current.encounterInsightSucceeded;
    }


    public void AddEncounterEffect(QuestEncounterRuntimeEffect effect)
    {
        if (active == null || effect == null || string.IsNullOrEmpty(effect.characterId))
            return;

        if (active.encounterEffects == null)
            active.encounterEffects = new List<QuestEncounterRuntimeEffect>();

        effect.remainingRooms = Mathf.Max(1, effect.remainingRooms);
        active.encounterEffects.Add(effect);
        OnActiveChanged?.Invoke();
    }


    public CharacterStats GetEncounterStatModifiers(string characterId)
    {
        CharacterStats result = new CharacterStats();

        if (active == null || active.encounterEffects == null || string.IsNullOrEmpty(characterId))
            return result;

        foreach (QuestEncounterRuntimeEffect effect in active.encounterEffects)
        {
            if (effect == null || effect.characterId != characterId ||
                effect.remainingRooms <= 0 || effect.statModifiers == null)
            {
                continue;
            }

            result += effect.statModifiers;
        }

        return result;
    }


    public bool ResolveCurrentEncounterNode()
    {
        QuestRouteNode current = GetCurrentRouteNode();

        if (current == null ||
            (current.type != QuestRouteNodeType.RandomEncounter &&
             current.type != QuestRouteNodeType.Rest))
        {
            return false;
        }

        current.encounterResolved = true;
        return CompleteCurrentRouteNode();
    }


    public void BeginCurrentEncounterBattle(List<int> monsterRoleIds)
    {
        QuestRouteNode current = GetCurrentRouteNode();

        if (current == null || current.type != QuestRouteNodeType.RandomEncounter)
            return;

        current.encounterResolved = true;
        current.encounterMonsterRoleIds = monsterRoleIds != null
            ? new List<int>(monsterRoleIds)
            : new List<int>();
        OnActiveChanged?.Invoke();
    }


    public bool CompleteCurrentRouteNode()
    {
        QuestRouteNode current = GetCurrentRouteNode();

        if (active == null || current == null || current.type == QuestRouteNodeType.Start)
            return false;

        AdvanceEncounterEffects(current.id);
        current.cleared = true;
        OnActiveChanged?.Invoke();

        if (current.type == QuestRouteNodeType.Boss ||
            current.nextNodeIds == null ||
            current.nextNodeIds.Count == 0)
        {
            CompleteActive();
            return false;
        }

        return true;
    }


    private void AdvanceEncounterEffects(int completedNodeId)
    {
        if (active == null || active.encounterEffects == null)
            return;

        for (int i = active.encounterEffects.Count - 1; i >= 0; i--)
        {
            QuestEncounterRuntimeEffect effect = active.encounterEffects[i];

            if (effect == null)
            {
                active.encounterEffects.RemoveAt(i);
                continue;
            }

            if (effect.sourceNodeId == completedNodeId)
            {
                effect.sourceNodeId = -1;
                continue;
            }

            effect.remainingRooms--;

            if (effect.remainingRooms > 0)
                continue;

            active.encounterEffects.RemoveAt(i);

            CharacterManager manager = GameManager.Instance != null
                ? GameManager.Instance.GetAllCharacters().Find(characterManager =>
                    characterManager != null &&
                    characterManager.character != null &&
                    characterManager.character.ID == effect.characterId)
                : null;

            if (manager != null)
            {
                manager.character.UpdateFinalStats();
                manager.UpdateCharacterUI();
            }
        }
    }


    private void BuildRoute(ActiveQuestRuntime runtime)
    {
        if (runtime == null || runtime.def == null)
            return;

        runtime.routeNodes = new List<QuestRouteNode>();

        System.Random random = new System.Random(runtime.def.seed ^ 0x51A7E);
        QuestStageDefinitionSO stage = GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetQuestStage(runtime.def.stageKey)
            : null;
        int mapCount = stage != null && stage.mapPrefabs != null
            ? stage.mapPrefabs.Count
            : 0;
        int baseEncounterDepth = runtime.def.difficulty switch
        {
            QuestDifficulty.Easy => random.Next(5, 7),
            QuestDifficulty.Normal => random.Next(6, 8),
            QuestDifficulty.Hard => random.Next(7, 9),
            _ => random.Next(5, 7)
        };
        int encounterDepth = baseEncounterDepth +
                             Mathf.Max(0, Mathf.Clamp(runtime.def.recommendedPartySize, 1, 4) - 1);
        int guaranteedRestCount = Mathf.Max(1, encounterDepth / 5);
        HashSet<int> guaranteedRestDepths = new HashSet<int>();

        for (int i = 1; i <= guaranteedRestCount; i++)
        {
            int restDepth = Mathf.RoundToInt(i * (encounterDepth + 1f) / (guaranteedRestCount + 1f));
            guaranteedRestDepths.Add(Mathf.Clamp(restDepth, 3, encounterDepth));
        }

        int nextId = 0;

        QuestRouteNode start = new QuestRouteNode
        {
            id = nextId++,
            depth = 0,
            column = 0,
            type = QuestRouteNodeType.Start,
            mapPrefabIndex = -1,
            cleared = true
        };

        runtime.routeNodes.Add(start);
        List<QuestRouteNode> previousLayer = new List<QuestRouteNode> { start };
        bool previousLayerHasElite = false;
        int lastRestDepth = -1000;

        for (int depth = 1; depth <= encounterDepth; depth++)
        {
            int nodeCount = random.Next(2, 4);
            List<QuestRouteNode> layer = new List<QuestRouteNode>();
            bool guaranteedRest = guaranteedRestDepths.Contains(depth);
            bool layerHasElite = false;
            bool layerHasRest = false;
            bool nearGuaranteedRest = false;

            if (!guaranteedRest)
            {
                foreach (int guaranteedRestDepth in guaranteedRestDepths)
                {
                    if (Mathf.Abs(guaranteedRestDepth - depth) <= 2)
                    {
                        nearGuaranteedRest = true;
                        break;
                    }
                }
            }

            bool allowRest = depth > 2 &&
                             depth - lastRestDepth > 2 &&
                             !nearGuaranteedRest;

            for (int column = 0; column < nodeCount; column++)
            {
                QuestRouteNodeType nodeType = guaranteedRest
                    ? QuestRouteNodeType.Rest
                    : PickRouteNodeType(random, allowRest, !previousLayerHasElite);

                QuestRouteNode node = new QuestRouteNode
                {
                    id = nextId++,
                    depth = depth,
                    column = column,
                    type = nodeType,
                    mapPrefabIndex = PickRouteMapIndex(runtime.def, mapCount, random)
                };

                layerHasElite |= nodeType == QuestRouteNodeType.Elite;
                layerHasRest |= nodeType == QuestRouteNodeType.Rest;
                runtime.routeNodes.Add(node);
                layer.Add(node);
            }

            ConnectRouteLayers(previousLayer, layer, random);
            previousLayer = layer;
            previousLayerHasElite = layerHasElite;

            if (layerHasRest)
                lastRestDepth = depth;
        }

        QuestRouteNode boss = new QuestRouteNode
        {
            id = nextId,
            depth = encounterDepth + 1,
            column = 0,
            type = QuestRouteNodeType.Boss,
            mapPrefabIndex = PickRouteMapIndex(runtime.def, mapCount, random)
        };

        runtime.routeNodes.Add(boss);
        ConnectRouteLayers(previousLayer, new List<QuestRouteNode> { boss }, random);
        runtime.currentRouteNodeId = start.id;
        runtime.stageNodeIndex = 0;
    }


    private static QuestRouteNodeType PickRouteNodeType(
        System.Random random,
        bool allowRest,
        bool allowElite)
    {
        QuestRouteNodeType type;

        do
        {
            int roll = random.Next(0, 100);

            if (roll < 15)
                type = QuestRouteNodeType.Rest;
            else if (roll < 30)
                type = QuestRouteNodeType.RandomEncounter;
            else if (roll < 50)
                type = QuestRouteNodeType.Elite;
            else
                type = QuestRouteNodeType.Battle;
        }
        while ((!allowRest && type == QuestRouteNodeType.Rest) ||
               (!allowElite && type == QuestRouteNodeType.Elite));

        return type;
    }


    private static int PickRouteMapIndex(QuestDef quest, int mapCount, System.Random random)
    {
        if (mapCount <= 0)
            return quest.mapPrefabIndex;

        return random.Next(0, mapCount);
    }


    private static void ConnectRouteLayers(
        List<QuestRouteNode> previousLayer,
        List<QuestRouteNode> nextLayer,
        System.Random random)
    {
        if (previousLayer == null || previousLayer.Count == 0 ||
            nextLayer == null || nextLayer.Count == 0)
        {
            return;
        }

        if (previousLayer.Count == 1 && previousLayer[0].type == QuestRouteNodeType.Start)
        {
            foreach (QuestRouteNode next in nextLayer)
                AddRouteConnection(previousLayer[0], next.id);

            return;
        }

        for (int i = 0; i < previousLayer.Count; i++)
        {
            QuestRouteNode previous = previousLayer[i];
            int primaryIndex = previousLayer.Count == 1
                ? random.Next(0, nextLayer.Count)
                : Mathf.RoundToInt(i * (nextLayer.Count - 1f) / Mathf.Max(1, previousLayer.Count - 1));

            AddRouteConnection(previous, nextLayer[primaryIndex].id);

            if (nextLayer.Count > 1 && random.NextDouble() < 0.5)
            {
                int secondaryIndex = Mathf.Clamp(
                    primaryIndex + (random.Next(0, 2) == 0 ? -1 : 1),
                    0,
                    nextLayer.Count - 1);
                AddRouteConnection(previous, nextLayer[secondaryIndex].id);
            }
        }

        foreach (QuestRouteNode next in nextLayer)
        {
            bool hasIncoming = previousLayer.Exists(previous =>
                previous.nextNodeIds != null && previous.nextNodeIds.Contains(next.id));

            if (hasIncoming)
                continue;

            QuestRouteNode closest = previousLayer[Mathf.Clamp(next.column, 0, previousLayer.Count - 1)];
            AddRouteConnection(closest, next.id);
        }
    }


    private static void AddRouteConnection(QuestRouteNode node, int nextNodeId)
    {
        if (node.nextNodeIds == null)
            node.nextNodeIds = new List<int>();

        if (!node.nextNodeIds.Contains(nextNodeId))
            node.nextNodeIds.Add(nextNodeId);
    }


    public void ReserveToggle(
        string questId)
    {
        QuestBoardEntry entry =
            board.Find(
                b =>
                    b != null &&
                    b.def != null &&
                    b.def.id == questId);


        if (entry == null)
            return;


        if (entry.status ==
            QuestStatus.Board)
        {
            entry.status =
                QuestStatus.Reserved;

            entry.reserved =
                true;
        }
        else if (
            entry.status ==
            QuestStatus.Reserved)
        {
            entry.status =
                QuestStatus.Board;

            entry.reserved =
                false;
        }


        OnBoardChanged?.Invoke();
    }


    public void CompleteActive(
        bool rewardClaimed = false)
    {
        if (active == null)
            return;


        ReviveActivePartyMembers();
        ResolveReward(active.def);

        if (active.def != null && active.def.isRescueQuest)
        {
            ResolveRescuedCharacters(active.def);

            PlayerData playerData = PlayerManager.Instance != null
                ? PlayerManager.Instance.GetCurrentPlayerData()
                : null;
            int recoveredItemCount = SharedInventoryUtility.RecoverDefeatedExpedition(
                playerData,
                active.def.sourceQuestId);

            if (recoveredItemCount > 0)
                Debug.Log($"실종 원정대의 물품 {recoveredItemCount}개를 구조대 창고로 회수했습니다.");
        }


        ExpireActiveQuestEssences();

        completed.Add(
            new CompletedQuestEntry
            {
                def =
                    active.def,

                status =
                    rewardClaimed
                        ? QuestStatus.Rewarded
                        : QuestStatus.CompletedPendingReward
            });


        active = null;


        OnActiveChanged?.Invoke();
        OnCompletedChanged?.Invoke();
    }

    private void ReviveActivePartyMembers()
    {
        if (active?.partyCharacterIds == null)
            return;

        foreach (string characterId in active.partyCharacterIds)
        {
            if (string.IsNullOrEmpty(characterId))
                continue;

            CharacterManager battleCharacter = null;

            if (GameManager.Instance != null)
            {
                foreach (CharacterManager manager in GameManager.Instance.GetAllCharacters())
                {
                    if (manager != null &&
                        manager.character != null &&
                        manager.character.ID == characterId)
                    {
                        battleCharacter = manager;
                        break;
                    }
                }
            }

            CharacterManager pooledCharacter = CharacterPoolManager.Instance != null
                ? CharacterPoolManager.Instance.Get(characterId)
                : null;
            CharacterData character = battleCharacter != null
                ? battleCharacter.character
                : pooledCharacter != null
                    ? pooledCharacter.character
                    : null;

            if (character == null)
                continue;

            ReviveCharacter(character);
            RefreshRevivedCharacter(battleCharacter);

            if (pooledCharacter != null && pooledCharacter != battleCharacter)
            {
                ReviveCharacter(pooledCharacter.character);
                RefreshRevivedCharacter(pooledCharacter);
            }

            PlayerManager.Instance?.SaveCharacter(character);
        }
    }

    private static void ReviveCharacter(CharacterData character)
    {
        if (character == null)
            return;

        if (character.FinalStats == null)
            character.UpdateFinalStats();

        character.IsAlive = true;
        character.CurrentHp = character.FinalStats != null
            ? Mathf.Max(1, character.FinalStats.MaxHp)
            : 1;
    }

    private static void RefreshRevivedCharacter(CharacterManager manager)
    {
        if (manager == null)
            return;

        manager.battlePresentationHandler?.SetDeadState(false);
        manager.UpdateCharacterUI();
    }


    private void ExpireActiveQuestEssences()
    {
        if (active?.def == null)
            return;

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData == null)
            return;

        List<InventorySlotData> storage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.ExpeditionStorage);
        int expiredCount = SharedInventoryUtility.ExpireMonsterEssences(
            storage,
            active.def.id);

        if (expiredCount > 0)
            Debug.Log($"사용하지 않은 정수 {expiredCount}개가 빛을 잃었습니다.");
    }


    private QuestDef CloneAsRescueQuest(
        QuestDef source,
        List<string> characterIds)
    {
        List<MonsterSpawn> enemyPool = new List<MonsterSpawn>();

        if (source.enemyPool != null)
        {
            foreach (MonsterSpawn spawn in source.enemyPool)
            {
                if (spawn == null)
                    continue;

                enemyPool.Add(new MonsterSpawn
                {
                    monsterUid = spawn.monsterUid,
                    count = spawn.count
                });
            }
        }

        QuestReward sourceReward = source.reward;
        QuestReward reward = sourceReward != null
            ? new QuestReward
            {
                randomSeed = sourceReward.randomSeed,
                kind = sourceReward.kind,
                tier = sourceReward.tier,
                goldAmount = sourceReward.goldAmount,
                goldClaimed = sourceReward.goldClaimed,
                itemsClaimed = sourceReward.itemsClaimed,
                maxEquipmentRarity = sourceReward.maxEquipmentRarity,
                equipmentFilter = sourceReward.equipmentFilter,
                weaponType = sourceReward.weaponType,
                armorCategory = sourceReward.armorCategory,
                equipmentType = sourceReward.equipmentType,
                skillDiscipline = sourceReward.skillDiscipline,
                rolled = sourceReward.rolled,
                equipmentUids = sourceReward.equipmentUids != null
                    ? new List<int>(sourceReward.equipmentUids)
                    : new List<int>(),
                equipmentRarities = sourceReward.equipmentRarities != null
                    ? new List<EquipmentRarity>(sourceReward.equipmentRarities)
                    : new List<EquipmentRarity>(),
                skillUids = sourceReward.skillUids != null
                    ? new List<int>(sourceReward.skillUids)
                    : new List<int>()
            }
            : new QuestReward();

        List<string> rescueIds = new List<string>();

        foreach (string characterId in characterIds)
        {
            if (!string.IsNullOrEmpty(characterId) &&
                !rescueIds.Contains(characterId))
            {
                rescueIds.Add(characterId);
            }
        }

        return new QuestDef
        {
            id = $"RESCUE_{source.id}_{Guid.NewGuid():N}",
            isRescueQuest = true,
            sourceQuestId = source.id,
            rescueCharacterIds = rescueIds,
            seed = source.seed,
            tier = source.tier,
            difficulty = source.difficulty,
            stageKey = source.stageKey,
            mapPrefabIndex = source.mapPrefabIndex,
            issuer = source.issuer,
            bossUid = source.bossUid,
            title = source.title,
            recommendedLevel = source.recommendedLevel,
            recommendedPartySize = source.recommendedPartySize,
            maxPartySize = 4,
            enemyPool = enemyPool,
            reward = reward
        };
    }


    private void ResolveRescuedCharacters(QuestDef rescueQuest)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        PlayerData playerData = playerManager != null
            ? playerManager.GetCurrentPlayerData()
            : null;

        if (playerData == null || rescueQuest.rescueCharacterIds == null)
            return;

        if (playerData.characterIds == null)
            playerData.characterIds = new List<string>();

        if (playerData.missingCharacterIds == null)
            playerData.missingCharacterIds = new List<string>();

        if (playerData.revivalRequiredCharacterIds == null)
            playerData.revivalRequiredCharacterIds = new List<string>();

        foreach (string characterId in rescueQuest.rescueCharacterIds)
        {
            if (string.IsNullOrEmpty(characterId))
                continue;

            playerData.missingCharacterIds.RemoveAll(id => id == characterId);

            if (!playerData.characterIds.Contains(characterId))
                playerData.characterIds.Add(characterId);

            if (!playerData.revivalRequiredCharacterIds.Contains(characterId))
                playerData.revivalRequiredCharacterIds.Add(characterId);

            playerData.activeCharacterIds?.RemoveAll(id => id == characterId);

            if (CharacterPoolManager.Instance != null &&
                CharacterPoolManager.Instance.Get(characterId) == null)
            {
                string rescuedId = characterId;

                playerManager.LoadCharacter(
                    rescuedId,
                    character =>
                    {
                        if (character != null)
                            CharacterPoolManager.Instance?.AddCharacterToPool(character);
                    });
            }
        }
    }

    #endregion


    #region Quest Generation

    private QuestDef GenerateOneQuest(int boardIndex)
    {
        if (GameDataRegistry.Instance == null)
        {
            Debug.LogError(
                "GameDataRegistry가 없습니다.");

            return null;
        }


        int seed =
            Guid.NewGuid().GetHashCode() ^
            Environment.TickCount;


        System.Random rand =
            new System.Random(seed);


        int unlockedLevel =
            Mathf.Max(
                maxUnlockedLevel,
                1);


        int unlockedTier =
            Mathf.Max(
                (unlockedLevel - 1) / 10 + 1,
                1);


        int minimumTier =
            Mathf.Clamp(
                minQuestTier,
                1,
                unlockedTier);


        int maximumTier =
            Mathf.Clamp(
                maxQuestTier,
                minimumTier,
                unlockedTier);


        int preferredTier =
            GetPreferredQuestTierForBoardIndex(
                boardIndex,
                minimumTier,
                maximumTier);


        int questTier = preferredTier > 0
            ? preferredTier
            : rand.Next(
                minimumTier,
                maximumTier + 1);


        QuestStageDefinitionSO stage =
            PickStage(rand);


        int mapPrefabIndex =
            PickMapPrefabIndex(
                stage,
                rand);


        QuestIssuer issuer =
            PickIssuer(rand);


        int tierMinimumLevel =
            (questTier - 1) * 10 + 1;


        int tierMaximumLevel =
            Mathf.Min(
                questTier * 10,
                unlockedLevel);


        int recommendedLevel =
            rand.Next(
                tierMinimumLevel,
                tierMaximumLevel + 1);


        QuestDifficulty difficulty =
            GetQuestDifficulty(
                recommendedLevel);


        int recommendedPartySize =
            rand.Next(1, 5);


        int maxPartySize = 4;


        MonsterRoleSO boss =
            PickMonster(
                CharacterType.Boss,
                stage != null
                    ? stage.monsterTags
                    : null,
                rand);


        List<MonsterSpawn> enemyPool =
            BuildEnemyPool(
                recommendedPartySize,
                stage != null
                    ? stage.monsterTags
                    : null,
                rand);


        if (boss != null)
        {
            enemyPool.Add(
                new MonsterSpawn
                {
                    monsterUid =
                        boss.id,

                    count =
                        1
                });
        }


        QuestReward reward =
            GenerateReward(
                questTier,
                difficulty,
                issuer,
                rand);


        string title =
            BuildQuestTitle(
                boss,
                stage);


        string id =
            $"Q_{(stage != null ? stage.stageKey : "Unknown")}_" +
            $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_" +
            $"{rand.Next(100, 999)}";


        return new QuestDef
        {
            id =
                id,

            seed =
                seed,

            tier =
                questTier,

            difficulty =
                difficulty,

            stageKey =
                stage != null
                    ? stage.stageKey
                    : "",

            mapPrefabIndex =
                mapPrefabIndex,

            issuer =
                issuer,

            bossUid =
                boss != null
                    ? boss.id
                    : 0,

            title =
                title,

            recommendedLevel =
                recommendedLevel,

            recommendedPartySize =
                recommendedPartySize,

            maxPartySize =
                maxPartySize,

            enemyPool =
                enemyPool,

            reward =
                reward
        };
    }


    private QuestStageDefinitionSO PickStage(System.Random rand)
    {
        List<QuestStageDefinitionSO> stages =
            GameDataRegistry.Instance
                .GetAllQuestStages();


        if (stages == null ||
            stages.Count == 0)
        {
            return null;
        }


        List<QuestStageDefinitionSO> availableStages =
            stages.FindAll(stage =>
                stage != null &&
                GameDataRegistry.Instance
                    .GetMonsterRolesByTypeAndTags(
                        CharacterType.Normal,
                        stage.monsterTags)
                    .Count > 0);


        if (availableStages.Count == 0)
            return null;


        return availableStages[
            rand.Next(
                0,
                availableStages.Count)];
    }


    private int PickMapPrefabIndex(
        QuestStageDefinitionSO stage,
        System.Random rand)
    {
        if (stage == null ||
            stage.mapPrefabs == null ||
            stage.mapPrefabs.Count == 0)
        {
            return -1;
        }


        List<int> availableMapIndexes =
            new List<int>();


        for (int i = 0;
             i < stage.mapPrefabs.Count;
             i++)
        {
            if (stage.mapPrefabs[i] != null)
                availableMapIndexes.Add(i);
        }


        if (availableMapIndexes.Count == 0)
            return -1;


        return availableMapIndexes[
            rand.Next(
                0,
                availableMapIndexes.Count)];
    }


    private QuestDifficulty GetQuestDifficulty(int level)
    {
        int levelInTier =
            (Mathf.Max(1, level) - 1) % 10 + 1;

        if (levelInTier <= 3)
            return QuestDifficulty.Easy;

        if (levelInTier <= 7)
            return QuestDifficulty.Normal;

        return QuestDifficulty.Hard;
    }


    private QuestIssuer PickIssuer(
        System.Random rand)
    {
        int totalWeight = 0;


        foreach (QuestIssuer issuer in GeneratedIssuers)
        {
            totalWeight +=
                preferredIssuers != null &&
                preferredIssuers.Contains(issuer)
                    ? PreferredIssuerWeight
                    : 1;
        }


        int roll = rand.Next(0, totalWeight);


        foreach (QuestIssuer issuer in GeneratedIssuers)
        {
            int weight =
                preferredIssuers != null &&
                preferredIssuers.Contains(issuer)
                    ? PreferredIssuerWeight
                    : 1;


            if (roll < weight)
                return issuer;


            roll -= weight;
        }


        return GeneratedIssuers[0];
    }


    private int GetPreferredQuestTierForBoardIndex(
        int boardIndex,
        int minimumTier,
        int maximumTier)
    {
        List<int> selectedTiers =
            new List<int>();


        if (preferredQuestTiers != null)
        {
            foreach (int tier in preferredQuestTiers)
            {
                if (tier >= minimumTier &&
                    tier <= maximumTier &&
                    !selectedTiers.Contains(tier))
                {
                    selectedTiers.Add(tier);
                }
            }
        }


        if (selectedTiers.Count == 0)
            return 0;


        selectedTiers.Sort();


        return selectedTiers[
            Mathf.Abs(boardIndex) %
            selectedTiers.Count];
    }


    private string BuildQuestTitle(
        MonsterRoleSO boss,
        QuestStageDefinitionSO stage)
    {
        if (boss != null &&
            boss.baseMonster != null &&
            !string.IsNullOrEmpty(
                boss.baseMonster.monsterName))
        {
            return
                $"{boss.baseMonster.monsterName} 토벌";
        }


        if (stage != null && !string.IsNullOrEmpty(stage.stageName))
            return $"{stage.stageName} 토벌 의뢰";


        return "토벌 의뢰";
    }

    #endregion


    #region Monster

    private MonsterRoleSO PickMonster(
        CharacterType type,
        List<string> monsterTags,
        System.Random rand)
    {
        List<MonsterRoleSO> candidates =
            GameDataRegistry.Instance
                .GetMonsterRolesByTypeAndTags(
                    type,
                    monsterTags);


        if (candidates.Count == 0)
            return null;


        return candidates[
            rand.Next(
                0,
                candidates.Count)];
    }


    private List<MonsterSpawn> BuildEnemyPool(
        int partySize,
        List<string> monsterTags,
        System.Random rand)
    {
        List<MonsterSpawn> result =
            new List<MonsterSpawn>();


        List<MonsterRoleSO> normalMonsters =
            GameDataRegistry.Instance
                .GetMonsterRolesByTypeAndTags(
                    CharacterType.Normal,
                    monsterTags);


        int remaining =
            partySize * 4 +
            Mathf.RoundToInt(
                partySize * 1.5f);


        while (
            remaining > 0 &&
            normalMonsters.Count > 0)
        {
            MonsterRoleSO monster =
                normalMonsters[
                    rand.Next(
                        0,
                        normalMonsters.Count)];


            int pack =
                rand.Next(2, 5);


            int count =
                Math.Min(
                    pack,
                    remaining);


            AddMonsterSpawn(
                result,
                monster.id,
                count);


            remaining -= count;
        }


        List<MonsterRoleSO> eliteMonsters =
            GameDataRegistry.Instance
                .GetMonsterRolesByTypeAndTags(
                    CharacterType.Elite,
                    monsterTags);


        if (eliteMonsters.Count > 0)
        {
            int rolls =
                rand.Next(0, 3);


            for (int i = 0;
                 i < rolls;
                 i++)
            {
                if (rand.Next(0, 100) >=
                    eliteSpawnChance)
                {
                    continue;
                }


                MonsterRoleSO elite =
                    eliteMonsters[
                        rand.Next(
                            0,
                            eliteMonsters.Count)];


                AddMonsterSpawn(
                    result,
                    elite.id,
                    1);
            }
        }


        return result;
    }


    private void AddMonsterSpawn(
        List<MonsterSpawn> pool,
        int monsterUid,
        int count)
    {
        MonsterSpawn existing =
            pool.Find(
                x =>
                    x.monsterUid ==
                    monsterUid);


        if (existing != null)
        {
            existing.count +=
                count;

            return;
        }


        pool.Add(
            new MonsterSpawn
            {
                monsterUid =
                    monsterUid,

                count =
                    count
            });
    }


    public CharacterData GenerateMonster(
        int monsterRoleId,
        int questLevel,
        int randomSeed = 0)
    {
        if (GameDataRegistry.Instance == null)
            return null;


        MonsterRoleSO monsterRole =
            GameDataRegistry.Instance
                .GetMonsterRole(
                    monsterRoleId);


        if (monsterRole == null)
            return null;


        return MonsterGenerator.Generate(
            monsterRole,
            questLevel,
            randomSeed);
    }

    #endregion


    #region Reward

    private QuestReward GenerateReward(
        int questTier,
        QuestDifficulty difficulty,
        QuestIssuer issuer,
        System.Random rand)
    {
        QuestReward reward =
            new QuestReward
            {
                randomSeed = rand.Next(1, int.MaxValue),
                tier = questTier,
                goldAmount = CalculateGoldReward(questTier, difficulty),
                maxEquipmentRarity = GetMaximumEquipmentRewardRarity(difficulty)
            };


        List<EquipmentDefinitionSO> equipments =
            GameDataRegistry.Instance
                .GetEquipmentsByTier(
                    questTier)
                .FindAll(equipment =>
                    equipment != null &&
                    equipment.canDrop &&
                    IsEquipmentAllowedForIssuer(
                        equipment,
                        issuer));


        List<SkillDefinitionSO> skills =
            GameDataRegistry.Instance
                .GetRewardSkills()
                .FindAll(skill =>
                    IsRewardSkillAvailable(
                        skill,
                        questTier) &&
                    IsSkillAllowedForIssuer(
                        skill,
                        issuer));


        bool hasEquipment =
            equipments.Count > 0;


        bool hasSkill =
            skills.Count > 0;


        if (!hasEquipment &&
            !hasSkill)
        {
            return reward;
        }


        bool giveEquipment;


        if (!hasSkill)
        {
            giveEquipment =
                true;
        }
        else if (!hasEquipment)
        {
            giveEquipment =
                false;
        }
        else
        {
            giveEquipment =
                rand.Next(0, 100) <
                equipmentRewardChance;
        }


        if (giveEquipment)
        {
            EquipmentDefinitionSO equipment =
                equipments[
                    rand.Next(
                        0,
                        equipments.Count)];


            reward.kind = QuestRewardKind.Equipment;


            SetEquipmentRewardFilter(
                reward,
                equipment);
        }
        else
        {
            SkillDefinitionSO skill =
                PickSkillReward(
                    questTier,
                    difficulty,
                    issuer,
                    rand);


            if (skill != null)
            {
                reward.kind = QuestRewardKind.Skill;
                reward.skillDiscipline = skill.discipline;
            }
        }


        return reward;
    }


    private SkillDefinitionSO PickSkillReward(
        int questTier,
        QuestDifficulty difficulty,
        QuestIssuer issuer,
        System.Random rand)
    {
        List<SkillDefinitionSO> candidates =
            GameDataRegistry.Instance
                .GetRewardSkills()
                .FindAll(skill =>
                    IsRewardSkillAvailable(
                        skill,
                        questTier) &&
                    IsSkillAllowedForIssuer(
                        skill,
                        issuer));


        return PickSkillReward(
            candidates,
            questTier,
            difficulty,
            rand);
    }


    private SkillDefinitionSO PickSkillReward(
        List<SkillDefinitionSO> candidates,
        int questTier,
        QuestDifficulty difficulty,
        System.Random rand)
    {
        if (candidates == null)
            return null;


        if (candidates.Count == 0)
            return null;


        int totalWeight =
            0;


        foreach (SkillDefinitionSO skill
                 in candidates)
        {
            totalWeight +=
                GetSkillRewardWeight(
                    skill,
                    questTier,
                    difficulty);
        }


        if (totalWeight <= 0)
            return null;


        int roll =
            rand.Next(
                0,
                totalWeight);


        int currentWeight =
            0;


        foreach (SkillDefinitionSO skill
                 in candidates)
        {
            currentWeight +=
                GetSkillRewardWeight(
                    skill,
                    questTier,
                    difficulty);


            if (roll < currentWeight)
                return skill;
        }


        return candidates[0];
    }


    private int GetSkillRewardWeight(
        SkillDefinitionSO skill,
        int questTier,
        QuestDifficulty difficulty)
    {
        if (skill == null)
            return 0;


        int skillCost =
            skill.GetTotalResourceCost();


        int difficultyOffset =
            difficulty == QuestDifficulty.Easy
                ? -1
                : difficulty == QuestDifficulty.Hard
                    ? 1
                    : 0;


        int targetCost = Mathf.Clamp(
            questTier + difficultyOffset,
            1,
            Mathf.Max(1, maxRewardSkillCost));


        int costDistance =
            Mathf.Abs(
                skillCost - targetCost);


        return Mathf.Max(
            1,
            100 /
            (1 + costDistance * 2));
    }


    private bool IsRewardSkillAvailable(
        SkillDefinitionSO skill,
        int questTier)
    {
        return skill != null &&
               skill.CanAppearInRewardPool(questTier) &&
               skill.GetTotalResourceCost() <= Mathf.Max(1, maxRewardSkillCost);
    }


    private bool IsEquipmentAllowedForIssuer(
        EquipmentDefinitionSO equipment,
        QuestIssuer issuer)
    {
        WeaponDefinitionSO weapon =
            equipment as WeaponDefinitionSO;


        ArmorDefinitionSO armor =
            equipment as ArmorDefinitionSO;


        switch (issuer)
        {
            case QuestIssuer.HunterGuild:
                return
                    (weapon != null &&
                     (weapon.weaponType == WeaponType.Bow ||
                      weapon.weaponType == WeaponType.Dagger)) ||
                    (armor != null &&
                     armor.armorCategory == ArmorCategory.LightArmor);

            case QuestIssuer.WarriorGuild:
                return
                    (weapon != null &&
                     weapon.weaponType != WeaponType.Bow &&
                     (weapon.weaponTags == null ||
                      !weapon.weaponTags.Contains(WeaponTag.MagicWeapon))) ||
                    (armor != null &&
                     (armor.armorCategory == ArmorCategory.LightArmor ||
                      armor.armorCategory == ArmorCategory.HeavyArmor));

            case QuestIssuer.SwordDojo:
                return
                    (weapon != null &&
                     (weapon.weaponType == WeaponType.LongSword ||
                      weapon.weaponType == WeaponType.Two_HandedSword ||
                      weapon.weaponType == WeaponType.Greatsword ||
                      weapon.weaponType == WeaponType.Shield)) ||
                    (armor != null &&
                     armor.armorCategory == ArmorCategory.HeavyArmor);

            case QuestIssuer.MageTower:
                return
                    (weapon != null &&
                     ((weapon.weaponTags != null &&
                       weapon.weaponTags.Contains(WeaponTag.MagicWeapon)) ||
                      weapon.weaponType == WeaponType.Staff ||
                      weapon.weaponType == WeaponType.Book ||
                      weapon.weaponType == WeaponType.Orb)) ||
                    (armor != null &&
                     armor.armorCategory == ArmorCategory.ClothArmor);

            case QuestIssuer.TownCouncil:
                return equipment.equipType == EquipmentType.Ring ||
                       equipment.equipType == EquipmentType.Necklace;

            case QuestIssuer.ThievesGuild:
                return
                    (weapon != null &&
                     weapon.weaponType == WeaponType.Dagger) ||
                    (armor != null &&
                     armor.armorCategory == ArmorCategory.LightArmor);

            default:
                return true;
        }
    }


    private bool IsSkillAllowedForIssuer(
        SkillDefinitionSO skill,
        QuestIssuer issuer)
    {
        if (skill == null)
            return false;


        switch (issuer)
        {
            case QuestIssuer.HunterGuild:
                return skill.discipline == SkillDiscipline.Archery ||
                       skill.discipline == SkillDiscipline.DaggerArt;

            case QuestIssuer.WarriorGuild:
                return skill.discipline == SkillDiscipline.WeaponArt ||
                       skill.discipline == SkillDiscipline.MartialArt;

            case QuestIssuer.SwordDojo:
                return skill.discipline == SkillDiscipline.Swordsmanship ||
                       skill.discipline == SkillDiscipline.ShieldArt;

            case QuestIssuer.MageTower:
                return skill.discipline == SkillDiscipline.Magic;

            case QuestIssuer.ThievesGuild:
                return skill.discipline == SkillDiscipline.DaggerArt;

            case QuestIssuer.TownCouncil:
                return skill.discipline != SkillDiscipline.Basic &&
                       skill.discipline != SkillDiscipline.Monster;

            default:
                return skill.discipline != SkillDiscipline.Monster;
        }
    }


    private void SetEquipmentRewardFilter(
        QuestReward reward,
        EquipmentDefinitionSO equipment)
    {
        if (equipment is WeaponDefinitionSO weapon)
        {
            reward.equipmentFilter =
                QuestEquipmentRewardFilter.WeaponType;

            reward.weaponType =
                weapon.weaponType;

            return;
        }


        if (equipment is ArmorDefinitionSO armor)
        {
            reward.equipmentFilter =
                QuestEquipmentRewardFilter.ArmorCategory;

            reward.armorCategory =
                armor.armorCategory;

            return;
        }


        reward.equipmentFilter =
            QuestEquipmentRewardFilter.EquipmentType;

        reward.equipmentType =
            equipment.equipType;
    }


    private bool MatchesEquipmentRewardFilter(
        EquipmentDefinitionSO equipment,
        QuestReward reward)
    {
        if (equipment == null || reward == null)
            return false;


        switch (reward.equipmentFilter)
        {
            case QuestEquipmentRewardFilter.Any:
                return true;

            case QuestEquipmentRewardFilter.WeaponType:
                return equipment is WeaponDefinitionSO weapon &&
                       weapon.weaponType == reward.weaponType;

            case QuestEquipmentRewardFilter.ArmorCategory:
                return equipment is ArmorDefinitionSO armor &&
                       armor.armorCategory == reward.armorCategory;

            case QuestEquipmentRewardFilter.EquipmentType:
                return equipment.equipType == reward.equipmentType;

            default:
                return false;
        }
    }


    public QuestReward ResolveReward(QuestDef quest)
    {
        if (quest == null || quest.reward == null)
            return null;


        QuestReward reward = quest.reward;


        if (reward.goldAmount <= 0)
        {
            reward.goldAmount = CalculateGoldReward(
                quest.tier,
                quest.difficulty);
        }


        if (reward.rolled)
            return reward;


        bool hasLegacyResolvedReward =
            (reward.equipmentUids != null &&
             reward.equipmentUids.Count > 0) ||
            (reward.skillUids != null &&
             reward.skillUids.Count > 0);


        if (hasLegacyResolvedReward)
        {
            reward.rolled = true;
            return reward;
        }


        int randomSeed = reward.randomSeed != 0
            ? reward.randomSeed
            : quest.seed;


        System.Random rand =
            new System.Random(randomSeed);


        if (reward.kind == QuestRewardKind.Equipment)
        {
            List<EquipmentDefinitionSO> candidates =
                GameDataRegistry.Instance
                    .GetEquipmentsByTier(reward.tier)
                    .FindAll(equipment =>
                        equipment != null &&
                        equipment.canDrop &&
                        IsEquipmentAllowedForIssuer(
                            equipment,
                            quest.issuer) &&
                        MatchesEquipmentRewardFilter(
                            equipment,
                            reward));


            if (candidates.Count > 0)
            {
                EquipmentDefinitionSO equipment =
                    candidates[rand.Next(0, candidates.Count)];


                reward.equipmentUids.Add(equipment.uid);


                EquipmentRarity rarity =
                    RollEquipmentRewardRarity(
                        quest.difficulty,
                        rand);


                reward.equipmentRarities.Add(
                    rarity <= reward.maxEquipmentRarity
                        ? rarity
                        : reward.maxEquipmentRarity);
            }
        }
        else if (reward.kind == QuestRewardKind.Skill)
        {
            List<SkillDefinitionSO> candidates =
                GameDataRegistry.Instance
                    .GetRewardSkills()
                    .FindAll(skill =>
                        IsRewardSkillAvailable(
                            skill,
                            reward.tier) &&
                        IsSkillAllowedForIssuer(
                            skill,
                            quest.issuer) &&
                        skill.discipline == reward.skillDiscipline);


            SkillDefinitionSO skill =
                PickSkillReward(
                    candidates,
                    reward.tier,
                    quest.difficulty,
                    rand);


            if (skill != null)
                reward.skillUids.Add(skill.uid);
        }


        reward.rolled = true;
        return reward;
    }


    public static int CalculateGoldReward(
        int questTier,
        QuestDifficulty difficulty)
    {
        int tier = Mathf.Max(1, questTier);
        int difficultyStep = Mathf.Clamp((int)difficulty, 0, 2);

        return 1000 + tier * 1000 + difficultyStep * 400;
    }


    public int ClaimCompletedGoldRewards(PlayerData playerData)
    {
        return ClaimCompletedRewards(playerData);
    }


    public int ClaimCompletedRewards(PlayerData playerData)
    {
        if (playerData == null || completed == null)
            return 0;

        int totalGold = 0;
        int grantedItemCount = 0;
        bool changed = false;

        foreach (CompletedQuestEntry entry in completed)
        {
            if (entry?.def == null)
                continue;

            QuestReward reward = ResolveReward(entry.def);

            if (reward == null)
                continue;

            if (!reward.goldClaimed)
            {
                if (reward.goldAmount > 0)
                {
                    playerData.gold += reward.goldAmount;
                    totalGold += reward.goldAmount;
                }

                reward.goldClaimed = true;
                changed = true;
            }

            bool hasItemReward =
                (reward.kind == QuestRewardKind.Equipment &&
                 reward.equipmentUids != null &&
                 reward.equipmentUids.Count > 0) ||
                (reward.kind == QuestRewardKind.Skill &&
                 reward.skillUids != null &&
                 reward.skillUids.Count > 0);

            if (!reward.itemsClaimed && TryClaimCompletedItemReward(playerData, reward))
            {
                reward.itemsClaimed = true;

                if (hasItemReward)
                    grantedItemCount++;

                changed = true;
            }

            QuestStatus nextStatus = reward.goldClaimed && reward.itemsClaimed
                ? QuestStatus.Rewarded
                : QuestStatus.CompletedPendingReward;

            if (entry.status != nextStatus)
            {
                entry.status = nextStatus;
                changed = true;
            }
        }

        if (totalGold > 0)
            Debug.Log($"완료 의뢰 보수 {totalGold:N0} G를 수령했습니다.");

        if (grantedItemCount > 0)
            Debug.Log($"완료 의뢰의 주요 보상 {grantedItemCount}개를 용병단 창고에 보관했습니다.");

        if (changed)
            OnCompletedChanged?.Invoke();

        return totalGold;
    }


    private static bool TryClaimCompletedItemReward(
        PlayerData playerData,
        QuestReward reward)
    {
        if (playerData == null || reward == null)
            return false;

        if (reward.kind == QuestRewardKind.None)
            return true;

        List<InventorySlotData> storage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.CompanyStorage);

        if (storage == null || GameDataRegistry.Instance == null)
            return false;

        if (reward.kind == QuestRewardKind.Equipment)
        {
            if (reward.equipmentUids == null || reward.equipmentUids.Count == 0)
                return true;

            EquipmentDefinitionSO definition = GameDataRegistry.Instance.GetEquipment(
                reward.equipmentUids[0]);

            if (definition == null)
                return false;

            EquipmentRarity rarity = reward.equipmentRarities != null &&
                                     reward.equipmentRarities.Count > 0
                ? reward.equipmentRarities[0]
                : EquipmentRarity.Common;
            GeneratedEquipmentData generated = EquipmentGenerator.Generate(
                definition,
                rarity);

            if (generated == null)
                return false;

            EquipmentInstanceRepository.AddToPlayer(generated);

            if (SharedInventoryUtility.AddItem(
                    storage,
                    definition.uid,
                    1,
                    generated.instanceId))
            {
                return true;
            }

            EquipmentInstanceRepository.RemoveFromPlayer(generated.instanceId);
            return false;
        }

        if (reward.kind == QuestRewardKind.Skill)
        {
            if (reward.skillUids == null || reward.skillUids.Count == 0)
                return true;

            int skillUid = reward.skillUids[0];

            if (GameDataRegistry.Instance.GetSkill(skillUid) == null ||
                GameDataRegistry.Instance.GetItem(SharedInventoryUtility.SkillBookItemUid) == null)
            {
                return false;
            }

            return SharedInventoryUtility.AddInventorySlot(
                storage,
                new InventorySlotData
                {
                    itemUid = SharedInventoryUtility.SkillBookItemUid,
                    count = 1,
                    skillBookSkillUid = skillUid
                });
        }

        return true;
    }


    private EquipmentRarity GetMaximumEquipmentRewardRarity(
        QuestDifficulty difficulty)
    {
        return difficulty == QuestDifficulty.Easy
            ? EquipmentRarity.Rare
            : EquipmentRarity.Epic;
    }


    private EquipmentRarity RollEquipmentRewardRarity(
        QuestDifficulty difficulty,
        System.Random rand)
    {
        int roll = rand.Next(0, 100);

        if (difficulty == QuestDifficulty.Easy)
        {
            if (roll < 70) return EquipmentRarity.Common;
            if (roll < 95) return EquipmentRarity.Uncommon;
            return EquipmentRarity.Rare;
        }

        if (difficulty == QuestDifficulty.Normal)
        {
            if (roll < 35) return EquipmentRarity.Common;
            if (roll < 80) return EquipmentRarity.Uncommon;
            if (roll < 98) return EquipmentRarity.Rare;
            return EquipmentRarity.Epic;
        }

        if (roll < 10) return EquipmentRarity.Common;
        if (roll < 40) return EquipmentRarity.Uncommon;
        if (roll < 85) return EquipmentRarity.Rare;
        return EquipmentRarity.Epic;
    }

    #endregion
}
