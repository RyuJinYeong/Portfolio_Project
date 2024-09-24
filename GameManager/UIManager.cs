using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

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

    public GameObject skillButtonPrefab;
    public GameObject skillBar;
    public GameObject infoPanel;
    public Transform skillButtonParent;

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

        // 기존 스킬 버튼 제거
        foreach (Transform child in skillButtonParent)
        {
            Destroy(child.gameObject);
            skillBar.SetActive(false);
        }

        if (characterData.IsMine)
        {
            skillBar.SetActive(true);
            // 새로운 스킬 버튼 생성
            foreach (SkillBase skill in characterData.Skills)
            {
                if (skill.CanUse)
                {
                    GameObject skillButton = Instantiate(skillButtonPrefab, skillButtonParent);
                    skillButton.GetComponentInChildren<Image>().sprite = skill.skillIcon;
                    skillButton.GetComponent<Button>().onClick.AddListener(() => // 버튼 액션 감지
                    {
                        characterTargeting.StartTargeting(skill); 
                    });
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
        skillBar.SetActive(true);  // 대응 스킬 UI 활성화

        foreach (SkillBase skill in currentCharacter.character.Skills)
        {
            if (skill.IsCounterSkill)  // 대응 가능한 스킬만 보여줌
            {
                GameObject skillButton = Instantiate(skillButtonPrefab, skillButtonParent);
                skillButton.GetComponentInChildren<Image>().sprite = skill.skillIcon;
                skillButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    currentCharacter.SelectSkill(skill, currentCharacter);  // 대응 스킬 선택
                });
            }
        }
    }

    // 활성화된 시너지를 UI에 표시하는 메서드
    public void UpdateSynergyUI(List<string> activeSynergies)
    {
        string synergyText = "Active Synergies:\n";
        foreach (var synergy in activeSynergies)
        {
            synergyText += $"{synergy}\n";
        }
        // 시너지 정보를 화면에 출력 (구체적인 UI 구현은 별도로)
        Debug.Log(synergyText);
    }
}
