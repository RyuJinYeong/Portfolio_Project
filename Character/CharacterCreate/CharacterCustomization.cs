using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using P09.Modular.Humanoid.Data;

public class CharacterCustomization : MonoBehaviour
{
    [Header("P09 Model Root")]
    [SerializeField] private Transform modelRoot;

    [Header("Body Customization Containers")]
    public EditPartDataContainer genderContainer;
    public EditPartDataContainer faceTypeContainer;
    public EditPartDataContainer hairStyleContainer;
    public EditPartDataContainer hairColorContainer;
    public EditPartDataContainer skinColorContainer;
    public EditPartDataContainer eyeColorContainer;
    public EditPartDataContainer facialHairContainer;
    public EditPartDataContainer bustSizeContainer;

    [Header("Weapon Roots")]
    public Transform weaponRoot;
    public Transform bowRoot;
    public Transform shieldRoot;
    public Transform staffRoot;
    public Transform swordRoot;
    public Transform axeRoot;
    public Transform hammerRoot;
    public Transform twoHandedRoot;

    [Header("Weapon Models")]
    public GameObject[] bows;
    public GameObject[] shields;
    public GameObject[] staffs;
    public GameObject[] swords;
    public GameObject[] axes;
    public GameObject[] hammers;
    public GameObject[] twoHandedWeapons;

    [Header("Runtime")]
    public bool isMale = true;

    private const int MaleGenderId = 1;
    private const int FemaleGenderId = 2;

    private const string SkinMaterialPattern = @"^P09_.*_Skin.*$";
    private const string EyeMaterialPattern = @"^P09_Eye.*$";

    private int currentGenderId = MaleGenderId;
    private int currentFaceTypeId = 1;
    private int currentHairStyleId = 1;
    private int currentHairColorId = 1;
    private int currentSkinColorId = 1;
    private int currentEyeColorId = 1;
    private int currentFacialHairId = 0;
    private int currentBustSizeId = 2;

    private Transform[] cachedTransforms;
    private Renderer[] cachedRenderers;

    private void Awake()
    {
        if (modelRoot == null)
            modelRoot = transform;

        CacheModelParts();
        CacheWeaponObjectsFromParent();
        DeactivateAllWeapons();
    }

    private void CacheModelParts()
    {
        if (modelRoot == null) return;

        cachedTransforms = modelRoot.GetComponentsInChildren<Transform>(true);
        cachedRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
    }

    private void CacheWeaponObjectsFromParent()
    {
        if (weaponRoot != null)
        {
            if (bowRoot == null) bowRoot = FindDirectChild(weaponRoot, "Bow");
            if (shieldRoot == null) shieldRoot = FindDirectChild(weaponRoot, "Shield");
            if (staffRoot == null) staffRoot = FindDirectChild(weaponRoot, "Staff");
            if (swordRoot == null) swordRoot = FindDirectChild(weaponRoot, "Sword");
            if (axeRoot == null) axeRoot = FindDirectChild(weaponRoot, "Axe");
            if (hammerRoot == null) hammerRoot = FindDirectChild(weaponRoot, "Hammer");
            if (twoHandedRoot == null) twoHandedRoot = FindDirectChild(weaponRoot, "TwoHanded");
        }

        if (bowRoot != null && (bows == null || bows.Length == 0))
            bows = GetDirectChildObjectsByPrefix(bowRoot, "Bow_");

        if (shieldRoot != null && (shields == null || shields.Length == 0))
            shields = GetDirectChildObjectsByPrefix(shieldRoot, "Shield_");

        if (staffRoot != null && (staffs == null || staffs.Length == 0))
            staffs = GetDirectChildObjectsByPrefix(staffRoot, "Staff_");

        if (swordRoot != null && (swords == null || swords.Length == 0))
            swords = GetDirectChildObjectsByPrefix(swordRoot, "Sword_");

        if (axeRoot != null && (axes == null || axes.Length == 0))
            axes = GetDirectChildObjectsByPrefix(axeRoot, "Axe_");

        if (hammerRoot != null && (hammers == null || hammers.Length == 0))
            hammers = GetDirectChildObjectsByPrefix(hammerRoot, "Hammer_");

        if (twoHandedRoot != null && (twoHandedWeapons == null || twoHandedWeapons.Length == 0))
            twoHandedWeapons = GetAllDirectChildObjects(twoHandedRoot);
    }

