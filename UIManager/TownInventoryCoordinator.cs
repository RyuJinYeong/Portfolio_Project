using UnityEngine;
using UnityEngine.UI;

public class TownInventoryCoordinator : MonoBehaviour
{
    public static TownInventoryCoordinator Instance { get; private set; }

    [Header("Town Buttons")]
    public Button storageButton;
    public Button characterManagementButton;
    public Button questButton;
    public Button recruitButton;

    [Header("Town Windows")]
    public TownCharacterManagementPanel characterManagementPanel;
    public InventoryUIController inventoryUIController;
    public GameObject questPanel;
    public GameObject recruitPanel;

    [Header("Camera Focus")]
    public string storageFocusKey = "Storage";
    public string characterManagementFocusKey = "CharacterManage";
    public string questFocusKey = "Quest";
    public string recruitFocusKey = "Employ";

    private UIManager legacyUIManager;
    private bool townWindowSession;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        legacyUIManager = UIManager.Instance != null
            ? UIManager.Instance
            : FindObjectOfType<UIManager>();

        if (storageButton != null &&
            characterManagementPanel != null &&
            storageButton.transform.IsChildOf(characterManagementPanel.transform))
        {
            GameObject townMenuCanvas = GameObject.Find("TownMenuCanvas");
            Transform townStorageButton = townMenuCanvas != null
                ? townMenuCanvas.transform.Find("StorageButton")
                : null;

            if (townStorageButton != null)
                storageButton = townStorageButton.GetComponent<Button>();
        }

