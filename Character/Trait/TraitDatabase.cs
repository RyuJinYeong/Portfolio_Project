using System;
using System.Collections.Generic;

public static class TraitDataBase
{
    private static readonly Dictionary<int, Func<TraitBase>> Map = new()
    {
        { 1001, () => new DurableTrait() },
        { 1002, () => new BarbarianPowerTrait() },
        { 1003, () => new DeftnessTrait() },
        { 1004, () => new SwiftMovementTrait() },
        { 1005, () => new KeenEyeTrait() },
        { 1006, () => new KeenInsightTrait() },
        { 1400, () => new OneArmedTrait() },
        { 1401, () => new DunceTrait() },

        { 2550, () => new SwordMasteryTrait() },
        { 2551, () => new BowMasteryTrait() },

        { 2801, () => new FireElementalAptitudeTrait() },
        { 2802, () => new WaterElementalAptitudeTrait() },
        { 2803, () => new EarthElementalAptitudeTrait() },
        { 2804, () => new WindElementalAptitudeTrait() },
    };

    public static TraitBase Create(int id) =>
        Map.TryGetValue(id, out var ctor) ? ctor() : null;
}
