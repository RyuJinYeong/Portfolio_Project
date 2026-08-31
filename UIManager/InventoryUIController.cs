using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public static InventoryUIController Instance { get; private set; }

    public SharedInventoryWindowUI companyStorageWindow;
    public SharedInventoryWindowUI expeditionInventoryWindow;
    public CharacterEquipmentWindowUI equipmentWindow;
    public CharacterSkillWindowUI skillWindow;
    public GameObject itemTooltipPrefab;

    public CharacterManager SelectedCharacter { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (itemTooltipPrefab != null)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            Transform parent = rootCanvas != null ? rootCanvas.transform : transform;
            GameObject tooltipObject = Instantiate(itemTooltipPrefab, parent);
            tooltipObject.name = itemTooltipPrefab.name;

            InventoryItemTooltipUI tooltip = tooltipObject.GetComponent<InventoryItemTooltipUI>();
            tooltip.Initialize();
        }
    }

    public void SetSelectedCharacter(CharacterManager characterManager)
    {
        SelectedCharacter = characterManager;

        if (companyStorageWindow != null)
            companyStorageWindow.SetCharacter(characterManager);

        if (expeditionInventoryWindow != null)
            expeditionInventoryWindow.SetCharacter(characterManager);
    }

    public void OpenCompanyStorage()
    {
        if (companyStorageWindow != null)
        {
            companyStorageWindow.inventoryType = SharedInventoryType.CompanyStorage;
            companyStorageWindow.Open(SelectedCharacter);
        }
    }

    public void OpenExpeditionInventory()
    {
        SharedInventoryWindowUI storage = expeditionInventoryWindow != null
            ? expeditionInventoryWindow
            : companyStorageWindow;

        if (storage != null)
        {
            storage.inventoryType = SharedInventoryType.ExpeditionStorage;
            storage.Open(SelectedCharacter);
        }
    }

    public void OpenEquipment()
    {
        if (equipmentWindow != null && SelectedCharacter != null)
        {
            equipmentWindow.Open(
                SelectedCharacter,
                GetAccessibleInventoryType());
        }
    }

    public void OpenSkills()
    {
        if (skillWindow != null && SelectedCharacter != null)
            skillWindow.Open(SelectedCharacter);
    }

    public void ToggleAccessibleStorageFromEquipment()
    {
        if (equipmentWindow == null || SelectedCharacter == null)
            return;

        SharedInventoryWindowUI storage = GetAccessibleStorageWindow();

        if (storage == null)
            return;

        if (storage.gameObject.activeInHierarchy)
        {
            storage.Close();
            SetWindowPosition(equipmentWindow, Vector2.zero);
            return;
        }

        SharedInventoryWindowUI otherStorage = storage == companyStorageWindow
            ? expeditionInventoryWindow
            : companyStorageWindow;

        if (otherStorage != null)
            otherStorage.Close();

        storage.SetCharacter(SelectedCharacter);
        PositionEquipmentAndStorage(storage);
        storage.Open(SelectedCharacter);
    }

    public void CloseAttachedStorage()
    {
        if (companyStorageWindow != null && companyStorageWindow.gameObject.activeInHierarchy)
            companyStorageWindow.Close();

        if (expeditionInventoryWindow != null && expeditionInventoryWindow.gameObject.activeInHierarchy)
            expeditionInventoryWindow.Close();

        SetWindowPosition(equipmentWindow, Vector2.zero);
    }

    public void OnStorageWindowClosed(SharedInventoryWindowUI storage)
    {
        if (equipmentWindow != null && equipmentWindow.gameObject.activeInHierarchy)
            SetWindowPosition(equipmentWindow, Vector2.zero);
    }

    public SharedInventoryWindowUI GetAccessibleStorageWindow()
    {
        SharedInventoryType inventoryType = GetAccessibleInventoryType();
        SharedInventoryWindowUI storage = inventoryType == SharedInventoryType.CompanyStorage
            ? companyStorageWindow
            : expeditionInventoryWindow != null
                ? expeditionInventoryWindow
                : companyStorageWindow;

        if (storage != null)
            storage.inventoryType = inventoryType;

        return storage;
    }

    private void PositionEquipmentAndStorage(SharedInventoryWindowUI storage)
    {
        RectTransform equipmentRect = equipmentWindow.transform as RectTransform;
        RectTransform storageRect = storage.transform as RectTransform;

        if (equipmentRect == null || storageRect == null)
            return;

        const float gap = 10f;
        float equipmentWidth = equipmentRect.rect.width;
        float storageWidth = storageRect.rect.width;

        SetWindowPosition(equipmentWindow, new Vector2(-(storageWidth + gap) * 0.5f, 0f));
        SetWindowPosition(storage, new Vector2((equipmentWidth + gap) * 0.5f, 0f));
    }

    private static void SetWindowPosition(MonoBehaviour window, Vector2 position)
    {
        if (window != null && window.transform is RectTransform rect)
            rect.anchoredPosition = position;
    }

    public void CloseAll()
    {
        if (companyStorageWindow != null)
            companyStorageWindow.Close();

        if (expeditionInventoryWindow != null)
            expeditionInventoryWindow.Close();

        if (equipmentWindow != null)
            equipmentWindow.Close();

        if (skillWindow != null)
            skillWindow.Close();
    }

    private SharedInventoryType GetAccessibleInventoryType()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        return playerData != null && playerData.currentStage == "Town"
            ? SharedInventoryType.CompanyStorage
            : SharedInventoryType.ExpeditionStorage;
    }
}
