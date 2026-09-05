using System.Collections.Generic;
using UnityEngine;

public class QuestEncounterResolution
{
    public string message;
    public bool startsBattle;
    public List<int> monsterRoleIds = new();
}

public static class QuestEncounterResolver
{
    private static readonly StatRequirementType[] DefaultStats =
    {
        StatRequirementType.Strength,
        StatRequirementType.Dexterity,
        StatRequirementType.Speed,
        StatRequirementType.Intelligence,
        StatRequirementType.Wisdom,
        StatRequirementType.Health,
        StatRequirementType.Vitality,
        StatRequirementType.Endurance,
        StatRequirementType.Detection,
        StatRequirementType.Insight
    };

    public static QuestEncounterResolution Resolve(
        QuestEncounterChoiceData choice,
        CharacterManager selectedCharacter)
    {
        QuestEncounterResolution resolution = new QuestEncounterResolution();

        if (choice != null && choice.requiresCharacter &&
            !CanSelectCharacter(choice, selectedCharacter))
        {
            resolution.message =
                "현재 퀘스트 티어에서 과잉전력으로 판정된 캐릭터는 이 선택을 실행할 수 없습니다.";
            return resolution;
        }

        bool checkAttempted = choice != null && choice.abilityCheck != null &&
                              choice.abilityCheck.enabled;
        float successChance = GetSuccessChance(choice, selectedCharacter);
        bool checkSucceeded = !checkAttempted || Random.Range(0f, 100f) < successChance;
        List<QuestEncounterOutcomeData> outcomePool = checkSucceeded
            ? choice?.outcomes
            : choice?.failureOutcomes;
        QuestEncounterOutcomeData outcome = PickOutcome(outcomePool);

        if (outcome == null)
        {
            resolution.message = checkAttempted && !checkSucceeded
                ? $"[{GetCheckStatName(choice.abilityCheck.stat)} 판정 실패]\n아무 일도 일어나지 않았습니다."
                : "아무 일도 일어나지 않았습니다.";
            return resolution;
        }

        List<CharacterManager> targets = GetTargets(
            outcome.targetScope,
            selectedCharacter,
            outcome.effectType == QuestEncounterEffectType.RandomPermanentStat);
        string characterName = selectedCharacter != null && selectedCharacter.character != null
            ? selectedCharacter.character.Name
            : string.Empty;
        string statName = string.Empty;
        int amount = 0;
        string itemName = string.Empty;

        switch (outcome.effectType)
        {
            case QuestEncounterEffectType.RandomPermanentStat:
                ApplyPermanentStat(outcome, targets, out statName, out amount);
                break;

            case QuestEncounterEffectType.RecoverResources:
                RecoverResources(targets);
                break;

            case QuestEncounterEffectType.TemporaryStatModifier:
                ApplyTemporaryEffect(outcome, targets);
                break;

            case QuestEncounterEffectType.RandomEquipment:
                itemName = AddRandomEquipment(outcome);
                break;

            case QuestEncounterEffectType.StartBattle:
                resolution.startsBattle = outcome.monsterRoleIds != null &&
                                          outcome.monsterRoleIds.Count > 0;
                resolution.monsterRoleIds = outcome.monsterRoleIds != null
                    ? new List<int>(outcome.monsterRoleIds)
                    : new List<int>();
                break;

            case QuestEncounterEffectType.RandomRegisteredItem:
                itemName = AddRandomRegisteredItem(outcome);
                break;
        }

        string resultMessage = FormatResult(
            outcome.resultText,
            characterName,
            statName,
            amount,
            itemName);
        resolution.message = checkAttempted
            ? $"[{GetCheckStatName(choice.abilityCheck.stat)} 판정 {(checkSucceeded ? "성공" : "실패")}]\n{resultMessage}"
            : resultMessage;

        PlayerManager.Instance?.SavePlayerDataToPlayFab();
        return resolution;
    }

