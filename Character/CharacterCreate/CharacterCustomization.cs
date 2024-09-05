using InfinityPBR;
using UnityEngine;

public class CharacterCustomization : MonoBehaviour
{
    public RenderTexture portraitRenderTexture;
    public Camera portraitCamera;

    public GameObject characterRoot;
    public GameObject characterHair;
    public GameObject characterBody;

    public Sprite characterPortrait { get; set; }

    public void SetHairStyle(string hairStyle)
    {
        characterRoot.GetComponent<PrefabAndObjectManager>().ActivateGroup(hairStyle); // hairStyle 업데이트
    }

    public void SetHairColor(string hairColor)
    {
        characterHair.GetComponent<ColorShiftRuntime>().SetColorSet(hairColor); // hairColor 업데이트
    }

    public void SetSkinTone(string skinTone)
    {
        characterBody.GetComponent<ColorShiftRuntime>().SetColorSet(skinTone); // skinTones 업데이트
    }

    public void SetBodyType(string bodyType) // 성별, 몸체 타입, 종족, Random Face, Default Face를 매개변수로 받음.
    {
        characterRoot.GetComponent<BlendShapesPresetManager>().ActivatePreset(bodyType); // bodyTypes 업데이트
    }

    public void SetOutfit(string outfit)
    {
        // outfit에 따른 옷 오브젝트 변경 로직
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
}
