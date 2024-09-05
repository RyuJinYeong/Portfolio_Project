using System.Runtime.Serialization;

public abstract class TraitBase
{
    public abstract string Name { get; }
    public abstract bool IsPercentage { get; }

    public abstract void ApplyTrait(CharacterData character);    
    public abstract void RemoveTrait(CharacterData character);
}