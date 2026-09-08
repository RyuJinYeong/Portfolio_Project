using System.Collections.Generic;
using Jorjouto.AnimComposerSystem;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Skill")]
public class SkillDefinitionSO : ScriptableObject
{
    public int uid;
    public string skillName;

    [TextArea]
    public string description;

    public Texture2D icon;

    [Header("Presentation")]
    public ScriptableObject_AnimComposer composer;
    public List<ScriptableObject_AnimComposer> followUpComposers = new();
    [Tooltip("Auto는 스킬 계열과 공격 속성으로 충돌음을 정합니다. 몬스터나 예외 스킬만 직접 지정합니다.")]
    public SkillImpactSoundCategory impactSoundCategory = SkillImpactSoundCategory.Auto;

    [Header("Skill Value")]
    public float activationSpeed = 1f;

    public int staminaCost;
    public int mentalCost;

    public SkillType type;

    [Header("Skill Style")]
    public SkillStyle style;

    [Header("UI")]
    public SkillDiscipline discipline = SkillDiscipline.Basic;

    [Header("Reward")]
    [Min(1)]
    public int firstRewardTier = 1;

    [Header("Damage")]
    public List<SkillDamageComponentData> damageComponents = new();

    [Header("Equipment Requirement")]
    public SkillEquipmentRequirementData equipmentRequirement = new SkillEquipmentRequirementData();

    [Header("Skill Flags")]
    public bool isCounterSkill;
    public bool isStatusEffectSkill;
    public bool isRangedSkill;

    [Tooltip("몬스터 전용 스킬이면 일반 보상/상점/스킬북 풀에서 제외")]
    public bool monsterOnly;

    [Header("Counter")]
    public CounterActionType counterActionType = CounterActionType.None;

    [Tooltip("가드/패링은 성공 시 방어도 계수와 대응 위력으로, 파훼는 일반 성공 피해 감소율로 사용. 회피는 사용하지 않음")]
    public float successArmorAttackMultiplier = 1f;

    [Tooltip("회피 일반 성공 시 최소 피해 감소율")]
    public float minEvadeReductionRate = 0.1f;

    [Header("Attack Effects")]
    public List<AttackEffectType> attackEffects = new();

    [Header("Status Effect")]
    public int statusAccuracyBonus;
    public List<StatusEffectApplyData> statusEffects = new();

    [Header("Status Consume Effect")]
    public List<StatusConsumeEffectData> statusConsumeEffects = new();

    [Header("Acquisition Condition")]
    public bool isConditionalSkill;
    public List<StatRequirementData> requiredStats = new();
    public List<int> requiredTraitIds = new();

    [Header("Evolution")]
    public bool isEvolvableSkill;
    public int evolvedSkillUid;

    public int evolRequiredUseCount;
    public int evolRequiredKillCount;
    public int evolRequiredDamageCount;
    public List<int> evolRequiredTraitIds = new();

    private void OnValidate()
    {
        isStatusEffectSkill =
            statusEffects != null && statusEffects.Count > 0;

        if (damageComponents != null)
        {
            foreach (SkillDamageComponentData component in damageComponents)
            {
                if (component == null)
                    continue;

                if (component.hitCount < 1)
                    component.hitCount = 1;

                if (component.damageMultiplier < 0f)
                    component.damageMultiplier = 0f;

                if (component.damageType == SkillType.Mixed)
                    component.damageType = SkillType.Physical;
            }
        }

        if (!isCounterSkill)
            counterActionType = CounterActionType.None;

        if (counterActionType == CounterActionType.Evade)
            successArmorAttackMultiplier = 0f;
    }

    public bool CanAppearInRewardPool()
    {
        return !monsterOnly;
    }

    public bool CanAppearInRewardPool(int questTier)
    {
        return !monsterOnly && questTier >= Mathf.Max(1, firstRewardTier);
    }

    public bool HasStatusEffects()
    {
        return statusEffects != null && statusEffects.Count > 0;
    }

    public bool HasStatusConsumeEffects()
    {
        return statusConsumeEffects != null && statusConsumeEffects.Count > 0;
    }

    public bool HasAttackEffect(AttackEffectType effect)
    {
        return attackEffects != null && attackEffects.Contains(effect);
    }

    public int GetTotalResourceCost()
    {
        return Mathf.Max(0, staminaCost) + Mathf.Max(0, mentalCost);
    }

    public float GetTotalDamageMultiplier()
    {
        if (damageComponents == null || damageComponents.Count == 0)
            return 0f;

        float total = 0f;

        foreach (SkillDamageComponentData component in damageComponents)
        {
            if (component == null)
                continue;

            int hitCount = component.hitCount < 1 ? 1 : component.hitCount;
            total += component.damageMultiplier * hitCount;
        }

        return total;
    }

    public float GetHighestSingleHitMultiplier()
    {
        if (damageComponents == null || damageComponents.Count == 0)
            return 0f;

        float highest = 0f;

        foreach (SkillDamageComponentData component in damageComponents)
        {
            if (component == null)
                continue;

            if (component.damageMultiplier > highest)
                highest = component.damageMultiplier;
        }

        return highest;
    }

