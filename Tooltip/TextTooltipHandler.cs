using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI textComponent;
    private string currentTooltipText = "";
    private Camera cam;
    private bool isMouseOver = false;

    private void Awake()
    {
        textComponent = this.gameObject.transform.GetComponent<TextMeshProUGUI>();
        cam = textComponent.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
        UpdateTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
        TooltipManager.Instance.HideTooltip();
        currentTooltipText = "";
    }

    private void Update()
    {
        if (isMouseOver)
        {
            UpdateTooltip();
        }
    }

    private void UpdateTooltip()
    {
        Vector2 localMousePosition = textComponent.rectTransform.InverseTransformPoint(Input.mousePosition);
        int charIndex = TMP_TextUtilities.GetCursorIndexFromPosition(textComponent, localMousePosition, cam);

        if (charIndex != -1 && charIndex < textComponent.text.Length)
        {
            string word = GetWordFromIndex(textComponent.text, charIndex);

            if (TooltipManager.Instance.keywordTooltips.TryGetValue(word, out string description))
            {
                if (description != currentTooltipText)
                {
                    TooltipManager.Instance.ShowTooltip(description, Input.mousePosition);
                    currentTooltipText = description;
                }
            }
        }
    }

    private string GetWordFromIndex(string text, int index)
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

        string potentialWord = text.Substring(start, end - start);

        // 공백 포함된 여러 단어로 구성된 키워드 체크
        foreach (var keyword in TooltipManager.Instance.keywordTooltips.Keys)
        {
            if (text.Contains(keyword))
            {
                int keywordStart = text.IndexOf(keyword);
                int keywordEnd = keywordStart + keyword.Length;

                if (index >= keywordStart && index <= keywordEnd)
                {
                    return keyword;
                }
            }
        }

        return potentialWord;
    }

    private bool IsSeparator(char c)
    {
        return char.IsWhiteSpace(c) || char.IsPunctuation(c);
    }
}
