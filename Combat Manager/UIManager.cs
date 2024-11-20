using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public GameObject turnOrderPanel;  // 상단 턴 큐 패널
    public GameObject characterPortraitPrefab;  // 캐릭터 초상화 프리팹

    public TextMeshProUGUI turnTimerText; // 남은 턴 시간을 표시하는 텍스트

    public GameObject damageTextPrefab;  // 데미지 텍스트 프리팹

    public Button turnEndButton; // 턴 종료 버튼 추가
    public Button counterTurnEndButton; // 자동 대응 버튼 추가

    public GameObject synergyInfoPanel; // 시너지 정보가 표시되는 패널
        
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
    }

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
        Debug.Log(characterManager.character.Name + " " + characterManager.isPlayerTurn);
        // 모든 핫바 버튼과 RawImage를 비활성화
        foreach (var button in hotbarButtons)
        {
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
            if (skill.QuickSlot && skill.CanUse && !skill.IsCounterSkill) // 사용 가능하고 대응 스킬이 아닌 경우
            {
                if (hotbarIndex < hotbarButtons.Length) // 핫바 슬롯이 남아있는 경우
                {
                    GameObject button = hotbarButtons[hotbarIndex];

                    button.GetComponent<RawImage>().texture = skill.icon; // Texture2D로 아이콘 설정
                    button.GetComponent<RawImage>().enabled = true;       // 아이콘 표시
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
            else if(skill.QuickSlot && skill.CanUse && skill.IsCounterSkill && characterManager.combatHandler.isDefenseCharacter)
            {
                if (hotbarIndex < hotbarButtons.Length) // 핫바 슬롯이 남아있는 경우
                {
                    GameObject button = hotbarButtons[hotbarIndex];

                    button.GetComponent<RawImage>().texture = skill.icon; // Texture2D로 아이콘 설정
                    button.GetComponent<RawImage>().enabled = true;       // 아이콘 표시
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

    public void ShowQueuedSkills(CharacterManager currentCharacter)
    {
        // 기존 UI를 업데이트하고, 큐에 있는 스킬 목록을 시각적으로 표시
        DisplayQueuedSkills(currentCharacter.GetSkillQueue());

        // 상대방에게도 큐에 쌓인 스킬 목록을 보여줌
        NotifyOpponentOfQueuedSkills(currentCharacter.GetSkillQueue());
    }

    private void DisplayQueuedSkills(List<(SkillBase skill, CharacterManager target)> skillQueue)
    {
        // 큐에 있는 스킬들을 화면에 표시
        foreach (var skill in skillQueue)
        {
            // UI에 스킬 아이콘, 이름, 타겟 정보를 표시하는 로직 추가
            // 예: skillBar에 스킬 아이콘 추가
        }
    }

    private void NotifyOpponentOfQueuedSkills(List<(SkillBase skill, CharacterManager target)> skillQueue)
    {
        // 상대방에게 스킬 목록을 전달하는 로직 (멀티플레이어 게임일 경우 네트워크 메시지 전송 등)
    }
    public void ShowCounterSkillUI(CharacterManager currentCharacter)
    {
        // 대응 스킬 UI 업데이트 - 방어 캐릭터로 지정됐을때만 출력
        if (currentCharacter.combatHandler.isDefenseCharacter)
        {
            skillBar.SetActive(true);

            int counterSkillIndex = 0;

            foreach (SkillBase skill in currentCharacter.character.Skills)
            {
                if (skill.QuickSlot && skill.IsCounterSkill && skill.CanUse)  // 사용 가능하고 퀵슬롯에 등록된 대응 스킬만 보여줌
                {
                    if (counterSkillIndex < hotbarButtons.Length)
                    {
                        GameObject button = hotbarButtons[counterSkillIndex];

                        button.GetComponent<RawImage>().texture = skill.icon; // Texture2D로 아이콘 설정
                        button.GetComponent<RawImage>().enabled = true;       // 아이콘 표시
                        button.GetComponent<Button>().interactable = true;    // 버튼 활성화

                        button.GetComponent<Button>().onClick.RemoveAllListeners();
                        button.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            currentCharacter.SelectCounterSkill(skill, currentCharacter);  // 대응 스킬 선택
                        });

                        counterSkillIndex++; // 다음 핫바 슬롯으로 이동
                    }
                }
            }
        }
    }

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
