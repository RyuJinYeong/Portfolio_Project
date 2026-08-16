using System;
using System.Collections.Generic;

[Serializable]
public class GeneratedEquipmentData
{
    public string instanceId;

    // 원본 EquipmentDefinitionSO uid
    public int definitionUid;

    public int tier;
    public EquipmentRarity rarity;

    // 수식어까지 반영된 최종 이름
    public string generatedName;

    // 최종 합산 스탯
    public CharacterStats statModifiers = new CharacterStats();
    public CharacterSpecialStats specialStatModifiers = new CharacterSpecialStats();

    // 고정 장비 Trait + Special Affix Trait 합산
    public List<int> grantedTraitIds = new();

    // 어떤 수식어가 어떤 능력치를 줬는지 추적
    public List<EquipmentAffixRollData> appliedAffixes = new();

    // 실제 생성된 장비 인스턴스가 사용할 외형 키
    public string visualKey;
}