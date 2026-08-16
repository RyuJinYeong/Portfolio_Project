using System.Collections.Generic;

public static class EquipmentInstanceRepository
{
    private static Dictionary<string, GeneratedEquipmentData> runtimeEquipments = new();

    public static GeneratedEquipmentData Get(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return null;

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData != null && playerData.generatedEquipments != null)
        {
            GeneratedEquipmentData playerEquipment =
                playerData.generatedEquipments.Find(e => e != null && e.instanceId == instanceId);

            if (playerEquipment != null)
                return playerEquipment;
        }

        if (runtimeEquipments != null &&
            runtimeEquipments.TryGetValue(instanceId, out GeneratedEquipmentData runtimeEquipment))
        {
            return runtimeEquipment;
        }

        return null;
    }

    public static void AddToPlayer(GeneratedEquipmentData equipment)
    {
        if (equipment == null)
            return;

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData == null)
            return;

        if (playerData.generatedEquipments == null)
            playerData.generatedEquipments = new List<GeneratedEquipmentData>();

        EnsureInstanceId(equipment);

        GeneratedEquipmentData existing =
            playerData.generatedEquipments.Find(e => e != null && e.instanceId == equipment.instanceId);

        if (existing != null)
            return;

        playerData.generatedEquipments.Add(equipment);
    }

    public static void AddRuntime(GeneratedEquipmentData equipment)
    {
        if (equipment == null)
            return;

        EnsureInstanceId(equipment);

        if (runtimeEquipments == null)
            runtimeEquipments = new Dictionary<string, GeneratedEquipmentData>();

        runtimeEquipments[equipment.instanceId] = equipment;
    }

    public static void PromoteRuntimeToPlayer(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return;

        if (runtimeEquipments == null)
            return;

        if (!runtimeEquipments.TryGetValue(instanceId, out GeneratedEquipmentData equipment))
            return;

        AddToPlayer(equipment);
        runtimeEquipments.Remove(instanceId);
    }

    public static void RemoveFromPlayer(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return;

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData == null || playerData.generatedEquipments == null)
            return;

        playerData.generatedEquipments.RemoveAll(e => e != null && e.instanceId == instanceId);
    }

    public static void RemoveRuntime(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return;

        if (runtimeEquipments == null)
            return;

        runtimeEquipments.Remove(instanceId);
    }

    public static void ClearRuntime()
    {
        if (runtimeEquipments != null)
            runtimeEquipments.Clear();
    }

    private static void EnsureInstanceId(GeneratedEquipmentData equipment)
    {
        if (equipment == null)
            return;

        if (string.IsNullOrEmpty(equipment.instanceId))
            equipment.instanceId = System.Guid.NewGuid().ToString();
    }
}