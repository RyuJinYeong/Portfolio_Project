using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TownSkillPreviewCardUI : MonoBehaviour
{
    public RawImage icon;
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text costText;
    public TMP_Text descriptionText;
    public SkillButton tooltipHandler;

    public void Bind(SkillDefinitionSO skill)
    {
        if (skill == null)
            return;

        if (icon != null)
            icon.texture = skill.icon;

        if (nameText != null)
            nameText.text = skill.skillName;

        if (typeText != null)
            typeText.text = skill.isCounterSkill ? "대응 스킬" : "공격 스킬";

        if (costText != null)
            costText.text = BuildCostText(skill);

        if (descriptionText != null)
            descriptionText.text = BuildEffectText(skill);

        if (tooltipHandler != null)
        {
            tooltipHandler.skill = skill;
            tooltipHandler.isCounterSkill = skill.isCounterSkill;
        }
    }

    private static string BuildCostText(SkillDefinitionSO skill)
    {
        List<string> costs = new List<string>();

        if (skill.staminaCost > 0)
            costs.Add($"지구력 {skill.staminaCost} 소모");

        if (skill.mentalCost > 0)
            costs.Add($"정신력 {skill.mentalCost} 소모");

        return costs.Count > 0 ? string.Join(" / ", costs) : "리소스 소모 없음";
    }

    private static string BuildEffectText(SkillDefinitionSO skill)
    {
        if (skill.isCounterSkill)
        {
            if (skill.counterActionType == CounterActionType.Evade)
            {
                int reductionPercent = Mathf.RoundToInt(skill.minEvadeReductionRate * 100f);
                return $"대응 성공시 최소 {reductionPercent}%의 피해량 감소";
            }

            int armorPercent = Mathf.RoundToInt(skill.successArmorAttackMultiplier * 100f);
            string armorType = skill.type == SkillType.Magical ? "마법 방어도" : "물리 방어도";
            return $"대응 성공시 공격력의 {armorPercent}% {armorType} 획득";
        }

        if (skill.GetTotalDamageMultiplier() > 0f)
            return skill.GetDamageSummaryText().Replace("\n", " / ");

        return "피해 없음";
    }
}
