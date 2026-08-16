using System;
using UnityEngine;

[Serializable]
public class StatusStatFormulaData
{
    public float strength;
    public float dexterity;
    public float speed;
    public float intelligence;
    public float wisdom;
    public float health;
    public float endurance;
    public float detection;
    public float insight;

    public float Evaluate(CharacterStats stats)
    {
        if (stats == null)
            return 0f;

        return
            stats.Strength * strength +
            stats.Dexterity * dexterity +
            stats.Speed * speed +
            stats.Intelligence * intelligence +
            stats.Wisdom * wisdom +
            stats.Health * health +
            stats.Endurance * endurance +
            stats.Detection * detection +
            stats.Insight * insight;
    }
}