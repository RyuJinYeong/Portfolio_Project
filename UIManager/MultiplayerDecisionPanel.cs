using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerDecisionPanel : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text description;
    public TMP_Text status;
    public Transform rows;
    public GameObject rowTemplate;
    public Button leaveButton;
    public Color[] playerColors;
    public AudioSource diceAudio;
    public AudioClip diceRollSound;
    public GameObject battlePanel;
    public TMP_Text battleStatus;
    public Button battleLeaveButton;

    private MultiplayerSession session;
    private readonly List<GameObject> spawnedRows = new();
    private string soundExpeditionId;
    private int lastDiceSequence;
    private BattleLootPanel sharedLootPanel;
    private int sharedLootDecisionId = -1;

    private void Start()
    {
        session = MultiplayerSession.Instance;
        if (session == null) return;
        session.ExpeditionChanged += Refresh;
        session.BattleChanged += RefreshBattle;
        leaveButton?.onClick.AddListener(session.LeaveExpedition);
        battleLeaveButton?.onClick.AddListener(session.LeaveExpedition);
        Refresh();
    }

    private void OnDestroy()
    {
        if (session == null) return;
        session.ExpeditionChanged -= Refresh;
        session.BattleChanged -= RefreshBattle;
        leaveButton?.onClick.RemoveListener(session.LeaveExpedition);
        battleLeaveButton?.onClick.RemoveListener(session.LeaveExpedition);
        CloseSharedLoot();
    }

    private void Refresh()
    {
        var expedition = session.Expedition;
        if (panel != null)
            panel.SetActive(false);
        RefreshBattle();
        ClearRows();
        if (!session.InExpedition || expedition == null || session.IsSharedBattle)
        {
            CloseSharedLoot();
            return;
        }
        if (soundExpeditionId == expedition.id && expedition.diceSequence > lastDiceSequence &&
            diceAudio != null && diceRollSound != null)
            diceAudio.PlayOneShot(diceRollSound);
        soundExpeditionId = expedition.id;
        lastDiceSequence = expedition.diceSequence;
        var decision = expedition.decision;
        if (decision == null)
        {
            CloseSharedLoot();
            return;
        }

        if (decision.kind == "loot")
        {
            ShowLootDecision(expedition, decision);
            return;
        }

        CloseSharedLoot();
        if (decision.kind == "route")
            ShowRouteDecision(expedition, decision);
        else
            ShowEncounterDecision(expedition, decision);
    }

    private void ShowEncounterDecision(
        MultiplayerSession.ExpeditionState expedition,
        MultiplayerSession.SharedDecision decision)
    {
        QuestNodeMapPanel map = UIManager.Instance?.questNodeMapPanel;
        if (map == null)
            return;

        QuestEncounterDialog dialog = map.GetEncounterDialog();
        if (!map.gameObject.activeInHierarchy || dialog == null || !dialog.gameObject.activeSelf)
            dialog = map.OpenSharedEncounter();
        if (dialog == null)
            return;

        dialog.ShowSharedDecision(decision.title, expedition.text);
        foreach (var option in decision.options)
        {
            GameObject row = Instantiate(rowTemplate, dialog.SharedChoiceContainer);
            row.SetActive(true);
            spawnedRows.Add(row);
            Button choose = row.transform.Find("Choice")?.GetComponent<Button>();
            if (choose == null)
                continue;
            choose.GetComponentInChildren<TMP_Text>().text = option.label;
            choose.interactable = !decision.rolling && decision.winner == null &&
                (decision.ownerId == null || decision.ownerId == session.LocalProfileId);
            string optionId = option.id;
            choose.onClick.AddListener(() => session.ChooseExpeditionOption(optionId));
            BuildVoters(row.transform.Find("Voters"), expedition, decision, option.id, false);
        }
    }

    private void ShowRouteDecision(
        MultiplayerSession.ExpeditionState expedition,
        MultiplayerSession.SharedDecision decision)
    {
        QuestNodeMapPanel map = UIManager.Instance?.questNodeMapPanel;
        if (map == null)
            return;

        map.SetSharedStatus(expedition.text);
        foreach (MultiplayerSession.SharedOption option in decision.options)
        {
            if (!int.TryParse(option.id, out int nodeId))
                continue;
            Transform node = map.GetRouteNodeTransform(nodeId);
            if (node == null)
                continue;

            GameObject sourceRow = Instantiate(rowTemplate, node);
            Transform voters = sourceRow.transform.Find("Voters");
            if (voters == null)
            {
                Destroy(sourceRow);
                continue;
            }
            voters.SetParent(node, false);
            voters.gameObject.name = "SharedVoters";
            voters.gameObject.SetActive(true);
            spawnedRows.Add(voters.gameObject);
            BuildVoters(voters, expedition, decision, option.id, true);
            Destroy(sourceRow);
        }
    }

    private void ShowLootDecision(
        MultiplayerSession.ExpeditionState expedition,
        MultiplayerSession.SharedDecision decision)
    {
        if (expedition.pendingLoot == null || expedition.pendingLoot.Count == 0)
            return;

        if (sharedLootPanel == null || sharedLootDecisionId != decision.id)
        {
            CloseSharedLoot();
            sharedLootDecisionId = decision.id;
            sharedLootPanel = UIManager.Instance?.OpenSharedBattleLoot(
                expedition.pendingLoot[0],
                decision.title,
                () => session.ChooseExpeditionOption("need"),
                () => session.ChooseExpeditionOption("pass"));
        }
        if (sharedLootPanel == null)
            return;

        MultiplayerSession.SharedBallot own = decision.ballots.Find(ballot =>
            ballot.ownerId == session.LocalProfileId && ballot.optionId != null);
        bool canChoose = !decision.rolling && decision.winner == null;
        sharedLootPanel.RefreshSharedDecision(
            decision.winner != null || decision.rolling
                ? decision.title + "\n" + expedition.text
                : decision.title,
            canChoose,
            own?.optionId);

        GameObject sourceRow = Instantiate(rowTemplate, sharedLootPanel.transform);
        Transform voters = sourceRow.transform.Find("Voters");
        if (voters != null)
        {
            voters.SetParent(sharedLootPanel.transform, false);
            if (voters is RectTransform rect)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 54f);
                rect.localScale = Vector3.one * 0.42f;
            }
            voters.gameObject.SetActive(true);
            spawnedRows.Add(voters.gameObject);
            BuildVoters(voters, expedition, decision, null, false);
        }
        Destroy(sourceRow);
    }

    private void BuildVoters(
        Transform voters,
        MultiplayerSession.ExpeditionState expedition,
        MultiplayerSession.SharedDecision decision,
        string optionId,
        bool twoColumn)
    {
        if (voters == null)
            return;

        for (int i = 0; i < voters.childCount; i++)
            voters.GetChild(i).gameObject.SetActive(false);

        if (twoColumn && voters is RectTransform votersRect)
        {
            votersRect.anchorMin = new Vector2(0.5f, 0f);
            votersRect.anchorMax = new Vector2(0.5f, 0f);
            votersRect.pivot = new Vector2(0.5f, 1f);
            votersRect.anchoredPosition = new Vector2(0f, -8f);
            votersRect.sizeDelta = new Vector2(190f, 92f);
        }

        int index = 0;
        foreach (MultiplayerSession.SharedBallot ballot in decision.ballots)
        {
            if (optionId != null && ballot.optionId != optionId)
                continue;
            if (index >= voters.childCount || index >= 4)
                break;

            Transform badge = voters.GetChild(index);
            badge.gameObject.SetActive(true);
            if (twoColumn && badge is RectTransform badgeRect)
            {
                badgeRect.anchorMin = new Vector2(0.5f, 1f);
                badgeRect.anchorMax = new Vector2(0.5f, 1f);
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.localScale = Vector3.one * 0.5f;
                badgeRect.anchoredPosition = new Vector2(index % 2 == 0 ? -43f : 43f, -20f - 38f * (index / 2));
            }

            int playerIndex = expedition.players.FindIndex(player => player.ownerId == ballot.ownerId);
            Image border = badge.Find("Border")?.GetComponent<Image>();
            if (border != null && playerColors != null && playerColors.Length > 0)
                border.color = playerColors[Mathf.Max(0, playerIndex) % playerColors.Length];
            MultiplayerSession.FormationSlot slot = expedition.party.Find(candidate =>
                candidate.character.id == ballot.characterId);
            TMP_Text nameText = badge.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = slot?.character?.name ?? "용병";
            RawImage portrait = badge.Find("Border/Portrait")?.GetComponent<RawImage>();
            if (portrait != null)
                portrait.texture = CharacterPoolManager.Instance?.Get(ballot.characterId)?.character?.Portrait;
            Button roll = badge.Find("Roll")?.GetComponent<Button>();
            if (roll != null)
            {
                roll.interactable = ballot.ownerId == session.LocalProfileId && decision.rolling &&
                    decision.winner == null && ballot.eligible && ballot.roll == 0;
                string characterId = ballot.characterId;
                roll.onClick.AddListener(() => session.RollExpeditionDice(characterId));
            }
            TMP_Text value = badge.Find("Value")?.GetComponent<TMP_Text>();
            if (value != null)
                value.text = ballot.roll > 0 ? ballot.roll.ToString() :
                    ballot.previousRoll > 0 ? $"{ballot.previousRoll}→?" : "—";
            index++;
        }
    }

    private void ClearRows()
    {
        foreach (GameObject row in spawnedRows)
        {
            if (row == null)
                continue;
            row.SetActive(false);
            Destroy(row);
        }
        spawnedRows.Clear();
    }

    private void CloseSharedLoot()
    {
        if (sharedLootPanel != null)
            sharedLootPanel.CloseSharedDecision();
        sharedLootPanel = null;
        sharedLootDecisionId = -1;
    }

    private void RefreshBattle()
    {
        if (battlePanel == null)
            return;
        battlePanel.SetActive(session.IsSharedBattle);
        if (!battlePanel.activeSelf) return;
        transform.SetAsLastSibling();
        var battle = session.Battle;
        if (session.FriendlyReconnectPending)
        {
            battleStatus.text = "재접속 대기 중 · 120초 안에 복귀하지 못한 참가자의 팀은 몰수패합니다.";
            return;
        }
        if (battle == null) { battleStatus.text = "참가자들의 전투 준비를 기다립니다."; return; }
        var actor = session.FindBattleCharacter(battle.attacker);
        if (battle.phase == MultiplayerSession.SharedBattlePhase.Attack)
            battleStatus.text = $"{actor.character.Name}의 공격 턴 · " +
                (session.OwnsBattleCharacter(actor) ? "공격을 지정하고 턴을 종료하세요." : "공격 선택을 기다립니다.");
        else if (battle.phase == MultiplayerSession.SharedBattlePhase.Counter)
        {
            var defender = session.FindBattleCharacter(battle.defender);
            string defense = defender == null ? "방패 버튼을 먼저 누른 캐릭터가 방어자가 됩니다." :
                $"방어자: {defender.character.Name}" + (battle.protectedTarget == null ? " · 보호 대상 선택 중" : "");
            battleStatus.text = defense + "\n" + (battle.readyPlayers.Contains(session.LocalProfileId) ?
                "대응 확정 완료 · 다른 참가자를 기다립니다." : "본인 캐릭터의 대응을 선택한 뒤 대응 확정을 눌러주세요.");
        }
        else battleStatus.text = battle.phase == MultiplayerSession.SharedBattlePhase.Finished ? "전투 결과를 정리합니다." : "공동 전투 진행 중";
    }
}
