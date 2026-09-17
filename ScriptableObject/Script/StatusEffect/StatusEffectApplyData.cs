using System;
using UnityEngine;

[Serializable]
public class StatusEffectApplyData
{
    public int statusEffectId;

    [Tooltip("이 스킬이 해당 상태이상을 걸 기본 확률")]
    public int baseChance = 100;

    [Tooltip("부여할 스택 수")]
    public int stackAmount = 1;
}