using UnityEngine.EventSystems;
using UnityEngine;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public SkillBase skill;  // 연결된 스킬 객체
    public int queueIndex = -1; // 스킬 큐에서의 인덱스 (기본값은 -1로 비활성화 의미)

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Instance.ShowTooltip(skill, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();    
    }
}
