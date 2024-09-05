using UnityEngine;
using UnityEngine.EventSystems;

public class IconTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string tooltipDescription;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Instance.ShowTooltip(tooltipDescription, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }
}
