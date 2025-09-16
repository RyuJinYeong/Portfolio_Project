using SoftKitty.InventoryEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class PositionEntry { public string characterId; public bool isFront; }

public class PlayerData // 플레이어 계정 정보
{
    // 플레이어의 닉네임
    public string playerName;
    // 계정 레벨 - 레벨에 따라 파티 구성 인원수가 확장되고 추가 기능이 해금됨
    public int level;
    // 플레이어의 골드
    public int gold;
    // 플레이어의 창고 아이템 목록
    public InventoryHolder storage = new InventoryHolder();
    // 보유한 캐릭터 ID 목록
    public List<string> characterIds = new List<string>();

    // 진행 중인 스테이지와 해당 스테이지를 진행 중인 캐릭터 ID 목록
    public string currentStage = "Town";
    public List<string> activeCharacterIds = new List<string>();

    // 저장용(네이티브 Dictionary대신)
    public List<PositionEntry> positions = new();

    // 런타임 캐시
    [Newtonsoft.Json.JsonIgnore]
    // 캐릭터 위치 정보
    public Dictionary<string, bool> characterPositionMapping = new Dictionary<string, bool>(); // true for front row, false for back row
}
