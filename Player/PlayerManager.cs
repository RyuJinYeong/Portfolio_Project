using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using SoftKitty.InventoryEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

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

    public void GetSkillIconAddressesBulk(IEnumerable<string> characterIds, Action<List<string>> onDone)
    {
        var ids = characterIds?.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList() ?? new List<string>();
        if (ids.Count == 0) { onDone?.Invoke(new List<string>()); return; }

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            var keys = new HashSet<string>();

            foreach (var id in ids)
            {
                var key = id;
                if (result.Data == null || !result.Data.TryGetValue(key, out var rec) || string.IsNullOrEmpty(rec.Value))
                    continue;

                try
                {
                    var dto = JsonConvert.DeserializeObject<CharacterSaveDTO>(rec.Value);
                    if (dto?.skills != null)
                        foreach (var s in dto.skills) TryAddSkillIcon(s.uid, keys);
                    if (dto?.defaultCounterSkillUid != 0)
                        TryAddSkillIcon(dto.defaultCounterSkillUid, keys);
                    // 장비 부여 스킬 등을 DTO에 넣었다면 여기도 추가
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"GetSkillIconAddressesBulk: parse error for {id}\n{e}");
                }
            }

            onDone?.Invoke(keys.ToList());
        },
        err =>
        {
            Debug.LogError($"GetSkillIconAddressesBulk: PlayFab error\n{err.GenerateErrorReport()}");
            onDone?.Invoke(new List<string>());
        });
    }

    static void TryAddSkillIcon(int uid, HashSet<string> keys)
    {
        if (uid == 0) return;
        if (ItemManager.itemDic.TryGetValue(uid, out var item) && item is SkillBase sb)
        {
            if (!string.IsNullOrEmpty(sb.IconAddress))
                keys.Add(sb.IconAddress);
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
            if (result.Data != null && result.Data.ContainsKey("PlayerDataV2"))
            {
                string jsonData = result.Data["PlayerDataV2"].Value;
                var dto = JsonConvert.DeserializeObject<PlayerSaveDTO>(jsonData);
                _instance.currentPlayerData = SaveMapper.FromDto(dto);
                Debug.Log("Player data (V2) loaded.");
            }
            else if (result.Data != null && result.Data.ContainsKey("PlayerData"))
            {
                // 구버전 호환(원한다면 마이그레이션 처리)
                string jsonData = result.Data["PlayerData"].Value;
                _instance.currentPlayerData = JsonUtility.FromJson<PlayerData>(jsonData);
                Debug.LogWarning("Legacy PlayerData loaded. Consider migrating to V2.");
            }
            else
            {
                _instance.currentPlayerData = new PlayerData();
                Debug.LogWarning("No player data found, initializing new player data.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"OnDataReceived parse error: {e}");
            _instance.currentPlayerData = new PlayerData();
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
            Data = new Dictionary<string, string> { { "PlayerDataV2", jsonData } }
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
        if (_instance.currentPlayerData != null)
        {
            _instance.currentPlayerData.characterPositionMapping[characterID] = isFrontRow;
            SavePlayerDataToPlayFab();
        }
    }
}