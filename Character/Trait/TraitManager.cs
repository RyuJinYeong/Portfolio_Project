using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TraitManager
{
    public static void AddTrait(CharacterManager manager, TraitBase trait)
    {
        if (!manager.character.Traits.Contains(trait))
        {
            manager.character.RemoveAllTraits(manager);
            manager.character.Traits.Add(trait);
            manager.character.ApplyAllTraits(manager);// 특성 추가 후 바로 적용

            manager.character.UpdateFinalStats(); // 특성 적용 결과로 변화한 스탯을 최종 스탯에 적용
        }
    }

    public static void RemoveTrait(CharacterManager manager, TraitBase trait)
    {
        if (manager.character.Traits.Contains(trait))
        {
            manager.character.RemoveAllTraits(manager); // 캐릭터에 적용된 특성 해제
            manager.character.Traits.Remove(trait); // 캐릭터가 보유중인 특성 제거
            manager.character.ApplyAllTraits(manager);  // 특성 제거 후 모든 특성 새로 적용

            manager.character.UpdateFinalStats(); // 특성 제거 후 변화한 스탯을 최종 스탯에 적용
        }
    }
}