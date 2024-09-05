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
        // 튜토리얼 완료 후 스테이지 선택 UI 활성화
        GameManager.Instance.ShowStageSelectionUI();
    }

    private void ShowTutorial()
    {
        dialogueUI.SetActive(true);
        tutorialQuestUI.SetActive(true);

        // 대화 및 튜토리얼 퀘스트 로직 구현
    }

    public void OnTutorialQuestCompleted()
    {
        // 퀘스트 완료 후 튜토리얼 종료
        OnTutorialCompleted();
    }
}
