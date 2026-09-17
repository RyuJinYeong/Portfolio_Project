using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Buff")]
public class BuffDefinitionSO : StatusEffectDefinitionSO
{
    public int blockedStatusId;
    [Min(1)] public int incomingAttackDuration = 3;

    private void OnEnable()
    {
        isDebuff = false;
    }
}
