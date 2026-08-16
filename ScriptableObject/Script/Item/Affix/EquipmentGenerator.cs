using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EquipmentGenerator
{
    public static GeneratedEquipmentData Generate(EquipmentDefinitionSO baseEquipment, EquipmentRarity rarity)
    {
        if (baseEquipment == null)
            return null;

        GeneratedEquipmentData generated = CreateBaseGeneratedEquipment(baseEquipment, rarity);

        // Legendary는 수동 설계 장비로 간주하고 랜덤 생성 없이 그대로 사용
        // Affix 랜덤 생성 없이 SO에 입력된 이름/스탯/Trait 그대로 사용한다.
        if (rarity == EquipmentRarity.Legendary)
            return generated;

        ApplyRarityBaseBonus(generated, rarity);

        if (!baseEquipment.canGenerateAffix)
            return generated;

        if (rarity == EquipmentRarity.Common)
            return generated;

        List<string> nameParts = new List<string>();

        if (EquipmentRarityUtility.HasPrefix(rarity))
        {
            EquipmentAffixDefinitionSO prefix = RollAffix(baseEquipment, rarity, EquipmentAffixType.Prefix);
            ApplyAffix(generated, prefix, baseEquipment.tier);
            AddNamePart(nameParts, prefix);
        }

        if (EquipmentRarityUtility.HasSuffix(rarity))
        {
            EquipmentAffixDefinitionSO suffix = RollAffix(baseEquipment, rarity, EquipmentAffixType.Suffix);
            ApplyAffix(generated, suffix, baseEquipment.tier);
            AddNamePart(nameParts, suffix);
        }

        if (ShouldRollSpecial(rarity))
        {
            EquipmentAffixDefinitionSO special = RollAffix(baseEquipment, rarity, EquipmentAffixType.Special);
            ApplyAffix(generated, special, baseEquipment.tier);
            AddNamePart(nameParts, special);
        }

        nameParts.Add(baseEquipment.itemName);
        generated.generatedName = string.Join(" ", nameParts);

        return generated;
    }

    private static GeneratedEquipmentData CreateBaseGeneratedEquipment(
        EquipmentDefinitionSO baseEquipment,
        EquipmentRarity rarity)
    {
        GeneratedEquipmentData generated = new GeneratedEquipmentData
        {
            instanceId = System.Guid.NewGuid().ToString(),
            definitionUid = baseEquipment.uid,
            tier = baseEquipment.tier,
            rarity = rarity,
            generatedName = baseEquipment.itemName,

            statModifiers = baseEquipment.statModifiers != null
                ? baseEquipment.statModifiers.Copy()
                : new CharacterStats(),

            specialStatModifiers = baseEquipment.specialStatModifiers != null
                ? baseEquipment.specialStatModifiers.Copy()
                : new CharacterSpecialStats(),

            grantedTraitIds = baseEquipment.grantedTraitIds != null
                ? new List<int>(baseEquipment.grantedTraitIds)
                : new List<int>(),

            appliedAffixes = new List<EquipmentAffixRollData>(),

            visualKey = baseEquipment.GetVisualKey(rarity)
        };

        return generated;
    }

    private static void ApplyRarityBaseBonus(GeneratedEquipmentData generated, EquipmentRarity rarity)
    {
        if (generated == null)
            return;

        int percent = EquipmentRarityUtility.GetBaseStatBonusPercent(rarity);

        generated.statModifiers = EquipmentStatRoller.ScaleStats(generated.statModifiers, percent);
        generated.specialStatModifiers = EquipmentStatRoller.ScaleSpecialStats(generated.specialStatModifiers, percent);
    }

    private static bool ShouldRollSpecial(EquipmentRarity rarity)
    {
        if (EquipmentRarityUtility.HasGuaranteedSpecial(rarity))
            return true;

        if (rarity == EquipmentRarity.Rare)
            return Random.Range(0, 100) < EquipmentRarityUtility.GetRareSpecialChancePercent();

        return false;
    }

    private static EquipmentAffixDefinitionSO RollAffix(
        EquipmentDefinitionSO equipment,
        EquipmentRarity rarity,
        EquipmentAffixType affixType)
    {
        List<EquipmentAffixDefinitionSO> candidates = GameDataRegistry.Instance
            .GetAllEquipmentAffixes()
            .Where(a =>
                a != null &&
                a.affixType == affixType &&
                a.CanApplyTo(equipment, rarity) &&
                a.weight > 0)
            .ToList();

        if (candidates.Count == 0)
            return null;

        int totalWeight = 0;

        foreach (EquipmentAffixDefinitionSO affix in candidates)
            totalWeight += affix.weight;

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        foreach (EquipmentAffixDefinitionSO affix in candidates)
        {
            current += affix.weight;

            if (roll < current)
                return affix;
        }

        return candidates[0];
    }

    private static void ApplyAffix(
        GeneratedEquipmentData generated,
        EquipmentAffixDefinitionSO affix,
        int equipmentTier)
    {
        if (generated == null || affix == null)
            return;

        EquipmentAffixRollData rollData = new EquipmentAffixRollData
        {
            affixId = affix.id,
            affixType = affix.affixType,
            affixName = affix.affixName,
            rolledStatModifiers = new CharacterStats(),
            rolledSpecialStatModifiers = new CharacterSpecialStats(),
            grantedTraitIds = affix.grantedTraitIds != null
                ? new List<int>(affix.grantedTraitIds)
                : new List<int>()
        };

        if (affix.affixType == EquipmentAffixType.Special)
        {
            ApplySpecialAffix(generated, rollData);
            generated.appliedAffixes.Add(rollData);
            return;
        }

        CharacterStats rolledStats = EquipmentStatRoller.RollStats(
            affix.minStatModifiers,
            affix.maxStatModifiers);

        CharacterSpecialStats rolledSpecialStats = EquipmentStatRoller.RollSpecialStats(
            affix.minSpecialStatModifiers,
            affix.maxSpecialStatModifiers);

        int multiplier = EquipmentTierUtility.GetAffixTierMultiplierPercent(equipmentTier);

        rolledStats = EquipmentStatRoller.ScaleStats(rolledStats, multiplier);
        rolledSpecialStats = EquipmentStatRoller.ScaleSpecialStats(rolledSpecialStats, multiplier);

        generated.statModifiers += rolledStats;
        generated.specialStatModifiers += rolledSpecialStats;

        rollData.rolledStatModifiers = rolledStats;
        rollData.rolledSpecialStatModifiers = rolledSpecialStats;

        generated.appliedAffixes.Add(rollData);
    }

    private static void ApplySpecialAffix(
        GeneratedEquipmentData generated,
        EquipmentAffixRollData rollData)
    {
        if (generated == null || rollData == null || rollData.grantedTraitIds == null)
            return;

        foreach (int traitId in rollData.grantedTraitIds)
        {
            if (!generated.grantedTraitIds.Contains(traitId))
                generated.grantedTraitIds.Add(traitId);
        }
    }

    private static void AddNamePart(List<string> nameParts, EquipmentAffixDefinitionSO affix)
    {
        if (nameParts == null || affix == null)
            return;

        if (string.IsNullOrEmpty(affix.affixName))
            return;

        nameParts.Add(affix.affixName);
    }
}