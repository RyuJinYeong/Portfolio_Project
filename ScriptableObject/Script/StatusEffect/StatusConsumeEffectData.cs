using System;
using UnityEngine;

[Serializable]
public class StatusConsumeEffectData
{
    public int sourceStatusId;

    public bool consumeAllStacks;

    public int maxConsumeStack = 3;

    public bool removeConsumedStacks = true;

    [Header("Damage")]
    public SkillType damageType = SkillType.Physical;
    public SkillAttribute damageAttribute = SkillAttribute.None;

    public float damagePerStackAttackMultiplier;

    [Header("Result Status")]
    public int resultStatusId;

    public int resultBaseChance;
    public int resultChancePerConsumedStack;

    public int resultStackAmount = 1;
}