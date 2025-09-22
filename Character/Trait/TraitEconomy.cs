using System;

public static class TraitEconomy
{
    public static int BaseCostByGrade(TraitGrade g) => g switch
    {
        TraitGrade.F => 1,
        TraitGrade.E => 2,
        TraitGrade.D => 3,
        TraitGrade.C => 5,
        TraitGrade.B => 8,
        TraitGrade.A => 13,
        TraitGrade.S => 21,
        _ => 0
    };

    public static int Cost(TraitBase t)
    {
        int baseCost = BaseCostByGrade(t.Grade);
        return t.Polarity switch
        {
            TraitPolarity.Positive => baseCost * Math.Max(1, t.Level),
            TraitPolarity.Negative => -baseCost,
            TraitPolarity.Mixed => 0,
            _ => 0
        };
    }
}
