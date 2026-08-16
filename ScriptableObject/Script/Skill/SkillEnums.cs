public enum SkillStyle
{
    Strength,   // 힘
    Dexterity,  // 기교
    Speed       // 속도
}

public enum CounterActionType
{
    None,
    Evade,
    Parry,
    Guard,
    Break
}

public enum CounterResult
{
    None,
    Fail,
    Success,
    GreatSuccess
}

public enum AttackEffectType
{
    Breakthrough
}

public enum RevealLevel
{
    None,
    Partial,
    Full
}

public enum SkillDamageKind
{
    Physical,
    Magical
}

public enum SkillType // 스킬 타입 (물리/마법)
{
    Physical,
    Magical,
    Mixed // 혼합 타입 (물리+마법)
}

public enum SkillAttribute // 스킬 세부 속성
{
    Fire,
    Ice,
    Lightning,
    Magic, // 속성이 부여되지 않은 일반 마법 공격 속성, 버프 스킬용
    Pierce, // 관통
    Slash, // 참격
    Smash, // 타격
    None // 물리 버프 스킬용 무속성 타입 추가
}

public enum SkillDiscipline // ui 표현용 스킬 필요 무기 열거형
{
    Basic,          // 기본기
    WeaponArt,      // 무기술
    Swordsmanship,  // 검술
    Archery,        // 궁술
    ShieldArt,      // 방패술
    MartialArt,     // 체술
    HeavyWeapon,    // 중량 무기술
    Magic,          // 마법
    Monster         // 몬스터 전용
}