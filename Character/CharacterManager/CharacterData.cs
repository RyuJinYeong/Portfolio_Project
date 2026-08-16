using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CharacterStats // 기본 캐릭터 스탯
{
    #region 기본 스탯 필드
    public int Strength; // 힘
    public int Dexterity; // 기교
    public int Speed; // 속도
    public int Intelligence; // 지능
    public int Wisdom; // 지혜
    public int Health; // 건강
    public int Vitality; // 활력
    public int Endurance; // 인내

    public int Detection;
    public int Insight;

    public float AttackSpeed;
    public float CastSpeed;

    public float WeaponAttackSpeedMultiplier;
    public float WeaponCastSpeedMultiplier;

    public int MaxHp;
    public int MaxMentality;
    public int MaxStamina;

    public int StaminaRecovery;
    public int MentalityRecovery;
    #endregion

    #region 속성별저항력 ( 백분위 )

    public int FireResistance;
    public int IceResistance;
    public int LightningResistance;
    public int PierceResistance;
    public int SlashResistance;
    public int SmashResistance;

    #endregion

    #region 속성별 특화 ( 백분위 )

    public int FireAffinity; // 불 속성 특화
    public int IceAffinity; // 물 속성 특화
    public int LightningAffinity; // 땅 속성 특화
    public int PierceAffinity; // 관통 특화
    public int SlashAffinity; // 참격 특화
    public int SmashAffinity; // 타격 특화

    #endregion


    #region 공격력/방어력
    public int PhysicalAttack  = 0;
    public int MagicalAttack  = 0;
    public int PhysicalDefense = 0;
    public int MagicalDefense = 0;
    #endregion

    public void ClampNonNegative()
    {
        Strength = Mathf.Max(0, Strength);
        Dexterity = Mathf.Max(0, Dexterity);
        Speed = Mathf.Max(0, Speed);
        Intelligence = Mathf.Max(0, Intelligence);
        Wisdom = Mathf.Max(0, Wisdom);
        Health = Mathf.Max(0, Health);
        Vitality = Mathf.Max(0, Vitality);
        Endurance = Mathf.Max(0, Endurance);

        Detection = Mathf.Max(0, Detection);
        Insight = Mathf.Max(0, Insight);

        AttackSpeed = Mathf.Max(0, AttackSpeed);
        CastSpeed = Mathf.Max(0, CastSpeed);
        WeaponAttackSpeedMultiplier = Mathf.Max(0, WeaponAttackSpeedMultiplier);
        WeaponCastSpeedMultiplier = Mathf.Max(0, WeaponCastSpeedMultiplier);

        MaxHp = Mathf.Max(0, MaxHp);
        MaxMentality = Mathf.Max(0, MaxMentality);
        MaxStamina = Mathf.Max(0, MaxStamina);

        StaminaRecovery = Mathf.Max(0, StaminaRecovery);
        MentalityRecovery = Mathf.Max(0, MentalityRecovery);

        PhysicalAttack = Mathf.Max(0, PhysicalAttack);
        MagicalAttack = Mathf.Max(0, MagicalAttack);
        PhysicalDefense = Mathf.Max(0, PhysicalDefense);
        MagicalDefense = Mathf.Max(0, MagicalDefense);
    }


    #region 생성자
    public CharacterStats()
    {

    }

    public CharacterStats(int str, int dex, int spd, int intl, int wis, int hth, int end, int vit)
    {
        Strength = str;
        Dexterity = dex;
        Speed = spd;
        Intelligence = intl;
        Wisdom = wis;
        Health = hth;
        Endurance = end;
        Vitality = vit;
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
            Vitality = a.Vitality + b.Vitality,
            Detection = a.Detection + b.Detection,
            Insight = a.Insight + b.Insight,
            MaxMentality = a.MaxMentality + b.MaxMentality,
            MaxStamina = a.MaxStamina + b.MaxStamina,
            StaminaRecovery = a.StaminaRecovery + b.StaminaRecovery,
            MentalityRecovery = a.MentalityRecovery + b.MentalityRecovery,
            MaxHp = a.MaxHp + b.MaxHp,
            FireResistance = a.FireResistance + b.FireResistance,
            IceResistance = a.IceResistance + b.IceResistance,
            LightningResistance = a.LightningResistance + b.LightningResistance,
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
            IceAffinity = a.IceAffinity + b.IceAffinity,
            LightningAffinity = a.LightningAffinity + b.LightningAffinity,
            PierceAffinity = a.PierceAffinity + b.PierceAffinity,
            SlashAffinity = a.SlashAffinity + b.SlashAffinity,
            SmashAffinity = a.SmashAffinity + b.SmashAffinity
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
            Vitality = a.Vitality - b.Vitality,
            Endurance = a.Endurance - b.Endurance,
            Detection = a.Detection - b.Detection,
            Insight = a.Insight - b.Insight,
            MaxMentality = a.MaxMentality - b.MaxMentality,
            MaxStamina = a.MaxStamina - b.MaxStamina,
            StaminaRecovery = a.StaminaRecovery - b.StaminaRecovery,
            MentalityRecovery = a.MentalityRecovery - b.MentalityRecovery,
            MaxHp = a.MaxHp - b.MaxHp,
            FireResistance = a.FireResistance - b.FireResistance,
            IceResistance = a.IceResistance - b.IceResistance,
            LightningResistance = a.LightningResistance - b.LightningResistance,
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
            IceAffinity = a.IceAffinity - b.IceAffinity,
            LightningAffinity = a.LightningAffinity - b.LightningAffinity,
            PierceAffinity = a.PierceAffinity - b.PierceAffinity,
            SlashAffinity = a.SlashAffinity - b.SlashAffinity,
            SmashAffinity = a.SmashAffinity - b.SmashAffinity
        };
    }
    #endregion

    #region 특성 등급별 능력치 증가 적용을 위한 시프트 연산자 메서드

    public CharacterStats ShiftOperator(int shift)
    {
        int multiplier = 1 << Mathf.Max(0, shift);

        return new CharacterStats
        {
            Strength = Strength * multiplier,
            Dexterity = Dexterity * multiplier,
            Speed = Speed * multiplier,
            Intelligence = Intelligence * multiplier,
            Wisdom = Wisdom * multiplier,
            Health = Health * multiplier,
            Vitality = Vitality * multiplier,
            Endurance = Endurance * multiplier,

            Detection = Detection * multiplier,
            Insight = Insight * multiplier,

            MaxHp = MaxHp * multiplier,
            MaxMentality = MaxMentality * multiplier,
            MaxStamina = MaxStamina * multiplier,

            StaminaRecovery = StaminaRecovery * multiplier,
            MentalityRecovery = MentalityRecovery * multiplier,

            PhysicalAttack = PhysicalAttack * multiplier,
            MagicalAttack = MagicalAttack * multiplier,
            PhysicalDefense = PhysicalDefense * multiplier,
            MagicalDefense = MagicalDefense * multiplier,

            FireResistance = FireResistance * multiplier,
            IceResistance = IceResistance * multiplier,
            LightningResistance = LightningResistance * multiplier,

            PierceResistance = PierceResistance * multiplier,
            SlashResistance = SlashResistance * multiplier,
            SmashResistance = SmashResistance * multiplier,

            FireAffinity = FireAffinity * multiplier,
            IceAffinity = IceAffinity * multiplier,
            LightningAffinity = LightningAffinity * multiplier,

            PierceAffinity = PierceAffinity * multiplier,
            SlashAffinity = SlashAffinity * multiplier,
            SmashAffinity = SmashAffinity * multiplier,

            AttackSpeed = AttackSpeed * multiplier,
            CastSpeed = CastSpeed * multiplier,
            WeaponAttackSpeedMultiplier = WeaponAttackSpeedMultiplier * multiplier,
            WeaponCastSpeedMultiplier = WeaponCastSpeedMultiplier * multiplier
        };
    }

    #endregion

    #region DeepCopy 메서드
    public CharacterStats Copy()
    {
        return new CharacterStats
        {
            Strength = this.Strength,
            Dexterity = this.Dexterity,
            Speed = this.Speed,
            Intelligence = this.Intelligence,
            Wisdom = this.Wisdom,
            Vitality = this.Vitality,
            Health = this.Health,
            Endurance = this.Endurance,
            Detection = this.Detection,
            Insight = this.Insight,
            AttackSpeed = this.AttackSpeed,
            CastSpeed = this.CastSpeed,
            WeaponAttackSpeedMultiplier = this.WeaponAttackSpeedMultiplier,
            WeaponCastSpeedMultiplier = this.WeaponCastSpeedMultiplier,
            MaxHp = this.MaxHp,
            MaxMentality = this.MaxMentality,
            MaxStamina = this.MaxStamina,
            StaminaRecovery = this.StaminaRecovery,
            MentalityRecovery = this.MentalityRecovery,
            FireResistance = this.FireResistance,
            IceResistance = this.IceResistance,
            LightningResistance = this.LightningResistance,            
            PierceResistance = this.PierceResistance,
            SlashResistance = this.SlashResistance,
            SmashResistance = this.SmashResistance,
            FireAffinity = this.FireAffinity,
            IceAffinity = this.IceAffinity,
            LightningAffinity = this.LightningAffinity,
            PierceAffinity = this.PierceAffinity,
            SlashAffinity = this.SlashAffinity,
            SmashAffinity = this.SmashAffinity,
            PhysicalAttack = this.PhysicalAttack,
            MagicalAttack = this.MagicalAttack,
            PhysicalDefense = this.PhysicalDefense,
            MagicalDefense = this.MagicalDefense
        };
    }
    #endregion
}

