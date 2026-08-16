using UnityEngine;

public enum ConsumableType
{
    None,
    HealHp,
    RecoverStamina,
    RecoverMentality,
    LearnSkill,
    GainTrait
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

    protected void OnValidate()
    {
        category = ItemCategory.Consumable;
    }
}