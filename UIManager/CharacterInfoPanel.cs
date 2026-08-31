using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoPanel : MonoBehaviour
{
    [Header("Refs")]
    public RawImage portrait;
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text hpText;

    // UI 순서: 힘, 기교, 속도, 눈썰미, 인내, 지능, 지혜, 건강, 통찰
    public TMP_Text[] baseStats;

    [Header("Buttons")]
    public Button btnEquipment;
    public Button btnInventory;
    public Button btnSkills;

    CharacterManager characterManager;
    CharacterData characterData;

    public void Bind(CharacterManager cm)
    {
        characterManager = cm;
        characterData = cm != null ? cm.character : null;

        WireButtons(false);
        UpdateBasics();
        WireButtons(true);
    }

    public void Bind(CharacterData data)
    {
        characterManager = null;
        characterData = data;

        WireButtons(false);
        UpdateBasics();
    }

    void WireButtons(bool on)
    {
        if (!on)
        {
            if (btnEquipment != null) btnEquipment.onClick.RemoveAllListeners();
            if (btnInventory != null) btnInventory.onClick.RemoveAllListeners();
            if (btnSkills != null) btnSkills.onClick.RemoveAllListeners();
            return;
        }

        if (btnEquipment != null) btnEquipment.onClick.AddListener(OpenEquipment);
        if (btnInventory != null) btnInventory.onClick.AddListener(OpenInventory);
        if (btnSkills != null) btnSkills.onClick.AddListener(OpenSkills);
    }

    void UpdateBasics()
    {
        if (characterData == null)
            return;

        CharacterData c = characterData;

        if (portrait != null)
            portrait.texture = c.Portrait;

        if (nameText != null)
            nameText.text = c.Name;

        if (levelText != null)
            levelText.text = "Lv." + c.Level;

        if (hpText != null && c.FinalStats != null)
            hpText.text = c.CurrentHp + "/" + c.FinalStats.MaxHp;

        if (baseStats != null && baseStats.Length > 0 && c.FinalStats != null)
        {
            CharacterStats fs = c.FinalStats;

            string[] vals =
            {
                fs.Strength.ToString(),
                fs.Dexterity.ToString(),
                fs.Speed.ToString(),
                fs.Detection.ToString(),
                fs.Endurance.ToString(),
                fs.Intelligence.ToString(),
                fs.Wisdom.ToString(),
                fs.Health.ToString(),
                fs.Insight.ToString(),
                fs.Vitality.ToString()
            };

            for (int i = 0; i < baseStats.Length && i < vals.Length; i++)
            {
                if (baseStats[i] != null)
                    baseStats[i].text = vals[i];
            }
        }
    }

    void OpenEquipment()
    {
        if (characterManager == null)
            return;

        Debug.Log("SO 기반 장비 패널 연결 필요");

        // 예시:
        // UIManager.Instance.OpenCharacterEquipmentPanel(characterManager);
    }

    void OpenInventory()
    {
        Debug.Log("캐릭터 개별 인벤토리는 제거됨. 계정/원정대 창고 패널로 연결 필요");

        // 예시:
        // UIManager.Instance.OpenStoragePanel();
    }

    void OpenSkills()
    {
        if (characterManager == null)
            return;

        Debug.Log("SO 기반 스킬 패널 연결 필요");

        // 예시:
        // UIManager.Instance.OpenSkillPanel(characterManager);
    }

    public void Refresh()
    {
        UpdateBasics();
    }
}
