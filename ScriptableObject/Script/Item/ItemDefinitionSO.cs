using UnityEngine;

public enum ItemCategory
{
    Equipment,
    Consumable,
    Misc
}

public abstract class ItemDefinitionSO : ScriptableObject
{
    public int uid;
    public string itemName;

    [TextArea]
    public string description;

    public Texture2D icon;

    public ItemCategory category;

    public int price;
    public float weight;
    public int maxStack = 1;

    public bool tradeable = true;
    public bool deletable = true;
}