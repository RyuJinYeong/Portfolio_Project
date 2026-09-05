using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyFormationRosterItemUI : MonoBehaviour
{
    public RawImage portrait;
    public TMP_Text characterText;
    public TMP_Text sortiePayText;
    public Image selectedFrame;
    public Button selectButton;

    private CharacterData character;
    private Action<CharacterData> selected;

    public CharacterData Character => character;

    public void Bind(
        CharacterData data,
        bool isSelected,
        Action<CharacterData> onSelected)
    {
        character = data;
        selected = onSelected;

        if (portrait != null)
        {
            portrait.texture = data != null ? data.Portrait : null;
            portrait.enabled = data != null && data.Portrait != null;
        }

        if (characterText != null)
        {
            CharacterStats stats = data != null ? data.FinalStats : null;
            characterText.text = data != null
                ? $"Lv. {data.Level}  {data.Name}\n" +
                  $"{data.originName}\n" +
                  $"체력 {data.CurrentHp:N0} / {(stats != null ? stats.MaxHp : 0):N0}"
                : "";
        }

        if (sortiePayText != null)
        {
            sortiePayText.text = data != null
                ? $"출전 수당  {MercenaryGenerator.CalculateSortiePay(data):N0} G"
                : "";
        }

        if (selectedFrame != null)
            selectedFrame.enabled = isSelected;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(Select);
            selectButton.onClick.AddListener(Select);
        }
    }

    private void OnDisable()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(Select);
    }

    private void Select()
    {
        selected?.Invoke(character);
    }
}
