using System.Text;
using TMPro;
using UnityEngine;

public class TownTraitPreviewCardUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text polarityText;
    public TMP_Text descriptionText;
    public TraitTooltipHandler tooltipHandler;

    [Header("Polarity Text Colors")]
    public Color positiveTextColor;
    public Color negativeTextColor;
    public Color mixedTextColor;

    public void Bind(TraitRuntimeData runtime, TraitDefinitionSO trait)
    {
        if (trait == null)
            return;

        if (nameText != null)
        {
            TraitGrade grade = runtime != null
                ? TraitGradeUtility.GetGrade(runtime.point, trait)
                : trait.defaultAcquireGrade;
            nameText.text = $"{grade} {trait.traitName}";
            nameText.color = GetPolarityTextColor(trait.polarity);
        }

        if (polarityText != null)
        {
            polarityText.text = trait.polarity switch
            {
                TraitPolarity.Positive => "긍정",
                TraitPolarity.Negative => "부정",
                TraitPolarity.Mixed => "혼합",
                _ => "-"
            };
            polarityText.color = GetPolarityTextColor(trait.polarity);
        }

        if (descriptionText != null)
            descriptionText.text = BuildDescription(runtime, trait);

        if (tooltipHandler != null)
            tooltipHandler.Bind(trait, runtime);

    }

    private Color GetPolarityTextColor(TraitPolarity polarity)
    {
        return polarity switch
        {
            TraitPolarity.Positive => positiveTextColor,
            TraitPolarity.Negative => negativeTextColor,
            TraitPolarity.Mixed => mixedTextColor,
            _ => nameText != null ? nameText.color : Color.white
        };
    }

    public static string BuildDescription(TraitRuntimeData runtime, TraitDefinitionSO trait)
    {
        StringBuilder text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(trait.description))
            text.Append(trait.description.Trim());

        int shift = TraitGradeUtility.GetGradeShift(runtime, trait);
        CharacterStats stats = trait.statDelta != null
            ? trait.statDelta.ShiftOperator(shift)
            : new CharacterStats();
        CharacterSpecialStats specialStats = trait.specialStatDelta != null
            ? trait.specialStatDelta.ShiftOperator(shift)
            : new CharacterSpecialStats();

        StringBuilder changes = new StringBuilder();
        AppendStatChanges(changes, stats, trait.isPercentage);
        AppendSpecialStatChanges(changes, specialStats);

        if (changes.Length > 0)
        {
            if (text.Length > 0)
                text.Append("\n");

            text.Append(changes);
        }

        if (trait.hasRequiredWeaponCondition)
        {
            CharacterStats conditionalStats = trait.conditionalStatDelta != null
                ? trait.conditionalStatDelta.ShiftOperator(shift)
                : new CharacterStats();
            CharacterSpecialStats conditionalSpecialStats = trait.conditionalSpecialStatDelta != null
                ? trait.conditionalSpecialStatDelta.ShiftOperator(shift)
                : new CharacterSpecialStats();
            StringBuilder conditionalChanges = new StringBuilder();
            AppendStatChanges(conditionalChanges, conditionalStats, trait.isPercentage);
            AppendSpecialStatChanges(conditionalChanges, conditionalSpecialStats);

            if (conditionalChanges.Length > 0)
            {
                if (text.Length > 0)
                    text.Append("\n");

                string condition = $"{GetWeaponTypeText(trait.requiredWeaponType)} 장착 시 ";
                text.Append(condition);
                text.Append(conditionalChanges.ToString().Replace("\n", $"\n{condition}"));
            }
        }

        return text.Length > 0 ? text.ToString() : "효과 설명 없음";
    }

    private static void AppendStatChanges(StringBuilder text, CharacterStats stats, bool percentage)
    {
        string suffix = percentage ? "%" : string.Empty;
        AppendValue(text, percentage ? "본체 근력" : "근력", stats.Strength, suffix);
        AppendValue(text, percentage ? "본체 기교" : "기교", stats.Dexterity, suffix);
        AppendValue(text, percentage ? "본체 속도" : "속도", stats.Speed, suffix);
        AppendValue(text, percentage ? "본체 지능" : "지능", stats.Intelligence, suffix);
        AppendValue(text, percentage ? "본체 지혜" : "지혜", stats.Wisdom, suffix);
        AppendValue(text, percentage ? "본체 건강" : "건강", stats.Health, suffix);
        AppendValue(text, percentage ? "본체 활력" : "활력", stats.Vitality, suffix);
        AppendValue(text, percentage ? "본체 인내" : "인내", stats.Endurance, suffix);
        AppendValue(text, percentage ? "본체 눈썰미" : "눈썰미", stats.Detection, suffix);
        AppendValue(text, percentage ? "본체 통찰력" : "통찰력", stats.Insight, suffix);
        AppendValue(text, "최대 HP", stats.MaxHp, suffix);
        AppendValue(text, "최대 지구력", stats.MaxStamina, suffix);
        AppendValue(text, "최대 정신력", stats.MaxMentality, suffix);
        AppendValue(text, "지구력 회복", stats.StaminaRecovery, suffix);
        AppendValue(text, "정신력 회복", stats.MentalityRecovery, suffix);
        AppendValue(text, "물리 공격력", stats.PhysicalAttack, suffix);
        AppendValue(text, "물리 방어력", stats.PhysicalDefense, suffix);
        AppendValue(text, "마법 공격력", stats.MagicalAttack, suffix);
        AppendValue(text, "마법 방어력", stats.MagicalDefense, suffix);
        AppendValue(text, "공격 속도", stats.AttackSpeed, suffix);
        AppendValue(text, "시전 속도", stats.CastSpeed, suffix);
        AppendValue(text, "화염 저항", stats.FireResistance, "%");
        AppendValue(text, "얼음 저항", stats.IceResistance, "%");
        AppendValue(text, "번개 저항", stats.LightningResistance, "%");
        AppendValue(text, "관통 저항", stats.PierceResistance, "%");
        AppendValue(text, "참격 저항", stats.SlashResistance, "%");
        AppendValue(text, "타격 저항", stats.SmashResistance, "%");
        AppendValue(text, "화염 특화", stats.FireAffinity, "%");
        AppendValue(text, "얼음 특화", stats.IceAffinity, "%");
        AppendValue(text, "번개 특화", stats.LightningAffinity, "%");
        AppendValue(text, "관통 특화", stats.PierceAffinity, "%");
        AppendValue(text, "참격 특화", stats.SlashAffinity, "%");
        AppendValue(text, "타격 특화", stats.SmashAffinity, "%");
    }

    private static void AppendSpecialStatChanges(StringBuilder text, CharacterSpecialStats stats)
    {
        AppendValue(text, "대성공 확률", stats.CriticalChance, "%");
        AppendValue(text, "대성공 피해", stats.CriticalDamageBonus, "%");
        AppendValue(text, "대응 성공률", stats.DefenseSkillSuccessRateBonus, "%");
        AppendValue(text, "상태이상 저항", stats.StatusResistance, "%");
        AppendValue(text, "기본기 스킬 특화", stats.BasicSpecialization, "%");
        AppendValue(text, "무기술 스킬 특화", stats.WeaponArtSpecialization, "%");
        AppendValue(text, "검술 스킬 특화", stats.SwordsmanshipSpecialization, "%");
        AppendValue(text, "궁술 스킬 특화", stats.ArcherySpecialization, "%");
        AppendValue(text, "방패술 스킬 특화", stats.ShieldArtSpecialization, "%");
        AppendValue(text, "체술 스킬 특화", stats.MartialArtSpecialization, "%");
        AppendValue(text, "단검술 스킬 특화", stats.DaggerArtSpecialization, "%");
        AppendValue(text, "마법 스킬 특화", stats.MagicSpecialization, "%");
        AppendValue(text, "몬스터 스킬 특화", stats.MonsterSpecialization, "%");
        AppendValue(text, "맵 탐지 범위", stats.MapDetectionRange);
        AppendValue(text, "처치 시 HP 회복", stats.KillHpRecovery);
        AppendValue(text, "처치 시 지구력 회복", stats.KillStaminaRecovery);
        AppendValue(text, "처치 시 정신력 회복", stats.KillMentalityRecovery);
        AppendValue(text, "스킬 시전 속도", stats.SkillActivationSpeedBonus, "%");
        AppendValue(text, "개인 휴식 시 HP 회복량", stats.PersonalRestHpRecoveryBonus, "%p");
        AppendValue(text, "개인 휴식 시 사기 회복량", stats.PersonalRestMoraleRecoveryBonus, "%p");
        AppendValue(text, "휴식 시 HP 회복량", stats.PartyRestHpRecoveryBonus, "%p");
        AppendValue(text, "휴식 시 사기 회복량", stats.PartyRestMoraleRecoveryBonus, "%p");
        AppendValue(text, "레벨업 최소 상승치", stats.MinLevelUpStatGainBonus);
        AppendValue(text, "레벨업 최대 상승치", stats.MaxLevelUpStatGainBonus);
    }

    private static void AppendValue(StringBuilder text, string label, int value, string suffix = "")
    {
        if (value == 0)
            return;

        if (text.Length > 0)
            text.Append('\n');

        text.Append($"{label} {(value > 0 ? "+" : "-")}{Mathf.Abs(value)}{suffix}");
    }

    private static void AppendValue(StringBuilder text, string label, float value, string suffix = "")
    {
        if (Mathf.Approximately(value, 0f))
            return;

        if (text.Length > 0)
            text.Append('\n');

        text.Append($"{label} {(value > 0f ? "+" : "-")}{Mathf.Abs(value):0.##}{suffix}");
    }

    private static string GetSubjectParticle(string label)
    {
        if (string.IsNullOrEmpty(label))
            return "가";

        char lastCharacter = label[label.Length - 1];

        if (lastCharacter < '가' || lastCharacter > '힣')
            return "가";

        return (lastCharacter - '가') % 28 == 0 ? "가" : "이";
    }

    private static string GetWeaponTypeText(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Two_HandedSword => "양손검",
            WeaponType.Greatsword => "대검",
            WeaponType.LongSword => "장검",
            WeaponType.Dagger => "단검",
            WeaponType.Bow => "활",
            WeaponType.Mace => "철퇴",
            WeaponType.Hammer => "망치",
            WeaponType.Shield => "방패",
            WeaponType.Axe => "도끼",
            WeaponType.Spear => "창",
            WeaponType.Staff => "지팡이",
            WeaponType.Book => "마도서",
            WeaponType.Orb => "오브",
            _ => weaponType.ToString()
        };
    }
}
