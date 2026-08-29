using UnityEngine;

public class TownCharacterStatsDetailUI : MonoBehaviour
{
    public TownCharacterStatValueUI[] statValues;

    private void Awake()
    {
        statValues = GetComponentsInChildren<TownCharacterStatValueUI>(true);
    }

    public void Refresh(CharacterStats stats)
    {
        if (statValues == null)
            return;

        foreach (TownCharacterStatValueUI statValue in statValues)
        {
            if (statValue != null)
                statValue.Refresh(stats);
        }
    }
}
