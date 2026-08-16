using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StatusEffectProcessor
{
    public static void ApplyStatus(CharacterData target, StatusEffectApplyData applyData)
    {
        if (target == null || applyData == null)
            return;

        StatusEffectDefinitionSO def = GameDataRegistry.Instance.GetStatusEffect(applyData.statusEffectId);

        if (def == null)
            return;

        if (target.StatusEffects == null)
            target.StatusEffects = new List<StatusEffectRuntimeData>();

        StatusEffectRuntimeData existing = target.StatusEffects
            .FirstOrDefault(s => s != null && s.statusEffectId == applyData.statusEffectId);

        int stackAmount = applyData.stackAmount;

        stackAmount = Mathf.Max(1, stackAmount);

        if (existing != null)
        {
            existing.stack += stackAmount;

            if (def.maxStack > 0)
                existing.stack = Mathf.Min(existing.stack, def.maxStack);

            return;
        }

        StatusEffectRuntimeData runtime = new StatusEffectRuntimeData
        {
            statusEffectId = applyData.statusEffectId,
            stack = stackAmount
        };

        if (def.maxStack > 0)
            runtime.stack = Mathf.Min(runtime.stack, def.maxStack);

        target.StatusEffects.Add(runtime);
    }

    public static void ApplyTurnEffects(CharacterManager targetManager)
    {
        if (targetManager == null || targetManager.character == null)
            return;

        CharacterData target = targetManager.character;

        if (target.StatusEffects == null)
            return;

        foreach (StatusEffectRuntimeData runtime in target.StatusEffects.ToList())
        {
            if (runtime == null)
                continue;

            StatusEffectDefinitionSO def = GameDataRegistry.Instance.GetStatusEffect(runtime.statusEffectId);

            if (def == null)
                continue;

            if (def.effectType != StatusEffectType.TurnDamage)
                continue;

            int damage = Mathf.Max(0, runtime.stack * def.fixedDamagePerStack);

            if (damage <= 0)
                continue;

            ApplyFixedDamage(targetManager, damage, def.statusName);
        }
    }

    public static int ApplyCounterStatusPenalty(
    CharacterData target,
    SkillDefinitionSO counterSkill,
    int baseChance)
    {
        if (target == null || target.StatusEffects == null || counterSkill == null)
            return baseChance;

        float debuffPower = 0f;

        foreach (StatusEffectRuntimeData runtime in target.StatusEffects)
        {
            if (runtime == null)
                continue;

            StatusEffectDefinitionSO def = GameDataRegistry.Instance.GetStatusEffect(runtime.statusEffectId);

            if (def == null)
                continue;

            switch (def.effectType)
            {
                case StatusEffectType.CounterSuccessPenalty:
                    debuffPower += runtime.stack * def.counterPenaltyPerStack;
                    break;

                case StatusEffectType.PhysicalCounterPenalty:
                    if (counterSkill.type == SkillType.Physical)
                        debuffPower += runtime.stack * def.counterPenaltyPerStack;
                    break;

                case StatusEffectType.MagicalCounterPenalty:
                    if (counterSkill.type == SkillType.Magical)
                        debuffPower += runtime.stack * def.counterPenaltyPerStack;
                    break;
            }
        }

        if (debuffPower <= 0f)
            return baseChance;

        const float resistance = 100f;

        float finalChance = baseChance * resistance / (resistance + debuffPower);

        return Mathf.Clamp(Mathf.RoundToInt(finalChance), 0, 100);
    }

    public static int ApplyDamageTakenPerHitEffects(CharacterManager targetManager)
    {
        if (targetManager == null || targetManager.character == null)
            return 0;

        CharacterData target = targetManager.character;

        if (target.StatusEffects == null)
            return 0;

        int totalAdditionalDamage = 0;

        foreach (StatusEffectRuntimeData runtime in target.StatusEffects)
        {
            if (runtime == null)
                continue;

            StatusEffectDefinitionSO def = GameDataRegistry.Instance.GetStatusEffect(runtime.statusEffectId);

            if (def == null)
                continue;

            if (def.effectType != StatusEffectType.DamageTakenPerHit)
                continue;

            int damage = Mathf.Max(0, runtime.stack * def.fixedDamagePerStack);
            totalAdditionalDamage += damage;
        }

        if (totalAdditionalDamage > 0)
            ApplyFixedDamage(targetManager, totalAdditionalDamage, "열상 추가 피해");

        return totalAdditionalDamage;
    }

    public static void ReduceDurations(CharacterData target)
    {
        if (target == null || target.StatusEffects == null)
            return;

        for (int i = target.StatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData runtime =
                target.StatusEffects[i];

            if (runtime == null)
            {
                target.StatusEffects.RemoveAt(i);
                continue;
            }

            StatusEffectDefinitionSO def =
                GameDataRegistry.Instance.GetStatusEffect(
                    runtime.statusEffectId);

            // 불균형은 턴 경과가 아니라 다음 대응을 취소할 때 제거
            if (def != null &&
                def.effectType == StatusEffectType.CancelNextCounter)
            {
                continue;
            }

            runtime.stack--;

            if (runtime.stack <= 0)
                target.StatusEffects.RemoveAt(i);
        }
    }

    public static int GetCounterSuccessPenalty(CharacterData target, SkillDefinitionSO counterSkill)
    {
        if (target == null || target.StatusEffects == null || counterSkill == null)
            return 0;

        int penalty = 0;

        foreach (StatusEffectRuntimeData runtime in target.StatusEffects)
        {
            if (runtime == null)
                continue;

            StatusEffectDefinitionSO def = GameDataRegistry.Instance.GetStatusEffect(runtime.statusEffectId);

            if (def == null)
                continue;

            switch (def.effectType)
            {
                case StatusEffectType.CounterSuccessPenalty:
                    penalty += runtime.stack * def.counterPenaltyPerStack;
                    break;

                case StatusEffectType.PhysicalCounterPenalty:
                    if (counterSkill.type == SkillType.Physical)
                        penalty += runtime.stack * def.counterPenaltyPerStack;
                    break;

                case StatusEffectType.MagicalCounterPenalty:
                    if (counterSkill.type == SkillType.Magical)
                        penalty += runtime.stack * def.counterPenaltyPerStack;
                    break;
            }
        }

        return penalty;
    }

    public static bool ConsumeCancelNextCounterStatus(CharacterData target)
    {
        if (target == null || target.StatusEffects == null)
            return false;

        for (int i = 0; i < target.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData runtime = target.StatusEffects[i];

            if (runtime == null)
                continue;

            StatusEffectDefinitionSO def = GameDataRegistry.Instance.GetStatusEffect(runtime.statusEffectId);

            if (def == null)
                continue;

            if (def.effectType != StatusEffectType.CancelNextCounter)
                continue;

            target.StatusEffects.RemoveAt(i);

            return true;
        }

        return false;
    }

    public static int ConsumeStatusStack(
    CharacterData target,
    int statusEffectId,
    int maxConsumeStack,
    bool consumeAllStacks,
    bool removeConsumedStacks)
    {
        if (target == null || target.StatusEffects == null)
            return 0;

        StatusEffectRuntimeData runtime = target.StatusEffects
            .FirstOrDefault(s => s != null && s.statusEffectId == statusEffectId);

        if (runtime == null)
            return 0;

        int consumeAmount = consumeAllStacks
            ? runtime.stack
            : Mathf.Min(runtime.stack, maxConsumeStack);

        if (consumeAmount <= 0)
            return 0;

        if (removeConsumedStacks)
        {
            runtime.stack -= consumeAmount;

            if (runtime.stack <= 0)
                target.StatusEffects.Remove(runtime);
        }

        return consumeAmount;
    }

    public static int ApplyStatusConsumeEffect(
        CharacterManager casterManager,
        CharacterManager targetManager,
        StatusConsumeEffectData effect)
    {
        if (casterManager == null || casterManager.character == null)
            return 0;

        if (targetManager == null || targetManager.character == null)
            return 0;

        if (effect == null)
            return 0;

        int consumedStack = ConsumeStatusStack(
            targetManager.character,
            effect.sourceStatusId,
            effect.maxConsumeStack,
            effect.consumeAllStacks,
            effect.removeConsumedStacks);

        if (consumedStack <= 0)
            return 0;

        int totalDamage = 0;

        if (effect.damagePerStackAttackMultiplier > 0f)
        {
            int basePower = GetDamageBasePower(casterManager.character, effect.damageType);

            int damage = Mathf.RoundToInt(
                basePower *
                effect.damagePerStackAttackMultiplier *
                consumedStack);

            if (damage > 0)
            {
                SkillType damageType = effect.damageType;

                if (damageType == SkillType.Mixed)
                    damageType = SkillType.Physical;

                totalDamage = targetManager.TakeDamage(
                    damage,
                    damageType,
                    effect.damageAttribute);
            }
        }

        if (effect.resultStatusId > 0)
        {
            StatusEffectApplyData resultApplyData =
                new StatusEffectApplyData
                {
                    statusEffectId =
                        effect.resultStatusId,

                    baseChance =
                        effect.resultBaseChance +
                        effect.resultChancePerConsumedStack *
                        consumedStack,

                    stackAmount = Mathf.Max(
                        1,
                        effect.resultStackAmount)
                };

            int chance =
                StatusEffectCalculator.GetStatusApplyChance(
                    casterManager.character,
                    targetManager.character,
                    null,
                    resultApplyData);

            int roll = Random.Range(1, 101);

            if (roll <= chance)
            {
                ApplyStatus(
                    targetManager.character,
                    resultApplyData);

                Debug.Log(
                    $"{targetManager.character.Name}에게 " +
                    $"결과 상태이상 {effect.resultStatusId} 적용 성공 " +
                    $"확률:{chance}% 굴림:{roll}");
            }
            else
            {
                Debug.Log(
                    $"{targetManager.character.Name}에게 " +
                    $"결과 상태이상 {effect.resultStatusId} 적용 실패 " +
                    $"확률:{chance}% 굴림:{roll}");
            }
        }

        return totalDamage;
    }

    private static int GetDamageBasePower(CharacterData caster, SkillType damageType)
    {
        if (caster == null || caster.FinalStats == null)
            return 0;

        switch (damageType)
        {
            case SkillType.Magical:
                return caster.FinalStats.MagicalAttack;

            case SkillType.Physical:
                return caster.FinalStats.PhysicalAttack;

            case SkillType.Mixed:
                return Mathf.Max(
                    caster.FinalStats.PhysicalAttack,
                    caster.FinalStats.MagicalAttack);

            default:
                return caster.FinalStats.PhysicalAttack;
        }
    }

    private static void ApplyFixedDamage(CharacterManager targetManager, int damage, string sourceName)
    {
        if (targetManager == null || targetManager.character == null)
            return;

        if (damage <= 0)
            return;

        CharacterData target = targetManager.character;

        target.CurrentHp -= damage;

        if (target.CurrentHp <= 0)
        {
            target.CurrentHp = 0;
            target.IsAlive = false;
        }

        Debug.Log($"{target.Name} - {sourceName} 고정 피해 {damage}");

        targetManager.UpdateCharacterUI();
    }
}