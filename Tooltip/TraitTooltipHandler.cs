using UnityEngine;
using UnityEngine.EventSystems;

public class TraitTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TraitDefinitionSO trait;

    public void Bind(TraitDefinitionSO trait)
    {
        this.trait = trait;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null && trait != null)
            TooltipManager.Instance.ShowTraitTooltip(trait, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();
    }
}
