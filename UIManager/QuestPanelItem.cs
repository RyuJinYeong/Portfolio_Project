using SoftKitty.InventoryEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanelItem : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI bossText;
    public TextMeshProUGUI recommendText;   // 예: "권장 레벨 4 / 권장 3명 (최대 5명)"
    public TextMeshProUGUI rewardText;      // 예: "보상: 골드 100, 경험치 200" (미구현)
    public TextMeshProUGUI issuerText;      // 의뢰 주체 표기(간단)

    [Header("Buttons")]
    public Button reserveButton;
    public Button acceptButton;

    [Header("State Widgets")]
    public GameObject reservedBadge;        // 예약 상태 뱃지(선택)
    public Color reservedColor = new Color(0.9f, 0.9f, 1f);
    public Color normalColor = Color.white;

    // 내부 상태
    private QuestBoardEntry entry;
    private System.Action<QuestBoardEntry> onToggleReserve;
    private System.Action<QuestBoardEntry> onAccept;

    public void Bind(QuestBoardEntry e, System.Action<QuestBoardEntry> onToggleReserve, System.Action<QuestBoardEntry> onAccept)
    {
        entry = e;
        this.onToggleReserve = onToggleReserve;
        this.onAccept = onAccept;

        var def = entry.def;

        titleText.text = def.title;

        stageText.text = $"지역:";
        bossText.text = $"보스:";

        recommendText.text = $"권장 Lv {def.recommendedLevel} / 권장 {def.recommendedPartySize}명 (최대 {def.maxPartySize}명)";
        issuerText.text = $"의뢰: {def.issuer}";

        // 예약 상태 반영
        ApplyReservedVisual(entry.status == QuestStatus.Reserved);

        // 버튼 리스너 갱신
        reserveButton.onClick.RemoveAllListeners();
        acceptButton.onClick.RemoveAllListeners();

        reserveButton.onClick.AddListener(() => {
            onToggleReserve?.Invoke(entry);
        });

        acceptButton.onClick.AddListener(() => {
            // 여기서는 파티 구성 화면으로 넘기는 신호만 보냄
            onAccept?.Invoke(entry);
        });
    }

    public void ApplyReservedVisual(bool isReserved)
    {
        if (reservedBadge) reservedBadge.SetActive(isReserved);

        var reserveLabel = reserveButton?.GetComponentInChildren<TextMeshProUGUI>();
        if (reserveLabel) reserveLabel.text = isReserved ? "예약 취소" : "예약";
    }
}
