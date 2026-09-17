using System.Collections.Generic;
using UnityEngine;

public enum TraitGrade : int { F = 0, E = 1, D = 2, C = 3, B = 4, A = 5, S = 6 }
public enum TraitPolarity : int { Positive = 0, Negative = 1, Mixed = 2 }

[CreateAssetMenu(menuName = "GameData/Trait")]
public class TraitDefinitionSO : ScriptableObject
{
    public int id;
    public string traitName;

    [TextArea]
    public string description;

    public Texture2D icon;

    public TraitPolarity polarity;

    [Header("Grade")]
    public bool canGradeUp = true;
    public TraitGrade defaultAcquireGrade = TraitGrade.F;

    [Header("기본스탯")]
    public bool isPercentage;
    public CharacterStats statDelta = new CharacterStats();

    [Header("특수스탯")]
    public CharacterSpecialStats specialStatDelta = new CharacterSpecialStats();

    [Header("특수조건")]
    public List<TraitSpecialFlag> specialFlags = new();

    [Header("조건부 스탯")]
    public bool hasRequiredWeaponCondition;
    public WeaponType requiredWeaponType;

    public CharacterStats conditionalStatDelta = new CharacterStats();
    public CharacterSpecialStats conditionalSpecialStatDelta = new CharacterSpecialStats();
}
