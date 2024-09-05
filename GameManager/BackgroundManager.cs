using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    public List<GameObject> forestBackgrounds;
    public List<GameObject> dungeonBackgrounds;
    public List<GameObject> tutorialBackgrounds;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetActiveBackground(string backgroundType)
    {
        DeactivateAllBackgrounds();

        switch (backgroundType)
        {
            case "Forest":
                ActivateRandomBackground(forestBackgrounds);
                break;
            case "Dungeon":
                ActivateRandomBackground(dungeonBackgrounds);
                break;
            case "Tutorial":
                ActivateRandomBackground(tutorialBackgrounds);
                break;
                // 다른 배경 타입도 추가 가능
        }
    }

    private void DeactivateAllBackgrounds()
    {
        foreach (var bg in forestBackgrounds)
        {
            bg.SetActive(false);
        }
        foreach (var bg in dungeonBackgrounds)
        {
            bg.SetActive(false);
        }
        foreach (var bg in tutorialBackgrounds)
        {
            bg.SetActive(false); 
        }
    }

    private void ActivateRandomBackground(List<GameObject> backgrounds)
    {
        if (backgrounds.Count > 0)
        {
            int index = Random.Range(0, backgrounds.Count);
            backgrounds[index].SetActive(true);
        }
    }
}
