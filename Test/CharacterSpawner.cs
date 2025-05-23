using UnityEngine;
using System.Collections.Generic;
using SoftKitty.InventoryEngine;
using UnityEngine.TextCore.Text;
using System.Security.Cryptography;
using UnityEngine.WSA;
using System.Collections;

public class CharacterSpawner : MonoBehaviour
{
    public GameObject frontCharacterObject; // 전열에 배치할 캐릭터 오브젝트 (TestOrigin)
    public GameObject backCharacterObject1; // 후열에 배치할 첫 번째 캐릭터 오브젝트 (사냥꾼)
    public GameObject backCharacterObject2; // 후열에 배치할 두 번째 캐릭터 오브젝트 (마법사)

    public GameObject enemyFrontCharacterObject; // 적군 전열 캐릭터 오브젝트
    public GameObject enemyBackCharacterObject1; // 적군 후열 첫 번째 캐릭터 오브젝트
    public GameObject enemyBackCharacterObject2; // 적군 후열 두 번째 캐릭터 오브젝트

    private void Start()
    {
        // 아군 캐릭터 데이터 초기화
        InitializeCharacter(frontCharacterObject, CharacterOrigin.GetOriginData()["TestOrigin"], true);
        InitializeCharacter(backCharacterObject1, CharacterOrigin.GetOriginData()["사냥꾼"], true);
        InitializeCharacter(backCharacterObject2, CharacterOrigin.GetOriginData()["마법사"], true);

        // 적군 캐릭터 데이터 초기화
        InitializeCharacter(enemyFrontCharacterObject, CharacterOrigin.GetOriginData()["방랑기사"], false);
        InitializeCharacter(enemyBackCharacterObject1, CharacterOrigin.GetOriginData()["사냥꾼"], false);
        InitializeCharacter(enemyBackCharacterObject2, CharacterOrigin.GetOriginData()["마법사"], false);        
    }

    IEnumerator Delay(CharacterManager characterManager, GameObject characterObject)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // 애니메이션 반영을 위한 짧은 딜레이

        characterManager.character.Portrait = characterObject.GetComponent<CharacterCustomization>().CapturePortrait(); // 초상화 촬영        
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

            VerifyAndSetEquipmentIcons((List<Equipment>)characterManager.character.GetEquipments());

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
                    // 먼저 장비를 스택에 추가
                    var addResult = equipmentHolder.AddItem(equipment, 1); // AddItem의 반환값을 활용하여 추가 성공 여부 확인

                    // 변경된 장비 정보를 딕셔너리에 추가하여 ItemChanged 호출
                    Dictionary<Item, int> _changedItems = new Dictionary<Item, int>();
                    _changedItems.Add(equipment, 1);
                    equipmentHolder.ItemChanged(_changedItems);

                    // 능력치에 장비 효과 적용
                    equipment.Equip(characterManager.character);                    
                }
            }

            characterManager.character.ApplyAllTraits(characterManager);
            characterManager.character.UpdateFinalStats();
            characterManager.character.FinalStats.CurrentHp = characterManager.character.FinalStats.MaxHp;

            StartCoroutine(Delay(characterManager, characterObject)); // 애니메이션 렌더링을 위한 딜레이 적용

            if (isMine)
            {
                characterManager.character.Name = "Test_Ally" + characterManager.character.originName;
            }
            else
            {
                characterManager.character.Name = "Test_Enemy" + characterManager.character.originName;
                characterManager.character.Type = CharacterType.Elite;
                characterManager.character.personality = Personality.Cunning;
            }
        }
        else
        {
            Debug.LogError("CharacterManager component is missing on the character object.");
        }
    }
    
    // 모든 장비 아이콘 재설정 코드 예시
    public void VerifyAndSetEquipmentIcons(List<Equipment> equipmentList)
    {
        foreach (Equipment equip in equipmentList)
        {
            if (equip != null && equip.icon == null)
            {
                // 아이템의 이름에서 공백을 제거하고 아이콘 다시 로드
                string iconName = equip.name.Replace(" ", "");
                equip.icon = Resources.Load<Texture2D>($"Icons/{iconName}");
            }
        }
    }
    
}