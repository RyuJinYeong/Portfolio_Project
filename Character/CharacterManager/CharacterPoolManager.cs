using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterPoolManager : MonoBehaviour
{
    public static CharacterPoolManager Instance { get; private set; }

    public GameObject characterPrefab_M;
    public GameObject characterPrefab_F;

    public Transform poolRoot; // 화면 밖 임시 보관 위치

    [Header("Portrait Capture")]
    public Camera portraitCamera;
    public RenderTexture portraitRenderTexture;

    [Header("Portrait Dummy")]
    public CharacterCustomization malePortraitDummy;
    public CharacterCustomization femalePortraitDummy;

    public readonly Dictionary<string, CharacterManager> _pool = new();
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
        if (IsBuilt)
        {
            onDone?.Invoke();
            return;
        }

        if (_building)
        {
            if (onDone != null)
                _waiters.Add(onDone);

            return;
        }

        _building = true;

        if (onDone != null)
            _waiters.Add(onDone);

        var pd = PlayerManager.Instance.GetCurrentPlayerData();
        if (pd == null || pd.characterIds == null || pd.characterIds.Count == 0)
        {
            FinishBuild();
            return;
        }

        int pending = 0;
        List<CharacterData> loadedCharacters = new List<CharacterData>();

        foreach (var id in pd.characterIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            if (_pool.ContainsKey(id)) continue;

            pending++;

            PlayerManager.Instance.LoadCharacter(id, ch =>
            {
                if (ch != null)
                    loadedCharacters.Add(ch);

                pending--;

                if (pending == 0)
                    StartCoroutine(CreatePooledSequentially(loadedCharacters));
            });
        }

        if (pending == 0)
            FinishBuild();
    }

    IEnumerator CreatePooledSequentially(List<CharacterData> characters)
    {
        foreach (var ch in characters)
        {
            yield return CreatePooled(ch);
        }

        FinishBuild();
    }

    IEnumerator CreatePooled(CharacterData ch)
    {
        var prefab = (ch.customizationData?.IsMale ?? true) ? characterPrefab_M : characterPrefab_F;

        var go = Instantiate(prefab, poolRoot.position, Quaternion.identity, poolRoot);
        go.name = $"Pooled_{ch.Name}_{ch.ID}";
        go.SetActive(true);

        var cm = go.GetComponent<CharacterManager>();
        cm.InitializeCharacter(ch);

        Texture2D portrait = null;

        bool isMale = ch.customizationData?.IsMale ?? true;

        if (malePortraitDummy != null)
            malePortraitDummy.gameObject.SetActive(false);

        if (femalePortraitDummy != null)
            femalePortraitDummy.gameObject.SetActive(false);

        CharacterCustomization dummy = isMale ? malePortraitDummy : femalePortraitDummy;

        if (dummy != null)
        {
            dummy.gameObject.SetActive(true);

            dummy.ApplyCustomization(ch);
            dummy.UpdateEquipmentAppearance(ch);

            // 더미 활성화 + 커스터마이징 + 스킨드 메시 반영 대기
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            portrait = dummy.CapturePortrait(portraitCamera, portraitRenderTexture);

            // 마지막 캐릭터 촬영 직후 바로 비활성화/FinishBuild로 넘어가는 것 방지
            yield return null;

            dummy.gameObject.SetActive(false);
        }

        if (portrait != null)
        {
            cm.character.Portrait = portrait;
            cm.character.Portrait.name = $"Portrait_{cm.character.ID}";
        }

        go.SetActive(false);
        _pool[ch.ID] = cm;
    }

    void FinishBuild()
    {
        IsBuilt = true;
        _building = false;

        var cbs = _waiters.ToArray();
        _waiters.Clear();

        foreach (var cb in cbs)
            cb?.Invoke();
    }

    public CharacterManager Get(string charId)
    {
        if (!string.IsNullOrEmpty(charId) && _pool.TryGetValue(charId, out var cm))
            return cm;

        return null;
    }

    public IEnumerable<CharacterManager> All()
    {
        return _pool.Values;
    }

    public void PlaceActive(string charId, Transform spawnPoint, bool active = true)
    {
        var cm = Get(charId);
        if (!cm || !spawnPoint) return;

        cm.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
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