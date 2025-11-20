using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;


    [Header("Tooltip UI Components")]
    public RawImage skillIcon;
    public Text skillNameText;
    public Text skillTypeText;
    public Text skillSpeedText;
    public Text costText;
    public Text descriptionText;
    public GameObject tooltipObject;

    public TextMeshProUGUI tooltipText;    

    public Vector3 tooltipOffset = new Vector3(40, -25, 0); // 마우스 커서로부터의 오프셋
    private bool isTooltipActive = false;

    #region TooltipKeyword
    public Dictionary<string, string> keywordTooltips = new Dictionary<string, string>
    {
        { "근력", "무기를 통한 물리 데미지 판정 관련 스탯" },
        { "기교", "경량 무기를 통한 물리 데미지 판정 관련 스탯" },
        { "속도", "물리 관련 행동 수치 및 공격속도 관련 스탯" },
        { "지능", "마법 공격력 및 정신력 관련 스탯" },
        { "지혜", "마법 관련 행동 수치 및 시전속도 관련 스탯" },
        { "건강", "HP 및 지구력 관련 스탯" },
        { "인내", "방어력 관련 스탯" },
        { "철벽", "모든 방어력 6 증가" },
        { "검술 숙련", "도검류 무기 장착시 물리공격력 20 증가" },
        { "야만인의 힘", "근력 10 증가" },
        { "궁술 숙련", "활 장착시 물리공격력 20 증가" },
        { "손재주", "기교 10 증가" },
        { "기초 원소적성", "랜덤한 속성적성 획득" },
        { "외팔", "기본 근력 스탯 반감, 양손무기 및 보조무기 착용 불가" },
        { "화염 원소적성", "화염 속성 마법 스킬 습득 가능, 속성 저항력 10% 증가" },
        { "물 원소적성", "물 속성 마법 스킬 습득 가능, 속성 저항력 10% 증가" },
        { "땅 원소적성", "땅 속성 마법 스킬 습득 가능, 속성 저항력 10% 증가" },
        { "바람 원소적성", "바람 속성 마법 스킬 습득 가능, 속성 저항력 10% 증가" },
        { "둔재", "기본 기교, 지능, 지혜 스탯 반감" },
        { "날카로운 눈썰미", "눈썰미 10 증가" },
        { "신속한 몸놀림", "속도 10 증가" },
        { "예리한 통찰력", "통찰력 10 증가" }
    };
    #endregion

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

    private void Update()
    {
        if (isTooltipActive)
        {
            UpdateTooltipPosition(Input.mousePosition);
        }
    }

    // SkillBase 객체를 이용한 툴팁
    public void ShowTooltip(SkillBase skill, Vector3 position)
    {
        skillIcon.texture = skill.icon;
        skillNameText.text = skill.name;
        skillTypeText.text = $"{(skill.Type == SkillType.Physical ? "물리 스킬" : "마법 스킬")} - {(skill.IsRangedSkill ? "원거리" : "근접")}";
        skillSpeedText.text = $"발동속도: {skill.ActivationSpeed}";
        costText.text = "";
        if (skill.StaminaCost != 0)
        {
            costText.text = $"지구력 : {skill.StaminaCost} 소모";
        }
        
        if(skill.MentalCost != 0)
        {
            costText.text += $" 정신력 : {skill.MentalCost} 소모";
        }
        descriptionText.text = skill.description;

        UpdateTooltipPosition(position);
        if(skill.uid != 0)
            tooltipObject.SetActive(true);
        isTooltipActive = true;
    }

    public void ShowTooltip(string text, Vector3 position)
    {
        tooltipText.text = text;
        UpdateTooltipPosition(position);
        tooltipObject.SetActive(true);
        isTooltipActive = true;
    }

    public void HideTooltip()
    {
        tooltipObject.SetActive(false);
        isTooltipActive = false;
    }

    public void UpdateTooltipPosition(Vector3 position)
    {
        tooltipObject.transform.position = position + tooltipOffset;
    }
}