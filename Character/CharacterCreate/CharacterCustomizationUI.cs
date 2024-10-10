using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCustomizationUI : MonoBehaviour
{
    public Toggle maleToggle;
    public Toggle femaleToggle;

    public bool isMale;

    public GameObject character_M;
    public GameObject character_F;

    public TextMeshProUGUI hairTypeText;
    public TextMeshProUGUI eyebrowsTypeText;
    public TextMeshProUGUI eyeTypeText;
    public TextMeshProUGUI mouthTypeText;    
    public TextMeshProUGUI hairColorText;
    public TextMeshProUGUI skinTonesText;
    public TextMeshProUGUI beardTypeText;

    public GameObject BeardUI; // 수염 옵션 UI

    public CharacterCustomization characterCustom;

    public CustomizationData customizationInfo = new CustomizationData();

    private int currentEyebrowIndex = 0;
    private int currentEyesIndex = 0;
    private int currentMouthIndex = 0;
    private int currentHairTypeIndex = 0;
    private int currentBeardIndex = 0; 
    private int currentHairColorIndex = 0;
    private int currentSkinToneIndex = 0;
        
    // 커스터마이징 가능한 항목들
    private string[] hairTypes = { "Hair 1", "Hair 2", "Hair 3", "Hair 4", "Hair 5", "Hair 6", "Hair 7", "Hair 8", "Hair 9", "Hair 10", "Hair 11" };
    private string[] eyebrowsTypes = { "Eyebrow 1", "Eyebrow 2", "Eyebrow 3", "Eyebrow 4", "Eyebrow 5" };
    private string[] eyesTypes = { "Eye 1", "Eye 2", "Eye 3", "Eye 4", "Eye 5" };
    private string[] mouthTypes = { "Mouth 1", "Mouth 2", "Mouth 3", "Mouth 4", "Mouth 5" };
    private string[] beardTypes = { "Beard 1", "Beard 2", "Beard 3", "Beard 4", "Beard 5", "Beard 6", "Beard 7", "Beard 8" };
    private string[] hairColors = { "HairColor 1", "HairColor 2", "HairColor 3", "HairColor 4", "HairColor 5" };
    private string[] skinTones = { "SkinTone 1", "SkinTone 2", "SkinTone 3", "SkinTone 4", "SkinTone 5" };

    void Start()
    {
        maleToggle.onValueChanged.AddListener(OnMaleToggleChanged);
        femaleToggle.onValueChanged.AddListener(OnFemaleToggleChanged);
        UpdateUI();
        characterCustom = character_M.GetComponent<CharacterCustomization>();
    }

    // 성별 선택 토글 로직
    void OnMaleToggleChanged(bool isOn)
    {
        if (isOn)
        {
            characterCustom = character_M.GetComponent<CharacterCustomization>();

            isMale = true;
            femaleToggle.isOn = false;
            characterCustom.isMale = true; // 커스터마이징 스크립트에 반영
            character_F.SetActive(false);
            character_M.SetActive(true);
            BeardUI.SetActive(true); // 수염 옵션 활성화
            UpdateUI();            
        }
    }

    void OnFemaleToggleChanged(bool isOn)
    {
        if (isOn)
        {
            characterCustom = character_F.GetComponent<CharacterCustomization>();

            isMale = false;
            maleToggle.isOn = false;
            characterCustom.isMale = false; // 커스터마이징 스크립트에 반영
            character_F.SetActive(true);
            character_M.SetActive(false);
            BeardUI.SetActive(false); // 수염 옵션 비활성화
            UpdateUI();
        }
    }

    // 헤어 스타일 변경
    public void NextHairStyle()
    {
        ChangeIndex(ref currentHairTypeIndex, hairTypes.Length);
        UpdateUI();
        characterCustom.SetHairStyle(currentHairTypeIndex); // 커스터마이징에 반영
    }

    public void NextEyebrows()
    {
        ChangeIndex(ref currentEyebrowIndex, eyebrowsTypes.Length);
        UpdateUI();
        characterCustom.SetEyebrows(currentEyebrowIndex);
    }

    public void NextEyes()
    {
        ChangeIndex(ref currentEyesIndex, eyesTypes.Length);
        UpdateUI();
        characterCustom.SetEyes(currentEyesIndex);
    }

    public void NextMouth()
    {
        ChangeIndex(ref currentMouthIndex, mouthTypes.Length);
        UpdateUI();
        characterCustom.SetMouth(currentMouthIndex);
    }

    public void NextBeard()
    {
        if (isMale)
        {
            ChangeIndex(ref currentBeardIndex, beardTypes.Length);
            UpdateUI();
            characterCustom.SetBeard(currentBeardIndex);
        }
    }

    public void NextHairColor()
    {
        ChangeIndex(ref currentHairColorIndex, hairColors.Length);
        UpdateUI();
        characterCustom.SetHairColor(currentHairColorIndex);
    }

    public void NextSkinTone()
    {
        ChangeIndex(ref currentSkinToneIndex, skinTones.Length);
        UpdateUI();
        characterCustom.SetSkinTone(currentSkinToneIndex);
    }

    // 인덱스 변경 로직
    private void ChangeIndex(ref int index, int length)
    {
        index = (index + 1) % length;
    }

    // 헤어 스타일 이전으로
    public void PreviousHairStyle()
    {
        ChangeIndexBackward(ref currentHairTypeIndex, hairTypes.Length);
        UpdateUI();
        characterCustom.SetHairStyle(currentHairTypeIndex); // 커스터마이징에 반영
    }

    // 눈썹 이전으로
    public void PreviousEyebrows()
    {
        ChangeIndexBackward(ref currentEyebrowIndex, eyebrowsTypes.Length);
        UpdateUI();
        characterCustom.SetEyebrows(currentEyebrowIndex);
    }

    // 눈 이전으로
    public void PreviousEyes()
    {
        ChangeIndexBackward(ref currentEyesIndex, eyesTypes.Length);
        UpdateUI();
        characterCustom.SetEyes(currentEyesIndex);
    }

    // 입 이전으로
    public void PreviousMouth()
    {
        ChangeIndexBackward(ref currentMouthIndex, mouthTypes.Length);
        UpdateUI();
        characterCustom.SetMouth(currentMouthIndex);
    }

    // 수염 이전으로 (남성일 때만)
    public void PreviousBeard()
    {
        if (isMale)
        {
            ChangeIndexBackward(ref currentBeardIndex, beardTypes.Length);
            UpdateUI();
            characterCustom.SetBeard(currentBeardIndex);
        }
    }

    // 머리 색상 이전으로
    public void PreviousHairColor()
    {
        ChangeIndexBackward(ref currentHairColorIndex, hairColors.Length);
        UpdateUI();
        characterCustom.SetHairColor(currentHairColorIndex);
    }

    // 피부 색상 이전으로
    public void PreviousSkinTone()
    {
        ChangeIndexBackward(ref currentSkinToneIndex, skinTones.Length);
        UpdateUI();
        characterCustom.SetSkinTone(currentSkinToneIndex);
    }

    // 인덱스 감소 로직
    private void ChangeIndexBackward(ref int index, int length)
    {
        index = (index - 1 + length) % length;
    }

    // UI 업데이트 로직
    void UpdateUI()
    {
        hairTypeText.text = hairTypes[currentHairTypeIndex];
        eyebrowsTypeText.text = eyebrowsTypes[currentEyebrowIndex];
        eyeTypeText.text = eyesTypes[currentEyesIndex];
        mouthTypeText.text = mouthTypes[currentMouthIndex];
        beardTypeText.text = beardTypes[currentBeardIndex];
        hairColorText.text = hairColors[currentHairColorIndex];
        skinTonesText.text = skinTones[currentSkinToneIndex];

        customizationInfo.IsMale = isMale;

        customizationInfo.HairType = currentHairTypeIndex;
        customizationInfo.EyebrowsType = currentEyebrowIndex;
        customizationInfo.EyeType = currentEyesIndex;
        customizationInfo.MouthType = currentMouthIndex;        
        customizationInfo.BeardType = currentBeardIndex;

        customizationInfo.HairColor = currentHairColorIndex;
        customizationInfo.SkinTone = currentSkinToneIndex;
    }
}
