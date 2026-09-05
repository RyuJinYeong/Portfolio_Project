using System.Collections.Generic;
using UnityEngine;

public class GameDataRegistryTester : MonoBehaviour
{
    [Header("Test Party Origins")]
    [SerializeField] private List<OriginDefinitionSO> partyOrigins = new();

    [Header("Test Dungeon Data")]
    [SerializeField] private QuestStageDefinitionSO dungeonStage;
    [SerializeField] private List<MonsterRoleSO> dungeonMonsterRoles = new();

    public IReadOnlyList<OriginDefinitionSO> PartyOrigins => partyOrigins;
    public QuestStageDefinitionSO DungeonStage => dungeonStage;

    public bool RegisterTestData()
    {
        GameDataRegistry registry = GameDataRegistry.Instance;

        if (registry == null)
        {
            Debug.LogError("[TestScene] GameDataRegistry가 없습니다.");
            return false;
        }

        foreach (OriginDefinitionSO origin in partyOrigins)
        {
            if (origin == null)
                continue;

            registry.origins.RemoveAll(value => value != null && value.id == origin.id);
            registry.origins.Add(origin);
        }

        registry.Build();

        return ValidateRegisteredData(registry);
    }

    public List<MonsterRoleSO> GetMonsterRoles(CharacterType characterType)
    {
        List<MonsterRoleSO> result = new();

        foreach (MonsterRoleSO role in dungeonMonsterRoles)
        {
            if (role != null && role.characterType == characterType)
                result.Add(role);
        }

        return result;
    }

    private bool ValidateRegisteredData(GameDataRegistry registry)
    {
        bool valid = partyOrigins.Count == 4 && dungeonStage != null;

        foreach (OriginDefinitionSO origin in partyOrigins)
        {
            if (origin == null || registry.GetOrigin(origin.id) == null)
                valid = false;
        }

        if (dungeonStage == null || registry.GetQuestStage(dungeonStage.stageKey) == null)
            valid = false;

        foreach (MonsterRoleSO role in dungeonMonsterRoles)
        {
            if (role == null || registry.GetMonsterRole(role.id) == null)
                valid = false;
        }

        if (GetMonsterRoles(CharacterType.Normal).Count == 0)
            valid = false;

        if (!valid)
            Debug.LogError("[TestScene] 4개 출신지 또는 던전 전투 데이터 등록이 불완전합니다.");

        return valid;
    }
}