    public static float GetSuccessChance(
        QuestEncounterChoiceData choice,
        CharacterManager selectedCharacter)
    {
        if (choice == null || choice.abilityCheck == null || !choice.abilityCheck.enabled)
            return 100f;

        QuestEncounterAbilityCheckData check = choice.abilityCheck;
        int statValue = GetCheckValue(check.stat, selectedCharacter);
        ActiveQuestRuntime active = QuestManager.Instance != null
            ? QuestManager.Instance.active
            : null;
        int tier = active != null && active.def != null
            ? Mathf.Max(1, active.def.tier)
            : 1;
        int difficultyStep = active != null && active.def != null
            ? (int)active.def.difficulty
            : 0;
        float chance = check.baseSuccessChancePercent +
                       statValue * check.chancePerStatPoint -
                       (tier - 1) * check.tierPenaltyPercent -
                       difficultyStep * check.difficultyPenaltyPercent;
        float minimum = Mathf.Min(
            check.minimumSuccessChancePercent,
            check.maximumSuccessChancePercent);
        float maximum = Mathf.Max(
            check.minimumSuccessChancePercent,
            check.maximumSuccessChancePercent);
        return Mathf.Clamp(chance, minimum, maximum);
    }

    public static string GetCheckStatName(QuestEncounterCheckStat stat)
    {
        return stat switch
        {
            QuestEncounterCheckStat.Strength => "근력",
            QuestEncounterCheckStat.Dexterity => "기교",
            QuestEncounterCheckStat.Speed => "속도",
            QuestEncounterCheckStat.Intelligence => "지능",
            QuestEncounterCheckStat.Wisdom => "지혜",
            QuestEncounterCheckStat.Health => "건강",
            QuestEncounterCheckStat.Vitality => "활력",
            QuestEncounterCheckStat.Endurance => "인내",
            QuestEncounterCheckStat.Detection => "눈썰미",
            QuestEncounterCheckStat.Insight => "통찰력",
            QuestEncounterCheckStat.PhysicalAttack => "물리 공격력",
            QuestEncounterCheckStat.MagicalAttack => "마법 공격력",
            QuestEncounterCheckStat.HigherPhysicalOrMagicalAttack => "물리/마법 공격력",
            _ => "능력"
        };
    }

    public static bool CanSelectCharacter(
        QuestEncounterChoiceData choice,
        CharacterManager selectedCharacter)
    {
        if (selectedCharacter == null || selectedCharacter.character == null)
            return false;

        ActiveQuestRuntime active = QuestManager.Instance != null
            ? QuestManager.Instance.active
            : null;

        if (active == null || active.def == null ||
            !ProgressionRules.IsOverpoweredForQuest(selectedCharacter.character, active.def))
        {
            return true;
        }

        QuestRouteNode node = QuestManager.Instance.GetCurrentRouteNode();
        bool hasRandomEncounterAbilityCheck =
            node != null && node.type == QuestRouteNodeType.RandomEncounter &&
            choice != null && choice.abilityCheck != null && choice.abilityCheck.enabled;

        return !hasRandomEncounterAbilityCheck && !HasPermanentGrowthOutcome(choice);
    }

