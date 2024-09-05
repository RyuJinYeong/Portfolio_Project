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
}
