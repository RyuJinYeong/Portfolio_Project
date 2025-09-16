using SoftKitty.InventoryEngine;

public static class InventorySerializer
{
    public static string ExportJson(InventoryHolder holder)
    {
        if (holder == null) return null;
        return holder.GetSaveDataJsonString();
    }

    public static void ImportJson(InventoryHolder holder, string json)
    {
        if (holder == null || string.IsNullOrEmpty(json)) return;
        holder.Load(json);
    }
}
