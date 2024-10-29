using UnityEngine;
using System.Collections.Generic;
using SoftKitty.InventoryEngine;
using UnityEngine.TextCore.Text;
using static UnityEditor.Progress;
using System.Security.Cryptography;
using UnityEngine.WSA;
using System.Collections;

public class CharacterSpawner : MonoBehaviour
{
    public GameObject frontCharacterObject; // 전열에 배치할 캐릭터 오브젝트 (방랑기사)
    public GameObject backCharacterObject1; // 후열에 배치할 첫 번째 캐릭터 오브젝트 (사냥꾼)
    public GameObject backCharacterObject2; // 후열에 배치할 두 번째 캐릭터 오브젝트 (마법사)

    public GameObject enemyFrontCharacterObject; // 적군 전열 캐릭터 오브젝트
    public GameObject enemyBackCharacterObject1; // 적군 후열 첫 번째 캐릭터 오브젝트
    public GameObject enemyBackCharacterObject2; // 적군 후열 두 번째 캐릭터 오브젝트

    private void Awake()
    {
        EquipmentDatabase.InitializeDatabase();

        // 아군 캐릭터 데이터 초기화
        InitializeCharacter(frontCharacterObject, CharacterOrigin.GetOriginData()["방랑기사"], true);
        InitializeCharacter(backCharacterObject1, CharacterOrigin.GetOriginData()["사냥꾼"], true);
        InitializeCharacter(backCharacterObject2, CharacterOrigin.GetOriginData()["마법사"], true);

        // 적군 캐릭터 데이터 초기화
        InitializeCharacter(enemyFrontCharacterObject, CharacterOrigin.GetOriginData()["방랑기사"], false);
        InitializeCharacter(enemyBackCharacterObject1, CharacterOrigin.GetOriginData()["사냥꾼"], false);
        InitializeCharacter(enemyBackCharacterObject2, CharacterOrigin.GetOriginData()["마법사"], false);
    }

    private string GetTagFromEquipmentType(EquipmentType equipType)
    {
        switch (equipType)
        {
            case EquipmentType.Helmet: return "Helmet";
            case EquipmentType.Armor: return "Torso";
            case EquipmentType.Gloves: return "Gauntlet";
            case EquipmentType.Shoes: return "Boots";
            case EquipmentType.Ring: return "Ring";
            case EquipmentType.Necklace: return "Necklace";
            case EquipmentType.Cape: return "Cape";
            case EquipmentType.SubWeapon: return "OffHand";
            case EquipmentType.Weapon: return "MainHand";
            default: return null;
        }
    }

    private void InitializeCharacter(GameObject characterObject, CharacterData characterData, bool isMine)
    {
        if (characterObject == null)
        {
            Debug.LogError("Character object is missing for " + characterData.Name);
            return;
        }

        CharacterManager characterManager = characterObject.GetComponent<CharacterManager>();

        if (characterManager != null)
        {
            characterManager.InitializeCharacter(DeepCopy.DeepCopyCharacter(characterData));
            characterObject.GetComponent<CharacterCustomization>().UpdateEquipmentAppearance(characterManager.character);
            characterManager.character.IsMine = isMine;
            characterManager.character.IsAlive = true;

            if (characterManager.character.originName == "마법사") 
            {
                characterManager.character.Traits[0].ApplyTrait(characterManager);
            }


            List<Equipment> equipmentList = (List<Equipment>)characterManager.character.GetEquipments();

            InventoryHolder[] inventoryHolders = characterObject.GetComponents<InventoryHolder>();
            InventoryHolder inventoryHolder = null;
            InventoryHolder equipmentHolder = null;


            if (inventoryHolders[0].Type == InventoryHolder.HolderType.PlayerEquipment)
            {
                equipmentHolder = inventoryHolders[0];
            }
            else
            {
                inventoryHolder = inventoryHolders[0];
            }

            if (inventoryHolders[1].Type == InventoryHolder.HolderType.PlayerEquipment)
            {
                equipmentHolder = inventoryHolders[1];
            }
            else
            {
                inventoryHolder = inventoryHolders[1];
            }

            // 각 장비를 InventoryHolder의 Stacks에 추가
            foreach (Equipment equipment in equipmentList)
            {
                if (equipment != null)
                {
                    equipmentHolder.AddItem(equipment, 1);
                    // 능력치에 장비 효과 적용
                    equipment.Equip(characterManager.character);
                }
            }

            characterManager.character.UpdateFinalStats();
            characterManager.character.FinalStats.CurrentHp = characterManager.character.FinalStats.MaxHp;

            if (isMine)
            {
                characterManager.character.Name = "Test_Ally" + characterManager.character.originName;
            }
            else
            {
                characterManager.character.Name = "Test_Enemy" + characterManager.character.originName;
            }
        }
        else
        {
            Debug.LogError("CharacterManager component is missing on the character object.");
        }
    }

}
