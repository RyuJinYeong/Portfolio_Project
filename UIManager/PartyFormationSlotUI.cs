using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyFormationSlotUI : MonoBehaviour
{
    public RawImage portrait;
    public TMP_Text characterText;
    public TMP_Text emptyText;
    public TMP_Text rowText;
    public Button rowButton;
    public Button equipmentButton;
    public Button removeButton;

    private int slotIndex;
    private Action<int> removeRequested;
    private Action<int> rowToggleRequested;
    private Action<int> equipmentRequested;

    public void Bind(
        int index,
        CharacterData character,
        bool isFront,
        Action<int> onRemove,
        Action<int> onToggleRow,
        Action<int> onOpenEquipment)
    {
        slotIndex = index;
        removeRequested = onRemove;
        rowToggleRequested = onToggleRow;
        equipmentRequested = onOpenEquipment;

        bool occupied = character != null;

        if (portrait != null)
        {
            portrait.texture = occupied ? character.Portrait : null;
            portrait.enabled = occupied && character.Portrait != null;
        }

        if (characterText != null)
        {
            characterText.gameObject.SetActive(occupied);
            characterText.text = occupied
                ? $"Lv. {character.Level}  {character.Name}\n" +
                  $"{character.originName}\n" +
                  $"수당 {MercenaryGenerator.CalculateSortiePay(character):N0} G"
                : "";
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(!occupied);
            emptyText.text = $"{index + 1}\n+\n영웅을 배치하세요";
        }

        if (rowText != null)
            rowText.text = isFront ? "전열" : "후열";

        if (rowButton != null)
            rowButton.gameObject.SetActive(occupied);

        if (equipmentButton != null)
            equipmentButton.gameObject.SetActive(occupied);

        if (removeButton != null)
            removeButton.gameObject.SetActive(occupied);

        WireButtons();
    }

    private void OnDisable()
    {
        UnwireButtons();
    }

    private void WireButtons()
    {
        UnwireButtons();

        if (rowButton != null)
            rowButton.onClick.AddListener(ToggleRow);

        if (equipmentButton != null)
            equipmentButton.onClick.AddListener(OpenEquipment);

        if (removeButton != null)
            removeButton.onClick.AddListener(Remove);
    }

    private void UnwireButtons()
    {
        if (rowButton != null)
            rowButton.onClick.RemoveListener(ToggleRow);

        if (equipmentButton != null)
            equipmentButton.onClick.RemoveListener(OpenEquipment);

        if (removeButton != null)
            removeButton.onClick.RemoveListener(Remove);
    }

    private void ToggleRow()
    {
        rowToggleRequested?.Invoke(slotIndex);
    }

    private void Remove()
    {
        removeRequested?.Invoke(slotIndex);
    }

    private void OpenEquipment()
    {
        equipmentRequested?.Invoke(slotIndex);
    }
}
