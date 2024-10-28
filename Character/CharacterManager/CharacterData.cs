using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.TextCore.Text;
using SoftKitty.InventoryEngine;

public class CharacterStats // 기본 캐릭터 스탯
{
    // 프라이빗 필드
    private int strength;
    private int dexterity;
    private int speed;
    private int intelligence;
    private int wisdom;
    private int health;
    private int endurance;

    private int detection;
    private int insight;

    private float attackSpeed;
    private float castSpeed;

    private float weaponAttackSpeedMultiplier;
    private float weaponCastSpeedMultiplier;

    private int maxHp;
    private int currentHp;
    private int maxMentality;
    private int maxStamina;
    private int currentMentality;
    private int currentStamina;

    private int staminaRecovery;
    private int mentalityRecovery;

    // 프로퍼티
    public int Strength
    {
        get => strength;
        set => strength = Mathf.Max(0, value); // 최소값 0 보장
    }

    public int Dexterity
    {
        get => dexterity;
        set => dexterity = Mathf.Max(0, value);
    }

    public int Speed
    {
        get => speed;
        set => speed = Mathf.Max(0, value);
    }

    public int Intelligence
    {
        get => intelligence;
        set => intelligence = Mathf.Max(0, value);
    }

    public int Wisdom
    {
        get => wisdom;
        set => wisdom = Mathf.Max(0, value);
    }

    public int Health
    {
        get => health;
        set => health = Mathf.Max(0, value);
    }

    public int Endurance
    {
        get => endurance;
        set => endurance = Mathf.Max(0, value);
    }

    public int Detection
    {
        get => detection;
        set => detection = Mathf.Max(0, value);
    }

    public int Insight
    {
        get => insight;
        set => insight = Mathf.Max(0, value);
    }

    public float AttackSpeed
    {
        get => attackSpeed;
        set => attackSpeed = Mathf.Max(0, value);
    }

    public float CastSpeed
    {
        get => castSpeed;
        set => castSpeed = Mathf.Max(0, value);
    }

    public float WeaponAttackSpeedMultiplier
    {
        get => weaponAttackSpeedMultiplier;
        set => weaponAttackSpeedMultiplier = Mathf.Max(0, value);
    }

    public float WeaponCastSpeedMultiplier
    {
        get => weaponCastSpeedMultiplier;
        set => weaponCastSpeedMultiplier = Mathf.Max(0, value);
    }

    public int MaxHp
    {
        get => maxHp;
        set => maxHp = Mathf.Max(0, value);
    }

    public int CurrentHp
    {
        get => currentHp;
        set => currentHp = Mathf.Clamp(value, 0, MaxHp); // 0에서 MaxHp 사이로 제한
    }

    public int MaxMentality
    {
        get => maxMentality;
        set => maxMentality = Mathf.Max(0, value);
    }

    public int MaxStamina
    {
        get => maxStamina;
        set => maxStamina = Mathf.Max(0, value);
    }

    public int CurrentMentality
    {
        get => currentMentality;
        set => currentMentality = Mathf.Clamp(value, 0, MaxMentality);
    }

    public int CurrentStamina
    {
        get => currentStamina;
        set => currentStamina = Mathf.Clamp(value, 0, MaxStamina);
    }

    public int StaminaRecovery
    {
        get => staminaRecovery;
        set => staminaRecovery = Mathf.Max(0, value);
    }

    public int MentalityRecovery
    {
        get => mentalityRecovery;
        set => mentalityRecovery = Mathf.Max(0, value);
    }

    #region 속성별저항력 ( 백분위 )

    public int FireResistance { get; set; }
    public int WaterResistance { get; set; }
    public int EarthResistance { get; set; }
    public int WindResistance { get; set; }
    public int PierceResistance { get; set; }
    public int SlashResistance { get; set; }
    public int SmashResistance { get; set; }

    // 필요에 따라 속성 추가...

    #endregion

    #region 속성별 특화 ( 백분위 )

