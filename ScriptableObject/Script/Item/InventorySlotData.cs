using System;

[Serializable]
public class InventorySlotData
{
    public int itemUid;
    public int count;

    // 장비일 경우에만 사용.
    // 소비품/재료/기타 아이템이면 null 또는 빈 문자열.
    public string equipmentInstanceId;

    public bool IsGeneratedEquipment()
    {
        return !string.IsNullOrEmpty(equipmentInstanceId);
    }
}