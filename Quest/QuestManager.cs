using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestStatus { Board, Reserved, Active, CompletedPendingReward, Rewarded }
public enum QuestIssuer { HunterGuild, WarriorGuild, SwordDojo, MageTower, TownCouncil }

[Serializable]
public class QuestReward
{
    public int gold;
    public int[] itemUids;
}

[Serializable]
public class MonsterSpawn
{
    public int monsterUid;
    public int count;
}

[Serializable]
public class QuestDef
{
    public string id;                // ex) Q_Forest_20250928_001
    public int seed;                 // 결정적 랜덤 시드 (저장)
    public string stageKey;          // ex) "Forest"
    public QuestIssuer issuer;

    public int bossUid;
    public string title;

    public int recommendedLevel;
    public int recommendedPartySize;
    public int maxPartySize;

    public List<MonsterSpawn> enemyPool = new();
    public QuestReward reward = new QuestReward();
}

[Serializable]
public class QuestBoardEntry
{
    public QuestDef def;
    public bool reserved;
    public QuestStatus status; // Board / Reserved
}

[Serializable]
public class ActiveQuestRuntime
{
    public QuestDef def;
    public QuestStatus status; // Active
    public string leaderCharacterId;
    public List<string> partyCharacterIds = new();
    public string stageKey;
    public int stageNodeIndex;
    public bool retreated;
}

[Serializable]
public class CompletedQuestEntry
{
    public QuestDef def;
    public QuestStatus status; // CompletedPendingReward / Rewarded
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // 보드,진행,완료
    public List<QuestBoardEntry> board = new();           // 최대 8개
    public ActiveQuestRuntime active;
    public List<CompletedQuestEntry> completed = new();

    // UI 바인딩용 이벤트(선택)
    public event Action OnBoardChanged;
    public event Action OnActiveChanged;
    public event Action OnCompletedChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GenerateBoardIfEmpty(int targetCount = 8)
    {
        if (board == null) board = new List<QuestBoardEntry>();
        if (board.Count >= targetCount) return;

        while (board.Count < targetCount)
            board.Add(new QuestBoardEntry
            {
                def = GenerateOneQuest(),
                reserved = false,
                status = QuestStatus.Board
            });

        OnBoardChanged?.Invoke();
    }

    // 퀘스트 귀환 시 새로고침(예약만 유지)
    public void RefreshBoard(bool preserveReserved = true, int targetCount = 8)
    {
        var keep = new List<QuestBoardEntry>();
        if (preserveReserved)
            foreach (var e in board)
                if (e.status == QuestStatus.Reserved) keep.Add(e);

        board.Clear();
        board.AddRange(keep);

        while (board.Count < targetCount)
            board.Add(new QuestBoardEntry
            {
                def = GenerateOneQuest(),
                reserved = false,
                status = QuestStatus.Board
            });

        OnBoardChanged?.Invoke();
    }


    // 퀘스트 수락,예약,완료
    public bool CanAccept() => active == null;

    public bool Accept(string questId, string leaderId, List<string> partyIds)
    {
        if (!CanAccept()) return false;

        var entry = board.Find(b => b.def.id == questId && (b.status == QuestStatus.Board || b.status == QuestStatus.Reserved));
        if (entry == null) return false;

        active = new ActiveQuestRuntime
        {
            def = entry.def,
            status = QuestStatus.Active,
            leaderCharacterId = leaderId,
            partyCharacterIds = new List<string>(partyIds),
            stageKey = entry.def.stageKey,
            stageNodeIndex = 0,
            retreated = false
        };

        board.Remove(entry);
        OnBoardChanged?.Invoke();
        OnActiveChanged?.Invoke();
        return true;
    }

    public void ReserveToggle(string questId)
    {
        var entry = board.Find(b => b.def.id == questId);
        if (entry == null) return;

        if (entry.status == QuestStatus.Board)
        {
            entry.status = QuestStatus.Reserved;
            entry.reserved = true;
        }
        else if (entry.status == QuestStatus.Reserved)
        {
            entry.status = QuestStatus.Board;
            entry.reserved = false;
        }
        OnBoardChanged?.Invoke();
    }

    public void CompleteActive(bool rewardClaimed = false)
    {
        if (active == null) return;
        completed.Add(new CompletedQuestEntry
        {
            def = active.def,
            status = rewardClaimed ? QuestStatus.Rewarded : QuestStatus.CompletedPendingReward
        });
        active = null;

        OnActiveChanged?.Invoke();
        OnCompletedChanged?.Invoke();
    }

    // 퀘스트 1개 생성

