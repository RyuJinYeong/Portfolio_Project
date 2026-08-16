using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Status Effect")]
public class StatusEffectDefinitionSO : ScriptableObject
{
    public int id;
    public string statusName;

    [TextArea]
    public string description;

    public Texture2D icon;

    public bool isDebuff = true;

    public StatusDurationType durationType =
        StatusDurationType.FullTurn;

    public StatusEffectType effectType =
        StatusEffectType.None;

    [Tooltip("0이면 스택 제한 없음")]
    public int maxStack;

    [Header("Effect Value")]
    public int fixedDamagePerStack;

    [Tooltip("대응 성공률 감소 공식에서 사용하는 스택당 위력")]
    public int counterPenaltyPerStack;

    [Header("Status Apply Power")]
    [Tooltip("공격자가 이 상태이상을 부여할 때 사용하는 능력치 공식")]
    public List<StatusStatWeightData> attackerStatWeights = new();
}