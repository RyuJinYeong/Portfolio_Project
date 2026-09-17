using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class IconStore
{
    // 동시에 로드할 Addressables 작업 수(네트워크/디스크 병렬)
    public static int MaxConcurrent = 6;

    class Entry
    {
        public Texture2D tex;
        public AsyncOperationHandle<Texture2D> handle;
        public HashSet<string> groups = new(); // 동일 키가 여러 그룹에 속할 수 있음
    }

    static readonly Dictionary<string, Entry> _map = new();
    static readonly Dictionary<string, HashSet<string>> _groupIndex = new();

    // ────────────────────────── 기본 조회 ──────────────────────────
    public static bool TryGet(string key, out Texture2D tex)
    {
        if (!string.IsNullOrEmpty(key) && _map.TryGetValue(key, out var e) && e.tex != null)
        {
            tex = e.tex;
            return true;
        }
        tex = null;
        return false;
    }

    public static Texture2D GetOrNull(string key)
        => (!string.IsNullOrEmpty(key) && _map.TryGetValue(key, out var e)) ? e.tex : null;

    // ────────────────────────── 내부: put ──────────────────────────
    static void Put(string key, AsyncOperationHandle<Texture2D> handle, string group)
    {
        if (string.IsNullOrEmpty(key)) { if (handle.IsValid()) Addressables.Release(handle); return; }
        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        { if (handle.IsValid()) Addressables.Release(handle); return; }

        if (!_map.TryGetValue(key, out var e))
            e = _map[key] = new Entry { tex = handle.Result, handle = handle };
        else
        {
            // 이미 있으면 새 핸들은 해제(기존 텍스처 유지)
            if (handle.IsValid()) Addressables.Release(handle);
        }

        e.groups.Add(group);
        if (!_groupIndex.TryGetValue(group, out var set))
            set = _groupIndex[group] = new HashSet<string>();
        set.Add(key);
    }

    // ────────────────────────── 프리로드(멀티키) ──────────────────────────
    public static IEnumerator Preload(IEnumerable<string> keys, string group, Action<float> onProgress = null)
    {
        var uniq = new HashSet<string>(keys ?? Array.Empty<string>());
        var queue = new Queue<string>();
        foreach (var k in uniq)
            if (!string.IsNullOrEmpty(k) && !TryGet(k, out _))
                queue.Enqueue(k);

        int total = queue.Count;
        int done = 0;
        var inflight = new List<(string key, AsyncOperationHandle<Texture2D> h)>();

        while (queue.Count > 0 || inflight.Count > 0)
        {
            while (queue.Count > 0 && inflight.Count < MaxConcurrent)
            {
                var key = queue.Dequeue();
                var h = Addressables.LoadAssetAsync<Texture2D>(key);
                inflight.Add((key, h));
            }

            for (int i = inflight.Count - 1; i >= 0; --i)
            {
                var it = inflight[i];
                if (it.h.IsDone)
                {
                    if (it.h.Status == AsyncOperationStatus.Succeeded && it.h.Result != null)
                        Put(it.key, it.h, group);
                    else if (it.h.IsValid())
                        Addressables.Release(it.h);

                    inflight.RemoveAt(i);
                    done++;
                    onProgress?.Invoke(total > 0 ? (float)done / total : 1f);
                }
            }
            yield return null;
        }
        onProgress?.Invoke(1f);
    }

    // ────────────────────────── 프리로드(단일) ──────────────────────────
    public static IEnumerator PreloadSingle(string key, string group, Action<Texture2D> onDone = null)
    {
        if (string.IsNullOrEmpty(key)) yield break;
        if (TryGet(key, out var already)) { onDone?.Invoke(already); yield break; }

        var h = Addressables.LoadAssetAsync<Texture2D>(key);
        yield return h;

        if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null)
        {
            Put(key, h, group);
            onDone?.Invoke(h.Result);
        }
        else
        {
            if (h.IsValid()) Addressables.Release(h);
            onDone?.Invoke(null);
        }
    }

    // ────────────────────────── 그룹 해제 ──────────────────────────
    public static void ClearGroup(string group)
    {
        if (!_groupIndex.TryGetValue(group, out var set)) return;

        foreach (var key in set)
        {
            if (!_map.TryGetValue(key, out var e)) continue;
            e.groups.Remove(group);

            if (e.groups.Count == 0)
            {
                if (e.handle.IsValid()) Addressables.Release(e.handle);
                _map.Remove(key);
            }
        }
        _groupIndex.Remove(group);
    }

    public static void ClearAll()
    {
        foreach (var e in _map.Values)
            if (e.handle.IsValid()) Addressables.Release(e.handle);
        _map.Clear();
        _groupIndex.Clear();
    }
}