[Serializable] // 특수 스탯 클래스 - 전투, 유틸, 전투 지속 관련 특수 스탯
public class CharacterSpecialStats
{
    [Header("전투")]
    [Tooltip("공격 대성공 확률. 15면 15%")]
    public int CriticalChance;
    [Tooltip("공격 대성공 피해 배율. 150이면 1.5배")]
    public int CriticalDamageBonus;

    public int StatusResistance; // 공용 상태이상 저항력

    [Header("유틸")]
    public int MapDetectionRange;
    public int TrapDetectionBonus;
    public int EventInsightBonus;

    [Header("전투 지속")]
    public int KillHpRecovery;
    public int KillStaminaRecovery;
    public int KillMentalityRecovery;

    public static CharacterSpecialStats operator +(CharacterSpecialStats a, CharacterSpecialStats b)
    {
        if (a == null) a = new CharacterSpecialStats();
        if (b == null) b = new CharacterSpecialStats();

        return new CharacterSpecialStats
        {
            CriticalChance = a.CriticalChance + b.CriticalChance,
            CriticalDamageBonus = a.CriticalDamageBonus + b.CriticalDamageBonus,
            StatusResistance = a.StatusResistance + b.StatusResistance,

            MapDetectionRange = a.MapDetectionRange + b.MapDetectionRange,
            TrapDetectionBonus = a.TrapDetectionBonus + b.TrapDetectionBonus,
            EventInsightBonus = a.EventInsightBonus + b.EventInsightBonus,

            KillHpRecovery = a.KillHpRecovery + b.KillHpRecovery,
            KillStaminaRecovery = a.KillStaminaRecovery + b.KillStaminaRecovery,
            KillMentalityRecovery = a.KillMentalityRecovery + b.KillMentalityRecovery
        };
    }

