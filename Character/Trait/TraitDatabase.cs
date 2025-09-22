using System.Collections.Generic;

public static class TraitDatabase
{
    private static readonly Dictionary<int, System.Func<TraitBase>> Map = new()
    {
        { 1000, () => new DurableTrait() },
        { 1001, () => new BarbarianPowerTrait() },
        { 1002, () => new DeftnessTrait() },
        { 1003, () => new SwiftMovementTrait() },
        { 1004, () => new KeenEyeTrait() },
        { 1005, () => new KeenInsightTrait() },
        { 1006, () => new SwordMasteryTrait() },
        { 1007, () => new BowMasteryTrait() },

        { 1010, () => new FireElementalAptitudeTrait() },
        { 1011, () => new WaterElementalAptitudeTrait() },
        { 1012, () => new EarthElementalAptitudeTrait() },
        { 1013, () => new WindElementalAptitudeTrait() },
        { 1014, () => new BasicElementalAptitudeTrait() },

        { 5000, () => new OneArmedTrait() },
        { 5001, () => new DunceTrait() },
    };
    public static TraitBase Create(int id) =>
        Map.TryGetValue(id, out var ctor) ? ctor() : null;
}
