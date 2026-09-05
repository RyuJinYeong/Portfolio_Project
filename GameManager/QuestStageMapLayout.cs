using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestStageEncounterCameraBinding
{
    public string encounterId;
    public Camera camera;
}

[Serializable]
public class QuestStageAreaVariant
{
    public GameObject root;
    [Tooltip("이 구역에서 SpawnPointManager 전체를 배치할 기준점입니다.")]
    public Transform battleSpawnPoint;
    public Transform encounterPropsRoot;
    [Tooltip("인카운트 ID 전용 카메라가 없을 때 사용할 기본 카메라입니다.")]
    public Camera encounterCamera;
    [Tooltip("이 배경에서 인카운트 ID별로 사용할 카메라입니다.")]
    public List<QuestStageEncounterCameraBinding> encounterCameras = new();
}

[Serializable]
public class QuestStageAreaGroup
{
    public QuestRouteNodeType nodeType;
    public List<QuestStageAreaVariant> variants = new();
}

public class QuestStageMapLayout : MonoBehaviour
{
    public List<QuestStageAreaGroup> areaGroups = new();

    private QuestStageAreaVariant activeVariant;

    public void ActivateArea(QuestRouteNodeType nodeType)
    {
        SetAllAreasActive(false);
        activeVariant = null;

        List<QuestStageAreaVariant> candidates = new List<QuestStageAreaVariant>();

        foreach (QuestStageAreaGroup group in areaGroups)
        {
            if (group == null || group.nodeType != nodeType || group.variants == null)
                continue;

            foreach (QuestStageAreaVariant variant in group.variants)
            {
                if (variant != null && variant.root != null)
                    candidates.Add(variant);
            }
        }

        if (candidates.Count == 0)
            return;

        activeVariant = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        activeVariant.root.SetActive(true);
        SetEncounterCameraActive(false);
    }

    public Transform GetBattleSpawnPoint()
    {
        if (activeVariant == null)
            return null;

        if (activeVariant.battleSpawnPoint != null)
            return activeVariant.battleSpawnPoint;

        return FindChildByName(activeVariant.root.transform, "BattleSpawnPoint");
    }

    public Camera GetEncounterCamera(string encounterId)
    {
        if (activeVariant == null)
            return null;

        if (!string.IsNullOrEmpty(encounterId) && activeVariant.encounterCameras != null)
        {
            foreach (QuestStageEncounterCameraBinding binding in activeVariant.encounterCameras)
            {
                if (binding != null && binding.camera != null &&
                    string.Equals(binding.encounterId, encounterId, StringComparison.OrdinalIgnoreCase))
                {
                    return binding.camera;
                }
            }
        }

        if (activeVariant.encounterCamera != null)
            return activeVariant.encounterCamera;

        Camera[] cameras = activeVariant.root.GetComponentsInChildren<Camera>(true);
        return cameras.Length == 1 ? cameras[0] : null;
    }

    public void SetEncounterCameraActive(bool active)
    {
        if (activeVariant == null || activeVariant.root == null)
            return;

        foreach (Camera camera in activeVariant.root.GetComponentsInChildren<Camera>(true))
        {
            camera.enabled = active;

            AudioListener listener = camera.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = active;
        }
    }

    public Transform GetEncounterPropsRoot()
    {
        if (activeVariant == null)
            return transform;

        return activeVariant.encounterPropsRoot != null
            ? activeVariant.encounterPropsRoot
            : activeVariant.root.transform;
    }

    private void SetAllAreasActive(bool active)
    {
        foreach (QuestStageAreaGroup group in areaGroups)
        {
            if (group == null || group.variants == null)
                continue;

            foreach (QuestStageAreaVariant variant in group.variants)
            {
                if (variant != null && variant.root != null)
                    variant.root.SetActive(active);
            }
        }
    }

    private Transform FindChildByName(Transform root, string namePrefix)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }
}
