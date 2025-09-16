using SoftKitty.InventoryEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;

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

    CharacterManagementPanel _cmp;

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

        _cmp = CharacterManagePanel.GetComponent<CharacterManagementPanel>();
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
        _cmp.OpenAndBuild();
    }

    public void OnClick_Recruit()
    {
        CloseAllTownOverlays();        
        RecruitPanel?.SetActive(true);
    }

    public void OnClick_QuestBoard()
    {
        CameraFocusRig.Instance?.Focus("Quest");
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
        foreach (GameObject button in hotbarButtons)
        {
            // 버튼의 RawImage 컴포넌트와 연결된 스킬의 아이콘을 비교하여 해당 스킬 찾기
            RawImage buttonImage = button.GetComponent<RawImage>();
            if (buttonImage != null && buttonImage.enabled)
            {
                SkillBase linkedSkill = characterManager.character.Skills.FirstOrDefault(skill => skill.icon == buttonImage.texture);
                if (linkedSkill != null)
                {
                    // 리소스가 충분한지 체크
                    bool canUseSkill = characterManager.character.FinalStats.CurrentStamina >= linkedSkill.StaminaCost &&
                                       characterManager.character.FinalStats.CurrentMentality >= linkedSkill.MentalCost;

                    // 현재 턴이 아니거나 리소스가 부족할 경우 스킬 사용 불가로 표시
                    bool Inactive = !characterManager.isPlayerTurn || !canUseSkill;
                    if(characterManager == TurnManager.Instance.defenseCharacter) // 방어캐릭터일 경우 턴 관련 부분 스킵 후 리소스 소모량만 계산
                    {
                        Inactive = !canUseSkill;
                    }

                    // 버튼의 CanvasGroup을 통해 투명도 설정
                    CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = button.AddComponent<CanvasGroup>(); // CanvasGroup이 없을 경우 추가
                    }

                    canvasGroup.alpha = Inactive ? 0.3f : 1.0f;  // 리소스가 부족할 경우 투명도를 낮춤
                }
            }
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

        currentHP.text = $"{characterData.FinalStats.CurrentHp}";
        characterHP.text = $"{characterData.FinalStats.MaxHp}";

        currentStamina.text = $"{characterData.FinalStats.CurrentStamina}";
        characterStamina.text = $"{characterData.FinalStats.MaxStamina}";

        currentMental.text = $"{characterData.FinalStats.CurrentMentality}";
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
        // 모든 핫바 버튼과 RawImage를 비활성화
        foreach (var button in hotbarButtons)
        {
            button.GetComponent<SkillButton>().skill = null;
            button.GetComponent<Button>().interactable = false; // 버튼 비활성화
            button.GetComponent<RawImage>().enabled = false;    // 이미지 비활성화            

            // 코스트 텍스트 초기화
            TextMeshProUGUI costText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (costText != null)
            {
                costText.text = ""; // 리소스 소모량 텍스트 초기화
            }

            // 코스트 프레임 - Bind 이미지 초기화
            Image[] images = button.GetComponentsInChildren<Image>();

            Image costFrameImage = images[1];

            if (costFrameImage != null)
            {
                costFrameImage.color = new Color(1f, 1f, 1f, 1f); // 기본 색상 (흰색, 투명도 1)
            }
        }

        // 사용 가능한 스킬이 있는 경우 핫바에 표시
        int hotbarIndex = 0;

        foreach (SkillBase skill in characterManager.character.Skills)
        {
            if (characterManager.combatHandler.isDefenseCharacter) // 선택된 캐릭터가 방어 캐릭터일 경우
            {
                if (skill.QuickSlot && skill.CanUse && skill.IsCounterSkill)
                {
                    if (hotbarIndex < hotbarButtons.Length) // 핫바 슬롯이 남아있는 경우
                    {
                        GameObject button = hotbarButtons[hotbarIndex];

                        button.GetComponent<RawImage>().texture = skill.icon; // Texture2D로 아이콘 설정
                        button.GetComponent<RawImage>().enabled = true;       // 아이콘 표시
                        button.GetComponent<SkillButton>().skill = skill;
                        if (characterManager.combatHandler.isDefenseCharacter)
                        {
                            button.GetComponent<Button>().interactable = true;    // 버튼 활성화

                            // 버튼 클릭 시 스킬 사용 처리
                            button.GetComponent<Button>().onClick.RemoveAllListeners(); // 기존 리스너 제거
                            button.GetComponent<Button>().onClick.AddListener(() =>
                            {
                                characterTargeting.StartTargeting(skill); // 스킬 타겟팅 시작
                            });
                        }

                        // 리소스 소모량 표시
                        TextMeshProUGUI costText = button.GetComponentInChildren<TextMeshProUGUI>();
                        Image[] images = button.GetComponentsInChildren<Image>();
                        if (costText != null)
                        {
                            Image costFrameImage = images[1];
                            if (skill.Type == SkillType.Physical)
                            {
                                costText.text = $"{skill.StaminaCost}";
                                costText.color = new Color(1f, 0.5f, 0f); // 주황색 (지구력)
                                costFrameImage.color = new Color(1f, 0.7f, 0.4f, 1f); // 부모 이미지 색상 변경 (주황 계열)
                            }
                            else if (skill.Type == SkillType.Magical)
                            {
                                costText.text = $"{skill.MentalCost}";
                                costText.color = new Color(0f, 0.5f, 1f); // 파란색 (정신력)
                                costFrameImage.color = new Color(0.4f, 0.6f, 1f, 1f); // 부모 이미지 색상 변경 (파란 계열)
                            }
                        }

                        hotbarIndex++; // 다음 핫바 슬롯으로 이동
                    }
                }
            }
            else if (skill.QuickSlot && skill.CanUse && !skill.IsCounterSkill) // 사용 가능한 공격 스킬 + 선택 대상이 방어캐릭터가 아닐 경우
            {
                if (hotbarIndex < hotbarButtons.Length) // 핫바 슬롯이 남아있는 경우
                {
                    GameObject button = hotbarButtons[hotbarIndex];

                    button.GetComponent<RawImage>().texture = skill.icon; // Texture2D로 아이콘 설정
                    button.GetComponent<RawImage>().enabled = true;       // 아이콘 표시
                    button.GetComponent<SkillButton>().skill = skill;

                    if (characterManager.isPlayerTurn)
                    {
                        button.GetComponent<Button>().interactable = true;    // 버튼 활성화

                        // 버튼 클릭 시 스킬 사용 처리
                        button.GetComponent<Button>().onClick.RemoveAllListeners(); // 기존 리스너 제거
                        button.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            characterTargeting.StartTargeting(skill); // 스킬 타겟팅 시작
                        });
                    }

                    // 리소스 소모량 표시
                    TextMeshProUGUI costText = button.GetComponentInChildren<TextMeshProUGUI>();
                    Image[] images = button.GetComponentsInChildren<Image>();
                    if (costText != null)
                    {
                        Image costFrameImage = images[1];
                        if (skill.Type == SkillType.Physical)
                        {
                            costText.text = $"{skill.StaminaCost}";
                            costText.color = new Color(1f, 0.5f, 0f); // 주황색 (지구력)
                            costFrameImage.color = new Color(1f, 0.7f, 0.4f, 1f); // 부모 이미지 색상 변경 (주황 계열)
                        }
                        else if (skill.Type == SkillType.Magical)
                        {
                            costText.text = $"{skill.MentalCost}";
                            costText.color = new Color(0f, 0.5f, 1f); // 파란색 (정신력)
                            costFrameImage.color = new Color(0.4f, 0.6f, 1f, 1f); // 부모 이미지 색상 변경 (파란 계열)
                        }
                    }

                    hotbarIndex++; // 다음 핫바 슬롯으로 이동
                }
            }
            
        }

        UpdateSkillTransparency(characterManager);
    }

    #region 카운터 스킬 패널 조작

    // 방어 대상과 방어자를 기반으로 패널을 초기화 - 방어대상 선택 시 호출
    public void UpdateCounterSkillPanel(CharacterManager defender, CharacterManager target)
    {
        // 기존 자식들 초기화
        ClearCounterSkillPanel();

        // 현재 공격자의 스킬 큐를 가져옴
        CharacterManager attacker = TurnManager.Instance.currentCharacter;
        List<(SkillBase skill, CharacterManager target)> atkSkillQueue = attacker.combatHandler.GetSkillQueue();
        
        // 방어자와 방어대상을 타겟으로 하는 스킬들만 추출하여 새로운 큐 구성
        List<(SkillBase skill, CharacterManager target)> filteredSkillQueue = atkSkillQueue.Where(item => item.target == defender || item.target == target).ToList(); // 리스트로 변환하여 순서를 유지
        
        // 자기 자신을 방어하는 경우
        if (defender == target)
        {
            // 하나의 프레임으로 표시
            AddSkillFrame(defender, filteredSkillQueue);
        }
        else
        {   
            // **각 캐릭터를 향한 공격이 있는지 개별적으로 체크
            bool hasDefenderAttack = filteredSkillQueue.Any(item => item.target == defender);
            bool hasTargetAttack = filteredSkillQueue.Any(item => item.target == target);

            // 방어자에게 향하는 공격이 있다면 패널 생성
            if (hasDefenderAttack)
            {
                AddSkillFrame(defender, filteredSkillQueue);
            }

            // 방어 대상에게 향하는 공격이 있다면 패널 생성
            if (hasTargetAttack)
            {
                AddSkillFrame(target, filteredSkillQueue);
            }
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
    private void AddSkillFrame(CharacterManager owner, List<(SkillBase skill, CharacterManager target)> skillQueue)
    {
        // 프레임 생성
        GameObject frame = Instantiate(skillQueueFramePrefab, counterSkillPanel);

        // 프레임의 제목 설정
        TextMeshProUGUI titleText = frame.GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = "=> " + owner.character.Name;
        }

        // 실제 스킬 아이콘을 배치할 Panel 객체 찾기
        Transform skillPanel = frame.transform.Find("SkillPanel");
        if (skillPanel == null)
        {
            Debug.LogWarning($"SkillPanel을 찾을 수 없습니다. Prefab 구조 확인 필요.");
            return;
        }

        // 순번을 재매기면서 스킬 아이콘 추가
        for (int i = 0; i < skillQueue.Count; i++)
        {
            // 현재 공격자의 스킬 큐에서 owner를 타겟으로 하는 것만 필터링
            if (skillQueue[i].target == owner)
                AddSkillIcon(skillPanel, skillQueue[i].skill, owner, i + 1); // 순번 부여하여 추가
        }
    }

    // 스킬 아이콘 추가 (공격스킬)
    private void AddSkillIcon(Transform parent, SkillBase skill, CharacterManager owner, int orderNumber)
    {
        CharacterManager defenseCharacter = TurnManager.Instance.defenseCharacter;

        GameObject skillQueueIcon = Instantiate(skillIconPrefab, parent);
        GameObject atkSkillIcon = skillQueueIcon.transform.GetChild(0).gameObject;
        GameObject defSkillIcon = skillQueueIcon.transform.GetChild(2).gameObject;

        // 공격 스킬 아이콘
        atkSkillIcon.GetComponent<SkillButton>().skill = skill;
        var atkIconImage = atkSkillIcon.GetComponent<RawImage>();
        if (atkIconImage && skill.icon) atkIconImage.texture = skill.icon;

        // ▼▼▼ 여기부터 "해당 슬롯에 현재 등록된 대응 스킬"을 조회하여 사용 ▼▼▼
        int idx0 = orderNumber - 1;
        var counterQueue = defenseCharacter.combatHandler.GetCounterSkillQueue();
        SkillBase counterToShow = defenseCharacter.character.DefaultCounterSkill;

        if (idx0 >= 0 && idx0 < counterQueue.Count && counterQueue[idx0].skill != null)
        {
            counterToShow = counterQueue[idx0].skill;
        }

        defSkillIcon.GetComponent<SkillButton>().skill = counterToShow;
        var defIconImage = defSkillIcon.GetComponent<RawImage>();
        if (defIconImage && counterToShow.icon) defIconImage.texture = counterToShow.icon;
        // ▲▲▲ 현재 등록된 대응 스킬 반영 끝

        // 순번 표시
        var orderText = skillQueueIcon.GetComponentInChildren<TextMeshProUGUI>();
        if (orderText) orderText.text = orderNumber > 0 ? orderNumber.ToString() : "";

        // 버튼 리스너
        var button = defSkillIcon.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                CharacterTargeting tgt = UIManager.Instance.characterTargeting;
                int idx0Local = orderNumber - 1;

                if (tgt.isDefenseSkillTargeting)
                {
                    // 새 대응 스킬로 교체
                    defenseCharacter.combatHandler.SetOrResetCounterSkill(idx0Local, tgt.selectedSkill);
                    tgt.StopTargeting();
                }
                else
                {
                    // 기본 대응 스킬로 리셋
                    defenseCharacter.combatHandler.SetOrResetCounterSkill(idx0Local, null);
                }
            });
        }
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
