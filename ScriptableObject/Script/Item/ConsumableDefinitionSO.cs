using UnityEngine;

public enum ConsumableType
{
    None,
    HealHp,
    RecoverStamina,
    RecoverMentality,
    LearnSkill,
    GainTrait,
    MonsterEssence
}

[CreateAssetMenu(menuName = "GameData/Item/Consumable")]
public class ConsumableDefinitionSO : ItemDefinitionSO
{
    public ConsumableType consumableType;

    public int hpAmount;
    public int staminaAmount;
    public int mentalityAmount;

    public int skillUid;
    public int traitId;

    [Header("Monster Essence Trait Weights")]
    [Min(0)] public int traitWeightF = 64;
    [Min(0)] public int traitWeightE = 32;
    [Min(0)] public int traitWeightD = 16;
    [Min(0)] public int traitWeightC = 8;
    [Min(0)] public int traitWeightB = 4;
    [Min(0)] public int traitWeightA = 2;
    [Min(0)] public int traitWeightS = 1;

    public int GetMonsterEssenceTraitWeight(TraitGrade grade)
    {
        return grade switch
        {
            TraitGrade.F => traitWeightF,
            TraitGrade.E => traitWeightE,
            TraitGrade.D => traitWeightD,
            TraitGrade.C => traitWeightC,
            TraitGrade.B => traitWeightB,
            TraitGrade.A => traitWeightA,
            TraitGrade.S => traitWeightS,
            _ => 0
        };
    }

    protected void OnValidate()
    {
        category = ItemCategory.Consumable;
    }
}
