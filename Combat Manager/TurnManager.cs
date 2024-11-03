using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private Queue<CharacterManager> turnQueue = new Queue<CharacterManager>();
    private CharacterManager currentCharacter;
    private GameManager gameManager;
    private List<CharacterManager> allCharacters; // 전투에 참여한 모든 캐릭터들을 관리하는 리스트

    private void Start()
    {
        gameManager = GameManager.Instance;
        InitializeTurnOrder();
    }

    public void InitializeTurnOrder()
    {
        allCharacters = gameManager.GetAllCharacters(); // 모든 캐릭터들을 가져와서 리스트에 저장

        foreach (var character in allCharacters)
        {
            Debug.Log("리소스 회복");
            character.RecoverResources();
        }

        UpdateTurnQueue();
        Debug.Log("turn queue : "+turnQueue.Count);
        StartNextTurn();
    }

    public void StartNextTurn()
    {
        if (turnQueue.Count == 0)
        {
            // 상태이상 처리, 리소스 회복
            ApplyStatusEffectsToAll();
            foreach (var character in allCharacters)
            {
                character.RecoverResources();
            }

            // 승리 혹은 패배 조건 체크
            if (CheckBattleEnd())
            {
                return;
            }

            UpdateTurnQueue(); // 큐 갱신
        }

        currentCharacter = turnQueue.Dequeue();

        // 캐릭터가 살아있으면 턴 시작, 그렇지 않으면 턴을 넘김
        if (currentCharacter.character.IsAlive)
        {
            currentCharacter.StartTurn(OnTurnEnd);
        }
        else
        {
            EndTurn(); // 사망한 캐릭터는 바로 턴을 넘김
        }
    }

    public void EndTurn()
    {
        // 턴이 끝날 때마다 승리/패배 조건 체크
        if (!CheckBattleEnd())
        {
            turnQueue.Enqueue(currentCharacter);
            StartNextTurn();
        }
    }

    private void ApplyStatusEffectsToAll() // 모든 캐릭터에게 상태이상 일괄적용
    {
        foreach (var characterManager in allCharacters)
        {
            if (characterManager.character.StatusEffects != null)
            {
                foreach (var statusEffect in characterManager.character.StatusEffects.ToList())
                {
                    statusEffect.ApplyEffect(characterManager); // 매턴 지속형 상태이상 효과 적용
                    statusEffect.ReduceTurn(); // 상태이상의 남은 지속 턴 감소

                    if (statusEffect.IsExpired()) // 해당 상태이상의 남은 턴이 0일 경우
                    {
                        statusEffect.OnExpire(characterManager); // 효과 적용 해제
                        characterManager.character.StatusEffects.Remove(statusEffect); // 리스트에서 상태이상 제거
                    }
                }
            }

            characterManager.UpdateCharacterUI();
        }
    }

    private bool CheckBattleEnd()
    {
        bool allAlliesDead = allCharacters.All(c => c.character.IsMine && !c.character.IsAlive);
        bool allEnemiesDead = allCharacters.All(c => !c.character.IsMine && !c.character.IsAlive);

        if (allEnemiesDead)
        {
            HandleVictory();
            return true;
        }
        else if (allAlliesDead)
        {
            HandleDefeat();
            return true;
        }

        return false;
    }

    private void HandleVictory()
    {
        // 경험치 획득, 아이템 드랍, 스테이지 선택 UI 등 처리
        Debug.Log("승리!");
        // 전투 종료 처리 로직 추가
    }

    private void HandleDefeat()
    {
        // 파티 전멸 UI 및 이후 처리
        Debug.Log("패배!");
        // 전투 종료 처리 로직 추가
    }

    private void UpdateTurnQueue()
    {
        turnQueue = new Queue<CharacterManager>(
            allCharacters.Where(c => c.character.IsAlive).OrderByDescending(c => Mathf.Max(c.character.FinalStats.AttackSpeed, c.character.FinalStats.CastSpeed))
            .ThenByDescending(c => Mathf.Max(c.character.FinalStats.Speed, c.character.FinalStats.Wisdom))
            .ThenByDescending(c => c.character.Type)
        );
    }

    // 턴 종료 콜백
    private void OnTurnEnd()
    {
        EndTurn();
    }
}
