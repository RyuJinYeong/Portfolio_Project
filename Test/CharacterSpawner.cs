using UnityEngine;
using System.Collections.Generic;

public class CharacterSpawner : MonoBehaviour
{
    public GameObject frontCharacterObject; // 전열에 배치할 캐릭터 오브젝트 (방랑기사)
    public GameObject backCharacterObject1; // 후열에 배치할 첫 번째 캐릭터 오브젝트 (사냥꾼)
    public GameObject backCharacterObject2; // 후열에 배치할 두 번째 캐릭터 오브젝트 (마법사)

    public GameObject enemyFrontCharacterObject; // 적군 전열 캐릭터 오브젝트
    public GameObject enemyBackCharacterObject1; // 적군 후열 첫 번째 캐릭터 오브젝트
    public GameObject enemyBackCharacterObject2; // 적군 후열 두 번째 캐릭터 오브젝트

    private void Start()
    {
        // 아군 캐릭터 데이터 초기화
        InitializeCharacter(frontCharacterObject, CharacterOrigin.GetOriginData()["방랑기사"], true);
        InitializeCharacter(backCharacterObject1, CharacterOrigin.GetOriginData()["사냥꾼"], true);
        InitializeCharacter(backCharacterObject2, CharacterOrigin.GetOriginData()["마법사"], true);

        // 적군 캐릭터 데이터 초기화
        InitializeCharacter(enemyFrontCharacterObject, CharacterOrigin.GetOriginData()["방랑기사"], false);
        InitializeCharacter(enemyBackCharacterObject1, CharacterOrigin.GetOriginData()["사냥꾼"], false);
        InitializeCharacter(enemyBackCharacterObject2, CharacterOrigin.GetOriginData()["마법사"], false);
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
            characterManager.InitializeCharacter(characterData);
            characterManager.character.IsMine = isMine;
            if(isMine)
            {
                characterData.Name = "Test_Ally" + characterData.originName;
            }
            else
            {
                characterData.Name = "Test_Enemy" + characterData.originName;
            }
        }
        else
        {
            Debug.LogError("CharacterManager component is missing on the character object.");
        }
    }

}
