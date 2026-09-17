using SoftKitty.InventoryEngine;
using System.Collections.Generic;
using UnityEngine;

public class GameDataRegistry : MonoBehaviour
{
    public static GameDataRegistry Instance { get; private set; }

    [Header("Items")]
    public List<ItemDefinitionSO> items = new();

    [Header("Skills")]
    public List<SkillDefinitionSO> skills = new();

    [Header("Traits")]
    public List<TraitDefinitionSO> traits = new();

    [Header("Character Creation Traits")]
    public List<TraitDefinitionSO> characterCreationTraits = new();

    [Header("Origins")]
    public List<OriginDefinitionSO> origins = new();

    [Header("Mercenaries")]
    public List<MercenaryDefinitionSO> mercenaryDefinitions = new();
    [Min(1)] public int recruitmentOfferCount = 5;

    [Header("Status Effects")]
    [SerializeField] private List<StatusEffectDefinitionSO> statusEffects = new();

    [Header("Equipment Affixes")]
    [SerializeField] private List<EquipmentAffixDefinitionSO> equipmentAffixes = new();

    [Header("Monster Bases")]
    [SerializeField] private List<MonsterBaseSO> monsterBases = new();

    [Header("Monster Roles")]
    [SerializeField] private List<MonsterRoleSO> monsterRoles = new();

    [Header("Quest Stages")]
    [SerializeField] private List<QuestStageDefinitionSO> questStages = new();

    [Header("Quest Encounters")]
    [SerializeField] private List<QuestEncounterDefinitionSO> questEncounters = new();


    private readonly Dictionary<int, StatusEffectDefinitionSO> statusEffectMap = new();

    private readonly Dictionary<int, ItemDefinitionSO> itemMap = new();
    private readonly Dictionary<int, EquipmentDefinitionSO> equipmentMap = new();

    private readonly Dictionary<int, SkillDefinitionSO> skillMap = new();

    private readonly Dictionary<int, TraitDefinitionSO> traitMap = new();
    private readonly Dictionary<TraitGrade, List<TraitDefinitionSO>> traitGradeMap = new();

    private readonly Dictionary<int, OriginDefinitionSO> originMap = new();

    private readonly Dictionary<int, EquipmentAffixDefinitionSO> equipmentAffixDic = new();

    private readonly Dictionary<int, MonsterBaseSO> monsterBaseMap = new();
    private readonly Dictionary<int, MonsterRoleSO> monsterRoleMap = new();
    private readonly Dictionary<string, QuestStageDefinitionSO> questStageMap = new();
    private readonly Dictionary<string, QuestEncounterDefinitionSO> questEncounterMap = new();

    private readonly Dictionary<int, List<EquipmentDefinitionSO>> equipmentTierMap = new();

