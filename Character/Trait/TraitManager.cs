using UnityEngine;

public static class TraitManager
{
    public static void AddTrait(CharacterManager manager, int traitId)
    {
        TraitDefinitionSO def = GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetTrait(traitId)
            : null;

        if (def == null)
        {
            Debug.LogWarning($"존재하지 않는 특성 ID입니다: {traitId}");
            return;
        }

        AddTrait(manager, traitId, def.defaultAcquireGrade);
    }

    public static void AddTrait(
        CharacterManager manager,
        int traitId,
        TraitGrade acquiredGrade)
    {
        if (manager == null || manager.character == null)
            return;

        CharacterData character = manager.character;
        TraitDefinitionSO def = GameDataRegistry.Instance.GetTrait(traitId);

        if (def == null)
        {
            Debug.LogWarning($"존재하지 않는 특성 ID입니다: {traitId}");
            return;
        }

        character.RemoveAllTraits(manager);

        TraitGradeUtility.AddTrait(
            character.Traits,
            def,
            acquiredGrade);

        ValidateEquipmentsAfterTraitChanged(manager);

        EquipmentManager.RebuildEquipmentStats(character);
        EquipmentManager.UpdateAvailableAttributes(character);
        EquipmentManager.UpdateSkillAvailability(character);

        character.ApplyAllTraits(manager);
        character.UpdateFinalStats();
    }

    public static void RemoveTrait(CharacterManager manager, int traitId)
    {
        if (manager == null || manager.character == null)
            return;

        CharacterData character = manager.character;
        TraitDefinitionSO def = GameDataRegistry.Instance.GetTrait(traitId);

        if (def == null)
        {
            Debug.LogWarning($"존재하지 않는 특성 ID입니다: {traitId}");
            return;
        }

        character.RemoveAllTraits(manager);

        TraitGradeUtility.RemoveTrait(
            character.Traits,
            def,
            def.defaultAcquireGrade);

        ValidateEquipmentsAfterTraitChanged(manager);

        EquipmentManager.RebuildEquipmentStats(character);
        EquipmentManager.UpdateAvailableAttributes(character);
        EquipmentManager.UpdateSkillAvailability(character);

        character.ApplyAllTraits(manager);
        character.UpdateFinalStats();
    }

    static void ValidateEquipmentsAfterTraitChanged(CharacterManager manager)
    {
        if (manager == null || manager.character == null)
            return;

        CharacterData character = manager.character;

        WeaponDefinitionSO mainWeapon = character.GetMainWeapon();

        if (mainWeapon != null && !character.CanEquipMainWeapon(mainWeapon))
            EquipmentManager.Unequip(manager, EquipmentType.Weapon, 1);

        WeaponDefinitionSO subWeapon = character.GetSubWeapon();

        if (subWeapon != null && !character.CanEquipSubWeapon())
            EquipmentManager.Unequip(manager, EquipmentType.SubWeapon, 1);
    }
}
