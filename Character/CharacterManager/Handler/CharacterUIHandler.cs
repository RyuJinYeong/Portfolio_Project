using SoftKitty.InventoryEngine;
using System.Collections.Generic;
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

    public GameObject turnIcon;
    public GameObject skillQueuePanel;

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
