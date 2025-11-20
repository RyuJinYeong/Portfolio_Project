using SoftKitty.InventoryEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

[System.Serializable]
public class SkillBase : Item  // 에셋의 Item 클래스를 상속받음
{
    public bool QuickSlot { get; set; } = false; // 스킬의 퀵슬롯 등록 여부
    public double ActivationSpeed { get; set; } // 스킬 발동 속도
    public float DamageMultiplier { get; set; } // 스킬의 공격력 배수
    public int StaminaCost { get; set; }  // 지구력 소모
    public int MentalCost { get; set; }  // 정신력 소모

    public bool IsCounterSkill { get; set; } // 대응 스킬 여부

    public bool IsStatusEffectSkill { get; set; } = false; // 상태이상 유발 여부

    public bool IsRangedSkill { get; set; } // 원거리 스킬 여부
    public bool IsBowSkill { get; set; } // 활 스킬 여부

    public bool CanUse { get; set; } // 사용 가능 여부
    public bool IsOffHand { get; set; } = false; // 보조무기 사용 여부

    public SkillType Type { get; set; } // 스킬 타입 (물리/마법)
    public SkillAttribute Attribute { get; set; } // 스킬 세부 속성

    public string IconAddress { get; set; } // Addressables에서 아이콘을 찾을 주소    


    // UI 바인딩 시점에 캐시에서 아이콘 꺼내기
    public bool EnsureIconFromCache()
    {
        if (icon != null) return false;
        if (string.IsNullOrEmpty(IconAddress)) return false;

        var tex = IconStore.GetOrNull(IconAddress);
        if (tex != null) { icon = tex; return true; }
        return false; // 아직 미로딩 → 자리표시자 사용
    }


    // 진화 및 습득 조건 관련 필드    
    public bool IsEvolvableSkill { get; set; } // 진화 가능한 스킬 여부
    public bool IsConditionalSkill { get; set; } // 습득 조건이 있는 스킬 여부

    // 습득 조건 관련 필드들
    public Dictionary<string, int> RequiredStats { get; set; } // 스킬 습득에 필요한 스탯 (예: 힘, 지능 등)
    public List<TraitBase> RequiredTraits { get; set; } // 스킬 습득에 필요한 특정 특성 리스트

    // 진화 조건 관련 필드들
    public int SkillUseCount { get; set; } // 스킬 사용 횟수
    public int SkillKillCount { get; set; } // 스킬로 적을 처치한 횟수
    public int SkillDamageCount { get; set; } // 스킬로 가한 총 데미지
    public int EvolRequiredUseCount { get; set; } // 진화에 필요한 스킬 사용 횟수
    public int EvolRequiredKillCount { get; set; } // 진화에 필요한 적 처치 횟수
    public int EvolRequiredDamageCount { get; set; } // 진화에 필요한 총 데미지
    public List<TraitBase> EvolRequiredTraits { get; set; } // 진화에 필요한 특성 리스트

    // 기본 생성자 (스킬의 핵심 정보만 초기화)
    public SkillBase(string name, double activationSpeed, float damageMultiplier, int staminaCost, int mentalCost, SkillType type, SkillAttribute attribute, string iconAddress)
    {
        this.name = name;
        ActivationSpeed = activationSpeed;
        DamageMultiplier = damageMultiplier;
        StaminaCost = staminaCost;
        MentalCost = mentalCost;
        Type = type;
        Attribute = attribute;
        IconAddress = iconAddress;

        // 기본 Item 필드 설정
        this.maxiumStack = 1;
        this.type = 4;
        this.weight = 0;
        this.dropRates = 0;
        this.deletable = false;
        this.visible = false;
        this.tags = new List<string>();
        tags.Add("Skills");

        // 조건 초기화
        RequiredStats = new Dictionary<string, int>();
        RequiredTraits = new List<TraitBase>();
        EvolRequiredTraits = new List<TraitBase>();
    }

    // 습득 조건을 추가하는 메서드
    public void SetAcquisitionConditions(Dictionary<string, int> stats, List<TraitBase> traits)
    {
        IsConditionalSkill = true;
        RequiredStats = stats ?? new Dictionary<string, int>();
        RequiredTraits = traits ?? new List<TraitBase>();
    }

