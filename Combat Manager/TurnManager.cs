using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    private Queue<CharacterManager> turnQueue = new Queue<CharacterManager>();
    private List<CharacterManager> turnOrderList = new List<CharacterManager>(); // 턴 순서 리스트 (큐 복사본)
    public CharacterManager currentCharacter;
    public CharacterManager defenceCharacter;
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

        foreach (CharacterManager character in allCharacters)
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
            foreach (CharacterManager character in allCharacters)
            {
                Debug.Log("리소스 회복");
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
        UIManager.Instance.UpdateTurnOrder(turnOrderList, currentCharacter);

        // 캐릭터가 살아있으면 턴 시작, 그렇지 않으면 턴을 넘김
        if (currentCharacter.character.IsAlive)
        {
            currentCharacter.StartTurn(OnTurnEnd);

            // 턴 종료 버튼 설정
            SetEndTurnButtonAction();
        }
        else
        {
            EndTurn(); // 사망한 캐릭터는 바로 턴을 넘김
        }
    }

    public void StartCounterTurn()
    {
        if (currentCharacter.GetSkillQueue().Count > 0)
        {
            Debug.Log("CounterTurn Start");

            SetCounterTurnButtonAction(); // 대응 턴 버튼 설정

            // 현재 턴인 캐릭터와 반대 진영에 있는 캐릭터를 찾아 방어 버튼을 활성화
            List<CharacterManager> allCharacters = GameManager.Instance.GetAllCharacters();
            List<CharacterManager> enemies = allCharacters
                .Where(character => character.character.IsMine != currentCharacter.character.IsMine && character.character.IsAlive)
                .ToList();

            // 방어 캐릭터 선택 UI 활성화
            foreach (CharacterManager enemy in enemies)
            {
                if (enemy.characterUIHandler.CounterButton != null) 
                {
                    enemy.characterUIHandler.CounterButton.SetActive(true);
                    enemy.characterUIHandler.CounterButton.GetComponent<Button>().onClick.RemoveAllListeners();
                    enemy.characterUIHandler.CounterButton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        enemy.isDefenseCharacter = true; // 방어 캐릭터로 선택
                        DisableDefenseButtons(enemies); // 다른 캐릭터들의 방어 버튼 비활성화

                        enemy.UpdateCharacterUI();
                        UIManager.Instance.characterTargeting.SelectCharacter(enemy);
                        defenceCharacter = enemy;
                    });
                }
            }

            // 대응 스킬 선택 UI 출력
            UIManager.Instance.ShowCounterSkillUI(defenceCharacter);
        }
        else
        {
            EndTurn(); // 현재 턴이 온 캐릭터가 선택한 스킬이 없을 경우 EndTurn() 호출
        }
    }

    private void DisableDefenseButtons(List<CharacterManager> manager)
    {
        foreach (CharacterManager enemy in manager)
        {
            if (!enemy.isDefenseCharacter)
            {
                enemy.characterUIHandler.CounterButton.SetActive(false);
            }
        }
    }

    private void EndTurn()
    {        
        currentCharacter.isPlayerTurn = false;
        currentCharacter.isInMeleeCombat = false;
        currentCharacter.meleeTarget = null;

        currentCharacter.UpdateCharacterUI();

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
        turnOrderList = turnQueue.ToList();
    }

    // 턴 종료 콜백 - StartCounterTurn을 콜백으로 호출해서 현재 턴인 캐릭터의 스킬 큐 카운트 후 0일경우 EndTurn 호출
    public void OnTurnEnd()
    {
        StartCounterTurn();
    }

    // 턴 종료 버튼 설정
    private void SetEndTurnButtonAction()
    {
        if (UIManager.Instance != null && UIManager.Instance.turnEndButton != null)
        {
            // 버튼에 새로운 리스너 추가
            UIManager.Instance.turnEndButton.onClick.RemoveAllListeners();
            UIManager.Instance.turnEndButton.onClick.AddListener(() =>
            {
                if (currentCharacter != null)
                {
                    CombatHandler combatHandler = currentCharacter.GetComponent<CombatHandler>();
                    if (combatHandler != null)
                    {
                        combatHandler.ExecuteSkillQueue(() =>
                        {
                            OnTurnEnd(); // 턴종료 버튼 리스너 추가 - 대응턴으로 턴 넘기기.
                        });
                    }
                }
            });

            // 현재 턴인 캐릭터에 맞게 버튼을 활성화 또는 비활성화
            UIManager.Instance.counterTurnEndButton.gameObject.SetActive(currentCharacter.character.IsMine);
        }
    }

    // 대응턴 종료 버튼 설정
    private void SetCounterTurnButtonAction()
    {
        if (UIManager.Instance != null && UIManager.Instance.counterTurnEndButton != null)
        {
            // 버튼에 새로운 리스너 추가
            UIManager.Instance.counterTurnEndButton.onClick.RemoveAllListeners();
            UIManager.Instance.counterTurnEndButton.onClick.AddListener(() =>
            {
                if (currentCharacter != null)
                {
                    CombatHandler combatHandler = currentCharacter.GetComponent<CombatHandler>();
                    if (combatHandler != null)
                    {
                        combatHandler.ExecuteSkillQueue(() => // 대응 스킬큐 순차 실행 메서드 구현 필요
                        {
                            EndTurn(); // 턴 종료
                        });
                    }
                }
            });

            // 현재 턴인 캐릭터에 맞게 버튼을 활성화 또는 비활성화
            UIManager.Instance.turnEndButton.gameObject.SetActive(currentCharacter.character.IsMine);
        }
    }
}
