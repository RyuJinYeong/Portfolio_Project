using UnityEngine;
using UnityEngine.EventSystems;

public class IconTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string tooltipDescription;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null) return;
        TooltipManager.Instance.ShowTooltip(tooltipDescription, Input.mousePosition);
        TooltipManager.Instance.hoveredBattleIcon = gameObject;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance?.hoveredBattleIcon == gameObject)
            TooltipManager.Instance.HideTooltip();
    }

    private void OnDisable()
    {
        if (TooltipManager.Instance?.hoveredBattleIcon == gameObject)
            TooltipManager.Instance.HideTooltip();
    }
}