    private GameObject[] GetAllDirectChildObjects(Transform parent)
    {
        if (parent == null) return new GameObject[0];

        return parent
            .Cast<Transform>()
            .OrderBy(t => t.name)
            .Select(t => t.gameObject)
            .ToArray();
    }

    private Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private GameObject[] GetDirectChildObjectsByPrefix(Transform parent, string prefix)
    {
        if (parent == null) return new GameObject[0];

        return parent
            .Cast<Transform>()
            .Where(t => t.name.StartsWith(prefix))
            .OrderBy(t => t.name)
            .Select(t => t.gameObject)
            .ToArray();
    }

    #region 커스터마이징 적용

    public void ApplyCustomization(CharacterData characterData)
    {
        if (characterData == null || characterData.customizationData == null)
            return;

        ApplyCustomization(characterData.customizationData);
    }

    public void ApplyCustomization(CustomizationData data)
    {
        if (data == null) return;

        currentGenderId = data.GenderId <= 0 ? (data.IsMale ? MaleGenderId : FemaleGenderId) : data.GenderId;
        isMale = currentGenderId == MaleGenderId;

        currentFaceTypeId = data.FaceTypeId <= 0 ? 1 : data.FaceTypeId;
        currentHairStyleId = data.HairStyleId <= 0 ? 1 : data.HairStyleId;
        currentHairColorId = data.HairColorId <= 0 ? 1 : data.HairColorId;
        currentSkinColorId = data.SkinColorId <= 0 ? 1 : data.SkinColorId;
        currentEyeColorId = data.EyeColorId <= 0 ? 1 : data.EyeColorId;

        currentFacialHairId = data.FacialHairId;
        currentBustSizeId = data.BustSizeId <= 0 ? 2 : data.BustSizeId;

        ApplyRendererPart(currentGenderId, genderContainer);
        ApplyRendererPart(currentFaceTypeId, faceTypeContainer);
        ApplyRendererPart(currentHairStyleId, hairStyleContainer);

        ApplyHairColor(currentHairColorId, currentHairStyleId);
        ApplySkinColor(currentSkinColorId);
        ApplyEyeColor(currentEyeColorId);

        if (isMale)
        {
            ApplyRendererPart(currentFacialHairId, facialHairContainer);
        }
        else
        {
            ClearRendererPart(facialHairContainer);
            ApplyBustSize(currentBustSizeId);
        }
    }


    #endregion

    #region 기존 코드 호환용 Setter

    public void SetGender(int genderId)
    {
        currentGenderId = genderId <= 0 ? MaleGenderId : genderId;
        isMale = currentGenderId == MaleGenderId;

        ApplyRendererPart(currentGenderId, genderContainer);
    }

    public void SetFaceType(int faceTypeId)
    {
        currentFaceTypeId = faceTypeId <= 0 ? 1 : faceTypeId;
        ApplyRendererPart(currentFaceTypeId, faceTypeContainer);
    }

    public void SetHairStyle(int hairStyleId)
    {
        currentHairStyleId = hairStyleId <= 0 ? 1 : hairStyleId;
        ApplyRendererPart(currentHairStyleId, hairStyleContainer);
        ApplyHairColor(currentHairColorId, currentHairStyleId);
    }

    public void SetHairColor(int hairColorId)
    {
        currentHairColorId = hairColorId <= 0 ? 1 : hairColorId;
        ApplyHairColor(currentHairColorId, currentHairStyleId);
    }

    public void SetSkinColor(int skinColorId)
    {
        currentSkinColorId = skinColorId <= 0 ? 1 : skinColorId;
        ApplySkinColor(currentSkinColorId);
    }

    public void SetEyeColor(int eyeColorId)
    {
        currentEyeColorId = eyeColorId <= 0 ? 1 : eyeColorId;
        ApplyEyeColor(currentEyeColorId);
    }

    public void SetFacialHair(int facialHairId)
    {
        currentFacialHairId = facialHairId;

        if (isMale)
            ApplyRendererPart(currentFacialHairId, facialHairContainer);
    }

    public void SetBustSize(int bustSizeId)
    {
        currentBustSizeId = bustSizeId <= 0 ? 2 : bustSizeId;

        if (!isMale)
            ApplyBustSize(currentBustSizeId);
    }

