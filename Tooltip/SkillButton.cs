using UnityEngine.EventSystems;
using UnityEngine;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public SkillBase skill;  // 연결된 스킬 객체

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Instance.ShowTooltip(skill, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();    
    }
}
