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
    public GameObject pairedSkillQueuePrefab;

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

        if (TryUpdatePairedSkillQueueUI(queue, caster))
            return;

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
                        TooltipManager.Instance?.HideTooltip();

                        CharacterTargeting targeting = UIManager.Instance != null
                            ? UIManager.Instance.characterTargeting
                            : null;

                        if (targeting != null &&
                            targeting.TryReplaceQueuedAttack(caster, capturedIndex))
                        {
                            return;
                        }

                        caster.combatHandler.RemoveSkillFromQueue(capturedIndex);
                    });
                }
            }
        }
    }

    public void UpdateCounterSkillQueueUI(List<SkillQueueData> queue, CharacterManager caster)
    {
        if (queue == null || caster == null || skillQueuePanel == null || skillIconPrefab == null)
            return;

        Debug.Log(caster.character.Name + "의 CounterSkillQueue를 기준으로 " + characterManager.character.Name + " UI 갱신");

        CharacterManager attacker = TurnManager.Instance != null
            ? TurnManager.Instance.currentCharacter
            : null;

        if (attacker != null &&
            attacker.combatHandler != null &&
            TryUpdatePairedSkillQueueUI(attacker.combatHandler.GetSkillQueue(), attacker))
        {
            return;
        }

        ClearChildren(skillQueuePanel.transform);

        for (int i = 0; i < queue.Count; i++)
        {
            SkillQueueData data = queue[i];

            if (data == null)
                continue;

            GameObject skillIconInstance = Instantiate(skillIconPrefab, skillQueuePanel.transform);

            ApplySkillIcon(skillIconInstance, data);
            ApplySkillButtonData(skillIconInstance, data, i, true);
            ApplyQueueText(skillIconInstance, data, i);

            SkillButton skillButton = skillIconInstance.GetComponent<SkillButton>();
            CharacterManager protectedTarget = data.protectedTarget ?? data.target;

            if (skillButton != null &&
                data.incomingSkill != null &&
                attacker != null &&
                protectedTarget != null)
            {
                SkillQueueData incomingAttack = new SkillQueueData(
                    data.incomingSkill,
                    attacker,
                    protectedTarget,
                    false,
                    data.order);
                incomingAttack.revealLevel = data.revealLevel;

                skillButton.pairedAttackQueueData = incomingAttack;
                skillButton.pairedCounterSkill = data.skill;
                skillButton.counterUser = caster;
                skillButton.protectedTarget = protectedTarget;
            }

            if (caster.character.IsMine)
            {
                Button button = skillIconInstance.GetComponent<Button>();

                if (button != null)
                {
                    int capturedIndex = i;
                    SkillButton capturedSkillButton = skillButton;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        TooltipManager.Instance?.HideTooltip();

                        CharacterTargeting targeting = UIManager.Instance != null
                            ? UIManager.Instance.characterTargeting
                            : null;

                        if (targeting != null && targeting.isDefenseSkillTargeting)
                        {
                            if (TurnManager.Instance != null &&
                                TurnManager.Instance.defenseCharacter == caster)
                            {
                                caster.combatHandler.SetOrResetCounterSkill(
                                    capturedIndex,
                                    targeting.selectedSkill);
                                targeting.StopTargeting();
                            }

                            return;
                        }

                        caster.combatHandler.CycleBasicCounterSkill(capturedIndex);
                    });

                    if (capturedSkillButton != null)
                        capturedSkillButton.onRightClick = null;
                }
            }
        }
    }

    private bool TryUpdatePairedSkillQueueUI(
        List<SkillQueueData> attackQueue,
        CharacterManager attacker)
    {
        if (attackQueue == null ||
            attacker == null ||
            characterManager == null ||
            characterManager.combatHandler == null ||
            pairedSkillQueuePrefab == null ||
            skillQueuePanel == null)
        {
            return false;
        }

        TurnManager turnManager = TurnManager.Instance;
        bool isProtectingOther =
            turnManager != null &&
            turnManager.defenseCharacter == characterManager &&
            turnManager.defenseTarget != null &&
            turnManager.defenseTarget != characterManager;

        if (isProtectingOther)
            return false;

        List<(SkillQueueData data, int index)> targetedAttacks = attackQueue
            .Select((data, index) => (data, index))
            .Where(entry =>
                entry.data != null &&
                entry.data.skill != null &&
                entry.data.target == characterManager)
            .ToList();

        if (targetedAttacks.Count == 0)
            return false;

        CharacterManager counterUser = characterManager;
        List<SkillQueueData> counterQueue = characterManager.combatHandler
            .GetCounterSkillQueue()
            .Where(entry =>
                entry != null &&
                (entry.protectedTarget ?? entry.target) == characterManager)
            .ToList();

        if (counterQueue == null ||
            counterQueue.Count != targetedAttacks.Count ||
            counterQueue.Any(entry =>
                entry == null ||
                (entry.protectedTarget ?? entry.target) != characterManager))
        {
            return false;
        }

        ClearChildren(skillQueuePanel.transform);

        for (int localIndex = 0; localIndex < targetedAttacks.Count; localIndex++)
        {
            SkillQueueData attackData = targetedAttacks[localIndex].data;
            int attackIndex = targetedAttacks[localIndex].index;
            SkillQueueData counterData = counterQueue[localIndex];
            GameObject pair = Instantiate(pairedSkillQueuePrefab, skillQueuePanel.transform);
            FitPairedQueueToHead(pair);

            if (pair.transform.childCount < 3)
            {
                Destroy(pair);
                continue;
            }

            GameObject attackIcon = pair.transform.GetChild(0).gameObject;
            GameObject counterIcon = pair.transform.GetChild(2).gameObject;

            ApplySkillIcon(attackIcon, attackData);
            ApplySkillButtonData(attackIcon, attackData, attackIndex, false);
            ApplyQueueText(attackIcon, attackData, attackIndex);

            ApplySkillIcon(counterIcon, counterData);
            ApplySkillButtonData(counterIcon, counterData, localIndex, true);
            ApplyQueueText(counterIcon, counterData, localIndex);

            if (counterData.skill == null)
            {
                RawImage counterImage = counterIcon.GetComponent<RawImage>();

                if (counterImage != null && UIManager.Instance != null)
                    counterImage.texture = UIManager.Instance.emptyCounterSlotBackground;
            }

            SkillButton counterSkillButton = counterIcon.GetComponent<SkillButton>();

            if (counterSkillButton != null)
            {
                counterSkillButton.pairedAttackQueueData = attackData;
                counterSkillButton.pairedCounterSkill = counterData.skill;
                counterSkillButton.counterUser = counterUser;
                counterSkillButton.protectedTarget = characterManager;
                counterSkillButton.onRightClick = null;
            }

            Button attackButton = attackIcon.GetComponent<Button>();

            if (attackButton != null)
            {
                attackButton.onClick.RemoveAllListeners();

                if (attacker.character.IsMine)
                {
                    int capturedAttackIndex = attackIndex;
                    attackButton.onClick.AddListener(() =>
                    {
                        TooltipManager.Instance?.HideTooltip();

                        CharacterTargeting targeting = UIManager.Instance != null
                            ? UIManager.Instance.characterTargeting
                            : null;

                        if (targeting != null &&
                            targeting.TryReplaceQueuedAttack(attacker, capturedAttackIndex))
                        {
                            return;
                        }

                        attacker.combatHandler.RemoveSkillFromQueue(capturedAttackIndex);
                    });
                }
            }

            Button counterButton = counterIcon.GetComponent<Button>();

            if (counterButton != null)
            {
                counterButton.onClick.RemoveAllListeners();

                if (counterUser == characterManager && characterManager.character.IsMine)
                {
                    int capturedCounterIndex = localIndex;
                    counterButton.onClick.AddListener(() =>
                    {
                        TooltipManager.Instance?.HideTooltip();

                        CharacterTargeting targeting = UIManager.Instance != null
                            ? UIManager.Instance.characterTargeting
                            : null;

                        if (targeting != null && targeting.isDefenseSkillTargeting)
                        {
                            if (TurnManager.Instance != null &&
                                TurnManager.Instance.defenseCharacter == characterManager)
                            {
                                characterManager.combatHandler.SetOrResetCounterSkill(
                                    capturedCounterIndex,
                                    targeting.selectedSkill);
                                targeting.StopTargeting();
                            }

                            return;
                        }

                        characterManager.combatHandler.CycleBasicCounterSkill(
                            capturedCounterIndex);
                    });
                }
            }
        }

        return true;
    }

    private void FitPairedQueueToHead(GameObject pair)
    {
        if (pair == null || pair.transform.childCount == 0 || skillIconPrefab == null)
            return;

        RectTransform pairRect = pair.GetComponent<RectTransform>();
        RectTransform attackRect = pair.transform.GetChild(0) as RectTransform;
        RectTransform singleIconRect = skillIconPrefab.GetComponent<RectTransform>();

        if (pairRect == null ||
            attackRect == null ||
            singleIconRect == null ||
            attackRect.rect.width <= 0f)
        {
            return;
        }

        float iconScale = singleIconRect.localScale.x *
                          singleIconRect.rect.width /
                          attackRect.rect.width;
        pairRect.localScale = new Vector3(iconScale, iconScale, 1f);
        pairRect.sizeDelta = new Vector2(
            singleIconRect.rect.width,
            singleIconRect.rect.height * 2.2f);
    }

    public void ClearCounterSkillQueueUI()
    {
        if (skillQueuePanel != null)
            ClearChildren(skillQueuePanel.transform);
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
        if (skillIconInstance == null || data == null)
            return;

        RawImage rawImage = skillIconInstance.GetComponent<RawImage>();

        if (rawImage == null)
            return;

        if (data.skill == null)
        {
            rawImage.texture = null;
            return;
        }

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
            slider.SetValueWithoutNotify(Mathf.Clamp01(hpPercentage));
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
            slider.SetValueWithoutNotify(Mathf.Clamp01(staminaPercentage));
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
            slider.SetValueWithoutNotify(Mathf.Clamp01(mentalityPercentage));
    }

    public void UpdateStatusEffects()
    {
        foreach (GameObject icon in activeStatusIcons)
        {
            if (icon != null)
                Destroy(icon);
        }

        activeStatusIcons.Clear();

        if (characterManager == null || characterManager.character == null ||
            characterManager.character.StatusEffects == null ||
            statusEffectIconPrefab == null || statusEffectIconParent == null)
        {
            if (statusEffectPanel != null)
                statusEffectPanel.SetActive(false);

            return;
        }

        foreach (StatusEffectRuntimeData runtime in characterManager.character.StatusEffects)
        {
            if (runtime == null || runtime.stack <= 0)
                continue;

            StatusEffectDefinitionSO definition =
                GameDataRegistry.Instance?.GetStatusEffect(runtime.statusEffectId);

            if (definition == null)
                continue;

            GameObject iconInstance = Instantiate(
                statusEffectIconPrefab,
                statusEffectIconParent);

            Image iconImage = iconInstance.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.color = definition.icon != null
                    ? Color.white
                    : new Color(0.25f, 0.25f, 0.25f, 0.85f);
            }

            TextMeshProUGUI stackText =
                iconInstance.GetComponentInChildren<TextMeshProUGUI>(true);

            if (stackText != null)
                stackText.text = runtime.stack.ToString();

            IconTooltipHandler tooltip =
                iconInstance.GetComponent<IconTooltipHandler>();

            if (tooltip != null)
                tooltip.tooltipDescription = BuildStatusEffectTooltip(definition, runtime.stack);

            activeStatusIcons.Add(iconInstance);
        }

        if (statusEffectPanel != null)
            statusEffectPanel.SetActive(activeStatusIcons.Count > 0);
    }

    private string BuildStatusEffectTooltip(
        StatusEffectDefinitionSO definition,
        int stack)
    {
        string description = definition.description;

        if (string.IsNullOrWhiteSpace(description))
        {
            description = definition.effectType switch
            {
                StatusEffectType.PhysicalCounterPenalty =>
                    "물리 대응 성공 확률이 감소합니다.",
                StatusEffectType.MagicalCounterPenalty =>
                    "마법 대응 성공 확률이 감소합니다.",
                StatusEffectType.CancelNextCounter =>
                    "다음 대응 스킬이 취소됩니다.",
                _ => "상태이상 효과가 적용 중입니다."
            };
        }

        return $"<b>{definition.statusName}</b>\n{description}\n현재 중첩: {stack}";
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