    public string GetDamageSummaryText()
    {
        if (damageComponents == null || damageComponents.Count == 0)
            return "피해 없음";

        List<string> lines = new List<string>();

        foreach (SkillDamageComponentData component in damageComponents)
        {
            if (component == null)
                continue;

            int hitCount = component.hitCount < 1 ? 1 : component.hitCount;
            int percent = Mathf.RoundToInt(component.damageMultiplier * 100f);

            string damageTypeText = GetSkillTypeText(component.damageType);
            string attributeText = GetAttributeText(component.attribute);

            if (hitCount <= 1)
                lines.Add($"{attributeText} {damageTypeText} 피해 {percent}%");
            else
                lines.Add($"{attributeText} {damageTypeText} 피해 {percent}% × {hitCount}회");
        }

        return string.Join("\n", lines);
    }

    public bool CanBeAcquiredBy(CharacterData character)
    {
        if (monsterOnly)
            return false;

        if (character == null || character.FinalStats == null)
            return false;

        foreach (StatRequirementData req in requiredStats)
        {
            if (GetStatValue(character.FinalStats, req.stat) < req.requiredValue)
                return false;
        }

        foreach (int traitId in requiredTraitIds)
        {
            if (!character.HasTrait(traitId))
                return false;
        }

        return true;
    }

    public string GetAcquisitionRequirementText()
    {
        List<string> requirements = new List<string>();

        foreach (StatRequirementData requirement in requiredStats ?? new List<StatRequirementData>())
        {
            if (requirement == null)
                continue;

            requirements.Add(
                $"{GetStatRequirementName(requirement.stat)} {requirement.requiredValue} 이상");
        }

        foreach (int traitId in requiredTraitIds ?? new List<int>())
        {
            TraitDefinitionSO trait = GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetTrait(traitId)
                : null;
            requirements.Add(trait != null ? $"특성: {trait.traitName}" : $"특성 UID: {traitId}");
        }

        return requirements.Count > 0 ? string.Join(" / ", requirements) : "없음";
    }

    private static string GetStatRequirementName(StatRequirementType type)
    {
        return type switch
        {
            StatRequirementType.Strength => "근력",
            StatRequirementType.Dexterity => "기교",
            StatRequirementType.Speed => "속도",
            StatRequirementType.Intelligence => "지능",
            StatRequirementType.Wisdom => "지혜",
            StatRequirementType.Health => "건강",
            StatRequirementType.Vitality => "활력",
            StatRequirementType.Endurance => "인내",
            StatRequirementType.Detection => "눈썰미",
            StatRequirementType.Insight => "통찰력",
            _ => type.ToString()
        };
    }

    public bool CanBeUsedBy(CharacterData character)
    {
        if (character == null)
            return false;

        bool hasExplicitEquipmentRequirement =
            equipmentRequirement != null &&
            equipmentRequirement.HasAnyRequirement();

        if (hasExplicitEquipmentRequirement)
            return MeetsEquipmentRequirement(character);

        return MeetsPhysicalAttributeRequirement(character);
    }

    public bool ShouldUseOffHand(CharacterData character)
    {
        if (character == null)
            return false;

        WeaponDefinitionSO mainWeapon = character.GetMainWeapon();
        WeaponDefinitionSO subWeapon = character.GetSubWeapon();

        if (discipline == SkillDiscipline.DaggerArt)
            return subWeapon != null && subWeapon.weaponType == WeaponType.Dagger;

        if (discipline == SkillDiscipline.ShieldArt)
            return subWeapon != null && subWeapon.weaponType == WeaponType.Shield;

        bool hasExplicitEquipmentRequirement =
            equipmentRequirement != null &&
            equipmentRequirement.HasAnyRequirement();

        if (hasExplicitEquipmentRequirement)
        {
            if (subWeapon == null)
                return false;

            if (equipmentRequirement.requireShield)
                return subWeapon.weaponType == WeaponType.Shield;

            bool mainAllowed =
                mainWeapon != null &&
                equipmentRequirement.allowedAnyWeaponTypes != null &&
                equipmentRequirement.allowedAnyWeaponTypes.Contains(mainWeapon.weaponType);
            bool subAllowed =
                (equipmentRequirement.allowedSubWeaponTypes != null &&
                 equipmentRequirement.allowedSubWeaponTypes.Contains(subWeapon.weaponType)) ||
                (equipmentRequirement.allowedAnyWeaponTypes != null &&
                 equipmentRequirement.allowedAnyWeaponTypes.Contains(subWeapon.weaponType));

            return !mainAllowed && subAllowed;
        }

        if (damageComponents == null)
            return false;

        if (subWeapon == null || subWeapon.attributes == null)
            return false;

        bool hasPhysicalAttribute = false;
        bool mainSupportsAll = true;
        bool subSupportsAll = true;

        foreach (SkillDamageComponentData component in damageComponents)
        {
            if (component == null)
                continue;

            if (component.damageType != SkillType.Physical)
                continue;

            if (!IsWeaponAttribute(component.attribute))
                continue;

            hasPhysicalAttribute = true;

            bool mainSupports =
                mainWeapon != null &&
                mainWeapon.attributes != null &&
                mainWeapon.attributes.Contains(component.attribute);

            bool subSupports =
                subWeapon.attributes.Contains(component.attribute);

            if (!mainSupports)
                mainSupportsAll = false;

            if (!subSupports)
                subSupportsAll = false;
        }

        if (!hasPhysicalAttribute)
            return false;

        return !mainSupportsAll && subSupportsAll;
    }

