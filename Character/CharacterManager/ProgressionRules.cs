using System.Collections.Generic;
using UnityEngine;

public static class ProgressionRules
{
    public const int OverpoweredLevelDifference = 10;

    public static int GetRequiredExperience(int currentLevel)
    {
        return 15 * (Mathf.Max(1, currentLevel) + 5);
    }

    public static int GetQuestTierMaximumLevel(int questTier)
    {
        return Mathf.Max(1, questTier) * 10;
    }

    public static bool IsOverpoweredForQuest(CharacterData character, QuestDef quest)
    {
        if (character == null || quest == null)
            return false;

        return character.Level - GetQuestTierMaximumLevel(quest.tier) >=
               OverpoweredLevelDifference;
    }

    public static int CalculateMonsterExperience(
        CharacterData participant,
        CharacterData monster,
        int highestPartyLevel)
    {
        if (participant == null || monster == null || monster.GrantsExperience == false)
            return 0;

        int characterLevel = Mathf.Max(1, participant.Level);
        int monsterLevel = Mathf.Max(1, monster.Level);
        int companionDifference = Mathf.Max(0, highestPartyLevel - characterLevel);
        bool isLowLevelCompanion = companionDifference >= 10;
        int creditedMonsterLevel = Mathf.Min(monsterLevel, characterLevel + 5);

        if (isLowLevelCompanion)
            creditedMonsterLevel = Mathf.Min(creditedMonsterLevel, characterLevel);

        float levelMultiplier = GetEnemyLevelMultiplier(
            characterLevel,
            creditedMonsterLevel);
        float companionMultiplier = GetCompanionMultiplier(companionDifference);
        int rankMultiplier = GetMonsterRankMultiplier(monster.Type);
        float experience = (creditedMonsterLevel + 5) *
                           rankMultiplier *
                           levelMultiplier *
                           companionMultiplier;

        return Mathf.Max(0, Mathf.FloorToInt(experience));
    }

    public static int SettleVictoryExperience(List<CharacterManager> combatants)
    {
        if (combatants == null)
            return 0;

        List<CharacterManager> participants = combatants.FindAll(manager =>
            manager != null && manager.character != null && manager.character.IsMine);
        List<CharacterData> monsters = new List<CharacterData>();

        foreach (CharacterManager manager in combatants)
        {
            if (manager != null && manager.character != null && !manager.character.IsMine)
                monsters.Add(manager.character);
        }

        int highestPartyLevel = 1;

        foreach (CharacterManager participant in participants)
            highestPartyLevel = Mathf.Max(highestPartyLevel, participant.character.Level);

        int totalGrantedExperience = 0;

        foreach (CharacterManager participant in participants)
        {
            int gainedExperience = 0;

            foreach (CharacterData monster in monsters)
            {
                gainedExperience += CalculateMonsterExperience(
                    participant.character,
                    monster,
                    highestPartyLevel);
            }

            if (gainedExperience <= 0)
                continue;

            GrantExperience(participant, gainedExperience);
            totalGrantedExperience += gainedExperience;
            Debug.Log($"[Experience] {participant.character.Name}: +{gainedExperience} XP");
        }

        return totalGrantedExperience;
    }

    public static int SettleVictoryEssenceDrops(List<CharacterManager> combatants)
    {
        ActiveQuestRuntime activeQuest = QuestManager.Instance != null
            ? QuestManager.Instance.active
            : null;
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (combatants == null || activeQuest?.def == null || playerData == null)
            return 0;

        List<InventorySlotData> storage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.ExpeditionStorage);
        int droppedCount = 0;

        foreach (CharacterManager manager in combatants)
        {
            CharacterData monster = manager != null ? manager.character : null;

            if (monster == null || monster.IsMine || monster.IsAlive ||
                (monster.Type != CharacterType.Elite && monster.Type != CharacterType.Boss))
            {
                continue;
            }

            if (SharedInventoryUtility.AddMonsterEssence(
                    storage,
                    activeQuest.def.id,
                    monster))
            {
                droppedCount++;
            }
        }