    public CharacterSpecialStats ShiftOperator(int shift)
    {
        int multiplier = 1 << Mathf.Max(0, shift);

        return new CharacterSpecialStats
        {
            CriticalChance = CriticalChance * multiplier,
            CriticalDamageBonus = CriticalDamageBonus * multiplier,
            StatusResistance = StatusResistance * multiplier,

            MapDetectionRange = MapDetectionRange * multiplier,
            TrapDetectionBonus = TrapDetectionBonus * multiplier,
            EventInsightBonus = EventInsightBonus * multiplier,

            KillHpRecovery = KillHpRecovery * multiplier,
            KillStaminaRecovery = KillStaminaRecovery * multiplier,
            KillMentalityRecovery = KillMentalityRecovery * multiplier
        };
    }

    public CharacterSpecialStats Copy()
    {
        return new CharacterSpecialStats
        {
            CriticalChance = CriticalChance,
            CriticalDamageBonus = CriticalDamageBonus,
            StatusResistance = StatusResistance,

            MapDetectionRange = MapDetectionRange,
            TrapDetectionBonus = TrapDetectionBonus,
            EventInsightBonus = EventInsightBonus,

            KillHpRecovery = KillHpRecovery,
            KillStaminaRecovery = KillStaminaRecovery,
            KillMentalityRecovery = KillMentalityRecovery
        };
    }
}

//커스터마이징 데이터 클래스
public class CustomizationData
{
    public bool IsMale { get; set; } = true;

    // P09 EditPartDataContainer의 ContentId 기준으로 저장
    public int GenderId { get; set; } = 1;      // 1 = Male, 2 = Female
    public int FaceTypeId { get; set; } = 1;
    public int HairStyleId { get; set; } = 1;
    public int HairColorId { get; set; } = 1;
    public int SkinColorId { get; set; } = 1;
    public int EyeColorId { get; set; } = 1;

    // 남성 전용 필드
    public int FacialHairId { get; set; } = 0;

    // 여성 전용 필드, P09 기준: 1 = S, 2 = M, 3 = L
    public int BustSizeId { get; set; } = 2;

    public void SyncGenderFromBool()
    {
        GenderId = IsMale ? 1 : 2;
    }

    public void SyncBoolFromGender()
    {
        IsMale = GenderId == 1;
    }
}

public class CharacterData
{
    public int originId; // 출신지
    public string originName; // 출신지 명

    public Personality personality = Personality.Simple; // 성향
    public int Belonging { get; set; } // 소속감
    public int Morale { get; set; } // 사기


    public string ID; // 캐릭터 식별을 위한 고유 ID
    public string Name; // 캐릭터 이름 - 중복 허용

    [JsonIgnore] // JSON 직렬화 무시 - 초상화는 인게임에서 처리
    public Texture2D Portrait; // 캐릭터 초상화 - 게임 실행시 게임씬에서 렌더이미지를 촬영하여 할당

    public CustomizationData customizationData; // 캐릭터 커스터마이징 데이터 - 이 데이터를 기반으로 생성된 베이스 캐릭터에 커스터마이징 적용
    public CharacterType Type {  get; set; } = CharacterType.Character;

