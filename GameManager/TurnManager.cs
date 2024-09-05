using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private Queue<CharacterManager> turnQueue = new Queue<CharacterManager>();
    private CharacterManager currentCharacter;
    private GameManager gameManager;

    private void Awake()
    {
        gameManager = GameManager.Instance;
    }

    public void InitializeTurnOrder()
    {
        List<CharacterManager> allCharacters = gameManager.GetAllCharacters();

        var sortedCharacters = allCharacters.OrderByDescending(c => Mathf.Max((float)c.character.FinalStats.AttackSpeed, (float)c.character.FinalStats.CastSpeed))
                                            .ThenByDescending(c => Mathf.Max(c.character.FinalStats.Speed, c.character.FinalStats.Wisdom))
                                            .ThenByDescending(c => c.character.Type)
                                            .ToList();

        foreach (var character in sortedCharacters)
        {
            turnQueue.Enqueue(character);
        }

        StartNextTurn();
    }

    public void StartNextTurn()
    {
        if (turnQueue.Count == 0)
        {
            // 모든 턴이 끝난 경우, 턴을 다시 초기화하거나 전투 종료 처리
            InitializeTurnOrder();
            return;
        }

        currentCharacter = turnQueue.Dequeue();
        gameManager.SetCurrentCharacter(currentCharacter);

        RecoverResources(currentCharacter);
        ApplyStatusEffects(currentCharacter);

        // CharacterManager에게 턴을 맡김
        currentCharacter.StartTurn(OnTurnEnd);
    }

    public void EndTurn()
    {
        turnQueue.Enqueue(currentCharacter);
        StartNextTurn();
    }

    public void UseSkill(SkillBase skill, CharacterManager target)
    {
        currentCharacter.UseSkill(skill, target);
        EndTurn();
    }

    private void RecoverResources(CharacterManager characterManager)
    {
        characterManager.character.FinalStats.CurrentStamina = Mathf.Min(characterManager.character.FinalStats.MaxStamina,
            characterManager.character.FinalStats.CurrentStamina + characterManager.character.FinalStats.StaminaRecovery);

        characterManager.character.FinalStats.CurrentMentality = Mathf.Min(characterManager.character.FinalStats.MaxMentality,
            characterManager.character.FinalStats.CurrentMentality + characterManager.character.FinalStats.MentalityRecovery);

        characterManager.UpdateCharacterUI();
    }

    private void ApplyStatusEffects(CharacterManager characterManager)
    {
        foreach (var statusEffect in characterManager.character.StatusEffects.ToList())
        {
            statusEffect.ApplyEffect(characterManager.character);

            if (statusEffect.IsExpired())
            {
                characterManager.character.StatusEffects.Remove(statusEffect);
            }
        }

        characterManager.UpdateCharacterUI();
    }

    // 턴 종료 콜백
    private void OnTurnEnd()
    {
        EndTurn();
    }
}
