using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static PlayerManager _instance;

    // 현재 로그인한 플레이어 정보
    private PlayerData currentPlayerData;
    private readonly HashSet<string> livingCharacterIds = new();
    private LocalPlayerSaveDTO localSave;
    private bool localSaveLoaded;
    private bool recoveredFromBackup;

    public string LocalSavePath => Path.Combine(Application.persistentDataPath, "Saves", "local-player.json");

    public bool HasLivingCharacter => livingCharacterIds.Count > 0;

    [NonSerialized]
    public bool suppressRemotePersistence;

    public List<CharacterManager> Characters = new List<CharacterManager>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

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
                _instance = FindFirstObjectByType<PlayerManager>();

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
        LoadPlayerDataFromPlayFab(null);
    }

    public void LoadPlayerDataFromPlayFab(Action onDataLoaded)
    {
        LoadLocalPlayerData(onDataLoaded);
    }

    public void LoadLocalPlayerData(Action onDataLoaded, Action<string> onError = null)
    {
        try
        {
            if (!localSaveLoaded || suppressRemotePersistence)
            {
                localSaveLoaded = false;
                recoveredFromBackup = false;
                string path = LocalSavePath;
                bool hasSave = File.Exists(path) || File.Exists(path + ".bak");
                if (hasSave)
                {
                    try
                    {
                        localSave = ReadLocalSave(path);
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
                    {
                        if (!File.Exists(path + ".bak"))
                            throw;

                        localSave = ReadLocalSave(path + ".bak");
                        recoveredFromBackup = true;
                        Debug.LogWarning($"Local save recovered from backup: {e.Message}");
                    }

                    currentPlayerData = SaveMapper.FromDto(localSave.player);
                    if (string.IsNullOrEmpty(localSave.player.profileId))
                    {
                        localSave.player.profileId = currentPlayerData.profileId;
                        WriteLocalSave();
                    }
                }
                else
                {
                    localSave = new LocalPlayerSaveDTO();
                    currentPlayerData = new PlayerData { playerName = "나의 용병단" };
                    SaveMapper.FromDto((QuestStateDTO)null);
                    localSave.player = SaveMapper.ToDto(currentPlayerData);
                    WriteLocalSave();
                }

                livingCharacterIds.Clear();
                foreach (string id in currentPlayerData.characterIds)
                {
                    if (localSave.characters.TryGetValue(id, out CharacterSaveDTO character) && character.isAlive)
                        livingCharacterIds.Add(id);
                }

                suppressRemotePersistence = false;
                localSaveLoaded = true;
                Debug.Log($"Local player data loaded: {path}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Local player data load failed. Existing save was not replaced: {e}");
            onError?.Invoke(e.Message);
            return;
        }

        onDataLoaded?.Invoke();
    }

    private LocalPlayerSaveDTO ReadLocalSave(string path)
    {
        LocalPlayerSaveDTO save = JsonConvert.DeserializeObject<LocalPlayerSaveDTO>(File.ReadAllText(path));
        if (save == null || save.v != 1 || save.player == null || save.characters == null)
            throw new JsonSerializationException("Invalid or unsupported local save.");

        foreach (var entry in save.characters)
        {
            if (entry.Value == null || entry.Value.id != entry.Key)
                throw new JsonSerializationException($"Invalid character record: {entry.Key}");
        }

        if (save.player.characterIds != null)
        {
            foreach (string id in save.player.characterIds)
            {
                if (string.IsNullOrEmpty(id) || !save.characters.ContainsKey(id))
                    throw new JsonSerializationException($"Missing character record: {id}");
            }
        }

        return save;
    }

    private void WriteLocalSave()
    {
        string path = LocalSavePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporaryPath = path + ".tmp";
        string json = JsonConvert.SerializeObject(localSave);
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, true))
            {
                writer.Write(json);
                writer.Flush();
            }
            stream.Flush(true);
        }

        if (File.Exists(path))
            File.Replace(temporaryPath, path, recoveredFromBackup ? null : path + ".bak");
        else
            File.Move(temporaryPath, path);

        recoveredFromBackup = false;
    }

    public void SavePlayerDataToPlayFab()
    {
        if (suppressRemotePersistence)
            return;

        if (!localSaveLoaded)
        {
            Debug.LogError("Local player data must be loaded before saving.");
            return;
        }

        try
        {
            localSave.player = SaveMapper.ToDto(currentPlayerData);
            WriteLocalSave();
        }
        catch (Exception e)
        {
            Debug.LogError($"Local player data save failed: {e}");
        }
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

    //캐릭터 생성 (고유 ID 발급 후 저장)
    public void CreateCharacter(CharacterData character)
    {
        // 고유 ID 생성
        character.ID = System.Guid.NewGuid().ToString();
        SaveCharacter(character);
    }

    // 캐릭터 데이터를 ID를 키로 저장
    public void SaveCharacter(CharacterData characterData)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.InExpedition) return;
        if (characterData != null && !string.IsNullOrEmpty(characterData.ID))
        {
            if (characterData.IsAlive)
                livingCharacterIds.Add(characterData.ID);
            else
                livingCharacterIds.Remove(characterData.ID);
        }

        if (suppressRemotePersistence)
            return;

        if (!localSaveLoaded)
        {
            Debug.LogError("Local player data must be loaded before saving characters.");
            return;
        }

        try
        {
            localSave.characters[characterData.ID] = SaveMapper.ToDto(characterData);
            WriteLocalSave();
            MultiplayerSession.Instance?.RefreshSavedCharacter(characterData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Local character save failed: {e}");
        }
    }

    public bool TryReviveCharacter(CharacterData character, int goldCost, int essenceCost)
    {
        PlayerData player = currentPlayerData;
        if (!localSaveLoaded || suppressRemotePersistence || character == null || goldCost < 0 || essenceCost < 1 ||
            MultiplayerSession.Instance?.InExpedition == true || QuestManager.Instance?.active != null ||
            player?.characterIds?.Contains(character.ID) != true ||
            player.revivalRequiredCharacterIds?.Contains(character.ID) != true ||
            player.missingCharacterIds?.Contains(character.ID) == true || player.gold < goldCost)
            return false;

        var materials = new List<InventorySlotData>();
        long available = 0;
        if (player.accountStorage != null)
            foreach (InventorySlotData slot in player.accountStorage)
                if (slot != null && slot.itemUid == SharedInventoryUtility.FadedEssenceItemUid && slot.count > 0 &&
                    MultiplayerSession.Instance?.IsFriendlyStakeLocked(slot) != true)
                {
                    materials.Add(slot);
                    available += slot.count;
                }
        if (available < essenceCost) return false;

        LocalPlayerSaveDTO previousSave = localSave;
        var previousStorage = new List<InventorySlotData>(player.accountStorage);
        var previousCounts = new Dictionary<InventorySlotData, int>();
        foreach (InventorySlotData slot in materials) previousCounts[slot] = slot.count;
        var previousRevivalIds = new List<string>(player.revivalRequiredCharacterIds);
        int previousGold = player.gold;
        bool previousAlive = character.IsAlive;
        int previousHp = character.CurrentHp;
        int previousStamina = character.CurrentStamina;
        int previousMentality = character.CurrentMentality;
        try
        {
            character.UpdateFinalStats();
            int remaining = essenceCost;
            foreach (InventorySlotData slot in materials)
            {
                int amount = Mathf.Min(remaining, slot.count);
                slot.count -= amount;
                remaining -= amount;
                if (slot.count == 0) player.accountStorage.Remove(slot);
                if (remaining == 0) break;
            }
            player.gold -= goldCost;
            player.revivalRequiredCharacterIds.RemoveAll(id => id == character.ID);
            character.IsAlive = true;
            character.CurrentHp = Mathf.Max(1, character.FinalStats.MaxHp);
            character.CurrentStamina = character.FinalStats.MaxStamina;
            character.CurrentMentality = character.FinalStats.MaxMentality;
            localSave = JsonConvert.DeserializeObject<LocalPlayerSaveDTO>(JsonConvert.SerializeObject(previousSave));
            localSave.player = SaveMapper.ToDto(player);
            localSave.characters[character.ID] = SaveMapper.ToDto(character);
            WriteLocalSave();
        }
        catch (Exception e)
        {
            localSave = previousSave;
            player.gold = previousGold;
            player.accountStorage.Clear();
            player.accountStorage.AddRange(previousStorage);
            foreach (var entry in previousCounts) entry.Key.count = entry.Value;
            player.revivalRequiredCharacterIds.Clear();
            player.revivalRequiredCharacterIds.AddRange(previousRevivalIds);
            character.IsAlive = previousAlive;
            character.CurrentHp = previousHp;
            character.CurrentStamina = previousStamina;
            character.CurrentMentality = previousMentality;
            Debug.LogError($"부활 저장 실패. 자원과 캐릭터 상태를 복원했습니다: {e}");
            return false;
        }
        livingCharacterIds.Add(character.ID);
        MultiplayerSession.Instance?.RefreshSavedCharacter(character);
        return true;
    }

    // 캐릭터 데이터를 로드하는 메서드
    public void LoadCharacter(string characterId, Action<CharacterData> onCharacterLoaded)
    {
        StartCoroutine(LoadLocalCharacter(characterId, onCharacterLoaded));
    }

    public MultiplayerSession.ExpeditionState SharedCheckpoint => localSave?.sharedCheckpoint;

    public bool HasSharedReceipt(string id) => localSave?.sharedReceipts?.Contains(id) == true;

    public bool SaveSharedCheckpoint(MultiplayerSession.ExpeditionState state)
    {
        if (!localSaveLoaded) return false;
        var previous = localSave.sharedCheckpoint;
        try
        {
            localSave.sharedCheckpoint = state == null ? null : JsonConvert.DeserializeObject<MultiplayerSession.ExpeditionState>(JsonConvert.SerializeObject(state));
            WriteLocalSave();
            return true;
        }
        catch (Exception e)
        {
            localSave.sharedCheckpoint = previous;
            Debug.LogError($"공동 원정 체크포인트 저장 실패: {e}");
            return false;
        }
    }

    public bool CommitSharedResult(string id, PlayerSaveDTO player, List<CharacterSaveDTO> characters)
    {
        if (!localSaveLoaded) return false;
        if (HasSharedReceipt(id)) return true;
        var previous = localSave;
        try
        {
            localSave = JsonConvert.DeserializeObject<LocalPlayerSaveDTO>(JsonConvert.SerializeObject(previous));
            localSave.player = player;
            foreach (var character in characters)
            {
                if (!player.characterIds.Contains(character.id)) throw new InvalidOperationException("다른 참가자의 캐릭터는 저장할 수 없습니다.");
                localSave.characters[character.id] = character;
            }
            localSave.sharedReceipts ??= new List<string>();
            localSave.sharedReceipts.Add(id);
            localSave.sharedCheckpoint = null;
            WriteLocalSave();
            return true;
        }
        catch (Exception e)
        {
            localSave = previous;
            Debug.LogError($"공동 원정 결과 저장 실패: {e}");
            return false;
        }
    }

    private IEnumerator LoadLocalCharacter(string characterId, Action<CharacterData> onCharacterLoaded)
    {
        string sharedExpeditionId = MultiplayerSession.Instance?.Expedition?.id;
        yield return null;
        if (sharedExpeditionId != null && MultiplayerSession.Instance?.Expedition?.id != sharedExpeditionId)
        {
            onCharacterLoaded?.Invoke(null);
            yield break;
        }
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.InExpedition)
        {
            onCharacterLoaded?.Invoke(MultiplayerSession.Instance.LoadExpeditionCharacter(characterId));
            yield break;
        }
        CharacterData character = null;
        try
        {
            if (localSaveLoaded && localSave.characters.TryGetValue(characterId, out CharacterSaveDTO dto))
                character = SaveMapper.FromDto(dto);
            else
                Debug.LogError("Local character data not found for ID: " + characterId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Local character load failed ({characterId}): {e}");
        }
        onCharacterLoaded?.Invoke(character);
    }

    public void SaveCharacterPosition(string characterID, bool isFrontRow)
    {
        if (_instance.currentPlayerData == null)
            return;

        _instance.currentPlayerData.SetPosition(characterID, isFrontRow);
        SavePlayerDataToPlayFab();
    }
}
