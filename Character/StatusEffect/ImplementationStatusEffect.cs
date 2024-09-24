public class SynergyEffect : StatusEffect // 시너지 효과로 발동되는 상태이상
{
    public float PhysicalDamageMultiplierBonus { get; private set; }
    public float MagicalDamageMultiplierBonus { get; private set; }
    public float AttackSpeedMultiplierBonus { get; private set; }
    public float CastSpeedMultiplierBonus { get; private set; }
    public bool IgnoreArmor { get; private set; }

    private float accumulatedPhysicalDamageMultiplier;
    private float accumulatedMagicalDamageMultiplier;
    private int stage; // 시너지 효과의 단계 (누적 시너지)

    public SynergyEffect(float physicalDamageBonus, float magicalDamageBonus, float attackSpeedBonus, float castSpeedBonus, bool ignoreArmor)
        : base("SynergyEffect", 1, false)
    {
        PhysicalDamageMultiplierBonus = physicalDamageBonus;
        MagicalDamageMultiplierBonus = magicalDamageBonus;
        AttackSpeedMultiplierBonus = attackSpeedBonus;
        CastSpeedMultiplierBonus = castSpeedBonus;
        IgnoreArmor = ignoreArmor;
        accumulatedPhysicalDamageMultiplier = 0.0f;
        accumulatedMagicalDamageMultiplier = 0.0f;
        stage = 0;
    }

    // 시너지 효과 적용
    public override void OnApply(CharacterManager character)
    {
        stage++;
        float stageMultiplier = 1.0f + 0.05f * stage; // 시너지 단계별 증가율 (5%씩 증가)

        accumulatedPhysicalDamageMultiplier += PhysicalDamageMultiplierBonus * stageMultiplier;
        accumulatedMagicalDamageMultiplier += MagicalDamageMultiplierBonus * stageMultiplier;

        character.character.PhysicalDamageMultiplier += accumulatedPhysicalDamageMultiplier;
        character.character.MagicalDamageMultiplier += accumulatedMagicalDamageMultiplier;
        character.character.AttackSpeedMultiplier += AttackSpeedMultiplierBonus;
        character.character.CastSpeedMultiplier += CastSpeedMultiplierBonus;

        if (IgnoreArmor)
        {
            // 방어력 무시 처리
        }
    }

    // 시너지 효과 해제
    public override void OnExpire(CharacterManager character)
    {
        character.character.PhysicalDamageMultiplier -= accumulatedPhysicalDamageMultiplier;
        character.character.MagicalDamageMultiplier -= accumulatedMagicalDamageMultiplier;
        character.character.AttackSpeedMultiplier -= AttackSpeedMultiplierBonus;
        character.character.CastSpeedMultiplier -= CastSpeedMultiplierBonus;

        accumulatedPhysicalDamageMultiplier = 0.0f;
        accumulatedMagicalDamageMultiplier = 0.0f;
        stage = 0; // 시너지 단계 초기화
    }
}
