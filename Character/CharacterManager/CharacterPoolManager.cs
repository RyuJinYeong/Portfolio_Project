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

    readonly Queue<CharacterManager> _portraitRefreshQueue = new();
    readonly Dictionary<CharacterManager, Action> _pendingPortraitRefreshCallbacks = new();
    Coroutine _portraitRefreshQueueRoutine;
    bool _portraitCaptureInProgress;

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
        var playerIds = PlayerManager.Instance.GetCurrentPlayerData()?.characterIds;
        if (IsBuilt && (playerIds == null || playerIds.TrueForAll(id =>
            string.IsNullOrEmpty(id) || _pool.ContainsKey(id))))
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
        yield return CapturePortraitSequentially(ch, captured => portrait = captured);

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

    public void RefreshPortrait(CharacterManager characterManager, Action onDone = null)
    {
        if (characterManager == null || characterManager.character == null)
        {
            onDone?.Invoke();
            return;
        }

        if (_pendingPortraitRefreshCallbacks.TryGetValue(characterManager, out Action pendingCallback))
        {
            _pendingPortraitRefreshCallbacks[characterManager] = pendingCallback + onDone;
        }
        else
        {
            _pendingPortraitRefreshCallbacks.Add(characterManager, onDone);
            _portraitRefreshQueue.Enqueue(characterManager);
        }

        if (_portraitRefreshQueueRoutine == null)
            _portraitRefreshQueueRoutine = StartCoroutine(ProcessPortraitRefreshQueue());
    }

    public void CapturePortrait(CharacterData character, Action onDone = null)
    {
        if (character == null)
        {
            onDone?.Invoke();
            return;
        }

        StartCoroutine(CapturePortraitRoutine(character, onDone));
    }

    public void CaptureMonsterPortrait(
        CharacterData character,
        GameObject modelPrefab,
        Action onDone = null)
    {
        if (character == null || modelPrefab == null)
        {
            onDone?.Invoke();
            return;
        }

        StartCoroutine(CaptureMonsterPortraitRoutine(character, modelPrefab, onDone));
    }

    private IEnumerator CaptureMonsterPortraitRoutine(
        CharacterData character,
        GameObject modelPrefab,
        Action onDone)
    {
        while (_portraitCaptureInProgress)
            yield return null;

        _portraitCaptureInProgress = true;

        if (malePortraitDummy != null)
            malePortraitDummy.gameObject.SetActive(false);

        if (femalePortraitDummy != null)
            femalePortraitDummy.gameObject.SetActive(false);

        CharacterCustomization anchor = malePortraitDummy != null
            ? malePortraitDummy
            : femalePortraitDummy;

        if (anchor == null || portraitCamera == null || portraitRenderTexture == null)
        {
            _portraitCaptureInProgress = false;
            onDone?.Invoke();
            yield break;
        }

        GameObject preview = Instantiate(modelPrefab);
        preview.name = $"PortraitPreview_{character.monsterRoleId}";
        preview.transform.SetPositionAndRotation(
            anchor.transform.position,
            anchor.transform.rotation);
        preview.transform.localScale = anchor.transform.lossyScale;

        foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        yield return null;
        yield return null;
        FitPreviewToAnchor(preview, anchor.gameObject);
        yield return new WaitForEndOfFrame();

        Texture2D portrait = anchor.CapturePortrait(portraitCamera, portraitRenderTexture);

        if (portrait != null)
        {
            character.Portrait = portrait;
            character.Portrait.name = $"Portrait_{character.ID}";
        }

        Destroy(preview);
        yield return null;

        _portraitCaptureInProgress = false;
        onDone?.Invoke();
    }

    private static void FitPreviewToAnchor(GameObject preview, GameObject anchor)
    {
        Transform previewHead = null;
        Transform anchorHead = null;
        foreach (var model in new[] { preview, anchor })
        {
            Transform head = null;
            var animator = model.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman) head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head == null)
                foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                    if (bone.name.Equals("head", StringComparison.OrdinalIgnoreCase) || bone.name.EndsWith(":Head", StringComparison.OrdinalIgnoreCase))
                    { head = bone; break; }
            if (model == preview) previewHead = head; else anchorHead = head;
        }
        if (previewHead != null && anchorHead != null)
        {
            float height = previewHead.position.y - preview.transform.position.y;
            if (height > 0.01f)
                preview.transform.localScale *= (anchorHead.position.y - anchor.transform.position.y) / height;
            preview.transform.position += anchorHead.position - previewHead.position;
            return;
        }
        if (!TryGetRendererBounds(preview, out Bounds previewBounds) ||
            !TryGetRendererBounds(anchor, out Bounds anchorBounds) ||
            previewBounds.size.y <= 0.001f)
        {
            return;
        }

        float scale = anchorBounds.size.y / previewBounds.size.y;
        preview.transform.localScale *= scale;

        if (TryGetRendererBounds(preview, out previewBounds))
            preview.transform.position += anchorBounds.center - previewBounds.center;
    }

    private static bool TryGetRendererBounds(GameObject target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bounds = default;
        bool found = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private IEnumerator CapturePortraitRoutine(CharacterData character, Action onDone)
    {
        Texture2D portrait = null;
        yield return CapturePortraitSequentially(character, captured => portrait = captured);

        if (portrait != null)
        {
            character.Portrait = portrait;
            character.Portrait.name = $"Portrait_{character.ID}";
        }

        onDone?.Invoke();
    }

    public void AddCharacterToPool(CharacterData character, Action onDone = null)
    {
        if (character == null || string.IsNullOrEmpty(character.ID))
        {
            onDone?.Invoke();
            return;
        }

        if (_pool.ContainsKey(character.ID))
        {
            onDone?.Invoke();
            return;
        }

        StartCoroutine(AddCharacterToPoolRoutine(character, onDone));
    }

    private IEnumerator AddCharacterToPoolRoutine(CharacterData character, Action onDone)
    {
        yield return CreatePooled(character);
        onDone?.Invoke();
    }

    private IEnumerator ProcessPortraitRefreshQueue()
    {
        while (_portraitRefreshQueue.Count > 0)
        {
            CharacterManager characterManager = _portraitRefreshQueue.Dequeue();

            if (!_pendingPortraitRefreshCallbacks.TryGetValue(
                    characterManager,
                    out Action onDone))
            {
                continue;
            }

            _pendingPortraitRefreshCallbacks.Remove(characterManager);

            yield return RefreshPortraitRoutine(characterManager, onDone);
        }

        _portraitRefreshQueueRoutine = null;
    }

    private IEnumerator RefreshPortraitRoutine(
        CharacterManager characterManager,
        Action onDone)
    {
        CharacterData character = characterManager.character;
        Texture2D portrait = null;

        yield return CapturePortraitSequentially(character, captured => portrait = captured);

        if (portrait != null)
        {
            Texture2D previousPortrait = character.Portrait;

            character.Portrait = portrait;
            character.Portrait.name = $"Portrait_{character.ID}";

            onDone?.Invoke();

            if (previousPortrait != null &&
                previousPortrait != portrait &&
                previousPortrait.name.StartsWith("Portrait_"))
            {
                Destroy(previousPortrait);
            }
        }
        else
        {
            onDone?.Invoke();
        }
    }

    private IEnumerator CapturePortraitSequentially(
        CharacterData character,
        Action<Texture2D> onCaptured)
    {
        while (_portraitCaptureInProgress)
            yield return null;

        _portraitCaptureInProgress = true;

        bool isMale = character.customizationData?.IsMale ?? true;

        if (malePortraitDummy != null)
            malePortraitDummy.gameObject.SetActive(false);

        if (femalePortraitDummy != null)
            femalePortraitDummy.gameObject.SetActive(false);

        CharacterCustomization dummy = isMale ? malePortraitDummy : femalePortraitDummy;

        if (dummy == null)
        {
            _portraitCaptureInProgress = false;
            onCaptured?.Invoke(null);
            yield break;
        }

        dummy.gameObject.SetActive(true);
        dummy.ApplyCustomization(character);
        dummy.UpdateEquipmentAppearance(character);

        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        Texture2D portrait = dummy.CapturePortrait(portraitCamera, portraitRenderTexture);

        yield return null;
        dummy.gameObject.SetActive(false);

        _portraitCaptureInProgress = false;
        onCaptured?.Invoke(portrait);
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
