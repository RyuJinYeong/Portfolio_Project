using System;
using UnityEngine;

public static class TraitEconomy
{
    public static int BaseCostByGrade(TraitGrade g) => g switch
    {
        TraitGrade.F => 1,
        TraitGrade.E => 2,
        TraitGrade.D => 3,
        TraitGrade.C => 5,  
        TraitGrade.B => 12,  
        TraitGrade.A => 25,
        TraitGrade.S => 55,
        _ => 0
    };

    public static int Cost(TraitBase t)
    {
        int baseCost = BaseCostByGrade(t.Grade);
        return t.Polarity switch
        {
            TraitPolarity.Positive => baseCost * Mathf.Max(1, t.Level),
            TraitPolarity.Negative => baseCost * Mathf.Max(1, t.Level),
            TraitPolarity.Mixed => 0,
            _ => 0
        };
    }
}
