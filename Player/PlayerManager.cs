using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static PlayerManager _instance;

    // 현재 로그인한 플레이어 정보
    private PlayerData currentPlayerData;

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
        if (result.Data != null && result.Data.ContainsKey("PlayerData"))
        {
            string jsonData = result.Data["PlayerData"].Value;
            Debug.Log("Player data loaded: " + jsonData); // 추가된 디버그 로그
            _instance.currentPlayerData = JsonUtility.FromJson<PlayerData>(jsonData);
            Debug.Log("Player data loaded successfully!");
            Debug.Log(_instance.currentPlayerData.characterIds[0] + " = JsonUtility.FromJson<PlayerData>().characterIds[0]");
        }
        else
        {
            _instance.currentPlayerData = new PlayerData();
            Debug.LogWarning("No player data found, initializing new player data.");
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
        string jsonData = JsonUtility.ToJson(_instance.currentPlayerData);
        Debug.Log("Saving player data: " + jsonData); // 추가된 디버그 로그

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "PlayerData", jsonData }
        }
        };

        PlayFabClientAPI.UpdateUserData(request,
        result => Debug.Log("Player data saved successfully."),
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
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { characterData.ID, JsonHelper.SerializeCharacterData(characterData) }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("Character data saved successfully."),
            error => Debug.LogError("Error saving character data: " + error.GenerateErrorReport())
        );
    }

    // 캐릭터 데이터를 로드하는 메서드
    public void LoadCharacter(string characterId, Action<CharacterData> onCharacterLoaded)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey(characterId))
            {
                string jsonData = result.Data[characterId].Value;
                CharacterData characterData = JsonHelper.DeserializeCharacterData(jsonData);

                if (characterData != null)
                {
                    characterData.InitializeSkills();
                    Debug.Log("Character data loaded successfully!");
                    onCharacterLoaded?.Invoke(characterData); // 로드된 데이터를 콜백으로 전달
                }
                else
                {
                    Debug.LogError("Failed to deserialize character data.");
                    onCharacterLoaded?.Invoke(null); // null을 콜백으로 전달하여 오류 상황 처리
                }
            }
            else
            {
                Debug.LogError("Character data not found for ID: " + characterId);
                onCharacterLoaded?.Invoke(null); // null을 콜백으로 전달하여 오류 상황 처리
            }
        },
        error =>
        {
            Debug.LogError("Error loading character data: " + error.GenerateErrorReport());
            onCharacterLoaded?.Invoke(null); // null을 콜백으로 전달하여 오류 상황 처리
        });
    }

    public void SaveCharacterPosition(string characterID, bool isFrontRow)
    {
        if (_instance.currentPlayerData != null)
        {
            _instance.currentPlayerData.characterPositionMapping[characterID] = isFrontRow;
            SavePlayerDataToPlayFab();
        }
    }
}