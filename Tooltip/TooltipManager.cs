using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
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
    public GameObject simpleTooltipObject;

    public TextMeshProUGUI tooltipText;

    public Vector3 tooltipOffset = new Vector3(40, -25, 0);
    public Vector2 minimumSimpleTooltipSize = new Vector2(80f, 50f);
    public float maximumSimpleTooltipWidth = 160f;
    public Vector2 simpleTooltipPadding = new Vector2(8f, 8f);
    private bool isTooltipActive = false;
    private string lastSizedTooltipText;
    private GameObject activeTooltipObject;

    public Dictionary<string, string> keywordTooltips = new Dictionary<string, string>
    {
        { "근력", "무기를 통한 물리 데미지에 영향을 주는 기본 스탯입니다." },
        { "기교", "경량 무기를 통한 물리 데미지에 영향을 주는 기본 스탯입니다." },
        { "속도", "최대 지구력과 공격 속도에 영향을 주는 기본 스탯입니다." },
        { "지능", "마법 공격력과 정신력 회복량에 영향을 주는 기본 스탯입니다." },
        { "지혜", "최대 정신력과 시전 속도에 영향을 주는 기본 스탯입니다." },
        { "건강", "HP와 상태이상 저항에 영향을 주는 기본 스탯입니다." },
        { "활력", "최대 체력과 지구력 회복량에 영향을 주는 기본 스탯입니다." },
        { "인내", "방어력에 영향을 주는 기본 스탯입니다." },        
        { "LV", "캐릭터의 현재 레벨입니다." },

        { "눈썰미", "간파 관련 판정에 영향을 주는 탐지 계열 스탯입니다." },
        { "통찰력", "통찰 관련 판정에 영향을 주는 탐지 계열 스탯입니다." },

        { "HP", "캐릭터가 보유할 수 있는 최대 체력입니다." },
        { "지구력", "물리 스킬에 사용되는 자원입니다." },
        { "정신력", "마법 스킬과 정신 계열 스킬에 사용되는 자원입니다." },
        { "턴당 지구력 회복량", "한 턴마다 회복하는 지구력입니다." },
        { "턴당 정신력 회복량", "한 턴마다 회복하는 정신력입니다." },

        { "물리 공격력", "물리 스킬과 무기 공격의 피해량에 영향을 주는 공격 능력치입니다." },
        { "마법 공격력", "마법 스킬의 피해량에 영향을 주는 공격 능력치입니다." },
        { "물리 방어력", "물리 피해를 줄이는 방어 능력치입니다." },
        { "마법 방어력", "마법 피해를 줄이는 방어 능력치입니다." },
        { "공격 속도", "물리 행동의 속도에 영향을 주는 능력치입니다." },
        { "시전 속도", "마법 행동의 속도에 영향을 주는 능력치입니다." },

        { "화염 저항", "화염 속성 피해를 줄이는 저항 능력치입니다." },
        { "번개 저항", "번개 속성 피해를 줄이는 저항 능력치입니다." },
        { "얼음 저항", "얼음 속성 피해를 줄이는 저항 능력치입니다." },
        { "관통 저항", "관통 계열 피해를 줄이는 저항 능력치입니다." },
        { "참격 저항", "참격 계열 피해를 줄이는 저항 능력치입니다." },
        { "타격 저항", "타격 계열 피해를 줄이는 저항 능력치입니다." },

        { "화염 특화", "화염 속성 공격의 효율에 영향을 주는 속성 특화 능력치입니다." },
        { "번개 특화", "번개 속성 공격의 효율에 영향을 주는 속성 특화 능력치입니다." },
        { "얼음 특화", "얼음 속성 공격의 효율에 영향을 주는 속성 특화 능력치입니다." },
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

        if (tooltipObject != null)
        {
            tooltipObject.transform.SetAsLastSibling();

            Canvas tooltipCanvas = tooltipObject.GetComponent<Canvas>();

            if (tooltipCanvas != null)
            {
                tooltipCanvas.overrideSorting = true;
                tooltipCanvas.sortingOrder = short.MaxValue;
            }
        }

        if (simpleTooltipObject != null && simpleTooltipObject != tooltipObject)
        {
            simpleTooltipObject.transform.SetAsLastSibling();

            Canvas tooltipCanvas = simpleTooltipObject.GetComponent<Canvas>();

            if (tooltipCanvas != null)
            {
                tooltipCanvas.overrideSorting = true;
                tooltipCanvas.sortingOrder = short.MaxValue;
            }
        }
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

        if (tooltipText != null && skillNameText == null && descriptionText == null)
            tooltipText.text = BuildSimpleSkillTooltip(skill);

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

        if (skill.uid != 0 && activeTooltipObject != null)
            activeTooltipObject.SetActive(true);

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
            tooltipText.text = $"<b>{trait.traitName} ({trait.defaultAcquireGrade})</b>\n{trait.description}";

        UpdateTooltipPosition(position);

        if (activeTooltipObject != null)
            activeTooltipObject.SetActive(true);

        isTooltipActive = true;
    }

    public void ShowItemTooltip(InventorySlotData slot, Vector3 position)
    {
        if (slot == null || GameDataRegistry.Instance == null)
            return;

        ItemDefinitionSO item = GameDataRegistry.Instance.GetItem(slot.itemUid);

        if (item == null)
            return;

        SetSkillTooltipMode();

        EquipmentRuntimeData equipment = item is EquipmentDefinitionSO
            ? EquipmentRuntimeResolver.Resolve(slot.itemUid, slot.equipmentInstanceId)
            : null;

        string displayName = slot.IsMonsterEssence() &&
                             !string.IsNullOrEmpty(slot.essenceMonsterName)
            ? $"{slot.essenceMonsterName}의 정수"
            : equipment != null && !string.IsNullOrEmpty(equipment.displayName)
                ? equipment.displayName
                : item.itemName;

        string typeText = GetItemTypeText(item, equipment);
        string detailText = BuildItemDescription(item, equipment);

        if (skillIcon != null)
            skillIcon.texture = item.icon;

        if (skillNameText != null)
            skillNameText.text = displayName;

        if (skillTypeText != null)
            skillTypeText.text = typeText;

        if (skillSpeedText != null)
        {
            skillSpeedText.text = equipment != null
                ? $"티어 {equipment.tier} / {equipment.rarity}"
                : slot.count > 1
                    ? $"보유 수량: {slot.count}"
                    : "";
        }

        if (costText != null)
            costText.text = $"가격: {item.price} / 무게: {item.weight:0.##}";

        if (descriptionText != null)
            descriptionText.text = detailText;

        if (tooltipText != null && skillNameText == null && descriptionText == null)
        {
            tooltipText.text =
                $"<b>{displayName}</b>\n{typeText}\n{detailText}";
        }

        UpdateTooltipPosition(position);

        if (activeTooltipObject != null)
            activeTooltipObject.SetActive(true);

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

        if (activeTooltipObject != null)
            activeTooltipObject.SetActive(true);

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

        if (simpleTooltipObject != null && simpleTooltipObject != tooltipObject)
            simpleTooltipObject.SetActive(false);

        activeTooltipObject = null;
        isTooltipActive = false;
    }

    public void UpdateTooltipPosition(Vector3 position)
    {
        GameObject currentTooltipObject = activeTooltipObject != null
            ? activeTooltipObject
            : tooltipObject;

        if (currentTooltipObject == null)
            return;

        RectTransform tooltipRect = currentTooltipObject.transform as RectTransform;

        if (tooltipRect == null)
        {
            currentTooltipObject.transform.position = position + tooltipOffset;
            return;
        }

        ResizeSimpleTooltipToContent(tooltipRect);

        Canvas rootCanvas = currentTooltipObject.GetComponentInParent<Canvas>()?.rootCanvas;
        Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        Vector3[] corners = new Vector3[4];
        tooltipRect.GetWorldCorners(corners);

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
        float tooltipWidth = Mathf.Abs(topRight.x - bottomLeft.x);
        float tooltipHeight = Mathf.Abs(topRight.y - bottomLeft.y);

        float horizontalGap = Mathf.Abs(tooltipOffset.x);
        float verticalGap = Mathf.Abs(tooltipOffset.y);
        float rightSpace = Screen.width - position.x;
        float leftSpace = position.x;
        float upperSpace = Screen.height - position.y;
        float lowerSpace = position.y;

        bool placeRight = rightSpace >= tooltipWidth + horizontalGap || rightSpace >= leftSpace;
        bool placeAbove = upperSpace >= tooltipHeight + verticalGap || upperSpace >= lowerSpace;

        tooltipRect.pivot = new Vector2(placeRight ? 0f : 1f, placeAbove ? 0f : 1f);

        Vector2 targetScreenPosition = new Vector2(
            position.x + (placeRight ? horizontalGap : -horizontalGap),
            position.y + (placeAbove ? verticalGap : -verticalGap));

        targetScreenPosition.x = placeRight
            ? Mathf.Clamp(targetScreenPosition.x, 0f, Mathf.Max(0f, Screen.width - tooltipWidth))
            : Mathf.Clamp(targetScreenPosition.x, tooltipWidth, Screen.width);

        targetScreenPosition.y = placeAbove
            ? Mathf.Clamp(targetScreenPosition.y, 0f, Mathf.Max(0f, Screen.height - tooltipHeight))
            : Mathf.Clamp(targetScreenPosition.y, tooltipHeight, Screen.height);

        if (tooltipRect.parent is RectTransform parentRect &&
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parentRect,
                targetScreenPosition,
                uiCamera,
                out Vector3 worldPosition))
        {
            tooltipRect.position = worldPosition;
        }
        else
        {
            tooltipRect.position = targetScreenPosition;
        }
    }

    private string BuildSimpleSkillTooltip(SkillDefinitionSO skill)
    {
        string text = $"<b>{skill.skillName}</b>\n{GetSkillTypeText(skill)}";

        if (skill.staminaCost > 0)
            text += $"\n지구력: {skill.staminaCost}";

        if (skill.mentalCost > 0)
            text += $"\n정신력: {skill.mentalCost}";

        text += $"\n발동 속도: {skill.activationSpeed:0.##}\n\n{BuildSkillDescription(skill)}";
        return text;
    }

    private string GetItemTypeText(
        ItemDefinitionSO item,
        EquipmentRuntimeData equipment)
    {
        if (item is WeaponDefinitionSO weapon)
            return $"장비 - {GetEquipmentTypeText(weapon.equipType)} / {weapon.weaponType}";

        if (item is EquipmentDefinitionSO equipmentDefinition)
            return $"장비 - {GetEquipmentTypeText(equipmentDefinition.equipType)}";

        if (item is ConsumableDefinitionSO)
            return "소모품";

        return "기타 아이템";
    }

    private string BuildItemDescription(
        ItemDefinitionSO item,
        EquipmentRuntimeData equipment)
    {
        StringBuilder builder = new StringBuilder();

        if (!string.IsNullOrEmpty(item.description))
            builder.Append(item.description);

        if (equipment != null)
        {
            AppendSection(builder, "능력치");
            AppendCharacterStats(builder, equipment.statModifiers);
            AppendSpecialStats(builder, equipment.specialStatModifiers);

            if (equipment.generated != null && equipment.generated.appliedAffixes != null)
            {
                foreach (EquipmentAffixRollData affix in equipment.generated.appliedAffixes)
                {
                    if (affix == null || string.IsNullOrEmpty(affix.affixName))
                        continue;

                    AppendLine(builder, $"옵션: {affix.affixName}");
                }
            }

            if (equipment.grantedTraitIds != null)
            {
                foreach (int traitId in equipment.grantedTraitIds)
                {
                    TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(traitId);

                    if (trait != null)
                        AppendLine(builder, $"특성: {trait.traitName} ({trait.defaultAcquireGrade})");
                }
            }
        }
        else if (item is ConsumableDefinitionSO consumable)
        {
            AppendSection(builder, "사용 효과");
            AppendLine(builder, GetConsumableEffectText(consumable));
        }

        return builder.ToString();
    }

    private void AppendCharacterStats(StringBuilder builder, CharacterStats stats)
    {
        if (stats == null)
            return;

        AppendStat(builder, "근력", stats.Strength);
        AppendStat(builder, "기교", stats.Dexterity);
        AppendStat(builder, "속도", stats.Speed);
        AppendStat(builder, "지능", stats.Intelligence);
        AppendStat(builder, "지혜", stats.Wisdom);
        AppendStat(builder, "건강", stats.Health);
        AppendStat(builder, "활력", stats.Vitality);
        AppendStat(builder, "인내", stats.Endurance);
        AppendStat(builder, "HP", stats.MaxHp);
        AppendStat(builder, "지구력", stats.MaxStamina);
        AppendStat(builder, "정신력", stats.MaxMentality);
        AppendStat(builder, "물리 공격력", stats.PhysicalAttack);
        AppendStat(builder, "마법 공격력", stats.MagicalAttack);
        AppendStat(builder, "물리 방어력", stats.PhysicalDefense);
        AppendStat(builder, "마법 방어력", stats.MagicalDefense);
        AppendStat(builder, "화염 저항", stats.FireResistance);
        AppendStat(builder, "얼음 저항", stats.IceResistance);
        AppendStat(builder, "번개 저항", stats.LightningResistance);
        AppendStat(builder, "관통 저항", stats.PierceResistance);
        AppendStat(builder, "참격 저항", stats.SlashResistance);
        AppendStat(builder, "타격 저항", stats.SmashResistance);
        AppendStat(builder, "화염 특화", stats.FireAffinity);
        AppendStat(builder, "얼음 특화", stats.IceAffinity);
        AppendStat(builder, "번개 특화", stats.LightningAffinity);
        AppendStat(builder, "관통 특화", stats.PierceAffinity);
        AppendStat(builder, "참격 특화", stats.SlashAffinity);
        AppendStat(builder, "타격 특화", stats.SmashAffinity);
        AppendFloatStat(builder, "공격 속도", stats.AttackSpeed);
        AppendFloatStat(builder, "시전 속도", stats.CastSpeed);
    }

    private void AppendSpecialStats(StringBuilder builder, CharacterSpecialStats stats)
    {
        if (stats == null)
            return;

        AppendStat(builder, "치명타 확률", stats.CriticalChance);
        AppendStat(builder, "치명타 피해", stats.CriticalDamageBonus);
        AppendStat(builder, "상태이상 저항", stats.StatusResistance);
        AppendStat(builder, "지도 탐지 범위", stats.MapDetectionRange);
        AppendStat(builder, "처치 시 HP 회복", stats.KillHpRecovery);
        AppendStat(builder, "처치 시 지구력 회복", stats.KillStaminaRecovery);
        AppendStat(builder, "처치 시 정신력 회복", stats.KillMentalityRecovery);
        AppendStat(builder, "최소 레벨업 상승치", stats.MinLevelUpStatGainBonus);
        AppendStat(builder, "최대 레벨업 상승치", stats.MaxLevelUpStatGainBonus);
    }

    private string GetConsumableEffectText(ConsumableDefinitionSO consumable)
    {
        switch (consumable.consumableType)
        {
            case ConsumableType.HealHp:
                return $"HP {consumable.hpAmount} 회복";

            case ConsumableType.RecoverStamina:
                return $"지구력 {consumable.staminaAmount} 회복";

            case ConsumableType.RecoverMentality:
                return $"정신력 {consumable.mentalityAmount} 회복";

            case ConsumableType.LearnSkill:
            {
                SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(consumable.skillUid);
                return skill != null ? $"스킬 습득: {skill.skillName}" : "스킬 습득";
            }

            case ConsumableType.GainTrait:
            {
                TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(consumable.traitId);
                return trait != null ? $"특성 획득: {trait.traitName}" : "특성 획득";
            }

            default:
                return "사용 효과 없음";
        }
    }

    private string GetEquipmentTypeText(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.Helmet: return "투구";
            case EquipmentType.Armor: return "갑옷";
            case EquipmentType.Gloves: return "장갑";
            case EquipmentType.Shoes: return "신발";
            case EquipmentType.Ring: return "반지";
            case EquipmentType.Necklace: return "목걸이";
            case EquipmentType.Weapon: return "주무기";
            case EquipmentType.SubWeapon: return "보조무기";
            default: return "장비";
        }
    }

    private void AppendSection(StringBuilder builder, string title)
    {
        if (builder.Length > 0)
            builder.Append("\n\n");

        builder.Append(title);
    }

    private void AppendLine(StringBuilder builder, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        builder.Append('\n');
        builder.Append(text);
    }

    private void AppendStat(StringBuilder builder, string name, int value)
    {
        if (value == 0)
            return;

        AppendLine(builder, $"{name}: {(value > 0 ? "+" : "")}{value}");
    }

    private void AppendFloatStat(StringBuilder builder, string name, float value)
    {
        if (Mathf.Approximately(value, 0f))
            return;

        AppendLine(builder, $"{name}: {(value > 0f ? "+" : "")}{value:0.##}");
    }

    private void ResizeSimpleTooltipToContent(RectTransform tooltipRect)
    {
        if (tooltipText == null || tooltipText.rectTransform != tooltipRect)
            return;

        string currentText = tooltipText.text ?? "";

        if (lastSizedTooltipText == currentText)
            return;

        lastSizedTooltipText = currentText;

        float maximumWidth = Mathf.Max(minimumSimpleTooltipSize.x, maximumSimpleTooltipWidth);
        Vector2 unconstrainedSize = tooltipText.GetPreferredValues(currentText);
        float tooltipWidth = Mathf.Clamp(
            unconstrainedSize.x + simpleTooltipPadding.x,
            minimumSimpleTooltipSize.x,
            maximumWidth);

        float textWidth = Mathf.Max(1f, tooltipWidth - simpleTooltipPadding.x);
        Vector2 constrainedSize = tooltipText.GetPreferredValues(currentText, textWidth, 0f);
        float tooltipHeight = Mathf.Max(
            minimumSimpleTooltipSize.y,
            constrainedSize.y + simpleTooltipPadding.y);

        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tooltipWidth);
        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipHeight);
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

        if (activeTooltipObject != null)
            activeTooltipObject.SetActive(true);

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

        if (activeTooltipObject != null)
            activeTooltipObject.SetActive(true);

        isTooltipActive = true;
    }

    private void SetSkillTooltipMode()
    {
        activeTooltipObject = tooltipObject;

        if (simpleTooltipObject != null && simpleTooltipObject != tooltipObject)
            simpleTooltipObject.SetActive(false);

        if (tooltipText != null)
            tooltipText.text = "";
    }

    private void SetTextTooltipMode()
    {
        activeTooltipObject = simpleTooltipObject != null
            ? simpleTooltipObject
            : tooltipObject;

        if (tooltipObject != null && tooltipObject != activeTooltipObject)
            tooltipObject.SetActive(false);

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
        StringBuilder text = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(skill.description))
            text.Append(skill.description.Trim());

        if (!skill.isCounterSkill)
        {
            AppendSectionSeparator(text);
            text.Append("공격 성능:\n");
            text.Append(skill.GetDamageSummaryText());

            float total = skill.GetTotalDamageMultiplier();

            if (total > 0f)
                text.Append($"\n총 위력: {Mathf.RoundToInt(total * 100f)}%");
        }

        if (skill.isCounterSkill)
        {
            AppendSectionSeparator(text);

            if (skill.counterActionType == CounterActionType.Evade)
            {
                int reductionPercent = Mathf.RoundToInt(skill.minEvadeReductionRate * 100f);
                text.Append($"성공 시 최소 {reductionPercent}% 피해 감소");
            }
            else
            {
                int armorPercent = Mathf.RoundToInt(skill.successArmorAttackMultiplier * 100f);
                string armorType = skill.type == SkillType.Magical ? "마법 방어도" : "물리 방어도";
                text.Append($"성공 시 공격력의 {armorPercent}%만큼 {armorType} 획득");
            }
        }

        if (skill.attackEffects != null && skill.attackEffects.Count > 0)
        {
            text.Append("\n공격 효과: ");

            for (int i = 0; i < skill.attackEffects.Count; i++)
            {
                text.Append(GetAttackEffectText(skill.attackEffects[i]));

                if (i < skill.attackEffects.Count - 1)
                    text.Append(", ");
            }
        }

        if (skill.statusEffects != null && skill.statusEffects.Count > 0)
        {
            AppendSectionSeparator(text);
            text.Append("상태이상 적용:");

            foreach (StatusEffectApplyData applyData in skill.statusEffects)
            {
                if (applyData == null)
                    continue;

                StatusEffectDefinitionSO status = GameDataRegistry.Instance != null
                    ? GameDataRegistry.Instance.GetStatusEffect(applyData.statusEffectId)
                    : null;
                string statusName = status != null && !string.IsNullOrWhiteSpace(status.statusName)
                    ? status.statusName
                    : $"상태이상 {applyData.statusEffectId}";

                text.Append($"\n{statusName} {applyData.stackAmount}스택을 {applyData.baseChance}% 확률로 적용");
            }
        }

        return text.ToString();
    }

    private static void AppendSectionSeparator(StringBuilder text)
    {
        if (text.Length > 0)
            text.Append("\n\n");
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
