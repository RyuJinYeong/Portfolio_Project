using UnityEngine;
using System.Collections.Generic;
using SoftKitty.InventoryEngine;
using System.Collections;

public class CharacterSpawner : MonoBehaviour
{
    public GameObject frontCharacterObject;     // 아군 전열(TestOrigin)
    public GameObject backCharacterObject1;     // 아군 후열1(사냥꾼)
    public GameObject backCharacterObject2;     // 아군 후열2(마법사)

    public GameObject enemyFrontCharacterObject; // 적 전열(방랑기사)
    public GameObject enemyBackCharacterObject1; // 적 후열1(사냥꾼)
    public GameObject enemyBackCharacterObject2; // 적 후열2(마법사)

    // 아이콘 캐시 그룹 (테스트씬 전용)
    private const string IconGroup = "TestBattle";

    // Start를 코루틴으로 변경
    private IEnumerator Start()
    {
        // 아이콘 프리로드
        var origins = CharacterOrigin.GetOriginData();
        var plan = new List<(GameObject go, CharacterData data, bool mine)>
        {
            (frontCharacterObject,       origins["TestOrigin"], true),
            (backCharacterObject1,       origins["사냥꾼"],     true),
            (backCharacterObject2,       origins["마법사"],     true),
            (enemyFrontCharacterObject,  origins["방랑기사"],   false),
            (enemyBackCharacterObject1,  origins["사냥꾼"],     false),
            (enemyBackCharacterObject2,  origins["마법사"],     false),
        };

        var keys = new HashSet<string>();
        foreach (var (_, data, _) in plan) CollectSkillIconAddresses(data, keys);
        if (keys.Count > 0) yield return IconStore.Preload(keys, "TestBattle");

        // 스폰 + 등록
        foreach (var (go, data, mine) in plan)
        {
            InitializeCharacter(go, data, mine);  // <-- 반환값을 CharacterManager로            
        }

        // 스폰이 전부 끝났음을 알림 → TurnManager가 자동 시작
        GameManager.Instance.SignalRosterReady();                

        TurnManager.Instance.ForceRebuildAndRestart();
    }

    private void OnDestroy()
    {
        // 테스트씬 끝날 때 그룹 단위 정리 (원하면 주석처리해서 재입장시 캐시 재사용 가능)
        IconStore.ClearGroup(IconGroup);
    }

    // 각 캐릭터의 스킬 아이콘 주소를 keys에 추가
    private static void CollectSkillIconAddresses(CharacterData characterData, HashSet<string> keys)
    {
        if (characterData?.Skills != null)
            foreach (var s in characterData.Skills)
                if (!string.IsNullOrEmpty(s.IconAddress)) keys.Add(s.IconAddress);

        if (characterData?.DefaultCounterSkill != null &&
            !string.IsNullOrEmpty(characterData.DefaultCounterSkill.IconAddress))
            keys.Add(characterData.DefaultCounterSkill.IconAddress);
    }

    // 캐시에 들어있는 텍스처를 스킬/카운터 스킬에 꽂아넣기
    private static void EnsureSkillIconsFromCache(CharacterData c)
    {
        if (c?.Skills != null)
            foreach (var s in c.Skills)
                s?.EnsureIconFromCache();

        c?.DefaultCounterSkill?.EnsureIconFromCache();
    }

    IEnumerator Delay(CharacterManager characterManager, GameObject characterObject)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // 애니 반영용 짧은 딜레이
        //characterManager.character.Portrait = characterObject.GetComponent<CharacterCustomization>().CapturePortrait();
    }

    private void InitializeCharacter(GameObject characterObject, CharacterData characterData, bool isMine)
    {
        if (characterObject == null)
        {
            Debug.LogError("Character object is missing for " + characterData?.Name);
            return;
        }

        var characterManager = characterObject.GetComponent<CharacterManager>();
        if (characterManager == null)
        {
            Debug.LogError("CharacterManager component is missing on the character object.");
            return;
        }

        // 원본을 깊은 복사하여 인스턴스 생성
        characterManager.InitializeCharacter(DeepCopy.DeepCopyCharacter(characterData));

        // (프리로드가 끝난 상태이므로) 스킬 아이콘을 캐시에서 연결
        EnsureSkillIconsFromCache(characterManager.character);

        // 장비 외형 반영 및 소유/상태 세팅
        characterObject.GetComponent<CharacterCustomization>().UpdateEquipmentAppearance(characterManager.character);
        characterManager.character.IsMine = isMine;
        characterManager.character.IsAlive = true;

        if (characterManager.character.originName == "마법사")
            characterManager.character.Traits[0].ApplyTrait(characterManager);

        // 장비 아이콘(리소스) 확인 및 보정
        VerifyAndSetEquipmentIcons((List<Equipment>)characterManager.character.GetEquipments());

        // InventoryHolder 연결
        var holders = characterObject.GetComponents<InventoryHolder>();
        InventoryHolder inventoryHolder = null;
        InventoryHolder equipmentHolder = null;

        if (holders.Length >= 2)
        {
            equipmentHolder = (holders[0].Type == InventoryHolder.HolderType.PlayerEquipment) ? holders[0] : holders[1];
            inventoryHolder = (holders[0].Type == InventoryHolder.HolderType.PlayerEquipment) ? holders[1] : holders[0];
        }

        // 장비 적용
        foreach (var equipment in (List<Equipment>)characterManager.character.GetEquipments())
        {
            if (equipment == null) continue;

            equipmentHolder?.AddItem(equipment, 1);
            var changed = new Dictionary<Item, int> { { equipment, 1 } };
            equipmentHolder?.ItemChanged(changed);

            equipment.Equip(characterManager.character);
        }

        characterManager.character.ApplyAllTraits(characterManager);
        characterManager.character.UpdateFinalStats();
        characterManager.character.CurrentHp = characterManager.character.FinalStats.MaxHp;

        StartCoroutine(Delay(characterManager, characterObject));

        // 테스트용 이름/성향
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

        GameManager.Instance.RegisterCharacter(characterManager);
    }

    // 기존 장비 아이콘 리소스 보정 (리소스 기반 유지)
    public void VerifyAndSetEquipmentIcons(List<Equipment> equipmentList)
    {
        foreach (Equipment equip in equipmentList)
        {
            if (equip != null && equip.icon == null)
            {
                string iconName = equip.name.Replace(" ", "");
                equip.icon = Resources.Load<Texture2D>($"Icons/{iconName}");
            }
        }
    }
}
