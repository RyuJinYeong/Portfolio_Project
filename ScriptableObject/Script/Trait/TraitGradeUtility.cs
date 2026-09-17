using System.Collections.Generic;
using UnityEngine;

public static class TraitGradeUtility
{
    public const int MaxPoint = 64;

    public static int GetGradeValue(TraitGrade grade)
    {
        return 1 << (int)grade;
    }

    public static int GetSignedValue(int point, TraitPolarity polarity)
    {
        return polarity switch
        {
            TraitPolarity.Positive => point,
            TraitPolarity.Negative => -point,
            TraitPolarity.Mixed => 0,
            _ => point
        };
    }

    public static TraitGrade GetGrade(int point)
    {
        if (point >= 64) return TraitGrade.S;
        if (point >= 32) return TraitGrade.A;
        if (point >= 16) return TraitGrade.B;
        if (point >= 8) return TraitGrade.C;
        if (point >= 4) return TraitGrade.D;
        if (point >= 2) return TraitGrade.E;
        return TraitGrade.F;
    }

    public static float GetProgress01(int point)
    {
        TraitGrade grade = GetGrade(point);

        if (grade == TraitGrade.S)
            return 1f;

        int current = GetGradeValue(grade);
        int next = GetGradeValue((TraitGrade)((int)grade + 1));

        return (float)(point - current) / (next - current);
    }

    public static int GetGradeShift(TraitRuntimeData runtime, TraitDefinitionSO def)
    {
        if (runtime == null || def == null)
            return 0;

        if (!def.canGradeUp)
            return 0;

        TraitGrade currentGrade = GetGrade(runtime.point);

        int shift = (int)currentGrade - (int)def.defaultAcquireGrade;

        return Mathf.Max(0, shift);
    }

    public static int GetGradeMultiplier(TraitRuntimeData runtime, TraitDefinitionSO def)
    {
        int shift = GetGradeShift(runtime, def);
        return 1 << shift;
    }

    public static bool AddTrait(List<TraitRuntimeData> traits, TraitDefinitionSO def, TraitGrade acquiredGrade)
    {
        if (traits == null || def == null)
            return false;

        int addPoint = GetGradeValue(acquiredGrade);

        TraitRuntimeData owned = traits.Find(t => t.traitId == def.id);

        if (owned == null)
        {
            traits.Add(new TraitRuntimeData
            {
                traitId = def.id,
                point = Mathf.Min(addPoint, MaxPoint)
            });

            return true;
        }

        if (!def.canGradeUp)
            return false;

        owned.point = Mathf.Min(owned.point + addPoint, MaxPoint);
        return true;
    }

    public static bool RemoveTrait(List<TraitRuntimeData> traits, TraitDefinitionSO def, TraitGrade removedGrade)
    {
        if (traits == null || def == null)
            return false;

        TraitRuntimeData owned = traits.Find(t => t.traitId == def.id);

        if (owned == null)
            return false;

        if (!def.canGradeUp)
        {
            traits.Remove(owned);
            return true;
        }

        int removePoint = GetGradeValue(removedGrade);
        owned.point -= removePoint;

        if (owned.point <= 0)
            traits.Remove(owned);

        return true;
    }
}
