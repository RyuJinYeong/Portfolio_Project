using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.DebugManager;

public enum UIMode { Town, Battle }

[Serializable]
public class StageVisualProfile
{
    public string key = "Forest";          // "Town", "Forest", "Dungeon", "Tutorial"...
    [Header("Skybox")]
    public Material skybox;

    [Header("Fog")]
    public bool useFog = false;
    public FogMode fogMode = FogMode.Exponential;
    public Color fogColor = Color.gray;
    public float fogDensity = 0.01f;

    [Header("UI Mode")]
    public UIMode uiMode = UIMode.Battle;   // Town이면 Town, 그 외 Battle
}

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Sets")]
    public List<GameObject> townBackgrounds;
    public List<GameObject> forestBackgrounds;
    public List<GameObject> dungeonBackgrounds;
    public List<GameObject> tutorialBackgrounds;
    // 필요 시 desertBackgrounds 등 추가

    [Header("Profiles")]
    public List<StageVisualProfile> profiles = new(); // 인스펙터로 키별 등록

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
    }

    public void SetActiveStage(string stageType)
    {
        DeactivateAll();

        var prof = profiles != null ? profiles.Find(p => p.key == stageType) : null;
        ApplyVisualProfile(prof, stageType);

        UIManager.Instance?.UISwitch(prof.uiMode);

        switch (stageType)
        {
            case "Town":
                ActivateRandom(townBackgrounds);
                break;
            case "Forest":
                ActivateRandom(forestBackgrounds);
                break;
            case "Dungeon":
                ActivateRandom(dungeonBackgrounds);
                break;
            case "Tutorial":
                ActivateRandom(tutorialBackgrounds);
                break;
            default:
                Debug.LogWarning($"[StageManager] Unknown stageType: {stageType}");
                break;
        }
    }

    void DeactivateAll()
    {
        void Off(List<GameObject> list)
        {
            if (list == null) return;
            foreach (var go in list) if (go) go.SetActive(false);
        }

        Off(townBackgrounds);
        Off(forestBackgrounds);
        Off(dungeonBackgrounds);
        Off(tutorialBackgrounds);
    }

    void ActivateRandom(List<GameObject> list)
    {
        if (list == null || list.Count == 0) return;
        var idx = UnityEngine.Random.Range(0, list.Count);
        if (list[idx]) list[idx].SetActive(true);
    }

    void ApplyVisualProfile(StageVisualProfile p, string stageType)
    {
        if (p == null)
        {
            RenderSettings.fog = false;
            DynamicGI.UpdateEnvironment();
            return;
        }

        // Skybox
        if (p.skybox) RenderSettings.skybox = p.skybox;
        if (RenderSettings.skybox)
        {
            var m = RenderSettings.skybox;
        }

        // Fog
        RenderSettings.fog = p.useFog;
        if (p.useFog)
        {
            RenderSettings.fogMode = p.fogMode;
            RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogDensity = p.fogDensity;
        }


        DynamicGI.UpdateEnvironment();
    }
}


public static class StagePreloadPolicy
{
    // 스테이지 타입별로 어떤 캐릭터들의 스킬 아이콘을 미리 로드할지 정의
    public static IEnumerable<string> GetPreloadCharacterIds(PlayerData pd, string stageType)
    {
        if (pd == null) yield break;

        switch (stageType)
        {
            case "Town":
                // 로비: 보유(플레이어블) 전체
                foreach (var id in pd.characterIds) yield return id;
                break;

            // 필요하면 케이스 추가: "PvP", "Field", "Raid" 등
            default:
                // 전투류 기본값: 출정 멤버만
                foreach (var id in pd.activeCharacterIds) yield return id;
                break;
        }
    }
}