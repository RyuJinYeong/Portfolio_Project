using InfinityPBR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCustomizationUI : MonoBehaviour
{
    public Toggle maleToggle;
    public Toggle femaleToggle;
    public bool isMale;

    public TextMeshProUGUI bodyTypeText;
    public TextMeshProUGUI hairstyleText;
    public TextMeshProUGUI hairColorText;
    public TextMeshProUGUI skinTonesText;
    public TextMeshProUGUI voiceText;

    public CustomizationData customizationInfo = new CustomizationData();

    public CharacterCustomization characterCustom;

    private string[] bodyTypes = { "Weak", "Normal", "Strong" };
    private string[] hairStyles = { "Hair 1", "Hair 2", "Hair 3", "Hair 4", "Hair 5", "Hair 6", "Hair 7", "Hair 8", "Hair 9", "Hair 10", "Hair 11", "Hair 12", "No Hair"};
    private string[] hairColors = { "Hair 1", "Hair 2", "Hair 3", "Hair 4", "Hair 5", "Hair 6", "Hair 7", "Hair 8", "Hair 9", "Hair 10", "Hair 11", "Hair 12" };
    private string[] skinTones = { "Skin 1", "Skin 2", "Skin 3", "Skin 4", "Skin 5", "Skin 6" };
    private string[] voices = { "1", "2", "3" };

    private int currentBodyTypeIndex = 0;
    private int currentHairstyleIndex = 0;
    private int currentHairColorIndex = 0;
    private int currentFaceIndex = 0;
    private int currentVoiceIndex = 0;

    void Start()
    {
        maleToggle.onValueChanged.AddListener(OnMaleToggleChanged);
        femaleToggle.onValueChanged.AddListener(OnFemaleToggleChanged);

        UpdateUI();
    }

    void OnMaleToggleChanged(bool isOn) // Inspector의 Toggle On value Changed에서 변경된 값에 해당되는 string값 전달
    {
        if (isOn)
        {
            femaleToggle.isOn = false;
            isMale = true;
        }
    }

    void OnFemaleToggleChanged(bool isOn) // Inspector의 Toggle On value Changed에서 변경된 값에 해당되는 string값 전달
    {
        if (isOn)
        {
            maleToggle.isOn = false;
            isMale = false;
        }
    }

    public void NextBodyType()
    {
        currentBodyTypeIndex = (currentBodyTypeIndex + 1) % bodyTypes.Length;
        UpdateUI();
    }

    public void PreviousBodyType()
    {
        currentBodyTypeIndex = (currentBodyTypeIndex - 1 + bodyTypes.Length) % bodyTypes.Length;
        UpdateUI();
    }

    public void NextHairstyle()
    {
        currentHairstyleIndex = (currentHairstyleIndex + 1) % hairStyles.Length;
        UpdateUI();
    }

    public void PreviousHairstyle()
    {
        currentHairstyleIndex = (currentHairstyleIndex - 1 + hairStyles.Length) % hairStyles.Length;
        UpdateUI();
    }

    public void NextHairColor()
    {
        currentHairColorIndex = (currentHairColorIndex + 1) % hairColors.Length;
        UpdateUI();
    }

    public void PreviousHairColor()
    {
        currentHairColorIndex = (currentHairColorIndex - 1 + hairColors.Length) % hairColors.Length;
        UpdateUI();
    }

    public void NextskinTones()
    {
        currentFaceIndex = (currentFaceIndex + 1) % skinTones.Length;
        UpdateUI();
    }

    public void PreviousskinTones()
    {
        currentFaceIndex = (currentFaceIndex - 1 + skinTones.Length) % skinTones.Length;
        UpdateUI();
    }

    public void NextVoice()
    {
        currentVoiceIndex = (currentVoiceIndex + 1) % voices.Length;
        UpdateUI();
    }

    public void PreviousVoice()
    {
        currentVoiceIndex = (currentVoiceIndex - 1 + voices.Length) % voices.Length;
        UpdateUI();
    }

    void UpdateUI()
    {
        bodyTypeText.text = bodyTypes[currentBodyTypeIndex];
        hairstyleText.text = "Hairstyle: " + hairStyles[currentHairstyleIndex];
        hairColorText.text = "Hair Color: " + hairColors[currentHairColorIndex];
        skinTonesText.text = "Skin Tone: " + skinTones[currentFaceIndex];
        voiceText.text = "Voice: " + voices[currentVoiceIndex];

        customizationInfo.HairColor = hairColors[currentHairColorIndex];
        customizationInfo.HairStyle = hairStyles[currentHairstyleIndex];
        customizationInfo.BodyType = bodyTypes[currentBodyTypeIndex];
        customizationInfo.SkinTone = skinTones[currentFaceIndex];
        customizationInfo.IsMale = isMale;

        characterCustom.SetHairColor(hairColors[currentHairColorIndex]);
        characterCustom.SetHairStyle(hairStyles[currentHairstyleIndex]);
        characterCustom.SetSkinTone(skinTones[currentFaceIndex]);
        characterCustom.SetBodyType(bodyTypes[currentBodyTypeIndex]);
    }
}