    public CharacterStats OriginBaseStats { get; set; } = new CharacterStats(); // 출신지/성장 기준 원본 스탯
    public CharacterSpecialStats OriginSpecialStats { get; set; } = new CharacterSpecialStats(); // 특성 적용 전 원본 특수 스탯

    public CharacterStats BaseStats { get; set; } // 기본 스탯 + 특성으로 증감된 스탯
    public CharacterStats ModifiedStats { get; set; } = new CharacterStats(); // 증감 스탯 - 장비, 버프 등으로 변화한 스탯
    public CharacterStats FinalStats { get; set; } // 최종 스탯

    public CharacterSpecialStats BaseSpecialStats { get; set; } = new CharacterSpecialStats(); // OriginSpecialStats + 특성
    public CharacterSpecialStats ModifiedSpecialStats { get; set; } = new CharacterSpecialStats(); // 장비, 버프, 디버프 특수 스탯
    public CharacterSpecialStats FinalSpecialStats { get; set; } = new CharacterSpecialStats(); // 최종 특수 스탯

    [JsonIgnore]
    public CharacterStats tempStats = new CharacterStats(); // 임시스탯

    public float PhysicalDamageMultiplier { get; set; } = 1.0f; // 물리 데미지 배율 - 특성, 상태이상 등으로 변화 (기본 1.0f) 
    public float MagicalDamageMultiplier { get; set; } = 1.0f; // 마법 데미지 배율 - 특성, 상태이상 등으로 변화 (기본 1.0f) 
    public float AttackSpeedMultiplier { get; set; } = 1.0f; // 공격 속도 배율 - 특성, 상태이상 등으로 변화 (기본 1.0f) 
    public float CastSpeedMultiplier { get; set; } = 1.0f; // 시전 속도 배율 - 특성, 상태이상 등으로 변화 (기본 1.0f) 

    public bool IsAlive { get; set; } // 캐릭터의 생존유무
    public bool IsMine { get; set; } // 캐릭터 아군여부

    public int PhysicalArmor { get; set; } // 물리 방어도
    public int MagicalArmor { get; set; } // 마법 방어도


    // 캐릭터가 보유하고 있는 스킬과 특성 리스트

    public List<TraitRuntimeData> Traits { get; set; } = new List<TraitRuntimeData>(); // 캐릭터의 특성 목록
    public List<SkillRuntimeData> Skills { get; set; } = new List<SkillRuntimeData>(); // 습득한 스킬 목록


    public int DefaultCounterSkill { get; set; } // 기본 대응 스킬

    // 캐릭터에 적용되어있는 상태이상
    public List<StatusEffectRuntimeData> StatusEffects { get; set; } = new(); // 적용중인 상태이상 목록

    public List<SkillAttribute> AvailableAttributes { get; set; } = new List<SkillAttribute>(); // 캐릭터가 장착한 무기의 세부 속성 리스트

    // 장비로 인해 습득한 스킬과 특성 리스트
    public List<TraitRuntimeData> EquipmentTraitRuntimes { get; set; } = new List<TraitRuntimeData>(); // 장비로 인해 적용된 특성 런타임 데이터

    //캐릭터의 장비
    public EquipmentSlotData EquipmentSlots { get; set; } = new EquipmentSlotData();


    public int Level { get; set; }
    public int Exp { get; set; }

    public int CurrentHp { get; set; }
    public int CurrentStamina { get; set; }
    public int CurrentMentality { get; set; }

    
    public void ClampRuntimeResources()
    {
        if (FinalStats != null)
        {
            CurrentHp = Mathf.Clamp(CurrentHp, 0, FinalStats.MaxHp);
            CurrentStamina = Mathf.Clamp(CurrentStamina, 0, FinalStats.MaxStamina);
            CurrentMentality = Mathf.Clamp(CurrentMentality, 0, FinalStats.MaxMentality);
        }
    }


    #region 장비 관련 유틸리티 메서드 - 런타임 장비 조회 + 정의 SO 데이터 원본 조회

    public EquipmentRuntimeData GetEquipmentRuntime(int itemUid, string instanceId)
    {
        return EquipmentRuntimeResolver.Resolve(itemUid, instanceId);
    }

    public EquipmentRuntimeData GetHelmetRuntime()
    {
        return GetEquipmentRuntime(EquipmentSlots.helmetUid, EquipmentSlots.helmetInstanceId);
    }

    public EquipmentRuntimeData GetArmorRuntime()
    {
        return GetEquipmentRuntime(EquipmentSlots.armorUid, EquipmentSlots.armorInstanceId);
    }

    public EquipmentRuntimeData GetGlovesRuntime()
    {
        return GetEquipmentRuntime(EquipmentSlots.glovesUid, EquipmentSlots.glovesInstanceId);
    }

    public EquipmentRuntimeData GetShoesRuntime()
    {
        return GetEquipmentRuntime(EquipmentSlots.shoesUid, EquipmentSlots.shoesInstanceId);
    }

