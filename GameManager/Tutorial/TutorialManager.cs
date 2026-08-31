using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public GameObject dialogueUI;
    public GameObject tutorialQuestUI;

    private void Start()
    {
        ShowTutorial();
    }

    public void OnTutorialCompleted()
    {
        // 튜토리얼 완료
    }

    private void ShowTutorial()
    {
        dialogueUI.SetActive(true);
        tutorialQuestUI.SetActive(true);

        // 대화 및 튜토리얼 퀘스트 로직 구현
    }
}
