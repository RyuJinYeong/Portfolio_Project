using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveDTO
{
    public int v = 7;

    public string playerName;
    public int level;
    public int gold;

    public string currentStage;

    public List<InventorySlotData> accountStorage = new();
    public List<InventorySlotData> expeditionStorage = new();
    public List<LostExpeditionInventoryData> lostExpeditionInventories = new();

    public List<GeneratedEquipmentData> generatedEquipments = new();

    public List<string> characterIds = new();
    public List<string> missingCharacterIds = new();
    public List<string> revivalRequiredCharacterIds = new();
    public List<string> activeCharacterIds = new();
    public List<CharacterSaveDTO> recruitmentCandidates = new();
    public bool recruitmentCandidatesInitialized;
    public int recruitmentRefreshCount;
    public string reservedRecruitmentCandidateId;

    public List<PositionEntry> positions = new();

    public QuestStateDTO questState;
}

[Serializable]
public class CharacterSaveDTO
{
    public int v = 4;

    public string id;
    public string name;
    public int type;

    // OriginDefinitionSO.id
    public int originId;
    public string originName;

    public int personality;
    public int belonging;
    public int morale;

    public bool isMale;
    public int genderId;
    public int faceTypeId;
    public int hairStyleId;
    public int hairColorId;
    public int skinColorId;
    public int eyeColorId;
    public int facialHairId;
    public int bustSizeId;

    public int level;
    public int exp;
    public int pendingLevelUps;
    public int currentHp;
    public int currentStamina;
    public int currentMentality;

    public CharacterStats originBaseStats;
    public CharacterSpecialStats originSpecialStats;
    public CharacterStats modifiedStats;
    public CharacterSpecialStats modifiedSpecialStats;

    public EquipmentSlotData equipmentSlots = new();

    public float physicalDamageMultiplier;
    public float magicalDamageMultiplier;
    public float attackSpeedMultiplier;
    public float castSpeedMultiplier;

    public bool isAlive;
    public bool isMine;

    public int physicalArmor;
    public int magicalArmor;

    public List<SkillSaveDTO> skills = new();
    public int defaultCounterSkillUid;

    public List<TraitSaveDTO> traits = new();
}

[Serializable]
public class SkillSaveDTO
{
    public int uid;

    public int useCount;
    public int killCount;
    public int damageCount;

    public bool quickSlot;
    public bool canUse;
    public bool useOffHand;
}

[Serializable]
public class TraitSaveDTO
{
    public int id;
    public int point;
}

[Serializable]
public class QuestStateDTO
{
    public List<QuestBoardEntry> board = new();
    public List<QuestIssuer> preferredIssuers = new();
    public List<int> preferredQuestTiers = new();
    public ActiveQuestRuntime active;
    public List<CompletedQuestEntry> completed = new();
}