    public EquipmentRuntimeData GetRing1Runtime()
    {
        return GetEquipmentRuntime(EquipmentSlots.ring1Uid, EquipmentSlots.ring1InstanceId);
    }

    public EquipmentRuntimeData GetRing2Runtime()
    {
        return GetEquipmentRuntime(EquipmentSlots.ring2Uid, EquipmentSlots.ring2InstanceId);
    }

    public EquipmentRuntimeData GetNecklaceRuntime()
    {
        return GetEquipmentRuntime(EquipmentSlots.necklaceUid, EquipmentSlots.necklaceInstanceId);
    }

    public EquipmentRuntimeData GetMainWeaponRuntime()
    {
        return GetEquipmentRuntime(EquipmentSlots.weaponUid, EquipmentSlots.weaponInstanceId);
    }

    public EquipmentRuntimeData GetSubWeaponRuntime()
    {
        return GetEquipmentRuntime(EquipmentSlots.subWeaponUid, EquipmentSlots.subWeaponInstanceId);
    }

    public List<EquipmentRuntimeData> GetEquipmentRuntimes()
    {
        List<EquipmentRuntimeData> result = new List<EquipmentRuntimeData>();

        AddEquipmentRuntimeIfValid(result, EquipmentSlots.helmetUid, EquipmentSlots.helmetInstanceId);
        AddEquipmentRuntimeIfValid(result, EquipmentSlots.armorUid, EquipmentSlots.armorInstanceId);
        AddEquipmentRuntimeIfValid(result, EquipmentSlots.glovesUid, EquipmentSlots.glovesInstanceId);
        AddEquipmentRuntimeIfValid(result, EquipmentSlots.shoesUid, EquipmentSlots.shoesInstanceId);

        AddEquipmentRuntimeIfValid(result, EquipmentSlots.ring1Uid, EquipmentSlots.ring1InstanceId);
        AddEquipmentRuntimeIfValid(result, EquipmentSlots.ring2Uid, EquipmentSlots.ring2InstanceId);
        AddEquipmentRuntimeIfValid(result, EquipmentSlots.necklaceUid, EquipmentSlots.necklaceInstanceId);

        AddEquipmentRuntimeIfValid(result, EquipmentSlots.weaponUid, EquipmentSlots.weaponInstanceId);
        AddEquipmentRuntimeIfValid(result, EquipmentSlots.subWeaponUid, EquipmentSlots.subWeaponInstanceId);

        return result;
    }

    private void AddEquipmentRuntimeIfValid(List<EquipmentRuntimeData> list, int uid, string instanceId)
    {
        EquipmentRuntimeData equipment = GetEquipmentRuntime(uid, instanceId);

        if (equipment != null)
            list.Add(equipment);
    }

    public EquipmentDefinitionSO GetEquipmentDefinition(int uid)
    {
        if (uid <= 0 || GameDataRegistry.Instance == null)
            return null;

        return GameDataRegistry.Instance.GetEquipment(uid);
    }

    public EquipmentDefinitionSO GetHelmet()
    {
        return GetEquipmentDefinition(EquipmentSlots.helmetUid);
    }

    public EquipmentDefinitionSO GetArmor()
    {
        return GetEquipmentDefinition(EquipmentSlots.armorUid);
    }

    public EquipmentDefinitionSO GetGloves()
    {
        return GetEquipmentDefinition(EquipmentSlots.glovesUid);
    }

    public EquipmentDefinitionSO GetShoes()
    {
        return GetEquipmentDefinition(EquipmentSlots.shoesUid);
    }

    public EquipmentDefinitionSO GetRing1()
    {
        return GetEquipmentDefinition(EquipmentSlots.ring1Uid);
    }

    public EquipmentDefinitionSO GetRing2()
    {
        return GetEquipmentDefinition(EquipmentSlots.ring2Uid);
    }

    public EquipmentDefinitionSO GetNecklace()
    {
        return GetEquipmentDefinition(EquipmentSlots.necklaceUid);
    }

    public WeaponDefinitionSO GetMainWeapon()
    {
        return GetEquipmentDefinition(EquipmentSlots.weaponUid) as WeaponDefinitionSO;
    }

    public WeaponDefinitionSO GetSubWeapon()
    {
        return GetEquipmentDefinition(EquipmentSlots.subWeaponUid) as WeaponDefinitionSO;
    }

    public List<EquipmentDefinitionSO> GetEquipments()
    {
        List<EquipmentDefinitionSO> result = new List<EquipmentDefinitionSO>();

        AddEquipmentIfValid(result, EquipmentSlots.helmetUid);
        AddEquipmentIfValid(result, EquipmentSlots.armorUid);
        AddEquipmentIfValid(result, EquipmentSlots.glovesUid);
        AddEquipmentIfValid(result, EquipmentSlots.shoesUid);
        AddEquipmentIfValid(result, EquipmentSlots.ring1Uid);
        AddEquipmentIfValid(result, EquipmentSlots.ring2Uid);
        AddEquipmentIfValid(result, EquipmentSlots.necklaceUid);
        AddEquipmentIfValid(result, EquipmentSlots.weaponUid);
        AddEquipmentIfValid(result, EquipmentSlots.subWeaponUid);

        return result;
    }

