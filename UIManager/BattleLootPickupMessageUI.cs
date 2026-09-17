using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleLootPickupMessageUI : MonoBehaviour
{
    public GameObject messageBar;
    public RectTransform itemRoot;
    public GameObject itemPrefab;
    public float popDuration = 0.18f;
    public float displayDuration = 1.5f;
    public float fadeDuration = 0.35f;

    private readonly List<ItemEntry> displayedItems = new();

    private sealed class ItemEntry
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
    }

    private void Awake()
    {
        if (messageBar != null)
            messageBar.SetActive(false);

        if (itemPrefab != null)
            itemPrefab.SetActive(false);
    }

    public void ShowItem(InventorySlotData slot)
    {
        ItemDefinitionSO item = slot != null && GameDataRegistry.Instance != null
            ? GameDataRegistry.Instance.GetItem(slot.itemUid)
            : null;

        if (slot == null || item == null || itemRoot == null || itemPrefab == null)
            return;

        if (messageBar != null)
            messageBar.SetActive(true);

        GameObject itemObject = Instantiate(itemPrefab, itemRoot);
        itemObject.name = $"LootPickup_{slot.itemUid}";

        RawImage icon = itemObject.transform.Find("Mask/Icon")?.GetComponent<RawImage>();
        if (icon != null)
        {
            icon.texture = item.icon;
            icon.enabled = item.icon != null;
        }

        Text numberText = itemObject.transform.Find("Number")?.GetComponent<Text>();
        if (numberText != null)
        {
            numberText.gameObject.SetActive(true);
            numberText.text = Mathf.Max(1, slot.count).ToString();
        }

        CanvasGroup canvasGroup = itemObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = itemObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform rectTransform = itemObject.transform as RectTransform;
        if (rectTransform != null)
            rectTransform.localScale = Vector3.one * 0.65f;

        ItemEntry entry = new ItemEntry
        {
            gameObject = itemObject,
            rectTransform = rectTransform,
            canvasGroup = canvasGroup
        };

        displayedItems.Add(entry);
        itemObject.SetActive(true);
        transform.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemRoot);
        StartCoroutine(AnimateItem(entry));
    }

    private IEnumerator AnimateItem(ItemEntry entry)
    {
        float popTime = Mathf.Max(0.01f, popDuration);
        float elapsed = 0f;

        while (entry.gameObject != null && elapsed < popTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / popTime);
            float scale = Mathf.Lerp(0.65f, 1.08f, EaseOutBack(progress));

            if (entry.rectTransform != null)
                entry.rectTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (entry.rectTransform != null)
            entry.rectTransform.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, displayDuration));

        float fadeTime = Mathf.Max(0.01f, fadeDuration);
        elapsed = 0f;
        Vector2 startPosition = entry.rectTransform != null
            ? entry.rectTransform.anchoredPosition
            : Vector2.zero;

        while (entry.gameObject != null && elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeTime);
            entry.canvasGroup.alpha = 1f - progress;

            if (entry.rectTransform != null)
            {
                entry.rectTransform.anchoredPosition =
                    startPosition + Vector2.up * Mathf.Lerp(0f, 18f, progress);
                entry.rectTransform.localScale =
                    Vector3.one * Mathf.Lerp(1f, 0.85f, progress);
            }

            yield return null;
        }

        displayedItems.Remove(entry);

        if (entry.gameObject != null)
            Destroy(entry.gameObject);

        LayoutRebuilder.ForceRebuildLayoutImmediate(itemRoot);

        if (displayedItems.Count == 0)
        {
            if (messageBar != null)
                messageBar.SetActive(false);

            Destroy(gameObject);
        }
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.70158f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted +
               overshoot * shifted * shifted;
    }
}