    public int FireAffinity { get; set; } // 불 속성 특화
    public int WaterAffinity { get; set; } // 물 속성 특화
    public int EarthAffinity { get; set; } // 땅 속성 특화
    public int WindAffinity { get; set; } // 바람 속성 특화
    public int PierceAffinity { get; set; } // 관통 특화
    public int SlashAffinity { get; set; } // 참격 특화
    public int SmashAffinity { get; set; } // 타격 특화

    #endregion


    #region 공격력/방어력
    public uint PhysicalAttack { get; set; }
    public uint MagicalAttack { get; set; }
    public int PhysicalDefense { get; set; }
    public int MagicalDefense { get; set; }
    #endregion


    public int Lv { get; set; }
    public int Exp { get; set; }


    #region 생성자
    public CharacterStats()
    {

    }

    public CharacterStats(int str, int dex, int spd, int intl, int wis, int hth, int end)
    {
        Strength = str;
        Dexterity = dex;
        Speed = spd;
        Intelligence = intl;
        Wisdom = wis;
        Health = hth;
        Endurance = end;
        Lv = 1;
    }
    #endregion

    #region 연산자 오버로딩 구현부
    public static CharacterStats operator +(CharacterStats a, CharacterStats b)
    {
        if (a == null) a = new CharacterStats();
        if (b == null) b = new CharacterStats();

        return new CharacterStats
        {
            Strength = a.Strength + b.Strength,
            Dexterity = a.Dexterity + b.Dexterity,
            Speed = a.Speed + b.Speed,
            Intelligence = a.Intelligence + b.Intelligence,
            Wisdom = a.Wisdom + b.Wisdom,
            Health = a.Health + b.Health,
            Endurance = a.Endurance + b.Endurance,
            Detection = a.Detection + b.Detection,
            Insight = a.Insight + b.Insight,
            MaxMentality = a.MaxMentality + b.MaxMentality,
            MaxStamina = a.MaxStamina + b.MaxStamina,
            StaminaRecovery = a.StaminaRecovery + b.StaminaRecovery,
            MentalityRecovery = a.MentalityRecovery + b.MentalityRecovery,
            MaxHp = a.MaxHp + b.MaxHp,
            FireResistance = a.FireResistance + b.FireResistance,
            WaterResistance = a.WaterResistance + b.WaterResistance,
            EarthResistance = a.EarthResistance + b.EarthResistance,
            WindResistance = a.WindResistance + b.WindResistance,
            PierceResistance = a.PierceResistance + b.PierceResistance,
            SlashResistance = a.SlashResistance + b.SlashResistance,
            SmashResistance = a.SmashResistance + b.SmashResistance,
            PhysicalAttack = a.PhysicalAttack + b.PhysicalAttack,
            MagicalAttack = a.MagicalAttack + b.MagicalAttack,
            PhysicalDefense = a.PhysicalDefense + b.PhysicalDefense,
            MagicalDefense = a.MagicalDefense + b.MagicalDefense,
            AttackSpeed = a.AttackSpeed + b.AttackSpeed,
            CastSpeed = a.CastSpeed + b.CastSpeed,
            WeaponAttackSpeedMultiplier = a.WeaponAttackSpeedMultiplier + b.WeaponAttackSpeedMultiplier,
            WeaponCastSpeedMultiplier = a.WeaponCastSpeedMultiplier + b.WeaponCastSpeedMultiplier,
            FireAffinity = a.FireAffinity + b.FireAffinity,
            WaterAffinity = a.WaterAffinity + b.WaterAffinity,
            EarthAffinity = a.EarthAffinity + b.EarthAffinity,
            WindAffinity = a.WindAffinity + b.WindAffinity,
            PierceAffinity = a.PierceAffinity + b.PierceAffinity,
            SlashAffinity = a.SlashAffinity + b.SlashAffinity,
            SmashAffinity = a.SmashAffinity + b.SmashAffinity,
            Lv = a.Lv + b.Lv,
            Exp = a.Exp + b.Exp
        };
    }

