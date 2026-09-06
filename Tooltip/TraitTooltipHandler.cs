using UnityEngine;
using UnityEngine.EventSystems;

public class TraitTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TraitDefinitionSO trait;
    private TraitRuntimeData runtime;

    public void Bind(TraitDefinitionSO trait)
    {
        this.trait = trait;
        runtime = null;
    }

    public void Bind(TraitDefinitionSO trait, TraitRuntimeData runtime)
    {
        this.trait = trait;
        this.runtime = runtime;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null && trait != null)
            TooltipManager.Instance.ShowTraitTooltip(trait, runtime, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();
    }
}
