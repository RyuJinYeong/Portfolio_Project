using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static PlayerManager _instance;

    // 현재 로그인한 플레이어 정보
    private PlayerData currentPlayerData;

    public List<CharacterManager> Characters = new List<CharacterManager>();

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 싱글톤 인스턴스를 반환하는 프로퍼티
    public static PlayerManager Instance
    {
        get
        {
            // 인스턴스가 없는 경우에만 생성
            if (_instance == null)
            {
                // 씬에서 PlayerManager 오브젝트를 찾음
                _instance = FindObjectOfType<PlayerManager>();

                // 씬에 PlayerManager 오브젝트가 없는 경우에는 새로 생성
                if (_instance == null)
                {
                    GameObject obj = new GameObject("PlayerManager");
                    _instance = obj.AddComponent<PlayerManager>();
                }
            }
            return _instance;
        }
    }

    // 플레이어 로그인 후 데이터를 불러오는 메서드
    public void LoadPlayerDataFromPlayFab()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnDataError);
    }

    private void OnDataReceived(GetUserDataResult result)
    {
        try
        {
            var data = result.Data;

            if (data != null && data.TryGetValue("PlayerData", out var rec) && !string.IsNullOrEmpty(rec.Value))
            {
                var dto = JsonConvert.DeserializeObject<PlayerSaveDTO>(rec.Value);

                _instance.currentPlayerData = SaveMapper.FromDto(dto);
                Debug.Log("Player data loaded.");

                if (dto.questState != null)
                {
                    SaveMapper.FromDto(dto.questState);
                }
                else
                {
                    QuestManager.Instance?.GenerateBoardIfEmpty(8);
                }
            }
            else
            {
                _instance.currentPlayerData = new PlayerData();
                Debug.LogWarning("No player data found, initializing new player data.");

                QuestManager.Instance?.GenerateBoardIfEmpty(8);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"OnDataReceived parse error: {e}");
            _instance.currentPlayerData = new PlayerData();
            QuestManager.Instance?.GenerateBoardIfEmpty(8);
        }
    }

    private void OnDataError(PlayFabError error)
    {
        Debug.LogError("Error loading player data: " + error.GenerateErrorReport());
        // 클라이언트에 네트워크 에러 UI 출력 구현 예정
    }

    // 플레이어 데이터를 PlayFab에 저장하는 메서드
    public void SavePlayerDataToPlayFab()
    {
        var dto = SaveMapper.ToDto(_instance.currentPlayerData);
        string jsonData = JsonConvert.SerializeObject(dto);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "PlayerData", jsonData } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("Player data (V2) saved."),
            error => Debug.LogError("Error saving player data: " + error.GenerateErrorReport())
        );
    }

    public void SetCurrentPlayerData(PlayerData playerData)
    {
        _instance.currentPlayerData = playerData;
    }

    public PlayerData GetCurrentPlayerData()
    {
        return _instance.currentPlayerData;
    }

    public void AddCharacterID(string characterID)
    {
        if (_instance.currentPlayerData != null)
        {
            currentPlayerData.characterIds.Add(characterID);
            SavePlayerDataToPlayFab();
        }
    }

    //캐릭터 생성 (고유 ID 발급 후 플레이팹에 저장)
    public void CreateCharacter(CharacterData character)
    {
        // 고유 ID 생성
        character.ID = System.Guid.NewGuid().ToString();
        SaveCharacter(character);
    }

    // 캐릭터 데이터를 ID를 키로 플레이팹에 저장
    public void SaveCharacter(CharacterData characterData)
    {
        var dto = SaveMapper.ToDto(characterData);
        string json = JsonConvert.SerializeObject(dto);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { characterData.ID, json } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("Character DTO saved."),
            error => Debug.LogError("Error saving character DTO: " + error.GenerateErrorReport())
        );
    }

    // 캐릭터 데이터를 로드하는 메서드
    public void LoadCharacter(string characterId, Action<CharacterData> onCharacterLoaded)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            var key = characterId;
            if (result.Data != null && result.Data.ContainsKey(key))
            {
                try
                {
                    string json = result.Data[key].Value;
                    var dto = JsonConvert.DeserializeObject<CharacterSaveDTO>(json);
                    var ch = SaveMapper.FromDto(dto);

                    onCharacterLoaded?.Invoke(ch);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogError("Character DTO parse error: " + e);
                }
            }

            Debug.LogError("Character data not found for ID: " + characterId);
            onCharacterLoaded?.Invoke(null);

        }, error =>
        {
            Debug.LogError("Error loading character data: " + error.GenerateErrorReport());
            onCharacterLoaded?.Invoke(null);
        });
    }

    public void SaveCharacterPosition(string characterID, bool isFrontRow)
    {
        if (_instance.currentPlayerData == null)
            return;

        _instance.currentPlayerData.SetPosition(characterID, isFrontRow);
        SavePlayerDataToPlayFab();
    }
}