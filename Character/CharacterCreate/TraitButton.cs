using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TraitButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TraitBase trait;
    public Button button;
    public TextMeshProUGUI buttonText;

    private TraitSelectionUI traitSelectionUI;
    private bool isAdded;

    public void Initialize(TraitBase trait, TraitSelectionUI ui, bool isAdded, int traitCost)
    {
        this.trait = trait;
        this.traitSelectionUI = ui;
        this.isAdded = isAdded;

        // Ç¥±â: "{Grade} {Name} ({+/-BaseCost})"
        int baseCost = TraitEconomy.BaseCostByGrade(trait.Grade);
        string sign = trait.Polarity == TraitPolarity.Negative ? "+" : (baseCost > 0 ? "-" : "");
        string grade = trait.Grade.ToString(); // F/E/D/C/B/A/S

        buttonText.text = $"{grade} {trait.Name} ({sign}{baseCost})";

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        if (isAdded)
        {
            traitSelectionUI.MoveTraitToLeft(trait);
        }
        else
        {
            traitSelectionUI.MoveTraitToRight(trait);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance.keywordTooltips.TryGetValue(trait.Name, out string description))
        {
            TooltipManager.Instance.ShowTooltip(description, Input.mousePosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }
}
