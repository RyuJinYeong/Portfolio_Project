using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StatusEffect : Root
{
    public int RemainingTurns { get; private set; } // 남은 턴 수
    public bool IsDebuff { get; private set; } // 디버프 여부

    public StatusEffect(string name, string description, Texture2D icon, int duration, bool isDebuff)
    {
        Name = name;
        Description = description;
        Icon = icon;
        RemainingTurns = duration;
        IsDebuff = isDebuff;
        ObjectType = ObjectType.StatusEffect;
    }

    public virtual void ApplyEffect(CharacterManager character) { } // 매턴 효과 적용
    public virtual void OnApply(CharacterManager character) { }   // 최초 적용될 때 호출
    public virtual void OnExpire(CharacterManager character) { }  // 효과 만료 시 호출

    public void ReduceTurn() // 남은 턴 감소
    {
        RemainingTurns--;
    }

    public bool IsExpired() // 상태이상 만료 여부 체크
    {
        return RemainingTurns <= 0;
    }
}
