using System.Collections;
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
    private ScrollRect listScrollRect;
    private Coroutine scrollResetRoutine;

    private void OnEnable()
    {
        SharedInventoryUtility.InventoryChanged += Refresh;
        SharedInventoryUtility.CharacterChanged += OnCharacterChanged;

        if (actionButton != null)
            actionButton.onClick.AddListener(OpenSelectedItemTargetMenu);

        if (transferButton != null)
            transferButton.gameObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Refresh();
        scrollResetRoutine = StartCoroutine(ResetScrollToTop());
    }

    private void OnDisable()
    {
        if (scrollResetRoutine != null)
        {
            StopCoroutine(scrollResetRoutine);
            scrollResetRoutine = null;
        }

        SharedInventoryUtility.InventoryChanged -= Refresh;
        SharedInventoryUtility.CharacterChanged -= OnCharacterChanged;

        if (actionButton != null)
            actionButton.onClick.RemoveListener(OpenSelectedItemTargetMenu);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open(CharacterManager character = null)
    {
        selectedCharacter = character;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        Refresh();
        if (scrollResetRoutine != null)
            StopCoroutine(scrollResetRoutine);
        scrollResetRoutine = StartCoroutine(ResetScrollToTop());
    }

    public void SetCharacter(CharacterManager character)
    {
        selectedCharacter = character;
        RefreshHeader();
        RefreshButtons();
    }

    public void Close()
    {
        UIManager.Instance?.CloseInventoryItemActionMenu();

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
        displayedCapacity = SharedInventoryUtility.GetStorageCapacity(
            playerData,
            inventoryType);

        int viewCount = Mathf.Max(displayedCapacity, occupiedSlotCount);

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

        if (button == (int)UnityEngine.EventSystems.PointerEventData.InputButton.Left)
            OpenItemMenu(slot, true);
        else if (button == (int)UnityEngine.EventSystems.PointerEventData.InputButton.Right)
        {
            if (!HandleContextRightClick(slot))
                OpenItemMenu(slot, false);
        }
    }

    private bool HandleContextRightClick(InventorySlotData slot)
    {
        InventoryUIController controller = InventoryUIController.Instance;

        if (slot == null || controller == null)
            return false;

        bool equipmentOpen = controller.equipmentWindow != null &&
                             controller.equipmentWindow.gameObject.activeInHierarchy;
        bool companyStorageOpen = controller.companyStorageWindow != null &&
                                  controller.companyStorageWindow.gameObject.activeInHierarchy;
        bool expeditionStorageOpen = controller.expeditionInventoryWindow != null &&
                                     controller.expeditionInventoryWindow.gameObject.activeInHierarchy;
        int openWindowCount = (equipmentOpen ? 1 : 0) +
                              (companyStorageOpen ? 1 : 0) +
                              (expeditionStorageOpen ? 1 : 0);

        if (openWindowCount == 2 && equipmentOpen)
        {
            ItemDefinitionSO item = GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetItem(slot.itemUid)
                : null;

            if (item is EquipmentDefinitionSO)
            {
                UIManager.Instance?.CloseInventoryItemActionMenu();
                UseOrEquip(controller.SelectedCharacter, slot);
                return true;
            }
        }

        if (openWindowCount == 2 && companyStorageOpen && expeditionStorageOpen)
        {
            PlayerData playerData = PlayerManager.Instance != null
                ? PlayerManager.Instance.GetCurrentPlayerData()
                : null;
            SharedInventoryType destinationType =
                inventoryType == SharedInventoryType.CompanyStorage
                    ? SharedInventoryType.ExpeditionStorage
                    : SharedInventoryType.CompanyStorage;
            int count = slot.IsGeneratedEquipment() ? 1 : Mathf.Max(1, slot.count);

            UIManager.Instance?.CloseInventoryItemActionMenu();

            if (SharedInventoryUtility.MoveItem(
                    playerData,
                    inventoryType,
                    destinationType,
                    slot,
                    count))
            {
                SharedInventoryUtility.SaveChanges();
            }

            return true;
        }

        return false;
    }

    private void OpenSelectedItemTargetMenu()
    {
        if (selectedSlot == null)
            return;

        OpenItemMenu(selectedSlot, true);
    }

    private void OpenItemMenu(InventorySlotData slot, bool directTargetSelection)
    {
        if (slot == null || UIManager.Instance == null)
            return;

        ItemDefinitionSO item = GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(slot.itemUid)
            : null;

        if (directTargetSelection &&
            item is not EquipmentDefinitionSO &&
            item is not ConsumableDefinitionSO)
        {
            return;
        }

        InventoryItemTooltipUI.Instance?.Hide();
        TooltipManager.Instance?.HideTooltip();

        UIManager.Instance.OpenInventoryItemActionMenu(
            slot,
            directTargetSelection,
            IsInTown(),
            target => UseOrEquip(target, slot),
            target => Appraise(target, slot),
            () => Sell(slot),
            () => Discard(slot));
    }

    private void Appraise(CharacterManager target, InventorySlotData slot)
    {
        if (target == null || target.character == null || slot == null ||
            PlayerManager.Instance == null)
        {
            return;
        }

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();
        List<InventorySlotData> storage =
            SharedInventoryUtility.GetStorage(playerData, inventoryType);

        if (SharedInventoryUtility.AppraiseMonsterEssence(target, storage, slot))
            SharedInventoryUtility.SaveChanges(target);
    }

    private void UseOrEquip(CharacterManager target, InventorySlotData slot)
    {
        if (target == null || target.character == null || slot == null ||
            PlayerManager.Instance == null || GameDataRegistry.Instance == null)
        {
            return;
        }

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();
        List<InventorySlotData> storage =
            SharedInventoryUtility.GetStorage(playerData, inventoryType);

        if (storage == null || !storage.Contains(slot))
            return;

        ItemDefinitionSO item = GameDataRegistry.Instance.GetItem(slot.itemUid);
        bool changed = false;

        if (item is EquipmentDefinitionSO equipment)
        {
            int slotIndex = equipment.equipType == EquipmentType.Ring
                ? GetPreferredRingSlot(target.character.EquipmentSlots)
                : 1;

            changed = SharedInventoryUtility.EquipFromStorage(
                target,
                storage,
                slot,
                slotIndex);
        }
        else if (item is ConsumableDefinitionSO)
        {
            changed = SharedInventoryUtility.UseItem(
                target,
                storage,
                slot);
        }

        if (changed)
        {
            selectedCharacter = target;
            SharedInventoryUtility.SaveChanges(target);
        }
    }

    private void Sell(InventorySlotData slot)
    {
        if (!IsInTown() || PlayerManager.Instance == null)
            return;

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();
        List<InventorySlotData> storage =
            SharedInventoryUtility.GetStorage(playerData, inventoryType);

        if (SharedInventoryUtility.SellInventorySlot(playerData, storage, slot))
            SharedInventoryUtility.SaveChanges();
    }

    private void Discard(InventorySlotData slot)
    {
        if (PlayerManager.Instance == null)
            return;

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();
        List<InventorySlotData> storage =
            SharedInventoryUtility.GetStorage(playerData, inventoryType);

        if (SharedInventoryUtility.DiscardInventorySlot(storage, slot))
            SharedInventoryUtility.SaveChanges();
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
            itemCountText.text = $"  {occupiedSlotCount}/{displayedCapacity}";

        if (legacyItemCountText != null)
            legacyItemCountText.text = $"  {occupiedSlotCount}/{displayedCapacity}";

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
        bool hasSelection = selectedSlot != null;

        ItemDefinitionSO item = hasSelection && GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(selectedSlot.itemUid)
            : null;

        bool canAct = item is EquipmentDefinitionSO || item is ConsumableDefinitionSO;

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
            {
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }
        }

        itemViews.Clear();
    }

    private IEnumerator ResetScrollToTop()
    {
        yield return null;

        Animation openingAnimation = GetComponent<Animation>();
        if (openingAnimation != null && openingAnimation.isPlaying && openingAnimation.clip != null)
        {
            AnimationState state = openingAnimation[openingAnimation.clip.name];
            float remaining = state.length - state.time;
            if (remaining > 0f && state.speed > 0f && Time.timeScale > 0f)
                yield return new WaitForSecondsRealtime(remaining / (state.speed * Time.timeScale));
        }

        if (listScrollRect == null && content != null)
            listScrollRect = content.GetComponentInParent<ScrollRect>();

        if (listScrollRect == null)
        {
            scrollResetRoutine = null;
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)listScrollRect.transform);
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        Canvas.ForceUpdateCanvases();
        listScrollRect.StopMovement();
        listScrollRect.verticalNormalizedPosition = 1f;
        scrollResetRoutine = null;
    }

    private static int GetPreferredRingSlot(EquipmentSlotData slots)
    {
        if (slots == null || slots.ring1Uid <= 0)
            return 1;

        if (slots.ring2Uid <= 0)
            return 2;

        return 1;
    }

    private static bool IsInTown()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        return playerData == null || playerData.currentStage == "Town";
    }

}
