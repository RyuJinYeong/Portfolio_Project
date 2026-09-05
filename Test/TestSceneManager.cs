using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class TestSceneManager : MonoBehaviour
{
    [Header("Test Infrastructure")]
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject spawnPointManagerPrefab;
    [SerializeField] private GameObject uiManagerPrefab;
    [SerializeField] private GameObject allyCombatPrefab;
    [SerializeField] private GameObject enemyCombatPrefab;
    [SerializeField] private GameDataRegistryTester registryTester;

    [Header("Dungeon Battle")]
    [SerializeField] private bool startBattleOnPlay = true;
    [SerializeField, Min(1)] private int questTier = 1;
    [SerializeField, Min(1)] private int questLevel = 5;
    [SerializeField, Range(1, 5)] private int enemyCount = 4;
    [SerializeField] private QuestDifficulty questDifficulty = QuestDifficulty.Normal;
    [SerializeField] private QuestRouteNodeType testNodeType = QuestRouteNodeType.Battle;
    [SerializeField] private int randomSeed = 20260905;

    private void Awake()
    {
        EnsureTestInfrastructure();
    }

    private IEnumerator Start()
    {
        if (!startBattleOnPlay)
            yield break;

        yield return null;
        StartTestBattle();
    }

    [ContextMenu("Start Test Battle")]
    public void StartTestBattle()
    {
        if (GameManager.Instance == null ||
            SpawnPointManager.Instance == null ||
            QuestManager.Instance == null ||
            registryTester == null)
        {
            Debug.LogError("[TestScene] 전투 테스트용 매니저 참조가 준비되지 않았습니다.");
            return;
        }

        if (!registryTester.RegisterTestData())
            return;

        PlayerManager playerManager = PlayerManager.Instance;
        playerManager.suppressRemotePersistence = true;

        PlayerData playerData = new PlayerData
        {
            playerName = "전투 테스트 원정대",
            level = questLevel,
            currentStage = registryTester.DungeonStage.stageKey
        };

        playerManager.SetCurrentPlayerData(playerData);

        QuestDef quest = BuildTestQuest();
        QuestRouteNode routeNode = new QuestRouteNode
        {
            id = 1,
            depth = 1,
            column = 0,
            type = testNodeType,
            mapPrefabIndex = -1
        };

        QuestManager.Instance.active = new ActiveQuestRuntime
        {
            def = quest,
            status = QuestStatus.Active,
            leaderCharacterId = "TEST_ALLY_0",
            stageKey = quest.stageKey,
            stageNodeIndex = 0,
            currentRouteNodeId = routeNode.id,
            routeNodes = new List<QuestRouteNode> { routeNode }
        };

        UIManager.Instance?.questNodeMapPanel?.Close();

        GameManager.Instance.ResetRoster();

        if (!SpawnTestParty(playerData))
            return;

        List<int> monsterRoleIds = BuildEnemyRoleIds();

        if (monsterRoleIds.Count == 0)
        {
            Debug.LogError("[TestScene] 던전용 테스트 몬스터를 찾지 못했습니다.");
            return;
        }

        foreach (int roleId in monsterRoleIds)
        {
            quest.enemyPool.Add(new MonsterSpawn
            {
                monsterUid = roleId,
                count = 1
            });
        }

        GameManager.Instance.StartEncounterBattle(monsterRoleIds);
        Debug.Log($"[TestScene] {quest.title} 전투 시작 - 아군 4명 / 적군 {monsterRoleIds.Count}명");
    }

    private void EnsureTestInfrastructure()
    {
        if (GameManager.Instance == null && gameManagerPrefab != null)
            Instantiate(gameManagerPrefab);

        if (SpawnPointManager.Instance == null && spawnPointManagerPrefab != null)
            Instantiate(spawnPointManagerPrefab);

        if (UIManager.Instance == null && uiManagerPrefab != null)
        {
            GameObject uiRoot = Instantiate(uiManagerPrefab);

            if (uiRoot.transform.localScale == Vector3.zero)
                uiRoot.transform.localScale = Vector3.one;
        }

        if (GameManager.Instance != null && enemyCombatPrefab != null)
            GameManager.Instance.enemyPrefab = enemyCombatPrefab;
    }

    private QuestDef BuildTestQuest()
    {
        List<MonsterRoleSO> bosses = registryTester.GetMonsterRoles(CharacterType.Boss);

        return new QuestDef
        {
            id = "TEST_DUNGEON_COMBAT",
            seed = randomSeed,
            tier = questTier,
            difficulty = questDifficulty,
            stageKey = registryTester.DungeonStage.stageKey,
            mapPrefabIndex = -1,
            issuer = QuestIssuer.WarriorGuild,
            bossUid = bosses.Count > 0 ? bosses[0].id : 0,
            title = "테스트 던전 언데드 토벌",
            recommendedLevel = questLevel,
            recommendedPartySize = 4,
            maxPartySize = 4
        };
    }

    private bool SpawnTestParty(PlayerData playerData)
    {
        IReadOnlyList<OriginDefinitionSO> origins = registryTester.PartyOrigins;

        if (origins == null || origins.Count != 4)
        {
            Debug.LogError("[TestScene] 방랑기사, 마법사, 사냥꾼, 도적 출신지를 4개 모두 지정해야 합니다.");
            return false;
        }

        List<CharacterData> frontCharacters = new();
        List<CharacterData> backCharacters = new();

        for (int i = 0; i < origins.Count; i++)
        {
            OriginDefinitionSO origin = origins[i];

            if (origin == null)
                return false;

            CharacterData character = origin.CreateRuntimeCharacterData();
            character.ID = $"TEST_ALLY_{i}";
            character.Name = origin.originName;
            character.Level = questLevel;
            character.Belonging = 100;
            character.Morale = 100;
            character.IsAlive = true;
            character.IsMine = true;
            character.customizationData = new CustomizationData
            {
                IsMale = true,
                GenderId = 1
            };
            character.UpdateFinalStats();
            character.CurrentHp = character.FinalStats.MaxHp;
            character.CurrentStamina = character.FinalStats.MaxStamina;
            character.CurrentMentality = character.FinalStats.MaxMentality;

            bool isFront = origin.id == 1000 || origin.id == 1004;

            if (isFront)
                frontCharacters.Add(character);
            else
                backCharacters.Add(character);

            playerData.characterIds.Add(character.ID);
            playerData.activeCharacterIds.Add(character.ID);
            playerData.SetPosition(character.ID, isFront);
            QuestManager.Instance.active.partyCharacterIds.Add(character.ID);
        }

        Transform[] spawnPoints = SpawnPointManager.Instance.GetAllySpawnPoints(
            frontCharacters.Count,
            backCharacters.Count);

        int spawnIndex = 0;

        foreach (CharacterData character in frontCharacters)
        {
            if (!SpawnAlly(character, spawnPoints[spawnIndex++]))
                return false;
        }

        foreach (CharacterData character in backCharacters)
        {
            if (!SpawnAlly(character, spawnPoints[spawnIndex++]))
                return false;
        }

        return true;
    }

    private bool SpawnAlly(CharacterData character, Transform spawnPoint)
    {
        GameObject prefab = allyCombatPrefab != null
            ? allyCombatPrefab
            : GameManager.Instance.characterPrefab_M;

        if (prefab == null)
        {
            Debug.LogError("[TestScene] 테스트용 아군 전투 프리팹이 지정되지 않았습니다.");
            return false;
        }

        GameObject instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        CharacterManager characterManager = instance.GetComponent<CharacterManager>();

        if (characterManager == null)
        {
            Debug.LogError("[TestScene] 아군 프리팹에 CharacterManager가 없습니다.");
            Destroy(instance);
            return false;
        }

        characterManager.InitializeCharacter(character);
        characterManager.battlePresentationHandler?.BindVisual(
            characterManager.transform,
            GameManager.Instance.humanoidCombatController,
            true);
        GameManager.Instance.RegisterCharacter(characterManager);
        return true;
    }

    private List<int> BuildEnemyRoleIds()
    {
        List<MonsterRoleSO> normalRoles = registryTester.GetMonsterRoles(CharacterType.Normal);
        List<MonsterRoleSO> specialRoles = testNodeType == QuestRouteNodeType.Boss
            ? registryTester.GetMonsterRoles(CharacterType.Boss)
            : testNodeType == QuestRouteNodeType.Elite
                ? registryTester.GetMonsterRoles(CharacterType.Elite)
                : new List<MonsterRoleSO>();

        List<int> result = new();
        System.Random random = new System.Random(randomSeed);

        if (specialRoles.Count > 0)
            result.Add(specialRoles[random.Next(0, specialRoles.Count)].id);

        while (result.Count < enemyCount && normalRoles.Count > 0)
            result.Add(normalRoles[random.Next(0, normalRoles.Count)].id);

        return result;
    }
}
