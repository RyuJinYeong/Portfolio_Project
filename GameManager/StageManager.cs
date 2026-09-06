using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugManager;

public enum UIMode { Town, Battle }

public enum StageAmbientMode
{
    Skybox = 0,
    Gradient = 1,
    Color = 3
}

[Serializable]
public class StageVisualProfile
{
    public string key = "Forest";          // "Town", "Forest", "Dungeon", "Tutorial"...
    [Header("Skybox")]
    public bool overrideSkybox = true;
    public Material skybox;

    [Header("Sun / Environment Lighting")]
    public bool overrideSun;
    public Light sun;
    public bool overrideEnvironmentLighting;
    public Color realtimeShadowColor = Color.gray;
    public StageAmbientMode ambientMode = StageAmbientMode.Skybox;
    [ColorUsage(true, true)]
    public Color ambientLight = Color.gray;
    [ColorUsage(true, true)]
    public Color ambientSkyColor = Color.gray;
    [ColorUsage(true, true)]
    public Color ambientEquatorColor = Color.gray;
    [ColorUsage(true, true)]
    public Color ambientGroundColor = Color.gray;
    [Min(0f)] public float ambientIntensity = 1f;

    [Header("Environment Reflections")]
    public bool overrideEnvironmentReflections;
    public DefaultReflectionMode defaultReflectionMode = DefaultReflectionMode.Skybox;
    public Texture customReflection;
    [Min(16)] public int reflectionResolution = 128;
    [Min(0f)] public float reflectionIntensity = 1f;
    [Min(1)] public int reflectionBounces = 1;

    [Header("Fog")]
    public bool useFog = false;
    public FogMode fogMode = FogMode.Exponential;
    public Color fogColor = Color.gray;
    public float fogDensity = 0.01f;
    public float fogStartDistance;
    public float fogEndDistance = 300f;

    [Header("Other Environment Settings")]
    public bool overrideOtherEnvironmentSettings;
    [Min(0f)] public float haloStrength = 0.5f;
    [Min(0f)] public float flareFadeSpeed = 3f;
    [Min(0f)] public float flareStrength = 1f;

