using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Root rootObject; // Root 클래스를 상속받은 모든 객체에 대해 참조
    public TextMeshProUGUI textComponent;
    public bool isTextTooltip = false; // 텍스트 기반인지 아이콘 기반인지 구분

    private void Awake()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rootObject != null)
        {
            // 아이콘 기반 툴팁
            if (!isTextTooltip)
            {
                TooltipManager.Instance.ShowTooltip(rootObject.Description, Input.mousePosition);
            }
            // 텍스트 기반 툴팁
            else
            {
                string keyword = GetWordFromText(textComponent.text, TMP_TextUtilities.GetCursorIndexFromPosition(textComponent, Input.mousePosition, null));
                if (!string.IsNullOrEmpty(keyword))
                {
                    TooltipManager.Instance.ShowTooltip(TooltipManager.Instance.keywordTooltips[keyword], Input.mousePosition);
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }

    // 텍스트에서 특정 단어 추출
    private string GetWordFromText(string text, int index)
    {
        if (index < 0 || index >= text.Length)
            return string.Empty;

        int start = index;
        while (start > 0 && !IsSeparator(text[start - 1]))
        {
            start--;
        }

        int end = index;
        while (end < text.Length && !IsSeparator(text[end]))
        {
            end++;
        }

        return text.Substring(start, end - start);
    }

    private bool IsSeparator(char c)
    {
        return char.IsWhiteSpace(c) || char.IsPunctuation(c);
    }
}