    private void AddEquipmentIfValid(List<EquipmentDefinitionSO> list, int uid)
    {
        EquipmentDefinitionSO equipment = GetEquipmentDefinition(uid);

        if (equipment != null)
            list.Add(equipment);
    }

    #endregion

    public bool HasTrait(int traitId)
    {
        foreach (TraitRuntimeData runtime in GetAllTraitRuntimes())
        {
            if (runtime.traitId == traitId)
                return true;
        }

        return false;
    }

    public bool HasCharacterTrait(int traitId) // 장비 특성 제외하고 캐릭터 특성만 확인
    {
        if (Traits == null)
            return false;

        return Traits.Any(t => t != null && t.traitId == traitId);
    }

    public bool HasTraitFlag(TraitSpecialFlag flag)
    {
        foreach (TraitRuntimeData runtime in GetAllTraitRuntimes())
        {
            TraitDefinitionSO def = GameDataRegistry.Instance.GetTrait(runtime.traitId);

            if (def == null || def.specialFlags == null)
                continue;

            if (def.specialFlags.Contains(flag))
                return true;
        }

        return false;
    }

    public IEnumerable<TraitRuntimeData> GetAllTraitRuntimes()
    {
        if (Traits != null)
        {
            foreach (TraitRuntimeData trait in Traits)
            {
                if (trait != null)
                    yield return trait;
            }
        }

        if (EquipmentTraitRuntimes != null)
        {
            foreach (TraitRuntimeData trait in EquipmentTraitRuntimes)
            {
                if (trait != null)
                    yield return trait;
            }
        }
    }

    // 무기 착용 가능 여부
    public bool CanEquipMainWeapon(WeaponDefinitionSO weapon)
    {
        if (weapon == null)
            return true;

        if (HasTraitFlag(TraitSpecialFlag.OneArmed) &&
            weapon.weaponTags != null &&
            weapon.weaponTags.Contains(WeaponTag.TwoHanded))
        {
            return false;
        }

        return true;
    }

    public bool CanEquipSubWeapon()
    {
        return !HasTraitFlag(TraitSpecialFlag.OneArmed);
    }

    // 모든 특성 효과 제거
    public void RemoveAllTraits(CharacterManager manager)
    {
        BaseStats = OriginBaseStats != null ? OriginBaseStats.Copy() : new CharacterStats();
        BaseSpecialStats = OriginSpecialStats != null ? OriginSpecialStats.Copy() : new CharacterSpecialStats();
    }

    // 모든 특성 효과 적용
    public void ApplyAllTraits(CharacterManager manager)
    {
        if (BaseStats == null)
            BaseStats = OriginBaseStats != null ? OriginBaseStats.Copy() : new CharacterStats();

        if (BaseSpecialStats == null)
            BaseSpecialStats = OriginSpecialStats != null ? OriginSpecialStats.Copy() : new CharacterSpecialStats();

        ApplyTraitPass(false);
        ApplyTraitPass(true);

        BaseStats.ClampNonNegative();
    }

    // 특성 적용 패스
    void ApplyTraitPass(bool percentage)
    {
        foreach (TraitRuntimeData runtime in GetAllTraitRuntimes())
        {
            TraitDefinitionSO def = GameDataRegistry.Instance.GetTrait(runtime.traitId);

            if (def == null)
                continue;

            if (def.isPercentage != percentage)
                continue;

            int shift = TraitGradeUtility.GetGradeShift(runtime, def);

            CharacterStats statDelta = def.statDelta != null
                ? def.statDelta.ShiftOperator(shift)
                : new CharacterStats();

            CharacterSpecialStats specialDelta = def.specialStatDelta != null
                ? def.specialStatDelta.ShiftOperator(shift)
                : new CharacterSpecialStats();

            if (percentage)
                ApplyPercentageStats(BaseStats, statDelta);
            else
                BaseStats += statDelta;

            BaseSpecialStats += specialDelta;

            if (CanApplyConditionalTrait(def))
            {
                CharacterStats conditionalStatDelta = def.conditionalStatDelta != null
                    ? def.conditionalStatDelta.ShiftOperator(shift)
                    : new CharacterStats();

                CharacterSpecialStats conditionalSpecialDelta = def.conditionalSpecialStatDelta != null
                    ? def.conditionalSpecialStatDelta.ShiftOperator(shift)
                    : new CharacterSpecialStats();

                if (percentage)
                    ApplyPercentageStats(BaseStats, conditionalStatDelta);
                else
                    BaseStats += conditionalStatDelta;

                BaseSpecialStats += conditionalSpecialDelta;
            }
        }
    }

