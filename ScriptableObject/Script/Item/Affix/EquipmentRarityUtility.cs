using UnityEngine;

public static class EquipmentRarityUtility
{
    public static Color GetColor(EquipmentRarity rarity)
    {
        switch (rarity)
        {
            case EquipmentRarity.Common:
                return Color.white;

            case EquipmentRarity.Uncommon:
                return new Color(0.3f, 1f, 0.3f);

            case EquipmentRarity.Rare:
                return new Color(0.3f, 0.6f, 1f);

            case EquipmentRarity.Epic:
                return new Color(0.75f, 0.3f, 1f);

            case EquipmentRarity.Legendary:
                return new Color(1f, 0.55f, 0.1f);

            default:
                return Color.white;
        }
    }

    public static int GetBaseStatBonusPercent(EquipmentRarity rarity)
    {
        switch (rarity)
        {
            case EquipmentRarity.Common:
                return 100;

            case EquipmentRarity.Uncommon:
                return 110;

            case EquipmentRarity.Rare:
                return 120;

            case EquipmentRarity.Epic:
                return 130;

            case EquipmentRarity.Legendary:
                return 100;

            default:
                return 100;
        }
    }

    public static bool HasPrefix(EquipmentRarity rarity)
    {
        return rarity >= EquipmentRarity.Uncommon && rarity != EquipmentRarity.Legendary;
    }

    public static bool HasSuffix(EquipmentRarity rarity)
    {
        return rarity >= EquipmentRarity.Rare && rarity != EquipmentRarity.Legendary;
    }

    public static bool HasGuaranteedSpecial(EquipmentRarity rarity)
    {
        return rarity == EquipmentRarity.Epic;
    }

    public static bool CanRollSpecial(EquipmentRarity rarity)
    {
        return rarity == EquipmentRarity.Rare || rarity == EquipmentRarity.Epic;
    }

    public static int GetRareSpecialChancePercent()
    {
        return 15;
    }
}