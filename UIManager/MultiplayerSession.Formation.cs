using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Transporting;
using Newtonsoft.Json;
using UnityEngine;

public partial class MultiplayerSession
{
    [Serializable]
    public class FormationMember
    {
        public string ownerId;
        public string name;
        public bool ready;
        public bool host;
    }

    [Serializable]
    public class FormationSlot
    {
        public string ownerId;
        public CharacterSaveDTO character;
        public bool isFront;
        public List<GeneratedEquipmentData> equipment = new();
    }

    [Serializable]
    public class FormationState
    {
        public int revision;
        public QuestDef quest;
        public List<FormationSlot> slots = new();
        public List<FormationMember> members = new();
        public string savedExpeditionId;
        public FriendlyMatchSettings friendly;
    }

    public struct FormationMessage : IBroadcast
    {
        public int revision;
        public string slots;
        public string removeCharacterId;
    }

    public FormationState Formation { get; private set; } = new();
    private FormationState serverFormation = new();
    private string openedQuestId;
    public string LocalProfileId => PlayerManager.Instance != null ? PlayerManager.Instance.GetCurrentPlayerData()?.profileId : null;
    public event Action FormationChanged;

    public void SelectQuest(QuestDef quest)
    {
        if (!IsHost || quest == null || serverFormation.quest?.id == quest.id || resumeCheckpoint != null) return;
        serverFormation.quest = JsonConvert.DeserializeObject<QuestDef>(JsonConvert.SerializeObject(quest));
        if (quest.id != "friendly") serverFormation.friendly = null;
        serverFormation.slots.Clear();
        InvalidateReady();
        if (members.Count > 0) PublishMembers();
    }

    public void SubmitFormation(List<CharacterData> characters, List<bool> rows)
    {
        if (!Connected || characters.Count != rows.Count || characters.Count > (IsFriendlyRoom ? Formation.friendly.teamSize : 4)) return;
        if (Formation.savedExpeditionId != null) return;
        var player = PlayerManager.Instance.GetCurrentPlayerData();
        var slots = new List<FormationSlot>();
        for (int i = 0; i < characters.Count; i++)
        {
            if (!player.characterIds.Contains(characters[i].ID) || !characters[i].IsAlive || IsExpeditionCharacterLocked(characters[i].ID)) return;
            slots.Add(new FormationSlot
            {
                character = SaveMapper.ToDto(characters[i]), isFront = rows[i], ownerId = LocalProfileId,
                equipment = GetFormationEquipment(characters[i])
            });
        }
        networkManager.ClientManager.Broadcast(new FormationMessage
        {
            revision = Formation.revision, slots = JsonConvert.SerializeObject(slots)
        });
    }

    public void RemoveFormationSlot(string characterId)
    {
        if (!Connected) return;
        networkManager.ClientManager.Broadcast(new FormationMessage
        {
            revision = Formation.revision, removeCharacterId = characterId
        });
    }

    private void OnFormationMessage(NetworkConnection connection, FormationMessage message, Channel channel)
    {
        if (serverExpedition != null || resumeCheckpoint != null) return;
        if (!members.TryGetValue(connection.ClientId, out var member) || serverFormation.quest == null) return;
        if (!string.IsNullOrEmpty(message.removeCharacterId))
        {
            int removed = serverFormation.slots.RemoveAll(slot => slot.character.id == message.removeCharacterId &&
                (slot.ownerId == member.profileId || member.profileId == LocalProfileId));
            if (removed == 0) return;
        }
        else
        {
            if (string.IsNullOrEmpty(message.slots) || message.slots.Length > 131072)
            {
                PublishMembers();
                return;
            }
            List<FormationSlot> submitted;
            try { submitted = JsonConvert.DeserializeObject<List<FormationSlot>>(message.slots); }
            catch (JsonException)
            {
                PublishMembers();
                return;
            }
            if (submitted == null || submitted.Count > (serverFormation.friendly?.teamSize ?? 4) ||
                submitted.Any(slot => slot?.character == null || string.IsNullOrEmpty(slot.character.id) ||
                    slot.character.id.Length > 64 || !slot.character.isAlive || slot.equipment == null ||
                    slot.equipment.Count > 9 || slot.equipment.Any(item => item == null || string.IsNullOrEmpty(item.instanceId))) ||
                submitted.Select(slot => slot.character.id).Distinct().Count() != submitted.Count)
            {
                PublishMembers();
                return;
            }

            var others = serverFormation.slots.Where(slot => slot.ownerId != member.profileId).ToList();
            var friendly = serverFormation.friendly;
            int team = friendly?.participants.Find(p => p.ownerId == member.profileId)?.team ?? -1;
            int existingCount = friendly == null ? others.Count : others.Count(slot =>
                friendly.participants.Any(p => p.ownerId == slot.ownerId && p.team == team));
            if (existingCount + submitted.Count > (friendly?.teamSize ?? 4) || submitted.Any(slot =>
                others.Exists(other => other.character.id == slot.character.id)))
            {
                PublishMembers();
                return;
            }
            foreach (var slot in submitted) slot.ownerId = member.profileId;
            int insertIndex = serverFormation.slots.FindIndex(slot => slot.ownerId == member.profileId);
            serverFormation.slots.RemoveAll(slot => slot.ownerId == member.profileId);
            serverFormation.slots.InsertRange(insertIndex < 0 ? serverFormation.slots.Count : insertIndex, submitted);
        }
        InvalidateReady();
        PublishMembers();
    }

