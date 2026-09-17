using System.Collections.Generic;
using UnityEngine;

public abstract class EquipmentDefinitionSO : ItemDefinitionSO
{
    public EquipmentType equipType;

    [Header("Tier")]
    public int tier = 1;

    [Header("Base Stat Modifiers")]
    public CharacterStats statModifiers = new CharacterStats();
    public CharacterSpecialStats specialStatModifiers = new CharacterSpecialStats();

    [Header("Generation")]
    public bool canGenerateAffix = true;

    [Tooltip("전설/고유 장비처럼 랜덤 생성이 아니라 수동 설계된 장비인지 여부")]
    public bool isUnique = false;

    [Tooltip("몬스터 처치 드롭, 퀘스트 보상 풀, 랜덤 상점 풀 등에 포함될 수 있는 장비인지 여부")]
    public bool canDrop = true;

    [Header("Fixed Equipment Traits")]
    public List<int> grantedTraitIds = new();

    [Header("Visual")]
    public string visualKey;

    [Header("Rarity Visual Override")]
    public string uncommonVisualKey;
    public string rareVisualKey;
    public string epicVisualKey;
    public string legendaryVisualKey;

    public string GetVisualKey(EquipmentRarity rarity)
    {
        switch (rarity)
        {
            case EquipmentRarity.Uncommon:
                return !string.IsNullOrEmpty(uncommonVisualKey) ? uncommonVisualKey : visualKey;

            case EquipmentRarity.Rare:
                return !string.IsNullOrEmpty(rareVisualKey) ? rareVisualKey : visualKey;

            case EquipmentRarity.Epic:
                return !string.IsNullOrEmpty(epicVisualKey) ? epicVisualKey : visualKey;

            case EquipmentRarity.Legendary:
                return !string.IsNullOrEmpty(legendaryVisualKey) ? legendaryVisualKey : visualKey;

            default:
                return visualKey;
        }
    }

    protected virtual void OnValidate()
    {
        category = ItemCategory.Equipment;
        maxStack = 1;

        if (isUnique)
            canGenerateAffix = false;
    }
}