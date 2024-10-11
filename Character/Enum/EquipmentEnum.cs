public enum WeaponCategory
{
    HeavyWeapon,   // 중량 무기
    LightWeapon   // 경량 무기    
}

public enum WeaponTag
{
    TwoHanded,     // 양손 무기 - 원거리 무기의 경우 보조무기 장착이 불가능하게 임의로 설정
    MagicWeapon,    // 마법 무기 - 마법 공격력 유무를 가리는 태그
    OffhandWeapon,  // 보조 무기
}

public enum WeaponType
{
    Sword, // 검
    Bow, // 활
    BluntWeapon, // 둔기
    Shield, // 방패
    Axe, // 도끼
    Spear, // 창
    Staff // 스태프
}

public enum EquipmentType
{
    Helmet,     // 헬멧
    Armor,      // 갑옷
    Gloves,     // 장갑
    Shoes,      // 신발
    Ring,       // 반지
    Earring,    // 귀걸이
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