    private void InvalidateReady()
    {
        serverFormation.revision++;
        if (serverFormation.friendly != null)
            foreach (var participant in serverFormation.friendly.participants)
                participant.locked = participant.accepted = false;
        foreach (int id in members.Keys.ToArray())
        {
            var member = members[id];
            member.ready = false;
            members[id] = member;
        }
    }

    private void ApplyFormation(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        Formation = JsonConvert.DeserializeObject<FormationState>(json);
        ready = Formation.members.Find(member => member.ownerId == LocalProfileId)?.ready ?? false;
        FormationChanged?.Invoke();
    }

    private void OpenFormationWhenAvailable()
    {
        if (InExpedition || !Connected || Formation.quest == null || openedQuestId == Formation.quest.id ||
            UIManager.Instance == null || UIManager.Instance.partyFormationPanel == null ||
            CharacterPoolManager.Instance == null || !CharacterPoolManager.Instance.IsBuilt) return;
        openedQuestId = Formation.quest.id;
        if (IsHost && UIManager.Instance.partyFormationPanel.gameObject.activeInHierarchy) return;
        UIManager.Instance.CloseManagedWindows();
        UIManager.Instance.partyFormationPanel.Open(Formation.quest);
    }

    public void RefreshSavedCharacter(CharacterData character)
    {
        if (InExpedition) return;
        if (!Connected || character == null || !Formation.slots.Exists(slot =>
            slot.ownerId == LocalProfileId && slot.character.id == character.ID)) return;
        var slots = Formation.slots.Where(slot => slot.ownerId == LocalProfileId).Select(slot =>
            new FormationSlot
            {
                ownerId = slot.ownerId, isFront = slot.isFront,
                character = slot.character.id == character.ID ? SaveMapper.ToDto(character) : slot.character,
                equipment = slot.character.id == character.ID ? GetFormationEquipment(character) : slot.equipment
            }).ToList();
        string json = JsonConvert.SerializeObject(slots);
        if (json == JsonConvert.SerializeObject(Formation.slots.Where(slot => slot.ownerId == LocalProfileId))) return;
        networkManager.ClientManager.Broadcast(new FormationMessage { revision = Formation.revision, slots = json });
    }

    public bool CanReady(out string reason)
    {
        reason = "";
        if (IsFriendlyRoom)
        {
            if (HasSavedExpedition) { reason = "보관된 원정을 먼저 재개하거나 정리하세요."; return false; }
            if (!Formation.slots.Any(slot => slot.ownerId == LocalProfileId))
            { reason = "본인 용병을 한 명 이상 편성하세요."; return false; }
            return ValidateFriendlyStake(out reason);
        }
        if (Formation.savedExpeditionId != null)
        {
            var saved = PlayerManager.Instance.SharedCheckpoint;
            if (!Formation.slots.Exists(slot => slot.ownerId == LocalProfileId))
                reason = "보관된 원정의 참가자 정보가 없습니다.";
            else if (saved?.id == Formation.savedExpeditionId && saved.sortiePay.TryGetValue(LocalProfileId, out int pay) &&
                     PlayerManager.Instance.GetCurrentPlayerData().gold < pay)
                reason = "보관된 원정의 출전 수당이 부족합니다.";
            return reason.Length == 0;
        }
        if (HasSavedExpedition)
            reason = "보관된 공동 원정을 재개하거나 포기한 뒤 새 공동 원정을 시작하세요.";
        else if (Formation.quest == null)
            reason = "호스트가 의뢰를 선택해야 합니다.";
        else if (!Formation.slots.Exists(slot => slot.ownerId == LocalProfileId))
            reason = "본인 소유의 캐릭터를 한 명 이상 편성하세요.";
        else
        {
            int pay = 0;
            foreach (var slot in Formation.slots.Where(slot => slot.ownerId == LocalProfileId))
            {
                var character = CharacterPoolManager.Instance?.Get(slot.character.id)?.character;
                if (character == null || !character.IsAlive || IsExpeditionCharacterLocked(slot.character.id))
                {
                    reason = "편성된 캐릭터 정보를 다시 확인하세요.";
                    break;
                }
                pay += MercenaryGenerator.CalculateSortiePay(character);
            }
            if (string.IsNullOrEmpty(reason) && PlayerManager.Instance.GetCurrentPlayerData().gold < pay)
                reason = "본인 캐릭터의 출전 수당을 지급할 골드가 부족합니다.";
        }
        return string.IsNullOrEmpty(reason);
    }

    public bool CanDepart(out string reason)
    {
        if (!IsHost)
        {
            reason = "호스트만 출발할 수 있습니다.";
            return false;
        }
        if (!CanReady(out reason)) return false;
        if (IsFriendlyRoom)
        {
            if (!FriendlyFormationComplete(Formation, out reason)) return false;
            if (!Formation.friendly.participants.All(p => p.locked && p.accepted))
            { reason = "모든 참가자가 내기를 잠그고 최종 수락해야 합니다."; return false; }
            return true;
        }
        if (resumeCheckpoint != null && resumeCheckpoint.players.Any(player => !Formation.members.Exists(member => member.ownerId == player.ownerId)))
        {
            reason = "기존 원정 참가자가 모두 돌아와야 재개할 수 있습니다.";
            return false;
        }
        foreach (var member in Formation.members)
        {
            if (!Formation.slots.Exists(slot => slot.ownerId == member.ownerId))
                reason = $"{member.name}의 캐릭터 편성을 기다리고 있습니다.";
            else if (!member.host && !member.ready)
                reason = $"{member.name}의 준비를 기다리고 있습니다.";
            if (!string.IsNullOrEmpty(reason)) return false;
        }
        return true;
    }
}