    public void SetSkinTone(int skinColorId) => SetSkinColor(skinColorId);
    public void SetBeard(int facialHairId) => SetFacialHair(facialHairId);

    public void SetEyebrows(int faceTypeId) => SetFaceType(faceTypeId);
    public void SetEyes(int eyeColorId) => SetEyeColor(eyeColorId);
    public void SetMouth(int index) { }

    #endregion

    #region P09 외형 적용 로직

    private void ApplyRendererPart(int currentId, EditPartDataContainer container)
    {
        if (container == null || container.PartDataList == null)
            return;

        EnsureCache();

        foreach (Transform child in cachedTransforms)
        {
            foreach (var data in container.PartDataList)
            {
                if (string.IsNullOrEmpty(data.MeshName))
                    continue;

                if (child.name == data.MeshName)
                {
                    child.gameObject.SetActive(data.ContentId == currentId);
                }
                else if (child.name == string.Format(data.MeshName, "Male"))
                {
                    child.gameObject.SetActive(isMale && data.ContentId == currentId);
                }
                else if (child.name == string.Format(data.MeshName, "Female") ||
                         child.name == string.Format(data.MeshName, "Fem"))
                {
                    child.gameObject.SetActive(!isMale && data.ContentId == currentId);
                }
            }
        }
    }

