public class CounterResolveData
{
    public CounterResult result;

    public SkillDefinitionSO counterSkill;
    public CharacterManager counterUser;

    public float damageMultiplier = 1f;
    public float effectScale = 1f;

    public bool cancelCurrentSkill;
    public bool cancelNextSkill;

    public bool skipStatusEffects;

    // 역상성 대응 실패로 공격 대성공이 발생했는지
    public bool attackGreatSuccess;

    public bool IsSuccess
    {
        get
        {
            return result == CounterResult.Success ||
                   result == CounterResult.GreatSuccess;
        }
    }
}