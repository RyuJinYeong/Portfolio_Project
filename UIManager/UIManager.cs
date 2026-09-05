using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Roots")]
    public GameObject battleUiRoot; // 전투 UI 루트
    public GameObject townUiRoot;   // 마을 UI 루트
    public GameObject TownMenuCanvas; // 마을 UI - 월드 스페이스 메뉴 캔버스

    [Header("Town Buttons")]
    public Button storageButton;
    public Button characterManagementButton;
    public Button questButton;
    public Button recruitButton;

    [Header("Town Windows")]
    public TownCharacterManagementPanel characterManagementPanel;
    public TownCharacterManagementPanel partyManagementPanel;
    public InventoryUIController inventoryUIController;
    public GameObject questPanel;
    public GameObject recruitPanel;
    public PartyFormationPanel partyFormationPanel;
    public QuestNodeMapPanel questNodeMapPanel;

    [Header("Global Windows")]
    public LevelGrowthPanel levelGrowthPanel;
    [SerializeField] private GameObject battleDefeatDialogPrefab;
    [SerializeField] private BattleLootPanel battleLootPanelPrefab;
    [SerializeField] private InventoryItemActionMenuUI inventoryItemActionMenu;

    [Header("Expedition Buttons")]
    public GameObject expeditionButtonRoot;
    public GameObject expeditionInventoryButton;
    public GameObject expeditionPartyManagementButton;
    public GameObject expeditionMapButton;

    private GameObject activeBattleDefeatDialog;

    [Header("Camera Focus")]
    public string storageFocusKey = "Storage";
    public string characterManagementFocusKey = "CharacterManage";
    public string questFocusKey = "Quest";
    public string recruitFocusKey = "Employ";

    // 외부(파티편성/게임매니저)로 이벤트 넘겨줄 훅
    public System.Action<QuestDef> onQuestAcceptRequest;

    QuestPanel _questPanel;
    private bool townWindowSession;

    public GameObject turnOrderPanel;  // 상단 턴 큐 패널
    public GameObject characterPortraitPrefab;  // 캐릭터 초상화 프리팹

    public TextMeshProUGUI turnTimerText; // 남은 턴 시간을 표시하는 텍스트

    public GameObject damageTextPrefab;  // 데미지 텍스트 프리팹

    public Button turnEndButton; // 턴 종료 버튼 추가
    public Button counterTurnEndButton; // 자동 대응 버튼 추가

    public GameObject synergyInfoPanel; // 시너지 정보가 표시되는 패널

    public GameObject skillQueueFramePrefab; // 스킬 큐를 표시할 프레임 Prefab
    public GameObject skillIconPrefab; // 스킬 아이콘 Prefab
    public Transform counterSkillPanel; // 하단부의 방어자, 방어 대상 스킬 정보 패널    

    public RawImage characterPortrait;
    public Image characterHpImage;
    public TextMeshProUGUI characterName;

    public TextMeshProUGUI currentHP;
    public TextMeshProUGUI currentStamina;
    public TextMeshProUGUI currentMental;

    public TextMeshProUGUI characterHP;
    public TextMeshProUGUI characterStamina;
    public TextMeshProUGUI characterMental;

    public TextMeshProUGUI characterPhysicalAttack;
    public TextMeshProUGUI characterMagicAttack;
    public TextMeshProUGUI characterPhysicalDefense;
    public TextMeshProUGUI characterMagicDefense;

    public GameObject skillBar;
    public GameObject infoPanel;
    
    // 핫바 버튼 연결을 위한 배열
    public GameObject[] hotbarButtons = new GameObject[12]; // 12개의 핫바 버튼을 위한 GameObject 배열

    public CharacterTargeting characterTargeting;
    private CharacterManager displayedCharacter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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
            characterManagementPanel.uiManager = this;

        if (partyManagementPanel != null)
            partyManagementPanel.uiManager = this;

        _questPanel = questPanel != null ? questPanel.GetComponent<QuestPanel>() : null;

        if (partyFormationPanel == null)
            partyFormationPanel = GetComponentInChildren<PartyFormationPanel>(true);

        if (questNodeMapPanel == null)
            questNodeMapPanel = GetComponentInChildren<QuestNodeMapPanel>(true);

        if (levelGrowthPanel == null)
            levelGrowthPanel = GetComponentInChildren<LevelGrowthPanel>(true);

        if (inventoryItemActionMenu == null)
            inventoryItemActionMenu = GetComponentInChildren<InventoryItemActionMenuUI>(true);

        WireQuestPanel();
        WireInventoryButtons();
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
    }

    private void WireQuestPanel()
    {
        if (_questPanel == null) return;

        // 퀘스트 카드에서 "수락" 눌렀을 때 → 상층으로 이벤트 전달(파티 편성 화면이 받게)
        _questPanel.OnAcceptRequest = def =>
        {
            CloseManagedWindows();

            if (partyFormationPanel != null)
                partyFormationPanel.Open(def);
            else
                ReturnCameraWhenAllWindowsClosed();

            onQuestAcceptRequest?.Invoke(def);
        };
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (townWindowSession)
                CloseTopWindow();
            else if (CameraFocusRig.Instance && CameraFocusRig.Instance.isFocused)
                CameraFocusRig.Instance.FocusHome();
        }

        if (townWindowSession)
            ReturnCameraWhenAllWindowsClosed();
    }

    public void UISwitch(UIMode mode)
    {
        bool isTown = (mode == UIMode.Town);

        // 루트 토글
        if (townUiRoot) townUiRoot.SetActive(isTown);
        if (TownMenuCanvas) TownMenuCanvas.SetActive(isTown);
        if (battleUiRoot) battleUiRoot.SetActive(!isTown);

        // 타운 진입 시엔 모든 서브 패널 닫고 기본 상태로
        if (isTown)
        {
            CloseManagedWindows();
            townWindowSession = false;
        }

        SetExpeditionButtonState(false, !isTown);
    }

    #region 마을내 UI 버튼 조작

    public void OpenCompanyStorage()
    {
        bool keepCharacterManagement = characterManagementPanel != null &&
                                       characterManagementPanel.gameObject.activeInHierarchy;

        BeginTownWindowSession(
            keepCharacterManagement ? characterManagementFocusKey : storageFocusKey);

        if (keepCharacterManagement)
            inventoryUIController?.CloseAll();
        else
            CloseManagedWindows();

        if (inventoryUIController == null)
            return;

        inventoryUIController.SetSelectedCharacter(null);
        SetWindowPosition(inventoryUIController.companyStorageWindow, Vector2.zero);
        inventoryUIController.OpenCompanyStorage();

        if (keepCharacterManagement && inventoryUIController.companyStorageWindow != null)
            inventoryUIController.companyStorageWindow.transform.SetAsLastSibling();
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

        if (IsInTown())
            BeginTownWindowSession(characterManagementFocusKey);
        inventoryUIController.SetSelectedCharacter(characterManager);
        inventoryUIController.CloseAttachedStorage();
        SetWindowPosition(inventoryUIController.equipmentWindow, Vector2.zero);
        inventoryUIController.OpenEquipment();
    }

    public void OpenCharacterSkills(CharacterManager characterManager)
    {
        if (characterManager == null || inventoryUIController == null)
            return;

        if (IsInTown())
            BeginTownWindowSession(characterManagementFocusKey);
        inventoryUIController.SetSelectedCharacter(characterManager);
        inventoryUIController.OpenSkills();
    }

    public void CloseCharacterManagement()
    {
        if (characterManagementPanel != null)
            characterManagementPanel.gameObject.SetActive(false);

        if (partyManagementPanel != null)
            partyManagementPanel.gameObject.SetActive(false);

        if (IsInTown())
            ReturnCameraWhenAllWindowsClosed();
    }

    public void ReturnToTownCameraWhenIdle()
    {
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

        if (partyManagementPanel != null &&
            partyManagementPanel.gameObject.activeInHierarchy)
        {
            partyManagementPanel.gameObject.SetActive(false);
            return;
        }

        if (recruitPanel != null && recruitPanel.activeInHierarchy)
        {
            recruitPanel.SetActive(false);
            return;
        }

        if (partyFormationPanel != null &&
            partyFormationPanel.gameObject.activeInHierarchy)
        {
            partyFormationPanel.Close();
            return;
        }

        if (questNodeMapPanel != null &&
            questNodeMapPanel.gameObject.activeInHierarchy)
        {
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

        if (partyManagementPanel != null)
            partyManagementPanel.gameObject.SetActive(false);

        if (recruitPanel != null)
            recruitPanel.SetActive(false);

        if (questPanel != null)
            questPanel.SetActive(false);

        if (partyFormationPanel != null)
            partyFormationPanel.Close();

        if (questNodeMapPanel != null)
            questNodeMapPanel.Close();
    }

    public void ReturnToQuestBoardFromPartyFormation()
    {
        if (partyFormationPanel != null)
            partyFormationPanel.Close();

        if (questPanel != null)
            questPanel.SetActive(true);
    }

    public void OpenQuestNodeMap()
    {
        if (questNodeMapPanel != null)
            questNodeMapPanel.Open();
    }

    public void OpenExpeditionPartyManagement()
    {
        inventoryUIController?.CloseAll();

        if (partyManagementPanel != null)
        {
            partyManagementPanel.OpenAndBuildExpeditionParty();
            partyManagementPanel.transform.SetAsLastSibling();
        }
    }

    public void OpenExpeditionInventory()
    {
        if (inventoryUIController == null)
            return;

        CharacterManager selectedCharacter = partyManagementPanel != null
            ? partyManagementPanel.SelectedCharacter
            : null;

        if (selectedCharacter != null)
            inventoryUIController.SetSelectedCharacter(selectedCharacter);

        SetWindowPosition(inventoryUIController.expeditionInventoryWindow, Vector2.zero);
        inventoryUIController.OpenExpeditionInventory();

        if (inventoryUIController.expeditionInventoryWindow != null)
            inventoryUIController.expeditionInventoryWindow.transform.SetAsLastSibling();
    }

    public void OpenCompanyStorageFromPartyFormation()
    {
        if (inventoryUIController == null)
            return;

        SetWindowPosition(inventoryUIController.companyStorageWindow, Vector2.zero);
        inventoryUIController.OpenCompanyStorage();

        if (inventoryUIController.companyStorageWindow != null)
            inventoryUIController.companyStorageWindow.transform.SetAsLastSibling();
    }

    public void SetExpeditionMapButtonVisible(bool visible)
    {
        if (expeditionMapButton != null)
            expeditionMapButton.SetActive(visible);
    }

    public void SetExpeditionButtonState(bool utilityButtonsVisible, bool mapButtonVisible)
    {
        if (expeditionButtonRoot != null)
            expeditionButtonRoot.SetActive(utilityButtonsVisible || mapButtonVisible);

        if (expeditionInventoryButton != null)
            expeditionInventoryButton.SetActive(utilityButtonsVisible);

        if (expeditionPartyManagementButton != null)
            expeditionPartyManagementButton.SetActive(utilityButtonsVisible);

        SetExpeditionMapButtonVisible(mapButtonVisible);
    }

    public void BringExpeditionButtonsToFront()
    {
        if (expeditionButtonRoot != null && expeditionButtonRoot.activeInHierarchy)
            expeditionButtonRoot.transform.SetAsLastSibling();
    }

    public void OpenQuestEncounter()
    {
        if (battleUiRoot != null)
            battleUiRoot.SetActive(false);

        if (questNodeMapPanel != null)
            questNodeMapPanel.OpenEncounter();
    }

    public bool OpenPendingLevelUps(System.Action onCompleted = null)
    {
        if (levelGrowthPanel == null || CharacterPoolManager.Instance == null)
            return false;

        return levelGrowthPanel.Open(
            CharacterPoolManager.Instance.All(),
            onCompleted);
    }

    public bool OpenBattleDefeat(System.Action onContinue)
    {
        if (battleDefeatDialogPrefab == null)
        {
            Debug.LogError("[UIManager] Battle defeat dialog prefab is not assigned.");
            return false;
        }

        if (activeBattleDefeatDialog != null)
            Destroy(activeBattleDefeatDialog);

        activeBattleDefeatDialog = Instantiate(battleDefeatDialogPrefab, transform);
        activeBattleDefeatDialog.name = "BattleDefeatDialog";
        activeBattleDefeatDialog.transform.SetAsLastSibling();

        Animator animator = activeBattleDefeatDialog.GetComponent<Animator>();

        if (animator != null)
            animator.enabled = false;

        if (activeBattleDefeatDialog.transform is RectTransform rootRect)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.localScale = Vector3.one;
        }

        Transform panel = activeBattleDefeatDialog.transform.Find("ConfirmPanel");
        TextMeshProUGUI titleText = panel != null
            ? panel.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>()
            : null;
        TextMeshProUGUI messageText = panel != null
            ? panel.Find("Text (TMP)2")?.GetComponent<TextMeshProUGUI>()
            : null;
        Button continueButton = panel != null
            ? panel.Find("ButtonYes")?.GetComponent<Button>()
            : null;

        if (panel == null || titleText == null || messageText == null || continueButton == null)
        {
            Debug.LogError("[UIManager] Battle defeat dialog hierarchy is invalid.");
            Destroy(activeBattleDefeatDialog);
            activeBattleDefeatDialog = null;
            return false;
        }

        panel.localScale = Vector3.one;
        titleText.text = "패배";
        messageText.text = "원정대가 전멸했습니다.";

        TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonText != null)
            buttonText.text = "계속하기";

        if (turnEndButton != null)
            turnEndButton.gameObject.SetActive(false);
        if (counterTurnEndButton != null)
            counterTurnEndButton.gameObject.SetActive(false);

        characterTargeting?.StopTargeting();
        ClearHotbarButtons();

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() =>
        {
            continueButton.interactable = false;
            Destroy(activeBattleDefeatDialog);
            activeBattleDefeatDialog = null;
            onContinue?.Invoke();
        });

        return true;
    }

    public bool OpenBattleLoot(
        IReadOnlyList<InventorySlotData> loot,
        System.Action onCompleted)
    {
        if (loot == null || !loot.Any(slot => slot != null && slot.itemUid > 0 && slot.count > 0))
            return false;

        if (battleLootPanelPrefab == null)
        {
            Debug.LogError("[UIManager] Battle loot panel prefab is not assigned.");
            return false;
        }

        BattleLootPanel panel = Instantiate(battleLootPanelPrefab, transform);
        panel.name = "BattleLoot";

        if (panel.transform is RectTransform rectTransform)
            rectTransform.localScale = Vector3.one;

        panel.Open(loot, onCompleted);
        return true;
    }

    public bool OpenInventoryItemActionMenu(
        InventorySlotData slot,
        bool directTargetSelection,
        bool allowSell,
        System.Action<CharacterManager> onUseOrEquip,
        System.Action onSell,
        System.Action onDiscard)
    {
        if (slot == null || inventoryItemActionMenu == null)
            return false;

        inventoryItemActionMenu.Open(
            slot,
            GetInventoryActionTargets(),
            directTargetSelection,
            allowSell && IsInTown(),
            onUseOrEquip,
            onSell,
            onDiscard);
        return true;
    }

    public void CloseInventoryItemActionMenu()
    {
        inventoryItemActionMenu?.Close();
    }

    private List<CharacterManager> GetInventoryActionTargets()
    {
        IEnumerable<CharacterManager> candidates = CharacterPoolManager.Instance != null
            ? CharacterPoolManager.Instance.All()
            : GameManager.Instance != null
                ? GameManager.Instance.GetAllCharacters()
                : Enumerable.Empty<CharacterManager>();
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;
        HashSet<string> expeditionIds = !IsInTown() && playerData?.activeCharacterIds != null
            ? new HashSet<string>(playerData.activeCharacterIds)
            : null;

        return candidates
            .Where(manager => manager != null &&
                              manager.character != null &&
                              manager.character.IsMine &&
                              manager.character.IsAlive &&
                              (expeditionIds == null ||
                               expeditionIds.Contains(manager.character.ID)))
            .GroupBy(manager => manager.character.ID)
            .Select(group => group.First())
            .ToList();
    }

    private void WireInventoryButtons()
    {
        Transform partyPanel = partyFormationPanel != null
            ? partyFormationPanel.transform
            : transform.Find("PartyFormationPanel");

        ReplaceButtonAction(
            FindButton(partyPanel, "ExpeditionInventory"),
            OpenExpeditionInventory);
        ReplaceButtonAction(
            FindButton(partyPanel, "Crate"),
            OpenCompanyStorageFromPartyFormation);

        Transform companyStorage = inventoryUIController != null &&
                                   inventoryUIController.companyStorageWindow != null
            ? inventoryUIController.companyStorageWindow.transform
            : transform.Find("CompanyStorageWindow");
        Transform expeditionShortcut = FindDescendant(
            companyStorage,
            "ExpeditionInventoryButtons");

        if (expeditionShortcut == null)
        {
            expeditionShortcut = FindDescendant(
                companyStorage,
                "ExpeditionInventory");
        }

        ReplaceButtonAction(
            FindButton(expeditionShortcut, "ExpandBt"),
            OpenExpeditionInventory);
    }

    private static Button FindButton(Transform root, string objectName)
    {
        Transform target = FindDescendant(root, objectName);

        if (target == null)
            return null;

        return target.GetComponent<Button>() ??
               target.GetComponentInChildren<Button>(true);
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }

    private static void ReplaceButtonAction(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
    }

    private static bool IsInTown()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        return playerData == null || playerData.currentStage == "Town";
    }

    private void BeginTownWindowSession(string focusKey)
    {
        townWindowSession = true;
        CameraFocusRig.Instance?.Focus(focusKey);
    }

    private void ReturnCameraWhenAllWindowsClosed()
    {
        if (!townWindowSession || HasOpenManagedWindow())
            return;

        townWindowSession = false;
        CameraFocusRig.Instance?.FocusHome();
    }

    private bool HasOpenManagedWindow()
    {
        if (characterManagementPanel != null &&
            characterManagementPanel.gameObject.activeInHierarchy)
        {
            return true;
        }

        if (partyManagementPanel != null &&
            partyManagementPanel.gameObject.activeInHierarchy)
        {
            return true;
        }

        if ((recruitPanel != null && recruitPanel.activeInHierarchy) ||
            (questPanel != null && questPanel.activeInHierarchy) ||
            (partyFormationPanel != null &&
             partyFormationPanel.gameObject.activeInHierarchy) ||
            (questNodeMapPanel != null &&
             questNodeMapPanel.gameObject.activeInHierarchy))
        {
            return true;
        }

        return inventoryUIController != null &&
               (IsOpen(inventoryUIController.companyStorageWindow) ||
                IsOpen(inventoryUIController.expeditionInventoryWindow) ||
                IsOpen(inventoryUIController.equipmentWindow) ||
                IsOpen(inventoryUIController.skillWindow));
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

    #endregion





    // 데미지 팝업 생성
    public void ShowDamage(int damageAmount, Vector3 worldPosition)
    {
        // 월드 좌표를 스크린 좌표로 변환
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition + Vector3.up * 2);  // 캐릭터 위쪽에 표시되도록 위치 조정

        // 데미지 텍스트 인스턴스 생성 및 캔버스의 자식으로 추가
        GameObject damageTextInstance = Instantiate(damageTextPrefab, this.transform);
        damageTextInstance.transform.position = screenPosition;

        TextMeshProUGUI damageText = damageTextInstance.GetComponent<TextMeshProUGUI>();

        // 데미지 텍스트 설정 (-n 형식, 빨간색)
        damageText.text = $"-{damageAmount}";
        damageText.color = Color.red;

        // 텍스트를 일정 시간 동안 표시 후 사라지게 하는 코루틴 호출
        StartCoroutine(PopDamage(damageTextInstance));
    }

    // 데미지 텍스트 표시 효과 (팝업 후 서서히 사라짐)
    private IEnumerator PopDamage(GameObject damageTextInstance)
    {
        TextMeshProUGUI damageText = damageTextInstance.GetComponent<TextMeshProUGUI>();

        // 텍스트 팝업 효과
        float t = 0f;
        Vector3 originalScale = damageText.transform.localScale;

        while (t < 1f)
        {
            t += Time.deltaTime * 5f;  // 빠르게 팝업하는 효과
            damageText.transform.localScale = originalScale * (1f + t * 0.2f); // 스케일 증가
            damageText.transform.position += Vector3.up * Time.deltaTime * 20; // 약간 위로 이동
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // 텍스트가 서서히 사라지며 축소되는 효과
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * 3f;  // 서서히 사라지는 속도
            damageText.color = new Color(damageText.color.r, damageText.color.g, damageText.color.b, t); // 알파 값 조정
            damageText.transform.localScale = originalScale * (1f + t * 0.2f);
            yield return null;
        }

        // 텍스트 오브젝트 삭제
        Destroy(damageTextInstance);
    }


    // 남은 턴 시간을 업데이트하는 메서드
    public void UpdateTurnTimer(float timeRemaining)
    {
        if (turnTimerText != null)
        {
            turnTimerText.text = Mathf.CeilToInt(timeRemaining).ToString();
        }
    }

    public void UpdateSkillTransparency(CharacterManager characterManager)
    {
        if (characterManager == null || characterManager.character == null)
            return;

        foreach (GameObject button in hotbarButtons)
        {
            if (button == null)
                continue;

            SkillButton skillButton = button.GetComponent<SkillButton>();
            if (skillButton == null || skillButton.skill == null)
                continue;

            SkillDefinitionSO linkedSkill = skillButton.skill;

            bool canUseSkill =
                characterManager.character.CurrentStamina >= linkedSkill.staminaCost &&
                characterManager.character.CurrentMentality >= linkedSkill.mentalCost;

            bool inactive = !characterManager.isPlayerTurn || !canUseSkill;

            if (characterManager == TurnManager.Instance.defenseCharacter)
            {
                inactive = !canUseSkill;
            }

            CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = button.AddComponent<CanvasGroup>();

            canvasGroup.alpha = inactive ? 0.3f : 1.0f;

            Button uiButton = button.GetComponent<Button>();
            if (uiButton != null)
                uiButton.interactable = !inactive;
        }
    }


    // 턴 큐 이미지 업데이트 메서드
    public void UpdateTurnOrder(List<CharacterManager> turnQueue, CharacterManager currentCharacter)
    {
        // 기존 턴 큐 UI 초기화
        foreach (Transform child in turnOrderPanel.transform)
        {
            Destroy(child.gameObject);  // 기존 초상화 제거
        }

        // 턴 큐에 있는 모든 캐릭터의 초상화 추가
        foreach (var characterManager in turnQueue)
        {
            GameObject portraitObj = Instantiate(characterPortraitPrefab, turnOrderPanel.transform);
            RawImage portraitImage = portraitObj.GetComponent<RawImage>();
            portraitImage.texture = characterManager.character.Portrait;

            // 현재 턴인 캐릭터 강조 표시
            if (characterManager == currentCharacter)
            {
                portraitObj.transform.localScale = Vector3.one * 1.15f; // 크기 1.1배 증가
            }
            else
            {
                portraitObj.transform.localScale = Vector3.one; // 기본 크기
            }
        }
    }

    public void DisplayCharacterInfo(CharacterManager characterManager)
    {
        displayedCharacter = characterManager;
        infoPanel.SetActive(true);
        RefreshDisplayedCharacterInfo();

        skillBar.SetActive(characterManager.character.IsMine); // 캐릭터가 자신의 것일 경우 스킬바 활성화

        // 핫바 스킬 업데이트
        UpdateHotbarSkills(characterManager);
    }

    public void RefreshCharacterInfo(CharacterManager characterManager)
    {
        if (characterManager == null || characterManager != displayedCharacter)
            return;

        RefreshDisplayedCharacterInfo();
        UpdateSkillTransparency(characterManager);
    }

    private void RefreshDisplayedCharacterInfo()
    {
        if (displayedCharacter == null || displayedCharacter.character == null)
            return;

        CharacterData characterData = displayedCharacter.character;

        characterPortrait.texture = characterData.Portrait;
        characterName.text = characterData.Name;

        currentHP.text = $"{characterData.CurrentHp}";
        characterHP.text = $"{characterData.FinalStats.MaxHp}";

        if (characterHpImage != null)
        {
            characterHpImage.fillAmount = characterData.FinalStats.MaxHp > 0
                ? Mathf.Clamp01((float)characterData.CurrentHp / characterData.FinalStats.MaxHp)
                : 0f;
        }

        currentStamina.text = $"{characterData.CurrentStamina}";
        characterStamina.text = $"{characterData.FinalStats.MaxStamina}";

        currentMental.text = $"{characterData.CurrentMentality}";
        characterMental.text = $"{characterData.FinalStats.MaxMentality}";

        characterPhysicalAttack.text = $"{characterData.FinalStats.PhysicalAttack}";
        characterMagicAttack.text = $"{characterData.FinalStats.MagicalAttack}";
        characterPhysicalDefense.text = $"{characterData.FinalStats.PhysicalDefense}";
        characterMagicDefense.text = $"{characterData.FinalStats.MagicalDefense}";
    }

    // 핫바 스킬 업데이트 메서드
    public void UpdateHotbarSkills(CharacterManager characterManager)
    {
        if (characterManager == null || characterManager.character == null)
            return;

        ClearHotbarButtons();

        int hotbarIndex = 0;

        foreach (SkillRuntimeData runtime in characterManager.character.Skills)
        {
            if (runtime == null)
                continue;

            SkillDefinitionSO skill = GameDataRegistry.Instance.GetSkill(runtime.skillUid);

            if (skill == null)
                continue;

            if (!runtime.quickSlot || !runtime.canUse)
                continue;

            bool showAsCounter = characterManager.combatHandler.isDefenseCharacter;

            if (showAsCounter)
            {
                if (!skill.isCounterSkill)
                    continue;
            }
            else
            {
                if (skill.isCounterSkill)
                    continue;
            }

            if (hotbarIndex >= hotbarButtons.Length)
                break;

            BindHotbarButton(hotbarButtons[hotbarIndex], characterManager, skill, showAsCounter);

            hotbarIndex++;
        }

        UpdateSkillTransparency(characterManager);
    }

    private void ClearHotbarButtons()
    {
        foreach (var buttonObj in hotbarButtons)
        {
            if (buttonObj == null)
                continue;

            SkillButton skillButton = buttonObj.GetComponent<SkillButton>();
            if (skillButton != null)
            {
                skillButton.skill = null;
                skillButton.queueData = null;
                skillButton.queueIndex = -1;
                skillButton.isCounterSkill = false;
            }

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = false;
            }

            RawImage rawImage = buttonObj.GetComponent<RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = null;
                rawImage.enabled = false;
            }

            TextMeshProUGUI costText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (costText != null)
                costText.text = "";

            Image[] images = buttonObj.GetComponentsInChildren<Image>();
            if (images != null && images.Length > 1)
            {
                Image costFrameImage = images[1];
                if (costFrameImage != null)
                    costFrameImage.color = new Color(1f, 1f, 1f, 1f);
            }

            CanvasGroup canvasGroup = buttonObj.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
    }

    private void BindHotbarButton(
    GameObject buttonObj,
    CharacterManager characterManager,
    SkillDefinitionSO skill,
    bool isCounterSkill)
    {
        if (buttonObj == null || characterManager == null || skill == null)
            return;

        RawImage rawImage = buttonObj.GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.texture = skill.icon;
            rawImage.enabled = true;
        }

        SkillButton skillButton = buttonObj.GetComponent<SkillButton>();
        if (skillButton != null)
        {
            skillButton.skill = skill;
            skillButton.queueData = null;
            skillButton.queueIndex = -1;
            skillButton.isCounterSkill = isCounterSkill;
        }

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            button.interactable = true;
            button.onClick.AddListener(() =>
            {
                characterTargeting.StartTargeting(skill);
            });
        }

        ApplySkillCostUI(buttonObj, skill);
    }

    private void ApplySkillCostUI(GameObject buttonObj, SkillDefinitionSO skill)
    {
        if (buttonObj == null || skill == null)
            return;

        TextMeshProUGUI costText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        Image[] images = buttonObj.GetComponentsInChildren<Image>();

        if (costText == null)
            return;

        Image costFrameImage = null;

        if (images != null && images.Length > 1)
            costFrameImage = images[1];

        if (skill.type == SkillType.Physical)
        {
            costText.text = $"{skill.staminaCost}";
            costText.color = new Color(1f, 0.5f, 0f);

            if (costFrameImage != null)
                costFrameImage.color = new Color(1f, 0.7f, 0.4f, 1f);
        }
        else if (skill.type == SkillType.Magical)
        {
            costText.text = $"{skill.mentalCost}";
            costText.color = new Color(0f, 0.5f, 1f);

            if (costFrameImage != null)
                costFrameImage.color = new Color(0.4f, 0.6f, 1f, 1f);
        }
    }

    #region 카운터 스킬 패널 조작

    // 방어 대상과 방어자를 기반으로 패널을 초기화 - 방어대상 선택 시 호출
    public void UpdateCounterSkillPanel(CharacterManager defender, CharacterManager target)
    {
        ClearCounterSkillPanel();

        CharacterManager attacker = TurnManager.Instance.currentCharacter;

        if (attacker == null || defender == null || target == null)
            return;

        attacker.combatHandler.ResolveConcealForSkillQueue();

        List<SkillQueueData> atkSkillQueue = attacker.combatHandler.GetSkillQueue();

        List<SkillQueueData> filteredSkillQueue = atkSkillQueue
            .Where(item => item != null && (item.target == defender || item.target == target))
            .ToList();

        if (defender == target)
        {
            AddSkillFrame(defender, filteredSkillQueue);
        }
        else
        {
            bool hasDefenderAttack = filteredSkillQueue.Any(item => item.target == defender);
            bool hasTargetAttack = filteredSkillQueue.Any(item => item.target == target);

            if (hasDefenderAttack)
                AddSkillFrame(defender, filteredSkillQueue);

            if (hasTargetAttack)
                AddSkillFrame(target, filteredSkillQueue);
        }
    }
    // 패널 초기화 (기존 자식 오브젝트 삭제)
    public void ClearCounterSkillPanel()
    {
        foreach (Transform child in counterSkillPanel)
        {
            Destroy(child.gameObject);
        }
    }

    // 스킬 큐 프레임 추가
    private void AddSkillFrame(CharacterManager owner, List<SkillQueueData> skillQueue)
    {
        GameObject frame = Instantiate(skillQueueFramePrefab, counterSkillPanel);

        TextMeshProUGUI titleText = frame.GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = "=> " + owner.character.Name;
        }

        Transform skillPanel = frame.transform.Find("SkillPanel");
        if (skillPanel == null)
        {
            Debug.LogWarning("SkillPanel을 찾을 수 없습니다. Prefab 구조 확인 필요.");
            return;
        }

        int orderNumber = 1;

        for (int i = 0; i < skillQueue.Count; i++)
        {
            if (skillQueue[i].target == owner)
            {
                AddSkillIcon(skillPanel, skillQueue[i], owner, orderNumber);
                orderNumber++;
            }
        }
    }

    // 스킬 아이콘 추가 (공격스킬)
    private void AddSkillIcon(
    Transform parent,
    SkillQueueData attackQueueData,
    CharacterManager owner,
    int orderNumber)
    {
        if (parent == null || attackQueueData == null || attackQueueData.skill == null)
            return;

        CharacterManager defenseCharacter = TurnManager.Instance.defenseCharacter;

        if (defenseCharacter == null)
            return;

        GameObject skillQueueIcon = Instantiate(skillIconPrefab, parent);

        GameObject atkSkillIcon = skillQueueIcon.transform.GetChild(0).gameObject;
        GameObject defSkillIcon = skillQueueIcon.transform.GetChild(2).gameObject;

        ApplyAttackSkillIcon(atkSkillIcon, attackQueueData);

        int idx0 = orderNumber - 1;

        List<SkillQueueData> counterQueue = defenseCharacter.combatHandler.GetCounterSkillQueue();

        SkillDefinitionSO counterToShow = GetDefaultCounterSkill(defenseCharacter);

        if (idx0 >= 0 && idx0 < counterQueue.Count && counterQueue[idx0].skill != null)
        {
            counterToShow = counterQueue[idx0].skill;
        }

        ApplyCounterSkillIcon(defSkillIcon, counterToShow, idx0);

        TextMeshProUGUI orderText = skillQueueIcon.GetComponentInChildren<TextMeshProUGUI>();
        if (orderText != null)
        {
            orderText.text = orderNumber > 0 ? orderNumber.ToString() : "";
        }

        Button button = defSkillIcon.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                CharacterTargeting tgt = UIManager.Instance.characterTargeting;
                int idx0Local = orderNumber - 1;

                if (tgt.isDefenseSkillTargeting)
                {
                    defenseCharacter.combatHandler.SetOrResetCounterSkill(idx0Local, tgt.selectedSkill);
                    tgt.StopTargeting();
                }
                else
                {
                    defenseCharacter.combatHandler.SetOrResetCounterSkill(idx0Local, null);
                }
            });
        }
    }

    private void ApplyAttackSkillIcon(GameObject iconObj, SkillQueueData data)
    {
        if (iconObj == null || data == null || data.skill == null)
            return;

        SkillButton skillButton = iconObj.GetComponent<SkillButton>();
        if (skillButton != null)
        {
            skillButton.skill = data.skill;
            skillButton.queueData = data;
            skillButton.queueIndex = data.order - 1;
            skillButton.isCounterSkill = false;
        }

        RawImage rawImage = iconObj.GetComponent<RawImage>();
        if (rawImage != null)
        {
            if (data.isConcealed && data.revealLevel != RevealLevel.Full)
                rawImage.texture = null;
            else
                rawImage.texture = data.skill.icon;
        }
    }

    private void ApplyCounterSkillIcon(GameObject iconObj, SkillDefinitionSO skill, int queueIndex)
    {
        if (iconObj == null)
            return;

        SkillButton skillButton = iconObj.GetComponent<SkillButton>();
        if (skillButton != null)
        {
            skillButton.skill = skill;
            skillButton.queueData = null;
            skillButton.queueIndex = queueIndex;
            skillButton.isCounterSkill = true;
        }

        RawImage rawImage = iconObj.GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.texture = skill != null ? skill.icon : null;
        }
    }
    private SkillDefinitionSO GetDefaultCounterSkill(CharacterManager characterManager)
    {
        if (characterManager == null || characterManager.character == null)
            return null;

        if (characterManager.character.DefaultCounterSkill <= 0)
            return null;

        return GameDataRegistry.Instance.GetSkill(characterManager.character.DefaultCounterSkill);
    }


    #endregion 


    // 시너지 정보를 UI에 표시하는 메서드 - 구현 보류
    public void UpdateSynergyUI()//List<SynergyEffect> activeSynergies, List<SynergyRule> allSynergies)
    {
        // 활성화된 시너지를 UI에 표시
        foreach (Transform child in synergyInfoPanel.transform)
        {
            Destroy(child.gameObject);  // 기존 UI 아이템 삭제
        }
        /*
        foreach (var synergy in allSynergies)
        {
            //string status = activeSynergies.Contains(synergy.Name) ? " (활성화)" : " (비활성)";
            GameObject newSynergyText = new GameObject(synergy.Name + status);
            newSynergyText.transform.SetParent(synergyInfoPanel.transform);
            newSynergyText.AddComponent<Text>().text = synergy.Name + status; // 텍스트 표시
        }*/
    }
}
