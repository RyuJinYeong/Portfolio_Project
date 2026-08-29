using UnityEngine;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public SkillDefinitionSO skill;
    public SkillQueueData queueData;

    public int queueIndex = -1;
    public bool isCounterSkill;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;

        if (queueData != null && queueData.skill != null)
        {
            TooltipManager.Instance.ShowTooltip(queueData, Input.mousePosition);
            return;
        }

        if (skill != null)
        {
            TooltipManager.Instance.ShowTooltip(skill, Input.mousePosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;

        TooltipManager.Instance.HideTooltip();
    }
}
