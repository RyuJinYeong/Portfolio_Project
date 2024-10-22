using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EquipmentHandler : MonoBehaviour
{
    public Transform WeaponParent;  // 무기 부모 오브젝트
    public Transform ArmorParent;   // 방어구 부모 오브젝트

    private GameObject[] rightHandWeapons; // 주무기
    private GameObject[] leftHandWeapons; // 보조무기
    private GameObject[] twoHandedWeapons; // 양손무기
    public GameObject[] armors; // 방어구 배열 (중갑, 경갑, 의복 등)

    string handType;

    void Start()
    {
        // 각 배열에 부모 오브젝트의 자식 오브젝트들을 할당
        rightHandWeapons = GetChildObjects(WeaponParent.GetChild(0));
        leftHandWeapons = GetChildObjects(WeaponParent.GetChild(1));
        twoHandedWeapons = GetChildObjects(WeaponParent.GetChild(2));

        armors = GetChildObjects(ArmorParent);
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

        if (characterData.SubWeapon is Weapon subWeapon && subWeapon != null)
        {
            handType = "LeftHanded";
            ChangeWeaponModel(subWeapon.WeaponType);
        }
    }

    // 무기 외형을 변경하는 메서드
    private void ChangeWeaponModel(WeaponType weaponType)
    {
        // 모든 무기 비활성화
        DeactivateAllWeapons();

        // 무기 타입 및 위치에 따른 무기 활성화
        switch (handType)
        {
            case "RightHanded":
                ActivateRightHandWeapon(weaponType);
                break;
            case "LeftHanded":
                ActivateLeftHandWeapon(weaponType);
                break;
            case "TwoHanded":
                ActivateTwoHandedWeapon(weaponType);
                break;
        }
    }
    private void DeactivateAllWeapons()
    {
        // 우수, 좌수, 양손 무기 모두 비활성화
        foreach (var weapon in rightHandWeapons) weapon.SetActive(false);
        foreach (var weapon in leftHandWeapons) weapon.SetActive(false);
        foreach (var weapon in twoHandedWeapons) weapon.SetActive(false);
    }

    // 우수 무기 활성화
    private void ActivateRightHandWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.LongSword:
                rightHandWeapons[0].SetActive(true);
                break;
            case WeaponType.Mace:
                rightHandWeapons[1].SetActive(true);
                break;
                // 나머지 무기 타입 처리
        }
    }

    // 좌수 무기 활성화
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
                // 나머지 무기 타입 처리
        }
    }

    // 양손 무기 활성화
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
                // 나머지 무기 타입 처리
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
        // 모든 방어구 비활성화
        foreach (var armor in armors)
        {
            armor.SetActive(false);
        }

        // ArmorCategory에 따른 방어구 활성화
        switch (armorCategory)
        {
            case ArmorCategory.HeavyArmor:
                armors[0].SetActive(true); // 중갑
                break;
            case ArmorCategory.LightArmor:
                armors[1].SetActive(true); // 경갑
                break;
            case ArmorCategory.ClothArmor:
                armors[2].SetActive(true); // 의복
                break;
        }
    }

    #endregion
}