    QuestDef GenerateOneQuest()
    {
        // 시드 고정(저장/로드 동일성 보장)
        int seed = Guid.NewGuid().GetHashCode() ^ Environment.TickCount;
        var rand = new System.Random(seed);

        // 스테이지/주체
        var stages = StageContentDB.AllStageKeys();
        string stage = stages[rand.Next(0, stages.Length)];
        var prof = StageContentDB.Get(stage) ?? new StageProfile
        {
            key = stage,
            label = stage,
            allowedTags = new[] { "Beast" },
            bossCandidates = Array.Empty<int>()
        };

        var issuers = (QuestIssuer[])Enum.GetValues(typeof(QuestIssuer));
        var issuer = issuers[rand.Next(0, issuers.Length)];

        // 권장 수치
        int recLv = rand.Next(1, 7);                  //
        int recParty = rand.Next(1, 5);               // 1~4인
        int maxParty = Mathf.Clamp(recParty + rand.Next(0, 2), 2, 6);

        // 보스 선택
        int bossUid = (prof.bossCandidates != null && prof.bossCandidates.Length > 0)
                        ? prof.bossCandidates[rand.Next(0, prof.bossCandidates.Length)]
                        : FallbackBossFromTags(prof, rand);

        // 스폰 풀 구성(태그 섞기 + 보스 추가)
        var pool = BuildMixedPool(prof, recParty, baseCount: recParty * 4, eliteChance: 20, rand: rand);
        pool.Add(new MonsterSpawn { monsterUid = bossUid, count = 1 });

        // 보상
        var reward = QuestRewardFactory.Build(recLv, issuer, rand);

        // 타이틀/설명
        string bossName = MonsterDB.GetName(bossUid);
        string title = $"{StageContentDB.GetLabel(stage)} - [{bossName}] 토벌";
        string desc = $"{StageContentDB.GetLabel(stage)}에 출몰한 {bossName} 처치";

        // ID
        string id = $"Q_{stage}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{rand.Next(100, 999)}";

        return new QuestDef
        {
            id = id,
            seed = seed,
            stageKey = stage,
            issuer = issuer,
            bossUid = bossUid,
            title = title,
            recommendedLevel = recLv,
            recommendedPartySize = recParty,
            maxPartySize = maxParty,
            enemyPool = pool,
            reward = reward
        };
    }

    int FallbackBossFromTags(StageProfile prof, System.Random rand)
    {
        if (prof.allowedTags == null || prof.allowedTags.Length == 0) return 19099; // 더미
        string tag = prof.allowedTags[rand.Next(0, prof.allowedTags.Length)];
        var pool = MonsterDB.GetUidsByTag(tag);
        if (pool.Length == 0) return 19099;
        return pool[Mathf.Clamp(pool.Length - 1, 0, pool.Length - 1)];
    }

    List<MonsterSpawn> BuildMixedPool(StageProfile prof, int party, int baseCount, int eliteChance, System.Random rand)
    {
        var list = new List<MonsterSpawn>();
        if (prof.allowedTags == null || prof.allowedTags.Length == 0) return list;

        // 사용할 태그 수(최소 1)
        int tagCount = Math.Clamp(rand.Next(1, prof.allowedTags.Length + 1), 1, prof.allowedTags.Length);
        var tags = new List<string>(prof.allowedTags);
        var pickTags = new List<string>();
        for (int i = 0; i < tagCount; i++)
        {
            int idx = rand.Next(0, tags.Count);
            pickTags.Add(tags[idx]);
            tags.RemoveAt(idx);
        }

        int remaining = baseCount + Mathf.RoundToInt(party * 1.5f);
        while (remaining > 0)
        {
            string t = pickTags[rand.Next(0, pickTags.Count)];
            var pool = MonsterDB.GetUidsByTag(t);
            if (pool.Length == 0) break;

            int uid = pool[rand.Next(0, pool.Length)];
            int pack = rand.Next(2, 5); // 2~4 마리
            list.Add(new MonsterSpawn { monsterUid = uid, count = Math.Min(pack, remaining) });
            remaining -= pack;
        }

        // 엘리트 약간
        if (prof.eliteCandidates != null && prof.eliteCandidates.Length > 0)
        {
            int rolls = rand.Next(0, 3); // 0~2
            for (int i = 0; i < rolls; i++)
                if (rand.Next(0, 100) < eliteChance)
                    list.Add(new MonsterSpawn { monsterUid = prof.eliteCandidates[rand.Next(0, prof.eliteCandidates.Length)], count = 1 });
        }

        return list;
    }
}
