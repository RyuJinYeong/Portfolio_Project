using UnityEngine;
using P09.Modular.Humanoid.Data;

public class CharacterCustomizationUI : MonoBehaviour
{
    [Header("Character Preview Objects")]
    public GameObject character_M;
    public GameObject character_F;

    [Header("Gender Exclusive UI")]
    public GameObject FacialHairUI;
    public GameObject BustSizeUI;

    [Header("Switchers")]
    public CustomizationSwitcher genderSwitcher;
    public CustomizationSwitcher faceTypeSwitcher;
    public CustomizationSwitcher hairStyleSwitcher;
    public CustomizationSwitcher hairColorSwitcher;
    public CustomizationSwitcher skinColorSwitcher;
    public CustomizationSwitcher eyeColorSwitcher;
    public CustomizationSwitcher facialHairSwitcher;
    public CustomizationSwitcher bustSizeSwitcher;

    [Header("Runtime")]
    public bool isMale = true;
    public CharacterCustomization characterCustom;
    public CustomizationData customizationInfo = new CustomizationData();

    private void Start()
    {
        InitSwitchers();

        if (customizationInfo == null)
            customizationInfo = new CustomizationData();

        customizationInfo.IsMale = isMale;
        customizationInfo.SyncGenderFromBool();

        SetSwitchersFromData();
        RefreshActivePreviewCharacter();
        ApplyCurrentCustomization();
        RefreshGenderExclusiveUI();
    }

    private void InitSwitchers()
    {
        genderSwitcher?.Init(OnSwitcherChanged);
        faceTypeSwitcher?.Init(OnSwitcherChanged);
        hairStyleSwitcher?.Init(OnSwitcherChanged);
        hairColorSwitcher?.Init(OnSwitcherChanged);
        skinColorSwitcher?.Init(OnSwitcherChanged);
        eyeColorSwitcher?.Init(OnSwitcherChanged);
        facialHairSwitcher?.Init(OnSwitcherChanged);
        bustSizeSwitcher?.Init(OnSwitcherChanged);
    }

    private void OnSwitcherChanged(EditPartType type, int contentId)
    {
        switch (type)
        {
            case EditPartType.Sex:
                customizationInfo.GenderId = contentId;
                customizationInfo.SyncBoolFromGender();
                isMale = customizationInfo.IsMale;

                RefreshActivePreviewCharacter();
                RefreshGenderExclusiveUI();
                break;

            case EditPartType.FaceType:
                customizationInfo.FaceTypeId = contentId;
                break;

            case EditPartType.HairStyle:
                customizationInfo.HairStyleId = contentId;
                break;

            case EditPartType.HairColor:
                customizationInfo.HairColorId = contentId;
                break;

            case EditPartType.Skin:
                customizationInfo.SkinColorId = contentId;
                break;

            case EditPartType.EyeColor:
                customizationInfo.EyeColorId = contentId;
                break;

            case EditPartType.FacialHair:
                customizationInfo.FacialHairId = contentId;
                break;

            case EditPartType.BustSize:
                customizationInfo.BustSizeId = contentId;
                break;
        }

        ApplyCurrentCustomization();
    }

    private void RefreshActivePreviewCharacter()
    {
        if (character_M != null)
            character_M.SetActive(isMale);

        if (character_F != null)
            character_F.SetActive(!isMale);

        GameObject activeObject = isMale ? character_M : character_F;

        if (activeObject != null)
            characterCustom = activeObject.GetComponent<CharacterCustomization>();

        if (characterCustom != null)
            characterCustom.isMale = isMale;
    }

    private void RefreshGenderExclusiveUI()
    {
        if (FacialHairUI != null)
            FacialHairUI.SetActive(isMale);

        if (BustSizeUI != null)
            BustSizeUI.SetActive(!isMale);

        if (facialHairSwitcher != null)
            facialHairSwitcher.gameObject.SetActive(isMale);

        if (bustSizeSwitcher != null)
            bustSizeSwitcher.gameObject.SetActive(!isMale);
    }

    private void ApplyCurrentCustomization()
    {
        if (characterCustom == null) return;
        characterCustom.ApplyCustomization(customizationInfo);
    }

    private void SetSwitchersFromData()
    {
        genderSwitcher?.SetContentId(customizationInfo.GenderId);
        faceTypeSwitcher?.SetContentId(customizationInfo.FaceTypeId);
        hairStyleSwitcher?.SetContentId(customizationInfo.HairStyleId);
        hairColorSwitcher?.SetContentId(customizationInfo.HairColorId);
        skinColorSwitcher?.SetContentId(customizationInfo.SkinColorId);
        eyeColorSwitcher?.SetContentId(customizationInfo.EyeColorId);
        facialHairSwitcher?.SetContentId(customizationInfo.FacialHairId);
        bustSizeSwitcher?.SetContentId(customizationInfo.BustSizeId);
    }

    public void ResetCustomization()
    {
        customizationInfo = new CustomizationData
        {
            IsMale = true,
            GenderId = 1,
            FaceTypeId = 1,
            HairStyleId = 1,
            HairColorId = 1,
            SkinColorId = 1,
            EyeColorId = 1,
            FacialHairId = 0,
            BustSizeId = 2
        };

        isMale = true;

        SetSwitchersFromData();
        RefreshActivePreviewCharacter();
        RefreshGenderExclusiveUI();
        ApplyCurrentCustomization();
    }

    public void RandomizeCustomization()
    {
        bool male = Random.value < 0.5f;

        customizationInfo = new CustomizationData
        {
            IsMale = male,
            GenderId = male ? 1 : 2,

            FaceTypeId = faceTypeSwitcher != null ? faceTypeSwitcher.GetRandomContentId() : 1,
            HairStyleId = hairStyleSwitcher != null ? hairStyleSwitcher.GetRandomContentId() : 1,
            HairColorId = hairColorSwitcher != null ? hairColorSwitcher.GetRandomContentId() : 1,
            SkinColorId = skinColorSwitcher != null ? skinColorSwitcher.GetRandomContentId() : 1,
            EyeColorId = eyeColorSwitcher != null ? eyeColorSwitcher.GetRandomContentId() : 1,

            FacialHairId = male && facialHairSwitcher != null ? facialHairSwitcher.GetRandomContentId() : 0,
            BustSizeId = !male && bustSizeSwitcher != null ? bustSizeSwitcher.GetRandomContentId() : 2
        };

        isMale = male;

        SetSwitchersFromData();
        RefreshActivePreviewCharacter();
        RefreshGenderExclusiveUI();
        ApplyCurrentCustomization();
    }
}