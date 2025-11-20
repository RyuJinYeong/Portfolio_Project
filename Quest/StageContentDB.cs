using System;
using System.Collections.Generic;

[Serializable]
public class StageProfile
{
    public string key;               // "Forest"
    public string label;             // "숲"
    public string[] allowedTags;     // 등장 가능 태그
    public int[] bossCandidates;     // 보스 후보 UID
    public int[] eliteCandidates;    // 엘리트 후보 UID(선택)
}

public static class StageContentDB
{
    static readonly Dictionary<string, StageProfile> PROFILES = new()
    {
        ["Forest"] = new StageProfile
        {
            key = "Forest",
            label = "숲",
            allowedTags = new[] { "Beast", "Bandit" },
            bossCandidates = new[] { 19001, 19101 },
            eliteCandidates = new[] { 19101 }
        },
        ["Cave"] = new StageProfile
        {
            key = "Cave",
            label = "동굴",
            allowedTags = new[] { "Undead", "Beast" },
            bossCandidates = new[] { 19002 },
            eliteCandidates = new[] { 19201 }
        },
        ["Ruins"] = new StageProfile
        {
            key = "Ruins",
            label = "유적",
            allowedTags = new[] { "Undead", "Bandit" },
            bossCandidates = new[] { 19003 },
            eliteCandidates = new[] { 19301 }
        },
    };

    public static StageProfile Get(string key) =>
        PROFILES.TryGetValue(key, out var p) ? p : null;

    public static string GetLabel(string key) =>
        Get(key)?.label ?? key;

    public static string[] AllStageKeys()
    {
        var arr = new string[PROFILES.Count];
        PROFILES.Keys.CopyTo(arr, 0);
        return arr;
    }
}
