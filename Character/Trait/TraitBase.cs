using System.Runtime.Serialization;

public abstract class TraitBase
{
    public abstract string Name { get; }
    public abstract bool IsPercentage { get; }

    public abstract int Id { get; }
    public abstract TraitGrade Grade { get; } // 특성 등급 필드

    public virtual int Level { get; set; } = 1; // 특성 레벨 필드 - 중복 습득시 레벨업
    public virtual TraitPolarity Polarity { get; set; } = TraitPolarity.Positive; // 특성 구분 필드


    public abstract void ApplyTrait(CharacterManager manager);
    public abstract void RemoveTrait(CharacterManager manager);
}