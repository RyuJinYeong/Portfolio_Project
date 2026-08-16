using UnityEngine;

public class GameDataRegistryTester : MonoBehaviour
{
    public int testSkillUid = 3000;
    public int testEquipmentUid = 1004;
    public int testTraitId = 1000;
    public TraitGrade testTraitGrade = TraitGrade.C;
    public Origin testOrigin = Origin.wandering_knight;

    void Start()
    {
        var registry = GameDataRegistry.Instance;

        Debug.Log(registry.GetSkill(testSkillUid) != null
            ? $"Skill OK: {registry.GetSkill(testSkillUid).skillName}"
            : $"Skill Missing: {testSkillUid}");

        Debug.Log(registry.GetEquipment(testEquipmentUid) != null
            ? $"Equipment OK: {registry.GetEquipment(testEquipmentUid).itemName}"
            : $"Equipment Missing: {testEquipmentUid}");

        Debug.Log(registry.GetTrait(testTraitId) != null
            ? $"Trait OK: {registry.GetTrait(testTraitId).traitName}"
            : $"Trait Missing: {testTraitId}");

        var traitsByGrade = registry.GetTraitsByGrade(testTraitGrade);
        Debug.Log($"Trait Grade {testTraitGrade} Count: {traitsByGrade.Count}");

        var randomTrait = registry.GetRandomTraitByGrade(testTraitGrade);
        Debug.Log(randomTrait != null
            ? $"Random Trait OK: {randomTrait.traitName} [{randomTrait.defaultAcquireGrade}]"
            : $"Random Trait Missing: {testTraitGrade}");

        Debug.Log(registry.GetOrigin(1000) != null
            ? $"Origin OK: {registry.GetOrigin(1000).originName}"
            : $"Origin Missing: {testOrigin}");
    }
}