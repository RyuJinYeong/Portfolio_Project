using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject characterPrefab_M;
    public GameObject characterPrefab_F;
    public GameObject enemyPrefab;
    public RuntimeAnimatorController humanoidCombatController;

    // 필드의 모든 캐릭터를 관리하는 리스트
    public List<CharacterManager> allCharacters = new List<CharacterManager>();

    public event Action RosterReady;

    private void Awake()
    {
        if (Instance == null)
        { 
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        SetDefaultCursor();        
    }
    public void RegisterCharacter(CharacterManager cm)
    {
        if (cm == null) return;

        // 이미 목록에 있으면 스킵
        if (allCharacters.Contains(cm))
        {
            Debug.Log($"[GM] 중복 등록 스킵: {cm.name} ({cm.GetInstanceID()})");
            return;
        }

        allCharacters.Add(cm);
        Debug.Log($"[GM] 캐릭터 등록: {cm.name} ({cm.GetInstanceID()}) / 총 {allCharacters.Count}");
    }

    public void UnregisterCharacter(CharacterManager cm)
    {
        if (cm == null) return;
        if (allCharacters.Remove(cm))
            Debug.Log($"[GM] 캐릭터 해제: {cm.name} ({cm.GetInstanceID()}) / 총 {allCharacters.Count}");
    }

    // 테스트/전투 시작 전에 깨끗하게
    public void ResetRoster()
    {
        allCharacters.RemoveAll(c => c == null);
        allCharacters.Clear();
        Debug.Log("[GM] 로스터 초기화");
    }

    // 전투 참가자 등록 신호
    public void SignalRosterReady()
    {
        // 혹시 비었으면 아무 것도 하지 않음
        if (allCharacters == null || allCharacters.Count == 0) return;

        Debug.Log("로스터 준비 완료");
        // TurnManager에게 통보
        RosterReady?.Invoke();
    }

    private void SetDefaultCursor()
    {
        Texture2D cursorTexture = Resources.Load<Texture2D>("Cursor/Cursor_Basic");
        Vector2 cursorHotspot = new Vector2(cursorTexture.width * 0.3f, 0);
        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
    }

    // 스테이지 전환 + 스킬 아이콘 로드 
    public void SwitchStage(string stageType)
    {
        StartCoroutine(CoSwitchStage(stageType, true));
    }

    public void ReturnToTownAfterQuestAbandon()
    {
        StartCoroutine(CoSwitchStage("Town", false));
    }

    public void EnterQuestRouteNode(QuestRouteNode node)
    {
        ActiveQuestRuntime activeQuest = QuestManager.Instance != null
            ? QuestManager.Instance.active
            : null;

        if (node == null || activeQuest == null || activeQuest.def == null)
            return;

        SwitchStage(activeQuest.stageKey);
    }

    IEnumerator CoSwitchStage(
        string stageType,
        bool claimCompletedQuestRewards)
    {
        var pd = PlayerManager.Instance.GetCurrentPlayerData();
        if (pd == null)
            yield break;

        pd.currentStage = stageType;

        if (stageType == "Town")
            RecoverReturningExpeditionHealth(pd);

        ClearSpawnedUnits();
        ApplyStage(stageType);

        if (stageType == "Town")
        {
            yield return null;

            SharedInventoryUtility.TransferExpeditionToCompany(pd);

            if (claimCompletedQuestRewards)
                QuestManager.Instance?.ClaimCompletedRewards(pd);

            MercenaryGenerator.ResetRecruitmentRefreshCost(pd);
            MercenaryGenerator.RefreshRecruitmentCandidates(pd);

            PlayerManager.Instance?.SavePlayerDataToPlayFab();

            yield break;
        }

        SpawnSortie(pd, HandleCurrentRouteNodeReady);
    }

    private void RecoverReturningExpeditionHealth(PlayerData playerData)
    {
        if (playerData?.activeCharacterIds == null)
            return;

        foreach (string characterId in playerData.activeCharacterIds)
        {
            if (string.IsNullOrEmpty(characterId))
                continue;

            CharacterManager battleCharacter = allCharacters.Find(manager =>
                manager != null &&
                manager.character != null &&
                manager.character.ID == characterId);
            CharacterManager pooledCharacter = CharacterPoolManager.Instance != null
                ? CharacterPoolManager.Instance.Get(characterId)
                : null;
            CharacterData character = battleCharacter != null
                ? battleCharacter.character
                : pooledCharacter != null
                    ? pooledCharacter.character
                    : null;

            if (character == null || !character.IsAlive || character.FinalStats == null)
                continue;

            character.CurrentHp = character.FinalStats.MaxHp;

            if (pooledCharacter != null &&
                pooledCharacter.character != null &&
                pooledCharacter.character != character &&
                pooledCharacter.character.FinalStats != null)
            {
                pooledCharacter.character.CurrentHp = pooledCharacter.character.FinalStats.MaxHp;
                pooledCharacter.UpdateCharacterUI();
            }

            battleCharacter?.UpdateCharacterUI();
            PlayerManager.Instance?.SaveCharacter(character);
        }
    }

    void ClearSpawnedUnits()
    {
        foreach (var cm in allCharacters)
            if (cm) Destroy(cm.gameObject);
        allCharacters.Clear();
    }

    void SpawnSortie(PlayerData pd, Action onCompleted = null)
    {
        if (pd.activeCharacterIds == null || pd.activeCharacterIds.Count == 0)
        {
            Debug.LogWarning("No active characters for stage.");
            onCompleted?.Invoke();
            return;
        }

        int front = 0;
        int back = 0;

        foreach (string id in pd.activeCharacterIds)
        {
            if (!pd.TryGetPosition(id, out bool isFront))
                continue;

            if (isFront)
                front++;
            else
                back++;
        }

        Transform[] allySpawns = SpawnPointManager.Instance.GetAllySpawnPoints(front, back);

        int fIdx = 0;
        int bIdx = 0;
        int pending = pd.activeCharacterIds.Count;

        foreach (string cid in pd.activeCharacterIds)
        {
            string characterId = cid;

            PlayerManager.Instance.LoadCharacter(characterId, ch =>
            {
                try
                {
                    if (ch == null)
                        return;

                    bool isFront = pd.TryGetPosition(characterId, out bool f) && f;

                    int spawnIndex = isFront
                        ? fIdx++
                        : front + bIdx++;

                    if (spawnIndex < 0 || spawnIndex >= allySpawns.Length)
                    {
                        Debug.LogError($"Spawn index out of range. characterId: {characterId}, index: {spawnIndex}, spawnCount: {allySpawns.Length}");
                        return;
                    }

                    Transform spawn = allySpawns[spawnIndex];
                    SpawnCharacter(ch, spawn);
                }
                finally
                {
                    pending--;

                    if (pending == 0)
                        onCompleted?.Invoke();
                }
            });
        }
    }

    public void ShowStageSelectionUI()
    {

        // 스테이지 선택 UI를 표시하는 로직 구현
    }

    public void LoadGameScene(string stage)
    {
        SceneManager.LoadScene("GameScene");
        StartCoroutine(CoAfterSceneLoaded(stage));
    }

    private IEnumerator CoAfterSceneLoaded(string stage)
    {
        yield return null;

        bool done = false;
        CharacterPoolManager.Instance.BuildPoolFromPlayerData(() => done = true);

        while (!done)
            yield return null;

        StartCoroutine(SetupGameScene(stage));
    }

    public IEnumerator SetupGameScene(string stage)
    {
        yield return new WaitForSeconds(1);

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();

        if (playerData == null)
        {
            Debug.LogError("Player data is null.");
            yield break;
        }

        playerData.currentStage = stage;

        if (string.IsNullOrEmpty(playerData.currentStage) ||
            playerData.activeCharacterIds == null ||
            playerData.activeCharacterIds.Count == 0)
        {
            Debug.LogWarning("No active characters or current stage is null.");
            yield break;
        }

        ApplyStage(playerData.currentStage);

        ActiveQuestRuntime activeQuest = QuestManager.Instance != null
            ? QuestManager.Instance.active
            : null;
        QuestRouteNode currentRouteNode = activeQuest != null
            ? QuestManager.Instance.GetCurrentRouteNode()
            : null;

        if (activeQuest != null &&
            activeQuest.status == QuestStatus.Active &&
            currentRouteNode != null &&
            currentRouteNode.cleared)
        {
            UIManager.Instance?.OpenQuestNodeMap();
            yield break;
        }

        allCharacters.Clear();

        List<string> activeCharacterIds = playerData.activeCharacterIds;

        int frontCount = 0;
        int backCount = 0;

        foreach (string characterId in activeCharacterIds)
        {
            if (!playerData.TryGetPosition(characterId, out bool isFront))
                continue;

            if (isFront)
                frontCount++;
            else
                backCount++;
        }

        Transform[] allySpawnPoints = SpawnPointManager.Instance.GetAllySpawnPoints(frontCount, backCount);

        int frontIndex = 0;
        int backIndex = 0;
        int pending = activeCharacterIds.Count;

        foreach (string id in activeCharacterIds)
        {
            string characterId = id;

            PlayerManager.Instance.LoadCharacter(characterId, characterData =>
            {
                try
                {
                    if (characterData == null)
                        return;

                    bool isFront = playerData.TryGetPosition(characterId, out bool frontPosition) && frontPosition;

                    int spawnIndex = isFront
                        ? frontIndex++
                        : frontCount + backIndex++;

                    if (spawnIndex < 0 || spawnIndex >= allySpawnPoints.Length)
                    {
                        Debug.LogError($"Spawn index out of range. characterId: {characterId}, index: {spawnIndex}, spawnCount: {allySpawnPoints.Length}");
                        return;
                    }

                    Transform point = allySpawnPoints[spawnIndex];
                    SpawnCharacter(characterData, point);
                }
                finally
                {
                    pending--;

                    if (pending == 0)
                        HandleCurrentRouteNodeReady();
                }
            });
        }
    }

    private void HandleCurrentRouteNodeReady()
    {
        QuestRouteNode routeNode = QuestManager.Instance != null
            ? QuestManager.Instance.GetCurrentRouteNode()
            : null;

        if (routeNode == null)
        {
            SignalRosterReady();
            return;
        }

        switch (routeNode.type)
        {
            case QuestRouteNodeType.RandomEncounter:
                if (routeNode.encounterResolved &&
                    routeNode.encounterMonsterRoleIds != null &&
                    routeNode.encounterMonsterRoleIds.Count > 0)
                {
                    StartEncounterBattle(routeNode.encounterMonsterRoleIds);
                }
                else
                {
                    UIManager.Instance?.OpenQuestEncounter();
                }
                break;

            case QuestRouteNodeType.Rest:
                UIManager.Instance?.OpenQuestEncounter();
                break;

            case QuestRouteNodeType.Battle:
            case QuestRouteNodeType.Elite:
            case QuestRouteNodeType.Boss:
                StartQuestNodeBattle(routeNode);
                break;

            default:
                SignalRosterReady();
                break;
        }
    }

    private void StartQuestNodeBattle(QuestRouteNode routeNode)
    {
        QuestDef quest = QuestManager.Instance != null && QuestManager.Instance.active != null
            ? QuestManager.Instance.active.def
            : null;

        if (quest == null || routeNode == null || GameDataRegistry.Instance == null)
        {
            SignalRosterReady();
            return;
        }

        List<int> normalMonsterIds = new List<int>();
        List<int> eliteMonsterIds = new List<int>();

        if (quest.enemyPool != null)
        {
            foreach (MonsterSpawn spawn in quest.enemyPool)
            {
                if (spawn == null || spawn.count <= 0 || spawn.monsterUid == quest.bossUid)
                    continue;

                MonsterRoleSO role = GameDataRegistry.Instance.GetMonsterRole(spawn.monsterUid);

                if (role == null)
                    continue;

                List<int> target = role.characterType == CharacterType.Elite
                    ? eliteMonsterIds
                    : role.characterType == CharacterType.Normal
                        ? normalMonsterIds
                        : null;

                if (target == null)
                    continue;

                for (int i = 0; i < spawn.count; i++)
                    target.Add(spawn.monsterUid);
            }
        }

        QuestStageDefinitionSO stage = GameDataRegistry.Instance.GetQuestStage(quest.stageKey);

        if (normalMonsterIds.Count == 0)
        {
            foreach (MonsterRoleSO role in GameDataRegistry.Instance.GetMonsterRolesByTypeAndTags(
                         CharacterType.Normal,
                         stage != null ? stage.monsterTags : null))
            {
                if (role != null)
                    normalMonsterIds.Add(role.id);
            }
        }

        if (routeNode.type == QuestRouteNodeType.Elite && eliteMonsterIds.Count == 0)
        {
            foreach (MonsterRoleSO role in GameDataRegistry.Instance.GetMonsterRolesByTypeAndTags(
                         CharacterType.Elite,
                         stage != null ? stage.monsterTags : null))
            {
                if (role != null)
                    eliteMonsterIds.Add(role.id);
            }
        }

        System.Random random = new System.Random(quest.seed ^ (routeNode.id * 397));
        int recommendedPartySize = Mathf.Clamp(quest.recommendedPartySize, 1, 4);
        int enemyCount = Mathf.Min(5, recommendedPartySize + random.Next(0, 2));
        List<int> monsterRoleIds = new List<int>();

        if (routeNode.type == QuestRouteNodeType.Boss && quest.bossUid != 0 &&
            GameDataRegistry.Instance.GetMonsterRole(quest.bossUid) != null)
        {
            monsterRoleIds.Add(quest.bossUid);
        }
        else if (routeNode.type == QuestRouteNodeType.Elite && eliteMonsterIds.Count > 0)
        {
            monsterRoleIds.Add(eliteMonsterIds[random.Next(0, eliteMonsterIds.Count)]);
        }

        while (monsterRoleIds.Count < enemyCount && normalMonsterIds.Count > 0)
            monsterRoleIds.Add(normalMonsterIds[random.Next(0, normalMonsterIds.Count)]);

        if (monsterRoleIds.Count == 0)
        {
            Debug.LogError($"No monsters available for quest node. stageKey: {quest.stageKey}, nodeType: {routeNode.type}");
            SignalRosterReady();
            return;
        }

        StartEncounterBattle(monsterRoleIds);
    }

    public void StartEncounterBattle(List<int> monsterRoleIds)
    {
        if (monsterRoleIds == null || monsterRoleIds.Count == 0 ||
            SpawnPointManager.Instance == null || GameDataRegistry.Instance == null)
        {
            return;
        }

        QuestRouteNode currentNode = QuestManager.Instance != null
            ? QuestManager.Instance.GetCurrentRouteNode() : null;
        if (currentNode != null && currentNode.type == QuestRouteNodeType.RandomEncounter)
        {
            StageManager.Instance.ActivateEncounterBattle();
            List<CharacterManager> allies = allCharacters
                .Where(c => c != null && c.character != null && c.character.IsMine).ToList();
            int frontCount = allies.Count(c => c.isFront);
            Transform[] allyPoints = SpawnPointManager.Instance.GetAllySpawnPoints(frontCount, allies.Count - frontCount);
            int frontIndex = 0;
            int backIndex = frontCount;
            foreach (CharacterManager ally in allies)
            {
                Transform point = allyPoints[ally.isFront ? frontIndex++ : backIndex++];
                ally.transform.SetPositionAndRotation(point.position, point.rotation);
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.questNodeMapPanel?.Close();
            UIManager.Instance.UISwitch(UIMode.Battle);
        }

        int spawnCount = Mathf.Min(5, monsterRoleIds.Count);
        int encounterSeed = QuestManager.Instance != null &&
                            QuestManager.Instance.active != null &&
                            QuestManager.Instance.active.def != null
            ? QuestManager.Instance.active.def.seed
            : 0;
        int formationSeed = encounterSeed +
                            (currentNode != null ? currentNode.id * 31 : 0) +
                            7919;
        System.Random formationRandom = new System.Random(formationSeed);
        List<int> frontRoleIds = new List<int>();
        List<int> backRoleIds = new List<int>();

        for (int i = 0; i < spawnCount; i++)
        {
            int roleId = monsterRoleIds[i];
            MonsterRoleSO role = GameDataRegistry.Instance.GetMonsterRole(roleId);

            if (ShouldSpawnMonsterInFront(role, formationRandom))
                frontRoleIds.Add(roleId);
            else
                backRoleIds.Add(roleId);
        }

        Transform[] spawnPoints = SpawnPointManager.Instance.GetEnemySpawnPoints(
            frontRoleIds.Count,
            backRoleIds.Count);
        List<int> arrangedRoleIds = new List<int>(frontRoleIds);
        arrangedRoleIds.AddRange(backRoleIds);
        int questLevel = QuestManager.Instance != null && QuestManager.Instance.active != null &&
                         QuestManager.Instance.active.def != null
            ? QuestManager.Instance.active.def.recommendedLevel
            : 1;

        for (int i = 0; i < arrangedRoleIds.Count && i < spawnPoints.Length; i++)
        {
            MonsterRoleSO role = GameDataRegistry.Instance.GetMonsterRole(arrangedRoleIds[i]);
            CharacterData monster = MonsterGenerator.Generate(
                role,
                questLevel,
                QuestManager.Instance != null && QuestManager.Instance.active != null &&
                QuestManager.Instance.active.def != null
                    ? QuestManager.Instance.active.def.seed + i + 1
                    : 0);

            if (monster != null)
                SpawnEnemy(monster, spawnPoints[i], i < frontRoleIds.Count);
        }

        SignalRosterReady();
    }

    private static bool ShouldSpawnMonsterInFront(
        MonsterRoleSO role,
        System.Random random)
    {
        if (role == null || role.skillUids == null || GameDataRegistry.Instance == null)
            return true;

        int meleeSkillCount = 0;
        int rangedSkillCount = 0;

        foreach (int skillUid in role.skillUids)
        {
            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(skillUid);

            if (skill == null || skill.isCounterSkill)
                continue;

            if (skill.isRangedSkill)
                rangedSkillCount++;
            else
                meleeSkillCount++;
        }

        if (rangedSkillCount > meleeSkillCount)
            return false;

        if (meleeSkillCount > rangedSkillCount || meleeSkillCount == 0)
            return true;

        return random == null || random.Next(0, 2) == 0;
    }

    private void ApplyStage(string stageType)
    {
        ActiveQuestRuntime activeQuest =
            QuestManager.Instance != null
                ? QuestManager.Instance.active
                : null;

        if (activeQuest != null &&
            activeQuest.status == QuestStatus.Active &&
            activeQuest.def != null &&
            activeQuest.stageKey == stageType)
        {
            QuestRouteNode routeNode = QuestManager.Instance.GetCurrentRouteNode();
            int mapPrefabIndex = routeNode != null
                ? routeNode.mapPrefabIndex
                : activeQuest.def.mapPrefabIndex;

            StageManager.Instance.SetActiveQuestStage(activeQuest.def, mapPrefabIndex);
            return;
        }

        StageManager.Instance.SetActiveStage(stageType);
    }


    // 아군 스폰 메서드 - 아군과 적군 스폰 메서드를 분리하지 말고 하나로 통합하고 isMine 변수를 통해서 아군 적군 여부를 구분, 캐릭터 매니저를 매개변수로 전달 받기.
    private void SpawnCharacter(CharacterData characterData, Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not found.");
            return;
        }

        CharacterManager pooledCharacter = CharacterPoolManager.Instance != null
            ? CharacterPoolManager.Instance.Get(characterData.ID)
            : null;

        if (characterData.Portrait == null &&
            pooledCharacter != null &&
            pooledCharacter.character != null)
        {
            characterData.Portrait = pooledCharacter.character.Portrait;
        }

        GameObject allyCharacter = new GameObject();

        if (characterData.customizationData.IsMale == true)
            allyCharacter = Instantiate(characterPrefab_M, spawnPoint.position, spawnPoint.rotation);
        else
            allyCharacter = Instantiate(characterPrefab_F, spawnPoint.position, spawnPoint.rotation);

        CharacterManager characterManager = allyCharacter.GetComponent<CharacterManager>();
        if (characterManager != null)
        {
            characterManager.InitializeCharacter(characterData);
            characterManager.battlePresentationHandler?.BindVisual(
                characterManager.transform,
                humanoidCombatController,
                true);
            RegisterCharacter(characterManager);
        }
        else
        {
            Debug.LogError("CharacterManager component is missing on the character prefab.");
        }
    }

    // 적군 스폰 (등록 포함)
    private void SpawnEnemy(CharacterData enemyData, Transform spawnPoint, bool isFront)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not found.");
            return;
        }

        MonsterRoleSO monsterRole = GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetMonsterRole(enemyData.monsterRoleId)
            : null;

        GameObject modelPrefab = monsterRole != null && monsterRole.modelPrefabOverride != null
            ? monsterRole.modelPrefabOverride
            : monsterRole != null && monsterRole.baseMonster != null && monsterRole.baseMonster.modelPrefab != null
                ? monsterRole.baseMonster.modelPrefab
                : null;

        if (enemyPrefab == null)
        {
            Debug.LogError($"Enemy combat prefab not found. monsterRoleId: {enemyData.monsterRoleId}");
            return;
        }

        GameObject gameObject = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        var characterManager = gameObject.GetComponent<CharacterManager>();
        if (characterManager != null)
        {
            characterManager.isFront = isFront;
            characterManager.InitializeCharacter(enemyData);

            if (modelPrefab != null)
            {
                foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;

                GameObject visual = Instantiate(modelPrefab, gameObject.transform);
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                visual.transform.localScale = Vector3.one;

                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;

                Outline outline = gameObject.GetComponent<Outline>();

                if (outline == null)
                    outline = gameObject.AddComponent<Outline>();

                outline.enabled = false;

                characterManager.battlePresentationHandler?.BindVisual(
                    visual.transform,
                    humanoidCombatController,
                    true);

                CharacterPoolManager.Instance?.CaptureMonsterPortrait(
                    enemyData,
                    modelPrefab,
                    () => TurnManager.Instance?.RefreshTurnOrderUI());
            }
            else
            {
                foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;

                Debug.LogWarning($"Monster model prefab not found. monsterRoleId: {enemyData.monsterRoleId}");
            }

            RegisterCharacter(characterManager); 
        }
        else
        {
            Debug.LogError("EnemyManager component is missing on the enemy prefab.");
        }
    }

    // 전투 시작 시, 모든 캐릭터 리스트를 턴 매니저로 넘겨줌
    public List<CharacterManager> GetAllCharacters()
    {
        return allCharacters;
    }
}
