using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentInfoHoverTooltip : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private string statKeyword;
    private TraitDefinitionSO trait;

    public void BindStat(string keyword)
    {
        statKeyword = keyword;
        trait = null;
    }

    public void BindTrait(TraitDefinitionSO traitDefinition)
    {
        trait = traitDefinition;
        statKeyword = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;

        if (trait != null)
            TooltipManager.Instance.ShowTraitTooltip(trait, Input.mousePosition);
        else if (!string.IsNullOrWhiteSpace(statKeyword))
            TooltipManager.Instance.ShowKeywordTooltip(statKeyword, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();
    }
}
