using System;
using UnityEngine;

public class CharacterCustomization : MonoBehaviour
{
    public RenderTexture portraitRenderTexture;
    public Camera portraitCamera;

    // 캐릭터 루트 및 모델들
    public GameObject characterRoot;
    public bool isMale;

    // 커스터마이징 요소들
    public GameObject[] eyebrows;
    public GameObject[] eyes;
    public GameObject[] mouth;    
    public GameObject[] hair;
    public GameObject[] beard;

    private CharacterManager characterManager; 

    // 장비 관리
    public Transform WeaponParent;  // 무기 부모 오브젝트
    public Transform ArmorParent;   // 방어구 부모 오브젝트

    public GameObject[] rightHandWeapons; // 주무기
    public GameObject[] leftHandWeapons; // 보조무기
    public GameObject[] twoHandedWeapons; // 양손무기
    private GameObject[] armors; // 방어구 배열 (중갑, 경갑, 의복 등)

    string handType;

    public Sprite characterPortrait { get; set; }

    void Awake()
    {
        // CharacterManager를 캐릭터 루트에서 찾아서 참조
        characterManager = characterRoot.GetComponent<CharacterManager>();

        // 각 무기 배열에 부모 오브젝트의 자식 오브젝트들을 할당
        GameObject[] newRightHandWeapons = GetChildObjects(WeaponParent.GetChild(0));
        GameObject[] newLeftHandWeapons = GetChildObjects(WeaponParent.GetChild(1));
        GameObject[] newTwoHandedWeapons = GetChildObjects(WeaponParent.GetChild(2));

        // 기존 배열의 크기를 유지하면서, 새로운 객체를 배열의 시작 부분에 채워 넣기
        for (int i = 0; i < newRightHandWeapons.Length && i < rightHandWeapons.Length; i++)
        {
            rightHandWeapons[i] = newRightHandWeapons[i];
        }

        for (int i = 0; i < newLeftHandWeapons.Length && i < leftHandWeapons.Length; i++)
        {
            leftHandWeapons[i] = newLeftHandWeapons[i];
        }

        for (int i = 0; i < newTwoHandedWeapons.Length && i < twoHandedWeapons.Length; i++)
        {
            twoHandedWeapons[i] = newTwoHandedWeapons[i];
        }

        // 배열 초기화가 제대로 되었는지 로그로 확인
        Debug.Log($"Right Hand Weapons Count: {rightHandWeapons.Length}");
        Debug.Log($"Left Hand Weapons Count: {leftHandWeapons.Length}");
        Debug.Log($"Two Handed Weapons Count: {twoHandedWeapons.Length}");
    }

    // 자식 오브젝트 배열로 반환
    private GameObject[] GetChildObjects(Transform parent)
    {
        int childCount = parent.childCount;
        GameObject[] childObjects = new GameObject[childCount];

        for (int i = 0; i < childCount; i++)
        {
            childObjects[i] = parent.GetChild(i).gameObject;
        }

        return childObjects;
    }

    #region 커스터마이징 로직

    // 커스터마이징 적용
    public void ApplyCustomization(CharacterData characterData)
    {
        SetHairStyle(characterData.customizationData.HairType);
        SetHairColor(characterData.customizationData.HairColor);

        SetEyebrows(characterData.customizationData.EyebrowsType);
        SetEyes(characterData.customizationData.EyeType);

        SetMouth(characterData.customizationData.MouthType);
        if (isMale)
            SetBeard(characterData.customizationData.BeardType);  // 남성일 경우만

        SetSkinTone(characterData.customizationData.SkinTone);
    }