        return droppedCount;
    }

    public static List<InventorySlotData> RollVictoryLoot(
        List<CharacterManager> combatants)
    {
        List<InventorySlotData> loot = new List<InventorySlotData>();
        ActiveQuestRuntime activeQuest = QuestManager.Instance != null
            ? QuestManager.Instance.active
            : null;

        if (combatants == null || activeQuest?.def == null)
            return loot;

        foreach (CharacterManager manager in combatants)
        {
            CharacterData monster = manager != null ? manager.character : null;

            if (monster == null || monster.IsMine || monster.IsAlive)
                continue;

            MonsterRoleSO role = GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetMonsterRole(monster.monsterRoleId)
                : null;
            int equipmentDropChance = role != null
                ? role.equippedItemDropChance
                : 0;

            foreach (GeneratedEquipmentData equipment in
                     EquipmentDropUtility.RollDroppedEquipments(
                         monster,
                         equipmentDropChance))
            {
                if (equipment == null || equipment.definitionUid <= 0 ||
                    string.IsNullOrEmpty(equipment.instanceId))
                {
                    continue;
                }

                loot.Add(new InventorySlotData
                {
                    itemUid = equipment.definitionUid,
                    count = 1,
                    equipmentInstanceId = equipment.instanceId
                });
            }

            if (monster.Type != CharacterType.Elite &&
                monster.Type != CharacterType.Boss)
            {
                continue;
            }

            InventorySlotData essence = SharedInventoryUtility.CreateMonsterEssence(
                activeQuest.def.id,
                monster);

            if (essence != null)
                loot.Add(essence);
        }

        return loot;
    }

    public static int GrantExperience(CharacterManager manager, int amount)
    {
        if (manager == null || manager.character == null || amount <= 0)
            return 0;

        CharacterData character = manager.character;
        character.Exp += amount;
        int gainedLevels = 0;

        while (character.Exp >= GetRequiredExperience(character.Level))
        {
            character.Exp -= GetRequiredExperience(character.Level);
            character.Level++;
            character.PendingLevelUps++;
            gainedLevels++;
        }

        manager.UpdateCharacterUI();
        PlayerManager.Instance?.SaveCharacter(character);
        return gainedLevels;
    }

    public static float GetEssenceDropMultiplier(int highestPartyLevel, int questTier)
    {
        int difference = highestPartyLevel - GetQuestTierMaximumLevel(questTier);

        if (difference <= 0)
            return 1f;
        if (difference < 5)
            return 0.5f;
        if (difference < 10)
            return 0.2f;

        return 0f;
    }

    public static int GetEssenceAcquisitionChance(int characterLevel, int questTier)
    {
        int difference = characterLevel - GetQuestTierMaximumLevel(questTier);

        if (difference <= 0)
            return 100;
        if (difference < 5)
            return 35;
        if (difference < 10)
            return 10;

        return 0;
    }

    private static float GetEnemyLevelMultiplier(int characterLevel, int monsterLevel)
    {
        int difference = monsterLevel - characterLevel;

        if (difference >= 0)
            return 1f + Mathf.Min(5, difference) * 0.04f;

        int lowerLevelDifference = -difference;

        if (lowerLevelDifference < 5)
            return 0.75f;
        if (lowerLevelDifference < 10)
            return 0.4f;
        if (lowerLevelDifference < 15)
            return 0.1f;

        return 0f;
    }

    private static float GetCompanionMultiplier(int levelDifference)
    {
        if (levelDifference < 10)
            return 1f;
        if (levelDifference < 15)
            return 0.75f;
        if (levelDifference < 20)
            return 0.5f;

        return 0.25f;
    }

    private static int GetMonsterRankMultiplier(CharacterType type)
    {
        return type switch
        {
            CharacterType.Elite => 3,
            CharacterType.Boss => 8,
            _ => 1
        };
    }
}
