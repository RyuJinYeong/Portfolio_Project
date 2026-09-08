using UnityEngine;
using UnityEngine.EventSystems;

public class TurnOrderPortraitUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private CharacterManager character;
    private CharacterTargeting characterTargeting;

    public void Bind(
        CharacterManager targetCharacter,
        CharacterTargeting targeting)
    {
        character = targetCharacter;
        characterTargeting = targeting;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        characterTargeting?.PreviewCharacter(character);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        characterTargeting?.ClearCharacterPreview(character);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            characterTargeting?.SelectCharacter(character);
    }

    private void OnDisable()
    {
        characterTargeting?.ClearCharacterPreview(character);
    }
}
