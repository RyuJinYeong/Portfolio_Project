using System.Collections.Generic;

public interface IMagicalSkill // 마법 타입 스킬
{
    int MentalCost { get; } // 정신력 소모량
}

public interface IPhysicalSkill // 물리 타입 스킬
{
    int EnduCost { get; } // 지구력 소모량
}

public interface IEvolvableSkill // 진화가 가능한 스킬
{    
    public int SkillUseCount { get; }
    public int SkillKillCount { get; }
    public int SkillDamageCount { get; }

    public int EvolUseCount { get; }
    public int EvolKillCount { get; }
    public int EvolDamageCount { get; }

    public void OnSkillUsed(int damage, CharacterData character, CharacterData target); // 스킬 발동시 호출될 진화조건 갱신용 메서드
    void EvolveSkill(CharacterData user); // 진화조건 충족시 스킬 진화 로직
    public void CheckEvolution(CharacterData user);
}

public interface IConditionalSkill // 습득 조건이 있는 스킬
{
    List<ISkillAcquisitionCondition> AcquisitionConditions { get;} // 스킬 습득 조건 List 추가 ( 이를 통해 스킬의 습득 조건 설정 가능 )
    bool CanBeAcquiredBy(CharacterData character); // 캐릭터를 매개변수로 받아 해당 캐릭터가 스킬 습득 조건을 만족하는지 판별
}

public interface IBuffSkill // 버프 스킬
{
    void ApplyBuff(CharacterData user, CharacterData target);
}

public interface IDefensiveSkill // 방어 스킬
{

}