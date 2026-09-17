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
    public bool LastProtectionAttempted { get; private set; }
    private readonly Dictionary<CharacterManager, SkillDefinitionSO> lastSuccessfulCounters = new();
    public IReadOnlyDictionary<CharacterManager, SkillDefinitionSO> LastSuccessfulCounters => lastSuccessfulCounters;
    private SkillQueueData preparedProtectionSkill;
    private CharacterManager preparedProtectionCharacter;
    private CounterResolveData preparedProtectionCounter;
    private CharacterManager preparedTargetCounterCharacter;
    private CounterResolveData preparedTargetCounter;
    private bool preparedTargetCounterAttempted;
    private readonly Dictionary<CharacterManager, List<StatusEffectRuntimeData>> statusAttackTargets = new();

    public void Awake()
    {
        characterManager = GetComponent<CharacterManager>();

        skillQueue = new List<SkillQueueData>();
        counterSkillQueue = new List<SkillQueueData>();
    }

    public void StartTurn(System.Action onTurnEnd)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.IsBattleReplica) return;
        characterManager.isPlayerTurn = true;
        bool useTurnTimer =
            TurnManager.Instance != null &&
            TurnManager.Instance.IsTurnTimeLimitEnabled &&
            !(MultiplayerSession.Instance != null && MultiplayerSession.Instance.IsSharedBattle);

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
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.RouteBattleCommand(
            "attack", characterManager, skill, target, concealed: isConcealed)) return;
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

        UIManager.Instance?.PlayBattleTargetConfirmSound(!characterManager.character.IsMine);

        RefreshAutomaticCounterSkills();
        RefreshSkillQueueUI(target);
        characterManager.UpdateCharacterUI();
        target.UpdateCharacterUI();
        UIManager.Instance.UpdateSkillTransparency(characterManager);
        UIManager.Instance.characterTargeting.RefreshConfirmedTargetLines();

        UpdateSynergies();
    }

    public void RemoveSkillFromQueue(int index)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.RouteBattleCommand(
            "removeAttack", characterManager, index: index)) return;
        if (index < 0 || index >= skillQueue.Count)
            return;

        SkillQueueData entry = skillQueue[index];

        RefundResources(entry.skill, entry.isConcealed, 1f);

        CharacterManager target = entry.target;
        skillQueue.RemoveAt(index);

        RefreshAutomaticCounterSkills();
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

    public bool ReplaceSkillInQueue(int index, SkillDefinitionSO newSkill, bool isConcealed = false)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.RouteBattleCommand(
            "replaceAttack", characterManager, newSkill, index: index, concealed: isConcealed)) return true;
        if (index < 0 ||
            index >= skillQueue.Count ||
            newSkill == null ||
            newSkill.isCounterSkill)
        {
            return false;
        }

        SkillQueueData oldEntry = skillQueue[index];
        CharacterManager target = oldEntry != null ? oldEntry.target : null;

        if (oldEntry == null || target == null)
            return false;

        bool hasOtherMeleeSkill = skillQueue
            .Where((entry, queueIndex) => queueIndex != index)
            .Any(entry =>
                entry != null &&
                entry.skill != null &&
                !entry.skill.isRangedSkill);

        if (newSkill.isRangedSkill && hasOtherMeleeSkill)
        {
            Debug.Log("경합 상태에서는 원거리 스킬을 사용할 수 없습니다.");
            return false;
        }

        if (characterManager.isInMeleeCombat &&
            !newSkill.isRangedSkill &&
            characterManager.meleeTarget != null &&
            characterManager.meleeTarget != target)
        {
            Debug.Log("경합 상태에서는 확정된 상대에게만 근접 스킬을 사용할 수 있습니다.");
            return false;
        }

        if (oldEntry.resourcesConsumed)
            RefundResources(oldEntry.skill, oldEntry.isConcealed, 1f);

        if (!ConsumeResources(newSkill, isConcealed))
        {
            if (oldEntry.resourcesConsumed)
                ConsumeResources(oldEntry.skill, oldEntry.isConcealed);

            return false;
        }

        SkillQueueData replacement = new SkillQueueData(
            newSkill,
            characterManager,
            target,
            isConcealed,
            oldEntry.order,
            true);
        skillQueue[index] = replacement;

        SkillQueueData firstMeleeSkill = skillQueue.FirstOrDefault(entry =>
            entry != null && entry.skill != null && !entry.skill.isRangedSkill);
        characterManager.isInMeleeCombat = firstMeleeSkill != null;
        characterManager.meleeTarget = firstMeleeSkill != null
            ? firstMeleeSkill.target
            : null;

        RefreshAutomaticCounterSkills();
        RefreshSkillQueueUI(target);
        characterManager.UpdateCharacterUI();
        target.UpdateCharacterUI();
        UIManager.Instance.UpdateSkillTransparency(characterManager);
        UIManager.Instance.characterTargeting.RefreshConfirmedTargetLines();
        UpdateSynergies();

        Debug.Log($"{oldEntry.skill.skillName} 공격 큐를 {newSkill.skillName}(으)로 교체");
        return true;
    }

    public void SelectCounterSkill(SkillDefinitionSO skill, CharacterManager target)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.RouteBattleCommand(
            "appendCounter", characterManager, skill, target)) return;
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
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.RouteBattleCommand(
            "clearCounter", characterManager, index: index)) return;
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

    public void ClearCounterSkillAt(int idx)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.RouteBattleCommand(
            "clearCounter", characterManager, index: idx)) return;
        if (idx < 0 || idx >= counterSkillQueue.Count)
            return;

        SkillQueueData oldEntry = counterSkillQueue[idx];

        if (oldEntry == null)
            return;

        if (oldEntry.resourcesConsumed && oldEntry.skill != null)
            RefundResources(oldEntry.skill, false, 1f);

        SkillQueueData replacement = new SkillQueueData(
            null,
            characterManager,
            characterManager,
            false,
            idx + 1);
        replacement.incomingSkill = oldEntry.incomingSkill;
        replacement.protectedTarget = oldEntry.protectedTarget;
        replacement.revealLevel = oldEntry.revealLevel;
        counterSkillQueue[idx] = replacement;

        RefreshCounterSkillQueueUI(characterManager);
        characterManager.UpdateCharacterUI();
        UIManager.Instance.UpdateSkillTransparency(characterManager);
        UIManager.Instance.characterTargeting.RefreshConfirmedTargetLines();

        TurnManager turnManager = TurnManager.Instance;

        if (turnManager != null)
        {
            UIManager.Instance.UpdateCounterSkillPanel(
                turnManager.defenseCharacter,
                turnManager.defenseTarget);
        }
    }

    public void SetOrResetCounterSkill(int idx, SkillDefinitionSO newSkill)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.RouteBattleCommand(
            "counter", characterManager, newSkill, index: idx)) return;
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

        SkillQueueData replacement = new SkillQueueData(
            finalSkill,
            characterManager,
            characterManager,
            false,
            idx + 1,
            consumeImmediately);
        replacement.incomingSkill = oldEntry.incomingSkill;
        replacement.protectedTarget = oldEntry.protectedTarget;
        replacement.revealLevel = oldEntry.revealLevel;
        counterSkillQueue[idx] = replacement;

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

            if (MultiplayerSession.Instance?.IsFriendlyMatch == true &&
                (item.target == null || !item.target.character.IsAlive))
            {
                RefundResources(item.skill, item.isConcealed, 0.5f);
                skillQueue.RemoveAt(0);
                DiscardCounterForCancelledAttack(item);
                if (item.target != null) RefreshSkillQueueUI(item.target);
                characterManager.UpdateCharacterUI();
                continue;
            }

            if (cancelNextSkill)
            {
                cancelNextSkill = false;

                Debug.Log($"{item.skill.skillName} 스킬이 패링 대성공 효과로 취소됨");

                skillQueue.RemoveAt(0);
                DiscardCounterForCancelledAttack(item);
                RefreshSkillQueueUI(item.target);
                characterManager.UpdateCharacterUI();
                item.target?.UpdateCharacterUI();

                MultiplayerSession.Instance?.PublishBattleState(MultiplayerSession.SharedBattlePhase.Resolving);

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
                characterManager.battlePresentationHandler?.PlaySkill(
                    item.skill,
                    item.target != null ? item.target.transform : null);
                UseSkill(item);

                foreach (var counter in lastSuccessfulCounters)
                {
                    if (counter.Key.character != null && counter.Key.character.IsAlive)
                    {
                        counter.Key.battlePresentationHandler?.FaceTarget(characterManager.transform);
                        counter.Key.battlePresentationHandler?.PlaySkill(
                            counter.Value,
                            characterManager.transform);
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

            if (MultiplayerSession.Instance != null)
                yield return MultiplayerSession.Instance.WaitForBattlePresentation();

            if (MultiplayerSession.Instance?.IsFriendlyMatch == true)
            {
                if (TurnManager.Instance.CheckBattleEnd()) yield break;
                if (!characterManager.character.IsAlive)
                {
                    CancelRemainingSkills();
                    onTurnEnd?.Invoke();
                    yield break;
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

    private void DiscardCounterForCancelledAttack(SkillQueueData cancelledAttack)
    {
        if (cancelledAttack == null || cancelledAttack.target == null)
            return;

        CharacterManager counterUser = cancelledAttack.target;
        TurnManager turnManager = TurnManager.Instance;

        if (turnManager != null &&
            turnManager.defenseCharacter != null &&
            turnManager.defenseTarget == cancelledAttack.target)
        {
            counterUser = turnManager.defenseCharacter;
        }

        if (counterUser.combatHandler == null)
            return;

        List<SkillQueueData> queue = counterUser.combatHandler.counterSkillQueue;
        int counterIndex = queue.FindIndex(entry =>
            entry != null &&
            entry.incomingSkill == cancelledAttack.skill &&
            (entry.protectedTarget ?? entry.target) == cancelledAttack.target);

        if (counterIndex < 0)
            return;

        SkillQueueData counterEntry = queue[counterIndex];

        if (counterEntry.resourcesConsumed && counterEntry.skill != null)
            counterUser.combatHandler.RefundResources(counterEntry.skill, false, 1f);

        queue.RemoveAt(counterIndex);
        counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
        counterUser.UpdateCharacterUI();
    }

    public void SetDefenseCharacter(CharacterManager defenseCharacter)
    {
        Debug.Log($"{defenseCharacter.character.Name} - Set Defense Character");

        defenseCharacter.combatHandler.isDefenseCharacter = true;

        UIManager.Instance.characterTargeting.SelectCharacter(defenseCharacter);
    }

    public void SetDefenseTarget(CharacterManager target)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.RouteBattleCommand(
            "protect", characterManager, target: target)) return;
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

        counterSkillQueue.Clear();

        if (attacker != null)
        {
            foreach (SkillQueueData incomingAttack in attacker.GetSkillQueue())
            {
                if (incomingAttack == null ||
                    (incomingAttack.target != characterManager && incomingAttack.target != target))
                {
                    continue;
                }

                SkillDefinitionSO automaticCounter = GetAutomaticCounterSkill(
                    incomingAttack.skill,
                    incomingAttack.target);
                SkillQueueData counterData = new SkillQueueData(
                    automaticCounter,
                    characterManager,
                    characterManager,
                    false,
                    counterSkillQueue.Count + 1);
                counterData.incomingSkill = incomingAttack.skill;
                counterData.protectedTarget = incomingAttack.target;
                counterData.revealLevel = incomingAttack.revealLevel;
                counterSkillQueue.Add(counterData);
            }
        }

        RefreshCounterSkillQueueUI(characterManager);
        target.characterUIHandler?.UpdateSkillQueueUI(
            attacker != null ? attacker.GetSkillQueue() : null,
            attacker);

        Debug.Log($"방어자 : {characterManager.character.Name} - 방어대상 : {target.character.Name}");

        if (TurnManager.Instance.defenseCharacter != null &&
            TurnManager.Instance.defenseTarget != null)
        {
            UIManager.Instance.UpdateCounterSkillPanel(
                TurnManager.Instance.defenseCharacter,
                TurnManager.Instance.defenseTarget);
        }
    }

    public void ApplySharedPresentation(MultiplayerSession.BattlePresentationResult result)
    {
        var session = MultiplayerSession.Instance;
        LastResolvedTarget = session.FindBattleCharacter(result.resolvedTarget);
        LastSkillWasCancelled = result.cancelled;
        LastProtectionSucceeded = result.protectionSucceeded;
        LastProtectionAttempted = result.protectionAttempted;
        lastSuccessfulCounters.Clear();
        foreach (var pair in result.counters)
            lastSuccessfulCounters.Add(session.FindBattleCharacter(pair.Key), GameDataRegistry.Instance.GetSkill(pair.Value));
    }

    public void PrepareProtectionForPresentation(SkillQueueData queuedSkill)
    {
        statusAttackTargets.Clear();
        preparedProtectionSkill = null;
        preparedProtectionCharacter = null;
        preparedProtectionCounter = null;
        preparedTargetCounterCharacter = null;
        preparedTargetCounter = null;
        preparedTargetCounterAttempted = false;
        LastResolvedTarget = null;
        LastSkillWasCancelled = false;
        LastProtectionSucceeded = false;
        LastProtectionAttempted = false;
        lastSuccessfulCounters.Clear();

        if (queuedSkill == null || queuedSkill.skill == null || queuedSkill.target == null)
            return;

        preparedProtectionSkill = queuedSkill;

        SkillDefinitionSO attackSkill = queuedSkill.skill;
        CharacterManager originalTarget = queuedSkill.target;
        LastResolvedTarget = originalTarget;

        CharacterManager defenseCharacter = null;

        if (originalTarget.combatHandler != null &&
            originalTarget.combatHandler.isDefenseTarget &&
            TurnManager.Instance != null)
        {
            defenseCharacter = TurnManager.Instance.defenseCharacter;
        }

        if (defenseCharacter != null)
        {
            preparedProtectionCharacter = defenseCharacter;
            preparedProtectionCounter = TryUseCounterSkill(
                attackSkill,
                defenseCharacter,
                characterManager,
                originalTarget,
                showGreatSuccessResult: false,
                refreshQueueUI: false);

            LastProtectionAttempted = preparedProtectionCounter != null;

            if (preparedProtectionCounter != null)
            {
                LastProtectionSucceeded = preparedProtectionCounter.IsSuccess;

                if (preparedProtectionCounter.cancelCurrentSkill)
                {
                    LastResolvedTarget = null;
                    LastSkillWasCancelled = true;
                    return;
                }

                if (preparedProtectionCounter.IsSuccess ||
                    (attackSkill.HasAttackEffect(AttackEffectType.Breakthrough) &&
                     defenseCharacter.isInMeleeCombat && defenseCharacter.meleeTarget == characterManager))
                {
                    LastResolvedTarget = preparedProtectionCounter.counterSkill != null &&
                                         preparedProtectionCounter.counterSkill.isRangedSkill
                        ? originalTarget
                        : defenseCharacter;
                    return;
                }
            }
        }

        if (originalTarget.combatHandler == null ||
            originalTarget.combatHandler.counterSkillQueue.Count == 0)
        {
            return;
        }

        preparedTargetCounterCharacter = originalTarget;
        preparedTargetCounterAttempted = true;
        preparedTargetCounter = TryUseCounterSkill(
            attackSkill,
            originalTarget,
            characterManager,
            originalTarget,
            showGreatSuccessResult: false,
            refreshQueueUI: false);

        if (preparedTargetCounter != null &&
            preparedTargetCounter.cancelCurrentSkill)
        {
            LastResolvedTarget = null;
            LastSkillWasCancelled = true;
        }
    }

    public void UseSkill(SkillQueueData queuedSkill)
    {
        bool hasPreparedCounters =
            queuedSkill != null &&
            ReferenceEquals(preparedProtectionSkill, queuedSkill);
        CharacterManager cachedProtectionCharacter = preparedProtectionCharacter;
        CounterResolveData cachedProtectionCounter = preparedProtectionCounter;
        CharacterManager cachedTargetCounterCharacter = preparedTargetCounterCharacter;
        CounterResolveData cachedTargetCounter = preparedTargetCounter;
        bool cachedTargetCounterAttempted = preparedTargetCounterAttempted;

        preparedProtectionSkill = null;
        preparedProtectionCharacter = null;
        preparedProtectionCounter = null;
        preparedTargetCounterCharacter = null;
        preparedTargetCounter = null;
        preparedTargetCounterAttempted = false;

        LastResolvedTarget = null;
        LastSkillWasCancelled = false;
        LastProtectionSucceeded = hasPreparedCounters &&
            cachedProtectionCounter != null &&
            cachedProtectionCounter.IsSuccess;

        if (!hasPreparedCounters)
        {
            statusAttackTargets.Clear();
            LastProtectionAttempted = false;
            lastSuccessfulCounters.Clear();
        }

        if (queuedSkill == null || queuedSkill.skill == null || queuedSkill.target == null)
            return;

        SkillDefinitionSO attackSkill = queuedSkill.skill;
        CharacterManager originalTarget = queuedSkill.target;
        CharacterManager actualTarget = originalTarget;
        LastResolvedTarget = originalTarget;

        skillQueue.RemoveAt(0);
        RefreshSkillQueueUI(originalTarget);

        if (hasPreparedCounters && cachedProtectionCharacter != null)
        {
            cachedProtectionCharacter.combatHandler.RefreshCounterSkillQueueUI(
                cachedProtectionCharacter);
            cachedProtectionCharacter.UpdateCharacterUI();
        }

        if (hasPreparedCounters &&
            cachedTargetCounterCharacter != null &&
            cachedTargetCounterCharacter != cachedProtectionCharacter)
        {
            cachedTargetCounterCharacter.combatHandler.RefreshCounterSkillQueueUI(
                cachedTargetCounterCharacter);
            cachedTargetCounterCharacter.UpdateCharacterUI();
        }

        ResolveConceal(queuedSkill, skillQueue.Count(s => s != null && s.isConcealed));

        CounterResolveData defenseCounter = hasPreparedCounters
            ? cachedProtectionCounter
            : null;
        CounterResolveData appliedCounter = null;

        if (cachedProtectionCharacter != null || originalTarget.combatHandler.isDefenseTarget)
        {
            CharacterManager defenseCharacter = hasPreparedCounters
                ? cachedProtectionCharacter
                : TurnManager.Instance.defenseCharacter;

            if (defenseCharacter != null)
            {
                if (!hasPreparedCounters)
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

                    if (hasPreparedCounters)
                        ShowCounterGreatSuccessResult(defenseCounter, defenseCharacter);

                    if (defenseCounter.cancelCurrentSkill)
                    {
                        LastResolvedTarget = null;
                        LastSkillWasCancelled = true;
                        Debug.Log($"{defenseCharacter.character.Name}의 파훼 대성공. {attackSkill.skillName} 무효화");
                        CompleteStatusAttack(attackSkill);
                        EndSynergyEffects();
                        return;
                    }

                    if (defenseCounter.cancelNextSkill)
                        cancelNextSkill = true;

                    bool isRangedDefenseCounter = defenseCounter.counterSkill != null &&
                                                  defenseCounter.counterSkill.isRangedSkill;

                    if (attackSkill.HasAttackEffect(AttackEffectType.Breakthrough) &&
                        !isRangedDefenseCounter &&
                        (defenseCounter.IsSuccess ||
                         (defenseCharacter.isInMeleeCombat && defenseCharacter.meleeTarget == characterManager)))
                    {
                        LastResolvedTarget = defenseCharacter;
                        ResolveBreakthroughProtection(
                            attackSkill,
                            originalTarget,
                            defenseCharacter,
                            defenseCounter);

                        CompleteStatusAttack(attackSkill);
                        EndSynergyEffects();
                        return;
                    }

                    if (defenseCounter.IsSuccess)
                    {
                        if (isRangedDefenseCounter)
                        {
                            actualTarget = originalTarget;
                        }
                        else
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
        }

        CounterResolveData targetCounter = null;

        bool protectionAlreadyCountered =
            defenseCounter != null &&
            defenseCounter.IsSuccess;

        bool usePreparedTargetCounter =
            hasPreparedCounters &&
            cachedTargetCounterAttempted &&
            cachedTargetCounterCharacter == actualTarget;

        if (!protectionAlreadyCountered &&
            (usePreparedTargetCounter ||
             actualTarget.combatHandler.counterSkillQueue.Count > 0))
        {
            targetCounter = usePreparedTargetCounter
                ? cachedTargetCounter
                : TryUseCounterSkill(
                    attackSkill,
                    actualTarget,
                    characterManager,
                    actualTarget);

            if (targetCounter != null)
            {
                appliedCounter = targetCounter;

                if (usePreparedTargetCounter)
                    ShowCounterGreatSuccessResult(targetCounter, actualTarget);

                if (targetCounter.cancelCurrentSkill)
                {
                    LastResolvedTarget = null;
                    LastSkillWasCancelled = true;
                    Debug.Log($"{actualTarget.character.Name}의 파훼 대성공. {attackSkill.skillName} 무효화");
                    CompleteStatusAttack(attackSkill);
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

        CompleteStatusAttack(attackSkill);
        EndSynergyEffects();
    }

    private void TrackStatusAttackTarget(SkillDefinitionSO skill, CharacterManager target)
    {
        if (skill == null || skill.isCounterSkill || target == null || target == characterManager ||
            target.character == null || !target.character.IsAlive ||
            target.character.IsMine == characterManager.character.IsMine || statusAttackTargets.ContainsKey(target))
            return;
        statusAttackTargets[target] = target.character.StatusEffects?.Where(s => s != null).ToList()
            ?? new List<StatusEffectRuntimeData>();
    }

    private void CompleteStatusAttack(SkillDefinitionSO skill)
    {
        foreach (var entry in statusAttackTargets)
        {
            if (entry.Key == null)
                continue;
            StatusEffectProcessor.CompleteIncomingAttack(entry.Key.character, skill, entry.Value);
            entry.Key.UpdateCharacterUI();
        }
        statusAttackTargets.Clear();
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
    bool showGreatSuccessResult = true,
    bool refreshQueueUI = true)
    {
        TrackStatusAttackTarget(attackSkill, counterUser);
        if (counterUser == null || counterUser.combatHandler.counterSkillQueue.Count == 0)
            return null;

        SkillQueueData counterData = counterUser.combatHandler.counterSkillQueue[0];
        counterUser.combatHandler.counterSkillQueue.RemoveAt(0);

        SkillDefinitionSO counterSkill = counterData.skill;

        if (counterSkill == null || counterUser.character.HasTraitFlag(TraitSpecialFlag.CannotCounter))
        {
            if (refreshQueueUI)
            {
                counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
                counterUser.UpdateCharacterUI();
            }

            return null;
        }

        // 자동으로 배정된 기본 대응은 비용을 소모하지 않는다.
        // 플레이어가 수동으로 교체한 대응은 큐 등록 시 이미 비용을 지불한다.

        bool isProtectingOther = counterUser != protectedTarget;

        CounterResolveData resolveData = new CounterResolveData
        {
            result = CounterResult.Fail,
            counterSkill = counterSkill,
            counterUser = counterUser,
            damageMultiplier = 1f,
            effectScale = 1f
        };

        if (CanUseCounterAgainst(attackSkill, counterSkill, isProtectingOther) &&
            StatusEffectProcessor.ConsumeCancelNextCounterStatus(counterUser.character, counterSkill))
        {
            Debug.Log($"{counterUser.character.Name}의 대응이 상태이상으로 취소됨");

            if (refreshQueueUI)
            {
                counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
                counterUser.UpdateCharacterUI();
            }

            return resolveData;
        }

        if (!CanUseCounterAgainst(attackSkill, counterSkill, isProtectingOther))
        {
            Debug.Log($"{counterUser.character.Name}의 {counterSkill.skillName} 대응 불가");

            if (refreshQueueUI)
            {
                counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
                counterUser.UpdateCharacterUI();
            }

            return resolveData;
        }

        int chance = counterUser.combatHandler.GetCounterSuccessChancePreview(
            attackSkill,
            attacker,
            protectedTarget,
            counterSkill,
            counterData.revealLevel);

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

        CharacterManager counterEffectTarget =
            counterSkill.isRangedSkill && protectedTarget != null
                ? protectedTarget
                : counterUser;

        switch (result)
        {
            case CounterResult.Fail:
                resolveData.damageMultiplier = 1f;

                ApplyCounterFailureMinimumArmor(
                    counterUser,
                    counterEffectTarget,
                    counterSkill,
                    successEffectScale);

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
                else if (counterSkill.counterActionType == CounterActionType.Break)
                {
                    float reductionRate = Mathf.Clamp01(
                        counterSkill.successArmorAttackMultiplier * successEffectScale);
                    resolveData.damageMultiplier = 1f - reductionRate;
                }
                else
                {
                    resolveData.damageMultiplier = 1f;
                    ApplyCounterSuccess(
                        counterUser,
                        counterEffectTarget,
                        counterSkill,
                        successEffectScale);
                }

                Debug.Log(
                    $"{counterUser.character.Name} {counterSkill.skillName} 대응 성공 " +
                    $"성공률:{chance}% 대성공률:{greatSuccessChance}% 위력비:{counterPowerRatio:F2}");
                break;

            case CounterResult.GreatSuccess:
                ApplyCounterGreatSuccess(resolveData, counterUser, counterSkill, attackSkill);
                if (counterSkill.counterActionType == CounterActionType.Evade &&
                    statusAttackTargets.TryGetValue(counterUser, out var statusesBeforeEvade))
                    statusesBeforeEvade.Clear();

                if (showGreatSuccessResult)
                    ShowCounterGreatSuccessResult(resolveData, counterUser);

                Debug.Log(
                    $"{counterUser.character.Name} {counterSkill.skillName} 대응 대성공 " +
                    $"성공률:{chance}% 대성공률:{greatSuccessChance}% 위력비:{counterPowerRatio:F2}");
                break;
        }

        if (refreshQueueUI)
        {
            counterUser.combatHandler.RefreshCounterSkillQueueUI(counterUser);
            counterUser.UpdateCharacterUI();
        }

        return resolveData;
    }

    public int GetCounterSuccessChancePreview(
        SkillDefinitionSO attackSkill,
        CharacterManager attacker,
        CharacterManager protectedTarget,
        SkillDefinitionSO counterSkill,
        RevealLevel revealLevel = RevealLevel.None)
    {
        if (characterManager == null || characterManager.character == null ||
            attacker == null || attacker.character == null ||
            protectedTarget == null || counterSkill == null ||
            characterManager.character.HasTraitFlag(TraitSpecialFlag.CannotCounter))
        {
            return 0;
        }

        bool isProtectingOther = characterManager != protectedTarget;

        if (!SkillCounterCalculator.CanUseCounterAgainst(
            attackSkill,
            counterSkill,
            isProtectingOther))
        {
            return 0;
        }

        int chance = SkillCounterCalculator.GetCounterSuccessChance(
            attacker.character,
            characterManager.character,
            attackSkill,
            counterSkill,
            isProtectingOther);

        if (!counterSkill.isRangedSkill && isProtectingOther)
        {
            chance += FormationUtility.GetProtectionFormationBonus(
                characterManager.isFront ? FormationRow.Front : FormationRow.Back,
                protectedTarget.isFront ? FormationRow.Front : FormationRow.Back);
        }

        if (attackSkill != null && revealLevel == RevealLevel.Full)
            chance += SkillConcealUtility.FullRevealCounterBonus;

        if (counterSkill.isRangedSkill)
            chance -= 10;

        chance = SkillCounterCalculator.ApplyDefenseSkillSuccessRateBonus(
            characterManager.character,
            counterSkill,
            chance,
            isProtectingOther);

        chance = StatusEffectProcessor.ApplyCounterStatusPenalty(
            characterManager.character,
            counterSkill,
            chance);

        return Mathf.Clamp(chance, 5, 95);
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
        CharacterManager counterEffectTarget,
        SkillDefinitionSO counterSkill,
        float effectScale)
    {
        ApplyCounterArmor(
            counterUser,
            counterEffectTarget,
            counterSkill,
            effectScale,
            1f);
    }

    private void ApplyCounterFailureMinimumArmor(
        CharacterManager counterUser,
        CharacterManager counterEffectTarget,
        SkillDefinitionSO counterSkill,
        float effectScale)
    {
        ApplyCounterArmor(
            counterUser,
            counterEffectTarget,
            counterSkill,
            effectScale,
            0.33f);
    }

    private void ApplyCounterArmor(
        CharacterManager counterUser,
        CharacterManager counterEffectTarget,
        SkillDefinitionSO counterSkill,
        float effectScale,
        float resultScale)
    {
        if (counterUser == null || counterUser.character == null ||
            counterEffectTarget == null || counterEffectTarget.character == null ||
            counterSkill == null)
            return;

        if (counterSkill.counterActionType != CounterActionType.Guard &&
            counterSkill.counterActionType != CounterActionType.Parry)
            return;

        int basePower = GetCounterArmorBasePower(counterUser.character, counterSkill);

        int armorGain = Mathf.RoundToInt(
            basePower *
            effectScale *
            resultScale);

        if (armorGain <= 0 && counterSkill.successArmorAttackMultiplier > 0f)
            armorGain = 1;

        if (armorGain <= 0)
            return;

        if (counterSkill.type == SkillType.Magical)
            counterEffectTarget.character.MagicalArmor += armorGain;
        else
            counterEffectTarget.character.PhysicalArmor += armorGain;

        Debug.Log(
            $"{counterUser.character.Name} {counterSkill.skillName} 대응으로 " +
            $"{counterEffectTarget.character.Name} 방어도 획득: {armorGain}");
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

        TrackStatusAttackTarget(skill, target);

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
                bool wasAlive = target.character.IsAlive && target.character.CurrentHp > 0;
                if (!wasAlive) break;
                float damageMultiplier = component.damageMultiplier;
                damageMultiplier *= offHandMultiplier;
                damageMultiplier *= counterDamageMultiplier;
                damageMultiplier *= specialDamageMultiplier;

                int damage = CalculateComponentDamage(skill, component, damageMultiplier);

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
                characterManager.RecoverOnKill(target.character, wasAlive);

                Debug.Log(
                    $"{characterManager.character.Name} - {skill.skillName} 사용 -> {target.character.Name}, " +
                    $"{component.attribute} {damageType} 피해 {finalDamage}");
            }
        }

        RegisterSkillRuntimeResult(skill, totalFinalDamage, target);

        Debug.Log($"{characterManager.character.Name} - {skill.skillName} 총 피해 {totalFinalDamage}");
    }

    private int CalculateComponentDamage(SkillDefinitionSO skill, SkillDamageComponentData component, float damageMultiplier)
    {
        if (component == null || characterManager == null || characterManager.character == null)
            return 0;

        CharacterData caster = characterManager.character;

        if (caster.FinalStats == null)
            return 0;

        int affinityBonus = component.attribute switch
        {
            SkillAttribute.Fire => caster.FinalStats.FireAffinity,
            SkillAttribute.Ice => caster.FinalStats.IceAffinity,
            SkillAttribute.Lightning => caster.FinalStats.LightningAffinity,
            SkillAttribute.Slash => caster.FinalStats.SlashAffinity,
            SkillAttribute.Pierce => caster.FinalStats.PierceAffinity,
            SkillAttribute.Smash => caster.FinalStats.SmashAffinity,
            _ => 0
        };
        CharacterSpecialStats specialStats = caster.FinalSpecialStats;
        int skillSpecialization = skill == null || specialStats == null ? 0 : skill.discipline switch
        {
            SkillDiscipline.Basic => specialStats.BasicSpecialization,
            SkillDiscipline.WeaponArt => specialStats.WeaponArtSpecialization,
            SkillDiscipline.Swordsmanship => specialStats.SwordsmanshipSpecialization,
            SkillDiscipline.Archery => specialStats.ArcherySpecialization,
            SkillDiscipline.ShieldArt => specialStats.ShieldArtSpecialization,
            SkillDiscipline.MartialArt => specialStats.MartialArtSpecialization,
            SkillDiscipline.DaggerArt => specialStats.DaggerArtSpecialization,
            SkillDiscipline.Magic => specialStats.MagicSpecialization,
            SkillDiscipline.Monster => specialStats.MonsterSpecialization,
            _ => 0
        };
        damageMultiplier *= Mathf.Max(0f, 1f + (affinityBonus + skillSpecialization) / 100f);

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

                StatusEffectProcessor.ApplyStatus(target.character, effect, characterManager.character.ID, characterManager.character);
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

            if (target == null || target.combatHandler == null)
                continue;

            SkillDefinitionSO defaultCounterSkill =
                target.combatHandler.GetAutomaticCounterSkill(
                    skillTargetPair.skill,
                    target);

            SkillQueueData data = new SkillQueueData(
                defaultCounterSkill,
                target,
                target,
                false,
                target.combatHandler.counterSkillQueue.Count + 1);
            data.incomingSkill = skillTargetPair.skill;
            data.protectedTarget = target;
            data.revealLevel = skillTargetPair.revealLevel;

            target.combatHandler.counterSkillQueue.Add(data);
            target.combatHandler.RefreshCounterSkillQueueUI(target);
            target.UpdateCharacterUI();

            if (defaultCounterSkill != null)
                Debug.Log($"{target.character.Name} - {defaultCounterSkill.skillName} 자동 대응 스킬 등록");
        }
    }

    private void RefreshAutomaticCounterSkills()
    {
        if (GameManager.Instance == null)
            return;

        List<CharacterManager> allCharacters = GameManager.Instance.GetAllCharacters();

        if (allCharacters == null)
            return;

        foreach (CharacterManager character in allCharacters)
        {
            if (character == null || character.combatHandler == null)
                continue;

            character.combatHandler.counterSkillQueue.Clear();
            character.characterUIHandler?.ClearCounterSkillQueueUI();
        }

        AutoAssignDefaultCounterSkills();

        foreach (CharacterManager target in skillQueue
                     .Where(entry => entry != null && entry.target != null)
                     .Select(entry => entry.target)
                     .Distinct())
        {
            RefreshSkillQueueUI(target);
        }
    }

    public void CycleBasicCounterSkill(int idx)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.RouteBattleCommand(
            "cycle", characterManager, index: idx)) return;
        if (idx < 0 || idx >= counterSkillQueue.Count)
            return;

        SkillQueueData currentEntry = counterSkillQueue[idx];
        SkillDefinitionSO currentSkill = currentEntry.skill;
        CounterActionType currentType = currentSkill != null
            ? currentSkill.counterActionType
            : CounterActionType.Evade;
        CounterActionType[] cycleOrder =
        {
            CounterActionType.Guard,
            CounterActionType.Parry,
            CounterActionType.Evade
        };
        int currentTypeIndex = System.Array.IndexOf(cycleOrder, currentType);
        bool refundedCurrentSkill = currentEntry.resourcesConsumed;

        if (refundedCurrentSkill)
            RefundResources(currentSkill, false, 1f);

        for (int offset = 1; offset <= cycleOrder.Length; offset++)
        {
            CounterActionType nextType = cycleOrder[
                (Mathf.Max(0, currentTypeIndex) + offset) % cycleOrder.Length];
            SkillDefinitionSO nextSkill = GetBestCounterSkillForType(
                nextType,
                currentEntry.incomingSkill,
                currentEntry.protectedTarget ?? characterManager);

            if (nextSkill == null)
                continue;

            if (!ConsumeResources(nextSkill, false))
                continue;

            SkillQueueData replacement = new SkillQueueData(
                nextSkill,
                characterManager,
                characterManager,
                false,
                idx + 1,
                true);
            replacement.incomingSkill = currentEntry.incomingSkill;
            replacement.protectedTarget = currentEntry.protectedTarget;
            replacement.revealLevel = currentEntry.revealLevel;
            counterSkillQueue[idx] = replacement;

            RefreshCounterSkillQueueUI(characterManager);
            characterManager.UpdateCharacterUI();
            UIManager.Instance?.UpdateSkillTransparency(characterManager);

            if (TurnManager.Instance != null)
            {
                UIManager.Instance?.UpdateCounterSkillPanel(
                    TurnManager.Instance.defenseCharacter,
                    TurnManager.Instance.defenseTarget);
            }

            return;
        }

        if (refundedCurrentSkill)
            ConsumeResources(currentSkill, false);
    }

    private SkillDefinitionSO GetAutomaticCounterSkill(
        SkillDefinitionSO incomingSkill,
        CharacterManager protectedTarget)
    {
        bool isProtectingOther = protectedTarget != null && protectedTarget != characterManager;

        return GetAvailableSkillDefinitions()
            .Where(skill =>
                skill != null &&
                skill.isCounterSkill &&
                skill.CanBeUsedBy(characterManager.character) &&
                skill.discipline == SkillDiscipline.Basic && skill.GetTotalResourceCost() == 0 &&
                (incomingSkill == null || SkillCounterCalculator.CanUseCounterAgainst(
                     incomingSkill,
                     skill,
                     isProtectingOther)) &&
                skill.counterActionType != CounterActionType.Break)
            .OrderByDescending(skill => incomingSkill != null &&
                                        HasCounterStyleAdvantage(skill.style, incomingSkill.style))
            .ThenByDescending(GetCounterPerformance)
            .FirstOrDefault();
    }

    public SkillDefinitionSO GetAutomaticCounterSkillForProtection(
        SkillDefinitionSO incomingSkill,
        CharacterManager protectedTarget)
    {
        if (protectedTarget == null || protectedTarget == characterManager)
            return null;

        SkillDefinitionSO skill = GetAutomaticCounterSkill(incomingSkill, protectedTarget);

        if (skill == null ||
            characterManager.character.CurrentStamina < skill.staminaCost ||
            characterManager.character.CurrentMentality < skill.mentalCost)
        {
            return null;
        }

        return skill;
    }

    private SkillDefinitionSO GetBestCounterSkillForType(
        CounterActionType actionType,
        SkillDefinitionSO incomingSkill,
        CharacterManager protectedTarget)
    {
        bool isProtectingOther = protectedTarget != null && protectedTarget != characterManager;

        return GetAvailableSkillDefinitions()
            .Where(skill =>
                skill != null &&
                skill.isCounterSkill &&
                skill.CanBeUsedBy(characterManager.character) &&
                skill.discipline == SkillDiscipline.Basic && skill.GetTotalResourceCost() == 0 &&
                skill.counterActionType == actionType &&
                characterManager.character.CurrentStamina >= skill.staminaCost &&
                characterManager.character.CurrentMentality >= skill.mentalCost &&
                (incomingSkill == null || SkillCounterCalculator.CanUseCounterAgainst(
                    incomingSkill,
                    skill,
                    isProtectingOther)))
            .OrderByDescending(GetCounterPerformance)
            .FirstOrDefault();
    }

    private static float GetCounterPerformance(SkillDefinitionSO skill)
    {
        if (skill == null)
            return 0f;

        return skill.counterActionType == CounterActionType.Evade
            ? skill.minEvadeReductionRate
            : skill.successArmorAttackMultiplier;
    }

    private SkillDefinitionSO GetDefaultCounterSkillDefinition()
    {
        if (characterManager == null || characterManager.character == null)
            return null;

        if (characterManager.character.DefaultCounterSkill <= 0)
            return null;

        SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(characterManager.character.DefaultCounterSkill);
        if (skill != null && skill.isCounterSkill && skill.discipline == SkillDiscipline.Basic &&
            skill.GetTotalResourceCost() == 0 && skill.CanBeUsedBy(characterManager.character))
            return skill;

        return GetAvailableSkillDefinitions().FirstOrDefault(candidate =>
            candidate != null && candidate.isCounterSkill && candidate.discipline == SkillDiscipline.Basic &&
            candidate.GetTotalResourceCost() == 0 && candidate.CanBeUsedBy(characterManager.character));
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

        CharacterManager attacker = TurnManager.Instance != null
            ? TurnManager.Instance.currentCharacter
            : null;

        if (attacker == null || attacker.combatHandler == null)
            return;

        foreach (CharacterManager protectedTarget in counterSkillQueue
                     .Where(entry => entry != null)
                     .Select(entry => entry.protectedTarget ?? entry.target)
                     .Where(target => target != null && target != characterManager)
                     .Distinct())
        {
            protectedTarget.characterUIHandler?.UpdateSkillQueueUI(
                attacker.GetSkillQueue(),
                attacker);
        }
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
