using System.Collections.Generic;

public static class EquipmentRuntimeResolver
{
    public static EquipmentRuntimeData Resolve(int itemUid, string instanceId)
    {
        if (itemUid <= 0)
            return null;

        EquipmentDefinitionSO definition = GameDataRegistry.Instance.GetEquipment(itemUid);

        if (definition == null)
            return null;

        GeneratedEquipmentData generated = EquipmentInstanceRepository.Get(instanceId);

        if (generated != null)
        {
            return new EquipmentRuntimeData
            {
                definition = definition,
                generated = generated,

                uid = itemUid,
                instanceId = instanceId,

                displayName = generated.generatedName,
                tier = generated.tier,
                rarity = generated.rarity,

                equipType = definition.equipType,

                statModifiers = generated.statModifiers != null
                    ? generated.statModifiers.Copy()
                    : new CharacterStats(),

                specialStatModifiers = generated.specialStatModifiers != null
                    ? generated.specialStatModifiers.Copy()
                    : new CharacterSpecialStats(),

                grantedTraitIds = generated.grantedTraitIds != null
                    ? new List<int>(generated.grantedTraitIds)
                    : new List<int>(),

                visualKey = !string.IsNullOrEmpty(generated.visualKey)
                    ? generated.visualKey
                    : definition.visualKey
            };
        }

        // 예외 fallback:
        // 구버전 저장 데이터나 직접 SO 장비를 참조하는 경우.
        return new EquipmentRuntimeData
        {
            definition = definition,
            generated = null,

            uid = itemUid,
            instanceId = null,

            displayName = definition.itemName,
            tier = definition.tier,
            rarity = EquipmentRarity.Common,

            equipType = definition.equipType,

            statModifiers = definition.statModifiers != null
                ? definition.statModifiers.Copy()
                : new CharacterStats(),

            specialStatModifiers = definition.specialStatModifiers != null
                ? definition.specialStatModifiers.Copy()
                : new CharacterSpecialStats(),

            grantedTraitIds = definition.grantedTraitIds != null
                ? new List<int>(definition.grantedTraitIds)
                : new List<int>(),

            visualKey = definition.visualKey
        };
    }
}