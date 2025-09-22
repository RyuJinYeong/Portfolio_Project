using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TraitManager
{
    public static void AddTrait(CharacterManager manager, TraitBase trait)
    {
        var exist = manager.character.Traits.FirstOrDefault(t => t.Id == trait.Id);


        manager.character.RemoveAllTraits(manager);//특성 효과 전체 제거

        if (exist != null)
        {
            // 이미 존재하는 특성인 경우 레벨업
            exist.Level += 1;
        }
        else
        {
            // 새로운 특성인 경우 추가
            manager.character.Traits.Add(trait);
        }

        manager.character.ApplyAllTraits(manager);// 특성 추가 후 바로 적용

        manager.character.UpdateFinalStats(); // 특성 적용 결과로 변화한 스탯을 최종 스탯에 적용
    }

    public static void RemoveTrait(CharacterManager manager, TraitBase trait)
    {
        var exist = manager.character.Traits.FirstOrDefault(t => t.Id == trait.Id);

        manager.character.RemoveAllTraits(manager); // 캐릭터에 적용된 특성 해제

        if (exist != null)
        {
            if(exist.Level == 1)
            {
                // 레벨이 1 이하인 경우 특성 제거
                manager.character.Traits.Remove(trait);
            }
            else
            {
                // 레벨이 1 초과인 경우 레벨다운
                exist.Level -= 1;
            }                
        }

        manager.character.ApplyAllTraits(manager);  // 특성 제거 혹은 레벨 다운 후 모든 특성 새로 적용

        manager.character.UpdateFinalStats(); // 특성 제거 후 변화한 스탯을 최종 스탯에 적용
    }
}