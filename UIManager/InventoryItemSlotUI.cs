using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public RawImage icon;
    public Texture emptyIcon;
    public Image frame;
    public Color emptyFrameColor = Color.white;
    public TMP_Text nameText;
    public TMP_Text countText;
    public Text legacyNameText;
    public Text legacyCountText;
    public GameObject selectedFrame;

    public InventorySlotData Slot { get; private set; }

    private Action<InventorySlotData, int> clickCallback;

    public void Bind(
        InventorySlotData slot,
        Action<InventorySlotData, int> onClick = null)
    {
        Slot = slot;
        clickCallback = onClick;

        ItemDefinitionSO item = slot != null && GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(slot.itemUid)
            : null;

        if (icon != null)
        {
            icon.texture = item != null && item.icon != null ? item.icon : emptyIcon;
            icon.enabled = icon.texture != null;
        }

        if (nameText != null)
            nameText.text = GetDisplayName(slot, item);

        if (legacyNameText != null)
            legacyNameText.text = GetDisplayName(slot, item);

        if (countText != null)
        {
            bool showCount = slot != null && slot.count > 1;
            countText.gameObject.SetActive(showCount);
            countText.text = showCount ? slot.count.ToString() : "";
        }

        if (legacyCountText != null)
        {
            bool showCount = slot != null && slot.count > 1;
            legacyCountText.gameObject.SetActive(showCount);
            legacyCountText.text = showCount ? slot.count.ToString() : "";
        }

        if (frame != null)
            frame.color = slot == null ? emptyFrameColor : GetFrameColor(slot, item);

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null)
            selectedFrame.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Slot == null)
            return;

        clickCallback?.Invoke(Slot, (int)eventData.button);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Slot == null)
            return;

        if (InventoryItemTooltipUI.Instance != null)
        {
            InventoryUIController controller = InventoryUIController.Instance;
            CharacterManager comparisonCharacter = controller != null &&
                                                   controller.equipmentWindow != null &&
                                                   controller.equipmentWindow.gameObject.activeInHierarchy
                ? controller.SelectedCharacter
                : null;

            InventoryItemTooltipUI.Instance.Show(
                Slot,
                comparisonCharacter,
                Input.mousePosition);
        }
        else if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowItemTooltip(Slot, Input.mousePosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryItemTooltipUI.Instance != null)
            InventoryItemTooltipUI.Instance.Hide();

        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();
    }

    private static string GetDisplayName(
        InventorySlotData slot,
        ItemDefinitionSO item)
    {
        if (slot == null || item == null)
            return "";

        if (slot.IsMonsterEssence() && !string.IsNullOrEmpty(slot.essenceMonsterName))
            return $"{slot.essenceMonsterName}의 정수";

        if (item is EquipmentDefinitionSO)
        {
            EquipmentRuntimeData runtime = EquipmentRuntimeResolver.Resolve(
                slot.itemUid,
                slot.equipmentInstanceId);

            if (runtime != null && !string.IsNullOrEmpty(runtime.displayName))
                return runtime.displayName;
        }

        return item.itemName;
    }

    private static Color GetFrameColor(
        InventorySlotData slot,
        ItemDefinitionSO item)
    {
        if (slot == null || !(item is EquipmentDefinitionSO))
            return Color.white;

        EquipmentRuntimeData runtime = EquipmentRuntimeResolver.Resolve(
            slot.itemUid,
            slot.equipmentInstanceId);

        return runtime != null
            ? EquipmentRarityUtility.GetColor(runtime.rarity)
            : Color.white;
    }
}
