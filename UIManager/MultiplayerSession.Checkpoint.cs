using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public partial class MultiplayerSession
{
    private ExpeditionState resumeCheckpoint;
    public bool HasSavedExpedition => PlayerManager.Instance.SharedCheckpoint != null;
    public bool IsExpeditionCharacterLocked(string id) => !InExpedition && PlayerManager.Instance.SharedCheckpoint?.party
        .Any(slot => slot.ownerId == LocalProfileId && slot.character.id == id) == true;

    public void ResumeSavedExpedition()
    {
        var saved = PlayerManager.Instance.SharedCheckpoint;
        if (saved == null || !Editable || InExpedition) return;
        if (saved.friendly != null && !saved.finished && !saved.settlementPrepared)
        {
            Status = "친선전 초대 코드로 다시 참가하세요. 보관된 친선전을 포기하면 내기 아이템을 잃고 몰수패합니다.";
            RefreshControls();
            return;
        }
        if (saved.finished || saved.settlementPrepared)
        {
            beforeExpedition = SaveMapper.ToDto(PlayerManager.Instance.GetCurrentPlayerData());
            beforeCharacters.Clear();
            foreach (var manager in CharacterPoolManager.Instance.All())
                beforeCharacters[manager.character.ID] = SaveMapper.ToDto(manager.character);
            Expedition = saved;
            previousSaveSuppression = PlayerManager.Instance.suppressRemotePersistence;
            if (CommitSharedRewards(saved))
            {
                RestoreBeforeExpedition();
                Status = "미수령 원정 보상을 저장했습니다.";
            }
            else { beforeExpedition = null; Expedition = null; Status = "보상 저장에 실패했습니다. 다시 시도해 주세요."; }
            RefreshControls();
            return;
        }
        if (saved.players.First(player => player.host).ownerId != LocalProfileId)
        {
            Status = "기존 호스트가 보관된 원정을 연 뒤 새 초대 코드로 참가하세요.";
            RefreshControls();
            return;
        }
        resumeCheckpoint = JsonConvert.DeserializeObject<ExpeditionState>(JsonConvert.SerializeObject(saved));
        pendingQuest = resumeCheckpoint.runtime.def;
        serverFormation.quest = resumeCheckpoint.runtime.def;
        serverFormation.slots = resumeCheckpoint.party;
        serverFormation.savedExpeditionId = resumeCheckpoint.id;
        transportIndex = 1;
        HostRoom();
    }

    public void AbandonSavedExpedition()
    {
        if (!Editable || InExpedition || PlayerManager.Instance.SharedCheckpoint?.settlementPrepared == true) return;
        var saved = PlayerManager.Instance.SharedCheckpoint;
        if (saved?.friendly != null)
        {
            beforeExpedition = SaveMapper.ToDto(PlayerManager.Instance.GetCurrentPlayerData());
            beforeCharacters.Clear();
            foreach (var manager in CharacterPoolManager.Instance.All())
                beforeCharacters[manager.character.ID] = SaveMapper.ToDto(manager.character);
            previousSaveSuppression = PlayerManager.Instance.suppressRemotePersistence;
            Expedition = saved;
            ResolveOfflineFriendlyForfeit(saved, FriendlyTeam(LocalProfileId));
            return;
        }
        if (PlayerManager.Instance.SaveSharedCheckpoint(null))
            Status = "보관된 원정을 포기했습니다. 용병을 다시 편성할 수 있습니다.";
        RefreshControls();
    }

    private void SaveExpeditionProgress(ExpeditionState state)
    {
        if (PlayerManager.Instance.HasSharedReceipt(state.id) ||
            (state.friendly == null && state.phase != ExpeditionPhase.Map && state.phase != ExpeditionPhase.Result)) return;
        if (!PlayerManager.Instance.SaveSharedCheckpoint(state))
        {
            Status = "공동 원정 진행 상태를 저장하지 못했습니다.";
            SendExpeditionCommand("reject");
        }
    }

    private void ResumeLoadedNode()
    {
        var saved = resumeCheckpoint;
        int revision = serverExpedition.revision;
        serverExpedition = JsonConvert.DeserializeObject<ExpeditionState>(JsonConvert.SerializeObject(saved));
        serverExpedition.revision = revision;
        serverExpedition.resuming = false;
        sharedDecisionId = Mathf.Max(sharedDecisionId, serverExpedition.decision?.id ?? 0);
        sharedResolution = serverExpedition.resolution;
        QuestManager.Instance.active = serverExpedition.runtime;
        resumeCheckpoint = null;
        if (serverExpedition.questCompleted) QuestManager.Instance.active = null;
        if (serverExpedition.decision?.winner != null) decisionDeadline = Time.realtimeSinceStartup + 1f;
        PublishExpedition();
    }
}