    private bool built;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Build();
    }


    public void Build()
    {
        statusEffectMap.Clear();

        itemMap.Clear();
        equipmentMap.Clear();
        equipmentTierMap.Clear();

        skillMap.Clear();

        traitMap.Clear();
        traitGradeMap.Clear();

        originMap.Clear();

        equipmentAffixDic.Clear();

        monsterBaseMap.Clear();
        monsterRoleMap.Clear();
        questStageMap.Clear();
        questEncounterMap.Clear();


        foreach (StatusEffectDefinitionSO status in statusEffects)
        {
            if (status == null)
                continue;

            if (statusEffectMap.ContainsKey(status.id))
            {
                Debug.LogWarning(
                    $"중복 상태이상 id: {status.id} / {status.statusName}");

                continue;
            }

            statusEffectMap.Add(status.id, status);
        }


        foreach (ItemDefinitionSO item in items)
        {
            if (item == null)
                continue;

            itemMap[item.uid] = item;

            if (item is EquipmentDefinitionSO equipment)
            {
                equipmentMap[item.uid] = equipment;

                if (!equipmentTierMap.TryGetValue(equipment.tier, out List<EquipmentDefinitionSO> tierList))
                {
                    tierList = new List<EquipmentDefinitionSO>();

                    equipmentTierMap.Add(equipment.tier, tierList);
                }

                tierList.Add(equipment);
            }
        }


        foreach (SkillDefinitionSO skill in skills)
        {
            if (skill == null)
                continue;

            skillMap[skill.uid] = skill;
        }


        foreach (TraitDefinitionSO trait in traits)
        {
            if (trait == null)
                continue;

            traitMap[trait.id] = trait;

            TraitGrade grade = trait.defaultAcquireGrade;

            if (!traitGradeMap.TryGetValue(grade, out List<TraitDefinitionSO> list))
            {
                list =new List<TraitDefinitionSO>();

                traitGradeMap.Add(grade, list);
            }

            list.Add(trait);
        }


        foreach (OriginDefinitionSO origin in origins)
        {
            if (origin == null)
                continue;

            originMap[origin.id] = origin;
        }


        foreach (EquipmentAffixDefinitionSO affix in equipmentAffixes)
        {
            if (affix == null)
                continue;

            if (equipmentAffixDic.ContainsKey(affix.id))
            {
                Debug.LogWarning($"중복 EquipmentAffix id: {affix.id}");

                continue;
            }

            equipmentAffixDic.Add(affix.id, affix);
        }


        foreach (MonsterBaseSO monsterBase in monsterBases)
        {
            if (monsterBase == null)
                continue;

            if (monsterBaseMap.ContainsKey(monsterBase.id))
            {
                Debug.LogWarning(
                    $"중복 MonsterBase id: " +
                    $"{monsterBase.id} / " +
                    $"{monsterBase.monsterName}");

                continue;
            }

            monsterBaseMap.Add(monsterBase.id, monsterBase);
        }


        foreach (MonsterRoleSO monsterRole in monsterRoles)
        {
            if (monsterRole == null)
                continue;

            if (monsterRoleMap.ContainsKey(monsterRole.id))
            {
                Debug.LogWarning(
                    $"중복 MonsterRole id: " +
                    $"{monsterRole.id} / " +
                    $"{monsterRole.roleName}");

                continue;
            }

            monsterRoleMap.Add(
                monsterRole.id,
                monsterRole);

            if (monsterRole.baseMonster == null)
            {
                Debug.LogWarning(
                    $"MonsterRole에 BaseMonster 없음: " +
                    $"{monsterRole.id} / " +
                    $"{monsterRole.roleName}");

                continue;
            }

        }


        foreach (QuestStageDefinitionSO questStage in questStages)
        {
            if (questStage == null || string.IsNullOrEmpty(questStage.stageKey))
                continue;

            if (questStageMap.ContainsKey(questStage.stageKey))
            {
                Debug.LogWarning($"중복 QuestStage key: {questStage.stageKey}");
                continue;
            }

            questStageMap.Add(questStage.stageKey, questStage);
        }


        foreach (QuestEncounterDefinitionSO encounter in questEncounters)
        {
            if (encounter == null || string.IsNullOrEmpty(encounter.encounterId))
                continue;

            if (questEncounterMap.ContainsKey(encounter.encounterId))
            {
                Debug.LogWarning($"중복 QuestEncounter id: {encounter.encounterId}");
                continue;
            }

            questEncounterMap.Add(encounter.encounterId, encounter);
        }


        built = true;
    }


    public bool IsBuilt()
    {
        return built;
    }


    #region Equipment Affix

    public EquipmentAffixDefinitionSO GetEquipmentAffix(int id)
    {
        equipmentAffixDic.TryGetValue(id, out EquipmentAffixDefinitionSO affix);

        return affix;
    }


    public List<EquipmentAffixDefinitionSO> GetAllEquipmentAffixes()
    {
        return equipmentAffixes;
    }

    #endregion


    #region Status Effect

    public StatusEffectDefinitionSO GetStatusEffect(int id)
    {
        statusEffectMap.TryGetValue(id, out StatusEffectDefinitionSO result);

        return result;
    }

    #endregion


    #region Item / Equipment

    public ItemDefinitionSO GetItem(int uid)
    {
        itemMap.TryGetValue(uid, out ItemDefinitionSO item);

        return item;
    }


    public EquipmentDefinitionSO GetEquipment(int uid)
    {
        equipmentMap.TryGetValue(uid, out EquipmentDefinitionSO equipment);

        return equipment;
    }


    public List<EquipmentDefinitionSO> GetAllEquipments()
    {
        return new List<EquipmentDefinitionSO>(equipmentMap.Values);
    }


    public List<EquipmentDefinitionSO> GetEquipmentsByTier(int tier)
    {
        if (equipmentTierMap.TryGetValue(tier, out List<EquipmentDefinitionSO> list))
        {
            return new List<EquipmentDefinitionSO>(list);
        }

        return new List<EquipmentDefinitionSO>();
    }

    #endregion


    #region Skill

    public SkillDefinitionSO GetSkill(int uid)
    {
        skillMap.TryGetValue(uid, out SkillDefinitionSO skill);

        return skill;
    }


    public List<SkillDefinitionSO> GetAllSkills()
    {
        return skills;
    }


    public List<SkillDefinitionSO> GetRewardSkills()
    {
        List<SkillDefinitionSO> result = new List<SkillDefinitionSO>();

        foreach (SkillDefinitionSO skill in skills)
        {
            if (skill == null)
                continue;

            if (!skill.CanAppearInRewardPool())
                continue;

            result.Add(skill);
        }

        return result;
    }

    #endregion


    #region Trait

    public TraitDefinitionSO GetTrait(int id)
    {
        id = id switch
        {
            5010 or 5015 or 5021 or 5026 => 5009,
            5023 => 5002,
            5004 or 5011 or 5012 or 5017 or 5022 or 5027 => 5007,
            5001 or 5003 or 5006 or 5024 => 5014,
            5013 or 5019 or 5020 => 5018,
            1004 or 1005 => 1024,
            1008 => 1021,
            _ => id
        };
        traitMap.TryGetValue(id, out TraitDefinitionSO trait);

        return trait;
    }


    public List<TraitDefinitionSO> GetAllTraits()
    {
        return traits;
    }


    public List<TraitDefinitionSO> GetCharacterCreationTraits()
    {
        return characterCreationTraits;
    }


    public List<TraitDefinitionSO> GetTraitsByGrade(TraitGrade grade)
    {
        if (traitGradeMap.TryGetValue(grade, out List<TraitDefinitionSO> list))
        {
            return list;
        }

        return new List<TraitDefinitionSO>();
    }


    public TraitDefinitionSO GetRandomTraitByGrade(TraitGrade grade)
    {
        if (!traitGradeMap.TryGetValue(grade, out List<TraitDefinitionSO> list))
        {
            return null;
        }

        if (list.Count == 0)
            return null;

        return list[
            Random.Range(
                0,
                list.Count)];
    }

    #endregion


    #region Origin

    public OriginDefinitionSO GetOrigin(int id)
    {
        originMap.TryGetValue(id, out OriginDefinitionSO origin);

        return origin;
    }


    public List<OriginDefinitionSO> GetAllOrigins()
    {
        return origins;
    }

    public List<MercenaryDefinitionSO> GetMercenaryDefinitions()
    {
        return mercenaryDefinitions;
    }

    #endregion


    #region Quest Encounter

    public QuestEncounterDefinitionSO GetQuestEncounter(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
            return null;

        questEncounterMap.TryGetValue(encounterId, out QuestEncounterDefinitionSO encounter);
        return encounter;
    }


    public List<QuestEncounterDefinitionSO> GetQuestEncounters(
        string stageKey,
        QuestRouteNodeType nodeType)
    {
        List<QuestEncounterDefinitionSO> result = new List<QuestEncounterDefinitionSO>();

        foreach (QuestEncounterDefinitionSO encounter in questEncounters)
        {
            if (encounter != null && encounter.CanAppear(stageKey, nodeType))
                result.Add(encounter);
        }

        return result;
    }

    #endregion


    #region Monster

    public MonsterBaseSO GetMonsterBase(int id)
    {
        monsterBaseMap.TryGetValue(id, out MonsterBaseSO monsterBase);

        return monsterBase;
    }


    public MonsterRoleSO GetMonsterRole(int id)
    {
        monsterRoleMap.TryGetValue(id, out MonsterRoleSO monsterRole);

        return monsterRole;
    }


    public List<MonsterBaseSO> GetAllMonsterBases()
    {
        return monsterBases;
    }


    public List<MonsterRoleSO> GetAllMonsterRoles()
    {
        return monsterRoles;
    }


    public List<MonsterRoleSO> GetMonsterRolesByTypeAndTags(
        CharacterType characterType,
        List<string> monsterTags)
    {
        List<MonsterRoleSO> result = new List<MonsterRoleSO>();

        foreach (MonsterRoleSO monsterRole in monsterRoles)
        {
            if (monsterRole == null || monsterRole.baseMonster == null)
                continue;

            if (monsterRole.characterType != characterType)
                continue;

            if (!HasAnyMonsterTag(monsterRole.baseMonster.tags, monsterTags))
                continue;

            result.Add(monsterRole);
        }

        return result;
    }


    private bool HasAnyMonsterTag(List<string> monsterTags, List<string> stageTags)
    {
        if (stageTags == null || stageTags.Count == 0)
            return true;

        if (monsterTags == null || monsterTags.Count == 0)
            return false;

        foreach (string tag in stageTags)
        {
            if (monsterTags.Contains(tag))
                return true;
        }

        return false;
    }

    #endregion


    #region Quest Stage

    public QuestStageDefinitionSO GetQuestStage(string stageKey)
    {
        if (string.IsNullOrEmpty(stageKey))
            return null;

        questStageMap.TryGetValue(stageKey, out QuestStageDefinitionSO questStage);
        return questStage;
    }


    public List<QuestStageDefinitionSO> GetAllQuestStages()
    {
        return questStages;
    }

    #endregion
}
