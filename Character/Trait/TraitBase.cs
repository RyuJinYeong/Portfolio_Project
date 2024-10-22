using System.Runtime.Serialization;

public abstract class TraitBase
{
    public abstract string Name { get; }
    public abstract bool IsPercentage { get; }

    public abstract void ApplyTrait(CharacterManager manager);    
    public abstract void RemoveTrait(CharacterManager manager);
}