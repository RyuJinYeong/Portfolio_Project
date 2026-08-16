public enum WeaponCategory
{
    HeavyWeapon,   // 중량 무기
    LightWeapon   // 경량 무기    
}

public enum WeaponTag
{
    TwoHanded = 0,     // 양손 무기 - 활의 경우 보조무기 장착이 불가능하게 임의로 설정
    MagicWeapon = 1    // 마법 무기 - 마법 공격력 유무를 가리는 태그
}

public enum WeaponType
{
    Two_HandedSword, // 양손검
    Greatsword,   // 대검
    LongSword,    // 장검
    Dagger,       // 단검
    Bow,          // 활
    Mace,         // 철퇴
    Hammer,       // 망치
    Shield,       // 방패
    Axe,          // 도끼
    Spear,        // 창
    Staff,        // 지팡이
    Book,         // 책
    Orb           // 오브
}

public enum EquipmentType
{
    Helmet,     // 헬멧
    Armor,      // 갑옷
    Gloves,     // 장갑
    Shoes,      // 신발    
    Ring,       // 반지
    Necklace,   // 목걸이        
    Weapon,     // 무기
    SubWeapon   // 보조무기
}

public enum ArmorCategory
{
    HeavyArmor,   // 중갑
    LightArmor,   // 경갑
    ClothArmor    // 의복
}

public enum EquipmentRarity
{
    Common,  //일반
    Uncommon,//고급
    Rare,    //희귀
    Epic,    //영웅
    Legendary//전설
}
public enum EquipmentAffixType
{
    Prefix,
    Suffix,
    Special
}
public enum EquipmentRarityRollMode
{
    Fixed,
    Random
}