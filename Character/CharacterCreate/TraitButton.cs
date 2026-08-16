using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitButton : MonoBehaviour
{
    public TraitDefinitionSO trait;

    private TraitSelectionUI selectionUI;
    private bool isSelected;
    private int traitCost;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public Button button;

    public void Initialize(
        TraitDefinitionSO trait,
        TraitSelectionUI selectionUI,
        bool isSelected,
        int traitCost)
    {
        this.trait = trait;
        this.selectionUI = selectionUI;
        this.isSelected = isSelected;
        this.traitCost = traitCost;

        if (nameText != null)
            nameText.text = trait != null ? trait.traitName : "";

        if (costText != null)
            costText.text = traitCost.ToString();

        if (descriptionText != null)
            descriptionText.text = trait != null ? trait.description : "";

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (trait == null || selectionUI == null)
            return;

        if (isSelected)
            selectionUI.MoveTraitToLeft(trait);
        else
            selectionUI.MoveTraitToRight(trait);
    }
}