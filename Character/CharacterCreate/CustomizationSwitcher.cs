using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using P09.Modular.Humanoid.Data;

public class CustomizationSwitcher : MonoBehaviour
{
    [Header("P09 Data")]
    [SerializeField] private EditPartDataContainer container;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ValueText;
    [SerializeField] private Image valueImage;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    private int currentIndex;
    private Action<EditPartType, int> onChanged;

    public EditPartType Type => container != null ? container.Type : EditPartType.None;
    public EditPartDataContainer Container => container;

    public void Init(Action<EditPartType, int> onChanged)
    {
        this.onChanged = onChanged;

        if (leftButton != null)
        {
            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(Previous);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(Next);
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, GetCount() - 1);
        UpdateView();
    }

    public void SetContentId(int contentId, bool notify = false)
    {
        var list = GetList();
        if (list == null || list.Count == 0) return;

        int index = list.FindIndex(x => x.ContentId == contentId);
        currentIndex = index >= 0 ? index : 0;

        UpdateView();

        if (notify)
            onChanged?.Invoke(Type, list[currentIndex].ContentId);
    }

    public int GetCurrentContentId()
    {
        var list = GetList();
        if (list == null || list.Count == 0) return 0;

        currentIndex = Mathf.Clamp(currentIndex, 0, list.Count - 1);
        return list[currentIndex].ContentId;
    }

    public int GetRandomContentId()
    {
        var list = GetList();
        if (list == null || list.Count == 0) return 0;

        return list[UnityEngine.Random.Range(0, list.Count)].ContentId;
    }

    private void Previous()
    {
        var list = GetList();
        if (list == null || list.Count == 0) return;

        currentIndex = (currentIndex - 1 + list.Count) % list.Count;
        UpdateView();

        onChanged?.Invoke(Type, list[currentIndex].ContentId);
    }

    private void Next()
    {
        var list = GetList();
        if (list == null || list.Count == 0) return;

        currentIndex = (currentIndex + 1) % list.Count;
        UpdateView();

        onChanged?.Invoke(Type, list[currentIndex].ContentId);
    }

    private void UpdateView()
    {
        var list = GetList();
        if (list == null || list.Count == 0) return;

        currentIndex = Mathf.Clamp(currentIndex, 0, list.Count - 1);
        var data = list[currentIndex];

        if (ValueText != null)
            ValueText.text = data.DisplayName;

        if (valueImage != null)
            valueImage.sprite = GetIcon(data);
    }

    private List<IEditPartData> GetList()
    {
        if (container == null) return null;
        return container.PartDataList;
    }

    private int GetCount()
    {
        var list = GetList();
        return list != null ? list.Count : 0;
    }

    private Sprite GetIcon(IEditPartData data)
    {
        if (data is ColorEditPartData colorData)
            return colorData.Icon;

        if (data is RendererEditPartData rendererData)
            return rendererData.Icon;

        return null;
    }
}