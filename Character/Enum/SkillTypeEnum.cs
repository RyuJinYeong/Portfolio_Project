public enum SkillType // 스킬 타입 (물리/마법)
{
    Physical,
    Magical
}

public enum SkillAttribute // 스킬 세부 속성
{
    Fire,
    Water,
    Earth,
    Wind,
    Magic, // 속성이 부여되지 않은 일반 마법 공격 속성, 버프 스킬용
    Pierce, // 관통
    Slash, // 참격
    Smash, // 타격
    None // 물리 버프 스킬용 무속성 타입 추가
}