    private static QuestEncounterOutcomeData PickOutcome(
        List<QuestEncounterOutcomeData> outcomes)
    {
        if (outcomes == null)
            return null;

        int totalWeight = 0;

        foreach (QuestEncounterOutcomeData outcome in outcomes)
        {
            if (outcome != null && outcome.weight > 0)
                totalWeight += outcome.weight;
        }

        if (totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        foreach (QuestEncounterOutcomeData outcome in outcomes)
        {
            if (outcome == null || outcome.weight <= 0)
                continue;

            current += outcome.weight;

            if (roll < current)
                return outcome;
        }

        return null;
    }

    private static int GetCheckValue(
        QuestEncounterCheckStat stat,
        CharacterManager selectedCharacter)
    {
        if (selectedCharacter != null && selectedCharacter.character != null)
            return GetCheckValue(stat, selectedCharacter.character.FinalStats);

        int best = 0;

        if (GameManager.Instance == null)
            return best;

        foreach (CharacterManager manager in GameManager.Instance.GetAllCharacters())
        {
            if (manager == null || manager.character == null || !manager.character.IsMine)
                continue;

            ActiveQuestRuntime active = QuestManager.Instance != null
                ? QuestManager.Instance.active
                : null;
            QuestRouteNode node = QuestManager.Instance != null
                ? QuestManager.Instance.GetCurrentRouteNode()
                : null;

            if (active != null && active.def != null && node != null &&
                node.type == QuestRouteNodeType.RandomEncounter &&
                ProgressionRules.IsOverpoweredForQuest(manager.character, active.def))
            {
                continue;
            }

            best = Mathf.Max(best, GetCheckValue(stat, manager.character.FinalStats));
        }

        return best;
    }

    private static int GetCheckValue(QuestEncounterCheckStat stat, CharacterStats stats)
    {
        if (stats == null)
            return 0;

        return stat switch
        {
            QuestEncounterCheckStat.Strength => stats.Strength,
            QuestEncounterCheckStat.Dexterity => stats.Dexterity,
            QuestEncounterCheckStat.Speed => stats.Speed,
            QuestEncounterCheckStat.Intelligence => stats.Intelligence,
            QuestEncounterCheckStat.Wisdom => stats.Wisdom,
            QuestEncounterCheckStat.Health => stats.Health,
            QuestEncounterCheckStat.Vitality => stats.Vitality,
            QuestEncounterCheckStat.Endurance => stats.Endurance,
            QuestEncounterCheckStat.Detection => stats.Detection,
            QuestEncounterCheckStat.Insight => stats.Insight,
            QuestEncounterCheckStat.PhysicalAttack => stats.PhysicalAttack,
            QuestEncounterCheckStat.MagicalAttack => stats.MagicalAttack,
            QuestEncounterCheckStat.HigherPhysicalOrMagicalAttack =>
                Mathf.Max(stats.PhysicalAttack, stats.MagicalAttack),
            _ => 0
        };
    }

    private static List<CharacterManager> GetTargets(
        QuestEncounterTargetScope targetScope,
        CharacterManager selectedCharacter,
        bool excludeOverpowered)
    {
        List<CharacterManager> result = new List<CharacterManager>();

        if (targetScope == QuestEncounterTargetScope.SelectedCharacter)
        {
            if (selectedCharacter != null && selectedCharacter.character != null &&
                (!excludeOverpowered || !IsOverpowered(selectedCharacter.character)))
            {
                result.Add(selectedCharacter);
            }

            return result;
        }

        if (targetScope != QuestEncounterTargetScope.WholeParty || GameManager.Instance == null)
            return result;

        foreach (CharacterManager manager in GameManager.Instance.GetAllCharacters())
        {
            if (manager != null && manager.character != null && manager.character.IsMine &&
                (!excludeOverpowered || !IsOverpowered(manager.character)))
            {
                result.Add(manager);
            }
        }

        return result;
    }

    private static bool HasPermanentGrowthOutcome(QuestEncounterChoiceData choice)
    {
        return ContainsEffect(choice?.outcomes, QuestEncounterEffectType.RandomPermanentStat) ||
               ContainsEffect(choice?.failureOutcomes, QuestEncounterEffectType.RandomPermanentStat);
    }

    private static bool ContainsEffect(
        List<QuestEncounterOutcomeData> outcomes,
        QuestEncounterEffectType effectType)
    {
        if (outcomes == null)
            return false;

        return outcomes.Exists(outcome => outcome != null && outcome.effectType == effectType);
    }

    private static bool IsOverpowered(CharacterData character)
    {
        ActiveQuestRuntime active = QuestManager.Instance != null
            ? QuestManager.Instance.active
            : null;

        return active != null && active.def != null &&
               ProgressionRules.IsOverpoweredForQuest(character, active.def);
    }

    private static void ApplyPermanentStat(
        QuestEncounterOutcomeData outcome,
        List<CharacterManager> targets,
        out string statName,
        out int amount)
    {
        statName = string.Empty;
        amount = 0;

        foreach (CharacterManager manager in targets)
        {
            CharacterData character = manager.character;
            List<StatRequirementType> candidates = outcome.candidateStats != null &&
                                                   outcome.candidateStats.Count > 0
                ? outcome.candidateStats
                : new List<StatRequirementType>(DefaultStats);

            StatRequirementType stat = candidates[Random.Range(0, candidates.Count)];
            int minimum = Mathf.Min(outcome.minAmount, outcome.maxAmount);
            int maximum = Mathf.Max(outcome.minAmount, outcome.maxAmount);
            int rolledAmount = Random.Range(minimum, maximum + 1);

            LevelGrowthUtility.AddStat(character.OriginBaseStats, stat, rolledAmount);
            character.OriginBaseStats.ClampNonNegative();
            character.RemoveAllTraits(manager);
            character.ApplyAllTraits(manager);
            character.UpdateFinalStats();

            if (manager.characterUIHandler != null)
                manager.UpdateCharacterUI();

            PlayerManager.Instance?.SaveCharacter(character);
            statName = GetStatName(stat);
            amount = rolledAmount;
        }
    }

    private static void RecoverResources(List<CharacterManager> targets)
    {
        foreach (CharacterManager manager in targets)
        {
            CharacterData character = manager.character;
            character.CurrentHp = character.FinalStats.MaxHp;
            character.CurrentStamina = character.FinalStats.MaxStamina;
            character.CurrentMentality = character.FinalStats.MaxMentality;

            if (manager.characterUIHandler != null)
                manager.UpdateCharacterUI();

            PlayerManager.Instance?.SaveCharacter(character);
        }
    }

    private static void ApplyTemporaryEffect(
        QuestEncounterOutcomeData outcome,
        List<CharacterManager> targets)
    {
        if (QuestManager.Instance == null)
            return;

        QuestRouteNode currentNode = QuestManager.Instance.GetCurrentRouteNode();

        foreach (CharacterManager manager in targets)
        {
            QuestManager.Instance.AddEncounterEffect(new QuestEncounterRuntimeEffect
            {
                characterId = manager.character.ID,
                effectName = outcome.temporaryEffectName,
                remainingRooms = Mathf.Max(1, outcome.durationRooms),
                sourceNodeId = currentNode != null ? currentNode.id : -1,
                statModifiers = outcome.temporaryStatModifiers != null
                    ? outcome.temporaryStatModifiers.Copy()
                    : new CharacterStats()
            });

            manager.character.UpdateFinalStats();

            if (manager.characterUIHandler != null)
                manager.UpdateCharacterUI();
        }
    }

    private static string AddRandomEquipment(QuestEncounterOutcomeData outcome)
    {
        if (GameDataRegistry.Instance == null || PlayerManager.Instance == null)
            return string.Empty;

        List<EquipmentDefinitionSO> candidates =
            GameDataRegistry.Instance.GetEquipmentsByTier(Mathf.Max(1, outcome.equipmentTier));

        candidates.RemoveAll(equipment => equipment == null || !equipment.canDrop);

        if (candidates.Count == 0)
            return string.Empty;

        EquipmentDefinitionSO definition = candidates[Random.Range(0, candidates.Count)];
        int maximumRarity = Mathf.Clamp(
            (int)outcome.maxEquipmentRarity,
            (int)EquipmentRarity.Common,
            (int)EquipmentRarity.Legendary);
        EquipmentRarity rarity = (EquipmentRarity)Random.Range(
            (int)EquipmentRarity.Common,
            maximumRarity + 1);
        GeneratedEquipmentData generated = EquipmentGenerator.Generate(definition, rarity);

        if (generated == null)
            return string.Empty;

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();

        if (playerData == null)
            return string.Empty;

        List<InventorySlotData> storage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.ExpeditionStorage);

        EquipmentInstanceRepository.AddToPlayer(generated);

        if (!SharedInventoryUtility.AddItem(
                storage,
                definition.uid,
                1,
                generated.instanceId))
        {
            EquipmentInstanceRepository.RemoveFromPlayer(generated.instanceId);
            return string.Empty;
        }

        return generated.generatedName;
    }

