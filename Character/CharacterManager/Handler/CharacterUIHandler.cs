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

    public GameObject CounterButton;
    public GameObject turnIcon;
    public GameObject skillQueuePanel;
    public GameObject counterSkillQueuePanel;
    public GameObject skillIconPrefab;

    [Header("Optional Icons")]
    public Texture2D concealedSkillIcon;

    public TextMeshProUGUI characterName;

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI mentalityText;

    public GameObject statusEffectIconPrefab;
    public Transform statusEffectIconParent;

    private CharacterManager characterManager;
    private Camera mainCamera;
    private List<GameObject> activeStatusIcons = new List<GameObject>();

    void Awake()
    {
        mainCamera = Camera.main;
        characterManager = GetComponentInParent<CharacterManager>();
    }

    void Update()
    {
        FaceCamera();
    }

    public void UpdateUI()
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
        if (mainCamera != null && StatusCanvas != null)
        {
            StatusCanvas.transform.LookAt(
                transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
    }

    public void UpdateSkillQueueUI(List<SkillQueueData> queue, CharacterManager caster)
    {
        if (queue == null || caster == null || skillQueuePanel == null || skillIconPrefab == null)
            return;

        Debug.Log(caster.character.Name + "의 SkillQueue를 기준으로 " + characterManager.character.Name + " UI 갱신");

        ClearChildren(skillQueuePanel.transform);

        for (int i = 0; i < queue.Count; i++)
        {
            SkillQueueData data = queue[i];

            if (data == null || data.skill == null)
                continue;

            if (data.target != characterManager)
                continue;

            GameObject skillIconInstance = Instantiate(skillIconPrefab, skillQueuePanel.transform);

            ApplySkillIcon(skillIconInstance, data);
            ApplySkillButtonData(skillIconInstance, data, i, false);
            ApplyQueueText(skillIconInstance, data, i);

            if (caster.character.IsMine)
            {
                Button button = skillIconInstance.GetComponent<Button>();

                if (button != null)
                {
                    int capturedIndex = i;

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        caster.combatHandler.RemoveSkillFromQueue(capturedIndex);
                    });
                }
            }
        }
    }

    public void UpdateCounterSkillQueueUI(List<SkillQueueData> queue, CharacterManager caster)
    {
        if (queue == null || caster == null || counterSkillQueuePanel == null || skillIconPrefab == null)
            return;

        Debug.Log(caster.character.Name + "의 CounterSkillQueue를 기준으로 " + characterManager.character.Name + " UI 갱신");

        ClearChildren(counterSkillQueuePanel.transform);

        for (int i = 0; i < queue.Count; i++)
        {
            SkillQueueData data = queue[i];

            if (data == null || data.skill == null)
                continue;

            if (data.target != characterManager)
                continue;

            GameObject skillIconInstance = Instantiate(skillIconPrefab, counterSkillQueuePanel.transform);

            ApplySkillIcon(skillIconInstance, data);
            ApplySkillButtonData(skillIconInstance, data, i, true);
            ApplyQueueText(skillIconInstance, data, i);

            if (caster.character.IsMine)
            {
                Button button = skillIconInstance.GetComponent<Button>();

                if (button != null)
                {
                    int capturedIndex = i;
                    SkillDefinitionSO capturedSkill = data.skill;

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (caster.character.DefaultCounterSkill > 0 &&
                            capturedSkill.uid == caster.character.DefaultCounterSkill)
                        {
                            Debug.Log("기본 대응 스킬은 제거할 수 없습니다: " + capturedSkill.skillName);
                            return;
                        }

                        caster.combatHandler.RemoveCounterSkillFromQueue(capturedIndex);
                    });
                }
            }
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    private void ApplySkillIcon(GameObject skillIconInstance, SkillQueueData data)
    {
        if (skillIconInstance == null || data == null || data.skill == null)
            return;

        RawImage rawImage = skillIconInstance.GetComponent<RawImage>();

        if (rawImage == null)
            return;

        if (ShouldShowConcealedIcon(data))
        {
            rawImage.texture = concealedSkillIcon;
            return;
        }

        rawImage.texture = data.skill.icon;
    }

    private void ApplySkillButtonData(
        GameObject skillIconInstance,
        SkillQueueData data,
        int queueIndex,
        bool isCounterSkill)
    {
        if (skillIconInstance == null || data == null)
            return;

        SkillButton sb = skillIconInstance.GetComponent<SkillButton>();

        if (sb == null)
            return;

        sb.skill = data.skill;
        sb.queueData = data;
        sb.queueIndex = queueIndex;
        sb.isCounterSkill = isCounterSkill;
    }

    private void ApplyQueueText(GameObject skillIconInstance, SkillQueueData data, int queueIndex)
    {
        if (skillIconInstance == null || data == null)
            return;

        TextMeshProUGUI[] texts = skillIconInstance.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts == null || texts.Length == 0)
            return;

        texts[0].text = (queueIndex + 1).ToString();

        if (texts.Length >= 2)
        {
            texts[1].text = GetSkillDisplayText(data);
        }
    }

    private bool ShouldShowConcealedIcon(SkillQueueData data)
    {
        if (data == null)
            return false;

        if (!data.isConcealed)
            return false;

        return data.revealLevel != RevealLevel.Full;
    }

    private string GetSkillDisplayText(SkillQueueData data)
    {
        if (data == null || data.skill == null)
            return string.Empty;

        if (!data.isConcealed)
            return data.skill.skillName;

        switch (data.revealLevel)
        {
            case RevealLevel.Full:
                return data.skill.skillName;

            case RevealLevel.Partial:
                return GetPartialSkillInfo(data.skill);

            case RevealLevel.None:
            default:
                return "은폐";
        }
    }

    private string GetPartialSkillInfo(SkillDefinitionSO skill)
    {
        if (skill == null)
            return "은폐";

        string style = GetStyleText(skill.style);
        string power = GetPowerRankText(skill.GetTotalDamageMultiplier());
        string speed = GetSpeedRankText(skill.activationSpeed);

        return $"은폐: {style} / {power} / {speed}";
    }

    private string GetStyleText(SkillStyle style)
    {
        return style switch
        {
            SkillStyle.Strength => "힘",
            SkillStyle.Dexterity => "기교",
            SkillStyle.Speed => "속도",
            _ => "-"
        };
    }

    private string GetPowerRankText(float damageMultiplier)
    {
        if (damageMultiplier >= 1.5f)
            return "고위력";

        if (damageMultiplier >= 0.9f)
            return "중위력";

        return "저위력";
    }

    private string GetSpeedRankText(float activationSpeed)
    {
        if (activationSpeed >= 1.2f)
            return "빠름";

        if (activationSpeed >= 0.9f)
            return "보통";

        return "느림";
    }

    public void UpdateResourceTexts()
    {
        if (characterManager == null || characterManager.character == null || characterManager.character.FinalStats == null)
            return;

        if (hpText != null)
            hpText.text = $"{characterManager.character.CurrentHp} / {characterManager.character.FinalStats.MaxHp}";

        if (staminaText != null)
            staminaText.text = $"{characterManager.character.CurrentStamina} / {characterManager.character.FinalStats.MaxStamina} (+{characterManager.character.FinalStats.StaminaRecovery})";

        if (mentalityText != null)
            mentalityText.text = $"{characterManager.character.CurrentMentality} / {characterManager.character.FinalStats.MaxMentality} (+{characterManager.character.FinalStats.MentalityRecovery})";
    }

    private void UpdateHPBar()
    {
        if (characterManager == null || characterManager.character == null || characterManager.character.FinalStats == null)
            return;

        if (hpBar == null)
            return;

        float hpPercentage = 0f;

        if (characterManager.character.FinalStats.MaxHp > 0)
            hpPercentage = (float)characterManager.character.CurrentHp / characterManager.character.FinalStats.MaxHp;

        Slider slider = hpBar.GetComponent<Slider>();

        if (slider != null)
            slider.value = hpPercentage;
    }

    private void UpdateStaminaBar()
    {
        if (characterManager == null || characterManager.character == null || characterManager.character.FinalStats == null)
            return;

        if (staminaBar == null)
            return;

        float staminaPercentage = 0f;

        if (characterManager.character.FinalStats.MaxStamina > 0)
            staminaPercentage = (float)characterManager.character.CurrentStamina / characterManager.character.FinalStats.MaxStamina;

        Slider slider = staminaBar.GetComponent<Slider>();

        if (slider != null)
            slider.value = staminaPercentage;
    }

    private void UpdateMentalityBar()
    {
        if (characterManager == null || characterManager.character == null || characterManager.character.FinalStats == null)
            return;

        if (mentalityBar == null)
            return;

        float mentalityPercentage = 0f;

        if (characterManager.character.FinalStats.MaxMentality > 0)
            mentalityPercentage = (float)characterManager.character.CurrentMentality / characterManager.character.FinalStats.MaxMentality;

        Slider slider = mentalityBar.GetComponent<Slider>();

        if (slider != null)
            slider.value = mentalityPercentage;
    }

    public void UpdateStatusEffects()
    {
        /*
        foreach (var icon in activeStatusIcons)
        {
            Destroy(icon);
        }

        activeStatusIcons.Clear();

        foreach (var effect in activeEffects)
        {
            GameObject iconInstance = Instantiate(statusEffectIconPrefab, statusEffectIconParent);
            iconInstance.GetComponentInChildren<RawImage>().texture = effect.Icon;
            activeStatusIcons.Add(iconInstance);
        }
        */
    }

    private void UpdateTurnIcon()
    {
        if (turnIcon == null || characterManager == null)
            return;

        turnIcon.SetActive(characterManager.isPlayerTurn);
    }

    private void UpdateCharacterName()
    {
        if (characterName == null || characterManager == null || characterManager.character == null)
            return;

        characterName.text = characterManager.character.Name;
    }
}