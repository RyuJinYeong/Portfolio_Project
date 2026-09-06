using System.Linq;
using UnityEngine;

public static class SkillManager
{
    public const int MaxQuickSlotCount = 12;

    public static bool AddSkill(CharacterData character, SkillDefinitionSO skillDef)
    {
        if (character == null || skillDef == null)
            return false;

        return AddSkill(character, skillDef.uid);
    }

    public static bool AddSkill(CharacterData character, int skillUid)
    {
        if (character == null)
            return false;

        if (character.Skills == null)
            character.Skills = new System.Collections.Generic.List<SkillRuntimeData>();

        if (character.Skills.Any(s => s.skillUid == skillUid))
        {
            Debug.Log("이미 존재하는 스킬 습득 시도");
            return false;
        }

        SkillDefinitionSO def = GameDataRegistry.Instance.GetSkill(skillUid);

        if (def == null)
        {
            Debug.LogWarning($"존재하지 않는 스킬 UID입니다: {skillUid}");
            return false;
        }

        if (!def.CanBeAcquiredBy(character))
        {
            Debug.Log($"{def.skillName} 스킬의 습득 조건을 충족하지 못했습니다.");
            return false;
        }

        SkillRuntimeData runtime = SkillRuntimeFactory.Create(def);

        if (runtime == null)
            return false;

        character.Skills.Add(runtime);

        EquipmentManager.UpdateAvailableAttributes(character);
        EquipmentManager.UpdateSkillAvailability(character);

        TryAutoRegisterQuickSlot(character, runtime);

        Debug.Log($"{def.skillName} 스킬 습득");

        return true;
    }

    public static bool RemoveSkill(CharacterData character, SkillDefinitionSO skillDef)
    {
        if (character == null || skillDef == null)
            return false;

        return RemoveSkill(character, skillDef.uid);
    }

    public static bool RemoveSkill(CharacterData character, int skillUid)
    {
        if (character == null || character.Skills == null)
            return false;

        SkillRuntimeData runtime = character.Skills.FirstOrDefault(s => s.skillUid == skillUid);

        if (runtime == null)
        {
            Debug.Log("존재하지 않는 스킬 삭제 시도");
            return false;
        }

        character.Skills.Remove(runtime);

        return true;
    }

    public static bool HasSkill(CharacterData character, int skillUid)
    {
        if (character == null || character.Skills == null)
            return false;

        return character.Skills.Any(s => s.skillUid == skillUid);
    }

    public static SkillRuntimeData GetRuntime(CharacterData character, int skillUid)
    {
        if (character == null || character.Skills == null)
            return null;

        return character.Skills.FirstOrDefault(s => s.skillUid == skillUid);
    }

    public static void TryAutoRegisterQuickSlot(CharacterData character, SkillRuntimeData runtime)
    {
        if (character == null || character.Skills == null || runtime == null)
            return;

        if (!runtime.canUse)
            return;

        if (runtime.quickSlot)
            return;

        int currentQuickSlotCount = character.Skills.Count(s => s.quickSlot);

        if (currentQuickSlotCount >= MaxQuickSlotCount)
            return;

        runtime.quickSlot = true;
    }

    public static void RefreshAutoQuickSlots(CharacterData character)
    {
        if (character == null || character.Skills == null)
            return;

        foreach (SkillRuntimeData runtime in character.Skills)
        {
            if (runtime == null)
                continue;

            if (!runtime.canUse)
            {
                runtime.quickSlot = false;
                continue;
            }

            TryAutoRegisterQuickSlot(character, runtime);
        }
    }
}
