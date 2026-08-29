using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TraitButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TraitDefinitionSO trait;

    private TraitSelectionUI selectionUI;
    private bool isSelected;
    private int traitCost;

    [FormerlySerializedAs("buttonText")]
    public TextMeshProUGUI nameText;
    public RawImage iconImage;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI polarityText;
    public Button button;

    [Header("Polarity Colors")]
    public Color positiveTextColor;
    public Color negativeTextColor;
    public Color mixedTextColor;

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

        if (iconImage != null)
        {
            iconImage.texture = trait != null ? trait.icon : null;
            iconImage.enabled = trait != null && trait.icon != null;
        }

        if (trait != null)
        {
            Color traitTextColor = trait.polarity switch
            {
                TraitPolarity.Positive => positiveTextColor,
                TraitPolarity.Negative => negativeTextColor,
                TraitPolarity.Mixed => mixedTextColor,
                _ => mixedTextColor
            };

            string pointChange = traitCost > 0
                ? $"-{traitCost}"
                : traitCost < 0
                    ? $"+{-traitCost}"
                    : "0";

            if (nameText != null)
            {
                nameText.text = gradeText == null && costText == null && polarityText == null
                    ? $"{trait.defaultAcquireGrade} {trait.traitName} ({pointChange})"
                    : trait.traitName;
                nameText.color = traitTextColor;
            }

            if (gradeText != null)
            {
                gradeText.text = trait.defaultAcquireGrade.ToString();
                switch (trait.defaultAcquireGrade)
                {
                    case TraitGrade.S:
                        gradeText.color = new Color(1f, 0.72f, 0.25f);
                        break;

                    case TraitGrade.A:
                        gradeText.color = new Color(0.76f, 0.48f, 1f);
                        break;

                    case TraitGrade.B:
                        gradeText.color = new Color(0.38f, 0.68f, 1f);
                        break;

                    case TraitGrade.C:
                        gradeText.color = new Color(0.48f, 0.8f, 0.4f);
                        break;

                    case TraitGrade.D:
                        gradeText.color = new Color(0.78f, 0.78f, 0.78f);
                        break;

                    case TraitGrade.E:
                        gradeText.color = new Color(0.72f, 0.52f, 0.34f);
                        break;

                    case TraitGrade.F:
                        gradeText.color = new Color(0.5f, 0.5f, 0.5f);
                        break;
                }
            }

            if (costText != null)
            {
                costText.text = pointChange;
                costText.color = traitCost > 0
                    ? new Color(0.95f, 0.38f, 0.34f)
                    : traitCost < 0
                        ? new Color(0.62f, 0.88f, 0.34f)
                        : Color.white;
            }

            if (polarityText != null)
            {
                switch (trait.polarity)
                {
                    case TraitPolarity.Positive:
                        polarityText.text = "● 긍정";
                        break;

                    case TraitPolarity.Negative:
                        polarityText.text = "● 부정";
                        break;

                    case TraitPolarity.Mixed:
                        polarityText.text = "● 혼합";
                        break;
                }

                polarityText.color = traitTextColor;
            }
        }
        else
        {
            if (nameText != null)
                nameText.text = "";

            if (gradeText != null)
                gradeText.text = "";

            if (costText != null)
                costText.text = "";

            if (polarityText != null)
                polarityText.text = "";
        }

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