    private bool MeetsEquipmentRequirement(CharacterData character)
    {
        if (equipmentRequirement == null)
            return true;

        if (!equipmentRequirement.HasAnyRequirement())
            return true;

        WeaponDefinitionSO mainWeapon = character.GetMainWeapon();
        WeaponDefinitionSO subWeapon = character.GetSubWeapon();

        if (equipmentRequirement.requireMainHandEmpty && mainWeapon != null)
            return false;

        if (equipmentRequirement.requireSubHandEmpty && subWeapon != null)
            return false;

        if (equipmentRequirement.requireAnyHandEmpty && mainWeapon != null && subWeapon != null)
            return false;

        if (equipmentRequirement.requireShield)
        {
            bool hasShield =
                mainWeapon != null && mainWeapon.weaponType == WeaponType.Shield ||
                subWeapon != null && subWeapon.weaponType == WeaponType.Shield;

            if (!hasShield)
                return false;
        }

        if (equipmentRequirement.allowedMainWeaponTypes != null &&
            equipmentRequirement.allowedMainWeaponTypes.Count > 0)
        {
            if (mainWeapon == null)
                return false;

            if (!equipmentRequirement.allowedMainWeaponTypes.Contains(mainWeapon.weaponType))
                return false;
        }

        if (equipmentRequirement.allowedSubWeaponTypes != null &&
            equipmentRequirement.allowedSubWeaponTypes.Count > 0)
        {
            if (subWeapon == null)
                return false;

            if (!equipmentRequirement.allowedSubWeaponTypes.Contains(subWeapon.weaponType))
                return false;
        }

        if (equipmentRequirement.allowedAnyWeaponTypes != null &&
            equipmentRequirement.allowedAnyWeaponTypes.Count > 0)
        {
            bool mainMatch =
                mainWeapon != null &&
                equipmentRequirement.allowedAnyWeaponTypes.Contains(mainWeapon.weaponType);

            bool subMatch =
                subWeapon != null &&
                equipmentRequirement.allowedAnyWeaponTypes.Contains(subWeapon.weaponType);

            if (!mainMatch && !subMatch)
                return false;
        }

        return true;
    }

    private bool MeetsPhysicalAttributeRequirement(CharacterData character)
    {
        if (damageComponents == null || damageComponents.Count == 0)
            return true;

        WeaponDefinitionSO mainWeapon = character.GetMainWeapon();
        WeaponDefinitionSO subWeapon = character.GetSubWeapon();

        foreach (SkillDamageComponentData component in damageComponents)
        {
            if (component == null)
                continue;

            if (component.damageType != SkillType.Physical)
                continue;

            if (!IsWeaponAttribute(component.attribute))
                continue;

            bool mainSupports =
                mainWeapon != null &&
                mainWeapon.attributes != null &&
                mainWeapon.attributes.Contains(component.attribute);

            bool subSupports =
                subWeapon != null &&
                subWeapon.attributes != null &&
                subWeapon.attributes.Contains(component.attribute);

            bool innateSupports =
                character.AvailableAttributes != null &&
                character.AvailableAttributes.Contains(component.attribute);

            if (!mainSupports && !subSupports && !innateSupports)
                return false;
        }

        return true;
    }

    private bool IsWeaponAttribute(SkillAttribute attribute)
    {
        return attribute == SkillAttribute.Pierce ||
               attribute == SkillAttribute.Slash ||
               attribute == SkillAttribute.Smash;
    }

    private int GetStatValue(CharacterStats stats, StatRequirementType type)
    {
        return type switch
        {
            StatRequirementType.Strength => stats.Strength,
            StatRequirementType.Dexterity => stats.Dexterity,
            StatRequirementType.Speed => stats.Speed,
            StatRequirementType.Intelligence => stats.Intelligence,
            StatRequirementType.Wisdom => stats.Wisdom,
            StatRequirementType.Health => stats.Health,
            StatRequirementType.Vitality => stats.Vitality,
            StatRequirementType.Endurance => stats.Endurance,
            StatRequirementType.Detection => stats.Detection,
            StatRequirementType.Insight => stats.Insight,
            _ => 0
        };
    }

    private string GetSkillTypeText(SkillType type)
    {
        return type switch
        {
            SkillType.Physical => "물리",
            SkillType.Magical => "마법",
            SkillType.Mixed => "혼합",
            _ => "-"
        };
    }

    private string GetAttributeText(SkillAttribute attribute)
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
            SkillAttribute.None => "무속성",
            _ => attribute.ToString()
        };
    }
}
