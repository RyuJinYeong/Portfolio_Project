using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public partial class MultiplayerSession
{
    private void BeginSharedNodeRewards()
    {
        serverExpedition.phase = ExpeditionPhase.Result;
        var pending = serverExpedition.friendly == null ? serverExpedition.party.FirstOrDefault(slot => slot.character.pendingLevelUps > 0) : null;
        if (pending != null)
        {
            var manager = FindBattleCharacter(pending.character.id);
            LevelGrowthUtility.GetGrowthRange(manager.character, out int minimum, out int maximum);
            var choices = LevelGrowthUtility.CreateChoices(minimum, maximum, new System.Random());
            string[] names = choices.Select(choice => choice.stat switch
            {
                StatRequirementType.Strength => "근력", StatRequirementType.Dexterity => "기량",
                StatRequirementType.Speed => "속도", StatRequirementType.Health => "건강",
                StatRequirementType.Vitality => "활력", StatRequirementType.Intelligence => "지능",
                StatRequirementType.Wisdom => "지혜", StatRequirementType.Endurance => "인내",
                StatRequirementType.Detection => "눈썰미", StatRequirementType.Insight => "통찰력", _ => choice.stat.ToString()
            }).ToArray();
            serverExpedition.decision = new SharedDecision
            {
                id = ++sharedDecisionId, kind = "growth", title = $"{pending.character.name} 레벨업 · 남은 성장 {pending.character.pendingLevelUps}회",
                ownerId = pending.ownerId, characterId = pending.character.id, growthChoices = choices,
                options = choices.Select((choice, i) => new SharedOption { id = i.ToString(), label = $"{names[i]} +{choice.amount}" }).ToList()
            };
            serverExpedition.text = "해당 용병의 소유자가 성장 보상을 선택합니다.";
            PublishExpedition();
            return;
        }
        if (serverExpedition.pendingLoot.Count > 0)
        {
            var item = serverExpedition.pendingLoot[0];
            var generated = EquipmentInstanceRepository.Get(item.equipmentInstanceId);
            string name = generated?.generatedName ?? GameDataRegistry.Instance.GetItem(item.itemUid).itemName;
            CreateSharedDecision("loot", $"전리품 분배 · {name} ×{item.count}\n입찰하면 투입한 용병마다 한 번씩 굴립니다. 동점이면 최고값끼리 재굴림합니다.",
                new List<SharedOption> { new() { id = "need", label = "입찰" }, new() { id = "pass", label = "양보" } });
            return;
        }
        if (serverExpedition.friendly != null)
        {
            CompleteSharedExpedition();
            return;
        }
        CreateSharedDecision(serverExpedition.afterRewards, serverExpedition.rewardSummary,
            new List<SharedOption> { new() { id = "continue", label = "계속" } });
    }

    private void CompleteSharedExpedition()
    {
        serverExpedition.phase = ExpeditionPhase.Result;
        serverExpedition.settlementPrepared = true;
        serverExpedition.decision = null;
        serverExpedition.text = serverExpedition.friendly == null ? "원정 완료 · 각 참가자의 보상을 저장하고 있습니다." :
            serverExpedition.rewardSummary + "\n내기 아이템을 정산하고 있습니다.";
        expeditionAcks.Clear();
        expeditionDeadline = Time.realtimeSinceStartup + 90f;
        PublishExpedition();
    }

    private bool CommitSharedRewards(ExpeditionState state)
    {
        var playerManager = PlayerManager.Instance;
        if (playerManager.HasSharedReceipt(state.id)) return true;
        if (state.friendly != null) return CommitFriendlyRewards(state);
        var originalQuest = JsonConvert.DeserializeObject<QuestStateDTO>(JsonConvert.SerializeObject(SaveMapper.ToDto(QuestManager.Instance)));
        var originalPlayer = playerManager.GetCurrentPlayerData();
        try
        {
            var player = SaveMapper.FromDto(JsonConvert.DeserializeObject<PlayerSaveDTO>(JsonConvert.SerializeObject(beforeExpedition)));
            playerManager.SetCurrentPlayerData(player);
            state.battleGold.TryGetValue(LocalProfileId, out int earned);
            state.sortiePay.TryGetValue(LocalProfileId, out int pay);
            player.gold += earned - pay;
            var ownSlots = state.party.Where(slot => slot.ownerId == LocalProfileId).ToList();
            foreach (var equipment in state.equipment) EquipmentInstanceRepository.AddRuntime(equipment);
            foreach (var reward in state.claimedLoot.Where(loot => loot.ownerId == LocalProfileId))
            {
                var item = JsonConvert.DeserializeObject<InventorySlotData>(JsonConvert.SerializeObject(reward.item));
                if (item.IsMonsterEssence()) SharedInventoryUtility.ExpireMonsterEssences(new List<InventorySlotData> { item }, item.essenceQuestId);
                if (!SharedInventoryUtility.AddInventorySlot(player.accountStorage, item)) return false;
                if (!string.IsNullOrEmpty(item.equipmentInstanceId))
                    EquipmentInstanceRepository.PromoteRuntimeToPlayer(item.equipmentInstanceId);
            }
            var quest = JsonConvert.DeserializeObject<QuestDef>(JsonConvert.SerializeObject(state.runtime.def));
            var rewardDef = QuestManager.Instance.ResolveReward(quest);
            int index = 0;
            int questGold = 0;
            foreach (var slot in state.party)
            {
                if (slot.ownerId == LocalProfileId)
                    questGold += rewardDef.goldAmount / state.party.Count + (index < rewardDef.goldAmount % state.party.Count ? 1 : 0);
                index++;
            }
            rewardDef.goldAmount = questGold;
            rewardDef.goldClaimed = rewardDef.itemsClaimed = false;
            QuestManager.Instance.board.RemoveAll(entry => entry.def?.id == quest.id);
            QuestManager.Instance.completed.Add(new CompletedQuestEntry { def = quest, status = QuestStatus.CompletedPendingReward });
            QuestManager.Instance.ClaimCompletedRewards(player);
            player.recruitmentCandidatesInitialized = false;
            player.recruitmentRefreshCount = 0;
            var characters = ownSlots.Select(slot => JsonConvert.DeserializeObject<CharacterSaveDTO>(JsonConvert.SerializeObject(slot.character))).ToList();
            foreach (var dto in characters)
            {
                var character = SaveMapper.FromDto(dto);
                character.IsAlive = true;
                character.CurrentHp = character.FinalStats.MaxHp;
                dto.currentHp = character.CurrentHp;
                dto.isAlive = true;
            }
            return playerManager.CommitSharedResult(state.id, SaveMapper.ToDto(player), characters);
        }
        finally
        {
            playerManager.SetCurrentPlayerData(originalPlayer);
            SaveMapper.FromDto(originalQuest);
        }
    }
}
