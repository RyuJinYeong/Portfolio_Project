using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoftKitty.InventoryEngine;


//캐릭터 관리 패널 하위의 캐릭터 정보 제어

public class CharacterInfoPanel : MonoBehaviour
{
    [Header("Refs")]
    public RawImage portrait;
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text hpText;
    public TMP_Text[] baseStats; // UI 순서 (힘, 기교, 속도, 눈썰미, 인내, 지능, 지혜, 건강, 통찰)

    [Header("Buttons")]
    public Button btnEquipment;
    public Button btnInventory;
    public Button btnSkills;

    CharacterManager characterManager;

    public void Bind(CharacterManager cm)
    {
        characterManager = cm;
        WireButtons(false);
        UpdateBasics();
        WireButtons(true);
    }

    void WireButtons(bool on)
    {
        if (!on)
        {
            if (btnEquipment) btnEquipment.onClick.RemoveAllListeners();
            if (btnInventory) btnInventory.onClick.RemoveAllListeners();
            if (btnSkills) btnSkills.onClick.RemoveAllListeners();
            return;
        }

        if (btnEquipment) btnEquipment.onClick.AddListener(OpenEquipment);
        if (btnInventory) btnInventory.onClick.AddListener(OpenInventory);
        if (btnSkills) btnSkills.onClick.AddListener(OpenSkills);
    }

    void UpdateBasics()
    {
        if (characterManager == null) return;
        var c = characterManager.character;

        portrait.texture = c.Portrait;
        nameText.text = c.Name;
        levelText.text = "Lv." + c.Level;
        hpText.text = c.CurrentHp + "/" + c.FinalStats.MaxHp;

        if (baseStats != null && baseStats.Length > 0)
        {
            var fs = c.FinalStats;
            string[] vals = new string[]
            {
                fs.Strength.ToString(),
                fs.Dexterity.ToString(),
                fs.Speed.ToString(),
                fs.Detection.ToString(),
                fs.Endurance.ToString(),
                fs.Intelligence.ToString(),
                fs.Wisdom.ToString(),
                fs.Health.ToString(),
                fs.Insight.ToString()
            };
            for (int i = 0; i < baseStats.Length && i < vals.Length; i++)
                baseStats[i].text = vals[i];
        }
    }

    void OpenEquipment()
    {
        if (characterManager == null) return;

        characterManager.character.CharacterEquipment.OpenWindow();
    }

    void OpenInventory()
    {
        if (characterManager == null) return;

        characterManager.character.CharacterInventory.OpenWindow();
    }

    void OpenSkills()
    {
        if (characterManager == null) return;

        characterManager.character.CharacterInventory.OpenWindowByName("Skills", "Skills");
    }

    // 능력치 갱신 시 호출 필요
    public void Refresh()
    {
        UpdateBasics();
    }
}
