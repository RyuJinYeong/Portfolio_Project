using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveDTO
{
    public int v = 1;                       // 스키마 버전
    public string playerName;
    public int level;
    public int gold;

    public string currentStage;
    public List<string> characterIds = new();
    public List<string> activeCharacterIds = new();

    // Dictionary 대체
    public List<PositionEntry> positions = new(); // { characterId, isFront }
}

[Serializable]
public class CharacterSaveDTO
{
    public int v = 1;                       // 스키마 버전

    // 기본 식별
    public string id;
    public string name;
    public int type;                  // CharacterType (int 저장)

    // 출신
    public int origin;                // Origin (int 저장)
    public string originName;

    // 성향/심리
    public int personality;           // Personality (int 저장)
    public int belonging;
    public int morale;

    // 커스터마이징 (필요한 것만 Opt-in)
    public bool isMale;
    public int hairType, eyebrowsType, eyeType, mouthType, beardType;
    public int hairColor, skinTone;

    // 진행/상태
    public int level;
    public int exp;
    public int currentHp;
    public int currentStamina;
    public int currentMentality;

    // 원천 스탯(계산 결과는 저장 X)
    public CharacterStats baseStats;
    public CharacterStats modifiedStats;

    // 장비 슬롯 - UID만
    public int helmetUid, armorUid, glovesUid, shoesUid, capeUid, ring1Uid, ring2Uid, necklaceUid, weaponUid, subWeaponUid;

    // 배율/속도 (상태이상/특성 등으로 변동되는 런타임계수)
    public float physicalDamageMultiplier;
    public float magicalDamageMultiplier;
    public float attackSpeedMultiplier;
    public float castSpeedMultiplier;

    // 생존여부 / 진영
    public bool isAlive;
    public bool isMine;

    // 방어도
    public int physicalArmor;
    public int magicalArmor;

    // 인벤토리 에셋 JSON 페이로드(그 에셋이 제공하는 JSON 문자열)
    public string inventoryJson;
    public string equipmentJson;

    // 스킬 UID + 진화 카운트
    public List<SkillSaveDTO> skills = new();
    public int defaultCounterSkillUid;

    public List<TraitSaveDTO> traits = new();
    public List<SkillSaveDTO> equipmentSkills = new();
    public List<TraitSaveDTO> equipmentTraits = new();

    // 장착 무기 세부 속성(아트리뷰트)
    public List<int> availableAttributes = new();
}

[Serializable]
public class SkillSaveDTO
{
    public int uid;
    public int useCount;
    public int killCount;
    public int damageCount;
    public bool quickSlot;
}

[Serializable]
public class TraitSaveDTO
{
    public int id;      // 정수 식별자
    public int level;   // 중복 스택(최소 1)
}