    public static CharacterStats operator -(CharacterStats a, CharacterStats b)
    {
        if (a == null) a = new CharacterStats();
        if (b == null) b = new CharacterStats();

        return new CharacterStats
        {
            Strength = a.Strength - b.Strength,
            Dexterity = a.Dexterity - b.Dexterity,
            Speed = a.Speed - b.Speed,
            Intelligence = a.Intelligence - b.Intelligence,
            Wisdom = a.Wisdom - b.Wisdom,
            Health = a.Health - b.Health,
            Endurance = a.Endurance - b.Endurance,
            Detection = a.Detection - b.Detection,
            Insight = a.Insight - b.Insight,
            MaxMentality = a.MaxMentality - b.MaxMentality,
            MaxStamina = a.MaxStamina - b.MaxStamina,
            StaminaRecovery = a.StaminaRecovery - b.StaminaRecovery,
            MentalityRecovery = a.MentalityRecovery - b.MentalityRecovery,
            MaxHp = a.MaxHp - b.MaxHp,
            FireResistance = a.FireResistance - b.FireResistance,
            WaterResistance = a.WaterResistance - b.WaterResistance,
            EarthResistance = a.EarthResistance - b.EarthResistance,
            WindResistance = a.WindResistance - b.WindResistance,
            PierceResistance = a.PierceResistance - b.PierceResistance,
            SlashResistance = a.SlashResistance - b.SlashResistance,
            SmashResistance = a.SmashResistance - b.SmashResistance,
            PhysicalAttack = a.PhysicalAttack - b.PhysicalAttack,
            MagicalAttack = a.MagicalAttack - b.MagicalAttack,
            PhysicalDefense = a.PhysicalDefense - b.PhysicalDefense,
            MagicalDefense = a.MagicalDefense - b.MagicalDefense,
            AttackSpeed = a.AttackSpeed - b.AttackSpeed,
            CastSpeed = a.CastSpeed - b.CastSpeed,
            WeaponAttackSpeedMultiplier = a.WeaponAttackSpeedMultiplier - b.WeaponAttackSpeedMultiplier,
            WeaponCastSpeedMultiplier = a.WeaponCastSpeedMultiplier - b.WeaponCastSpeedMultiplier,
            FireAffinity = a.FireAffinity - b.FireAffinity,
            WaterAffinity = a.WaterAffinity - b.WaterAffinity,
            EarthAffinity = a.EarthAffinity - b.EarthAffinity,
            WindAffinity = a.WindAffinity - b.WindAffinity,
            PierceAffinity = a.PierceAffinity - b.PierceAffinity,
            SlashAffinity = a.SlashAffinity - b.SlashAffinity,
            SmashAffinity = a.SmashAffinity - b.SmashAffinity,
            Lv = a.Lv - b.Lv,
            Exp = a.Exp - b.Exp
        };
    }
    #endregion
}

public class CustomizationData
{
    public bool IsMale { get; set; }
    public int HairType { get; set; }
    public int EyebrowsType { get; set; }
    public int EyeType { get; set; }
    public int MouthType { get; set; }
    public int BeardType { get; set; }

    public int HairColor { get; set; }
    public int SkinTone { get; set; }

    // 기타 커스터마이징 옵션들
}

public class CharacterData
{
    [JsonIgnore] // JSON 직렬화 시 무시 - Sprite는 DB 공간 낭비가 심해서 인게임에서 처리
    public Sprite Portrait; // 캐릭터 초상화 - 게임 실행시 게임씬에서 렌더이미지를 촬영하여 Sprite로 변환 후 할당

    public CustomizationData customizationData; // 캐릭터 커스터마이징 데이터 - 이 데이터를 기반으로 생성된 베이스 캐릭터에 커스터마이징 적용
    public CharacterType Type {  get; set; } = CharacterType.Character;

