using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemTooltipUI : MonoBehaviour
{
    private const string PositiveColor = "#469824";
    private const string NegativeColor = "#BF3126";

    public static InventoryItemTooltipUI Instance { get; private set; }

    private RectTransform rootRect;
    private RectTransform panelRect;
    private Text nameText;
    private Text typeText;
    private Text qualityText;
    private Text tagsText;
    private Text priceText;
    private Text countText;
    private Text descriptionText;
    private RawImage iconImage;
    private Image iconFrame;
    private Transform statsContainer;
    private Text statsTemplate;
    private Text essenceDetailsText;
    private Canvas tooltipCanvas;
    private readonly List<Text> statTexts = new List<Text>();

    public void Initialize()
    {
        Instance = this;
        rootRect = transform as RectTransform;
        tooltipCanvas = GetComponent<Canvas>();

        if (tooltipCanvas != null)
        {
            tooltipCanvas.overrideSorting = true;
            tooltipCanvas.sortingOrder = short.MaxValue;
        }

        Transform panel = FindDeepChild(transform, "Panel");
        Transform title = panel != null ? FindDirectChild(panel, "Title") : null;
        Transform iconBack = title != null ? FindDirectChild(title, "IconBack") : null;
        Transform currency = panel != null ? FindDirectChild(panel, "CurrencyItem") : null;

        panelRect = panel as RectTransform;
        nameText = GetText(title, "Name_text");
        typeText = GetText(title, "Type_text");
        qualityText = GetText(title, "Quality_text");
        tagsText = GetText(title, "Tags_text");
        priceText = GetText(currency, "Number");
        countText = GetText(iconBack, "Number");
        descriptionText = GetText(title, "Description_text");
        statsContainer = panel != null ? FindDirectChild(panel, "Stats") : null;
        Transform essenceDetails = statsContainer != null
            ? FindDirectChild(statsContainer, "EssenceDetails")
            : null;
        essenceDetailsText = essenceDetails != null ? essenceDetails.GetComponent<Text>() : null;

        Transform icon = iconBack != null ? FindDirectChild(iconBack, "Icon") : null;
        Transform frame = iconBack != null ? FindDirectChild(iconBack, "frame") : null;
        iconImage = icon != null ? icon.GetComponent<RawImage>() : null;
        iconFrame = frame != null ? frame.GetComponent<Image>() : null;

        SetChildActive(iconBack, "Upgrade", false);
        SetChildActive(iconBack, "Fav", false);
        SetChildActive(panel, "Hints", false);

        if (statsContainer != null)
        {
            for (int i = 0; i < statsContainer.childCount; i++)
            {
                Text statText = statsContainer.GetChild(i).GetComponent<Text>();

                if (statText == null || statText == essenceDetailsText)
                    continue;

                statsTemplate ??= statText;
                statText.supportRichText = true;
                statText.gameObject.SetActive(false);
                statTexts.Add(statText);
            }
        }

        if (descriptionText != null)
        {
            descriptionText.supportRichText = true;
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            descriptionText.resizeTextForBestFit = false;
            descriptionText.fontSize = 14;
        }

        CanvasGroup group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        Hide();
    }

    public void Show(
        InventorySlotData slot,
        CharacterManager comparisonCharacter,
        Vector2 screenPosition)
    {
        if (slot == null || GameDataRegistry.Instance == null)
            return;

        ItemDefinitionSO item = GameDataRegistry.Instance.GetItem(slot.itemUid);

        if (item == null)
            return;

        EquipmentRuntimeData equipment = item is EquipmentDefinitionSO
            ? EquipmentRuntimeResolver.Resolve(slot.itemUid, slot.equipmentInstanceId)
            : null;

        if (nameText != null)
        {
            nameText.text = GetDisplayName(slot, item, equipment);
            nameText.color = equipment != null
                ? EquipmentRarityUtility.GetColor(equipment.rarity)
                : Color.white;
        }

        if (typeText != null)
            typeText.text = GetItemTypeText(slot, item);

        if (tagsText != null)
        {
            tagsText.text = item is WeaponDefinitionSO ? BuildEquipmentDetails(item) : "";
            tagsText.gameObject.SetActive(!string.IsNullOrWhiteSpace(tagsText.text));
        }

        if (qualityText != null)
        {
            qualityText.text = equipment != null
                ? $"티어 {equipment.tier} · {GetRarityText(equipment.rarity)}"
                : slot.count > 1
                    ? $"보유 수량 {slot.count}"
                    : "";

            RectTransform precedingRow = tagsText != null && tagsText.gameObject.activeSelf
                ? tagsText.rectTransform
                : typeText != null ? typeText.rectTransform : null;
            if (precedingRow != null)
                qualityText.rectTransform.anchoredPosition = new Vector2(
                    qualityText.rectTransform.anchoredPosition.x,
                    precedingRow.anchoredPosition.y - precedingRow.rect.height - 2f);
        }

        if (priceText != null)
        {
            priceText.text = SharedInventoryUtility.GetSalePrice(slot).ToString("N0");
            float priceWidth = Mathf.Ceil(priceText.preferredWidth);
            priceText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, priceWidth);
            if (priceText.transform.parent is RectTransform currencyRect)
                currencyRect.anchoredPosition = new Vector2(
                    -16f - priceWidth - priceText.rectTransform.anchoredPosition.x,
                    currencyRect.anchoredPosition.y);
        }

        if (countText != null)
        {
            countText.gameObject.SetActive(slot.count > 1);
            countText.text = slot.count > 1 ? slot.count.ToString() : "";
        }

        if (iconImage != null)
        {
            iconImage.texture = item.icon;
            iconImage.enabled = item.icon != null;
        }

        if (iconFrame != null)
        {
            iconFrame.color = equipment != null
                ? EquipmentRarityUtility.GetColor(equipment.rarity)
                : Color.white;
        }

        if (descriptionText != null)
        {
            bool showEssenceDetailsBelow = slot.IsMonsterEssence() && essenceDetailsText != null;
            descriptionText.text = showEssenceDetailsBelow
                ? ""
                : equipment != null
                    ? item.description?.Trim()
                    : GetItemDescription(slot, item);
        }

        PopulateStats(
            slot,
            item,
            equipment,
            comparisonCharacter != null ? comparisonCharacter.character : null);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (tooltipCanvas != null)
        {
            tooltipCanvas.overrideSorting = true;
            tooltipCanvas.sortingOrder = short.MaxValue;
        }

        if (panelRect != null && descriptionText != null && statsContainer is RectTransform statsRect)
        {
            RectTransform descriptionRect = descriptionText.rectTransform;
            float descriptionHeight = Mathf.Max(24f, descriptionText.preferredHeight);
            descriptionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, descriptionHeight);
            float dividerY = descriptionRect.anchoredPosition.y - descriptionHeight - 12f;
            RectTransform divider = FindDirectChild(descriptionRect.parent, "line3") as RectTransform;
            if (divider != null)
                divider.anchoredPosition = new Vector2(divider.anchoredPosition.x, dividerY);
            statsRect.anchoredPosition = new Vector2(statsRect.anchoredPosition.x, dividerY - 14f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(statsRect);
            float statsHeight = statsContainer.gameObject.activeSelf ? LayoutUtility.GetPreferredHeight(statsRect) : 0f;
            statsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, statsHeight);
        }

        InventoryUIController controller = InventoryUIController.Instance;
        bool fixedToEquipment = equipment != null &&
                                controller != null &&
                                controller.equipmentWindow != null &&
                                controller.equipmentWindow.gameObject.activeInHierarchy;

        if (fixedToEquipment)
            UpdateEquipmentPosition(controller);
        else
            UpdatePosition(screenPosition);
    }

    private static string GetDisplayName(
        InventorySlotData slot,
        ItemDefinitionSO item,
        EquipmentRuntimeData equipment)
    {
        if (slot != null && slot.IsMonsterEssence() &&
            !string.IsNullOrEmpty(slot.essenceMonsterName))
        {
            return $"{slot.essenceMonsterName}의 정수";
        }

        if (slot != null && slot.IsSkillBook() && GameDataRegistry.Instance != null)
        {
            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(
                slot.skillBookSkillUid);

            if (skill != null)
                return $"{skill.skillName} 스킬북";
        }

        return equipment != null && !string.IsNullOrEmpty(equipment.displayName)
            ? equipment.displayName
            : item.itemName;
    }

    private static string GetItemDescription(
        InventorySlotData slot,
        ItemDefinitionSO item)
    {
        if (slot != null && slot.IsMonsterEssence())
        {
            string appraisal = SharedInventoryUtility.GetMonsterEssenceAppraisalText(slot);

            if (!string.IsNullOrEmpty(appraisal))
                return string.IsNullOrEmpty(item.description)
                    ? $"감정 결과\n{appraisal}"
                    : $"{item.description}\n\n감정 결과\n{appraisal}";
        }

        if (slot != null && slot.IsSkillBook() && GameDataRegistry.Instance != null)
        {
            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(
                slot.skillBookSkillUid);

            if (skill != null)
            {
                return $"사용하면 선택한 캐릭터가 {skill.skillName} 스킬을 습득합니다.\n\n" +
                       $"습득 조건: {skill.GetAcquisitionRequirementText()}";
            }
        }

        return item.description;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private string BuildStats(
        EquipmentRuntimeData equipment,
        CharacterData comparisonCharacter)
    {
        StringBuilder builder = new StringBuilder();

        if (equipment == null)
            return builder.ToString();

        EquipmentRuntimeData equipped = ResolveEquippedComparison(equipment, comparisonCharacter);
        CharacterStats currentStats = equipped != null ? equipped.statModifiers : null;
        CharacterSpecialStats currentSpecial = equipped != null ? equipped.specialStatModifiers : null;

        AppendSection(builder, equipped != null ? "장착 장비와 비교" : "능력치");
        AppendStats(builder, equipment.statModifiers, currentStats, equipped != null);
        StringBuilder special = new StringBuilder();
        AppendSpecialStats(special, equipment.specialStatModifiers, currentSpecial, equipped != null);

        if (equipment.grantedTraitIds != null)
        {
            foreach (int traitId in equipment.grantedTraitIds)
            {
                TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(traitId);

                if (trait != null)
                    AppendTextLine(special, $"특성: {trait.traitName}");
            }
        }

        if (special.Length > 0)
        {
            AppendSection(builder, "특수 효과");
            builder.Append(special);
        }

        return builder.ToString();
    }

    private void PopulateStats(
        InventorySlotData slot,
        ItemDefinitionSO item,
        EquipmentRuntimeData equipment,
        CharacterData comparisonCharacter)
    {
        if (statsContainer == null)
            return;

        foreach (Text statText in statTexts)
        {
            if (statText != null)
                statText.gameObject.SetActive(false);
        }

        if (essenceDetailsText != null)
            essenceDetailsText.gameObject.SetActive(false);

        if (equipment == null && slot != null && slot.IsMonsterEssence() && essenceDetailsText != null)
        {
            statsContainer.gameObject.SetActive(true);
            essenceDetailsText.text = GetItemDescription(slot, item);
            essenceDetailsText.gameObject.SetActive(true);
            return;
        }

        statsContainer.gameObject.SetActive(equipment != null);

        if (equipment == null)
            return;

        string[] lines = BuildStats(equipment, comparisonCharacter)
            .Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            Text statText;

            if (i < statTexts.Count)
            {
                statText = statTexts[i];
            }
            else if (statsTemplate != null)
            {
                statText = Instantiate(statsTemplate, statsContainer);
                statTexts.Add(statText);
            }
            else
            {
                break;
            }

            statText.text = lines[i];
            statText.gameObject.SetActive(true);
        }
    }

    private static string BuildEquipmentDetails(ItemDefinitionSO item)
    {
        if (item is WeaponDefinitionSO weapon)
        {
            string tags = JoinWeaponTags(weapon.weaponTags);
            string attributes = JoinWeaponAttributes(weapon.attributes);
            string details = $"{tags}[{GetWeaponCategoryText(weapon.weaponCategory)}]";
            return string.IsNullOrEmpty(attributes) ? details : $"{details} · {attributes}";
        }

        return "";
    }

    private static string JoinWeaponAttributes(List<SkillAttribute> attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return "";

        List<string> values = new List<string>();

        foreach (SkillAttribute attribute in attributes)
        {
            if (attribute != SkillAttribute.None)
                values.Add(GetAttributeText(attribute));
        }

        return string.Join(" / ", values);
    }

    private static string JoinWeaponTags(List<WeaponTag> tags)
    {
        if (tags == null || tags.Count == 0)
            return "";

        List<string> values = new List<string>();

        foreach (WeaponTag tag in tags)
        {
            values.Add(tag switch
            {
                WeaponTag.TwoHanded => "[양손]",
                WeaponTag.MagicWeapon => "[마법]",
                _ => tag.ToString()
            });
        }

        return string.Join("", values);
    }

    private static string GetWeaponCategoryText(WeaponCategory category)
    {
        return category == WeaponCategory.HeavyWeapon ? "중량" : "경량";
    }

    private static string GetArmorCategoryText(ArmorCategory category)
    {
        return category switch
        {
            ArmorCategory.HeavyArmor => "중갑",
            ArmorCategory.LightArmor => "경갑",
            ArmorCategory.ClothArmor => "의복",
            _ => category.ToString()
        };
    }

    private static string GetWeaponTypeText(WeaponType type)
    {
        return type switch
        {
            WeaponType.Two_HandedSword => "양손검",
            WeaponType.Greatsword => "대검",
            WeaponType.LongSword => "장검",
            WeaponType.Dagger => "단검",
            WeaponType.Bow => "활",
            WeaponType.Mace => "철퇴",
            WeaponType.Hammer => "해머",
            WeaponType.Shield => "방패",
            WeaponType.Axe => "도끼",
            WeaponType.Spear => "창",
            WeaponType.Staff => "지팡이",
            WeaponType.Book => "마도서",
            WeaponType.Orb => "보주",
            _ => type.ToString()
        };
    }

    private static string GetAttributeText(SkillAttribute attribute)
    {
        return attribute switch
        {
            SkillAttribute.Pierce => "관통",
            SkillAttribute.Slash => "참격",
            SkillAttribute.Smash => "타격",
            SkillAttribute.Fire => "화염",
            SkillAttribute.Ice => "얼음",
            SkillAttribute.Lightning => "번개",
            SkillAttribute.Magic => "마력",
            _ => attribute.ToString()
        };
    }

    private static EquipmentRuntimeData ResolveEquippedComparison(
        EquipmentRuntimeData candidate,
        CharacterData character)
    {
        if (candidate == null || character == null || character.EquipmentSlots == null)
            return null;

        int slotIndex = 1;

        if (candidate.equipType == EquipmentType.Ring)
        {
            if (character.EquipmentSlots.ring1Uid <= 0)
                slotIndex = 1;
            else if (character.EquipmentSlots.ring2Uid <= 0)
                slotIndex = 2;
        }

        InventorySlotData equippedSlot = SharedInventoryUtility.GetEquippedSlot(
            character.EquipmentSlots,
            candidate.equipType,
            slotIndex);

        return equippedSlot != null
            ? EquipmentRuntimeResolver.Resolve(
                equippedSlot.itemUid,
                equippedSlot.equipmentInstanceId)
            : null;
    }

    private static void AppendStats(
        StringBuilder builder,
        CharacterStats candidate,
        CharacterStats current,
        bool compare)
    {
        candidate ??= new CharacterStats();
        current ??= new CharacterStats();

        AppendValue(builder, "근력", candidate.Strength, current.Strength, compare);
        AppendValue(builder, "기교", candidate.Dexterity, current.Dexterity, compare);
        AppendValue(builder, "속도", candidate.Speed, current.Speed, compare);
        AppendValue(builder, "지능", candidate.Intelligence, current.Intelligence, compare);
        AppendValue(builder, "지혜", candidate.Wisdom, current.Wisdom, compare);
        AppendValue(builder, "건강", candidate.Health, current.Health, compare);
        AppendValue(builder, "활력", candidate.Vitality, current.Vitality, compare);
        AppendValue(builder, "인내", candidate.Endurance, current.Endurance, compare);
        AppendValue(builder, "눈썰미", candidate.Detection, current.Detection, compare);
        AppendValue(builder, "통찰력", candidate.Insight, current.Insight, compare);
        AppendValue(builder, "최대 HP", candidate.MaxHp, current.MaxHp, compare);
        AppendValue(builder, "최대 지구력", candidate.MaxStamina, current.MaxStamina, compare);
        AppendValue(builder, "최대 정신력", candidate.MaxMentality, current.MaxMentality, compare);
        AppendValue(builder, "지구력 회복", candidate.StaminaRecovery, current.StaminaRecovery, compare);
        AppendValue(builder, "정신력 회복", candidate.MentalityRecovery, current.MentalityRecovery, compare);
        AppendValue(builder, "물리 공격력", candidate.PhysicalAttack, current.PhysicalAttack, compare);
        AppendValue(builder, "마법 공격력", candidate.MagicalAttack, current.MagicalAttack, compare);
        AppendValue(builder, "물리 방어력", candidate.PhysicalDefense, current.PhysicalDefense, compare);
        AppendValue(builder, "마법 방어력", candidate.MagicalDefense, current.MagicalDefense, compare);
        AppendValue(builder, "공격 속도", candidate.AttackSpeed, current.AttackSpeed, compare);
        AppendValue(builder, "시전 속도", candidate.CastSpeed, current.CastSpeed, compare);
        AppendValue(builder, "화염 저항", candidate.FireResistance, current.FireResistance, compare, "%");
        AppendValue(builder, "얼음 저항", candidate.IceResistance, current.IceResistance, compare, "%");
        AppendValue(builder, "번개 저항", candidate.LightningResistance, current.LightningResistance, compare, "%");
        AppendValue(builder, "관통 저항", candidate.PierceResistance, current.PierceResistance, compare, "%");
        AppendValue(builder, "참격 저항", candidate.SlashResistance, current.SlashResistance, compare, "%");
        AppendValue(builder, "타격 저항", candidate.SmashResistance, current.SmashResistance, compare, "%");
        AppendValue(builder, "화염 특화", candidate.FireAffinity, current.FireAffinity, compare, "%");
        AppendValue(builder, "얼음 특화", candidate.IceAffinity, current.IceAffinity, compare, "%");
        AppendValue(builder, "번개 특화", candidate.LightningAffinity, current.LightningAffinity, compare, "%");
        AppendValue(builder, "관통 특화", candidate.PierceAffinity, current.PierceAffinity, compare, "%");
        AppendValue(builder, "참격 특화", candidate.SlashAffinity, current.SlashAffinity, compare, "%");
        AppendValue(builder, "타격 특화", candidate.SmashAffinity, current.SmashAffinity, compare, "%");
    }

    private static void AppendSpecialStats(
        StringBuilder builder,
        CharacterSpecialStats candidate,
        CharacterSpecialStats current,
        bool compare)
    {
        candidate ??= new CharacterSpecialStats();
        current ??= new CharacterSpecialStats();

        AppendValue(builder, "대성공 확률", candidate.CriticalChance, current.CriticalChance, compare, "%");
        AppendValue(builder, "대성공 피해", candidate.CriticalDamageBonus, current.CriticalDamageBonus, compare, "%");
        AppendValue(
            builder,
            "대응 성공률",
            candidate.DefenseSkillSuccessRateBonus,
            current.DefenseSkillSuccessRateBonus,
            compare, "%");
        AppendValue(builder, "상태이상 저항", candidate.StatusResistance, current.StatusResistance, compare, "%");
        AppendValue(builder, "기본기 스킬 특화", candidate.BasicSpecialization, current.BasicSpecialization, compare, "%");
        AppendValue(builder, "무기술 스킬 특화", candidate.WeaponArtSpecialization, current.WeaponArtSpecialization, compare, "%");
        AppendValue(builder, "검술 스킬 특화", candidate.SwordsmanshipSpecialization, current.SwordsmanshipSpecialization, compare, "%");
        AppendValue(builder, "궁술 스킬 특화", candidate.ArcherySpecialization, current.ArcherySpecialization, compare, "%");
        AppendValue(builder, "방패술 스킬 특화", candidate.ShieldArtSpecialization, current.ShieldArtSpecialization, compare, "%");
        AppendValue(builder, "체술 스킬 특화", candidate.MartialArtSpecialization, current.MartialArtSpecialization, compare, "%");
        AppendValue(builder, "단검술 스킬 특화", candidate.DaggerArtSpecialization, current.DaggerArtSpecialization, compare, "%");
        AppendValue(builder, "마법 스킬 특화", candidate.MagicSpecialization, current.MagicSpecialization, compare, "%");
        AppendValue(builder, "몬스터 스킬 특화", candidate.MonsterSpecialization, current.MonsterSpecialization, compare, "%");
        AppendValue(builder, "지도 탐지 범위", candidate.MapDetectionRange, current.MapDetectionRange, compare);
        AppendValue(builder, "처치 시 체력 회복", candidate.KillHpRecovery, current.KillHpRecovery, compare);
        AppendValue(builder, "처치 시 지구력 회복", candidate.KillStaminaRecovery, current.KillStaminaRecovery, compare);
        AppendValue(builder, "처치 시 정신력 회복", candidate.KillMentalityRecovery, current.KillMentalityRecovery, compare);
        AppendValue(builder, "스킬 시전 속도", candidate.SkillActivationSpeedBonus, current.SkillActivationSpeedBonus, compare, "%");
        AppendValue(builder, "개인 휴식 HP 회복", candidate.PersonalRestHpRecoveryBonus, current.PersonalRestHpRecoveryBonus, compare, "%p");
        AppendValue(builder, "개인 휴식 사기 회복", candidate.PersonalRestMoraleRecoveryBonus, current.PersonalRestMoraleRecoveryBonus, compare, "%p");
        AppendValue(builder, "원정대 휴식 HP 회복", candidate.PartyRestHpRecoveryBonus, current.PartyRestHpRecoveryBonus, compare, "%p");
        AppendValue(builder, "원정대 휴식 사기 회복", candidate.PartyRestMoraleRecoveryBonus, current.PartyRestMoraleRecoveryBonus, compare, "%p");
        AppendValue(builder, "최소 레벨업 상승치", candidate.MinLevelUpStatGainBonus, current.MinLevelUpStatGainBonus, compare);
        AppendValue(builder, "최대 레벨업 상승치", candidate.MaxLevelUpStatGainBonus, current.MaxLevelUpStatGainBonus, compare);
    }

    private static void AppendValue(
        StringBuilder builder,
        string label,
        float candidate,
        float current,
        bool compare,
        string suffix = "")
    {
        if (Mathf.Approximately(candidate, 0f) &&
            (!compare || Mathf.Approximately(current, 0f)))
        {
            return;
        }

        float difference = candidate - current;
        string value = Mathf.Approximately(candidate, Mathf.Round(candidate))
            ? Mathf.RoundToInt(candidate).ToString("+0;-0;0")
            : candidate.ToString("+0.##;-0.##;0");

        string comparison = "";

        if (compare && !Mathf.Approximately(difference, 0f))
        {
            string color = difference > 0f ? PositiveColor : NegativeColor;
            comparison = $" <color={color}>({difference:+0.##;-0.##;0}{suffix})</color>";
        }

        AppendTextLine(builder, $"{label} {value}{suffix}{comparison}");
    }

    private static void AppendSection(StringBuilder builder, string title)
    {
        if (builder.Length > 0)
            builder.Append("\n\n");

        builder.Append(title);
    }

    private static void AppendTextLine(StringBuilder builder, string text)
    {
        builder.Append('\n');
        builder.Append(text);
    }

    private void UpdatePosition(Vector2 screenPosition)
    {
        if (rootRect == null || panelRect == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootRect,
            screenPosition,
            uiCamera,
            out Vector2 localPoint);

        bool placeRight = screenPosition.x <= Screen.width * 0.65f;
        bool placeAbove = screenPosition.y <= Screen.height * 0.55f;

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(placeRight ? 0f : 1f, placeAbove ? 0f : 1f);
        panelRect.anchoredPosition = localPoint + new Vector2(
            placeRight ? 14f : -14f,
            placeAbove ? 14f : -14f);
    }

    private void UpdateEquipmentPosition(InventoryUIController controller)
    {
        if (rootRect == null || panelRect == null || controller.equipmentWindow == null)
            return;

        RectTransform equipmentRect = controller.equipmentWindow.transform as RectTransform;

        if (equipmentRect == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        Vector3[] corners = new Vector3[4];
        equipmentRect.GetWorldCorners(corners);

        bool storageOpen = (controller.companyStorageWindow != null &&
                            controller.companyStorageWindow.gameObject.activeInHierarchy) ||
                           (controller.expeditionInventoryWindow != null &&
                            controller.expeditionInventoryWindow.gameObject.activeInHierarchy);
        bool placeLeft = storageOpen;
        Vector3 edgeWorld = placeLeft
            ? (corners[0] + corners[1]) * 0.5f
            : (corners[2] + corners[3]) * 0.5f;
        Vector2 edgeScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, edgeWorld);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootRect,
            edgeScreen,
            uiCamera,
            out Vector2 localPoint);

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(placeLeft ? 1f : 0f, 0.5f);
        panelRect.anchoredPosition = localPoint + new Vector2(placeLeft ? -12f : 12f, 0f);
    }

    private static string GetItemTypeText(InventorySlotData slot, ItemDefinitionSO item)
    {
        if (slot != null && slot.IsMonsterEssence())
        {
            string baseType = item is ConsumableDefinitionSO ? "소모품" : "기타 아이템";
            return $"{baseType} - 몬스터의 정수";
        }

        if (item is WeaponDefinitionSO weapon)
            return $"{GetEquipmentTypeText(weapon.equipType)} · {GetWeaponTypeText(weapon.weaponType)}";

        if (item is ArmorDefinitionSO armor)
            return $"{GetEquipmentTypeText(armor.equipType)} · {GetArmorCategoryText(armor.armorCategory)}";

        if (item is EquipmentDefinitionSO equipment)
            return GetEquipmentTypeText(equipment.equipType);

        if (item is ConsumableDefinitionSO)
            return "소모품";

        return "기타 아이템";
    }

    private static string GetEquipmentTypeText(EquipmentType type)
    {
        return type switch
        {
            EquipmentType.Helmet => "투구",
            EquipmentType.Armor => "갑옷",
            EquipmentType.Gloves => "장갑",
            EquipmentType.Shoes => "신발",
            EquipmentType.Ring => "반지",
            EquipmentType.Necklace => "목걸이",
            EquipmentType.Weapon => "주무기",
            EquipmentType.SubWeapon => "보조무기",
            _ => "장비"
        };
    }

    private static string GetRarityText(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => "일반",
            EquipmentRarity.Uncommon => "고급",
            EquipmentRarity.Rare => "희귀",
            EquipmentRarity.Epic => "영웅",
            EquipmentRarity.Legendary => "전설",
            _ => rarity.ToString()
        };
    }

    private static Text GetText(Transform parent, string childName)
    {
        Transform child = parent != null ? FindDirectChild(parent, childName) : null;
        return child != null ? child.GetComponent<Text>() : null;
    }

    private static void SetChildActive(Transform parent, string childName, bool active)
    {
        Transform child = parent != null ? FindDirectChild(parent, childName) : null;

        if (child != null)
            child.gameObject.SetActive(active);
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
                return child;

            Transform nested = FindDeepChild(child, childName);

            if (nested != null)
                return nested;
        }

        return null;
    }
}
