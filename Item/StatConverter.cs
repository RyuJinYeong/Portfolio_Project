using SoftKitty.InventoryEngine;
using System.Collections.Generic;
using UnityEngine;

public static class StatsConverter
{
    public static List<Attribute> ConvertStatsToAttributes(CharacterStats stats)
    {
        List<Attribute> attributes = new List<Attribute>();

        attributes.Add(new Attribute { key = "str", name = "Strength", value = stats.Strength.ToString() });
        attributes.Add(new Attribute { key = "dex", name = "Dexterity", value = stats.Dexterity.ToString() });
        attributes.Add(new Attribute { key = "spd", name = "Speed", value = stats.Speed.ToString() });
        attributes.Add(new Attribute { key = "inte", name = "Intelligence", value = stats.Intelligence.ToString() });
        attributes.Add(new Attribute { key = "wis", name = "Wisdom", value = stats.Wisdom.ToString() });
        attributes.Add(new Attribute { key = "hth", name = "Health", value = stats.Health.ToString() });
        attributes.Add(new Attribute { key = "end", name = "Endurance", value = stats.Endurance.ToString() });
        attributes.Add(new Attribute { key = "det", name = "Detection", value = stats.Detection.ToString() });
        attributes.Add(new Attribute { key = "ins", name = "Insight", value = stats.Insight.ToString() });
        attributes.Add(new Attribute { key = "atkspd", name = "AttackSpeed", value = stats.AttackSpeed.ToString() });
        attributes.Add(new Attribute { key = "castspd", name = "CastSpeed", value = stats.CastSpeed.ToString() });
        attributes.Add(new Attribute { key = "wepatkspd", name = "WeaponAttackSpeedMultiplier", value = stats.WeaponAttackSpeedMultiplier.ToString() });
        attributes.Add(new Attribute { key = "wepcastspd", name = "WeaponCastSpeedMultiplier", value = stats.WeaponCastSpeedMultiplier.ToString() });
        attributes.Add(new Attribute { key = "mhp", name = "MaxHp", value = stats.MaxHp.ToString() });
        attributes.Add(new Attribute { key = "mmt", name = "MaxMentality", value = stats.MaxMentality.ToString() });
        attributes.Add(new Attribute { key = "mst", name = "MaxStamina", value = stats.MaxStamina.ToString() });
        attributes.Add(new Attribute { key = "stre", name = "StaminaRecovery", value = stats.StaminaRecovery.ToString() });
        attributes.Add(new Attribute { key = "mtre", name = "MentalityRecovery", value = stats.MentalityRecovery.ToString() });
        attributes.Add(new Attribute { key = "phatk", name = "PhysicalAttack", value = stats.PhysicalAttack.ToString() });
        attributes.Add(new Attribute { key = "mgatk", name = "MagicalAttack", value = stats.MagicalAttack.ToString() });
        attributes.Add(new Attribute { key = "phdef", name = "PhysicalDefense", value = stats.PhysicalDefense.ToString() });
        attributes.Add(new Attribute { key = "mgdef", name = "MagicalDefense", value = stats.MagicalDefense.ToString() });
        attributes.Add(new Attribute { key = "fireres", name = "FireResistance", value = stats.FireResistance.ToString() });
        attributes.Add(new Attribute { key = "waterres", name = "WaterResistance", value = stats.WaterResistance.ToString() });
        attributes.Add(new Attribute { key = "earthres", name = "EarthResistance", value = stats.EarthResistance.ToString() });
        attributes.Add(new Attribute { key = "windres", name = "WindResistance", value = stats.WindResistance.ToString() });
        attributes.Add(new Attribute { key = "pierres", name = "PierceResistance", value = stats.PierceResistance.ToString() });
        attributes.Add(new Attribute { key = "slres", name = "SlashResistance", value = stats.SlashResistance.ToString() });
        attributes.Add(new Attribute { key = "smres", name = "SmashResistance", value = stats.SmashResistance.ToString() });
        attributes.Add(new Attribute { key = "fireaff", name = "FireAffinity", value = stats.FireAffinity.ToString() });
        attributes.Add(new Attribute { key = "wateraff", name = "WaterAffinity", value = stats.WaterAffinity.ToString() });
        attributes.Add(new Attribute { key = "earaff", name = "EarthAffinity", value = stats.EarthAffinity.ToString() });
        attributes.Add(new Attribute { key = "windaff", name = "WindAffinity", value = stats.WindAffinity.ToString() });
        attributes.Add(new Attribute { key = "pieaff", name = "PierceAffinity", value = stats.PierceAffinity.ToString() });
        attributes.Add(new Attribute { key = "slsaff", name = "SlashAffinity", value = stats.SlashAffinity.ToString() });
        attributes.Add(new Attribute { key = "smaff", name = "SmashAffinity", value = stats.SmashAffinity.ToString() });

        return attributes;
    }
}