    bool CanApplyConditionalTrait(TraitDefinitionSO def)
    {
        if (def == null)
            return false;

        if (!def.hasRequiredWeaponCondition)
            return false;

        WeaponDefinitionSO weapon = GetMainWeapon();

        if (weapon == null)
            return false;

        return weapon.weaponType == def.requiredWeaponType;
    }

    // % 연산을 진행할 특성용 스탯 적용 메서드
    void ApplyPercentageStats(CharacterStats target, CharacterStats percent)
    {
        if (target == null || percent == null)
            return;

        target.Strength += Mathf.RoundToInt(target.Strength * percent.Strength / 100f);
        target.Dexterity += Mathf.RoundToInt(target.Dexterity * percent.Dexterity / 100f);
        target.Speed += Mathf.RoundToInt(target.Speed * percent.Speed / 100f);
        target.Intelligence += Mathf.RoundToInt(target.Intelligence * percent.Intelligence / 100f);
        target.Wisdom += Mathf.RoundToInt(target.Wisdom * percent.Wisdom / 100f);
        target.Health += Mathf.RoundToInt(target.Health * percent.Health / 100f);
        target.Endurance += Mathf.RoundToInt(target.Endurance * percent.Endurance / 100f);
        target.Vitality += Mathf.RoundToInt(target.Vitality * percent.Vitality / 100f);

        target.Detection += Mathf.RoundToInt(target.Detection * percent.Detection / 100f);
        target.Insight += Mathf.RoundToInt(target.Insight * percent.Insight / 100f);

        target.MaxHp += Mathf.RoundToInt(target.MaxHp * percent.MaxHp / 100f);
        target.MaxMentality += Mathf.RoundToInt(target.MaxMentality * percent.MaxMentality / 100f);
        target.MaxStamina += Mathf.RoundToInt(target.MaxStamina * percent.MaxStamina / 100f);

        target.StaminaRecovery += Mathf.RoundToInt(target.StaminaRecovery * percent.StaminaRecovery / 100f);
        target.MentalityRecovery += Mathf.RoundToInt(target.MentalityRecovery * percent.MentalityRecovery / 100f);

        target.PhysicalAttack += Mathf.RoundToInt(target.PhysicalAttack * percent.PhysicalAttack / 100f);
        target.MagicalAttack += Mathf.RoundToInt(target.MagicalAttack * percent.MagicalAttack / 100f);
        target.PhysicalDefense += Mathf.RoundToInt(target.PhysicalDefense * percent.PhysicalDefense / 100f);
        target.MagicalDefense += Mathf.RoundToInt(target.MagicalDefense * percent.MagicalDefense / 100f);

        target.FireResistance += Mathf.RoundToInt(target.FireResistance * percent.FireResistance / 100f);
        target.IceResistance += Mathf.RoundToInt(target.IceResistance * percent.IceResistance / 100f);
        target.LightningResistance += Mathf.RoundToInt(target.LightningResistance * percent.LightningResistance / 100f);

        target.PierceResistance += Mathf.RoundToInt(target.PierceResistance * percent.PierceResistance / 100f);
        target.SlashResistance += Mathf.RoundToInt(target.SlashResistance * percent.SlashResistance / 100f);
        target.SmashResistance += Mathf.RoundToInt(target.SmashResistance * percent.SmashResistance / 100f);

        target.FireAffinity += Mathf.RoundToInt(target.FireAffinity * percent.FireAffinity / 100f);
        target.IceAffinity += Mathf.RoundToInt(target.IceAffinity * percent.IceAffinity / 100f);
        target.LightningAffinity += Mathf.RoundToInt(target.LightningAffinity * percent.LightningAffinity / 100f);

        target.PierceAffinity += Mathf.RoundToInt(target.PierceAffinity * percent.PierceAffinity / 100f);
        target.SlashAffinity += Mathf.RoundToInt(target.SlashAffinity * percent.SlashAffinity / 100f);
        target.SmashAffinity += Mathf.RoundToInt(target.SmashAffinity * percent.SmashAffinity / 100f);

        target.AttackSpeed += target.AttackSpeed * percent.AttackSpeed / 100f;
        target.CastSpeed += target.CastSpeed * percent.CastSpeed / 100f;
        target.WeaponAttackSpeedMultiplier += target.WeaponAttackSpeedMultiplier * percent.WeaponAttackSpeedMultiplier / 100f;
        target.WeaponCastSpeedMultiplier += target.WeaponCastSpeedMultiplier * percent.WeaponCastSpeedMultiplier / 100f;
    }

    #region 생성자 오버로딩 구현부
    public CharacterData(CharacterStats baseStats)
    {
        OriginBaseStats = baseStats != null ? baseStats.Copy() : new CharacterStats();
        OriginSpecialStats = new CharacterSpecialStats();

        BaseStats = OriginBaseStats.Copy();
        BaseSpecialStats = OriginSpecialStats.Copy();

        ModifiedStats = new CharacterStats();
        ModifiedSpecialStats = new CharacterSpecialStats();

        FinalStats = new CharacterStats();
        FinalSpecialStats = new CharacterSpecialStats();

        Traits = new List<TraitRuntimeData>();
        Skills = new List<SkillRuntimeData>();

        UpdateFinalStats();
    }

