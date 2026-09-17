using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StatusEffectProcessor
{
    public static void ApplyStatus(CharacterData target, StatusEffectApplyData applyData, string sourceCharacterId = null, CharacterData source = null)
    {
        if (target == null || applyData == null)
            return;

        StatusEffectDefinitionSO def = GameDataRegistry.Instance.GetStatusEffect(applyData.statusEffectId);

        if (def == null || !CanApplyStatus(target, def.id))
            return;

        if (target.StatusEffects == null)
            target.StatusEffects = new List<StatusEffectRuntimeData>();

        StatusEffectRuntimeData existing = target.StatusEffects
            .FirstOrDefault(s => s != null && s.statusEffectId == applyData.statusEffectId);

        int stackAmount = applyData.stackAmount;

        stackAmount = Mathf.Max(1, stackAmount);

        if (existing == null)
        {
            existing = new StatusEffectRuntimeData
            {
                statusEffectId = applyData.statusEffectId,
                sourceCharacterId = sourceCharacterId,
                remainingRounds = def.remainingRoundsOnApply
            };
            target.StatusEffects.Add(existing);
        }

        if (def.effectType == StatusEffectType.TurnDamage || def.effectType == StatusEffectType.DamageTakenPerHit)
        {
            EnsureDamageStacks(existing, def);
            if (source == null && !string.IsNullOrEmpty(sourceCharacterId))
                source = GameManager.Instance?.GetAllCharacters()
                    .Find(manager => manager != null && manager.character.ID == sourceCharacterId)?.character;
            int damage = def.fixedDamagePerStack;
            if (source?.FinalStats != null && def.damagePerStackAttackMultiplier > 0f)
                damage = Mathf.Max(1, Mathf.RoundToInt(GetDamageBasePower(source, def.damageType) * def.damagePerStackAttackMultiplier));
            existing.damageStacks.Add(new StatusEffectStackData
            {
                stack = stackAmount,
                damagePerStack = Mathf.Max(1, damage),
                sourceCharacterId = sourceCharacterId
            });
        }

        existing.stack += stackAmount;
        if (def.maxStack > 0 && existing.stack > def.maxStack)
        {
            if (def.automaticResultStatusId > 0 && CanApplyStatus(target, def.automaticResultStatusId))
            {
                ConsumeStatusStack(target, def.id, def.maxStack + 1, false);
                ApplyStatus(target, new StatusEffectApplyData
                {
                    statusEffectId = def.automaticResultStatusId,
                    stackAmount = 1
                }, sourceCharacterId, source);
            }
            if (existing.stack > def.maxStack)
                ConsumeStatusStack(target, def.id, existing.stack - def.maxStack, false);
        }
    }

    public static bool CanApplyStatus(CharacterData target, int statusEffectId)
    {
        StatusEffectDefinitionSO def = GameDataRegistry.Instance?.GetStatusEffect(statusEffectId);
        if (target == null || def == null)
            return false;
        if (def.immunityStatusId <= 0)
            return true;
        return target.StatusEffects == null || !target.StatusEffects.Any(s => s != null && s.stack > 0 &&
            (s.statusEffectId == statusEffectId ||
             (GameDataRegistry.Instance.GetStatusEffect(s.statusEffectId) is BuffDefinitionSO buff &&
              buff.blockedStatusId == statusEffectId)));
    }

    private static void EnsureDamageStacks(StatusEffectRuntimeData runtime, StatusEffectDefinitionSO def)
    {
        runtime.damageStacks ??= new List<StatusEffectStackData>();
        if (runtime.damageStacks.Count == 0 && runtime.stack > 0)
            runtime.damageStacks.Add(new StatusEffectStackData
            {
                stack = runtime.stack,
                damagePerStack = Mathf.Max(1, def.fixedDamagePerStack),
                sourceCharacterId = runtime.sourceCharacterId
            });
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

            EnsureDamageStacks(runtime, def);
            foreach (StatusEffectStackData batch in runtime.damageStacks)
            {
                bool wasAlive = target.IsAlive && target.CurrentHp > 0;
                if (!wasAlive) break;
                ApplyFixedDamage(targetManager, batch.stack * batch.damagePerStack, def.statusName);
                if (!target.IsAlive && !string.IsNullOrEmpty(batch.sourceCharacterId))
                {
                    CharacterManager source = GameManager.Instance?.GetAllCharacters()
                        .Find(manager => manager != null && manager.character.ID == batch.sourceCharacterId);
                    source?.RecoverOnKill(target, wasAlive);
                }
            }
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

        float finalChance = baseChance - debuffPower;

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

            EnsureDamageStacks(runtime, def);
            int damage = runtime.damageStacks.Sum(batch => batch.stack * batch.damagePerStack);
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

            if (def != null && (IsCounterImmunity(def.effectType) || def.effectType == StatusEffectType.TurnDamage))
                continue;
            if (def != null && def.remainingRoundsOnApply > 0)
            {
                if (runtime.remainingRounds <= 0)
                    runtime.remainingRounds = def.remainingRoundsOnApply;
                if (--runtime.remainingRounds <= 0)
                    target.StatusEffects.RemoveAt(i);
                continue;
            }

            ConsumeStatusStack(target, runtime.statusEffectId, 1, false);
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

    public static bool ConsumeCancelNextCounterStatus(CharacterData target, SkillDefinitionSO counterSkill = null)
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

            bool matches = def.effectType == StatusEffectType.CancelNextCounter ||
                (def.effectType == StatusEffectType.CancelNextPhysicalCounter &&
                 counterSkill != null && counterSkill.type != SkillType.Magical) ||
                (def.effectType == StatusEffectType.CancelNextMagicalCounter &&
                 counterSkill != null && counterSkill.type != SkillType.Physical);
            if (!matches)
                continue;

            target.StatusEffects.RemoveAt(i);
            if (GameDataRegistry.Instance.GetStatusEffect(def.immunityStatusId) is BuffDefinitionSO immunity)
                ApplyStatus(target, new StatusEffectApplyData
                {
                    statusEffectId = def.immunityStatusId,
                    stackAmount = Mathf.Max(1, immunity.incomingAttackDuration)
                });

            return true;
        }

        return false;
    }

    public static bool IsCounterImmunity(StatusEffectType effectType)
    {
        return effectType == StatusEffectType.CounterImmunity ||
            effectType == StatusEffectType.PhysicalCounterImmunity ||
            effectType == StatusEffectType.MagicalCounterImmunity;
    }

    public static void CompleteIncomingAttack(CharacterData target, SkillDefinitionSO skill,
        List<StatusEffectRuntimeData> immunitiesBeforeAttack)
    {
        if (target?.StatusEffects == null || skill == null || skill.isCounterSkill || immunitiesBeforeAttack == null)
            return;
        bool physical = skill.type != SkillType.Magical;
        bool magical = skill.type != SkillType.Physical;
        foreach (StatusEffectRuntimeData immunity in immunitiesBeforeAttack)
        {
            if (!target.StatusEffects.Contains(immunity))
                continue;
            StatusEffectDefinitionSO def = GameDataRegistry.Instance.GetStatusEffect(immunity.statusEffectId);
            if (def == null)
                continue;
            if (def.effectType == StatusEffectType.CounterImmunity ||
                (def.effectType == StatusEffectType.PhysicalCounterImmunity && physical) ||
                (def.effectType == StatusEffectType.MagicalCounterImmunity && magical))
            {
                if (--immunity.stack <= 0)
                    target.StatusEffects.Remove(immunity);
            }
        }
    }

    public static int ConsumeStatusStack(
    CharacterData target,
    int statusEffectId,
    int maxConsumeStack,
    bool consumeAllStacks,
    bool removeConsumedStacks = true)
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
            int remaining = consumeAmount;
            if (runtime.damageStacks != null)
            {
                while (remaining > 0 && runtime.damageStacks.Count > 0)
                {
                    StatusEffectStackData batch = runtime.damageStacks[0];
                    int removed = Mathf.Min(remaining, batch.stack);
                    batch.stack -= removed;
                    remaining -= removed;
                    if (batch.stack <= 0)
                        runtime.damageStacks.RemoveAt(0);
                }
            }
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

        if (!targetManager.character.IsAlive || targetManager.character.CurrentHp <= 0)
            return 0;

        if (effect == null)
            return 0;

        if (effect.resultStatusId > 0 && !CanApplyStatus(targetManager.character, effect.resultStatusId))
            return 0;

        int consumedStack = ConsumeStatusStack(
            targetManager.character,
            effect.sourceStatusId,
            effect.maxConsumeStack,
            effect.consumeAllStacks,
            effect.resultStatusId <= 0 && effect.removeConsumedStacks);

        if (consumedStack <= 0)
            return 0;

        int totalDamage = 0;

        if (effect.damagePerStackAttackMultiplier > 0f)
        {
            int basePower = GetDamageBasePower(casterManager.character, effect.damageType);
            CharacterStats casterStats = casterManager.character.FinalStats;
            int affinityBonus = casterStats == null ? 0 : effect.damageAttribute switch
            {
                SkillAttribute.Fire => casterStats.FireAffinity,
                SkillAttribute.Ice => casterStats.IceAffinity,
                SkillAttribute.Lightning => casterStats.LightningAffinity,
                SkillAttribute.Slash => casterStats.SlashAffinity,
                SkillAttribute.Pierce => casterStats.PierceAffinity,
                SkillAttribute.Smash => casterStats.SmashAffinity,
                _ => 0
            };

            int damage = Mathf.RoundToInt(
                basePower *
                effect.damagePerStackAttackMultiplier *
                consumedStack *
                Mathf.Max(0f, 1f + affinityBonus / 100f));

            if (damage > 0)
            {
                SkillType damageType = effect.damageType;

                if (damageType == SkillType.Mixed)
                    damageType = SkillType.Physical;

                bool wasAlive = targetManager.character.IsAlive && targetManager.character.CurrentHp > 0;
                totalDamage = targetManager.TakeDamage(
                    damage,
                    damageType,
                    effect.damageAttribute);
                casterManager.RecoverOnKill(targetManager.character, wasAlive);
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
                ConsumeStatusStack(targetManager.character, effect.sourceStatusId, consumedStack, false,
                    effect.removeConsumedStacks);
                ApplyStatus(
                    targetManager.character,
                    resultApplyData,
                    casterManager.character.ID,
                    casterManager.character);

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

        damage = target.LimitFatalDamage(damage);
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
