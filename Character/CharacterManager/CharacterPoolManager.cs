using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterPoolManager : MonoBehaviour
{
    public static CharacterPoolManager Instance { get; private set; }

    public GameObject characterPrefab_M;
    public GameObject characterPrefab_F;

    public Transform poolRoot;          // 화면 밖 임시 보관 위치

    public readonly Dictionary<string, CharacterManager> _pool = new(); // charId -> manager
    public IReadOnlyDictionary<string, CharacterManager> Pool => _pool;
        
    public bool IsBuilt { get; private set; }
    bool _building = false;
    readonly List<Action> _waiters = new();

    void Awake()
    {
        Instance = this;
        if (!poolRoot)
        {
            var go = new GameObject("CharacterPoolRoot");
            poolRoot = go.transform;
            poolRoot.position = new Vector3(9999, 9999, 9999);
            DontDestroyOnLoad(go);
        }
        DontDestroyOnLoad(gameObject);
    }

    public void BuildPoolFromPlayerData(Action onDone = null)
    {
        // 빌드 완료된 상태면 즉시 콜백
        if (IsBuilt) { onDone?.Invoke(); return; }

        // 빌드 중이면 대기열에 합류
        if (_building) { if (onDone != null) _waiters.Add(onDone); return; }

        _building = true;
        if (onDone != null) _waiters.Add(onDone);

        var pd = PlayerManager.Instance.GetCurrentPlayerData();
        if (pd == null || pd.characterIds == null || pd.characterIds.Count == 0)
        {
            FinishBuild(); // 빈 계정이어도 빌드 완료로 처리
            return;
        }

        int pending = 0;
        foreach (var id in pd.characterIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            if (_pool.ContainsKey(id)) continue; // 이미 풀에 있으면 스킵

            pending++;
            PlayerManager.Instance.LoadCharacter(id, ch =>
            {
                try
                {
                    if (ch != null) CreatePooled(ch);
                }
                finally
                {
                    pending--;
                    if (pending == 0) FinishBuild();
                }
            });
        }

        if (pending == 0) FinishBuild();
    }

    void FinishBuild()
    {
        // 빌드 완료 처리 (1회 보장)
        IsBuilt = true;
        _building = false;
        var cbs = _waiters.ToArray();
        _waiters.Clear();
        foreach (var cb in cbs) cb?.Invoke();
    }

    void CreatePooled(CharacterData ch)
    {
        var prefab = (ch.customizationData?.IsMale ?? true) ? characterPrefab_M : characterPrefab_F;
        var go = Instantiate(prefab, poolRoot.position, Quaternion.identity, poolRoot);
        go.name = $"Pooled_{ch.Name}_{ch.ID}";
        var cm = go.GetComponent<CharacterManager>();
        cm.InitializeCharacter(ch);                // 기존 초기화 그대로 사용
        go.SetActive(false);                       // 기본 비활성
        _pool[ch.ID] = cm;
    }

    public CharacterManager Get(string charId)
        => (!string.IsNullOrEmpty(charId) && _pool.TryGetValue(charId, out var cm)) ? cm : null;

    public IEnumerable<CharacterManager> All() => _pool.Values;

    public void PlaceActive(string charId, Transform spawnPoint, bool active = true)
    {
        var cm = Get(charId);
        if (!cm || !spawnPoint) return;
        cm.gameObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        cm.gameObject.SetActive(active);
    }

    public void DeactivateAllToPool()
    {
        foreach (var cm in _pool.Values)
        {
            cm.transform.SetParent(poolRoot, true);
            cm.transform.localPosition = Vector3.zero;
            cm.gameObject.SetActive(false);
        }
    }
}
