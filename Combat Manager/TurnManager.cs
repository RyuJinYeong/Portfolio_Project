using System;
using System.Collections;
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
    public CharacterManager defenseCharacter;
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


        // 기존의 턴을 가지고 있는 캐릭터가 추가 턴이 있는 경우, 다시 턴을 부여
        if (currentCharacter != null && currentCharacter.hasExtraTurn)
        {
            currentCharacter.hasExtraTurn = false; // 추가 턴 사용 완료
            Debug.Log($"{currentCharacter.character.Name}이 추가 턴을 획득했습니다.");
        }
        else
        {
            currentCharacter = turnQueue.Dequeue(); // 추가 턴이 없으면 다음 캐릭터로 넘어감
        }

        Debug.Log("현재 Turn Queue.Count : " + turnQueue.Count + " 현재 턴 캐릭터 :" + currentCharacter.character.Name);
        UIManager.Instance.UpdateTurnOrder(turnOrderList, currentCharacter);

        // 캐릭터가 살아있으면 턴 시작, 그렇지 않으면 턴을 넘김
        if (currentCharacter.character.IsAlive)
        {
            currentCharacter.StartTurn(OnTurnEnd);

            if (currentCharacter.character.IsMine)
            {
                // 턴 종료 버튼 설정
                SetEndTurnButtonAction();
            }
            else
            {
                UIManager.Instance.turnEndButton.gameObject.SetActive(false);
            }
        }
        else
        {
            StartNextTurn(); // 사망한 캐릭터는 바로 턴을 넘김
        }
    }

    public void StartCounterTurn()
    {
        if (currentCharacter.combatHandler.turnTimerCoroutine != null)
        {
            StopCoroutine(currentCharacter.combatHandler.turnTimerCoroutine);
        }

        if (currentCharacter.GetSkillQueue().Count > 0)
        {
            currentCharacter.combatHandler.AutoAssignDefaultCounterSkills();
            Debug.Log("CounterTurn Start");
            //대응턴 시작 30초 제한
            currentCharacter.combatHandler.turnTimerCoroutine = StartCoroutine(currentCharacter.combatHandler.TurnTimer(30f, EndTurn));

            if (!currentCharacter.character.IsMine)
            {
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
                            defenseCharacter = enemy;
                            enemy.combatHandler.SetDefenseCharacter(enemy); // 캐릭터 매니저 필드값 변경 - 방어 캐릭터로 선택
                            DisableDefenseButtons(enemies); // 방어 버튼 비활성화
                            enemy.UpdateCharacterUI();

                            // 방어 대상 타겟팅 시작
                            UIManager.Instance.characterTargeting.StartDefenseTargeting(defenseCharacter);
                        });
                    }
                }            
            }
            else
            {
                UIManager.Instance.turnEndButton.gameObject.SetActive(false);
                UIManager.Instance.counterTurnEndButton.gameObject.SetActive(false);

                EndTurn();
                return;
            }
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
            enemy.characterUIHandler.CounterButton.SetActive(false);
        }
    }

    private void EndTurn()
    {
        UIManager.Instance.characterTargeting.lineRenderer.enabled = false;
        if (currentCharacter != null)
        {
            if (currentCharacter.combatHandler.turnTimerCoroutine != null)
            {
                StopCoroutine(currentCharacter.combatHandler.turnTimerCoroutine);
            }

            CombatHandler combatHandler = currentCharacter.GetComponent<CombatHandler>();
            if (combatHandler != null)
            {
                // 대응 스킬큐 순차 실행 메서드 구현 필요
                combatHandler.ExecuteSkillQueue(() =>{});
            }
        }

        // 방어자와 방어 대상 필드 초기화
        if (defenseCharacter != null)
        {
            defenseCharacter.combatHandler.isDefenseCharacter = false;
            defenseCharacter = null;
        }

        foreach (var character in allCharacters)
        {
            if (character.combatHandler.isDefenseTarget.ContainsKey(true))
            {
                character.combatHandler.isDefenseTarget[true] = null;
            }
        }

        currentCharacter.isPlayerTurn = false;
        currentCharacter.isInMeleeCombat = false;
        currentCharacter.meleeTarget = null;

        currentCharacter.UpdateCharacterUI();

        // 턴이 끝날 때마다 승리/패배 조건 체크
        if (!CheckBattleEnd())
        {
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
            UIManager.Instance.turnEndButton.gameObject.SetActive(true);
            // 버튼에 새로운 리스너 추가
            UIManager.Instance.turnEndButton.onClick.RemoveAllListeners();
            UIManager.Instance.turnEndButton.onClick.AddListener(() =>
            {
                if (currentCharacter != null)
                {
                    if (currentCharacter != null)
                    {
                        StartCounterTurn(); // 대응 턴 시작 (이후 대응턴 종료 버튼에서 스킬 큐 실행)
                    }
                }
            });
        }
    }

    // 자동대응 버튼 설정
    private void SetCounterTurnButtonAction()
    {
        if (UIManager.Instance != null && UIManager.Instance.counterTurnEndButton != null)
        {
            UIManager.Instance.counterTurnEndButton.gameObject.SetActive(true);
            UIManager.Instance.turnEndButton.gameObject.SetActive(false);
            // 버튼에 새로운 리스너 추가
            UIManager.Instance.counterTurnEndButton.onClick.RemoveAllListeners();
            UIManager.Instance.counterTurnEndButton.onClick.AddListener(() =>
            {
                if (currentCharacter != null)
                {
                    CombatHandler combatHandler = currentCharacter.GetComponent<CombatHandler>();
                    if (combatHandler != null)
                    {
                        // 대응 스킬 큐 실행
                        combatHandler.ExecuteSkillQueue(() =>
                        {
                            EndTurn(); // 턴 종료
                            UIManager.Instance.counterTurnEndButton.gameObject.SetActive(false);
                        });
                    }
                }
            });
        }
    }
}
