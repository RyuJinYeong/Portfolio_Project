using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private Queue<CharacterManager> turnQueue = new Queue<CharacterManager>();
    private List<CharacterManager> turnOrderList = new List<CharacterManager>(); // 턴 순서 리스트 (큐 복사본)
    public CharacterManager currentCharacter; // 공격자
    public CharacterManager defenseCharacter; // 방어자
    public CharacterManager defenseTarget;    // 방어대상
    private GameManager gameManager;
    private List<CharacterManager> allCharacters; // 전투에 참여한 모든 캐릭터들을 관리하는 리스트

    private bool _stageStarted; // 중복 시작 방지 플래그
    private bool battleEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        // GameManager 이벤트 구독
        gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.RosterReady -= OnRosterReady;
            gameManager.RosterReady += OnRosterReady;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.RosterReady -= OnRosterReady;
    }

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager != null)
        {
            gameManager.RosterReady -= OnRosterReady;
            gameManager.RosterReady += OnRosterReady;
        }

        // 혹시 이미 참가자 등록이 끝나 있었다면 한 번 더 체크
        StartCoroutine(WaitAndMaybeStart());
    }

    private IEnumerator WaitAndMaybeStart()
    {
        yield return null; // 한 프레임 유예
        if (_stageStarted) yield break;

        var list = gameManager?.GetAllCharacters();
        if (HasBothBattleSides(list))
            OnRosterReady();
    }

    // GameManager의 로스터 준비 이벤트 핸들러
    private void OnRosterReady()
    {
        if (_stageStarted) return;
        List<CharacterManager> roster = gameManager?.GetAllCharacters();

        if (!HasBothBattleSides(roster))
            return;

        var count = roster.Count;
        Debug.Log($"[TurnManager] RosterReady 수신, 참가자 {count}명");
        _stageStarted = true;
        InitializeTurnOrder();
    }

    public void ForceRebuildAndRestart() // 수동 강제 시작 기능 (디버깅/테스트용)
    {
        _stageStarted = true;
        InitializeTurnOrder();
    }

    public void InitializeTurnOrder()
    {
        allCharacters = gameManager.GetAllCharacters(); // 모든 캐릭터들을 가져와서 리스트에 저장
        battleEnded = false;
        BattlePresentationDirector.Instance?.PrepareBattle(allCharacters);

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
        DisableDefenseButtons(allCharacters);

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

        foreach (CharacterManager character in allCharacters)
        {
            if (character != null)
                character.UpdateCharacterUI();
        }

        if (UIManager.Instance != null)
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
            currentCharacter.combatHandler.StopCoroutine(
                currentCharacter.combatHandler.turnTimerCoroutine);
            currentCharacter.combatHandler.turnTimerCoroutine = null;
        }

        if (currentCharacter.GetSkillQueue().Count > 0)
        {
            currentCharacter.combatHandler.AutoAssignDefaultCounterSkills();
            Debug.Log("CounterTurn Start");
            //대응턴 시작 30초 제한
            currentCharacter.combatHandler.turnTimerCoroutine =
                currentCharacter.combatHandler.StartCoroutine(
                    currentCharacter.combatHandler.TurnTimer(30f, EndTurn));

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
                            UIManager.Instance.characterTargeting.StartDefenseCharacterTargeting(defenseCharacter);
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
        if (manager == null)
            return;

        foreach (CharacterManager enemy in manager)
        {
            if (enemy != null && enemy.characterUIHandler != null &&
                enemy.characterUIHandler.CounterButton != null)
            {
                enemy.characterUIHandler.CounterButton.SetActive(false);
            }
        }
    }

    private void EndTurn()
    {
        if (battleEnded)
            return;

        DisableDefenseButtons(allCharacters);
        UIManager.Instance.characterTargeting.StopTargeting();
        UIManager.Instance.ClearCounterSkillPanel(); // 카운터 스킬 패널 초기화
        CombatHandler combatHandler = null;
        bool hasQueuedSkills = false;

        if (currentCharacter != null)
        {
            if (currentCharacter.combatHandler.turnTimerCoroutine != null)
            {
                currentCharacter.combatHandler.StopCoroutine(
                    currentCharacter.combatHandler.turnTimerCoroutine);
                currentCharacter.combatHandler.turnTimerCoroutine = null;
            }

            combatHandler = currentCharacter.GetComponent<CombatHandler>();
            hasQueuedSkills = currentCharacter.GetSkillQueue().Count > 0;
        }

        if (currentCharacter != null)
        {
            currentCharacter.isPlayerTurn = false;
            currentCharacter.UpdateCharacterUI();
        }

        if (combatHandler != null)
        {
            combatHandler.ExecuteSkillQueue(FinishTurn);
            if (hasQueuedSkills)
                CheckBattleEnd();
        }
        else
            FinishTurn();
    }

    private void FinishTurn()
    {
        ClearDefenseSelection();

        if (battleEnded)
            return;

        // 스킬 및 대응 스킬 적용이 끝난 뒤 승리/패배 조건 체크
        if (!CheckBattleEnd())
            StartNextTurn();
    }

    private void ApplyStatusEffectsToAll()
    {
        foreach (var characterManager in allCharacters)
        {
            if (characterManager == null || characterManager.character == null)
                continue;

            StatusEffectProcessor.ApplyTurnEffects(characterManager);
            StatusEffectProcessor.ReduceDurations(characterManager.character);

            characterManager.UpdateCharacterUI();
        }
    }

    private bool CheckBattleEnd()
    {
        if (battleEnded || allCharacters == null)
            return battleEnded;

        List<CharacterManager> allies = allCharacters
            .Where(c => c != null && c.character != null && c.character.IsMine)
            .ToList();
        List<CharacterManager> enemies = allCharacters
            .Where(c => c != null && c.character != null && !c.character.IsMine)
            .ToList();
        bool allAlliesDead = allies.Count > 0 && allies.All(c => !c.character.IsAlive);
        bool allEnemiesDead = enemies.Count > 0 && enemies.All(c => !c.character.IsAlive);

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
        if (battleEnded)
            return;

        battleEnded = true;
        _stageStarted = false;
        StopBattleActions();
        int grantedExperience = ProgressionRules.SettleVictoryExperience(allCharacters);
        List<InventorySlotData> droppedLoot = ProgressionRules.RollVictoryLoot(allCharacters);
        Debug.Log($"승리! 파티 경험치 총 지급량: {grantedExperience}");

        if (droppedLoot.Count > 0)
            Debug.Log($"전리품 발견: {droppedLoot.Count}개");

        System.Action completeVictory = () =>
        {
            QuestManager.Instance?.CompleteCurrentRouteNode();
            PlayerManager.Instance?.SavePlayerDataToPlayFab();

            if (QuestManager.Instance != null && QuestManager.Instance.active != null)
                UIManager.Instance?.OpenQuestNodeMap();
            else
                GameManager.Instance?.SwitchStage("Town");
        };

        System.Action continueAfterLoot = () =>
        {
            bool openedGrowth = UIManager.Instance != null &&
                UIManager.Instance.OpenPendingLevelUps(completeVictory);

            if (!openedGrowth)
                completeVictory();
        };

        bool openedLoot = UIManager.Instance != null &&
            UIManager.Instance.OpenBattleLoot(droppedLoot, continueAfterLoot);

        if (!openedLoot)
        {
            List<InventorySlotData> overflowLoot = CollectLootWithoutPanel(droppedLoot);
            DiscardUncollectedRuntimeLoot(overflowLoot);
            SharedInventoryUtility.SaveChanges();
            continueAfterLoot();
        }
    }

    private static List<InventorySlotData> CollectLootWithoutPanel(
        List<InventorySlotData> loot)
    {
        List<InventorySlotData> overflow = new List<InventorySlotData>();
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;
        List<InventorySlotData> storage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.ExpeditionStorage);

        if (loot == null || storage == null)
            return overflow;

        foreach (InventorySlotData slot in loot)
        {
            if (!SharedInventoryUtility.AddInventorySlot(storage, slot))
            {
                overflow.Add(slot);
                continue;
            }

            if (slot.IsGeneratedEquipment())
                EquipmentInstanceRepository.PromoteRuntimeToPlayer(slot.equipmentInstanceId);
        }

        SharedInventoryUtility.SaveChanges();
        return overflow;
    }

    private static void DiscardUncollectedRuntimeLoot(
        List<InventorySlotData> loot)
    {
        foreach (InventorySlotData slot in loot ?? new List<InventorySlotData>())
        {
            if (slot != null && slot.IsGeneratedEquipment())
                EquipmentInstanceRepository.RemoveRuntime(slot.equipmentInstanceId);
        }
    }

    private void HandleDefeat()
    {
        if (battleEnded)
            return;

        battleEnded = true;
        _stageStarted = false;
        StopBattleActions();
        RegisterExpeditionDefeat();
        Debug.Log("패배!");

        bool opened = UIManager.Instance != null &&
            UIManager.Instance.OpenBattleDefeat(ReturnToTownAfterDefeat);

        if (!opened)
            ReturnToTownAfterDefeat();
    }

    private void ReturnToTownAfterDefeat()
    {
        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData != null)
            playerData.currentStage = "Town";

        QuestManager.Instance?.AbandonActive();
        GameManager.Instance?.SwitchStage("Town");
        PlayerManager.Instance?.SavePlayerDataToPlayFab();
    }

    private void RegisterExpeditionDefeat()
    {
        QuestManager questManager = QuestManager.Instance;
        ActiveQuestRuntime failedExpedition = questManager != null
            ? questManager.active
            : null;

        if (failedExpedition?.def == null)
            return;

        List<string> defeatedCharacterIds = new List<string>();

        if (failedExpedition.partyCharacterIds != null)
        {
            foreach (string characterId in failedExpedition.partyCharacterIds)
            {
                if (!string.IsNullOrEmpty(characterId) &&
                    !defeatedCharacterIds.Contains(characterId))
                {
                    defeatedCharacterIds.Add(characterId);
                }
            }
        }

        if (defeatedCharacterIds.Count == 0 && allCharacters != null)
        {
            foreach (CharacterManager characterManager in allCharacters)
            {
                CharacterData character = characterManager != null
                    ? characterManager.character
                    : null;

                if (character != null &&
                    character.IsMine &&
                    !string.IsNullOrEmpty(character.ID) &&
                    !defeatedCharacterIds.Contains(character.ID))
                {
                    defeatedCharacterIds.Add(character.ID);
                }
            }
        }

        if (defeatedCharacterIds.Count == 0)
            return;

        PlayerManager playerManager = PlayerManager.Instance;
        PlayerData playerData = playerManager != null
            ? playerManager.GetCurrentPlayerData()
            : null;

        if (playerData == null)
            return;

        if (playerData.missingCharacterIds == null)
            playerData.missingCharacterIds = new List<string>();

        if (playerData.revivalRequiredCharacterIds == null)
            playerData.revivalRequiredCharacterIds = new List<string>();

        List<InventorySlotData> expeditionStorage = SharedInventoryUtility.GetStorage(
            playerData,
            SharedInventoryType.ExpeditionStorage);
        SharedInventoryUtility.ExpireMonsterEssences(
            expeditionStorage,
            failedExpedition.def.id);
        SharedInventoryUtility.ArchiveDefeatedExpedition(
            playerData,
            failedExpedition.def.id,
            defeatedCharacterIds);

        foreach (string characterId in defeatedCharacterIds)
        {
            CharacterManager battleCharacter = allCharacters?.Find(character =>
                character != null &&
                character.character != null &&
                character.character.ID == characterId);
            CharacterManager pooledCharacter = CharacterPoolManager.Instance != null
                ? CharacterPoolManager.Instance.Get(characterId)
                : null;

            SetCharacterDead(battleCharacter?.character);
            SetCharacterDead(pooledCharacter?.character);

            CharacterData characterToSave = battleCharacter?.character ?? pooledCharacter?.character;

            if (characterToSave != null)
            {
                playerManager.SaveCharacter(characterToSave);
            }
            else
            {
                string missingCharacterId = characterId;

                playerManager.LoadCharacter(
                    missingCharacterId,
                    character =>
                    {
                        if (character == null)
                            return;

                        SetCharacterDead(character);
                        playerManager.SaveCharacter(character);
                    });
            }

            playerData.characterIds?.RemoveAll(id => id == characterId);
            playerData.activeCharacterIds?.RemoveAll(id => id == characterId);
            playerData.revivalRequiredCharacterIds.RemoveAll(id => id == characterId);
            playerData.RemovePosition(characterId);

            if (!playerData.missingCharacterIds.Contains(characterId))
                playerData.missingCharacterIds.Add(characterId);
        }

        playerData.currentStage = "Town";

        questManager.IssueRescueQuest(
            failedExpedition.def,
            defeatedCharacterIds);
        questManager.AbandonActive();
        playerManager.SavePlayerDataToPlayFab();
    }

    private static void SetCharacterDead(CharacterData character)
    {
        if (character == null)
            return;

        character.CurrentHp = 0;
        character.IsAlive = false;
    }

    private void StopBattleActions()
    {
        turnQueue.Clear();
        DisableDefenseButtons(allCharacters);
        ClearDefenseSelection();
        BattlePresentationDirector.Instance?.CancelAndRestore();

        if (allCharacters == null)
            return;

        foreach (CharacterManager character in allCharacters)
        {
            if (character == null)
                continue;

            character.isPlayerTurn = false;
            character.combatHandler?.StopAllCoroutines();
            character.UpdateCharacterUI();
        }
    }

    private void ClearDefenseSelection()
    {
        CharacterManager previousDefenseCharacter = defenseCharacter;

        if (previousDefenseCharacter != null && previousDefenseCharacter.combatHandler != null)
            previousDefenseCharacter.combatHandler.isDefenseCharacter = false;

        defenseCharacter = null;
        defenseTarget = null;

        if (allCharacters != null)
        {
            foreach (CharacterManager character in allCharacters)
            {
                if (character != null && character.combatHandler != null)
                    character.combatHandler.isDefenseTarget = false;
            }
        }

        if (previousDefenseCharacter != null && previousDefenseCharacter != currentCharacter)
            previousDefenseCharacter.UpdateCharacterUI();
    }

    private static bool HasBothBattleSides(List<CharacterManager> roster)
    {
        if (roster == null)
            return false;

        bool hasAlly = roster.Any(c =>
            c != null && c.character != null && c.character.IsMine);
        bool hasEnemy = roster.Any(c =>
            c != null && c.character != null && !c.character.IsMine);
        return hasAlly && hasEnemy;
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
            UIManager.Instance.counterTurnEndButton.gameObject.SetActive(false);
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
                UIManager.Instance.counterTurnEndButton.gameObject.SetActive(false);
                if (currentCharacter != null)
                {
                    CombatHandler combatHandler = currentCharacter.GetComponent<CombatHandler>();
                    if (combatHandler != null)
                    {
                        EndTurn();                        
                    }
                }
            });
        }
    }
}
