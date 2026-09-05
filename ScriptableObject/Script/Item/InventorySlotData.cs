using System;
using System.Collections.Generic;

[Serializable]
public class MonsterEssenceTraitData
{
    public int traitId;
    public int point;
}

[Serializable]
public class InventorySlotData
{
    public int itemUid;
    public int count;

    // 장비일 경우에만 사용.
    // 소비품/재료/기타 아이템이면 null 또는 빈 문자열.
    public string equipmentInstanceId;

    // 몬스터 정수일 때만 사용하는 퀘스트/몬스터/특성 스냅샷.
    public string essenceQuestId;
    public int essenceMonsterRoleId;
    public string essenceMonsterName;
    public List<MonsterEssenceTraitData> essenceTraits = new();

    public bool IsGeneratedEquipment()
    {
        return !string.IsNullOrEmpty(equipmentInstanceId);
    }

    public bool IsMonsterEssence()
    {
        return !string.IsNullOrEmpty(essenceQuestId);
    }
}
