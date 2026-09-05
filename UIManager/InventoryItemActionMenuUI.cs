using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemActionMenuUI : MonoBehaviour
{
    public RectTransform actionPanel;
    public RectTransform targetPanel;
    public RectTransform targetContent;
    public Button useOrEquipButton;
    public TMP_Text useOrEquipButtonText;
    public Button sellButton;
    public Button discardButton;
    public Button dismissButton;
    public Button targetButtonTemplate;

    private readonly List<Button> targetButtons = new();
    private readonly List<CharacterManager> targets = new();
    private Action<CharacterManager> useOrEquipRequested;
    private Action sellRequested;
    private Action discardRequested;
    private Vector2 pointerLocalPosition;
    private bool actionPanelOpensRight;

    private void Awake()
    {
        if (dismissButton != null)
            dismissButton.onClick.AddListener(Close);

        if (useOrEquipButton != null)
            useOrEquipButton.onClick.AddListener(ShowTargetPanel);

        if (sellButton != null)
            sellButton.onClick.AddListener(Sell);

        if (discardButton != null)
            discardButton.onClick.AddListener(Discard);
    }

    public void Open(
        InventorySlotData slot,
        IReadOnlyList<CharacterManager> availableTargets,
        bool directTargetSelection,
        bool allowSell,
        Action<CharacterManager> onUseOrEquip,
        Action onSell,
        Action onDiscard)
    {
        ClearTargets();
        useOrEquipRequested = onUseOrEquip;
        sellRequested = onSell;
        discardRequested = onDiscard;

        if (availableTargets != null)
        {
            foreach (CharacterManager target in availableTargets)
            {
                if (target != null && target.character != null)
                    targets.Add(target);
            }
        }

        ItemDefinitionSO item = slot != null && GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(slot.itemUid)
            : null;
        bool canUseOrEquip = item is EquipmentDefinitionSO ||
                             item is ConsumableDefinitionSO;
        bool canSell = allowSell && item != null && item.tradeable;
        bool canDiscard = item != null && item.deletable;

        if (!directTargetSelection && !canUseOrEquip && !canSell && !canDiscard)
        {
            Close();
            return;
        }

        if (useOrEquipButton != null)
        {
            useOrEquipButton.gameObject.SetActive(canUseOrEquip);
            useOrEquipButton.interactable = canUseOrEquip && targets.Count > 0;
        }

        if (useOrEquipButtonText != null)
        {
            useOrEquipButtonText.text = item is EquipmentDefinitionSO
                ? "장착"
                : "사용";
        }

        if (sellButton != null)
            sellButton.gameObject.SetActive(canSell);

        if (discardButton != null)
            discardButton.gameObject.SetActive(canDiscard);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (actionPanel != null)
            actionPanel.gameObject.SetActive(!directTargetSelection);

        if (targetPanel != null)
            targetPanel.gameObject.SetActive(false);

        BuildTargetButtons(item is EquipmentDefinitionSO ? "장착" : "사용");
        Canvas.ForceUpdateCanvases();
        CachePointerPosition();

        if (directTargetSelection)
            ShowTargetPanelAtPointer();
        else
            PositionActionPanel();
    }

    public void Close()
    {
        ClearTargets();
        useOrEquipRequested = null;
        sellRequested = null;
        discardRequested = null;
        gameObject.SetActive(false);
    }

    private void BuildTargetButtons(string actionName)
    {
        if (targetButtonTemplate == null || targetContent == null)
            return;

        foreach (CharacterManager target in targets)
        {
            Button button = Instantiate(targetButtonTemplate, targetContent);
            button.gameObject.SetActive(true);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = $"{target.character.Name}에게 {actionName}";

            CharacterManager capturedTarget = target;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => UseOrEquip(capturedTarget));
            targetButtons.Add(button);
        }

        if (targetPanel != null)
        {
            float height = Mathf.Clamp(44f + targets.Count * 36f, 80f, 300f);
            targetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }

    private void ClearTargets()
    {
        foreach (Button button in targetButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        targetButtons.Clear();
        targets.Clear();
    }

    private void ShowTargetPanel()
    {
        if (targetPanel == null || targets.Count == 0)
            return;

        targetPanel.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        PositionTargetPanelBesideActions();
    }

    private void ShowTargetPanelAtPointer()
    {
        if (targetPanel == null || targets.Count == 0)
        {
            Close();
            return;
        }

        targetPanel.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        PositionPanelAtPointer(targetPanel, out _);
    }

    private void UseOrEquip(CharacterManager target)
    {
        Action<CharacterManager> callback = useOrEquipRequested;
        Close();
        callback?.Invoke(target);
    }

    private void Sell()
    {
        Action callback = sellRequested;
        Close();
        callback?.Invoke();
    }

    private void Discard()
    {
        Action callback = discardRequested;
        Close();
        callback?.Invoke();
    }

    private void CachePointerPosition()
    {
        RectTransform root = transform as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (root == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                root,
                Input.mousePosition,
                eventCamera,
                out pointerLocalPosition))
        {
            pointerLocalPosition = Vector2.zero;
        }
    }

    private void PositionActionPanel()
    {
        if (actionPanel == null)
            return;

        PositionPanelAtPointer(actionPanel, out actionPanelOpensRight);
    }

    private void PositionPanelAtPointer(RectTransform panel, out bool opensRight)
    {
        RectTransform root = transform as RectTransform;

        if (root == null || panel == null)
        {
            opensRight = true;
            return;
        }

        float width = panel.rect.width;
        float height = panel.rect.height;
        Rect bounds = root.rect;
        opensRight = pointerLocalPosition.x + width <= bounds.xMax;

        panel.pivot = new Vector2(opensRight ? 0f : 1f, 1f);

        float x = Mathf.Clamp(pointerLocalPosition.x, bounds.xMin, bounds.xMax);
        float y = pointerLocalPosition.y;

        if (y - height < bounds.yMin)
            y = Mathf.Min(bounds.yMax, bounds.yMin + height);
        else
            y = Mathf.Min(y, bounds.yMax);

        panel.anchoredPosition = new Vector2(x, y);
    }

    private void PositionTargetPanelBesideActions()
    {
        RectTransform root = transform as RectTransform;

        if (root == null || actionPanel == null || targetPanel == null)
            return;

        Rect bounds = root.rect;
        float actionWidth = actionPanel.rect.width;
        float targetWidth = targetPanel.rect.width;
        float actionLeft = actionPanelOpensRight
            ? pointerLocalPosition.x
            : pointerLocalPosition.x - actionWidth;
        float actionRight = actionLeft + actionWidth;
        bool placeRight = actionRight + targetWidth <= bounds.xMax;

        targetPanel.pivot = new Vector2(placeRight ? 0f : 1f, 1f);
        float x = placeRight ? actionRight : actionLeft;
        float y = actionPanel.anchoredPosition.y;

        if (y - targetPanel.rect.height < bounds.yMin)
            y = Mathf.Min(bounds.yMax, bounds.yMin + targetPanel.rect.height);

        targetPanel.anchoredPosition = new Vector2(x, y);
    }
}
