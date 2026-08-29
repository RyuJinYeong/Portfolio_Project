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
    HunterGuild,
    WarriorGuild,
    SwordDojo,
    MageTower,
    TownCouncil
}

public enum QuestDifficulty
{
    Easy,
    Normal,
    Hard
}


[Serializable]
public class QuestReward
{
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
public class QuestDef
{
    public string id;

    // 생성 시 사용한 랜덤 시드
    public int seed;

    // 적과 장비 보상의 기준 Tier
    public int tier;

    public QuestDifficulty difficulty;

    public string stageKey;
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

    public ActiveQuestRuntime active;

    public List<CompletedQuestEntry> completed =
        new();


    public event Action OnBoardChanged;
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
        int targetCount = 8)
    {
        if (board == null)
        {
            board =
                new List<QuestBoardEntry>();
        }

        if (board.Count >= targetCount)
            return;


        while (board.Count < targetCount)
        {
            QuestDef quest =
                GenerateOneQuest();

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
        int targetCount = 8)
    {
        if (board == null)
        {
            board =
                new List<QuestBoardEntry>();
        }


        List<QuestBoardEntry> keep =
            new List<QuestBoardEntry>();


        if (preserveReserved)
        {
            foreach (QuestBoardEntry entry in board)
            {
                if (entry == null)
                    continue;

                if (entry.status ==
                    QuestStatus.Reserved)
                {
                    keep.Add(entry);
                }
            }
        }


        board.Clear();
        board.AddRange(keep);


        while (board.Count < targetCount)
        {
            QuestDef quest =
                GenerateOneQuest();

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

    #endregion


    #region Accept / Reserve / Complete

    public bool CanAccept()
    {
        return active == null;
    }


    public bool Accept(
        string questId,
        string leaderId,
        List<string> partyIds)
    {
        if (!CanAccept())
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

                retreated =
                    false
            };


        board.Remove(entry);


        OnBoardChanged?.Invoke();
        OnActiveChanged?.Invoke();


        return true;
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

    #endregion


    #region Quest Generation

    private QuestDef GenerateOneQuest()
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


        int questTier =
            rand.Next(
                minimumTier,
                maximumTier + 1);


        QuestStageDefinitionSO stage =
            PickStage(rand);


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


        int maxPartySize =
            Mathf.Clamp(
                recommendedPartySize +
                rand.Next(0, 2),
                2,
                6);


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
        QuestIssuer[] issuers =
            (QuestIssuer[])Enum.GetValues(
                typeof(QuestIssuer));


        return issuers[
            rand.Next(
                0,
                issuers.Length)];
    }


    private string BuildQuestTitle(
        MonsterRoleSO boss,
        QuestStageDefinitionSO stage)
    {
        if (boss != null &&
            boss.baseMonster != null)
        {
            if (!string.IsNullOrEmpty(
                    boss.roleName))
            {
                return
                    $"{boss.baseMonster.monsterName} " +
                    $"{boss.roleName} 토벌";
            }

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
        System.Random rand)
    {
        QuestReward reward =
            new QuestReward();


        List<EquipmentDefinitionSO> equipments =
            GameDataRegistry.Instance
                .GetEquipmentsByTier(
                    questTier);


        List<SkillDefinitionSO> skills =
            GameDataRegistry.Instance
                .GetRewardSkills()
                .FindAll(skill =>
                    IsRewardSkillAvailable(
                        skill,
                        questTier));


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


            reward.equipmentUids.Add(
                equipment.uid);

            reward.equipmentRarities.Add(
                RollEquipmentRewardRarity(
                    difficulty,
                    rand));
        }
        else
        {
            SkillDefinitionSO skill =
                PickSkillReward(
                    questTier,
                    difficulty,
                    rand);


            if (skill != null)
            {
                reward.skillUids.Add(
                    skill.uid);
            }
        }


        return reward;
    }


    private SkillDefinitionSO PickSkillReward(
        int questTier,
        QuestDifficulty difficulty,
        System.Random rand)
    {
        List<SkillDefinitionSO> candidates =
            GameDataRegistry.Instance
                .GetRewardSkills()
                .FindAll(skill =>
                    IsRewardSkillAvailable(
                        skill,
                        questTier));


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
