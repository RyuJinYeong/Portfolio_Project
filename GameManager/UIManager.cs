using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public GameObject synergyInfoPanel; // 시너지 정보가 표시되는 패널

    public GameObject statusEffectIconPrefab;  // 상태이상 아이콘 프리팹
    public Transform statusEffectIconParent;  // 상태이상 아이콘을 표시할 부모 오브젝트
    private List<GameObject> activeStatusIcons = new List<GameObject>(); // 활성화 상태이상 아이콘

    [Header("Character Info UI Elements")]
    public Image characterPortrait;
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

    public void DisplayCharacterInfo(CharacterManager characterManager)
    {
        infoPanel.SetActive(true);
        CharacterData characterData = characterManager.character;

        characterPortrait.sprite = characterData.Portrait; 
        characterName.text = characterData.Name;

        currentHP.text = $"{characterData.FinalStats.CurrentHp}";
        characterHP.text = $"{characterData.FinalStats.MaxHp}";

        currentStamina.text = $"{characterData.FinalStats.CurrentStamina}";
        characterStamina.text  = $"{ characterData.FinalStats.MaxStamina}";

        currentMental.text = $"{characterData.FinalStats.CurrentMentality}";
        characterMental.text  = $"{ characterData.FinalStats.MaxMentality}";

        characterPhysicalAttack.text = $"{characterData.FinalStats.PhysicalAttack}";
        characterMagicAttack.text = $"{characterData.FinalStats.MagicalAttack}";
        characterPhysicalDefense.text = $"{characterData.FinalStats.PhysicalDefense}";
        characterMagicDefense.text = $"{characterData.FinalStats.MagicalDefense}";

        skillBar.SetActive(characterData.IsMine); // 캐릭터가 자신의 것일 경우 스킬바 활성화

        // 핫바 스킬 업데이트
        UpdateHotbarSkills(characterData);        
    }

    // 핫바 스킬 업데이트 메서드
    private void UpdateHotbarSkills(CharacterData characterData)
    {
        // 모든 핫바 버튼과 RawImage를 비활성화
        foreach (var button in hotbarButtons)
        {
            button.GetComponent<Button>().interactable = false; // 버튼 비활성화
            button.GetComponent<RawImage>().enabled = false;    // 이미지 비활성화
        }

        // 사용 가능한 스킬이 있는 경우 핫바에 표시
        int hotbarIndex = 0;

        foreach (SkillBase skill in characterData.Skills)
        {
            if (skill.CanUse && !skill.IsCounterSkill) // 사용 가능하고 대응 스킬이 아닌 경우
            {
                if (hotbarIndex < hotbarButtons.Length) // 핫바 슬롯이 남아있는 경우
                {
                    GameObject button = hotbarButtons[hotbarIndex];

                    button.GetComponent<RawImage>().texture = skill.icon; // Texture2D로 아이콘 설정
                    button.GetComponent<RawImage>().enabled = true;       // 아이콘 표시
                    button.GetComponent<Button>().interactable = true;    // 버튼 활성화

                    // 버튼 클릭 시 스킬 사용 처리
                    button.GetComponent<Button>().onClick.RemoveAllListeners(); // 기존 리스너 제거
                    button.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        characterTargeting.StartTargeting(skill); // 스킬 타겟팅 시작
                    });

                    hotbarIndex++; // 다음 핫바 슬롯으로 이동
                }
            }
        }
    }

    public void ShowQueuedSkills(CharacterManager currentCharacter)
    {
        // 기존 UI를 업데이트하고, 큐에 있는 스킬 목록을 시각적으로 표시
        DisplayQueuedSkills(currentCharacter.skillQueue);

        // 상대방에게도 큐에 쌓인 스킬 목록을 보여줌
        NotifyOpponentOfQueuedSkills(currentCharacter.skillQueue);
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
        // 대응 스킬 UI 업데이트
        skillBar.SetActive(true);

        int counterSkillIndex = 0;

        foreach (SkillBase skill in currentCharacter.character.Skills)
        {
            if (skill.IsCounterSkill && skill.CanUse)  // 대응 가능한 스킬만 보여줌
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
                        currentCharacter.SelectSkill(skill, currentCharacter);  // 대응 스킬 선택
                    });

                    counterSkillIndex++; // 다음 핫바 슬롯으로 이동
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

    // 상태이상을 UI에 표시하는 메서드
    public void UpdateStatusEffects(List<StatusEffect> activeEffects)
    {
        // 기존 아이콘 초기화
        foreach (var icon in activeStatusIcons)
        {
            Destroy(icon);
        }
        activeStatusIcons.Clear();

        // 새로운 상태이상 아이콘 생성
        foreach (var effect in activeEffects)
        {
            GameObject iconInstance = Instantiate(statusEffectIconPrefab, statusEffectIconParent);
            iconInstance.GetComponentInChildren<RawImage>().texture = effect.Icon;
            //iconInstance.GetComponent<TooltipManager>().SetupTooltip(effect.Description);
            activeStatusIcons.Add(iconInstance);
        }
    }
}
