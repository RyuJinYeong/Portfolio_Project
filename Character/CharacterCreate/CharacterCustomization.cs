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

    public GameObject[] outFit;
    public GameObject[] Weapon;

    public Sprite characterPortrait { get; set; }

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

    public void SetOutfit(int index) 
    {
        ActivateModelFromArray(outFit, index);
    }

    public void SetWeapon(int index)
    {
        ActivateModelFromArray(Weapon, index);
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
