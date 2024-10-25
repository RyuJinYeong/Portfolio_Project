using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject characterPrefab_M;
    public GameObject characterPrefab_F;
    public GameObject enemyPrefab;

    // 필드의 모든 캐릭터를 관리하는 리스트
    public List<CharacterManager> allCharacters = new List<CharacterManager>(); 

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

    private void SetDefaultCursor()
    {
        Texture2D cursorTexture = Resources.Load<Texture2D>("Cursor/Cursor_Basic");
        Vector2 cursorHotspot = new Vector2(0,0); // 마우스 포인터 클릭 지점 좌표 설정
        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
    }

    public void ShowStageSelectionUI()
    {

        // 스테이지 선택 UI를 표시하는 로직 구현
    }

    public void LoadGameScene(string stage)
    {
        SceneManager.LoadScene("GameScene");
        StartCoroutine(SetupGameScene(stage));
    }

    public IEnumerator SetupGameScene(string stage)
    {
        yield return new WaitForSeconds(1); // 씬 로드 시간 대기

        var playerData = PlayerManager.Instance.GetCurrentPlayerData();

        if (playerData == null)
        {
            Debug.LogError("Player data is null.");
            ShowStageSelectionUI();
            yield break;
        }

        playerData.currentStage = stage;

        if (string.IsNullOrEmpty(playerData.currentStage) || playerData.activeCharacterIds == null || playerData.activeCharacterIds.Count == 0)
        {
            Debug.LogWarning("No active characters or current stage is null.");
            ShowStageSelectionUI();
            yield break;
        }

        BackgroundManager.Instance.SetActiveBackground(stage);

        var characterPositionMapping = playerData.characterPositionMapping;
        var activeCharacterIds = playerData.activeCharacterIds;

        int frontCount = 0;
        int backCount = 0;

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
                if (characterData != null)
                {
                    if (characterPositionMapping[characterId])
                    {
                        SpawnCharacter(characterData, allySpawnPoints[frontIndex]);
                        frontIndex++;
                    }
                    else
                    {
                        SpawnCharacter(characterData, allySpawnPoints[backIndex + frontCount]);
                        backIndex++;
                    }
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
        }
        else
        {
            Debug.LogError("CharacterManager component is missing on the character prefab.");
        }
    }

    // 적군 스폰 메서드
    private void SpawnEnemy(CharacterData enemyData, Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not found.");
            return;
        }

        GameObject enemyCharacter = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        CharacterManager enemyCharacterManager = enemyCharacter.GetComponent<CharacterManager>();
        if (enemyCharacterManager != null)
        {
            enemyCharacterManager.InitializeCharacter(enemyData);
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
