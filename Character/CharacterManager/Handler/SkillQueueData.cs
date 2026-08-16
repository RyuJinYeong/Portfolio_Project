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

    public SkillQueueData(
        SkillDefinitionSO skill,
        CharacterManager user,
        CharacterManager target,
        bool isConcealed,
        int order)
    {
        this.skill = skill;
        this.user = user;
        this.target = target;
        this.isConcealed = isConcealed;
        this.concealResolved = false;
        this.revealLevel = RevealLevel.None;
        this.order = order;
    }
}