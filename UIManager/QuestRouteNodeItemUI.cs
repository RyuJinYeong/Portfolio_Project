using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestRouteNodeItemUI : MonoBehaviour
{
    public Button button;
    public Image icon;
    public Image selectionFrame;
    public TMP_Text label;

    [Header("Node Icons")]
    public Sprite startIcon;
    public Sprite battleIcon;
    public Sprite eliteIcon;
    public Sprite randomEncounterIcon;
    public Sprite restIcon;
    public Sprite unknownIcon;
    public GameObject bossIconPrefab;

    private QuestRouteNode node;
    private Action<QuestRouteNode> onSelected;
    private GameObject bossIconInstance;

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(Select);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Select);
    }

    public void Bind(
        QuestRouteNode routeNode,
        bool detected,
        bool selectable,
        bool current,
        Action<QuestRouteNode> selected)
    {
        node = routeNode;
        onSelected = selected;

        if (button != null)
            button.interactable = selectable;

        if (selectionFrame != null)
            selectionFrame.gameObject.SetActive(current || selectable);

        if (bossIconInstance != null)
            Destroy(bossIconInstance);

        bool showBossIcon = detected &&
                            routeNode.type == QuestRouteNodeType.Boss &&
                            bossIconPrefab != null;

        if (showBossIcon)
        {
            bossIconInstance = Instantiate(bossIconPrefab, transform);
            bossIconInstance.transform.SetAsLastSibling();

            foreach (Image bossImage in bossIconInstance.GetComponentsInChildren<Image>(true))
                bossImage.raycastTarget = false;

            if (bossIconInstance.transform is RectTransform bossRect)
            {
                bossRect.anchorMin = new Vector2(0.5f, 0.5f);
                bossRect.anchorMax = new Vector2(0.5f, 0.5f);
                bossRect.pivot = new Vector2(0.5f, 0.5f);
                bossRect.anchoredPosition = Vector2.zero;
                bossRect.localScale = Vector3.one;
            }
        }

        if (icon != null)
        {
            icon.gameObject.SetActive(!showBossIcon);
            icon.sprite = detected ? GetNodeIcon(routeNode.type) : unknownIcon;
            icon.color = Color.black;
            icon.preserveAspect = true;

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(60f, 60f);
        }

        if (label != null)
            label.gameObject.SetActive(false);

        if (button != null && button.targetGraphic is Image background)
        {
            background.color = routeNode.cleared
                ? new Color(0.7f, 0.78f, 0.62f, 0.98f)
                : selectable
                    ? new Color(0.92f, 0.76f, 0.43f, 0.98f)
                    : detected
                        ? new Color(0.78f, 0.7f, 0.57f, 0.96f)
                        : new Color(0.55f, 0.51f, 0.45f, 0.92f);
        }
    }

    private Sprite GetNodeIcon(QuestRouteNodeType type)
    {
        return type switch
        {
            QuestRouteNodeType.Start => startIcon,
            QuestRouteNodeType.Elite => eliteIcon,
            QuestRouteNodeType.RandomEncounter => randomEncounterIcon,
            QuestRouteNodeType.Rest => restIcon,
            QuestRouteNodeType.Boss => unknownIcon,
            _ => battleIcon
        };
    }

    private void Select()
    {
        if (node != null)
            onSelected?.Invoke(node);
    }
}
