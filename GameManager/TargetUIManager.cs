using UnityEngine;
using UnityEngine.UI;

public class TargetUIManager : MonoBehaviour
{
    public GameObject skillIconPrefab;

    public void DisplaySkillIcon(SkillBase skill, CharacterManager target)
    {
        GameObject skillIcon = Instantiate(skillIconPrefab, target.transform.position + Vector3.up * 2, Quaternion.identity);
        skillIcon.GetComponentInChildren<RawImage>().texture = skill.icon;
        //skillIcon.GetComponentInChildren<Text>().text = skillQueue.IndexOf(skill).ToString();

        skillIcon.transform.SetParent(target.transform);
    }
}