using System.Collections.Generic;
using System.Linq;

public static class EquipmentPoolUtility
{
    public static List<EquipmentDefinitionSO> GetRewardableEquipments(
        IEnumerable<EquipmentDefinitionSO> source)
    {
        if (source == null)
            return new List<EquipmentDefinitionSO>();

        return source
            .Where(e => e != null && e.canDrop)
            .ToList();
    }

    public static List<EquipmentDefinitionSO> FilterByTier(
        IEnumerable<EquipmentDefinitionSO> source,
        int tier)
    {
        if (source == null)
            return new List<EquipmentDefinitionSO>();

        return source
            .Where(e => e != null && e.canDrop && e.tier == tier)
            .ToList();
    }

    public static List<EquipmentDefinitionSO> FilterByTierRange(
        IEnumerable<EquipmentDefinitionSO> source,
        int minTier,
        int maxTier)
    {
        if (source == null)
            return new List<EquipmentDefinitionSO>();

        return source
            .Where(e =>
                e != null &&
                e.canDrop &&
                e.tier >= minTier &&
                e.tier <= maxTier)
            .ToList();
    }

    public static List<EquipmentDefinitionSO> FilterByEquipType(
        IEnumerable<EquipmentDefinitionSO> source,
        EquipmentType equipType)
    {
        if (source == null)
            return new List<EquipmentDefinitionSO>();

        return source
            .Where(e =>
                e != null &&
                e.canDrop &&
                e.equipType == equipType)
            .ToList();
    }
}