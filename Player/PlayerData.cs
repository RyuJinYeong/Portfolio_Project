using System;
using System.Collections.Generic;

[Serializable]
public class PositionEntry
{
    public string characterId;
    public bool isFront;
}

[Serializable]
public class LostExpeditionInventoryData
{
    public string sourceQuestId;
    public List<string> characterIds = new();
    public List<InventorySlotData> items = new();
}

public class PlayerData
{
    // 플레이어의 닉네임 - 용병단 이름
    public string playerName;

    // 계정 레벨 - 레벨에 따라 파티 구성 인원수가 확장되고 추가 기능이 해금됨
    public int level;

    public int gold;

    // 계정 전체 공유 창고
    public List<InventorySlotData> accountStorage = new();

    // 현재 원정대/파티 단위 창고
    public List<InventorySlotData> expeditionStorage = new();

    // 전멸한 원정대가 현장에 남긴 창고. 구출 의뢰 성공 시 구조대 창고로 회수된다.
    public List<LostExpeditionInventoryData> lostExpeditionInventories = new();

    // 생성 장비 인스턴스 저장소
    public List<GeneratedEquipmentData> generatedEquipments = new();

    // 보유한 캐릭터 ID 목록
    public List<string> characterIds = new List<string>();

    // 원정대 전멸 후 마을 로스터에서 제외된 실종 캐릭터 ID 목록
    public List<string> missingCharacterIds = new List<string>();

    // 구출되었지만 아직 부활하지 않아 출전할 수 없는 캐릭터 ID 목록
    public List<string> revivalRequiredCharacterIds = new List<string>();

    // 현재 계정에 제시된 고용 가능 용병 목록
    public List<CharacterData> recruitmentCandidates = new List<CharacterData>();
    public bool recruitmentCandidatesInitialized;
    public int recruitmentRefreshCount;
    public string reservedRecruitmentCandidateId;

    // 진행 중인 스테이지와 해당 스테이지를 진행 중인 캐릭터 ID 목록
    public string currentStage = "Town";
    public List<string> activeCharacterIds = new List<string>();

    // 포지션 저장용
    public List<PositionEntry> positions = new();

    public bool TryGetPosition(string characterId, out bool isFront)
    {
        isFront = false;

        if (string.IsNullOrEmpty(characterId) || positions == null)
            return false;

        PositionEntry entry = positions.Find(p => p != null && p.characterId == characterId);

        if (entry == null)
            return false;

        isFront = entry.isFront;
        return true;
    }

    public bool IsFrontPosition(string characterId)
    {
        return TryGetPosition(characterId, out bool isFront) && isFront;
    }

    public void SetPosition(string characterId, bool isFront)
    {
        if (string.IsNullOrEmpty(characterId))
            return;

        if (positions == null)
            positions = new List<PositionEntry>();

        PositionEntry entry = positions.Find(p => p != null && p.characterId == characterId);

        if (entry == null)
        {
            positions.Add(new PositionEntry
            {
                characterId = characterId,
                isFront = isFront
            });

            return;
        }

        entry.isFront = isFront;
    }

    public void RemovePosition(string characterId)
    {
        if (string.IsNullOrEmpty(characterId) || positions == null)
            return;

        positions.RemoveAll(p => p == null || p.characterId == characterId);
    }
}
