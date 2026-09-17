using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Origin")]
public class OriginDefinitionSO : ScriptableObject
{
    public int id;
    public string originName;

    [TextArea]
    public string description;

    public Texture2D icon;

    public int traitPoint = 8;

    public CharacterType characterType = CharacterType.Character;
    public Personality defaultPersonality = Personality.Simple;

    public CharacterStats originBaseStats = new CharacterStats();
    public CharacterSpecialStats originSpecialStats = new CharacterSpecialStats();

    [Header("Level Growth Weights")]
    public List<StatGrowthWeightData> levelUpStatWeights = new();

    public List<int> initialTraitIds = new();
    public List<int> initialSkillUids = new();

    public int defaultCounterSkillUid;

    [Header("Initial Equipment Slots")]
    public EquipmentSpawnSlotSet initialEquipmentSlots = new EquipmentSpawnSlotSet();

    [Header("Additional Initial Equipments")]
    public List<EquipmentSpawnData> initialInventoryEquipments = new();

    public CharacterData CreateCharacterData()
    {
        CharacterData data = CreateBaseCharacterData();

        ApplyInitialTraits(data);
        ApplyInitialSkills(data);

        data.DefaultCounterSkill = defaultCounterSkillUid;

        StartingEquipmentApplier.ApplyStartingEquipmentSlots(data, initialEquipmentSlots);

        return data;
    }

    public CharacterData CreateRuntimeCharacterData()
    {
        CharacterData data = CreateBaseCharacterData();

        ApplyInitialTraits(data);
        ApplyInitialSkills(data);

        data.DefaultCounterSkill = defaultCounterSkillUid;

        RuntimeEquipmentApplier.ApplyRuntimeEquipmentSlots(data, initialEquipmentSlots);

        return data;
    }

    private CharacterData CreateBaseCharacterData()
    {
        CharacterData data = new CharacterData();

        data.originId = id;
        data.originName = originName;
        data.Type = characterType;
        data.personality = defaultPersonality;

        data.OriginBaseStats = originBaseStats != null
            ? originBaseStats.Copy()
            : new CharacterStats();

        data.OriginSpecialStats = originSpecialStats != null
            ? originSpecialStats.Copy()
            : new CharacterSpecialStats();

        data.BaseStats = data.OriginBaseStats.Copy();
        data.BaseSpecialStats = data.OriginSpecialStats.Copy();

        data.ModifiedStats = new CharacterStats();
        data.ModifiedSpecialStats = new CharacterSpecialStats();

        data.FinalStats = new CharacterStats();
        data.FinalSpecialStats = new CharacterSpecialStats();

        data.Traits = new List<TraitRuntimeData>();
        data.EquipmentTraitRuntimes = new List<TraitRuntimeData>();
        data.Skills = new List<SkillRuntimeData>();
        data.StatusEffects = new List<StatusEffectRuntimeData>();
        data.AvailableAttributes = new List<SkillAttribute>();

        data.EquipmentSlots = new EquipmentSlotData();

        data.Level = 1;
        data.Exp = 0;

        data.PhysicalDamageMultiplier = 1f;
        data.MagicalDamageMultiplier = 1f;
        data.AttackSpeedMultiplier = 1f;
        data.CastSpeedMultiplier = 1f;

        data.IsAlive = true;
        data.IsMine = characterType == CharacterType.Character;

        return data;
    }

    private void ApplyInitialTraits(CharacterData data)
    {
        if (data == null || initialTraitIds == null)
            return;

        foreach (int traitId in initialTraitIds)
        {
            TraitDefinitionSO trait = GameDataRegistry.Instance.GetTrait(traitId);

            if (trait == null)
                continue;

            TraitGradeUtility.AddTrait(data.Traits, trait, trait.defaultAcquireGrade);
        }
    }

    private void ApplyInitialSkills(CharacterData data)
    {
        if (data == null || initialSkillUids == null)
            return;

        foreach (int skillUid in initialSkillUids)
        {
            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(skillUid);

            if (skill == null)
                continue;

            data.Skills.Add(SkillRuntimeFactory.Create(skill));
        }
    }
}
