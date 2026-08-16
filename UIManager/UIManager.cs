using SoftKitty.InventoryEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Roots")]
    public GameObject battleUiRoot; // 전투 UI 루트
    public GameObject townUiRoot;   // 마을 UI 루트
    public GameObject TownMenuCanvas; // 마을 UI - 월드 스페이스 메뉴 캔버스

    [Header("Town Panel - Menu")]
    public GameObject CharacterManagePanel; // TownUI 하위 메뉴 패널 (캐릭터 관리)
    public GameObject RecruitPanel;         // TownUI 하위 메뉴 패널 (고용)
    public GameObject QuestBoardPanel;      // TownUI 하위 메뉴 패널 (퀘스트 게시판)

    // 외부(파티편성/게임매니저)로 이벤트 넘겨줄 훅
    public System.Action<QuestDef> onQuestAcceptRequest;

    CharacterManagementPanel _characterManagePanel;
    QuestPanel _questPanel;

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

    public InventoryHolder storage_temp;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _characterManagePanel = CharacterManagePanel.GetComponent<CharacterManagementPanel>();

        _questPanel = QuestBoardPanel.GetComponent<QuestPanel>();

        WireQuestPanel();
    }

    private void WireQuestPanel()
    {
        if (_questPanel == null) return;

        // 퀘스트 카드에서 "수락" 눌렀을 때 → 상층으로 이벤트 전달(파티 편성 화면이 받게)
        _questPanel.OnAcceptRequest = def =>
        {
            CloseAllTownOverlays();
            // 여기서 바로 Accept까지 태우지 말고, 파티 편성으로 위임
            onQuestAcceptRequest?.Invoke(def);
        };
    }

    public void Update()
    {
        // ESC: 타운 패널 > 닫기, 그 외엔 포커스 홈
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (townUiRoot && townUiRoot.activeInHierarchy &&
                ((RecruitPanel && RecruitPanel.activeSelf) || (CharacterManagePanel && CharacterManagePanel.activeSelf)))
            {
                CloseAllTownOverlays();
                CameraFocusRig.Instance?.FocusHome();
            }
            else if (CameraFocusRig.Instance && CameraFocusRig.Instance.isFocused)
            {
                CameraFocusRig.Instance.FocusHome();
            }
        }
    }

    public void UISwitch(UIMode mode)
    {
        bool isTown = (mode == UIMode.Town);

        // 루트 토글
        if (townUiRoot) townUiRoot.SetActive(isTown);
        if (TownMenuCanvas) TownMenuCanvas.SetActive(isTown);
        if (battleUiRoot) battleUiRoot.SetActive(!isTown);

        // 타운 진입 시엔 모든 서브 패널 닫고 기본 상태로
        if (isTown) CloseAllTownOverlays();
    }

    #region 마을내 UI 버튼 조작

    public void OnClick_Storage()
    {
        CameraFocusRig.Instance?.Focus("Storage");

        CloseAllTownOverlays();

        storage_temp.OpenWindow(); // 임시 창고 열기 - PlayerData의 InventoryHolder 필드와 연동 필요 -> DB 백업용

        /*
        PlayerData currentPlayer = PlayerManager.Instance.GetCurrentPlayerData();
        
        currentPlayer.storage.OpenWindow();  // 창고 열기
        */
    }

    public void Open_Storage()
    {
        storage_temp.OpenWindow();
    }

    public void OnClick_CharacterManage()
    {
        CloseAllTownOverlays();

        CharacterManagePanel?.SetActive(true);
        _characterManagePanel.OpenAndBuild();
    }

    public void OnClick_Recruit()
    {
        CloseAllTownOverlays();        
        RecruitPanel?.SetActive(true);
    }

    public void OnClick_QuestBoard()
    {
        CameraFocusRig.Instance?.Focus("Quest"); // 카메라 포커스 이동 - 퀘스트 패널은 월드 스페이스 캔버스에 있기 때문에 따로 패널 활성화가 필요하지 않음.
    }
    public void CloseAllTownOverlays()
    {        
        CharacterManagePanel?.SetActive(false);
        RecruitPanel?.SetActive(false);
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
        infoPanel.SetActive(true);
        CharacterData characterData = characterManager.character;

        characterPortrait.texture = characterData.Portrait;
        characterName.text = characterData.Name;

        currentHP.text = $"{characterData.CurrentHp}";
        characterHP.text = $"{characterData.FinalStats.MaxHp}";

        currentStamina.text = $"{characterData.CurrentStamina}";
        characterStamina.text = $"{characterData.FinalStats.MaxStamina}";

        currentMental.text = $"{characterData.CurrentMentality}";
        characterMental.text = $"{characterData.FinalStats.MaxMentality}";

        characterPhysicalAttack.text = $"{characterData.FinalStats.PhysicalAttack}";
        characterMagicAttack.text = $"{characterData.FinalStats.MagicalAttack}";
        characterPhysicalDefense.text = $"{characterData.FinalStats.PhysicalDefense}";
        characterMagicDefense.text = $"{characterData.FinalStats.MagicalDefense}";

        skillBar.SetActive(characterData.IsMine); // 캐릭터가 자신의 것일 경우 스킬바 활성화

        // 핫바 스킬 업데이트
        UpdateHotbarSkills(characterManager);
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

    // 시너지 정보를 UI에 표시하는 메서드
    public void UpdateSynergyUI(List<SynergyEffect> activeSynergies, List<SynergyRule> allSynergies)
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
