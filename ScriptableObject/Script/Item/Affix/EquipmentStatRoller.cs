using System.Reflection;
using UnityEngine;

public static class EquipmentStatRoller
{
    public static CharacterStats RollStats(CharacterStats min, CharacterStats max)
    {
        CharacterStats result = new CharacterStats();
        RollObjectFields(min, max, result);
        return result;
    }

    public static CharacterSpecialStats RollSpecialStats(CharacterSpecialStats min, CharacterSpecialStats max)
    {
        CharacterSpecialStats result = new CharacterSpecialStats();
        RollObjectFields(min, max, result);
        return result;
    }

    public static CharacterStats ScaleStats(CharacterStats source, int percent)
    {
        if (source == null)
            return new CharacterStats();

        CharacterStats result = source.Copy();
        ScaleObjectFields(result, percent);
        return result;
    }

    public static CharacterSpecialStats ScaleSpecialStats(CharacterSpecialStats source, int percent)
    {
        if (source == null)
            return new CharacterSpecialStats();

        CharacterSpecialStats result = source.Copy();
        ScaleObjectFields(result, percent);
        return result;
    }

    private static void RollObjectFields(object min, object max, object result)
    {
        if (min == null || max == null || result == null)
            return;

        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        FieldInfo[] fields = result.GetType().GetFields(flags);

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType == typeof(int))
            {
                int minValue = (int)field.GetValue(min);
                int maxValue = (int)field.GetValue(max);

                field.SetValue(result, RollInt(minValue, maxValue));
            }
            else if (field.FieldType == typeof(float))
            {
                float minValue = (float)field.GetValue(min);
                float maxValue = (float)field.GetValue(max);

                field.SetValue(result, RollFloat(minValue, maxValue));
            }
        }
    }

    private static void ScaleObjectFields(object target, int percent)
    {
        if (target == null)
            return;

        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        FieldInfo[] fields = target.GetType().GetFields(flags);

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType == typeof(int))
            {
                int value = (int)field.GetValue(target);
                field.SetValue(target, Mathf.RoundToInt(value * percent / 100f));
            }
            else if (field.FieldType == typeof(float))
            {
                float value = (float)field.GetValue(target);
                field.SetValue(target, value * percent / 100f);
            }
        }
    }

    private static int RollInt(int min, int max)
    {
        if (min == max)
            return min;

        if (max < min)
            return min;

        return Random.Range(min, max + 1);
    }

    private static float RollFloat(float min, float max)
    {
        if (Mathf.Approximately(min, max))
            return min;

        if (max < min)
            return min;

        return Random.Range(min, max);
    }
}