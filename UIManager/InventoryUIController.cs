using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public static InventoryUIController Instance { get; private set; }

    public SharedInventoryWindowUI companyStorageWindow;
    public SharedInventoryWindowUI expeditionInventoryWindow;
    public CharacterEquipmentWindowUI equipmentWindow;
    public CharacterSkillWindowUI skillWindow;

    public CharacterManager SelectedCharacter { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetSelectedCharacter(CharacterManager characterManager)
    {
        SelectedCharacter = characterManager;

        if (companyStorageWindow != null)
            companyStorageWindow.SetCharacter(characterManager);

        if (expeditionInventoryWindow != null)
            expeditionInventoryWindow.SetCharacter(characterManager);
    }

    public void OpenCompanyStorage()
    {
        if (companyStorageWindow != null)
            companyStorageWindow.Open(SelectedCharacter);
    }

    public void OpenExpeditionInventory()
    {
        if (expeditionInventoryWindow != null)
            expeditionInventoryWindow.Open(SelectedCharacter);
    }

    public void OpenEquipment()
    {
        if (equipmentWindow != null && SelectedCharacter != null)
        {
            equipmentWindow.Open(
                SelectedCharacter,
                GetAccessibleInventoryType());
        }
    }

    public void OpenSkills()
    {
        if (skillWindow != null && SelectedCharacter != null)
            skillWindow.Open(SelectedCharacter);
    }

    public void CloseAll()
    {
        if (companyStorageWindow != null)
            companyStorageWindow.Close();

        if (expeditionInventoryWindow != null)
            expeditionInventoryWindow.Close();

        if (equipmentWindow != null)
            equipmentWindow.Close();

        if (skillWindow != null)
            skillWindow.Close();
    }

    private SharedInventoryType GetAccessibleInventoryType()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        return playerData != null && playerData.currentStage == "Town"
            ? SharedInventoryType.CompanyStorage
            : SharedInventoryType.ExpeditionStorage;
    }
}
