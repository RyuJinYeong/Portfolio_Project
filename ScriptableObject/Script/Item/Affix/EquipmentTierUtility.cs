public static class EquipmentTierUtility
{
    public static int GetAffixTierMultiplierPercent(int tier)
    {
        if (tier <= 1)
            return 100;

        switch (tier)
        {
            case 2:
                return 140;

            case 3:
                return 180;

            case 4:
                return 230;

            case 5:
                return 280;

            default:
                return 280 + (tier - 5) * 50;
        }
    }
}