    public CharacterData()
    {
        OriginBaseStats = new CharacterStats();
        OriginSpecialStats = new CharacterSpecialStats();

        BaseStats = OriginBaseStats.Copy();
        BaseSpecialStats = OriginSpecialStats.Copy();

        ModifiedStats = new CharacterStats();
        ModifiedSpecialStats = new CharacterSpecialStats();

        FinalStats = new CharacterStats();
        FinalSpecialStats = new CharacterSpecialStats();

        Traits = new List<TraitRuntimeData>();
        Skills = new List<SkillRuntimeData>();
    }
    #endregion    

    // 최종 스탯 계산 - 장비 탈착, 특성 추가 혹은 삭제, 버프 획득 등의 상황에 호출해줘야함
    public void UpdateFinalStats()
    {
        FinalStats = BaseStats + ModifiedStats;
        tempStats = FinalStats;

        FinalStats = CalcStat(tempStats);
        FinalStats.ClampNonNegative();

        UpdateFinalSpecialStats();

        ClampRuntimeResources();
    }

    void UpdateFinalSpecialStats()
    {
        FinalSpecialStats = BaseSpecialStats + ModifiedSpecialStats;

        if (FinalStats == null)
            return;

        FinalSpecialStats.MapDetectionRange += FinalStats.Detection / 10;
        FinalSpecialStats.EventInsightBonus += FinalStats.Insight / 10;

        FinalSpecialStats.CriticalChance += 15;
        FinalSpecialStats.CriticalDamageBonus += 150;
        FinalSpecialStats.CriticalChance += FinalStats.Dexterity / 20;
        FinalSpecialStats.CriticalDamageBonus += FinalStats.Strength / 10;

        FinalSpecialStats.StatusResistance += Mathf.RoundToInt(
            FinalStats.Health * 0.5f +
            FinalStats.Endurance * 0.5f);
    }

    public CharacterStats CalcStat(CharacterStats stats)
    {
        WeaponDefinitionSO mainWeapon = GetMainWeapon();
        WeaponDefinitionSO subWeapon = GetSubWeapon();

        int baseMaxMentality = stats.MaxMentality + stats.Wisdom / 5 + 1;
        int baseMaxStamina = stats.MaxStamina + stats.Speed / 5 + 1;

        int baseStaminaRecovery = stats.StaminaRecovery + stats.Vitality / 10 + 1;
        int baseMentalityRecovery = stats.MentalityRecovery + stats.Intelligence / 10 + 1;
        int basePhysicalDefense = stats.PhysicalDefense + stats.Endurance / 5;
        int baseMagicalDefense = stats.MagicalDefense + stats.Endurance / 5;
        int basePhysicalAttack = stats.PhysicalAttack;
        int baseMagicalAttack = stats.MagicalAttack + stats.Intelligence; // 지능 지수에 따라 증가. - 장착 무기 타입 상관 없이 적용
        int baseDetection = stats.Detection + (int)(stats.Dexterity * ((double)(stats.Dexterity / 10.0)) + stats.Speed * ((double)(stats.Speed / 10.0)));
        int baseInsight =  stats.Insight + (int)(stats.Wisdom * ((double)(stats.Wisdom / 10.0)) + stats.Intelligence * ((double)(stats.Intelligence / 10.0)));
        int baseMaxHp = 15 + Level * 5 + stats.Vitality * 2 + stats.Health * 3 + stats.MaxHp;
        float baseAtkSpd = (1.0f + stats.Speed * 0.01f); // 기본 속도 1.0 + 속도 스탯 * 0.01

        if (mainWeapon != null && mainWeapon.statModifiers != null)
            baseAtkSpd *= mainWeapon.statModifiers.WeaponAttackSpeedMultiplier; // 장착중인 무기 공격속도 배율 적용

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
        if (mainWeapon != null && mainWeapon.weaponCategory == WeaponCategory.HeavyWeapon)
        {
            basePhysicalAttack += stats.Strength;
        }
        else if (mainWeapon != null && mainWeapon.weaponCategory == WeaponCategory.LightWeapon)
        {
            basePhysicalAttack += stats.Strength > stats.Dexterity
                ? stats.Strength
                : stats.Dexterity;
        }
        else if (mainWeapon == null && subWeapon != null)
        {
            if (subWeapon.weaponCategory == WeaponCategory.HeavyWeapon)
            {
                basePhysicalAttack += stats.Strength;
            }
            else if (subWeapon.weaponCategory == WeaponCategory.LightWeapon)
            {
                basePhysicalAttack += stats.Strength > stats.Dexterity
                    ? stats.Strength
                    : stats.Dexterity;
            }
        }
        else
        {
            basePhysicalAttack += stats.Strength;
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