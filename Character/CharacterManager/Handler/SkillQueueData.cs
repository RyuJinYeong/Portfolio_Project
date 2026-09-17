using System;

[Serializable]
public class SkillQueueData
{
    public SkillDefinitionSO skill;
    public CharacterManager user;
    public CharacterManager target;

    public bool isConcealed;
    public bool concealResolved;
    public RevealLevel revealLevel;

    public int order;
    public bool resourcesConsumed;

    // 대응 큐에서 어떤 공격을 자동으로 상대하는지 추적한다.
    public SkillDefinitionSO incomingSkill;
    public CharacterManager protectedTarget;

    public SkillQueueData(
        SkillDefinitionSO skill,
        CharacterManager user,
        CharacterManager target,
        bool isConcealed,
        int order,
        bool resourcesConsumed = false)
    {
        this.skill = skill;
        this.user = user;
        this.target = target;
        this.isConcealed = isConcealed;
        this.concealResolved = false;
        this.revealLevel = RevealLevel.None;
        this.order = order;
        this.resourcesConsumed = resourcesConsumed;
    }
}