    public CharacterStats BaseStats { get; set; } // 기본 스탯 + 특성으로 증감된 스탯
    public CharacterStats ModifiedStats { get; set; } = new CharacterStats(); // 증감 스탯 - 장비, 버프 등으로 변화한 스탯
    public CharacterStats FinalStats { get; set; } // 최종 스탯

    [JsonIgnore]
    public CharacterStats tempStats = new CharacterStats(); // 임시스탯

    [JsonIgnore]
    public List<Attribute> updatedAttributes;

    public float PhysicalDamageMultiplier { get; set; } = 1.0f; // 물리 데미지 배율 - 특성, 상태이상 등으로 변화 (기본 1.0f) 
    public float MagicalDamageMultiplier { get; set; } = 1.0f; // 마법 데미지 배율 - 특성, 상태이상 등으로 변화 (기본 1.0f) 
    public float AttackSpeedMultiplier { get; set; } = 1.0f; // 공격 속도 배율 - 특성, 상태이상 등으로 변화 (기본 1.0f) 
    public float CastSpeedMultiplier { get; set; } = 1.0f; // 시전 속도 배율 - 특성, 상태이상 등으로 변화 (기본 1.0f) 

    public int BonusStatpoint { get; set; } // 투자 가능 스탯
    public bool IsAlive { get; set; } // 캐릭터의 생존유무
    public bool IsMine { get; set; } // 캐릭터 아군여부

    public int PhysicalArmor { get; set; } // 물리 방어도
    public int MagicalArmor { get; set; } // 마법 방어도

    // 캐릭터가 보유하고 있는 스킬과 특성 리스트
    public List<TraitBase> Traits { get; set; }// 캐릭터의 특성 목록
    public List<SkillBase> Skills { get; set; }// 습득한 스킬 목록

    // 캐릭터에 적용되어있는 상태이상
    public List<StatusEffect> StatusEffects { get; set; } // 적용중인 상태이상 목록

    public List<SkillAttribute> AvailableAttributes { get; private set; } = new List<SkillAttribute>(); // 캐릭터가 장착한 무기의 세부 속성 리스트

    // 장비로 인해 습득한 스킬과 특성 리스트
    public List<TraitBase> EquipmentTraits { get; set; }
    public List<SkillBase> EquipmentSkills { get; set; }

    //캐릭터 인벤토리, 장비창 관리      
    public InventoryHolder CharacterInventory;
    public InventoryHolder CharacterEquipment;
    


    //캐릭터의 장비
    public Equipment Helmet { get; set; }
    public Equipment Armor { get; set; }
    public Equipment Gloves { get; set; }
    public Equipment Shoes { get; set; }    
    public Equipment Ring1 { get; set; }   
    public Equipment Ring2 { get; set; }
    public Equipment Earring1 { get; set; }
    public Equipment Earring2 { get; set; }
    public Equipment Necklace { get; set; }
    public Equipment Weapon { get; set; }
    public Equipment SubWeapon { get; set; }


    // 추가 정보
    public Origin origin; // 출신지
    public string originName; // 출신지 명

    public Personality personality = Personality.Simple; // 성향
    public int Belonging { get; set; } // 소속감
    public int Morale { get; set; } // 사기


    public string ID; // 캐릭터 식별을 위한 고유 ID
    public string Name; // 캐릭터 이름 - 중복 허용

    // 장비 리스트를 반환하는 메서드 - 캐릭터의 장착중인 모든 장비 순회를 위한 메서드
    public IEnumerable<Equipment> GetEquipments()
    {
        return new List<Equipment> { Helmet, Armor, Gloves, Shoes, Ring1, Ring2, Earring1, Earring2, Necklace, Weapon, SubWeapon };
    }

    // 스킬 중복 체크 함수
    public bool IsSkillUnique(SkillBase skill, Equipment excludeEquipment = null)
    {
        return GetEquipments().All(e => e == null || !(e is IHasSkill hasSkills && hasSkills.Skills.Contains(skill)) || e == excludeEquipment);
    }    