    [Header("UI Mode")]
    public UIMode uiMode = UIMode.Battle;   // Town이면 Town, 그 외 Battle
}

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Profiles")]
    public List<StageVisualProfile> profiles = new(); // 인스펙터로 키별 등록

    private GameObject townStage;
    private GameObject townCameraRoot;
    private GameObject battleCameraRoot;
    private GameObject activeQuestMap;
    private QuestStageMapLayout activeQuestMapLayout;
    private string activeQuestStageKey;
    private int activeQuestMapPrefabIndex = -1;
    private GameObject activeEncounterMap;
    private GameObject activeEncounterProps;
    private Camera activeEncounterCamera;
    private GameObject hiddenQuestMap;
    private bool restoreCameraRoots;
    private bool townCameraWasActive;
    private bool battleCameraWasActive;
    private string activeEncounterCameraPreviousTag;
    private string appliedEnvironmentStageKey;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Transform stageRoot = GameObject.Find("StageRoot")?.transform;
            townStage = stageRoot != null
                ? stageRoot.Find("Town")?.gameObject
                : null;

            if (townStage == null)
                Debug.LogError("[StageManager] StageRoot/Town was not found.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetActiveStage(string stageType)
    {
        DeactivateAll();

        ApplyStageProfile(stageType);

        if (townStage != null)
            townStage.SetActive(stageType == "Town");
    }

    public void SetActiveQuestStage(QuestDef quest)
    {
        SetActiveQuestStage(quest, quest != null ? quest.mapPrefabIndex : -1);
    }

    public void SetActiveQuestStage(QuestDef quest, int mapPrefabIndex)
    {
        if (quest == null || string.IsNullOrEmpty(quest.stageKey))
        {
            Debug.LogWarning("[StageManager] Active quest stage data is missing.");
            return;
        }

        if (activeQuestMap != null &&
            activeQuestStageKey == quest.stageKey &&
            activeQuestMapPrefabIndex == mapPrefabIndex)
        {
            DeactivateEncounterPresentation();
            ApplyStageProfile(quest.stageKey);
            ActivateCurrentQuestArea();
            return;
        }

        DeactivateAll();

        ApplyStageProfile(quest.stageKey);

        if (!ActivateQuestMap(quest, mapPrefabIndex))
        {
            Debug.LogError(
                $"[StageManager] Quest map prefab not found. " +
                $"stageKey: {quest.stageKey}, mapPrefabIndex: {mapPrefabIndex}");
        }
    }

    public void ActivateEncounterPresentation(QuestEncounterDefinitionSO encounter)
    {
        DeactivateEncounterPresentation();

        if (encounter == null)
            return;

        GameObject backgroundPrefab = encounter.encounterMapPrefab;

        if (encounter.availableInAllStages)
            backgroundPrefab = null;

        if (backgroundPrefab != null)
        {
            if (activeQuestMap != null && activeQuestMap.activeSelf)
            {
                hiddenQuestMap = activeQuestMap;
                hiddenQuestMap.SetActive(false);
            }
            activeEncounterMap = Instantiate(backgroundPrefab);
            activeEncounterMap.name = $"{backgroundPrefab.name}_EncounterMap";
        }

        if (encounter.encounterPropsPrefab != null)
        {
            Transform parent = activeEncounterMap != null
                ? activeEncounterMap.transform
                : activeQuestMapLayout != null
                    ? activeQuestMapLayout.GetEncounterPropsRoot()
                    : activeQuestMap != null
                        ? activeQuestMap.transform
                        : null;
            activeEncounterProps = Instantiate(encounter.encounterPropsPrefab, parent);
            activeEncounterProps.name = $"{encounter.encounterPropsPrefab.name}_EncounterProps";
        }

        activeEncounterCamera = activeEncounterMap != null
            ? activeEncounterMap.GetComponentInChildren<Camera>(true)
            : activeQuestMapLayout != null
                ? activeQuestMapLayout.GetEncounterCamera(encounter.encounterId)
                : null;

        if (activeEncounterCamera == null && activeEncounterMap == null &&
            activeQuestMapLayout == null && activeQuestMap != null)
        {
            activeEncounterCamera = activeQuestMap.GetComponentInChildren<Camera>(true);
        }

        if (activeEncounterCamera != null)
            SwitchToEncounterCamera(activeEncounterCamera);
        else
            Debug.LogWarning("[StageManager] Encounter camera is not assigned to the active encounter area.");
    }

    public void DeactivateEncounterPresentation()
    {
        if (activeEncounterCamera != null)
        {
            activeEncounterCamera.enabled = false;

            if (!string.IsNullOrEmpty(activeEncounterCameraPreviousTag))
                activeEncounterCamera.gameObject.tag = activeEncounterCameraPreviousTag;

            AudioListener listener = activeEncounterCamera.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = false;
        }

        activeEncounterCamera = null;
        activeEncounterCameraPreviousTag = null;

        if (restoreCameraRoots)
        {
            if (townCameraRoot != null)
                townCameraRoot.SetActive(townCameraWasActive);

            if (battleCameraRoot != null)
                battleCameraRoot.SetActive(battleCameraWasActive);

            restoreCameraRoots = false;
        }

        if (activeEncounterProps != null)
        {
            activeEncounterProps.SetActive(false);
            Destroy(activeEncounterProps);
        }

        if (activeEncounterMap != null)
        {
            activeEncounterMap.SetActive(false);
            Destroy(activeEncounterMap);
        }

        activeEncounterProps = null;
        activeEncounterMap = null;

        if (hiddenQuestMap != null)
            hiddenQuestMap.SetActive(true);

        hiddenQuestMap = null;
    }

    public void ActivateEncounterBattle()
    {
        DeactivateEncounterPresentation();
        activeQuestMapLayout?.ActivateArea(QuestRouteNodeType.Battle);
        AlignSpawnPointManager(QuestRouteNodeType.Battle);
        SwitchCamera(UIMode.Battle);
    }

    private void ApplyStageProfile(string stageType)
    {
        StageVisualProfile prof =
            profiles != null
                ? profiles.Find(p => p.key == stageType)
                : null;

        if (!string.Equals(appliedEnvironmentStageKey, stageType, StringComparison.Ordinal))
        {
            ApplyVisualProfile(prof, stageType);
            appliedEnvironmentStageKey = stageType;
        }

        UIMode uiMode = prof != null
            ? prof.uiMode
            : stageType == "Town"
                ? UIMode.Town
                : UIMode.Battle;

        SwitchCamera(uiMode);
        UIManager.Instance?.UISwitch(uiMode);
    }

    private void SwitchCamera(UIMode uiMode)
    {
        ResolveCameraRoots();

        bool isTown = uiMode == UIMode.Town;

        if (townCameraRoot != null)
            townCameraRoot.SetActive(isTown);

        if (battleCameraRoot != null)
            battleCameraRoot.SetActive(!isTown);
    }

    private void SwitchToEncounterCamera(Camera encounterCamera)
    {
        ResolveCameraRoots();

        townCameraWasActive = townCameraRoot != null && townCameraRoot.activeSelf;
        battleCameraWasActive = battleCameraRoot != null && battleCameraRoot.activeSelf;
        restoreCameraRoots = true;

        if (townCameraRoot != null)
            townCameraRoot.SetActive(false);

        if (battleCameraRoot != null && battleCameraRoot != encounterCamera.gameObject)
            battleCameraRoot.SetActive(false);

        activeEncounterCameraPreviousTag = encounterCamera.gameObject.tag;
        encounterCamera.gameObject.tag = "MainCamera";
        encounterCamera.gameObject.SetActive(true);
        encounterCamera.enabled = true;

        AudioListener listener = encounterCamera.GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = true;
    }

    private void ResolveCameraRoots()
    {
        if (townCameraRoot == null || battleCameraRoot == null)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "TownCameras")
                    townCameraRoot = root;
                else if (root.name == "MainCamera")
                    battleCameraRoot = root;
            }
        }

        if (battleCameraRoot == null && SpawnPointManager.Instance != null)
        {
            Camera battleCamera = SpawnPointManager.Instance.GetComponentInChildren<Camera>(true);

            if (battleCamera != null)
                battleCameraRoot = battleCamera.gameObject;
        }
    }

    private bool ActivateQuestMap(QuestDef quest, int mapPrefabIndex)
    {
        QuestStageDefinitionSO stage =
            GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetQuestStage(quest.stageKey)
                : null;

        if (stage == null ||
            stage.mapPrefabs == null ||
            mapPrefabIndex < 0 ||
            mapPrefabIndex >= stage.mapPrefabs.Count)
        {
            return false;
        }

        GameObject mapPrefab = stage.mapPrefabs[mapPrefabIndex];

        if (mapPrefab == null)
            return false;

        activeQuestMap = Instantiate(mapPrefab);
        activeQuestMap.name = $"{mapPrefab.name}_QuestMap";
        activeQuestMapLayout = activeQuestMap.GetComponentInChildren<QuestStageMapLayout>(true);
        activeQuestStageKey = quest.stageKey;
        activeQuestMapPrefabIndex = mapPrefabIndex;

        ActivateCurrentQuestArea();

        return true;
    }

    private void ActivateCurrentQuestArea()
    {
        QuestRouteNode currentNode = QuestManager.Instance != null
            ? QuestManager.Instance.GetCurrentRouteNode()
            : null;
        QuestRouteNodeType nodeType = currentNode != null
            ? currentNode.type
            : QuestRouteNodeType.Battle;
        activeQuestMapLayout?.ActivateArea(nodeType);
        AlignSpawnPointManager(nodeType);
    }

    private void AlignSpawnPointManager(QuestRouteNodeType nodeType)
    {
        if (nodeType != QuestRouteNodeType.Battle &&
            nodeType != QuestRouteNodeType.Elite &&
            nodeType != QuestRouteNodeType.Boss)
        {
            return;
        }

        if (SpawnPointManager.Instance == null)
            return;

        Transform battleSpawnPoint = activeQuestMapLayout != null
            ? activeQuestMapLayout.GetBattleSpawnPoint()
            : null;

        if (battleSpawnPoint == null && activeQuestMap != null)
        {
            Transform inactiveFallback = null;

            foreach (Transform candidate in activeQuestMap.GetComponentsInChildren<Transform>(true))
            {
                if (!candidate.name.StartsWith("BattleSpawnPoint", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (candidate.gameObject.activeInHierarchy)
                {
                    battleSpawnPoint = candidate;
                    break;
                }

                if (inactiveFallback == null)
                    inactiveFallback = candidate;
            }

            if (battleSpawnPoint == null)
                battleSpawnPoint = inactiveFallback;
        }

        if (battleSpawnPoint == null)
        {
            Debug.LogWarning("[StageManager] BattleSpawnPoint was not found in the active quest map.");
            return;
        }

        SpawnPointManager.Instance.transform.SetPositionAndRotation(
            battleSpawnPoint.position,
            battleSpawnPoint.rotation);
    }

    void DeactivateAll()
    {
        DeactivateEncounterPresentation();

        if (activeQuestMap != null)
            Destroy(activeQuestMap);

        activeQuestMap = null;
        activeQuestMapLayout = null;
        activeQuestStageKey = null;
        activeQuestMapPrefabIndex = -1;

        if (townStage != null)
            townStage.SetActive(false);
    }

    void ApplyVisualProfile(StageVisualProfile p, string stageType)
    {
        if (p == null)
        {
            RenderSettings.fog = false;
            DynamicGI.UpdateEnvironment();
            return;
        }

        if (p.overrideSkybox)
            RenderSettings.skybox = p.skybox;

        if (p.overrideSun)
            RenderSettings.sun = p.sun;

        if (p.overrideEnvironmentLighting)
        {
            RenderSettings.subtractiveShadowColor = p.realtimeShadowColor;
            RenderSettings.ambientMode = p.ambientMode switch
            {
                StageAmbientMode.Gradient => AmbientMode.Trilight,
                StageAmbientMode.Color => AmbientMode.Flat,
                _ => AmbientMode.Skybox
            };
            RenderSettings.ambientLight = p.ambientLight;
            RenderSettings.ambientSkyColor = p.ambientSkyColor;
            RenderSettings.ambientEquatorColor = p.ambientEquatorColor;
            RenderSettings.ambientGroundColor = p.ambientGroundColor;
            RenderSettings.ambientIntensity = p.ambientIntensity;
        }

        if (p.overrideEnvironmentReflections)
        {
            RenderSettings.defaultReflectionMode = p.defaultReflectionMode;
            RenderSettings.customReflectionTexture = p.customReflection;
            RenderSettings.defaultReflectionResolution = p.reflectionResolution;
            RenderSettings.reflectionIntensity = p.reflectionIntensity;
            RenderSettings.reflectionBounces = p.reflectionBounces;
        }

        RenderSettings.fog = p.useFog;
        if (p.useFog)
        {
            RenderSettings.fogMode = p.fogMode;
            RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogDensity = p.fogDensity;
            RenderSettings.fogStartDistance = p.fogStartDistance;
            RenderSettings.fogEndDistance = p.fogEndDistance;
        }

        if (p.overrideOtherEnvironmentSettings)
        {
            RenderSettings.haloStrength = p.haloStrength;
            RenderSettings.flareFadeSpeed = p.flareFadeSpeed;
            RenderSettings.flareStrength = p.flareStrength;
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
