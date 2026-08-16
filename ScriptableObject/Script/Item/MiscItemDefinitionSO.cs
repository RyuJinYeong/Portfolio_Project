using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Item/Misc")]
public class MiscItemDefinitionSO : ItemDefinitionSO
{
    public bool isQuestItem;
    public bool isRevivalToken;

    protected void OnValidate()
    {
        category = ItemCategory.Misc;
    }
}