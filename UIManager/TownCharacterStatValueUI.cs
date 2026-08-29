using TMPro;
using UnityEngine;

public enum TownCharacterStatDisplayType
{
    Strength,
    Dexterity,
    Speed,
    Intelligence,
    Wisdom,
    Health,
    Vitality,
    Endurance,
    Detection,
    Insight,
    PhysicalAttack,
    PhysicalDefense,
    MagicalAttack,
    MagicalDefense,
    AttackSpeed,
    CastSpeed,
    MaxHp,
    MaxStamina,
    MaxMentality,
    StaminaRecovery,
    MentalityRecovery,
    FireResistanceAffinity,
    IceResistanceAffinity,
    LightningResistanceAffinity,
    PierceResistanceAffinity,
    SlashResistanceAffinity,
    SmashResistanceAffinity,
    FireResistance,
    FireAffinity,
    IceResistance,
    IceAffinity,
    LightningResistance,
    LightningAffinity,
    PierceResistance,
    PierceAffinity,
    SlashResistance,
    SlashAffinity,
    SmashResistance,
    SmashAffinity
}

public class TownCharacterStatValueUI : MonoBehaviour
{
    public TownCharacterStatDisplayType statType;
    public TMP_Text valueText;

    public void Refresh(CharacterStats stats)
    {
        if (valueText == null)
        {
            TMP_Text labelText = GetComponentInChildren<TMP_Text>(true);

            if (labelText != null)
            {
                string value = stats == null ? "-" : GetValue(stats);
                labelText.text = $"{gameObject.name}  {value}";
            }

            return;
        }

        valueText.text = stats == null ? "-" : GetValue(stats);
    }

    private string GetValue(CharacterStats stats)
    {
        return statType switch
        {
            TownCharacterStatDisplayType.Strength => stats.Strength.ToString(),
            TownCharacterStatDisplayType.Dexterity => stats.Dexterity.ToString(),
            TownCharacterStatDisplayType.Speed => stats.Speed.ToString(),
            TownCharacterStatDisplayType.Intelligence => stats.Intelligence.ToString(),
            TownCharacterStatDisplayType.Wisdom => stats.Wisdom.ToString(),
            TownCharacterStatDisplayType.Health => stats.Health.ToString(),
            TownCharacterStatDisplayType.Vitality => stats.Vitality.ToString(),
            TownCharacterStatDisplayType.Endurance => stats.Endurance.ToString(),
            TownCharacterStatDisplayType.Detection => stats.Detection.ToString(),
            TownCharacterStatDisplayType.Insight => stats.Insight.ToString(),
            TownCharacterStatDisplayType.PhysicalAttack => stats.PhysicalAttack.ToString(),
            TownCharacterStatDisplayType.PhysicalDefense => stats.PhysicalDefense.ToString(),
            TownCharacterStatDisplayType.MagicalAttack => stats.MagicalAttack.ToString(),
            TownCharacterStatDisplayType.MagicalDefense => stats.MagicalDefense.ToString(),
            TownCharacterStatDisplayType.AttackSpeed => stats.AttackSpeed.ToString("0.##"),
            TownCharacterStatDisplayType.CastSpeed => stats.CastSpeed.ToString("0.##"),
            TownCharacterStatDisplayType.MaxHp => stats.MaxHp.ToString(),
            TownCharacterStatDisplayType.MaxStamina => stats.MaxStamina.ToString(),
            TownCharacterStatDisplayType.MaxMentality => stats.MaxMentality.ToString(),
            TownCharacterStatDisplayType.StaminaRecovery => stats.StaminaRecovery.ToString(),
            TownCharacterStatDisplayType.MentalityRecovery => stats.MentalityRecovery.ToString(),
            TownCharacterStatDisplayType.FireResistanceAffinity =>
                $"저항 {stats.FireResistance}%  /  특화 {stats.FireAffinity}%",
            TownCharacterStatDisplayType.IceResistanceAffinity =>
                $"저항 {stats.IceResistance}%  /  특화 {stats.IceAffinity}%",
            TownCharacterStatDisplayType.LightningResistanceAffinity =>
                $"저항 {stats.LightningResistance}%  /  특화 {stats.LightningAffinity}%",
            TownCharacterStatDisplayType.PierceResistanceAffinity =>
                $"저항 {stats.PierceResistance}%  /  특화 {stats.PierceAffinity}%",
            TownCharacterStatDisplayType.SlashResistanceAffinity =>
                $"저항 {stats.SlashResistance}%  /  특화 {stats.SlashAffinity}%",
            TownCharacterStatDisplayType.SmashResistanceAffinity =>
                $"저항 {stats.SmashResistance}%  /  특화 {stats.SmashAffinity}%",
            TownCharacterStatDisplayType.FireResistance => $"{stats.FireResistance}%",
            TownCharacterStatDisplayType.FireAffinity => $"{stats.FireAffinity}%",
            TownCharacterStatDisplayType.IceResistance => $"{stats.IceResistance}%",
            TownCharacterStatDisplayType.IceAffinity => $"{stats.IceAffinity}%",
            TownCharacterStatDisplayType.LightningResistance => $"{stats.LightningResistance}%",
            TownCharacterStatDisplayType.LightningAffinity => $"{stats.LightningAffinity}%",
            TownCharacterStatDisplayType.PierceResistance => $"{stats.PierceResistance}%",
            TownCharacterStatDisplayType.PierceAffinity => $"{stats.PierceAffinity}%",
            TownCharacterStatDisplayType.SlashResistance => $"{stats.SlashResistance}%",
            TownCharacterStatDisplayType.SlashAffinity => $"{stats.SlashAffinity}%",
            TownCharacterStatDisplayType.SmashResistance => $"{stats.SmashResistance}%",
            TownCharacterStatDisplayType.SmashAffinity => $"{stats.SmashAffinity}%",
            _ => "-"
        };
    }
}
