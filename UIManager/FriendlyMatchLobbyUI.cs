using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendlyMatchLobbyUI : MonoBehaviour
{
    public Button openButton;
    public GameObject wagerPanel;
    public Button closeButton;
    public GameObject controls;
    public Button sizeButton;
    public Button teamModeButton;
    public Button teamButton;
    public Button lockButton;
    public Button acceptButton;
    public TMP_Text settingsText;
    public TMP_Text offersText;
    public Transform inventoryContent;
    public Transform offerContent;
    public InventoryItemSlotUI itemTemplate;
    public GameObject resultPanel;
    public GameObject victoryTitle;
    public GameObject defeatTitle;
    public TMP_Text resultStatus;
    public Button rematchButton;
    public Button townButton;
    private string resultExpeditionId;
    private bool resultChoiceSent;
    private readonly List<InventoryItemSlotUI> slots = new();
    private MultiplayerSession session;

    private void Start()
    {
        session = MultiplayerSession.Instance;
        closeButton.onClick.AddListener(Close);
        sizeButton.onClick.AddListener(ChangeSize);
        teamModeButton.onClick.AddListener(ChangeTeamMode);
        teamButton.onClick.AddListener(ChangeTeam);
        lockButton.onClick.AddListener(Lock);
        acceptButton.onClick.AddListener(Accept);
        session.FormationChanged += Refresh;
        session.ExpeditionChanged += Refresh;
        SharedInventoryUtility.InventoryChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (session != null) session.FormationChanged -= Refresh;
        if (session != null) session.ExpeditionChanged -= Refresh;
        SharedInventoryUtility.InventoryChanged -= Refresh;
    }

    public void Open()
    {
        if (!session.IsFriendlyRoom || session.InExpedition) return;
        Canvas wagerCanvas = wagerPanel.GetComponent<Canvas>();
        wagerCanvas.overrideSorting = true;
        wagerCanvas.sortingOrder = 50;
        wagerPanel.SetActive(true);
        wagerPanel.transform.SetAsLastSibling();
        Refresh();
    }
    private void Close() => wagerPanel.SetActive(false);
    private void ChangeSize() => session.SendFriendlyCommand("size", session.Formation.friendly.teamSize % 5 + 1);
    private void ChangeTeamMode() => session.SendFriendlyCommand("teamMode", session.Formation.friendly.multiplePlayersPerTeam ? 0 : 1);
    private void ChangeTeam() => session.SendFriendlyCommand("team", 1 - session.FriendlyTeam(session.LocalProfileId));
    private void Lock() => session.SendFriendlyCommand("lock");
    private void Accept() => session.SendFriendlyCommand("accept");

    public void ShowResult(MultiplayerSession.ExpeditionState state)
    {
        if (resultExpeditionId == state.id) return;
        resultExpeditionId = state.id;
        resultChoiceSent = false;
        bool won = state.friendly.participants.Exists(p =>
            p.ownerId == session.LocalProfileId && p.team == state.friendly.winningTeam);
        victoryTitle.SetActive(won);
        defeatTitle.SetActive(!won);
        System.Action showChoices = () =>
        {
            if (!session.IsFriendlyMatch || session.Expedition.id != state.id) return;
            session.ConfirmFriendlyLoot(state.id);
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
            Refresh();
        };
        var loot = state.claimedLoot.Where(item => item.ownerId == session.LocalProfileId)
            .Select(item => item.item).ToList();
        if (!won || !UIManager.Instance.OpenBattleLoot(loot, 0, showChoices, true))
            showChoices();
    }

    public void Rematch()
    {
        resultChoiceSent = true;
        session.ChooseFriendlyResult(true);
        Refresh();
    }

    public void ReturnToTown()
    {
        resultChoiceSent = true;
        session.ChooseFriendlyResult(false);
        Refresh();
    }

    private void Refresh()
    {
        if (!session.IsFriendlyMatch)
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            resultExpeditionId = null;
        }
        else if (resultPanel != null && resultPanel.activeSelf)
        {
            var result = session.Expedition.friendly;
            rematchButton.interactable = !resultChoiceSent && !result.returnToTown;
            townButton.interactable = !result.returnToTown;
            resultStatus.text = result.returnToTown ? "내기 정산 확인이 끝나면 마을로 돌아갑니다." :
                $"재경기 동의 {result.rematchVotes.Count}/{session.Expedition.players.Count}" +
                (result.resultReady ? "" : "\n승리자의 내기 아이템 확인을 기다리고 있습니다.");
        }
        controls.SetActive(session.IsFriendlyRoom && !session.InExpedition);
        if (controls.activeSelf)
            openButton.transform.parent.SetAsLastSibling();
        foreach (var slot in slots) { slot.gameObject.SetActive(false); Destroy(slot.gameObject); }
        slots.Clear();
        if (!controls.activeSelf) { Close(); return; }
        var settings = session.Formation.friendly;
        var own = settings.participants.Find(p => p.ownerId == session.LocalProfileId);
        settingsText.text = $"팀당 {settings.teamSize}명 · " + (settings.multiplePlayersPerTeam ? "다인 팀전" : "용병단 1 vs 1");
        sizeButton.interactable = teamModeButton.interactable = session.IsHost;
        teamButton.interactable = own != null && !own.locked;
        lockButton.interactable = own != null;
        SetButtonText(sizeButton, $"팀당 {settings.teamSize}명");
        SetButtonText(teamModeButton, settings.multiplePlayersPerTeam ? "다인 팀전" : "1 대 1");
        SetButtonText(teamButton, own == null ? "진영" : $"{own.team + 1}팀");
        SetButtonText(lockButton, own?.locked == true ? "잠금 해제" : "내기 잠금");
        acceptButton.interactable = own != null && settings.participants.All(p => p.locked);
        SetButtonText(acceptButton, own?.accepted == true ? "수락 취소" : "최종 수락");
        offersText.text = "양 팀의 내기 · 가치는 자유롭게 합의\n";
        foreach (var p in settings.participants.OrderBy(p => p.team))
        {
            string name = session.Formation.members.Find(m => m.ownerId == p.ownerId)?.name ?? "참가자";
            offersText.text += $"\n[{p.team + 1}팀] {name} · {(p.accepted ? "수락" : p.locked ? "잠금" : "편집 중")}\n";
            offersText.text += p.stake.Count == 0 ? "내기 없음\n" : string.Join(", ", p.stake.Select(item =>
                $"{GameDataRegistry.Instance.GetItem(item.itemUid).itemName} ×{item.count}")) + "\n";
            foreach (var item in p.stake)
            {
                var view = Instantiate(itemTemplate, offerContent);
                view.gameObject.SetActive(true);
                view.Bind(item);
                if (view.nameText != null) view.nameText.text = $"{p.team + 1}팀 · {name}";
                slots.Add(view);
            }
        }
        if (own == null) return;
        foreach (var item in PlayerManager.Instance.GetCurrentPlayerData().accountStorage.Where(item => item != null && item.count > 0))
        {
            var view = Instantiate(itemTemplate, inventoryContent);
            view.gameObject.SetActive(true);
            bool selected = own.stake.Any(offer => Newtonsoft.Json.JsonConvert.SerializeObject(offer) == Newtonsoft.Json.JsonConvert.SerializeObject(item));
            view.Bind(item, (clicked, button) =>
            {
                if (own.locked) return;
                var next = new List<InventorySlotData>(own.stake);
                if (selected) next.RemoveAll(offer => Newtonsoft.Json.JsonConvert.SerializeObject(offer) == Newtonsoft.Json.JsonConvert.SerializeObject(clicked));
                else next.Add(clicked);
                session.SendFriendlyCommand("stake", stake: next);
            });
            view.SetSelected(selected);
            slots.Add(view);
        }
    }

    private static void SetButtonText(Button button, string value)
    {
        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = value;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null) legacyText.text = value;
    }
}
