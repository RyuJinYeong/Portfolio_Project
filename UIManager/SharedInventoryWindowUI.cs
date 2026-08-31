using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SharedInventoryWindowUI : MonoBehaviour
{
    public SharedInventoryType inventoryType;

    [Header("List")]
    public RectTransform content;
    public InventoryItemSlotUI itemPrefab;

    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text itemCountText;
    public TMP_Text selectedCharacterText;
    public TMP_Text actionButtonText;
    public Text legacyTitleText;
    public Text legacyItemCountText;
    public Text legacySelectedCharacterText;
    public Text legacyActionButtonText;
    public Text legacyGoldText;

    [Header("Buttons")]
    public Button actionButton;
    public Button transferButton;
    public Button closeButton;

    private readonly List<InventoryItemSlotUI> itemViews = new();
    private InventorySlotData selectedSlot;
    private CharacterManager selectedCharacter;
    private int occupiedSlotCount;
    private int displayedCapacity;

    private void OnEnable()
    {
        SharedInventoryUtility.InventoryChanged += Refresh;
        SharedInventoryUtility.CharacterChanged += OnCharacterChanged;

        if (inventoryType != SharedInventoryType.CompanyStorage && actionButton != null)
            actionButton.onClick.AddListener(UseOrEquipSelected);

        if (inventoryType != SharedInventoryType.CompanyStorage && transferButton != null)
            transferButton.onClick.AddListener(TransferSelected);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Refresh();
    }

    private void OnDisable()
    {
        SharedInventoryUtility.InventoryChanged -= Refresh;
        SharedInventoryUtility.CharacterChanged -= OnCharacterChanged;

        if (inventoryType != SharedInventoryType.CompanyStorage && actionButton != null)
            actionButton.onClick.RemoveListener(UseOrEquipSelected);

        if (inventoryType != SharedInventoryType.CompanyStorage && transferButton != null)
            transferButton.onClick.RemoveListener(TransferSelected);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open(CharacterManager character = null)
    {
        selectedCharacter = character;
        gameObject.SetActive(true);
        Refresh();
    }

    public void SetCharacter(CharacterManager character)
    {
        selectedCharacter = character;
        RefreshHeader();
        RefreshButtons();
    }

    public void Close()
    {
        if (InventoryItemTooltipUI.Instance != null)
            InventoryItemTooltipUI.Instance.Hide();

        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();

        gameObject.SetActive(false);

        if (InventoryUIController.Instance != null)
            InventoryUIController.Instance.OnStorageWindowClosed(this);
    }

    public void Refresh()
    {
        ClearItemViews();
        selectedSlot = null;

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        List<InventorySlotData> storage =
            SharedInventoryUtility.GetStorage(playerData, inventoryType);

        List<InventorySlotData> occupiedSlots = new List<InventorySlotData>();

        if (storage != null)
        {
            foreach (InventorySlotData slot in storage)
            {
                if (slot != null && slot.itemUid > 0 && slot.count > 0)
                    occupiedSlots.Add(slot);
            }
        }

        occupiedSlotCount = occupiedSlots.Count;
        displayedCapacity = inventoryType == SharedInventoryType.CompanyStorage
            ? SharedInventoryUtility.GetStorageCapacity(playerData, inventoryType)
            : occupiedSlotCount;

        int viewCount = inventoryType == SharedInventoryType.CompanyStorage
            ? Mathf.Max(displayedCapacity, occupiedSlotCount)
            : occupiedSlotCount;

        if (content != null && itemPrefab != null)
        {
            for (int i = 0; i < viewCount; i++)
            {
                InventoryItemSlotUI view = Instantiate(itemPrefab, content);
                view.gameObject.SetActive(true);
                view.Bind(i < occupiedSlotCount ? occupiedSlots[i] : null, OnItemClicked);
                itemViews.Add(view);
            }
        }

        RefreshHeader();
        RefreshButtons();
    }

    private void OnItemClicked(InventorySlotData slot, int button)
    {
        selectedSlot = slot;

        foreach (InventoryItemSlotUI view in itemViews)
            view.SetSelected(view.Slot == selectedSlot);

        RefreshButtons();

        if (button == (int)UnityEngine.EventSystems.PointerEventData.InputButton.Right)
            UseOrEquipSelected();
    }

    private void UseOrEquipSelected()
    {
        if (selectedSlot == null || selectedCharacter == null)
            return;

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();
        List<InventorySlotData> storage =
            SharedInventoryUtility.GetStorage(playerData, inventoryType);

        ItemDefinitionSO item = GameDataRegistry.Instance.GetItem(selectedSlot.itemUid);
        bool changed = false;

        if (item is EquipmentDefinitionSO equipment)
        {
            int slotIndex = equipment.equipType == EquipmentType.Ring
                ? GetPreferredRingSlot(selectedCharacter.character.EquipmentSlots)
                : 1;

            changed = SharedInventoryUtility.EquipFromStorage(
                selectedCharacter,
                storage,
                selectedSlot,
                slotIndex);
        }
        else if (item is ConsumableDefinitionSO)
        {
            changed = SharedInventoryUtility.UseItem(
                selectedCharacter,
                storage,
                selectedSlot);
        }

        if (changed)
            SharedInventoryUtility.SaveChanges(selectedCharacter);
    }

    private void TransferSelected()
    {
        if (selectedSlot == null)
            return;

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();
        SharedInventoryType destination = inventoryType == SharedInventoryType.CompanyStorage
            ? SharedInventoryType.ExpeditionStorage
            : SharedInventoryType.CompanyStorage;

        int count = selectedSlot.IsGeneratedEquipment()
            ? 1
            : selectedSlot.count;

        if (SharedInventoryUtility.MoveItem(
                playerData,
                inventoryType,
                destination,
                selectedSlot,
                count))
        {
            SharedInventoryUtility.SaveChanges();
        }
    }

    private void RefreshHeader()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (titleText != null)
        {
            titleText.text = inventoryType == SharedInventoryType.CompanyStorage
                ? "Storage"
                : "원정대 창고";
        }

        if (legacyTitleText != null)
        {
            legacyTitleText.text = inventoryType == SharedInventoryType.CompanyStorage
                ? "Storage"
                : "원정대 창고";
        }

        if (itemCountText != null)
        {
            itemCountText.text = inventoryType == SharedInventoryType.CompanyStorage
                ? $"  {occupiedSlotCount}/{displayedCapacity}"
                : occupiedSlotCount.ToString();
        }

        if (legacyItemCountText != null)
        {
            legacyItemCountText.text = inventoryType == SharedInventoryType.CompanyStorage
                ? $"  {occupiedSlotCount}/{displayedCapacity}"
                : occupiedSlotCount.ToString();
        }

        if (legacyGoldText != null)
        {
            legacyGoldText.text = playerData != null
                ? playerData.gold.ToString("N0")
                : "0";
        }

        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = selectedCharacter != null && selectedCharacter.character != null
                ? selectedCharacter.character.Name
                : "대상 캐릭터 없음";
        }

        if (legacySelectedCharacterText != null)
        {
            legacySelectedCharacterText.text = selectedCharacter != null && selectedCharacter.character != null
                ? selectedCharacter.character.Name
                : "대상 캐릭터 없음";
        }
    }

    private void RefreshButtons()
    {
        if (inventoryType == SharedInventoryType.CompanyStorage)
            return;

        bool hasSelection = selectedSlot != null;

        if (transferButton != null)
            transferButton.interactable = hasSelection;

        ItemDefinitionSO item = hasSelection && GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(selectedSlot.itemUid)
            : null;

        bool canAct = selectedCharacter != null &&
                      (item is EquipmentDefinitionSO || item is ConsumableDefinitionSO);

        if (actionButton != null)
            actionButton.interactable = canAct;

        if (actionButtonText != null)
        {
            actionButtonText.text = item is EquipmentDefinitionSO
                ? "장착"
                : item is ConsumableDefinitionSO
                    ? "사용"
                    : "선택";
        }

        if (legacyActionButtonText != null)
        {
            legacyActionButtonText.text = item is EquipmentDefinitionSO
                ? "장착"
                : item is ConsumableDefinitionSO
                    ? "사용"
                    : "선택";
        }
    }

    private void OnCharacterChanged(CharacterManager manager)
    {
        if (manager == selectedCharacter)
            RefreshHeader();
    }

    private void ClearItemViews()
    {
        foreach (InventoryItemSlotUI view in itemViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        itemViews.Clear();
    }

    private static int GetPreferredRingSlot(EquipmentSlotData slots)
    {
        if (slots == null || slots.ring1Uid <= 0)
            return 1;

        if (slots.ring2Uid <= 0)
            return 2;

        return 1;
    }

}