    // 특성 중복 체크 함수
    public bool IsTraitUnique(TraitBase trait, Equipment excludeEquipment = null)
    {
        return GetEquipments().All(e => e == null || !(e is IHasTrait hasTraits && hasTraits.Traits.Contains(trait)) || e == excludeEquipment);
    }

    // 무기 착용 가능 여부
    public bool CanEquipMainWeapon(Weapon weapon)
    {
        foreach (TraitBase trait in Traits)
        {
            if (trait is OneArmedTrait && (weapon.weaponTags.Contains(WeaponTag.TwoHanded)))
            {
                return false;
            }
        }
        return true;
    }

    public bool CanEquipSubWeapon()
    {
        foreach (TraitBase trait in Traits)
        {
            if (trait is OneArmedTrait)
            {
                return false;
            }
        }
        return true;
    }

    //모든 특성 효과 제거
    public void RemoveAllTraits(CharacterManager manager)
    {
        // 1단계: % 연산 특성 적용
        foreach (var trait in Traits)
        {
            if (trait.IsPercentage) 
            {
                trait.RemoveTrait(manager);
            }
        }

        // 2단계: 고정값 연산 적용
        foreach (var trait in Traits)
        {
            if (!trait.IsPercentage) 
            {
                trait.RemoveTrait(manager);
            }
        }
    }

    //모든 특성 효과 적용
    public void ApplyAllTraits(CharacterManager manager)
    {
        // 1단계: 고정값 연산 및 기타 특성 적용
        foreach (var trait in Traits)
        {
            if (!trait.IsPercentage) 
            {
                trait.ApplyTrait(manager);
            }
        }

        // 2단계: % 연산 특성 적용
        foreach (var trait in Traits)
        {
            if (trait.IsPercentage) 
            {
                trait.ApplyTrait(manager);
            }
        }
    }

    #region 생성자 오버로딩 구현부
    public CharacterData(CharacterStats baseStats) // 베이스 스탯을 받아서 나머지 값을 초기화하는 생성자.
    {
        BaseStats = baseStats;
        ModifiedStats = new CharacterStats();
        FinalStats = new CharacterStats();
        Traits = new List<TraitBase>();
        Skills = new List<SkillBase>();

        UpdateFinalStats();
    }

    public CharacterData() // 기본생성자
    {
        BaseStats = new CharacterStats();
        ModifiedStats = new CharacterStats();
        FinalStats = new CharacterStats();
        Traits = new List<TraitBase>();
        Skills = new List<SkillBase>();
    }

    //출신지별 캐릭터 생성을 위한 생성자
    public CharacterData(Origin origin, string originName, CharacterStats baseStats, Equipment helmet, Equipment armor, Equipment gloves, Equipment shoes, Equipment weapon, Equipment subWeapon, List<TraitBase> traits, List<SkillBase> skills)
    {
        this.origin = origin;
        this.originName = originName;
        BaseStats = baseStats;
        Traits = traits;
        Skills = skills;
        Helmet = helmet;
        Armor = armor;
        Gloves = gloves;
        Shoes = shoes;
        Weapon = weapon;
        SubWeapon = subWeapon;
    }
    #endregion    

    public void UpdateFinalStats() // 최종 스탯 계산 - 장비 탈착, 특성 추가 혹은 삭제, 버프 획득 등의 상황에 호출해줘야함
    {
        FinalStats = BaseStats + ModifiedStats; // 연산자 오버로딩으로 Class단위 연산 수행        
        tempStats = FinalStats;

        // 기타 스탯 계산        
        FinalStats = CalcStat(tempStats);

        // 최종 스탯을 업데이트한 후, Attribute로 변환하여 필요한 곳에서 사용할 수 있도록 동기화
        updatedAttributes = StatsConverter.ConvertStatsToAttributes(FinalStats);
    }