        if (characterManagementPanel != null)
            characterManagementPanel.townCoordinator = this;
    }

    private void OnEnable()
    {
        if (storageButton != null)
            storageButton.onClick.AddListener(OpenCompanyStorage);

        if (characterManagementButton != null)
            characterManagementButton.onClick.AddListener(OpenCharacterManagement);

        if (questButton != null)
            questButton.onClick.AddListener(OpenQuest);

        if (recruitButton != null)
            recruitButton.onClick.AddListener(OpenRecruit);
    }

    private void OnDisable()
    {
        if (storageButton != null)
            storageButton.onClick.RemoveListener(OpenCompanyStorage);

        if (characterManagementButton != null)
            characterManagementButton.onClick.RemoveListener(OpenCharacterManagement);

        if (questButton != null)
            questButton.onClick.RemoveListener(OpenQuest);

        if (recruitButton != null)
            recruitButton.onClick.RemoveListener(OpenRecruit);

        RestoreLegacyUIManager();
    }

    private void Update()
    {
        if (!townWindowSession)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            CloseTopWindow();

        ReturnCameraWhenAllWindowsClosed();
    }

    public void OpenCompanyStorage()
    {
        BeginTownWindowSession(storageFocusKey);
        CloseManagedWindows();

        if (inventoryUIController != null)
        {
            inventoryUIController.SetSelectedCharacter(null);
            SetWindowPosition(inventoryUIController.companyStorageWindow, Vector2.zero);
            inventoryUIController.OpenCompanyStorage();
        }
    }

    public void OpenCharacterManagement()
    {
        BeginTownWindowSession(characterManagementFocusKey);
        CloseManagedWindows();

        if (characterManagementPanel != null)
            characterManagementPanel.OpenAndBuild();
    }

    public void OpenQuest()
    {
        BeginTownWindowSession(questFocusKey);
        CloseManagedWindows();

        if (questPanel != null)
            questPanel.SetActive(true);
    }

    public void OpenRecruit()
    {
        BeginTownWindowSession(recruitFocusKey);
        CloseManagedWindows();

        if (recruitPanel != null)
            recruitPanel.SetActive(true);
    }

    public void OpenCharacterEquipment(CharacterManager characterManager)
    {
        if (characterManager == null || inventoryUIController == null)
            return;

        BeginTownWindowSession(characterManagementFocusKey);
        inventoryUIController.SetSelectedCharacter(characterManager);
        SetWindowPosition(inventoryUIController.companyStorageWindow, new Vector2(-330f, 0f));
        SetWindowPosition(inventoryUIController.equipmentWindow, new Vector2(330f, 0f));
        inventoryUIController.OpenCompanyStorage();
        inventoryUIController.OpenEquipment();
    }

    public void OpenCharacterSkills(CharacterManager characterManager)
    {
        if (characterManager == null || inventoryUIController == null)
            return;

        BeginTownWindowSession(characterManagementFocusKey);
        inventoryUIController.SetSelectedCharacter(characterManager);
        inventoryUIController.OpenSkills();
    }

    public void CloseCharacterManagement()
    {
        if (characterManagementPanel != null)
            characterManagementPanel.gameObject.SetActive(false);

        ReturnCameraWhenAllWindowsClosed();
    }

    public void CloseTopWindow()
    {
        if (inventoryUIController != null && IsOpen(inventoryUIController.skillWindow))
        {
            inventoryUIController.skillWindow.Close();
            return;
        }

        if (inventoryUIController != null && IsOpen(inventoryUIController.equipmentWindow))
        {
            inventoryUIController.equipmentWindow.Close();
            return;
        }

        if (inventoryUIController != null && IsOpen(inventoryUIController.companyStorageWindow))
        {
            inventoryUIController.companyStorageWindow.Close();
            return;
        }

        if (inventoryUIController != null && IsOpen(inventoryUIController.expeditionInventoryWindow))
        {
            inventoryUIController.expeditionInventoryWindow.Close();
            return;
        }

        if (characterManagementPanel != null &&
            characterManagementPanel.gameObject.activeInHierarchy)
        {
            characterManagementPanel.gameObject.SetActive(false);
            return;
        }

        if (recruitPanel != null && recruitPanel.activeInHierarchy)
        {
            recruitPanel.SetActive(false);
            return;
        }

        if (questPanel != null && questPanel.activeInHierarchy)
            questPanel.SetActive(false);
    }

    public void CloseManagedWindows()
    {
        if (inventoryUIController != null)
            inventoryUIController.CloseAll();

        if (characterManagementPanel != null)
            characterManagementPanel.gameObject.SetActive(false);

        if (recruitPanel != null)
            recruitPanel.SetActive(false);

        if (questPanel != null)
            questPanel.SetActive(false);
    }

    private void BeginTownWindowSession(string focusKey)
    {
        townWindowSession = true;

        if (legacyUIManager == null)
            legacyUIManager = UIManager.Instance;

        if (legacyUIManager != null)
            legacyUIManager.enabled = false;

        CameraFocusRig.Instance?.Focus(focusKey);
    }

    private void ReturnCameraWhenAllWindowsClosed()
    {
        if (!townWindowSession || HasOpenManagedWindow())
            return;

        townWindowSession = false;
        CameraFocusRig.Instance?.FocusHome();
        RestoreLegacyUIManager();
    }

    private bool HasOpenManagedWindow()
    {
        if (characterManagementPanel != null &&
            characterManagementPanel.gameObject.activeInHierarchy)
        {
            return true;
        }

        if ((recruitPanel != null && recruitPanel.activeInHierarchy) ||
            (questPanel != null && questPanel.activeInHierarchy))
        {
            return true;
        }

        return inventoryUIController != null &&
               (IsOpen(inventoryUIController.companyStorageWindow) ||
                IsOpen(inventoryUIController.expeditionInventoryWindow) ||
                IsOpen(inventoryUIController.equipmentWindow) ||
                IsOpen(inventoryUIController.skillWindow));
    }

    private void RestoreLegacyUIManager()
    {
        if (legacyUIManager != null)
            legacyUIManager.enabled = true;
    }

    private static bool IsOpen(MonoBehaviour window)
    {
        return window != null && window.gameObject.activeInHierarchy;
    }

    private static void SetWindowPosition(MonoBehaviour window, Vector2 position)
    {
        if (window != null && window.transform is RectTransform rect)
            rect.anchoredPosition = position;
    }
}