    public Sprite CapturePortrait()
    {
        portraitCamera.gameObject.SetActive(true);
        // RenderTexture를 활성화하여 현재 화면을 캡처
        portraitCamera.targetTexture = portraitRenderTexture;
        portraitCamera.Render();
        portraitCamera.targetTexture = null;

        // RenderTexture의 데이터를 Texture2D로 변환
        RenderTexture.active = portraitRenderTexture;
        Texture2D portraitTexture = new Texture2D(portraitRenderTexture.width, portraitRenderTexture.height, TextureFormat.RGB24, false);
        portraitTexture.ReadPixels(new Rect(0, 0, portraitRenderTexture.width, portraitRenderTexture.height), 0, 0);
        portraitTexture.Apply();
        RenderTexture.active = null;

        // Texture2D를 Sprite로 변환하여 저장
        Rect rect = new Rect(0, 0, portraitTexture.width, portraitTexture.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        characterPortrait = Sprite.Create(portraitTexture, rect, pivot);

        // Texture2D 메모리 해제
        Destroy(portraitTexture);
        portraitCamera.gameObject.SetActive(false);

        return characterPortrait;
    }


    // 눈썹 설정
    public void SetEyebrows(int index)
    {
        ActivateModelFromArray(eyebrows, index);
    }

    // 눈 설정
    public void SetEyes(int index)
    {
        ActivateModelFromArray(eyes, index);
    }

    // 입 설정
    public void SetMouth(int index)
    {
        ActivateModelFromArray(mouth, index);
    }

    // 수염 설정
    public void SetBeard(int index)
    {
        if (beard.Length > 0 && isMale)
        {
            ActivateModelFromArray(beard, index);
        }
    }

    // 헤어스타일 설정
    public void SetHairStyle(int index)
    {
        ActivateModelFromArray(hair, index);
    }

    // 머리색 설정
    public void SetHairColor(int index)
    {
        //머리 색 변경 로직
    }

    // 피부톤 설정
    public void SetSkinTone(int index)
    {
        //피부 톤 변경 로직
    }

    // 배열에서 선택한 모델만 활성화
    private void ActivateModelFromArray(GameObject[] modelArray, int index)
    {
        for (int i = 0; i < modelArray.Length; i++)
        {
            modelArray[i].SetActive(i == index);
        }
    }

    #endregion

    public void UpdateEquipmentAppearance(CharacterData characterData)
    {
        UpdateWeaponAppearance(characterData);
        //UpdateArmorAppearance(characterData); // - 로직 완성 전까지 적용 보류
    }

    #region 무기 로직

    // 무기 외형 업데이트 메서드
    public void UpdateWeaponAppearance(CharacterData characterData)
    {
        if (characterData.Weapon is Weapon weapon)
        {
            if (weapon.Tags.Contains(WeaponTag.TwoHanded))
            {
                handType = "TwoHanded";
                ChangeWeaponModel(weapon.WeaponType);
            }
            else
            {
                handType = "RightHanded";
                ChangeWeaponModel(weapon.WeaponType);
            }
        }
        else if (characterData.Weapon == null)
        {
            foreach (var rightHand in rightHandWeapons)
                rightHand.SetActive(false);
            foreach (var twoHand in twoHandedWeapons)
                twoHand.SetActive(false);
        }

        if (characterData.SubWeapon is Weapon subWeapon)
        {
            handType = "LeftHanded";
            ChangeWeaponModel(subWeapon.WeaponType);
        }
        else if (characterData.SubWeapon == null)
        {
            foreach (var leftHand in leftHandWeapons)
                leftHand.SetActive(false);
        }
    }

    // 무기 외형을 변경하는 메서드
    private void ChangeWeaponModel(WeaponType weaponType)
    {
        DeactivateWeapons(handType);

        switch (handType)
        {
            case "TwoHanded":
                ActivateTwoHandedWeapon(weaponType);
                break;
            case "RightHanded":
                ActivateRightHandWeapon(weaponType);
                break;
            case "LeftHanded":
                ActivateLeftHandWeapon(weaponType);
                break;
        }
    }
    private void DeactivateWeapons(string handType)
    {
        switch (handType)
        {
            case "RightHanded":
                foreach (var weapon in rightHandWeapons)
                    weapon.SetActive(false);
                foreach (var weapon in twoHandedWeapons)
                    weapon.SetActive(false);
                break;
            case "LeftHanded":
                foreach (var weapon in leftHandWeapons)
                    weapon.SetActive(false);
                foreach (var weapon in twoHandedWeapons)
                    weapon.SetActive(false);
                break;
            case "TwoHanded":
                DeactivateAllWeapons();
                break;
        }
    }

    private void DeactivateAllWeapons()
    {
        foreach (var weapon in rightHandWeapons) weapon.SetActive(false);

        foreach (var weapon in leftHandWeapons) weapon.SetActive(false);

        foreach (var weapon in twoHandedWeapons) weapon.SetActive(false);
    }

    private void ActivateRightHandWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.LongSword:
                rightHandWeapons[0].SetActive(true);
                break;
            case WeaponType.Dagger:
                rightHandWeapons[1].SetActive(true);
                break;
            case WeaponType.Staff:
                rightHandWeapons[2].SetActive(true);
                break;
            case WeaponType.Greatsword:
                rightHandWeapons[3].SetActive(true);
                break;
            case WeaponType.Axe:
                rightHandWeapons[4].SetActive(true);
                break;/*
            case WeaponType.Mace:
                rightHandWeapons[5].SetActive(true);
                break;
            case WeaponType.Hammer:
                rightHandWeapons[5].SetActive(true);
                break;*/
        }
    }

    private void ActivateLeftHandWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Dagger:
                leftHandWeapons[0].SetActive(true);
                break;
            case WeaponType.Shield:
                leftHandWeapons[1].SetActive(true);
                break;
            case WeaponType.Orb:
                leftHandWeapons[2].SetActive(true);
                break;
            case WeaponType.Book:
                leftHandWeapons[3].SetActive(true);
                break;
                // 기타 보조무기 타입들
        }
    }

    private void ActivateTwoHandedWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Bow:
                twoHandedWeapons[0].SetActive(true);
                break;
            case WeaponType.Spear:
                twoHandedWeapons[1].SetActive(true);
                break;
            case WeaponType.Hammer:
                twoHandedWeapons[2].SetActive(true);
                break;
            case WeaponType.Axe:
                twoHandedWeapons[3].SetActive(true);
                break;
            case WeaponType.TwoHandedSword:
                twoHandedWeapons[4].SetActive(true);
                break;
                // 기타 양손 무기
        }
    }

    #endregion

    #region 방어구 로직

    // 캐릭터 데이터에서 방어구 상태 확인
    public void UpdateArmorAppearance(CharacterData characterData)
    {
        if (characterData.Armor is Armor armor)
        {
            ChangeArmorModel(armor.ArmorCategory);
        }
    }

    // 방어구 외형을 변경하는 메서드
    private void ChangeArmorModel(ArmorCategory armorCategory)
    {
        foreach (var armor in armors)
        {
            armor.SetActive(false);
        }

        switch (armorCategory)
        {
            case ArmorCategory.HeavyArmor:
                armors[0].SetActive(true);
                break;
            case ArmorCategory.LightArmor:
                armors[1].SetActive(true);
                break;
            case ArmorCategory.ClothArmor:
                armors[2].SetActive(true);
                break;
        }
    }

    #endregion
}
