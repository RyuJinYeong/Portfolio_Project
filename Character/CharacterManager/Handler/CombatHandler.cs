using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatHandler : MonoBehaviour
{
    public CharacterManager characterManager;

    private List<SkillQueueData> skillQueue = new();
    private List<SkillQueueData> counterSkillQueue = new();


    public bool isDefenseTarget;
    public bool isDefenseCharacter;

    public Coroutine turnTimerCoroutine;
    private float totalDuration = 60.0f;

    private bool cancelNextSkill;

    public CharacterManager LastResolvedTarget { get; private set; }
    public bool LastSkillWasCancelled { get; private set; }
    public bool LastProtectionSucceeded { get; private set; }
    private readonly Dictionary<CharacterManager, SkillDefinitionSO> lastSuccessfulCounters = new();
    public IReadOnlyDictionary<CharacterManager, SkillDefinitionSO> LastSuccessfulCounters => lastSuccessfulCounters;
    private SkillQueueData preparedProtectionSkill;
    private CharacterManager preparedProtectionCharacter;
    private CounterResolveData preparedProtectionCounter;

    public void Awake()
    {
        characterManager = GetComponent<CharacterManager>();

        skillQueue = new List<SkillQueueData>();
        counterSkillQueue = new List<SkillQueueData>();
    }

    public void StartTurn(System.Action onTurnEnd)
    {
        characterManager.isPlayerTurn = true;
        bool useTurnTimer =
            TurnManager.Instance != null &&
            TurnManager.Instance.IsTurnTimeLimitEnabled;

        if (characterManager.character.Type != CharacterType.Character)
        {
            Debug.Log("AI 턴 시작");
            if (useTurnTimer)
                turnTimerCoroutine = StartCoroutine(TurnTimer(totalDuration, onTurnEnd));

            characterManager.UpdateCharacterUI();
            StartCoroutine(HandleAITurn(onTurnEnd));
        }
        else
        {
            Debug.Log("플레이어 턴 시작");
            if (useTurnTimer)
                turnTimerCoroutine = StartCoroutine(TurnTimer(totalDuration, onTurnEnd));

            characterManager.UpdateCharacterUI();

            UIManager.Instance.characterTargeting.SelectCharacter(characterManager);
        }
    }

    public IEnumerator TurnTimer(float duration, System.Action onTurnEnd)
    {
        float timeRemaining = duration;

        while (timeRemaining > 0)
        {
            UIManager.Instance.UpdateTurnTimer(timeRemaining);

            yield return null;
            timeRemaining -= Time.deltaTime;
        }

        if (characterManager.isPlayerTurn)
            onTurnEnd();
    }

    public void SelectSkill(SkillDefinitionSO skill, CharacterManager target, bool isConcealed = false)
    {
        if (skill == null || target == null)
            return;

        if (!characterManager.isPlayerTurn)
        {
            Debug.Log("본인 턴이 아니므로 스킬을 등록할 수 없습니다.");
            return;
        }

        if (skill.isCounterSkill)
        {
            Debug.Log("대응 스킬은 공격 스킬 큐에 등록할 수 없습니다.");
            return;
        }

        if (characterManager.isInMeleeCombat && skill.isRangedSkill)
        {
            Debug.Log("경합 상태에서는 원거리 스킬을 사용할 수 없습니다.");
            return;
        }

        if (characterManager.isInMeleeCombat &&
            !skill.isRangedSkill &&
            characterManager.meleeTarget != null &&
            characterManager.meleeTarget != target)
        {
            Debug.Log("경합 상태에서는 확정된 상대에게만 근접 스킬을 사용할 수 있습니다.");
            return;
        }

        if (!ConsumeResources(skill, isConcealed))
            return;

        if (!skill.isRangedSkill)
        {
            characterManager.isInMeleeCombat = true;
            characterManager.meleeTarget = target;
        }

        int order = skillQueue.Count + 1;
        SkillQueueData data = new SkillQueueData(
            skill,
            characterManager,
            target,
            isConcealed,
            order,
            true);

        skillQueue.Add(data);

        Debug.Log($"{skill.skillName} 공격 큐 등록. 은폐: {isConcealed}");

        RefreshSkillQueueUI(target);
        characterManager.UpdateCharacterUI();
        target.UpdateCharacterUI();
        UIManager.Instance.UpdateSkillTransparency(characterManager);

        UpdateSynergies();
    }

    public void RemoveSkillFromQueue(int index)
    {
        if (index < 0 || index >= skillQueue.Count)
            return;

        SkillQueueData entry = skillQueue[index];

        RefundResources(entry.skill, entry.isConcealed, 1f);

        CharacterManager target = entry.target;
        skillQueue.RemoveAt(index);

        RefreshSkillQueueUI(target);
        target?.UpdateCharacterUI();

        bool hasMeleeSkill = skillQueue.Any(s => !s.skill.isRangedSkill);

        if (!hasMeleeSkill && characterManager.isInMeleeCombat)
        {
            characterManager.isInMeleeCombat = false;
            characterManager.meleeTarget = null;

            Debug.Log("경합 상태 해제");
        }

        UIManager.Instance.characterTargeting.RefreshConfirmedTargetLines();

        characterManager.UpdateCharacterUI();
        UIManager.Instance.UpdateSkillTransparency(characterManager);
    }

    public void SelectCounterSkill(SkillDefinitionSO skill, CharacterManager target)
    {
        if (skill == null || target == null)
            return;

        if (!skill.isCounterSkill)
        {
            Debug.Log("대응 스킬이 아닙니다.");
            return;
        }

        if (skill.counterActionType == CounterActionType.Evade &&
            TurnManager.Instance != null &&
            TurnManager.Instance.defenseCharacter == characterManager &&
            TurnManager.Instance.defenseTarget != characterManager)
        {
            Debug.Log("회피는 다른 캐릭터를 보호할 때 사용할 수 없습니다.");
            return;
        }

        if (!ConsumeResources(skill, false))
            return;

        int order = counterSkillQueue.Count + 1;
        SkillQueueData data = new SkillQueueData(
            skill,
            characterManager,
            characterManager,
            false,
            order,
            true);

        counterSkillQueue.Add(data);

        Debug.Log($"{skill.skillName} 대응 큐 등록");

        RefreshCounterSkillQueueUI(characterManager);
        characterManager.UpdateCharacterUI();
        UIManager.Instance.UpdateSkillTransparency(characterManager);
    }

    public void RemoveCounterSkillFromQueue(int index)
    {
        if (index < 0 || index >= counterSkillQueue.Count)
            return;

        SkillQueueData entry = counterSkillQueue[index];

        if (entry.resourcesConsumed)
            RefundResources(entry.skill, false, 1f);

        counterSkillQueue.RemoveAt(index);

        RefreshCounterSkillQueueUI(entry.target);
        characterManager.UpdateCharacterUI();
        entry.target?.UpdateCharacterUI();
        UIManager.Instance.UpdateSkillTransparency(characterManager);
        UIManager.Instance.characterTargeting.RefreshConfirmedTargetLines();
    }

    public void SetOrResetCounterSkill(int idx, SkillDefinitionSO newSkill)
    {
        SkillDefinitionSO defaultCounter = GetDefaultCounterSkillDefinition();

        if (defaultCounter == null)
        {
            Debug.Log("기본 대응 스킬이 없습니다.");
            return;
        }

        SkillDefinitionSO finalSkill = newSkill != null ? newSkill : defaultCounter;
        bool evadeUnavailable =
            TurnManager.Instance != null &&
            TurnManager.Instance.defenseCharacter == characterManager &&
            TurnManager.Instance.defenseTarget != characterManager;
        bool defaultEvadeUnavailable =
            defaultCounter.counterActionType == CounterActionType.Evade &&
            evadeUnavailable;

        if (finalSkill.counterActionType == CounterActionType.Evade &&
            evadeUnavailable)
        {
            Debug.Log("회피는 다른 캐릭터를 보호할 때 사용할 수 없습니다.");
            return;
        }

        while (idx >= counterSkillQueue.Count)
        {
            counterSkillQueue.Add(new SkillQueueData(
                defaultEvadeUnavailable ? null : defaultCounter,
                characterManager,
                characterManager,
                false,
                counterSkillQueue.Count + 1));
        }

        SkillQueueData oldEntry = counterSkillQueue[idx];
        SkillDefinitionSO oldSkill = oldEntry.skill;

        if (oldEntry.resourcesConsumed)
            RefundResources(oldSkill, false, 1f);

        bool consumeImmediately = newSkill != null;

        if (consumeImmediately && !ConsumeResources(finalSkill, false))
        {
            if (oldEntry.resourcesConsumed)
                ConsumeResources(oldSkill, false);

            return;
        }

        counterSkillQueue[idx] = new SkillQueueData(
            finalSkill,
            characterManager,
            characterManager,
            false,
            idx + 1,
            consumeImmediately);

        RefreshCounterSkillQueueUI(characterManager);
        characterManager.UpdateCharacterUI();
        UIManager.Instance.UpdateSkillTransparency(characterManager);

        var tm = TurnManager.Instance;
        UIManager.Instance.UpdateCounterSkillPanel(tm.defenseCharacter, tm.defenseTarget);
    }

    private bool ConsumeResources(SkillDefinitionSO skill, bool isConcealed)
    {
        if (skill == null)
            return false;

        int staminaCost = skill.staminaCost;
        int mentalCost = skill.mentalCost;

        if (isConcealed)
        {
            staminaCost += SkillConcealUtility.GetAdditionalStaminaCost(skill);
            mentalCost += SkillConcealUtility.GetAdditionalMentalCost(skill);
        }

        if (characterManager.character.CurrentStamina < staminaCost ||
            characterManager.character.CurrentMentality < mentalCost)
        {
            Debug.Log("리소스가 부족하여 스킬을 사용할 수 없습니다.");
            return false;
        }

        characterManager.character.CurrentStamina -= staminaCost;
        characterManager.character.CurrentMentality -= mentalCost;

        characterManager.UpdateCharacterUI();

        return true;
    }

    private void RefundResources(SkillDefinitionSO skill, bool isConcealed, float rate)
    {
        if (skill == null)
            return;

        int stamina = skill.staminaCost;
        int mental = skill.mentalCost;

        if (isConcealed)
        {
            stamina += SkillConcealUtility.GetAdditionalStaminaCost(skill);
            mental += SkillConcealUtility.GetAdditionalMentalCost(skill);
        }

        characterManager.character.CurrentStamina += Mathf.FloorToInt(stamina * rate);
        characterManager.character.CurrentMentality += Mathf.FloorToInt(mental * rate);
        characterManager.UpdateCharacterUI();
    }

    public bool HandleEnemyDefeated()
    {
        if (characterManager.isInMeleeCombat &&
            characterManager.meleeTarget != null &&
            !characterManager.meleeTarget.character.IsAlive)
        {
            Debug.Log($"{characterManager.meleeTarget.character.Name} 처치 성공. 스킬 큐 취소 및 리소스 반환.");

            CancelRemainingSkills();

            characterManager.isInMeleeCombat = false;
            characterManager.meleeTarget = null;

            characterManager.hasExtraTurn = true;

            return true;
        }

        return false;
    }

    private void CancelRemainingSkills()
    {
        CharacterManager lastTarget = null;
        List<CharacterManager> affectedTargets = skillQueue
            .Where(item => item != null && item.target != null)
            .Select(item => item.target)
            .Distinct()
            .ToList();

        foreach (var item in skillQueue)
        {
            RefundResources(item.skill, item.isConcealed, 0.5f);
            lastTarget = item.target;
        }

        skillQueue.Clear();

        foreach (CharacterManager target in affectedTargets)
        {
            RefreshSkillQueueUI(target);
            target.UpdateCharacterUI();
        }

        if (lastTarget != null)
        {
            lastTarget.combatHandler.counterSkillQueue.Clear();
            lastTarget.combatHandler.RefreshCounterSkillQueueUI(lastTarget);
            lastTarget.UpdateCharacterUI();
        }

        characterManager.UpdateCharacterUI();

        Debug.Log($"스킬 시전 예약 취소 지구력: {characterManager.character.CurrentStamina}, 정신력: {characterManager.character.CurrentMentality}");
    }

    public void ExecuteSkillQueue(System.Action onTurnEnd)
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        if (skillQueue.Count > 0)
        {
            StartCoroutine(ExecuteSkills(onTurnEnd));
        }
        else
        {
            Debug.Log("선택된 스킬이 없습니다. 턴 종료");
            onTurnEnd();
        }
    }

    public IEnumerator ExecuteSkills(System.Action onTurnEnd)
    {
        while (skillQueue.Count > 0)
        {
            SkillQueueData item = skillQueue[0];

            if (cancelNextSkill)
            {
                cancelNextSkill = false;

                Debug.Log($"{item.skill.skillName} 스킬이 패링 대성공 효과로 취소됨");

                skillQueue.RemoveAt(0);
                RefreshSkillQueueUI(item.target);
                characterManager.UpdateCharacterUI();
                item.target?.UpdateCharacterUI();

                continue;
            }

            BattlePresentationDirector presentationDirector = BattlePresentationDirector.Instance;
            bool repeatsSameSkill =
                skillQueue.Count > 1 &&
                skillQueue[1] != null &&
                skillQueue[1].skill == item.skill &&
                skillQueue[1].target == item.target;

            if (presentationDirector != null)
                yield return presentationDirector.PlaySkill(
                    item,
                    () => UseSkill(item),
                    repeatsSameSkill);
            else
            {
                characterManager.battlePresentationHandler?.PlaySkill(item.skill);
                UseSkill(item);

                foreach (var counter in lastSuccessfulCounters)
                {
                    if (counter.Key.character != null && counter.Key.character.IsAlive)
                    {
                        counter.Key.battlePresentationHandler?.FaceTarget(characterManager.transform);
                        counter.Key.battlePresentationHandler?.PlaySkill(counter.Value);
                    }
                }

                if (characterManager.battlePresentationHandler != null)
                    yield return characterManager.battlePresentationHandler.WaitForSkill(
                        repeatsSameSkill ? 0.2f : 0f);

                foreach (var counter in lastSuccessfulCounters)
                {
                    if (counter.Key.battlePresentationHandler != null)
                        yield return counter.Key.battlePresentationHandler.WaitForSkill();
                }
            }

            if (item.target != null)
            {
                RefreshSkillQueueUI(item.target);
                RefreshCounterSkillQueueUI(item.target);
                item.target.UpdateCharacterUI();
            }

            if (item.target != null &&
                !item.target.character.IsAlive &&
                !item.skill.isRangedSkill)
            {
                if (HandleEnemyDefeated())
                {
                    onTurnEnd?.Invoke();
                    yield break;
                }
            }

        }

        skillQueue.Clear();
        counterSkillQueue.Clear();

        onTurnEnd();
    }

    public void SetDefenseCharacter(CharacterManager defenseCharacter)
    {
        Debug.Log($"{defenseCharacter.character.Name} - Set Defense Character");

        defenseCharacter.combatHandler.isDefenseCharacter = true;

        UIManager.Instance.characterTargeting.SelectCharacter(defenseCharacter);
    }

    public void SetDefenseTarget(CharacterManager target)
    {
        if (!isDefenseCharacter || target == null)
            return;

        CharacterManager attacker = TurnManager.Instance != null
            ? TurnManager.Instance.currentCharacter
            : null;
        bool isAttackedTarget = attacker != null &&
                                attacker.GetSkillQueue().Any(entry =>
                                    entry != null && entry.target == target);

        if (!isAttackedTarget)
        {
            Debug.Log("현재 공격받는 캐릭터만 대응 대상으로 지정할 수 있습니다.");
            return;
        }

        target.combatHandler.isDefenseTarget = true;
        TurnManager.Instance.defenseTarget = target;

        SkillDefinitionSO defaultCounter = GetDefaultCounterSkillDefinition();
        counterSkillQueue.Clear();

        if (defaultCounter != null && attacker != null)
        {
            foreach (SkillQueueData incomingAttack in attacker.GetSkillQueue())
            {
                if (incomingAttack == null ||
                    (incomingAttack.target != characterManager && incomingAttack.target != target))
                {
                    continue;
                }

                bool evadeCannotProtectOther =
                    incomingAttack.target != characterManager &&
                    defaultCounter.counterActionType == CounterActionType.Evade;
                counterSkillQueue.Add(new SkillQueueData(
                    evadeCannotProtectOther ? null : defaultCounter,
                    characterManager,
                    characterManager,
                    false,
                    counterSkillQueue.Count + 1));
            }
        }

        RefreshCounterSkillQueueUI(characterManager);

        Debug.Log($"방어자 : {characterManager.character.Name} - 방어대상 : {target.character.Name}");

        if (TurnManager.Instance.defenseCharacter != null &&
            TurnManager.Instance.defenseTarget != null)
        {
            UIManager.Instance.UpdateCounterSkillPanel(
                TurnManager.Instance.defenseCharacter,
                TurnManager.Instance.defenseTarget);
        }
    }

    public void PrepareProtectionForPresentation(SkillQueueData queuedSkill)
    {
        preparedProtectionSkill = null;
        preparedProtectionCharacter = null;
        preparedProtectionCounter = null;
        LastResolvedTarget = null;
        LastSkillWasCancelled = false;
        LastProtectionSucceeded = false;
        lastSuccessfulCounters.Clear();

        if (queuedSkill == null || queuedSkill.skill == null || queuedSkill.target == null)
            return;

        SkillDefinitionSO attackSkill = queuedSkill.skill;
        CharacterManager originalTarget = queuedSkill.target;
        LastResolvedTarget = originalTarget;

        if (originalTarget.combatHandler == null ||
            !originalTarget.combatHandler.isDefenseTarget ||
            TurnManager.Instance == null)
        {
            return;
        }

        CharacterManager defenseCharacter = TurnManager.Instance.defenseCharacter;

        if (defenseCharacter == null)
            return;

        preparedProtectionSkill = queuedSkill;
        preparedProtectionCharacter = defenseCharacter;
        preparedProtectionCounter = TryUseCounterSkill(
            attackSkill,
            defenseCharacter,
            characterManager,
            originalTarget,
            false);

        if (preparedProtectionCounter == null)
            return;

        LastProtectionSucceeded = preparedProtectionCounter.IsSuccess;

        if (preparedProtectionCounter.cancelCurrentSkill)
        {
            LastResolvedTarget = null;
            LastSkillWasCancelled = true;
            return;
        }

        if (preparedProtectionCounter.IsSuccess ||
            attackSkill.HasAttackEffect(AttackEffectType.Breakthrough))
        {
            LastResolvedTarget = defenseCharacter;
        }
    }

    public void UseSkill(SkillQueueData queuedSkill)
    {
        bool hasPreparedProtection =
            queuedSkill != null &&
            ReferenceEquals(preparedProtectionSkill, queuedSkill);
        CharacterManager cachedProtectionCharacter = preparedProtectionCharacter;
        CounterResolveData cachedProtectionCounter = preparedProtectionCounter;

        preparedProtectionSkill = null;
        preparedProtectionCharacter = null;
        preparedProtectionCounter = null;

        LastResolvedTarget = null;
        LastSkillWasCancelled = false;
        LastProtectionSucceeded = hasPreparedProtection &&
            cachedProtectionCounter != null &&
            cachedProtectionCounter.IsSuccess;

        if (!hasPreparedProtection)
            lastSuccessfulCounters.Clear();

        if (queuedSkill == null || queuedSkill.skill == null || queuedSkill.target == null)
            return;

        SkillDefinitionSO attackSkill = queuedSkill.skill;
        CharacterManager originalTarget = queuedSkill.target;
        CharacterManager actualTarget = originalTarget;
        LastResolvedTarget = originalTarget;

        skillQueue.RemoveAt(0);

        ResolveConceal(queuedSkill, skillQueue.Count(s => s != null && s.isConcealed));

        CounterResolveData defenseCounter = hasPreparedProtection
            ? cachedProtectionCounter
            : null;
        CounterResolveData appliedCounter = null;

        if (hasPreparedProtection || originalTarget.combatHandler.isDefenseTarget)
        {
            CharacterManager defenseCharacter = hasPreparedProtection
                ? cachedProtectionCharacter
                : TurnManager.Instance.defenseCharacter;

            if (defenseCharacter != null)
            {
                if (!hasPreparedProtection)
                {
                    defenseCounter = TryUseCounterSkill(
                        attackSkill,
                        defenseCharacter,
                        characterManager,
                        originalTarget);
                }

                if (defenseCounter != null)
                {
                    LastProtectionSucceeded = defenseCounter.IsSuccess;
                    appliedCounter = defenseCounter;

                    if (hasPreparedProtection)
                        ShowCounterGreatSuccessResult(defenseCounter, defenseCharacter);

                    if (defenseCounter.cancelCurrentSkill)
                    {
                        LastResolvedTarget = null;
                        LastSkillWasCancelled = true;
                        Debug.Log($"{defenseCharacter.character.Name}의 파훼 대성공. {attackSkill.skillName} 무효화");
                        EndSynergyEffects();
                        return;
                    }

                    if (defenseCounter.cancelNextSkill)
                        cancelNextSkill = true;

                    if (attackSkill.HasAttackEffect(AttackEffectType.Breakthrough))
                    {
                        LastResolvedTarget = defenseCharacter;
                        ResolveBreakthroughProtection(
                            attackSkill,
                            originalTarget,
                            defenseCharacter,
                            defenseCounter);

                        EndSynergyEffects();
                        return;
                    }

                    if (defenseCounter.IsSuccess)
                    {
                        actualTarget = defenseCharacter;

                        ApplyMeleeTargetChangeAfterProtection(
                            attackSkill,
                            originalTarget,
                            defenseCharacter);
                    }
                }
            }
        }

        CounterResolveData targetCounter = null;

        bool protectionAlreadyCountered =
            defenseCounter != null &&
            defenseCounter.IsSuccess;

        if (!protectionAlreadyCountered &&
            actualTarget.combatHandler.counterSkillQueue.Count > 0)
        {
            targetCounter = TryUseCounterSkill(
                attackSkill,
                actualTarget,
                characterManager,
                actualTarget);

            if (targetCounter != null)
            {
                appliedCounter = targetCounter;

                if (targetCounter.cancelCurrentSkill)
                {
                    LastResolvedTarget = null;
                    LastSkillWasCancelled = true;
                    Debug.Log($"{actualTarget.character.Name}의 파훼 대성공. {attackSkill.skillName} 무효화");
                    EndSynergyEffects();
                    return;
                }

                if (targetCounter.cancelNextSkill)
                    cancelNextSkill = true;
            }
        }

        float counterDamageMultiplier = appliedCounter != null
            ? appliedCounter.damageMultiplier
            : 1f;

        float attackGreatSuccessDamageMultiplier =
            GetAttackGreatSuccessDamageMultiplier(appliedCounter);

        LastResolvedTarget = actualTarget;
        ApplyAttackDamage(attackSkill, actualTarget, counterDamageMultiplier, attackGreatSuccessDamageMultiplier);

        if (appliedCounter != null &&
            appliedCounter.attackGreatSuccess)
        {
            UIManager.Instance?.ShowCombatResult(
                "Critical",
                actualTarget.transform.position);

            Debug.Log(
                $"{characterManager.character.Name} 공격 대성공. " +
                $"{attackSkill.skillName} 피해 배율 " +
                $"{attackGreatSuccessDamageMultiplier:F2}");
        }

        ApplySkillStatusConsumeEffects(attackSkill, actualTarget);

        ApplySkillAdditionalEffects(attackSkill, actualTarget, appliedCounter);

        actualTarget.UpdateCharacterUI();

        EndSynergyEffects();
    }

    private void ResolveBreakthroughProtection(
    SkillDefinitionSO attackSkill,
    CharacterManager originalTarget,
    CharacterManager protector,
    CounterResolveData protectorCounter)
    {
        if (attackSkill == null || originalTarget == null || protector == null || protectorCounter == null)
            return;

        CounterActionType actionType = protectorCounter.counterSkill != null
            ? protectorCounter.counterSkill.counterActionType
            : CounterActionType.None;

        bool stopsBreakthrough =
            protectorCounter.IsSuccess &&
            actionType != CounterActionType.Evade;

        float attackGreatSuccessDamageMultiplier =
            GetAttackGreatSuccessDamageMultiplier(protectorCounter);

        ApplyAttackDamage(attackSkill, protector, protectorCounter.damageMultiplier, attackGreatSuccessDamageMultiplier);

        if (protectorCounter.attackGreatSuccess)
        {
            UIManager.Instance?.ShowCombatResult(
                "Critical",
                protector.transform.position);

            Debug.Log(
                $"{characterManager.character.Name} 돌파 공격 대성공. " +
                $"{attackSkill.skillName} 피해 배율 " +
                $"{attackGreatSuccessDamageMultiplier:F2}");
        }

        ApplySkillStatusConsumeEffects(attackSkill, protector);

        ApplySkillAdditionalEffects(attackSkill, protector, protectorCounter);

        protector.UpdateCharacterUI();

        if (stopsBreakthrough)
        {
            Debug.Log($"{protector.character.Name}이 돌파를 저지함");

            ApplyMeleeTargetChangeAfterProtection(
                attackSkill,
                originalTarget,
                protector);

            return;
        }

        CancelProtectionAfterBreakthrough(originalTarget, protector);

        Debug.Log($"{attackSkill.skillName} 돌파 성공. {protector.character.Name}의 보호선을 돌파함");
    }

    private CounterResolveData TryUseCounterSkill(
    SkillDefinitionSO attackSkill,
    CharacterManager counterUser,
    CharacterManager attacker,
    CharacterManager protectedTarget,
    bool showGreatSuccessResult = true)
    {
        if (counterUser == null || counterUser.combatHandler.counterSkillQueue.Count == 0)
            return null;

        SkillQueueData counterData = counterUser.combatHandler.counterSkillQueue[0];
        counterUser.combatHandler.counterSkillQueue.RemoveAt(0);

        SkillDefinitionSO counterSkill = counterData.skill;

        if (counterSkill == null)
        {
            counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
            counterUser.UpdateCharacterUI();
            return null;
        }

        if (!counterData.resourcesConsumed &&
            !counterUser.combatHandler.ConsumeResources(counterSkill, false))
        {
            counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
            counterUser.UpdateCharacterUI();
            return null;
        }

        bool isProtectingOther = counterUser != protectedTarget;

        CounterResolveData resolveData = new CounterResolveData
        {
            result = CounterResult.Fail,
            counterSkill = counterSkill,
            counterUser = counterUser,
            damageMultiplier = 1f,
            effectScale = 1f
        };

        if (StatusEffectProcessor.ConsumeCancelNextCounterStatus(counterUser.character))
        {
            Debug.Log($"{counterUser.character.Name}의 대응이 불균형으로 취소됨");

            counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
            counterUser.UpdateCharacterUI();
            return resolveData;
        }

        if (!CanUseCounterAgainst(attackSkill, counterSkill, isProtectingOther))
        {
            Debug.Log($"{counterUser.character.Name}의 {counterSkill.skillName} 대응 불가");

            counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
            counterUser.UpdateCharacterUI();
            return resolveData;
        }

        int formationBonus = 0;

        if (!counterSkill.isRangedSkill && counterUser != protectedTarget)
        {
            formationBonus = FormationUtility.GetProtectionFormationBonus(
                counterUser.isFront ? FormationRow.Front : FormationRow.Back,
                protectedTarget.isFront ? FormationRow.Front : FormationRow.Back);
        }

        int revealBonus = 0;

        if (attackSkill != null && counterData.revealLevel == RevealLevel.Full)
            revealBonus = SkillConcealUtility.FullRevealCounterBonus;

        int chance = SkillCounterCalculator.GetCounterSuccessChance(
            attacker.character,
            counterUser.character,
            attackSkill,
            counterSkill,
            isProtectingOther);

        chance += formationBonus;
        chance += revealBonus;

        if (counterSkill.isRangedSkill)
            chance -= 10;

        chance = StatusEffectProcessor.ApplyCounterStatusPenalty(
            counterUser.character,
            counterSkill,
            chance);

        chance = Mathf.Clamp(chance, 5, 95);

        float counterPowerRatio = SkillCounterCalculator.GetCounterPowerRatio(
            attacker.character,
            counterUser.character,
            attackSkill,
            counterSkill);

        float successEffectScale = SkillCounterCalculator.GetCounterSuccessEffectScale(counterPowerRatio);

        int greatSuccessChance = 0;

        bool hasStyleAdvantage = HasCounterStyleAdvantage(
            counterSkill.style,
            attackSkill.style);

        if (hasStyleAdvantage)
        {
            greatSuccessChance = GetCounterGreatSuccessChance(
                chance,
                counterSkill.counterActionType);
        }

        int roll = Random.Range(1, 101);

        CounterResult result;

        if (greatSuccessChance > 0 && roll <= greatSuccessChance)
        {
            result = CounterResult.GreatSuccess;
        }
        else if (roll <= chance)
        {
            result = CounterResult.Success;
        }
        else
        {
            result = CounterResult.Fail;
        }

        resolveData.result = result;
        resolveData.effectScale = successEffectScale;

        if (resolveData.IsSuccess)
            lastSuccessfulCounters[counterUser] = counterSkill;

        switch (result)
        {
            case CounterResult.Fail:
                resolveData.damageMultiplier = 1f;

                bool attackHasStyleAdvantage = HasCounterStyleAdvantage(
                    attackSkill.style,
                    counterSkill.style);

                if (attackHasStyleAdvantage &&
                    attacker != null &&
                    attacker.character != null &&
                    attacker.character.FinalSpecialStats != null)
                {
                    int attackGreatSuccessChance = Mathf.Clamp(
                        attacker.character.FinalSpecialStats.CriticalChance,
                        0,
                        100);

                    int attackGreatSuccessRoll = Random.Range(1, 101);

                    resolveData.attackGreatSuccess =
                        attackGreatSuccessRoll <= attackGreatSuccessChance;

                    Debug.Log(
                        $"{attacker.character.Name} 공격 대성공 판정 " +
                        $"확률:{attackGreatSuccessChance}% " +
                        $"굴림:{attackGreatSuccessRoll} " +
                        $"결과:{resolveData.attackGreatSuccess}");
                }

                Debug.Log(
                    $"{counterUser.character.Name} {counterSkill.skillName} 대응 실패 " +
                    $"성공률:{chance}% 대성공률:{greatSuccessChance}% 위력비:{counterPowerRatio:F2}");
                break;

            case CounterResult.Success:
                if (counterSkill.counterActionType == CounterActionType.Evade)
                {
                    resolveData.damageMultiplier = GetCounterSuccessDamageMultiplier(
                        attackSkill,
                        counterSkill,
                        attacker.character,
                        counterUser.character);

                    resolveData.skipStatusEffects = true;
                }
                else
                {
                    resolveData.damageMultiplier = 1f;
                    ApplyCounterSuccess(counterUser, counterSkill, successEffectScale);
                }

                Debug.Log(
                    $"{counterUser.character.Name} {counterSkill.skillName} 대응 성공 " +
                    $"성공률:{chance}% 대성공률:{greatSuccessChance}% 위력비:{counterPowerRatio:F2}");
                break;

            case CounterResult.GreatSuccess:
                ApplyCounterGreatSuccess(resolveData, counterUser, counterSkill, attackSkill);

                if (showGreatSuccessResult)
                    ShowCounterGreatSuccessResult(resolveData, counterUser);

                Debug.Log(
                    $"{counterUser.character.Name} {counterSkill.skillName} 대응 대성공 " +
                    $"성공률:{chance}% 대성공률:{greatSuccessChance}% 위력비:{counterPowerRatio:F2}");
                break;
        }

        counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
        counterUser.UpdateCharacterUI();

        return resolveData;
    }

    private static void ShowCounterGreatSuccessResult(
        CounterResolveData resolveData,
        CharacterManager counterUser)
    {
        if (resolveData == null ||
            resolveData.result != CounterResult.GreatSuccess ||
            resolveData.counterSkill == null ||
            counterUser == null)
        {
            return;
        }

        string resultText = resolveData.counterSkill.counterActionType switch
        {
            CounterActionType.Parry => "Parry",
            CounterActionType.Guard => "Guard",
            CounterActionType.Evade => "Evade",
            _ => null
        };

        if (!string.IsNullOrEmpty(resultText))
        {
            UIManager.Instance?.ShowCombatResult(
                resultText,
                counterUser.transform.position);
        }
    }

    private void ApplyCounterGreatSuccess(
    CounterResolveData resolveData,
    CharacterManager counterUser,
    SkillDefinitionSO counterSkill,
    SkillDefinitionSO attackSkill)
    {
        if (resolveData == null ||
            counterUser == null ||
            counterSkill == null ||
            attackSkill == null)
        {
            return;
        }

        switch (counterSkill.counterActionType)
        {
            case CounterActionType.Guard:
                resolveData.damageMultiplier = 0f;
                resolveData.skipStatusEffects = false;
                break;

            case CounterActionType.Evade:
                resolveData.damageMultiplier = 0f;
                resolveData.skipStatusEffects = true;
                break;

            case CounterActionType.Parry:
                resolveData.damageMultiplier = 0f;
                resolveData.skipStatusEffects = false;

                if (!attackSkill.isRangedSkill)
                    resolveData.cancelNextSkill = true;

                break;

            case CounterActionType.Break:
                resolveData.damageMultiplier = 1f;
                resolveData.cancelCurrentSkill = true;
                resolveData.skipStatusEffects = true;
                break;

            default:
                resolveData.damageMultiplier = 1f;
                break;
        }
    }

    private void ApplyCounterSuccess(
    CharacterManager counterUser,
    SkillDefinitionSO counterSkill,
    float effectScale)
    {
        if (counterUser == null || counterUser.character == null || counterSkill == null)
            return;

        if (counterSkill.counterActionType == CounterActionType.Evade)
            return;

        int basePower = GetCounterArmorBasePower(counterUser.character, counterSkill);

        int armorGain = Mathf.RoundToInt(
            basePower *
            counterSkill.successArmorAttackMultiplier *
            effectScale);

        if (armorGain <= 0)
            return;

        if (counterSkill.type == SkillType.Magical)
            counterUser.character.MagicalArmor += armorGain;
        else
            counterUser.character.PhysicalArmor += armorGain;

        Debug.Log($"{counterUser.character.Name} {counterSkill.skillName} 대응 방어도 획득: {armorGain}");
    }

    private void ApplyMeleeTargetChangeAfterProtection(
        SkillDefinitionSO attackSkill,
        CharacterManager originalTarget,
        CharacterManager defenseCharacter)
    {
        if (attackSkill == null || originalTarget == null || defenseCharacter == null)
            return;

        if (attackSkill.isRangedSkill)
            return;

        originalTarget.combatHandler.counterSkillQueue.Clear();
        originalTarget.combatHandler.RefreshCounterSkillQueueUI(originalTarget);
        originalTarget.UpdateCharacterUI();

        originalTarget.combatHandler.isDefenseTarget = false;
        originalTarget.isInMeleeCombat = false;
        originalTarget.meleeTarget = null;

        defenseCharacter.isInMeleeCombat = true;
        defenseCharacter.meleeTarget = characterManager;

        for (int i = 0; i < skillQueue.Count; i++)
        {
            if (skillQueue[i].target == originalTarget)
                skillQueue[i].target = defenseCharacter;
        }

        characterManager.meleeTarget = defenseCharacter;

        Debug.Log($"공격자 {characterManager.character.Name} -> {originalTarget.character.Name}에서 {defenseCharacter.character.Name}로 경합 대상 변경");
    }

    private void ApplyAttackDamage(
    SkillDefinitionSO skill,
    CharacterManager target,
    float counterDamageMultiplier,
    float specialDamageMultiplier)
    {
        if (skill == null || target == null)
            return;

        if (skill.damageComponents == null || skill.damageComponents.Count == 0)
            return;

        SkillRuntimeData runtime = SkillManager.GetRuntime(characterManager.character, skill.uid);

        float offHandMultiplier = 1f;

        if (runtime != null && runtime.useOffHand)
            offHandMultiplier = 0.9f;

        int totalFinalDamage = 0;

        foreach (SkillDamageComponentData component in skill.damageComponents)
        {
            if (component == null)
                continue;

            int hitCount = component.hitCount < 1 ? 1 : component.hitCount;

            for (int i = 0; i < hitCount; i++)
            {
                float damageMultiplier = component.damageMultiplier;
                damageMultiplier *= offHandMultiplier;
                damageMultiplier *= counterDamageMultiplier;
                damageMultiplier *= specialDamageMultiplier;

                int damage = CalculateComponentDamage(component, damageMultiplier);

                SkillType damageType = component.damageType;

                if (damageType == SkillType.Mixed)
                    damageType = SkillType.Physical;

                int finalDamage = target.TakeDamage(
                    damage,
                    damageType,
                    component.attribute,
                    counterDamageMultiplier <= 0f);

                totalFinalDamage += finalDamage;

                int statusAdditionalDamage = StatusEffectProcessor.ApplyDamageTakenPerHitEffects(target);
                totalFinalDamage += statusAdditionalDamage;

                Debug.Log(
                    $"{characterManager.character.Name} - {skill.skillName} 사용 -> {target.character.Name}, " +
                    $"{component.attribute} {damageType} 피해 {finalDamage}");
            }
        }

        RegisterSkillRuntimeResult(skill, totalFinalDamage, target);

        Debug.Log($"{characterManager.character.Name} - {skill.skillName} 총 피해 {totalFinalDamage}");
    }

    private int CalculateComponentDamage(SkillDamageComponentData component, float damageMultiplier)
    {
        if (component == null || characterManager == null || characterManager.character == null)
            return 0;

        CharacterData caster = characterManager.character;

        switch (component.damageType)
        {
            case SkillType.Physical:
                return Mathf.FloorToInt(
                    caster.FinalStats.PhysicalAttack *
                    damageMultiplier *
                    caster.PhysicalDamageMultiplier);

            case SkillType.Magical:
                return Mathf.FloorToInt(
                    caster.FinalStats.MagicalAttack *
                    damageMultiplier *
                    caster.MagicalDamageMultiplier);

            case SkillType.Mixed:
                return Mathf.FloorToInt(
                    caster.FinalStats.PhysicalAttack *
                    damageMultiplier *
                    caster.PhysicalDamageMultiplier);

            default:
                return 0;
        }
    }

    private float GetCounterSuccessDamageMultiplier(
    SkillDefinitionSO attackSkill,
    SkillDefinitionSO counterSkill,
    CharacterData attacker,
    CharacterData counterUser)
    {
        if (counterSkill == null)
            return 1f;

        if (counterSkill.counterActionType != CounterActionType.Evade)
            return 1f;

        float attackFinalSpeed = GetFinalSkillSpeed(attacker, attackSkill);
        float evadeFinalSpeed = GetFinalSkillSpeed(counterUser, counterSkill);

        return GetEvadeSuccessDamageMultiplier(
            attackFinalSpeed,
            evadeFinalSpeed,
            counterSkill.minEvadeReductionRate);
    }

    private float GetEvadeSuccessDamageMultiplier(
        float attackFinalSpeed,
        float evadeFinalSpeed,
        float minReductionRate = 0.10f)
    {
        if (attackFinalSpeed <= 0f || evadeFinalSpeed <= 0f)
            return 1f;

        float reductionRate = 1f - (attackFinalSpeed / evadeFinalSpeed);

        reductionRate = Mathf.Max(minReductionRate, reductionRate);
        reductionRate = Mathf.Min(1f, reductionRate);

        return 1f - reductionRate;
    }

    private float GetFinalSkillSpeed(CharacterData character, SkillDefinitionSO skill)
    {
        if (character == null || character.FinalStats == null || skill == null)
            return 1f;

        float baseSpeed = 1f;

        switch (skill.type)
        {
            case SkillType.Physical:
                baseSpeed = character.FinalStats.AttackSpeed;
                break;

            case SkillType.Magical:
                baseSpeed = character.FinalStats.CastSpeed;
                break;

            case SkillType.Mixed:
                baseSpeed = Mathf.Min(
                    character.FinalStats.AttackSpeed,
                    character.FinalStats.CastSpeed);
                break;
        }

        return baseSpeed * skill.activationSpeed;
    }

    private int GetCounterArmorBasePower(CharacterData character, SkillDefinitionSO counterSkill)
    {
        if (character == null || character.FinalStats == null || counterSkill == null)
            return 0;

        if (counterSkill.type == SkillType.Magical)
            return character.FinalStats.MagicalAttack;

        return character.FinalStats.PhysicalAttack;
    }

    private void ApplySkillAdditionalEffects(
        SkillDefinitionSO skill,
        CharacterManager target,
        CounterResolveData counterResolveData)
    {
        if (skill == null || target == null)
            return;

        if (!skill.HasStatusEffects())
            return;

        if (counterResolveData != null && counterResolveData.skipStatusEffects)
            return;

        foreach (var effect in skill.statusEffects)
        {
            if (effect == null)
                continue;

            if (!CanApplyStatusAfterCounter(effect.statusEffectId, counterResolveData))
                continue;

            int chance = StatusEffectCalculator.GetStatusApplyChance(
                characterManager.character,
                target.character,
                skill,
                effect);

            int roll = Random.Range(1, 101);

            bool success = StatusEffectCalculator.RollStatusApply(
                characterManager.character,
                target.character,
                skill,
                effect,
                roll);

            if (success)
            {
                Debug.Log(
                    $"{target.character.Name}에게 상태이상 {effect.statusEffectId} 적용 성공 " +
                    $"확률:{chance}% 굴림:{roll}");

                StatusEffectProcessor.ApplyStatus(target.character, effect);
            }
            else
            {
                Debug.Log(
                    $"{target.character.Name}에게 상태이상 {effect.statusEffectId} 적용 실패 " +
                    $"확률:{chance}% 굴림:{roll}");
            }
        }
    }

    private bool CanUseCounterAgainst(
    SkillDefinitionSO attackSkill,
    SkillDefinitionSO counterSkill,
    bool isProtectingOther)
    {
        if (attackSkill == null || counterSkill == null)
            return false;

        if (!counterSkill.isCounterSkill)
            return false;

        switch (counterSkill.counterActionType)
        {
            case CounterActionType.Guard:
                return true;

            case CounterActionType.Evade:
                return !isProtectingOther;

            case CounterActionType.Parry:
                return true;

            case CounterActionType.Break:
                return attackSkill.isRangedSkill &&
                       counterSkill.isRangedSkill;

            default:
                return false;
        }
    }

    private int GetCounterGreatSuccessChance(
    int counterSuccessChance,
    CounterActionType actionType)
    {
        float ratio = GetCounterGreatSuccessRatio(actionType);

        int result = Mathf.RoundToInt(counterSuccessChance * ratio);

        return Mathf.Clamp(result, 0, counterSuccessChance);
    }

    private float GetCounterGreatSuccessRatio(CounterActionType actionType)
    {
        switch (actionType)
        {
            case CounterActionType.Guard:
                return 0.20f;

            case CounterActionType.Evade:
                return 0.20f;

            case CounterActionType.Parry:
                return 0.15f;

            case CounterActionType.Break:
                return 0.15f;

            default:
                return 0f;
        }
    }

    private float GetAttackGreatSuccessDamageMultiplier(
    CounterResolveData counterResolveData)
    {
        if (counterResolveData == null ||
            !counterResolveData.attackGreatSuccess)
        {
            return 1f;
        }

        if (characterManager == null ||
            characterManager.character == null ||
            characterManager.character.FinalSpecialStats == null)
        {
            return 1f;
        }

        int criticalDamagePercent =
            characterManager.character
                .FinalSpecialStats
                .CriticalDamageBonus;

        return Mathf.Max(1f, criticalDamagePercent / 100f);
    }

    private bool CanApplyStatusAfterCounter(
    int statusEffectId,
    CounterResolveData counterResolveData)
    {
        if (counterResolveData == null)
            return true;

        if (counterResolveData.result != CounterResult.GreatSuccess)
            return true;

        SkillDefinitionSO counterSkill = counterResolveData.counterSkill;

        if (counterSkill == null)
            return true;

        if (counterSkill.counterActionType != CounterActionType.Guard &&
            counterSkill.counterActionType != CounterActionType.Parry)
            return true;

        return IsStatusAllowedThroughPerfectGuardOrParry(statusEffectId);
    }

    private bool IsStatusAllowedThroughPerfectGuardOrParry(int statusEffectId)
    {
        StatusEffectDefinitionSO def = GameDataRegistry.Instance.GetStatusEffect(statusEffectId);

        if (def == null)
            return false;

        if (statusEffectId == 4001)
            return false;

        if (statusEffectId == 4011)
            return false;

        if (def.effectType == StatusEffectType.DamageTakenPerHit)
            return false;

        return true;
    }

    private void ApplySkillStatusConsumeEffects(
    SkillDefinitionSO skill,
    CharacterManager target)
    {
        if (skill == null || target == null)
            return;

        if (!skill.HasStatusConsumeEffects())
            return;

        foreach (StatusConsumeEffectData effect in skill.statusConsumeEffects)
        {
            StatusEffectProcessor.ApplyStatusConsumeEffect(
                characterManager,
                target,
                effect);
        }
    }

    private bool HasCounterStyleAdvantage(
    SkillStyle counterStyle,
    SkillStyle attackStyle)
    {
        return
            counterStyle == SkillStyle.Dexterity &&
            attackStyle == SkillStyle.Strength ||

            counterStyle == SkillStyle.Speed &&
            attackStyle == SkillStyle.Dexterity ||

            counterStyle == SkillStyle.Strength &&
            attackStyle == SkillStyle.Speed;
    }

    private void CancelProtectionAfterBreakthrough(
    CharacterManager originalTarget,
    CharacterManager protector)
    {
        if (originalTarget == null || protector == null)
            return;

        protector.combatHandler.counterSkillQueue.Clear();
        protector.combatHandler.RefreshCounterSkillQueueUI(protector);
        protector.UpdateCharacterUI();

        originalTarget.combatHandler.isDefenseTarget = false;
        protector.combatHandler.isDefenseCharacter = false;

        if (TurnManager.Instance.defenseTarget == originalTarget)
            TurnManager.Instance.defenseTarget = null;

        if (TurnManager.Instance.defenseCharacter == protector)
            TurnManager.Instance.defenseCharacter = null;

        Debug.Log($"{protector.character.Name}의 보호용 대응 큐가 돌파로 취소됨");
    }

    private void RegisterSkillRuntimeResult(
        SkillDefinitionSO skill,
        int finalDamage,
        CharacterManager target)
    {
        if (skill == null || characterManager.character.Skills == null)
            return;

        SkillRuntimeData runtime = characterManager.character.Skills
            .FirstOrDefault(s => s.skillUid == skill.uid);

        if (runtime == null)
            return;

        runtime.useCount += 1;
        runtime.damageCount += finalDamage;

        if (target != null && !target.character.IsAlive)
            runtime.killCount += 1;

        TryEvolveSkill(runtime, skill);
    }

    private void TryEvolveSkill(SkillRuntimeData runtime, SkillDefinitionSO skill)
    {
        if (runtime == null || skill == null)
            return;

        if (!skill.isEvolvableSkill)
            return;

        if (runtime.useCount < skill.evolRequiredUseCount)
            return;

        if (runtime.killCount < skill.evolRequiredKillCount)
            return;

        if (runtime.damageCount < skill.evolRequiredDamageCount)
            return;

        foreach (int traitId in skill.evolRequiredTraitIds)
        {
            if (!characterManager.character.HasTrait(traitId))
                return;
        }

        SkillDefinitionSO evolvedSkill = GameDataRegistry.Instance.GetSkill(skill.evolvedSkillUid);

        if (evolvedSkill == null)
            return;

        runtime.skillUid = evolvedSkill.uid;
        runtime.useCount = 0;
        runtime.killCount = 0;
        runtime.damageCount = 0;

        Debug.Log($"{skill.skillName} -> {evolvedSkill.skillName} 진화");
    }

    public void ResolveConcealForSkillQueue()
    {
        int concealedCount = skillQueue.Count(s => s != null && s.isConcealed);

        foreach (var queuedSkill in skillQueue)
        {
            ResolveConceal(queuedSkill, concealedCount);
        }
    }

    private void ResolveConceal(SkillQueueData queuedSkill, int concealedCount)
    {
        if (queuedSkill == null)
            return;

        if (!queuedSkill.isConcealed)
            return;

        if (queuedSkill.concealResolved)
            return;

        List<CharacterManager> defenders = GameManager.Instance.GetAllCharacters()
            .Where(c => c.character.IsMine != characterManager.character.IsMine && c.character.IsAlive)
            .ToList();

        List<CharacterData> defenderDataList = defenders.Select(c => c.character).ToList();

        CharacterData perceiver = SkillConcealUtility.GetBestPerceiver(defenderDataList);

        if (perceiver == null)
        {
            queuedSkill.revealLevel = RevealLevel.None;
            queuedSkill.concealResolved = true;
            return;
        }

        int chance = SkillConcealUtility.GetRevealChance(
            perceiver,
            characterManager.character,
            queuedSkill.skill,
            concealedCount);

        int roll = Random.Range(1, 101);

        queuedSkill.revealLevel = SkillConcealUtility.GetRevealLevelByRoll(chance, roll);
        queuedSkill.concealResolved = true;

        Debug.Log($"{queuedSkill.skill.skillName} 은폐 간파 판정: {queuedSkill.revealLevel} / 확률 {chance}, 굴림 {roll}");
    }

    public void AutoAssignDefaultCounterSkills()
    {
        foreach (var skillTargetPair in skillQueue)
        {
            CharacterManager target = skillTargetPair.target;

            SkillDefinitionSO defaultCounterSkill = target.combatHandler.GetDefaultCounterSkillDefinition();

            if (defaultCounterSkill == null)
                continue;

            SkillQueueData data = new SkillQueueData(
                defaultCounterSkill,
                target,
                target,
                false,
                target.combatHandler.counterSkillQueue.Count + 1);

            target.combatHandler.counterSkillQueue.Add(data);
            target.combatHandler.RefreshCounterSkillQueueUI(target);
            target.UpdateCharacterUI();

            Debug.Log($"{target.character.Name} - {defaultCounterSkill.skillName} 자동 대응 스킬 등록");
        }
    }

    private SkillDefinitionSO GetDefaultCounterSkillDefinition()
    {
        if (characterManager == null || characterManager.character == null)
            return null;

        if (characterManager.character.DefaultCounterSkill <= 0)
            return null;

        return GameDataRegistry.Instance.GetSkill(characterManager.character.DefaultCounterSkill);
    }

    private void UpdateSynergies()
    {
        // TODO:
        // SynergyManager가 SkillBase 기준이면 여기서 임시 비활성화.
        // 이후 SynergyManager를 SkillDefinitionSO 기준으로 바꾼 뒤 연결.
    }

    private void EndSynergyEffects() // 시너지 이펙트 구현 보류로 임시 비활성화   
    {
        /*
        foreach (var effect in activeSynergyEffects)
            effect.OnExpire(characterManager);

        activeSynergyEffects.Clear();*/
    }

    public IEnumerator HandleAITurn(System.Action onTurnEnd)
    {
        yield return new WaitForSeconds(2.0f);

        SkillDefinitionSO selectedSkill = AIChooseSkill();
        CharacterManager selectedTarget = FindTargetForAI();

        if (selectedSkill != null && selectedTarget != null)
            SelectSkill(selectedSkill, selectedTarget, false);

        yield return new WaitForSeconds(2.0f);
        onTurnEnd();
    }

    private SkillDefinitionSO AIChooseSkill()
    {
        List<SkillDefinitionSO> availableSkills = GetAvailableSkillDefinitions()
            .Where(skill =>
                !skill.isCounterSkill &&
                characterManager.character.CurrentStamina >= skill.staminaCost &&
                characterManager.character.CurrentMentality >= skill.mentalCost)
            .ToList();

        if (availableSkills.Count == 0)
        {
            Debug.Log("사용 가능한 스킬이 없습니다.");
            return null;
        }

        float personalityDeviationChance = 0.15f;
        bool deviateFromPersonality = Random.value < personalityDeviationChance;

        SkillDefinitionSO selectedSkill = null;

        switch (characterManager.character.personality)
        {
            case Personality.Simple:
                selectedSkill = availableSkills
                    .OrderBy(skill => skill.staminaCost + skill.mentalCost)
                    .FirstOrDefault();

                if (deviateFromPersonality)
                {
                    selectedSkill = availableSkills
                        .OrderByDescending(skill => skill.GetTotalDamageMultiplier())
                        .FirstOrDefault();
                }

                break;

            case Personality.Aggressive:
                selectedSkill = availableSkills
                    .OrderByDescending(skill => skill.GetTotalDamageMultiplier())
                    .FirstOrDefault();

                if (deviateFromPersonality)
                {
                    selectedSkill = availableSkills
                        .OrderBy(skill => skill.staminaCost + skill.mentalCost)
                        .FirstOrDefault();
                }

                break;

            case Personality.Cunning:
                selectedSkill = availableSkills
                    .FirstOrDefault(skill => skill.HasStatusEffects());

                if (selectedSkill == null)
                {
                    selectedSkill = availableSkills
                        .OrderByDescending(skill => skill.GetTotalDamageMultiplier())
                        .FirstOrDefault();
                }

                if (deviateFromPersonality)
                {
                    selectedSkill = availableSkills
                        .OrderBy(skill => skill.staminaCost)
                        .FirstOrDefault();
                }

                break;

            case Personality.Cautious:
                selectedSkill = availableSkills
                    .OrderBy(skill => skill.staminaCost + skill.mentalCost)
                    .FirstOrDefault();

                if (deviateFromPersonality)
                {
                    selectedSkill = availableSkills
                        .OrderByDescending(skill => skill.GetTotalDamageMultiplier())
                        .FirstOrDefault();
                }

                break;

            default:
                selectedSkill = availableSkills[0];
                break;
        }

        return selectedSkill;
    }

    private List<SkillDefinitionSO> GetAvailableSkillDefinitions()
    {
        List<SkillDefinitionSO> result = new List<SkillDefinitionSO>();

        if (characterManager.character.Skills == null)
            return result;

        foreach (SkillRuntimeData runtime in characterManager.character.Skills)
        {
            SkillDefinitionSO def = GameDataRegistry.Instance.GetSkill(runtime.skillUid);

            if (def == null)
                continue;

            if (!runtime.canUse)
                continue;

            result.Add(def);
        }

        return result;
    }

    private CharacterManager FindTargetForAI()
    {
        CharacterManager selectedTarget = null;

        List<CharacterManager> allCharacters = GameManager.Instance.GetAllCharacters();

        List<CharacterManager> enemies = allCharacters
            .Where(character =>
                character.character.IsMine != characterManager.character.IsMine &&
                character.character.IsAlive)
            .ToList();

        if (enemies.Count == 0)
        {
            Debug.Log("공격할 대상이 없습니다.");
            return null;
        }

        float personalityDeviationChance = 0.15f;
        bool deviateFromPersonality = Random.value < personalityDeviationChance;

        switch (characterManager.character.personality)
        {
            case Personality.Simple:
                selectedTarget = enemies.FirstOrDefault();
                if (deviateFromPersonality)
                    selectedTarget = enemies.OrderBy(enemy => enemy.character.CurrentHp).FirstOrDefault();
                break;

            case Personality.Aggressive:
                selectedTarget = enemies.OrderBy(enemy => enemy.character.CurrentHp).FirstOrDefault();
                if (deviateFromPersonality)
                    selectedTarget = enemies.FirstOrDefault();
                break;

            case Personality.Cunning:
                selectedTarget = enemies.OrderBy(enemy => enemy.character.CurrentHp).FirstOrDefault();
                if (deviateFromPersonality)
                    selectedTarget = enemies.OrderByDescending(enemy => enemy.character.FinalStats.PhysicalDefense).FirstOrDefault();
                break;

            case Personality.Cautious:
                selectedTarget = enemies
                    .Where(enemy => enemy.character.StatusEffects != null && enemy.character.StatusEffects.Count > 0)
                    .OrderBy(enemy => enemy.character.CurrentHp)
                    .FirstOrDefault();

                if (selectedTarget == null || deviateFromPersonality)
                    selectedTarget = enemies.OrderBy(enemy => enemy.character.CurrentHp).FirstOrDefault();
                break;

            default:
                selectedTarget = enemies[0];
                break;
        }

        return selectedTarget;
    }

    private SkillDefinitionSO AIChooseCounterSkill(SkillDefinitionSO incomingSkill)
    {
        return GetAvailableSkillDefinitions()
            .FirstOrDefault(skill =>
                skill.isCounterSkill &&
                characterManager.character.CurrentStamina >= skill.staminaCost &&
                characterManager.character.CurrentMentality >= skill.mentalCost);
    }

    private void RefreshSkillQueueUI(CharacterManager target)
    {
        if (target == null || target.characterUIHandler == null)
            return;

        target.characterUIHandler.UpdateSkillQueueUI(skillQueue, characterManager);
    }

    private void RefreshCounterSkillQueueUI(CharacterManager target)
    {
        if (target != null &&
            target != characterManager &&
            target.characterUIHandler != null)
        {
            target.characterUIHandler.ClearCounterSkillQueueUI();
        }

        if (characterManager == null || characterManager.characterUIHandler == null)
            return;

        characterManager.characterUIHandler.UpdateCounterSkillQueueUI(
            counterSkillQueue,
            characterManager);
    }

    public List<SkillQueueData> GetSkillQueue()
    {
        return skillQueue;
    }

    public List<SkillQueueData> GetCounterSkillQueue()
    {
        return counterSkillQueue;
    }
}
