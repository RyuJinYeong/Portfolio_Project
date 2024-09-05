using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TraitManager
{
    public static void AddTrait(CharacterData character, TraitBase trait)
    {
        if (!character.Traits.Contains(trait))
        {
            character.RemoveAllTraits();
            character.Traits.Add(trait);
            character.ApplyAllTraits();// 특성 추가 후 바로 적용
            
            character.UpdateFinalStats(); // 특성 적용 결과로 변화한 스탯을 최종 스탯에 적용
        }
    }

    public static void RemoveTrait(CharacterData character, TraitBase trait)
    {
        if (character.Traits.Contains(trait))
        {
            character.RemoveAllTraits(); // 캐릭터에 적용된 특성 해제
            character.Traits.Remove(trait); // 캐릭터가 보유중인 특성 제거
            character.ApplyAllTraits();  // 특성 제거 후 모든 특성 새로 적용

            character.UpdateFinalStats(); // 특성 제거 후 변화한 스탯을 최종 스탯에 적용
        }
    }
}