using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public SkillDefinitionSO skill;
    public SkillQueueData queueData;

    public int queueIndex = -1;
    public bool isCounterSkill;
    public SkillQueueData pairedAttackQueueData;
    public SkillDefinitionSO pairedCounterSkill;
    public CharacterManager counterUser;
    public CharacterManager protectedTarget;
    public System.Action onRightClick;
    public bool isConcealToggle;
    public RawImage concealBadge;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;

        TooltipManager.Instance.hoveredBattleIcon = gameObject;
        if (isConcealToggle)
        {
            TooltipManager.Instance.ShowSkillConcealTooltip(Input.mousePosition);
            return;
        }

        bool hasCounterChanceContext =
            isCounterSkill &&
            pairedAttackQueueData != null &&
            pairedAttackQueueData.skill != null &&
            counterUser != null &&
            protectedTarget != null;

        if (!hasCounterChanceContext && queueData != null && queueData.skill != null)
        {
            TooltipManager.Instance.ShowTooltip(queueData, Input.mousePosition);
        }
        else if (!hasCounterChanceContext && skill != null)
        {
            TooltipManager.Instance.ShowTooltip(skill, Input.mousePosition);
        }

        ShowCounterSuccessChance();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;

        if (TooltipManager.Instance.hoveredBattleIcon == gameObject)
            TooltipManager.Instance.HideTooltip();
    }

    private void OnDisable()
    {
        if (TooltipManager.Instance?.hoveredBattleIcon == gameObject)
            TooltipManager.Instance.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke();
    }

    public void SetConcealedBadge(bool concealed, Texture2D icon)
    {
        if (concealBadge == null)
            return;

        concealBadge.texture = icon;
        concealBadge.gameObject.SetActive(concealed && icon != null);
    }

    private void ShowCounterSuccessChance()
    {
        if (pairedAttackQueueData == null ||
            pairedAttackQueueData.skill == null ||
            counterUser == null ||
            counterUser.combatHandler == null ||
            protectedTarget == null)
        {
            return;
        }

        SkillDefinitionSO counterSkill = pairedCounterSkill;
        CharacterTargeting targeting = UIManager.Instance != null
            ? UIManager.Instance.characterTargeting
            : null;

        if (targeting != null &&
            targeting.isDefenseSkillTargeting &&
            targeting.selectedSkill != null)
        {
            counterSkill = targeting.selectedSkill;
        }

        if (counterSkill == null)
            return;

        int chance = counterUser.combatHandler.GetCounterSuccessChancePreview(
            pairedAttackQueueData.skill,
            pairedAttackQueueData.user,
            protectedTarget,
            counterSkill,
            pairedAttackQueueData.revealLevel);

        TooltipManager.Instance.ShowCounterSuccessChance(
            chance,
            counterSkill.style,
            pairedAttackQueueData.skill.style);
    }
}
