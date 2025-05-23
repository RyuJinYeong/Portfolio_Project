using SoftKitty.InventoryEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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

    private List<GameObject> skillQueueIcons = new List<GameObject>(); // 생성된 스킬 큐 아이콘 리스트
    private List<GameObject> counterSkillQueueIcons = new List<GameObject>(); // 생성된 스킬 큐 아이콘 리스트

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

    // 스킬 큐에 스킬 추가
    public void AddSkillToQueue(SkillBase skill, int order, CharacterManager caster)
    {
        // 스킬 아이콘 프리팹 인스턴스화 및 부모 설정
        GameObject skillIconInstance = Instantiate(skillIconPrefab, skillQueuePanel.transform);
        skillIconInstance.GetComponent<RawImage>().texture = skill.icon;  // 스킬 아이콘 설정

        var sb = skillIconInstance.GetComponent<SkillButton>();
        if (sb != null)
        {
            sb.skill = skill;
            sb.queueIndex = order - 1; // 0부터 시작
        }

        // 순서 표시 (좌상단 텍스트)
        TextMeshProUGUI orderText = skillIconInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (orderText != null)
        {
            orderText.text = order.ToString();
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
                        skillQueueIcons.Remove(skillIconInstance);

                        // 남아있는 스킬들의 순서 다시 설정
                        UpdateSkillQueueUI();

                        Destroy(skillIconInstance);
                    }
                });
            }
        }

        // 생성된 아이콘을 리스트에 저장
        skillQueueIcons.Add(skillIconInstance);
    }


    // 스킬 큐에서 스킬 제거
    public void RemoveSkillFromQueue(SkillBase skill, int index)
    {        
        // UI에서 스킬 아이콘 제거
        if (index < 0 || index >= skillQueueIcons.Count) return;

        GameObject icon = skillQueueIcons[index];
        skillQueueIcons.RemoveAt(index);
        Destroy(icon);

        // 남아있는 스킬들의 순서 다시 설정
        UpdateSkillQueueUI();
    }

    // 스킬 큐 UI 순서 업데이트
    private void UpdateSkillQueueUI()
    {
        Debug.Log("UpdateSkillQueueUI 호출 skillQueueIcons.Count = " + skillQueueIcons.Count);
        for (int i = 0; i < skillQueueIcons.Count; i++)
        {
            Debug.Log("i = " + i + " skillQueueIcons[i] = " + skillQueueIcons[i].GetComponent<SkillButton>().skill.name);
            TextMeshProUGUI orderText = skillQueueIcons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (orderText != null)
            {
                orderText.text = (i + 1).ToString();
            }

            // SkillButton 인덱스 갱신
            if (skillQueueIcons[i].TryGetComponent<SkillButton>(out var sb))
            {
                sb.queueIndex = i;
            }
        }
    }


    // 대응 스킬 큐에 스킬 추가
    public void AddCounterSkillToQueue(SkillBase skill, int order, CharacterManager caster)
    {
        // 스킬 아이콘 프리팹 인스턴스화 및 부모 설정
        skill.LoadIcon();
        GameObject skillIconInstance = Instantiate(skillIconPrefab, counterSkillQueuePanel.transform);
        skillIconInstance.GetComponent<RawImage>().texture = skill.icon;  // 스킬 아이콘 설정
        skillIconInstance.GetComponent<SkillButton>().skill = skill; // 툴팁용 스킬 객체 참조

        var sb = skillIconInstance.GetComponent<SkillButton>();
        if (sb != null)
        {
            sb.skill = skill;
            sb.queueIndex = order - 1;
        }

        // 순서 표시 (좌상단 텍스트)
        TextMeshProUGUI orderText = skillIconInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (orderText != null)
        {
            orderText.text = order.ToString();
        }

        if (caster.character.IsMine)
        {
            // 클릭 시 CombatHandler의 스킬 제거 메서드를 호출하는 리스너 추가
            var button = skillIconInstance.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    var bInfo = button.GetComponent<SkillButton>();
                    if (bInfo != null)
                    {
                        caster.combatHandler.RemoveCounterSkillFromQueue(bInfo.skill, bInfo.queueIndex);

                        counterSkillQueueIcons.Remove(skillIconInstance);
                        Destroy(skillIconInstance);
                        UpdateCounterSkillQueueUI();
                    }
                });
            }
        }

        // 생성된 아이콘을 리스트에 저장
        counterSkillQueueIcons.Add(skillIconInstance);
    }


    // 대응 스킬 큐에서 스킬 제거
    public void RemoveCounterSkillFromQueue(SkillBase skill, int index)
    {
        // UI에서 스킬 아이콘 제거
        if (index < 0 || index >= counterSkillQueueIcons.Count) return;

        GameObject icon = counterSkillQueueIcons[index];
        counterSkillQueueIcons.RemoveAt(index);
        Destroy(icon);

        // 남아있는 스킬들의 순서 다시 설정
        UpdateCounterSkillQueueUI();
    }

    // 대응 스킬 큐 UI 순서 업데이트
    private void UpdateCounterSkillQueueUI()
    {
        for (int i = 0; i < counterSkillQueueIcons.Count; i++)
        {
            TextMeshProUGUI orderText = counterSkillQueueIcons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (orderText != null)
            {
                orderText.text = (i + 1).ToString();
            }

            SkillButton sb = counterSkillQueueIcons[i].GetComponent<SkillButton>();
            if (sb != null)
            {
                sb.queueIndex = i;
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
