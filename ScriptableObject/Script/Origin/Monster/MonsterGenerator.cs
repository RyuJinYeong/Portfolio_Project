using System.Collections.Generic;

public static class MonsterGenerator
{
    public static CharacterData Generate(MonsterRoleSO role, int questLevel)
    {
        if (role == null || role.baseMonster == null)
            return null;

        CharacterData character = new CharacterData();

        character.originId = role.baseMonster.id;
        character.originName = role.baseMonster.monsterName + " " + role.roleName;

        character.Type = role.characterType;
        character.personality = role.personality;

        character.OriginBaseStats = role.baseMonster.baseStats != null
            ? role.baseMonster.baseStats.Copy()
            : new CharacterStats();

        character.OriginSpecialStats = role.baseMonster.baseSpecialStats != null
            ? role.baseMonster.baseSpecialStats.Copy()
            : new CharacterSpecialStats();

        if (role.roleStatBonus != null)
            character.OriginBaseStats += role.roleStatBonus;

        if (role.roleSpecialStatBonus != null)
            character.OriginSpecialStats += role.roleSpecialStatBonus;

        character.BaseStats = character.OriginBaseStats.Copy();
        character.BaseSpecialStats = character.OriginSpecialStats.Copy();

        character.ModifiedStats = new CharacterStats();
        character.ModifiedSpecialStats = new CharacterSpecialStats();

        character.FinalStats = new CharacterStats();
        character.FinalSpecialStats = new CharacterSpecialStats();

        character.Traits = new List<TraitRuntimeData>();
        character.EquipmentTraitRuntimes = new List<TraitRuntimeData>();
        character.Skills = new List<SkillRuntimeData>();
        character.StatusEffects = new List<StatusEffectRuntimeData>();
        character.AvailableAttributes = new List<SkillAttribute>();
        character.EquipmentSlots = new EquipmentSlotData();

        character.Level = questLevel + role.levelBonus;
        character.IsAlive = true;
        character.IsMine = false;

        ApplyTraits(character, role.baseMonster.baseTraitIds);
        ApplyTraits(character, role.traitIds);
        ApplySkills(character, role.skillUids);

        RuntimeEquipmentApplier.ApplyRuntimeEquipments(character, role.equipments);

        character.RemoveAllTraits(null);
        character.ApplyAllTraits(null);
        character.UpdateFinalStats();

        character.CurrentHp = character.FinalStats.MaxHp;
        character.CurrentStamina = character.FinalStats.MaxStamina;
        character.CurrentMentality = character.FinalStats.MaxMentality;

        return character;
    }

    private static void ApplyTraits(CharacterData character, List<int> traitIds)
    {
        if (character == null || traitIds == null)
            return;

        foreach (int traitId in traitIds)
        {
            TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(traitId);

            if (trait == null)
                continue;

            TraitGradeUtility.AddTrait(character.Traits, trait, trait.defaultAcquireGrade);
        }
    }

    private static void ApplySkills(CharacterData character, List<int> skillUids)
    {
        if (character == null || skillUids == null)
            return;

        foreach (int skillUid in skillUids)
        {
            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(skillUid);

            if (skill == null)
                continue;

            character.Skills.Add(SkillRuntimeFactory.Create(skill));
        }
    }
}