    // 진화 조건을 추가하는 메서드
    public void SetEvolutionConditions(int useCount, int killCount, int damageCount, List<TraitBase> traits)
    {
        IsEvolvableSkill = true;
        EvolRequiredUseCount = useCount;
        EvolRequiredKillCount = killCount;
        EvolRequiredDamageCount = damageCount;
        EvolRequiredTraits = traits ?? new List<TraitBase>();
    }

    // 습득 조건을 만족하는지 확인하는 메서드
    public bool CanBeAcquiredBy(CharacterData character)
    {
        // 스탯 조건 확인
        foreach (var condition in RequiredStats)
        {
            var stats = character.FinalStats.GetStats();
            if (!stats.TryGetValue(condition.Key, out var val) || val < condition.Value)
                return false;
        }

        // 특성 조건 확인
        foreach (var trait in RequiredTraits)
        {
            if (!character.Traits.Contains(trait))
            {
                return false; // 특성 조건 미충족
            }
        }

        return true; // 모든 조건 충족
    }

    // 스킬 진화 여부를 확인하는 메서드
    public void OnSkillUsed(int damage, CharacterData user, CharacterData target)
    {
        if (IsEvolvableSkill)
        {
            SkillUseCount++;
            SkillDamageCount += damage;

            if (target.CurrentHp <= 0) // 타겟이 죽었을 경우
            {
                SkillKillCount++;
            }
            CanEvolve(user);
        }
    }

    // 스킬 진화 조건 확인 메서드
    public bool CanEvolve(CharacterData character)
    {
        // 스킬 사용 횟수, 처치 횟수, 총 데미지 조건 확인
        if (SkillUseCount < EvolRequiredUseCount || SkillKillCount < EvolRequiredKillCount || SkillDamageCount < EvolRequiredDamageCount)
        {
            return false; // 조건 미충족
        }

        // 진화에 필요한 특성 조건 확인
        foreach (var trait in EvolRequiredTraits)
        {
            if (!character.Traits.Contains(trait))
            {
                return false; // 특성 조건 미충족
            }
        }

        return true; // 모든 조건 충족
    }

    // 스킬 진화 메서드
    public void EvolveSkill(CharacterData user)
    {
        // UID + 1로 다음 스킬을 찾음
        int nextSkillUid = this.uid + 1;
        
        if (ItemManager.itemDic[nextSkillUid].Copy() is SkillBase nextSkill)
        {
            SkillManager.RemoveSkill(user, this);
            SkillManager.AddSkill(user, nextSkill); // 진화된 스킬로 대체
            Debug.Log($"{name} 스킬 -> {nextSkill.name}");
        }
        else
        {
            Debug.LogWarning($"UID {nextSkillUid}에 해당하는 스킬을 찾을 수 없습니다.");
        }
    }


    // Item 클래스의 Copy 메서드 재정의
    public override Item Copy()
    {
        SkillBase copiedSkill = new SkillBase(name, ActivationSpeed, DamageMultiplier, StaminaCost, MentalCost, Type, Attribute, IconAddress)
        {
            uid = this.uid,
            maxiumStack = this.maxiumStack,
            type = this.type,
            weight = this.weight,
            dropRates = this.dropRates,
            deletable = this.deletable,
            CanUse = this.CanUse,
            IsCounterSkill = this.IsCounterSkill,
            IsEvolvableSkill = this.IsEvolvableSkill,
            IsConditionalSkill = this.IsConditionalSkill,
            IsRangedSkill = this.IsRangedSkill,
            IsBowSkill = this.IsBowSkill,
            IsOffHand = this.IsOffHand,
            SkillUseCount = this.SkillUseCount,
            SkillKillCount = this.SkillKillCount,
            SkillDamageCount = this.SkillDamageCount,
            EvolRequiredUseCount = this.EvolRequiredUseCount,
            EvolRequiredKillCount = this.EvolRequiredKillCount,
            EvolRequiredDamageCount = this.EvolRequiredDamageCount,
            QuickSlot = this.QuickSlot,
            IsStatusEffectSkill = this.IsStatusEffectSkill
        };

        // 습득 조건 및 진화 조건 복사
        foreach (var condition in RequiredStats)
        {
            copiedSkill.RequiredStats.Add(condition.Key, condition.Value);
        }

        copiedSkill.RequiredTraits.AddRange(RequiredTraits);
        copiedSkill.EvolRequiredTraits.AddRange(EvolRequiredTraits);

        // 아이콘 텍스처는 참조 공유(중복 메모리 방지)
        copiedSkill.icon = this.icon;

        return copiedSkill;
    }
}
