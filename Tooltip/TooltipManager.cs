using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("Tooltip UI Components")]
    public RawImage skillIcon;
    public Text skillNameText;
    public Text skillTypeText;
    public Text skillSpeedText;
    public Text costText;
    public Text descriptionText;
    public GameObject tooltipObject;

    public TextMeshProUGUI tooltipText;

    public Vector3 tooltipOffset = new Vector3(40, -25, 0);
    private bool isTooltipActive = false;

    public Dictionary<string, string> keywordTooltips = new Dictionary<string, string>
    {
        { "근력", "무기를 통한 물리 데미지에 영향을 주는 기본 스탯입니다." },
        { "기교", "경량 무기를 통한 물리 데미지에 영향을 주는 기본 스탯입니다." },
        { "속도", "지구력 수치와 공격 속도에 영향을 주는 기본 스탯입니다." },
        { "지능", "마법 공격력과 정신력 회복량에 영향을 주는 기본 스탯입니다." },
        { "지혜", "정신력과 시전 속도에 영향을 주는 기본 스탯입니다." },
        { "건강", "HP와 지구력 회복량에 영향을 주는 기본 스탯입니다." },
        { "인내", "방어력에 영향을 주는 기본 스탯입니다." },

        { "눈썰미", "간파 관련 판정에 영향을 주는 탐지 계열 스탯입니다." },
        { "통찰력", "간파 관련 판정에 영향을 주는 판단 계열 스탯입니다." },

        { "지구력", "물리 스킬에 사용되는 자원입니다." },
        { "정신력", "마법 스킬과 정신 계열 스킬에 사용되는 자원입니다." },

        { "물리 공격력", "물리 스킬과 무기 공격의 피해량에 영향을 주는 공격 능력치입니다." },
        { "마법 공격력", "마법 스킬의 피해량에 영향을 주는 공격 능력치입니다." },
        { "물리 방어력", "물리 피해를 줄이는 방어 능력치입니다." },
        { "마법 방어력", "마법 피해를 줄이는 방어 능력치입니다." },

        { "화염 저항", "화염 속성 피해를 줄이는 저항 능력치입니다." },
        { "물 저항", "물 속성 피해를 줄이는 저항 능력치입니다." },
        { "땅 저항", "땅 속성 피해를 줄이는 저항 능력치입니다." },
        { "바람 저항", "바람 속성 피해를 줄이는 저항 능력치입니다." },
        { "관통 저항", "관통 계열 피해를 줄이는 저항 능력치입니다." },
        { "참격 저항", "참격 계열 피해를 줄이는 저항 능력치입니다." },
        { "타격 저항", "타격 계열 피해를 줄이는 저항 능력치입니다." },

        { "화염 특화", "화염 속성 공격의 효율에 영향을 주는 속성 특화 능력치입니다." },
        { "물 특화", "물 속성 공격의 효율에 영향을 주는 속성 특화 능력치입니다." },
        { "땅 특화", "땅 속성 공격의 효율에 영향을 주는 속성 특화 능력치입니다." },
        { "바람 특화", "바람 속성 공격의 효율에 영향을 주는 속성 특화 능력치입니다." },
        { "관통 특화", "관통 계열 물리 공격의 효율에 영향을 주는 물리 특화 능력치입니다." },
        { "참격 특화", "참격 계열 물리 공격의 효율에 영향을 주는 물리 특화 능력치입니다." },
        { "타격 특화", "타격 계열 물리 공격의 효율에 영향을 주는 물리 특화 능력치입니다." }
    };

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (isTooltipActive)
            UpdateTooltipPosition(Input.mousePosition);
    }

    public void ShowTooltip(SkillQueueData queueData, Vector3 position)
    {
        if (queueData == null || queueData.skill == null)
            return;

        if (queueData.isConcealed)
        {
            switch (queueData.revealLevel)
            {
                case RevealLevel.None:
                    ShowConcealedTooltip(position);
                    return;

                case RevealLevel.Partial:
                    ShowPartialSkillTooltip(queueData.skill, position);
                    return;

                case RevealLevel.Full:
                    ShowTooltip(queueData.skill, position);
                    return;
            }
        }

        ShowTooltip(queueData.skill, position);
    }

    public void ShowTooltip(SkillDefinitionSO skill, Vector3 position)
    {
        if (skill == null)
            return;

        SetSkillTooltipMode();

        if (skillIcon != null)
            skillIcon.texture = skill.icon;

        if (skillNameText != null)
            skillNameText.text = skill.skillName;

        if (skillTypeText != null)
            skillTypeText.text = GetSkillTypeText(skill);

        if (skillSpeedText != null)
            skillSpeedText.text = $"발동속도: {skill.activationSpeed} / 스타일: {GetStyleText(skill.style)}";

        if (costText != null)
            costText.text = GetCostText(skill);

        if (descriptionText != null)
            descriptionText.text = BuildSkillDescription(skill);

        UpdateTooltipPosition(position);

        if (skill.uid != 0 && tooltipObject != null)
            tooltipObject.SetActive(true);

        isTooltipActive = true;
    }

    public void ShowTraitTooltip(int traitId, Vector3 position)
    {
        TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(traitId);
        ShowTraitTooltip(trait, position);
    }

    public void ShowTraitTooltip(TraitDefinitionSO trait, Vector3 position)
    {
        if (trait == null)
            return;

        SetTextTooltipMode();

        if (skillNameText != null)
            skillNameText.text = trait.traitName;

        if (skillTypeText != null)
            skillTypeText.text = "특성";

        if (skillSpeedText != null)
            skillSpeedText.text = "";

        if (costText != null)
            costText.text = "";

        if (descriptionText != null)
            descriptionText.text = trait.description;

        if (tooltipText != null)
            tooltipText.text = trait.description;

        UpdateTooltipPosition(position);

        if (tooltipObject != null)
            tooltipObject.SetActive(true);

        isTooltipActive = true;
    }

    public void ShowTooltip(string text, Vector3 position)
    {
        SetTextTooltipMode();

        if (tooltipText != null)
            tooltipText.text = text;

        if (skillNameText != null)
            skillNameText.text = "정보";

        if (skillTypeText != null)
            skillTypeText.text = "";

        if (skillSpeedText != null)
            skillSpeedText.text = "";

        if (costText != null)
            costText.text = "";

        if (descriptionText != null)
            descriptionText.text = text;

        UpdateTooltipPosition(position);

        if (tooltipObject != null)
            tooltipObject.SetActive(true);

        isTooltipActive = true;
    }

    public void ShowKeywordTooltip(string keyword, Vector3 position)
    {
        if (string.IsNullOrEmpty(keyword))
            return;

        if (keywordTooltips != null && keywordTooltips.TryGetValue(keyword, out string desc))
        {
            ShowTooltip(desc, position);
            return;
        }

        ShowTooltip(keyword, position);
    }

    public void HideTooltip()
    {
        if (tooltipObject != null)
            tooltipObject.SetActive(false);

        isTooltipActive = false;
    }

    public void UpdateTooltipPosition(Vector3 position)
    {
        if (tooltipObject == null)
            return;

        tooltipObject.transform.position = position + tooltipOffset;
    }

    private void ShowConcealedTooltip(Vector3 position)
    {
        SetTextTooltipMode();

        if (skillIcon != null)
            skillIcon.texture = null;

        if (skillNameText != null)
            skillNameText.text = "은폐된 스킬";

        if (skillTypeText != null)
            skillTypeText.text = "정보 없음";

        if (skillSpeedText != null)
            skillSpeedText.text = "간파 실패";

        if (costText != null)
            costText.text = "";

        if (descriptionText != null)
            descriptionText.text = "상대가 스킬 정보를 은폐했습니다. 타겟은 확인 가능하지만 스킬의 정체는 알 수 없습니다.";

        if (tooltipText != null)
            tooltipText.text = "은폐된 스킬";

        UpdateTooltipPosition(position);

        if (tooltipObject != null)
            tooltipObject.SetActive(true);

        isTooltipActive = true;
    }

    private void ShowPartialSkillTooltip(SkillDefinitionSO skill, Vector3 position)
    {
        if (skill == null)
            return;

        SetSkillTooltipMode();

        if (skillIcon != null)
            skillIcon.texture = null;

        if (skillNameText != null)
            skillNameText.text = "일부 간파된 스킬";

        if (skillTypeText != null)
            skillTypeText.text = $"{GetStyleText(skill.style)} 스타일 / {GetSkillRangeText(skill)}";

        if (skillSpeedText != null)
            skillSpeedText.text = $"속도: {GetSpeedRankText(skill.activationSpeed)}";

        if (costText != null)
            costText.text = $"위력: {GetPowerRankText(skill.GetTotalDamageMultiplier())}";

        if (descriptionText != null)
            descriptionText.text = "스킬의 일부 정보만 간파했습니다. 정확한 스킬명과 세부 효과는 알 수 없습니다.";

        UpdateTooltipPosition(position);

        if (tooltipObject != null)
            tooltipObject.SetActive(true);

        isTooltipActive = true;
    }

    private void SetSkillTooltipMode()
    {
        if (tooltipText != null)
            tooltipText.text = "";
    }

    private void SetTextTooltipMode()
    {
        if (skillIcon != null)
            skillIcon.texture = null;
    }

    private string GetSkillTypeText(SkillDefinitionSO skill)
    {
        string typeText = skill.type == SkillType.Physical ? "물리 스킬" : "마법 스킬";
        string rangeText = GetSkillRangeText(skill);

        if (skill.isCounterSkill)
            return $"{typeText} - {rangeText} 대응 - {GetCounterActionText(skill.counterActionType)}";

        return $"{typeText} - {rangeText}";
    }

    private string GetSkillRangeText(SkillDefinitionSO skill)
    {
        return skill.isRangedSkill ? "원거리" : "근접";
    }

    private string GetCostText(SkillDefinitionSO skill)
    {
        string text = "";

        if (skill.staminaCost != 0)
            text += $"지구력 : {skill.staminaCost} 소모";

        if (skill.mentalCost != 0)
        {
            if (!string.IsNullOrEmpty(text))
                text += " / ";

            text += $"정신력 : {skill.mentalCost} 소모";
        }

        if (string.IsNullOrEmpty(text))
            text = "소모 없음";

        return text;
    }

    private string BuildSkillDescription(SkillDefinitionSO skill)
    {
        string text = skill.description;

        if (!skill.isCounterSkill)
        {
            text += "\n\n피해 구성:";
            text += "\n" + skill.GetDamageSummaryText();

            float total = skill.GetTotalDamageMultiplier();

            if (total > 0f)
                text += $"\n총 위력: {Mathf.RoundToInt(total * 100f)}%";
        }

        if (skill.isCounterSkill)
        {
            text += $"\n\n대응 타입: {GetCounterActionText(skill.counterActionType)}";
        }

        if (skill.attackEffects != null && skill.attackEffects.Count > 0)
        {
            text += "\n공격 효과: ";

            for (int i = 0; i < skill.attackEffects.Count; i++)
            {
                text += GetAttackEffectText(skill.attackEffects[i]);

                if (i < skill.attackEffects.Count - 1)
                    text += ", ";
            }
        }

        if (skill.HasStatusEffects())
            text += "\n상태이상 효과 있음";

        return text;
    }

    private string GetStyleText(SkillStyle style)
    {
        return style switch
        {
            SkillStyle.Strength => "힘",
            SkillStyle.Dexterity => "기교",
            SkillStyle.Speed => "속도",
            _ => "-"
        };
    }

    private string GetCounterActionText(CounterActionType type)
    {
        return type switch
        {
            CounterActionType.Evade => "회피",
            CounterActionType.Parry => "패링",
            CounterActionType.Guard => "방어",
            CounterActionType.Break => "파훼",
            _ => "-"
        };
    }

    private string GetAttackEffectText(AttackEffectType type)
    {
        return type switch
        {
            AttackEffectType.Breakthrough => "돌파",
            _ => "-"
        };
    }

    private string GetPowerRankText(float damageMultiplier)
    {
        if (damageMultiplier >= 1.5f)
            return "고위력";

        if (damageMultiplier >= 0.9f)
            return "중위력";

        return "저위력";
    }

    private string GetSpeedRankText(float activationSpeed)
    {
        if (activationSpeed >= 1.2f)
            return "빠름";

        if (activationSpeed >= 0.9f)
            return "보통";

        return "느림";
    }
}