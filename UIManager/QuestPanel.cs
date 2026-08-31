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

    // 외부로 던지는 이벤트(파티 구성창으로 전환)
    public System.Action<QuestDef> OnAcceptRequest;

    // 캐싱
    private readonly List<QuestPanelItem> _cards = new();

    void OnEnable()
    {
        // 이벤트 구독
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnBoardChanged += Rebuild;
        }

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnClickRefresh);

        // 초기 빌드
        Rebuild();
    }

    void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnBoardChanged -= Rebuild;
        }
        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(OnClickRefresh);
    }

    void OnClickRefresh()
    {
        // 테스트용 수동 갱신(실제 게임에서는 귀환 시 GameManager에서 호출)
        QuestManager.Instance.RefreshBoard(
            targetCount: 8
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

        foreach (var entry in list)
        {
            var card = Instantiate(cardPrefab, cardContainer);
            card.Bind(entry,
                onToggleReserve: HandleToggleReserve,
                onAccept: HandleAccept
            );
            _cards.Add(card);
        }
    }

    void HandleToggleReserve(QuestBoardEntry entry)
    {
        QuestManager.Instance.ReserveToggle(entry.def.id);

        // 전체 Rebuild
        Rebuild();

        // 필요 시 저장(상층 플레이어 매니저에서 SavePlayerDataToPlayFab 호출)
    }

    void HandleAccept(QuestBoardEntry entry)
    {
        // 파티 구성 화면으로 넘겨 상층에서
        // QuestManager.Instance.Accept(questId, leaderId, partyIds) 호출하도록 위임
        OnAcceptRequest?.Invoke(entry.def);
    }
}