    private static string AddRandomRegisteredItem(QuestEncounterOutcomeData outcome)
    {
        if (outcome.itemUids == null || outcome.itemUids.Count == 0 ||
            GameDataRegistry.Instance == null || PlayerManager.Instance == null)
        {
            return string.Empty;
        }

        int itemUid = outcome.itemUids[Random.Range(0, outcome.itemUids.Count)];
        ItemDefinitionSO item = GameDataRegistry.Instance.GetItem(itemUid);

        if (item == null)
            return string.Empty;

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();

        if (playerData == null)
            return string.Empty;

        List<InventorySlotData> storage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.ExpeditionStorage);

        return SharedInventoryUtility.AddItem(
            storage,
            item.uid,
            Mathf.Max(1, outcome.itemCount))
            ? item.itemName
            : string.Empty;
    }

    private static string FormatResult(
        string source,
        string characterName,
        string statName,
        int amount,
        string itemName)
    {
        string result = string.IsNullOrEmpty(source)
            ? "선택의 결과가 결정되었습니다."
            : source;

        return result
            .Replace("{character}", characterName)
            .Replace("{stat}", statName)
            .Replace("{amount}", Mathf.Abs(amount).ToString())
            .Replace("{item}", string.IsNullOrEmpty(itemName) ? "알 수 없는 아이템" : itemName);
    }

    private static string GetStatName(StatRequirementType stat)
    {
        return stat switch
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
            _ => stat.ToString()
        };
    }
}
