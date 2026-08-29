using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillListItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public RawImage icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Text legacyNameText;
    public Text legacyCostText;
    public GameObject quickSlotIcon;

    public int index;
    public SkillDefinitionSO skill;
    public SkillRuntimeData runtime;

    Action<int, int> clickCallback;

    public void Bind(
        int index,
        SkillDefinitionSO skill,
        SkillRuntimeData runtime,
        Action<int, int> clickCallback)
    {
        this.index = index;
        this.skill = skill;
        this.runtime = runtime;
        this.clickCallback = clickCallback;

        if (icon == null)
            icon = GetComponentInChildren<RawImage>();

        if (icon != null)
        {
            icon.texture = skill != null ? skill.icon : null;
            icon.enabled = skill != null && skill.icon != null;
            icon.color = runtime == null || runtime.canUse
                ? Color.white
                : new Color(0.35f, 0.35f, 0.35f, 0.8f);
        }

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();

        if (nameText == null && texts.Length > 0)
            nameText = texts[0];

        if (costText == null && texts.Length > 1)
            costText = texts[1];

        if (nameText != null)
            nameText.text = skill != null ? skill.skillName : "";

        if (legacyNameText != null)
            legacyNameText.text = skill != null ? skill.skillName : "";

        if (costText != null)
            costText.text = GetCostText(skill);

        if (legacyCostText != null)
            legacyCostText.text = GetCostText(skill);

        if (quickSlotIcon != null)
            quickSlotIcon.SetActive(runtime != null && runtime.quickSlot);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public bool IsNameMatch(string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return true;

        if (skill == null)
            return false;

        return skill.skillName.Contains(searchText);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickCallback == null)
            return;

        clickCallback.Invoke(index, (int)eventData.button);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null && skill != null)
            TooltipManager.Instance.ShowTooltip(skill, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();
    }

    string GetCostText(SkillDefinitionSO skill)
    {
        if (skill == null)
            return "";

        if (skill.type == SkillType.Physical)
            return $"{skill.staminaCost}";

        if (skill.type == SkillType.Magical)
            return $"{skill.mentalCost}";

        return "";
    }
}
