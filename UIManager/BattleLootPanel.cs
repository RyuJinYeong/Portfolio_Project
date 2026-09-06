using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleLootPanel : MonoBehaviour
{
    public RectTransform content;
    public InventoryItemSlotUI itemPrefab;
    public Button collectAllButton;
    public Button closeButton;
    public TMP_Text titleText;
    public Text legacyTitleText;
    public BattleLootPickupMessageUI pickupMessagePrefab;

    private readonly List<InventorySlotData> remainingLoot = new();
    private readonly List<InventoryItemSlotUI> itemViews = new();
    private Action completed;
    private bool finishing;
    private bool showingExpeditionStorage;
    private bool returnToTownAfterLoot;
    private bool preparingReturn;
    private int acquiredGold;
    private BattleLootPickupMessageUI activePickupMessage;

    public void Open(IReadOnlyList<InventorySlotData> loot, Action onCompleted)
    {
        Open(loot, 0, onCompleted);
    }

    public void Open(
        IReadOnlyList<InventorySlotData> loot,
        int gold,
        Action onCompleted)
    {
        ClearViews();
        remainingLoot.Clear();
        finishing = false;
        showingExpeditionStorage = false;
        preparingReturn = false;
        acquiredGold = Mathf.Max(0, gold);
        QuestRouteNode currentNode = QuestManager.Instance?.GetCurrentRouteNode();
        returnToTownAfterLoot = currentNode != null &&
            (currentNode.type == QuestRouteNodeType.Boss ||
             currentNode.nextNodeIds == null || currentNode.nextNodeIds.Count == 0);
        completed = onCompleted;

        if (loot != null)
        {
            foreach (InventorySlotData slot in loot)
            {
                if (slot != null && slot.itemUid > 0 && slot.count > 0)
                    remainingLoot.Add(slot);
            }
        }

        collectAllButton?.onClick.RemoveAllListeners();
        collectAllButton?.onClick.AddListener(OnPrimaryButton);
        closeButton?.onClick.RemoveAllListeners();
        closeButton?.onClick.AddListener(() => Finish(true));

        Transform blocker = transform.Find("BlockControl");
        if (blocker != null)
        {
            blocker.gameObject.SetActive(true);
            blocker.SetAsFirstSibling();
        }

        gameObject.SetActive(true);
        RefreshPanel();
        transform.SetAsLastSibling();
    }

    private void BuildViews()
    {
        if (content == null || itemPrefab == null)
            return;

        IReadOnlyList<InventorySlotData> slots = GetDisplayedSlots();

        foreach (InventorySlotData slot in slots)
        {
            if (slot == null || slot.itemUid <= 0 || slot.count <= 0)
                continue;

            InventoryItemSlotUI view = Instantiate(itemPrefab, content);
            view.gameObject.SetActive(true);
            Action<InventorySlotData, int> clickAction = showingExpeditionStorage
                ? (Action<InventorySlotData, int>)OnExpeditionStorageItemClicked
                : CollectOne;
            view.Bind(slot, clickAction);
            itemViews.Add(view);
        }
    }

    private IReadOnlyList<InventorySlotData> GetDisplayedSlots()
    {
        if (!showingExpeditionStorage)
            return remainingLoot;

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        return playerData?.expeditionStorage ?? new List<InventorySlotData>();
    }

    private void CollectOne(InventorySlotData slot, int button)
    {
        if (button != (int)PointerEventData.InputButton.Left ||
            slot == null ||
            !remainingLoot.Contains(slot))
        {
            return;
        }

        if (!Collect(slot))
        {
            showingExpeditionStorage = true;
            RefreshPanel();
            return;
        }

        ShowPickupMessage(slot);
        remainingLoot.Remove(slot);

        if (remainingLoot.Count == 0)
            Finish();
        else
            RefreshPanel();
    }

    private void OnPrimaryButton()
    {
        if (showingExpeditionStorage)
        {
            if (preparingReturn && remainingLoot.Count == 0)
            {
                Finish(true);
                return;
            }

            showingExpeditionStorage = false;
            RefreshPanel();
            return;
        }

        InventorySlotData[] loot = remainingLoot.ToArray();

        foreach (InventorySlotData slot in loot)
        {
            if (Collect(slot))
            {
                ShowPickupMessage(slot);
                remainingLoot.Remove(slot);
            }
        }

        if (remainingLoot.Count == 0)
        {
            Finish();
            return;
        }

        showingExpeditionStorage = true;
        RefreshPanel();
    }

    private void OnExpeditionStorageItemClicked(InventorySlotData slot, int button)
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;
        List<InventorySlotData> storage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.ExpeditionStorage);

        if (storage == null || slot == null || !storage.Contains(slot))
            return;

        ItemDefinitionSO item = GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(slot.itemUid)
            : null;
        bool directTargetSelection = button == (int)PointerEventData.InputButton.Left;

        if (button != (int)PointerEventData.InputButton.Left &&
            button != (int)PointerEventData.InputButton.Right)
        {
            return;
        }

        if (directTargetSelection &&
            item is not EquipmentDefinitionSO &&
            item is not ConsumableDefinitionSO)
        {
            return;
        }

        InventoryItemTooltipUI.Instance?.Hide();
        UIManager.Instance?.OpenInventoryItemActionMenu(
            slot,
            directTargetSelection,
            false,
            target =>
            {
                if (!UseOrEquip(target, storage, slot))
                    return;

                SharedInventoryUtility.SaveChanges(target);
                RefreshPanel();
            },
            target =>
            {
                if (!SharedInventoryUtility.AppraiseMonsterEssence(target, storage, slot))
                    return;

                SharedInventoryUtility.SaveChanges(target);
                RefreshPanel();
            },
            null,
            () =>
            {
                if (!SharedInventoryUtility.DiscardInventorySlot(storage, slot))
                    return;

                SharedInventoryUtility.SaveChanges();
                RefreshPanel();
            });
    }

    private static bool UseOrEquip(
        CharacterManager target,
        List<InventorySlotData> storage,
        InventorySlotData slot)
    {
        if (target == null || target.character == null ||
            GameDataRegistry.Instance == null)
        {
            return false;
        }

        ItemDefinitionSO item = GameDataRegistry.Instance.GetItem(slot.itemUid);

        if (item is EquipmentDefinitionSO equipment)
        {
            int slotIndex = equipment.equipType == EquipmentType.Ring
                ? GetPreferredRingSlot(target.character.EquipmentSlots)
                : 1;

            return SharedInventoryUtility.EquipFromStorage(
                target,
                storage,
                slot,
                slotIndex);
        }

        return item is ConsumableDefinitionSO &&
               SharedInventoryUtility.UseItem(target, storage, slot);
    }

    private void RefreshPanel()
    {
        ClearViews();

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;
        List<InventorySlotData> storage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.ExpeditionStorage);
        int occupied = CountOccupiedSlots(storage);
        int capacity = SharedInventoryUtility.GetStorageCapacity(
            playerData,
            SharedInventoryType.ExpeditionStorage);
        string title = showingExpeditionStorage
            ? $"원정대 보관함 {occupied}/{capacity}  ·  좌클릭 대상 선택  ·  우클릭 메뉴"
            : acquiredGold > 0
                ? $"획득 전리품 {remainingLoot.Count}개  ·  골드 +{acquiredGold:N0} G"
                : $"획득 전리품 {remainingLoot.Count}개  ·  획득할 아이템을 선택하세요";

        if (preparingReturn && showingExpeditionStorage)
        {
            string goldText = acquiredGold > 0
                ? $" · 이번 전투 골드 +{acquiredGold:N0} G"
                : string.Empty;
            title = $"복귀 준비 · 원정대 보관함 {occupied}/{capacity}{goldText}\n정수를 사용할 수 있습니다. 마을로 복귀하면 남은 정수는 빛을 잃습니다.";
        }

        SetTitle(title);

        if (collectAllButton != null)
        {
            collectAllButton.gameObject.SetActive(true);
            collectAllButton.interactable = true;
            SetButtonText(
                collectAllButton,
                showingExpeditionStorage
                    ? preparingReturn && remainingLoot.Count == 0
                        ? "마을로 복귀"
                        : "전리품으로 돌아가기"
                    : remainingLoot.Count > 0
                        ? "모두 획득"
                        : "확인");
        }

        if (closeButton != null)
        {
            closeButton.interactable = true;
            SetButtonText(
                closeButton,
                preparingReturn
                    ? remainingLoot.Count > 0 ? "남은 전리품 포기하고 복귀" : "마을로 복귀"
                    : remainingLoot.Count > 0 ? "남은 전리품 포기" : "닫기");
        }

        BuildViews();
    }

    private void SetTitle(string value)
    {
        if (titleText != null)
            titleText.text = value;
        if (legacyTitleText != null)
            legacyTitleText.text = value;
    }

    private static int CountOccupiedSlots(List<InventorySlotData> storage)
    {
        int count = 0;

        foreach (InventorySlotData slot in storage ?? new List<InventorySlotData>())
        {
            if (slot != null && slot.itemUid > 0 && slot.count > 0)
                count++;
        }

        return count;
    }

    private static void SetButtonText(Button button, string value)
    {
        if (button == null)
            return;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            tmp.text = value;

        Text legacy = button.GetComponentInChildren<Text>(true);
        if (legacy != null)
            legacy.text = value;
    }

    private static bool Collect(InventorySlotData slot)
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;
        List<InventorySlotData> storage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.ExpeditionStorage);

        if (!SharedInventoryUtility.AddInventorySlot(storage, slot))
            return false;

        if (slot.IsGeneratedEquipment())
            EquipmentInstanceRepository.PromoteRuntimeToPlayer(slot.equipmentInstanceId);

        return true;
    }

    private static int GetPreferredRingSlot(EquipmentSlotData slots)
    {
        if (slots == null || slots.ring1Uid <= 0)
            return 1;

        return slots.ring2Uid <= 0 ? 2 : 1;
    }

    private void ShowPickupMessage(InventorySlotData slot)
    {
        if (slot == null || pickupMessagePrefab == null)
            return;

        if (activePickupMessage == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            Transform parent = canvas != null ? canvas.transform : transform.parent;
            activePickupMessage = Instantiate(pickupMessagePrefab, parent);
            activePickupMessage.name = "BattleLootPickupMessages";

            if (activePickupMessage.transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }
        }

        activePickupMessage.ShowItem(slot);
    }

    private void Finish(bool confirmReturn = false)
    {
        if (finishing)
            return;

        if (returnToTownAfterLoot && (!preparingReturn || !confirmReturn))
        {
            preparingReturn = true;
            showingExpeditionStorage = true;
            InventoryItemTooltipUI.Instance?.Hide();
            UIManager.Instance?.CloseInventoryItemActionMenu();
            SharedInventoryUtility.SaveChanges();
            RefreshPanel();
            return;
        }

        finishing = true;

        foreach (InventorySlotData slot in remainingLoot)
        {
            if (slot != null && slot.IsGeneratedEquipment())
                EquipmentInstanceRepository.RemoveRuntime(slot.equipmentInstanceId);
        }

        remainingLoot.Clear();
        InventoryItemTooltipUI.Instance?.Hide();
        UIManager.Instance?.CloseInventoryItemActionMenu();
        SharedInventoryUtility.SaveChanges();

        Action callback = completed;
        completed = null;
        gameObject.SetActive(false);
        Destroy(gameObject);
        callback?.Invoke();
    }

    private void ClearViews()
    {
        foreach (InventoryItemSlotUI view in itemViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        itemViews.Clear();
    }
}
