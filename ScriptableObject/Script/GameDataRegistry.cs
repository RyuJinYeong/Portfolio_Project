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

    [Header("Origins")]
    public List<OriginDefinitionSO> origins = new();

    [Header("Status Effects")]
    [SerializeField] private List<StatusEffectDefinitionSO> statusEffects = new();

    private readonly Dictionary<int, StatusEffectDefinitionSO> statusEffectMap = new();
    private readonly Dictionary<int, ItemDefinitionSO> itemMap = new();
    private readonly Dictionary<int, EquipmentDefinitionSO> equipmentMap = new();
    private readonly Dictionary<int, SkillDefinitionSO> skillMap = new();
    private readonly Dictionary<int, TraitDefinitionSO> traitMap = new();
    private readonly Dictionary<TraitGrade, List<TraitDefinitionSO>> traitGradeMap = new();
    private readonly Dictionary<int, OriginDefinitionSO> originMap = new();

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
        itemMap.Clear();
        equipmentMap.Clear();
        skillMap.Clear();
        traitMap.Clear();
        traitGradeMap.Clear();
        originMap.Clear();
        statusEffectMap.Clear();

        foreach (StatusEffectDefinitionSO status in statusEffects)
        {
            if (status == null)
                continue;

            if (statusEffectMap.ContainsKey(status.id))
            {
                Debug.LogWarning($"중복 상태이상 id: {status.id} / {status.statusName}");
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
                equipmentMap[item.uid] = equipment;
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
                list = new List<TraitDefinitionSO>();
                traitGradeMap[grade] = list;
            }

            list.Add(trait);
        }

        foreach (OriginDefinitionSO origin in origins)
        {
            if (origin == null)
                continue;

            originMap[origin.id] = origin;
        }

        equipmentAffixDic.Clear();

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

        built = true;
    }

    public bool IsBuilt()
    {
        return built;
    }

    [SerializeField] private List<EquipmentAffixDefinitionSO> equipmentAffixes = new();

    private Dictionary<int, EquipmentAffixDefinitionSO> equipmentAffixDic = new();

    public EquipmentAffixDefinitionSO GetEquipmentAffix(int id)
    {
        if (equipmentAffixDic == null)
            return null;

        equipmentAffixDic.TryGetValue(id, out EquipmentAffixDefinitionSO affix);
        return affix;
    }

    public List<EquipmentAffixDefinitionSO> GetAllEquipmentAffixes()
    {
        return equipmentAffixes;
    }

    public StatusEffectDefinitionSO GetStatusEffect(int id)
    {
        if (statusEffectMap == null)
            return null;

        statusEffectMap.TryGetValue(id, out StatusEffectDefinitionSO result);
        return result;
    }

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

    public SkillDefinitionSO GetSkill(int uid)
    {
        skillMap.TryGetValue(uid, out SkillDefinitionSO skill);
        return skill;
    }

    public TraitDefinitionSO GetTrait(int id)
    {
        traitMap.TryGetValue(id, out TraitDefinitionSO trait);
        return trait;
    }

    public OriginDefinitionSO GetOrigin(int id)
    {
        originMap.TryGetValue(id, out OriginDefinitionSO origin);
        return origin;
    }

    public List<OriginDefinitionSO> GetAllOrigins()
    {
        return origins;
    }

    public List<TraitDefinitionSO> GetTraitsByGrade(TraitGrade grade)
    {
        if (traitGradeMap.TryGetValue(grade, out List<TraitDefinitionSO> list))
            return list;

        return new List<TraitDefinitionSO>();
    }

    public TraitDefinitionSO GetRandomTraitByGrade(TraitGrade grade)
    {
        if (!traitGradeMap.TryGetValue(grade, out List<TraitDefinitionSO> list))
            return null;

        if (list.Count == 0)
            return null;

        return list[Random.Range(0, list.Count)];
    }
}