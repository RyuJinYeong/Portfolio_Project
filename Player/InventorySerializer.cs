using SoftKitty.InventoryEngine;

public static class InventorySerializer
{
    public static string ExportJson(InventoryHolder holder)
        => holder == null ? null : holder.GetSaveDataJsonString();

    public static void ImportJson(InventoryHolder holder, string json)
    {
        if (holder == null || string.IsNullOrEmpty(json)) return;
        holder.Load(json);       // 에셋 내장 로드
        holder.CalWeight();      // 무게/캐시 재계산(선택)
        // 필요하면 UI 새로고침 지점에서 holder.ItemChanged(...)를 호출
    }
}