    private void ClearRendererPart(EditPartDataContainer container)
    {
        if (container == null || container.PartDataList == null)
            return;

        EnsureCache();

        foreach (Transform child in cachedTransforms)
        {
            foreach (var data in container.PartDataList)
            {
                if (string.IsNullOrEmpty(data.MeshName))
                    continue;

                if (child.name == data.MeshName ||
                    child.name == string.Format(data.MeshName, "Male") ||
                    child.name == string.Format(data.MeshName, "Female") ||
                    child.name == string.Format(data.MeshName, "Fem"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    private void ApplyHairColor(int hairColorId, int hairStyleId)
    {
        if (hairColorContainer == null || hairColorContainer.PartDataList == null)
            return;

        EnsureCache();

        var currentData = hairColorContainer.PartDataList
            .FirstOrDefault(d => d.ContentId == hairColorId) as HairColorEditPartData;

        if (currentData == null)
            return;

        foreach (Transform child in cachedTransforms)
        {
            foreach (var data in hairColorContainer.PartDataList)
            {
                if (string.IsNullOrEmpty(data.MeshName))
                    continue;

                if (child.name == string.Format(data.MeshName, hairStyleId))
                {
                    Renderer renderer = child.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.material = currentData.GetMaterial(hairStyleId);
                }
            }
        }
    }

    private void ApplySkinColor(int skinColorId)
    {
        if (skinColorContainer == null || skinColorContainer.PartDataList == null)
            return;

        EnsureCache();

        var currentData = skinColorContainer.PartDataList
            .FirstOrDefault(d => d.ContentId == skinColorId) as ColorEditPartData;

        if (currentData == null)
            return;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;

                if (Regex.IsMatch(materials[i].name, SkinMaterialPattern))
                    materials[i] = currentData.Material;
            }

            renderer.materials = materials;
        }
    }

    private void ApplyEyeColor(int eyeColorId)
    {
        if (eyeColorContainer == null || eyeColorContainer.PartDataList == null)
            return;

        EnsureCache();

        var currentData = eyeColorContainer.PartDataList
            .FirstOrDefault(d => d.ContentId == eyeColorId) as ColorEditPartData;

        if (currentData == null)
            return;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;

                if (Regex.IsMatch(materials[i].name, EyeMaterialPattern))
                    materials[i] = currentData.Material;
            }

            renderer.materials = materials;
        }
    }

    private void ApplyBustSize(int bustSizeId)
    {
        if (bustSizeContainer == null || bustSizeContainer.PartDataList == null)
            return;

        EnsureCache();

        var currentData = bustSizeContainer.PartDataList
            .FirstOrDefault(d => d.ContentId == bustSizeId) as BustSizeEditPartData;

        if (currentData == null)
            return;

        foreach (Transform child in cachedTransforms)
        {
            if (child.name != string.Format(currentData.MeshName, "R") &&
                child.name != string.Format(currentData.MeshName, "L"))
            {
                continue;
            }

            child.localScale = currentData.Size;
        }
    }

    private void EnsureCache()
    {
        if (cachedTransforms == null || cachedTransforms.Length == 0 ||
            cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheModelParts();
        }
    }

    #endregion

    #region 무기 / 보조무기 외형 로직

    public void UpdateEquipmentAppearance(CharacterData characterData)
    {
        UpdateWeaponAppearance(characterData);
        UpdateArmorAppearance(characterData);
    }

    public void UpdateWeaponAppearance(CharacterData characterData)
    {
        DeactivateAllWeapons();

        if (characterData == null)
            return;

        EquipmentRuntimeData mainEquipment = characterData.GetMainWeaponRuntime();
        EquipmentRuntimeData subEquipment = characterData.GetSubWeaponRuntime();

        WeaponDefinitionSO mainWeapon = mainEquipment != null
            ? mainEquipment.definition as WeaponDefinitionSO
            : null;

        WeaponDefinitionSO subWeapon = subEquipment != null
            ? subEquipment.definition as WeaponDefinitionSO
            : null;

        bool mainWeaponIsTwoHanded = false;

        if (mainWeapon != null)
        {
            mainWeaponIsTwoHanded =
                mainWeapon.weaponTags != null &&
                mainWeapon.weaponTags.Contains(WeaponTag.TwoHanded);

            bool activatedByVisualKey = ActivateWeaponByVisualKey(mainEquipment);

            if (!activatedByVisualKey)
            {
                if (mainWeaponIsTwoHanded)
                    ActivateTwoHandedWeapon(mainWeapon.weaponType);
                else
                    ActivateMainWeapon(mainWeapon.weaponType);
            }
        }

        if (mainWeaponIsTwoHanded)
            return;

        if (subWeapon != null)
        {
            bool activatedByVisualKey = ActivateWeaponByVisualKey(subEquipment);

            if (!activatedByVisualKey)
                ActivateSubWeapon(subWeapon.weaponType);
        }
    }

    private void UpdateArmorAppearance(CharacterData characterData)
    {
        DeactivateAllArmorVisuals();

        if (characterData == null)
            return;

        ActivateArmorPart(characterData.GetHelmet(), "Head");

        ActivateArmorPart(characterData.GetArmor(), "Chest");
        ActivateArmorPart(characterData.GetArmor(), "Waist");

        ActivateArmorPart(characterData.GetGloves(), "Arm");

        ActivateArmorPart(characterData.GetShoes(), "Leg");
    }

    private void ActivateArmorPart(EquipmentDefinitionSO equipment, string partSuffix)
    {
        string visualKey = equipment != null
            ? equipment.visualKey
            : "Armor_001";

        if (string.IsNullOrEmpty(visualKey))
            return;

        string parentName = NormalizeArmorVisualKey(visualKey);

        Transform armorRoot = FindChildRecursive(modelRoot, parentName);

        if (armorRoot == null)
        {
            Debug.LogWarning($"방어구 외형 부모를 찾지 못했습니다: {parentName}");
            return;
        }

        armorRoot.gameObject.SetActive(true);

        for (int i = 0; i < armorRoot.childCount; i++)
        {
            Transform child = armorRoot.GetChild(i);

            if (child == null)
                continue;

            if (child.name.EndsWith("_" + partSuffix))
            {
                child.gameObject.SetActive(true);
                return;
            }
        }

        Debug.LogWarning($"방어구 파츠를 찾지 못했습니다: {parentName} / {partSuffix}");
    }

    private void DeactivateAllArmorVisuals()
    {
        if (modelRoot == null)
            return;

        Transform[] allChildren = modelRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child == null)
                continue;

            if (IsArmorPartObject(child.name))
                child.gameObject.SetActive(false);
        }
    }

    private bool IsArmorPartObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        return objectName.Contains("_Armor_") &&
               (objectName.EndsWith("_Arm") ||
                objectName.EndsWith("_Chest") ||
                objectName.EndsWith("_Head") ||
                objectName.EndsWith("_Leg") ||
                objectName.EndsWith("_Waist"));
    }

    private string NormalizeArmorVisualKey(string visualKey)
    {
        if (string.IsNullOrEmpty(visualKey))
            return "";

        if (visualKey.StartsWith("Armor_"))
            return visualKey;

        if (visualKey.StartsWith("armor_"))
            return "Armor_" + visualKey.Substring("armor_".Length);

        return visualKey;
    }

    private Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);

            if (found != null)
                return found;
        }

        return null;
    }

    private bool ActivateWeaponByVisualKey(EquipmentRuntimeData equipment)
    {
        if (equipment == null)
            return false;

        if (string.IsNullOrEmpty(equipment.visualKey))
            return false;

        if (weaponRoot == null)
            return false;

        Transform weaponVisual = FindChildRecursive(weaponRoot, equipment.visualKey);

        if (weaponVisual == null)
        {
            Debug.LogWarning($"무기 외형을 찾지 못했습니다: {equipment.visualKey}");
            return false;
        }

        ActivateSelfAndParentsUntil(weaponVisual, weaponRoot);
        weaponVisual.gameObject.SetActive(true);

        return true;
    }

    private void ActivateSelfAndParentsUntil(Transform target, Transform stopRoot)
    {
        if (target == null)
            return;

        Transform current = target;

        while (current != null)
        {
            current.gameObject.SetActive(true);

            if (current == stopRoot)
                break;

            current = current.parent;
        }
    }

    private void ActivateMainWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Bow:
                ActivateArrayIndex(bows, 0);
                break;

            case WeaponType.Staff:
                ActivateArrayIndex(staffs, 0);
                break;

            case WeaponType.LongSword:
                ActivateArrayIndex(swords, 0);
                break;

            case WeaponType.Dagger:
                ActivateArrayIndex(swords, 1);
                break;

            case WeaponType.Greatsword:
                ActivateArrayIndex(swords, 2);
                break;

            case WeaponType.Axe:
                ActivateArrayIndex(axes, 0);
                break;

            case WeaponType.Hammer:
                ActivateArrayIndex(hammers, 0);
                break;

            case WeaponType.Two_HandedSword:
                ActivateArrayIndex(swords, 4);
                break;

            case WeaponType.Spear:
                // 현재 모델 없음
                break;
        }
    }

    private void ActivateSubWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Shield:
                ActivateArrayIndex(shields, 0);
                break;

            case WeaponType.Dagger:
                ActivateArrayIndex(swords, 1);
                break;

            case WeaponType.Orb:
                // 현재 모델 없음
                break;

            case WeaponType.Book:
                // 현재 모델 없음
                break;
        }
    }

    private void ActivateTwoHandedWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Bow:
                ActivateArrayIndex(bows, 0);
                break;

            case WeaponType.Staff:
                ActivateArrayIndex(staffs, 0);
                break;

            case WeaponType.Axe:
                ActivateTwoHandedByName("Axe");
                break;

            case WeaponType.Hammer:
                ActivateTwoHandedByName("Hammer");
                break;

            case WeaponType.Two_HandedSword:
            case WeaponType.Greatsword:
                ActivateTwoHandedByName("Sword");
                break;

            case WeaponType.Spear:
                ActivateTwoHandedByName("Spear");
                break;

            default:
                break;
        }
    }
    private void ActivateTwoHandedByName(string keyword)
    {
        if (twoHandedWeapons == null || twoHandedWeapons.Length == 0)
            return;

        foreach (var obj in twoHandedWeapons)
        {
            if (obj == null) continue;

            if (obj.name.Contains(keyword))
            {
                obj.SetActive(true);
                return;
            }
        }
    }

    private void DeactivateAllWeapons()
    {
        SetAllActive(bows, false);
        SetAllActive(shields, false);
        SetAllActive(staffs, false);
        SetAllActive(swords, false);
        SetAllActive(axes, false);
        SetAllActive(hammers, false);
        SetAllActive(twoHandedWeapons, false);
    }

    private void SetAllActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void ActivateArrayIndex(GameObject[] array, int index)
    {
        if (array == null) return;
        if (index < 0 || index >= array.Length) return;
        if (array[index] == null) return;

        array[index].SetActive(true);
    }

    #endregion

    #region 캐릭터 초상화 촬영

    public Texture2D CapturePortrait(Camera camera, RenderTexture renderTexture)
    {
        if (camera == null || renderTexture == null)
            return null;

        bool prevCameraActive = camera.gameObject.activeSelf;
        RenderTexture prevTarget = camera.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        camera.gameObject.SetActive(true);
        camera.targetTexture = renderTexture;
        camera.Render();

        RenderTexture.active = renderTexture;

        Texture2D texture = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGBA32,
            false
        );

        texture.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );

        texture.Apply();

        camera.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        camera.gameObject.SetActive(prevCameraActive);

        return texture;
    }

    #endregion
}
