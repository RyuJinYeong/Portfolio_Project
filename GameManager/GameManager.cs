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
        StartCoroutine(CoSwitchStage(stageType));
    }

    IEnumerator CoSwitchStage(string stageType)
    {
        var pd = PlayerManager.Instance.GetCurrentPlayerData();
        if (pd == null)
            yield break;

        pd.currentStage = stageType;

        StageManager.Instance.SetActiveStage(stageType);

        if (stageType == "Town")
        {
            ClearSpawnedUnits();
            yield break;
        }

        SpawnSortie(pd);
    }

    void ClearSpawnedUnits()
    {
        foreach (var cm in allCharacters)
            if (cm) Destroy(cm.gameObject);
        allCharacters.Clear();
    }

    void SpawnSortie(PlayerData pd)
    {
        if (pd.activeCharacterIds == null || pd.activeCharacterIds.Count == 0)
        {
            Debug.LogWarning("No active characters for stage.");
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

        foreach (string cid in pd.activeCharacterIds)
        {
            string characterId = cid;

            PlayerManager.Instance.LoadCharacter(characterId, ch =>
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

        StageManager.Instance.SetActiveStage(playerData.currentStage);

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
                        SignalRosterReady();
                }
            });
        }
    }


    // 아군 스폰 메서드 - 아군과 적군 스폰 메서드를 분리하지 말고 하나로 통합하고 isMine 변수를 통해서 아군 적군 여부를 구분, 캐릭터 매니저를 매개변수로 전달 받기.
    private void SpawnCharacter(CharacterData characterData, Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not found.");
            return;
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
            RegisterCharacter(characterManager);
        }
        else
        {
            Debug.LogError("CharacterManager component is missing on the character prefab.");
        }
    }

    // 적군 스폰 (등록 포함)
    private void SpawnEnemy(CharacterData enemyData, Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not found.");
            return;
        }

        MonsterRoleSO monsterRole = GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetMonsterRole(enemyData.monsterRoleId)
            : null;

        GameObject prefab = monsterRole != null && monsterRole.modelPrefabOverride != null
            ? monsterRole.modelPrefabOverride
            : monsterRole != null && monsterRole.baseMonster != null && monsterRole.baseMonster.modelPrefab != null
                ? monsterRole.baseMonster.modelPrefab
                : enemyPrefab;

        if (prefab == null)
        {
            Debug.LogError($"Monster prefab not found. monsterRoleId: {enemyData.monsterRoleId}");
            return;
        }

        GameObject gameObject = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        var characterManager = gameObject.GetComponent<CharacterManager>();
        if (characterManager != null)
        {
            characterManager.InitializeCharacter(enemyData);
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
