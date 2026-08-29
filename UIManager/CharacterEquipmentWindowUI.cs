using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEquipmentWindowUI : MonoBehaviour
{
    [Serializable]
    public class EquipmentSlotBinding
    {
        public EquipmentType equipmentType;
        [Min(1)]
        public int slotIndex = 1;
        public InventoryItemSlotUI view;
    }

    public TMP_Text characterNameText;
    public TMP_Text characterLevelText;
    public Text legacyCharacterNameText;
    public Text legacyCharacterLevelText;
    public Button closeButton;
    public SharedInventoryType returnInventoryType = SharedInventoryType.ExpeditionStorage;
    public List<EquipmentSlotBinding> slots = new();

    private CharacterManager characterManager;

    private void OnEnable()
    {
        SharedInventoryUtility.EquipmentChanged += OnEquipmentChanged;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Refresh();
    }

    private void OnDisable()
    {
        SharedInventoryUtility.EquipmentChanged -= OnEquipmentChanged;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open(
        CharacterManager manager,
        SharedInventoryType inventoryType = SharedInventoryType.ExpeditionStorage)
    {
        characterManager = manager;
        returnInventoryType = inventoryType;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();

        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        CharacterData character = characterManager != null
            ? characterManager.character
            : null;

        if (characterNameText != null)
            characterNameText.text = character != null ? character.Name : "";

        if (legacyCharacterNameText != null)
            legacyCharacterNameText.text = character != null ? character.Name : "";

        if (characterLevelText != null)
            characterLevelText.text = character != null ? $"Lv. {character.Level}" : "";

        if (legacyCharacterLevelText != null)
            legacyCharacterLevelText.text = character != null ? $"Lv. {character.Level}" : "";

        foreach (EquipmentSlotBinding binding in slots)
        {
            if (binding == null || binding.view == null)
                continue;

            InventorySlotData slot = character != null
                ? SharedInventoryUtility.GetEquippedSlot(
                    character.EquipmentSlots,
                    binding.equipmentType,
                    binding.slotIndex)
                : null;

            EquipmentSlotBinding capturedBinding = binding;
            binding.view.Bind(
                slot,
                (_, button) => OnSlotClicked(capturedBinding, button));
        }
    }

    private void OnSlotClicked(EquipmentSlotBinding binding, int button)
    {
        if (button != (int)UnityEngine.EventSystems.PointerEventData.InputButton.Right ||
            characterManager == null)
        {
            return;
        }

        PlayerData playerData = PlayerManager.Instance.GetCurrentPlayerData();
        List<InventorySlotData> storage =
            SharedInventoryUtility.GetStorage(playerData, returnInventoryType);

        if (SharedInventoryUtility.UnequipToStorage(
                characterManager,
                storage,
                binding.equipmentType,
                binding.slotIndex))
        {
            SharedInventoryUtility.SaveChanges(characterManager);
        }
    }

    private void OnEquipmentChanged(CharacterManager manager)
    {
        if (manager == characterManager)
            Refresh();
    }
}