    // 캐릭터의 스킬을 초기화하고 아이콘을 로드하는 메서드
    public void InitializeSkills()
    {
        foreach (SkillBase skill in Skills)
        {
            skill.LoadIcon();
        }
    }

    public CharacterStats CalcStat(CharacterStats stats)
    {
        // 중간 값을 저장하는 변수

        int baseMaxMentality = stats.MaxMentality + stats.Wisdom / 5 + 1;
        int baseMaxStamina = stats.MaxStamina + stats.Speed / 5 + 1;

        int baseStaminaRecovery = stats.StaminaRecovery + stats.Health / 10 + 1;
        int baseMentalityRecovery = stats.MentalityRecovery + stats.Intelligence / 10 + 1;
        int basePhysicalDefense = stats.PhysicalDefense + stats.Endurance / 5;
        int baseMagicalDefense = stats.MagicalDefense + stats.Endurance / 5;
        uint basePhysicalAttack = stats.PhysicalAttack;
        uint baseMagicalAttack = stats.MagicalAttack + (uint)stats.Intelligence; // 지능 지수에 따라 증가. - 장착 무기 타입 상관 없이 적용
        int baseDetection = stats.Detection + (int)(stats.Dexterity * ((double)(stats.Dexterity / 10.0)) + stats.Speed * ((double)(stats.Speed / 10.0)));
        int baseInsight =  stats.Insight + (int)(stats.Wisdom * ((double)(stats.Wisdom / 10.0)) + stats.Intelligence * ((double)(stats.Intelligence / 10.0)));
        int baseMaxHp = 15 + stats.Lv * 5 + stats.Health * 3 + stats.MaxHp;
        float baseAtkSpd = (1.0f + stats.Speed * 0.01f); // 기본 속도 1.0 + 속도 스탯 * 0.01
        if (Weapon != null)
            baseAtkSpd *= this.Weapon.StatModifiers.WeaponAttackSpeedMultiplier; // * 무기 속도 배율
        float baseCastSpd;

        if (stats.WeaponCastSpeedMultiplier == 0) // 장착중인 무기가 시전속도 능력치가 없을 경우
        {
            baseCastSpd = ((1.0f + stats.Wisdom * 0.01f) * 0.8f); // (기본 속도 1.0 + 지혜 * 0.01) * 0.8 ( 시전속도 20% 감소 )
        }
        else
        {            
            baseCastSpd = (1.0f + stats.Wisdom * 0.01f) * stats.WeaponCastSpeedMultiplier; // (기본 속도 1.0 + 지혜 * 0.01) * 무기 배율, 시전속도는 보조무기의 영향도 받게 구성
        }

        // 장착중인 무기 카테고리 구분 후 해당 스탯 적용
        if (this.Weapon is Weapon heavyWeapon && heavyWeapon.WeaponCategory == WeaponCategory.HeavyWeapon)
        {
            basePhysicalAttack += (uint)stats.Strength;
        }
        else if (this.Weapon is Weapon lightWeapon && lightWeapon.WeaponCategory == WeaponCategory.LightWeapon)
        {
            if (stats.Strength > stats.Dexterity)
                basePhysicalAttack += (uint)stats.Strength;
            else
                basePhysicalAttack += (uint)stats.Dexterity;
        }

        // 새로운 값으로 업데이트
        stats.StaminaRecovery = baseStaminaRecovery;
        stats.MentalityRecovery = baseMentalityRecovery;
        stats.PhysicalDefense = basePhysicalDefense;
        stats.MagicalDefense = baseMagicalDefense;
        stats.PhysicalAttack = basePhysicalAttack;
        stats.MagicalAttack = baseMagicalAttack;
        stats.Detection = baseDetection;
        stats.Insight = baseInsight;
        stats.AttackSpeed = baseAtkSpd;
        stats.CastSpeed = baseCastSpd;
        stats.MaxMentality = baseMaxMentality;
        stats.MaxStamina = baseMaxStamina;
        stats.MaxHp = baseMaxHp;

        return stats;
    }
}