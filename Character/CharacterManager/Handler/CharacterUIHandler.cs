using SoftKitty.InventoryEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CharacterUIHandler : MonoBehaviour
{
    public GameObject StatusCanvas;

    public GameObject hpBar;
    public GameObject staminaBar;
    public GameObject mentalityBar;
    public GameObject statusEffectPanel;

    public GameObject CounterButton;
    public GameObject turnIcon;
    public GameObject skillQueuePanel;
    public GameObject counterSkillQueuePanel;
    public GameObject skillIconPrefab;
    
    public TextMeshProUGUI characterName;

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI mentalityText;    

    private CharacterManager characterManager;
    private Camera mainCamera; 

    void Awake()
    {        
        mainCamera = Camera.main;
        characterManager = GetComponentInParent<CharacterManager>();
    }

    void Update()
    {
        // 매 프레임마다 UI 캔버스를 카메라 방향으로 회전
        FaceCamera();
    }

    public void UpdateUI() // 상태 변화 감지 후 CharacterManager에서 호출
    {
        UpdateHPBar();
        UpdateStaminaBar();
        UpdateMentalityBar();
        UpdateStatusEffects();
        UpdateTurnIcon();
        UpdateCharacterName();
        UpdateResourceTexts();
    }

    public void FaceCamera()
    {
        // 현재 UI 캔버스가 카메라를 항상 바라보도록 설정
        if (mainCamera != null)
        {
            StatusCanvas.transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }

    // 스킬 큐 UI 업데이트
    public void UpdateSkillQueueUI(List<(SkillBase skill, CharacterManager target)> queue, CharacterManager caster)
    {
        Debug.Log(caster.character.Name +"의 Skillqueue를 기준으로 " + this.characterManager.character.Name + " UI 갱신");
        // 기존 UI 전부 제거
        foreach (Transform child in skillQueuePanel.transform)
            Destroy(child.gameObject);

        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i].target == this.characterManager)
            {
                // 스킬 아이콘 프리팹 인스턴스화 및 부모 설정
                GameObject skillIconInstance = Instantiate(skillIconPrefab, skillQueuePanel.transform);
                queue[i].skill.LoadIcon(); // 스킬 아이콘 로드
                skillIconInstance.GetComponent<RawImage>().texture = queue[i].skill.icon;  // 스킬 아이콘 설정                

                var sb = skillIconInstance.GetComponent<SkillButton>();
                if (sb != null)
                {
                    sb.skill = queue[i].skill;
                    sb.queueIndex = i; // 0부터 시작
                }

                // 순서 표시 (좌상단 텍스트)
                TextMeshProUGUI orderText = skillIconInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (orderText != null)
                {
                    orderText.text = (i + 1).ToString();
                }

                if (caster.character.IsMine)
                {
                    // 클릭 시 CombatHandler의 스킬 제거 메서드를 호출하는 리스너 추가
                    var button = skillIconInstance.GetComponent<Button>();
                    if (button != null)
                    {
                        button.onClick.AddListener(() =>
                        {
                            if (sb != null)
                            {
                                caster.combatHandler.RemoveSkillFromQueue(sb.skill, sb.queueIndex);
                            }
                        });
                    }
                }
            }
        }
    }

    public void UpdateCounterSkillQueueUI(List<(SkillBase skill, CharacterManager target)> queue, CharacterManager caster)
    {
        Debug.Log(caster.character.Name + "의 CounterSkillqueue를 기준으로 " + this.characterManager.character.Name + " UI 갱신");
        // 기존 UI 전부 제거
        foreach (Transform child in counterSkillQueuePanel.transform)
            Destroy(child.gameObject);

        // 대응 스킬 큐 업데이트

        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i].target == this.characterManager)
            {
                // 스킬 아이콘 프리팹 인스턴스화 및 부모 설정
                GameObject skillIconInstance = Instantiate(skillIconPrefab, counterSkillQueuePanel.transform);
                queue[i].skill.LoadIcon(); // 스킬 아이콘 로드
                skillIconInstance.GetComponent<RawImage>().texture = queue[i].skill.icon;  // 스킬 아이콘 설정                

                var sb = skillIconInstance.GetComponent<SkillButton>();
                if (sb != null)
                {
                    sb.skill = queue[i].skill;
                    sb.queueIndex = i; // 0부터 시작
                }

                // 순서 표시 (좌상단 텍스트)
                TextMeshProUGUI orderText = skillIconInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (orderText != null)
                {
                    orderText.text = (i + 1).ToString();
                }

                if (caster.character.IsMine)
                {
                    // 클릭 시 CombatHandler의 스킬 제거 메서드를 호출하는 리스너 추가
                    var button = skillIconInstance.GetComponent<Button>();
                    if (button != null)
                    {
                        button.onClick.AddListener(() =>
                        {
                            if (sb != null && sb.skill != caster.character.DefaultCounterSkill)
                            {
                                caster.combatHandler.RemoveCounterSkillFromQueue(sb.skill, sb.queueIndex);
                            }
                            else
                            {
                                Debug.Log("기본 대응 스킬은 제거할 수 없습니다: " + sb.skill.name);
                            }
                        });
                    }
                }
            }
        }
    }

    public void UpdateResourceTexts()
    {
        hpText.text = $"{characterManager.character.FinalStats.CurrentHp} / {characterManager.character.FinalStats.MaxHp}";
        staminaText.text = $"{characterManager.character.FinalStats.CurrentStamina} / {characterManager.character.FinalStats.MaxStamina} (+{characterManager.character.FinalStats.StaminaRecovery})";
        mentalityText.text = $"{characterManager.character.FinalStats.CurrentMentality} / {characterManager.character.FinalStats.MaxMentality} (+{characterManager.character.FinalStats.MentalityRecovery})";
    }

    private void UpdateHPBar()
    {
        float hpPercentage = (float)characterManager.character.FinalStats.CurrentHp / characterManager.character.FinalStats.MaxHp;
        hpBar.GetComponent<Slider>().value = hpPercentage;
    }

    private void UpdateStaminaBar()
    {
        float staminaPercentage = (float)characterManager.character.FinalStats.CurrentStamina / characterManager.character.FinalStats.MaxStamina;
        staminaBar.GetComponent<Slider>().value = staminaPercentage;
    }

    private void UpdateMentalityBar()
    {
        float mentalityPercentage = (float)characterManager.character.FinalStats.CurrentMentality / characterManager.character.FinalStats.MaxMentality;
        mentalityBar.GetComponent<Slider>().value = mentalityPercentage;
    }

    public GameObject statusEffectIconPrefab;  // 상태이상 아이콘 프리팹
    public Transform statusEffectIconParent;  // 상태이상 아이콘을 표시할 부모 오브젝트
    private List<GameObject> activeStatusIcons = new List<GameObject>(); // 활성화 상태이상 아이콘

    // 상태이상을 UI에 표시하는 메서드
    public void UpdateStatusEffects()
    {
        /*
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
        }*/
    }

    private void UpdateTurnIcon()
    {
        turnIcon.SetActive(characterManager.isPlayerTurn);
    }

    private void UpdateCharacterName()
    {
        characterName.text = characterManager.character.Name;
    }
}
