using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using SoftKitty.InventoryEngine;
using System;

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
                
        EquipmentDatabase.InitializeDatabase();
        SkillDatabase.InitializeDatabase();

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
        if (pd == null) yield break;

        // 이전/다음 그룹 계산 (그룹=스테이지명)
        string oldGroup = pd.currentStage; // 캡처
        string nextGroup = stageType;

        // 스테이지 상태 업데이트
        pd.currentStage = stageType;

        // 아이콘 캐시: 이전 그룹 정리 → 이번 스테이지 프리로드
        if (!string.IsNullOrEmpty(oldGroup) && oldGroup != nextGroup)
            IconStore.ClearGroup(oldGroup);

        // 어떤 캐릭터들의 스킬을 프리로드할지 스테이지 정책으로 결정
        var targetIds = StagePreloadPolicy.GetPreloadCharacterIds(pd, stageType);

        // 아이콘 주소 수집 → 프리로드
        List<string> keys = null;
        bool done = false;
        PlayerManager.Instance.GetSkillIconAddressesBulk(targetIds, list => { keys = list ?? new List<string>(); done = true; });
        while (!done) yield return null;

        // 배경(스테이지) 토글
        StageManager.Instance.SetActiveStage(stageType);

        // 마을이면 유닛 정리, 전투면 스폰
        if (stageType == "Town")
        {
            ClearSpawnedUnits();
            yield break;
        }

        SpawnSortie(pd); // 스폰 로직
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

        // 포메이션 로직 재사용
        var map = pd.characterPositionMapping;
        int front = 0, back = 0;
        foreach (var id in pd.activeCharacterIds)
            if (map.TryGetValue(id, out var isFront)) { if (isFront) front++; else back++; }

        var allySpawns = SpawnPointManager.Instance.GetAllySpawnPoints(front, back);
        var enemySpawns = SpawnPointManager.Instance.GetEnemySpawnPoints(front, back);

        int fIdx = 0, bIdx = 0;

        foreach (var cid in pd.activeCharacterIds)
        {
            PlayerManager.Instance.LoadCharacter(cid, ch =>
            {
                if (ch == null) return;

                // 혹시 누락된 경우 캐시에서 세팅
                if (ch.Skills != null) foreach (var s in ch.Skills) s.EnsureIconFromCache();
                if (ch.DefaultCounterSkill != null) ch.DefaultCounterSkill.EnsureIconFromCache();

                bool isFront = map.TryGetValue(cid, out var f) && f;
                var spawn = isFront ? allySpawns[fIdx++] : allySpawns[front + bIdx++];

                SpawnCharacter(ch, spawn);
            });
        }
    }

    // 필요 시, 새 적/상인 등장할 때 추가 아이콘 온디맨드
    public IEnumerator PreloadExtraStageIcons(IEnumerable<int> skillUids)
    {
        var keys = new List<string>();
        foreach (var uid in skillUids)
            if (ItemManager.itemDic.TryGetValue(uid, out var it) && it is SkillBase sb && !string.IsNullOrEmpty(sb.IconAddress))
                keys.Add(sb.IconAddress);

        if (keys.Count > 0)
            yield return IconStore.Preload(keys, "Stage");
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
        // 한 프레임 대기
        yield return null;


        // 오브젝트 풀링
        bool done = false;
        CharacterPoolManager.Instance.BuildPoolFromPlayerData(() => done = true);
        while (!done) yield return null;


        // 아이콘 프리로드
        var pd = PlayerManager.Instance.GetCurrentPlayerData();
        var targetIds = StagePreloadPolicy.GetPreloadCharacterIds(pd, stage);

        List<string> keys = null;
        bool addrDone = false;
        PlayerManager.Instance.GetSkillIconAddressesBulk(targetIds, list => { keys = list ?? new List<string>(); addrDone = true; });
        while (!addrDone) yield return null;

        if (keys != null && keys.Count > 0)
        {         
            yield return IconStore.Preload(keys, stage);         
        }

        foreach (var cm in CharacterPoolManager.Instance.All())
        {
            IconFixer.FixAllIcons(cm.character);
        }

        // 풀 보장 후 게임씬 셋업
        StartCoroutine(SetupGameScene(stage));
    }

    public IEnumerator SetupGameScene(string stage)
    {
        yield return new WaitForSeconds(1); // 씬 로드 시간 대기
        
        var playerData = PlayerManager.Instance.GetCurrentPlayerData();

        if (playerData == null)
        {
            Debug.LogError("Player data is null.");
            yield break;
        }

        playerData.currentStage = stage;

        if (string.IsNullOrEmpty(playerData.currentStage) || playerData.activeCharacterIds == null || playerData.activeCharacterIds.Count == 0)
        {
            Debug.LogWarning("No active characters or current stage is null.");
            yield break;
        }

        StageManager.Instance.SetActiveStage(playerData.currentStage);

        allCharacters.Clear();

        var characterPositionMapping = playerData.characterPositionMapping;
        var activeCharacterIds = playerData.activeCharacterIds;

        int frontCount = 0;
        int backCount = 0;

        int pending = activeCharacterIds.Count;

        foreach (var characterId in activeCharacterIds)
        {
            if (characterPositionMapping.ContainsKey(characterId))
            {
                if (characterPositionMapping[characterId])
                {
                    frontCount++;
                }
                else
                {
                    backCount++;
                }
            }
        }

        var allySpawnPoints = SpawnPointManager.Instance.GetAllySpawnPoints(frontCount, backCount);
        var enemySpawnPoints = SpawnPointManager.Instance.GetEnemySpawnPoints(frontCount, backCount);

        int frontIndex = 0;
        int backIndex = 0;

        foreach (var characterId in activeCharacterIds)
        {
            PlayerManager.Instance.LoadCharacter(characterId, characterData =>
            {
                try
                {
                    if (characterData != null)
                    {
                        Transform point;
                        if (characterPositionMapping[characterId])
                            point = allySpawnPoints[frontIndex++];
                        else
                            point = allySpawnPoints[backIndex++ + frontCount];

                        SpawnCharacter(characterData, point);
                    }
                }
                finally
                {
                    pending--;
                    // 전원 처리되면 Turn 시작 신호 
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

        GameObject gameObject = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
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
