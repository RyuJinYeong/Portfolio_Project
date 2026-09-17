using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    [Header("Wiring")]
    public Transform cardContainer;         // 카드가 들어갈 부모(Grid/Vertical Layout)
    public QuestPanelItem cardPrefab;            // 카드 프리팹

    [Header("Toolbar")]
    public Button refreshButton;            // 귀환 후 수동 새로고침 용(테스트)
    public Button previousPageButton;
    public Button nextPageButton;
    public TextMeshProUGUI pageText;
    public Button preferenceButton;
    public Button abandonButton;
    public GameObject preferencePanel;

    [Header("Detail")]
    public QuestPanelItem detailView;

    // 외부로 던지는 이벤트(파티 구성창으로 전환)
    public System.Action<QuestDef> OnAcceptRequest;

    // 캐싱
    private readonly List<QuestPanelItem> _cards = new();
    private int currentPage;
    private string selectedQuestId;

    void OnEnable()
    {
        if (abandonButton == null)
        {
            Transform abandonButtonTransform =
                transform.Find("QuestBoardLayout/AbandonQuestButton");

            if (abandonButtonTransform != null)
                abandonButton = abandonButtonTransform.GetComponent<Button>();
        }

        // 이벤트 구독
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnBoardChanged += Rebuild;
            QuestManager.Instance.OnActiveChanged += UpdateAbandonButton;
        }

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnClickRefresh);

        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(OnClickPreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(OnClickNextPage);

        if (preferenceButton != null)
            preferenceButton.onClick.AddListener(OnClickPreference);

        if (abandonButton != null)
            abandonButton.onClick.AddListener(OnClickAbandon);

        // 초기 빌드
        Rebuild();
        UpdateAbandonButton();
    }

    void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnBoardChanged -= Rebuild;
            QuestManager.Instance.OnActiveChanged -= UpdateAbandonButton;
        }
        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(OnClickRefresh);

        if (previousPageButton != null)
            previousPageButton.onClick.RemoveListener(OnClickPreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.RemoveListener(OnClickNextPage);

        if (preferenceButton != null)
            preferenceButton.onClick.RemoveListener(OnClickPreference);

        if (abandonButton != null)
            abandonButton.onClick.RemoveListener(OnClickAbandon);
    }

    void OnClickRefresh()
    {
        // 테스트용 수동 갱신(실제 게임에서는 귀환 시 GameManager에서 호출)
        QuestManager.Instance.RefreshBoard(
            targetCount: QuestManager.BoardQuestCount
        );
        // Save 호출은 상층(플레이어 매니저)에서 일괄로 해도 됨
    }

    void ClearCards()
    {
        foreach (var c in _cards)
        {
            if (c) Destroy(c.gameObject);
        }
        _cards.Clear();
    }

    void Rebuild()
    {
        if (QuestManager.Instance == null) return;

        ClearCards();

        var list = QuestManager.Instance.board;

        int pageCount = GetPageCount();
        currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);

        int startIndex = currentPage * QuestManager.BoardPageSize;
        int endIndex = Mathf.Min(
            startIndex + QuestManager.BoardPageSize,
            list.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            var entry = list[i];
            var card = Instantiate(cardPrefab, cardContainer);
            card.Bind(entry,
                onToggleReserve: HandleToggleReserve,
                onAccept: HandleAccept,
                onSelect: HandleSelect
            );
            _cards.Add(card);
        }

        if (pageText != null)
            pageText.text = $"{currentPage + 1} / {pageCount}";

        if (previousPageButton != null)
            previousPageButton.interactable = currentPage > 0;

        if (nextPageButton != null)
            nextPageButton.interactable = currentPage < pageCount - 1;

        QuestBoardEntry selectedEntry = list.Find(
            entry =>
                entry != null &&
                entry.def != null &&
                entry.def.id == selectedQuestId);

        if (selectedEntry == null && list.Count > 0)
        {
            selectedEntry = list[0];
            selectedQuestId = selectedEntry.def.id;
        }

        BindDetail(selectedEntry);
    }

    int GetPageCount()
    {
        int count = QuestManager.Instance != null &&
                    QuestManager.Instance.board != null
            ? QuestManager.Instance.board.Count
            : 0;

        return Mathf.Max(
            1,
            Mathf.CeilToInt(
                count / (float)QuestManager.BoardPageSize));
    }

    void HandleToggleReserve(QuestBoardEntry entry)
    {
        QuestManager.Instance.ReserveToggle(entry.def.id);

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.SavePlayerDataToPlayFab();
    }

    void OnClickPreviousPage()
    {
        if (currentPage <= 0)
            return;

        currentPage--;
        Rebuild();
    }

    void OnClickNextPage()
    {
        int pageCount = GetPageCount();

        if (currentPage >= pageCount - 1)
            return;

        currentPage++;
        Rebuild();
    }

    void OnClickPreference()
    {
        if (preferencePanel != null)
            preferencePanel.SetActive(true);
    }

    void OnClickAbandon()
    {
        MultiplayerSession.Instance.Open();
    }

    void UpdateAbandonButton()
    {
        if (abandonButton != null)
        {
            abandonButton.gameObject.SetActive(true);
            abandonButton.interactable = QuestManager.Instance != null && QuestManager.Instance.active == null;
        }
    }

    void HandleAccept(QuestBoardEntry entry)
    {
        // 파티 구성 화면으로 넘겨 상층에서
        // QuestManager.Instance.Accept(questId, leaderId, partyIds) 호출하도록 위임
        OnAcceptRequest?.Invoke(entry.def);
    }

    void HandleSelect(QuestBoardEntry entry)
    {
        if (entry == null || entry.def == null)
            return;

        selectedQuestId = entry.def.id;
        BindDetail(entry);
    }

    void BindDetail(QuestBoardEntry entry)
    {
        if (detailView == null)
            return;

        detailView.gameObject.SetActive(entry != null);

        if (entry == null)
            return;

        detailView.Bind(
            entry,
            HandleToggleReserve,
            HandleAccept,
            null);
    }
}
