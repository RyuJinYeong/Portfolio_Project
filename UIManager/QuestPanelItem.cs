using SoftKitty.InventoryEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanelItem : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI issuerText;
    public TextMeshProUGUI tierDifficultyText;
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI monsterText;
    public TextMeshProUGUI bossText;
    public TextMeshProUGUI recommendText;   // 예: "권장 레벨 4 / 권장 3명 (최대 5명)"
    public TextMeshProUGUI environmentText;
    public TextMeshProUGUI rewardText;

    [Header("Buttons")]
    public Button reserveButton;
    public Button acceptButton;
    public Button selectButton;

    [Header("Display Mode")]
    public bool detailViewMode;

    [Header("Issuer Accent")]
    public Image issuerTopStrip;
    public Image issuerTitleLine;

    [Header("Issuer Icon")]
    public Image issuerIconPrimary;
    public Image issuerIconSecondary;
    public Sprite warriorGuildIconPrimary;
    public Sprite warriorGuildIconSecondary;
    public Sprite knightOrderIcon;
    public Sprite hunterGuildIconPrimary;
    public Sprite hunterGuildIconSecondary;
    public Sprite mageTowerIcon;
    public Sprite adventurerGuildIcon;

    [Header("State Widgets")]
    public GameObject reservedBadge;        // 예약 상태 뱃지(선택)
    public Color reservedColor = new Color(0.9f, 0.9f, 1f);
    public Color normalColor = Color.white;

    // 내부 상태
    private QuestBoardEntry entry;
    private System.Action<QuestBoardEntry> onToggleReserve;
    private System.Action<QuestBoardEntry> onAccept;
    private System.Action<QuestBoardEntry> onSelect;

    public void Bind(
        QuestBoardEntry e,
        System.Action<QuestBoardEntry> onToggleReserve,
        System.Action<QuestBoardEntry> onAccept,
        System.Action<QuestBoardEntry> onSelect = null)
    {
        entry = e;
        this.onToggleReserve = onToggleReserve;
        this.onAccept = onAccept;
        this.onSelect = onSelect;

        var def = entry.def;

        titleText.text = def.isRescueQuest
            ? $"[구출] {def.title}"
            : def.title;

        issuerText.gameObject.SetActive(false);
        tierDifficultyText.text = $"T{def.tier} · {GetDifficultyText(def.difficulty)}";


        QuestStageDefinitionSO stage =
            GameDataRegistry.Instance != null
                ? GameDataRegistry.Instance.GetQuestStage(def.stageKey)
                : null;


        stageText.text = $"스테이지: {(stage != null ? stage.stageName : def.stageKey)}";

        monsterText.text = def.isRescueQuest
            ? $"목표: 실종된 원정대원 {def.rescueCharacterIds?.Count ?? 0}명 구출\n" +
              $"등장 몬스터군: {GetMonsterGroupText(def)}"
            : $"등장 몬스터군: {GetMonsterGroupText(def)}";


        MonsterRoleSO boss =
            GameDataRegistry.Instance != null && def.bossUid != 0
                ? GameDataRegistry.Instance.GetMonsterRole(def.bossUid)
                : null;


        bossText.gameObject.SetActive(boss != null);


        if (boss != null)
            bossText.text = $"보스: {boss.roleName}";


        recommendText.text = detailViewMode
            ? $"권장 레벨: Lv.{def.recommendedLevel}\n" +
              $"파티 인원: 권장 {def.recommendedPartySize}명 / 최대 {def.maxPartySize}명"
            : $"권장 인원: {def.recommendedPartySize}~{def.maxPartySize}명";


        string environment =
            GetEnvironmentText(stage);


        environmentText.gameObject.SetActive(
            !string.IsNullOrEmpty(environment));


        if (!string.IsNullOrEmpty(environment))
            environmentText.text = $"특수 환경: {environment}";


        rewardText.text =
            $"주요 보상: {GetRewardText(def.reward)}";

        Color issuerColor = GetIssuerColor(def.issuer);

        if (issuerTopStrip != null)
            issuerTopStrip.color = issuerColor;

        if (issuerTitleLine != null)
            issuerTitleLine.color = issuerColor;

        ApplyIssuerIcon(def.issuer);
        ApplyDisplayMode();

        // 예약 상태 반영
        ApplyReservedVisual(entry.status == QuestStatus.Reserved);

        // 버튼 리스너 갱신
        reserveButton.onClick.RemoveAllListeners();
        acceptButton.onClick.RemoveAllListeners();

        if (selectButton != null)
            selectButton.onClick.RemoveAllListeners();

        if (!detailViewMode)
        {
            reserveButton.onClick.AddListener(() => {
                onToggleReserve?.Invoke(entry);
            });
        }

        if (detailViewMode)
        {
            acceptButton.onClick.AddListener(() => {
                // 여기서는 파티 구성 화면으로 넘기는 신호만 보냄
                onAccept?.Invoke(entry);
            });
        }

        if (selectButton != null && onSelect != null)
        {
            selectButton.onClick.AddListener(() => {
                this.onSelect?.Invoke(entry);
            });
        }
    }

    public void ApplyReservedVisual(bool isReserved)
    {
        if (reservedBadge) reservedBadge.SetActive(isReserved);

        Image reserveImage = reserveButton != null
            ? reserveButton.image
            : null;


        if (reserveImage != null)
        {
            Color issuerColor = entry != null && entry.def != null
                ? GetIssuerColor(entry.def.issuer)
                : normalColor;

            reserveImage.color = isReserved
                ? Color.Lerp(issuerColor, Color.white, 0.25f)
                : issuerColor;
        }
    }


    private void ApplyDisplayMode()
    {
        monsterText.gameObject.SetActive(detailViewMode);
        bossText.gameObject.SetActive(
            detailViewMode && bossText.gameObject.activeSelf);
        environmentText.gameObject.SetActive(
            detailViewMode && environmentText.gameObject.activeSelf);

        reserveButton.gameObject.SetActive(!detailViewMode);
        acceptButton.gameObject.SetActive(detailViewMode);

        if (selectButton != null)
            selectButton.interactable = !detailViewMode;
    }


    private void ApplyIssuerIcon(QuestIssuer issuer)
    {
        Sprite primary = null;
        Sprite secondary = null;


        if (issuerIconPrimary != null)
        {
            issuerIconPrimary.rectTransform.anchoredPosition = Vector2.zero;
            issuerIconPrimary.rectTransform.localRotation = Quaternion.identity;
            issuerIconPrimary.rectTransform.localScale = Vector3.one;
        }


        if (issuerIconSecondary != null)
        {
            issuerIconSecondary.rectTransform.anchoredPosition = Vector2.zero;
            issuerIconSecondary.rectTransform.localRotation = Quaternion.identity;
            issuerIconSecondary.rectTransform.localScale = Vector3.one;
        }


        switch (issuer)
        {
            case QuestIssuer.WarriorGuild:
                primary = warriorGuildIconPrimary;
                secondary = warriorGuildIconSecondary;

                if (issuerIconPrimary != null)
                    issuerIconPrimary.rectTransform.anchoredPosition = new Vector2(-0.0069f, 0f);

                if (issuerIconSecondary != null)
                {
                    issuerIconSecondary.rectTransform.anchoredPosition = new Vector2(0.0088f, 0f);
                    issuerIconSecondary.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                }
                break;

            case QuestIssuer.SwordDojo:
                primary = knightOrderIcon;
                break;

            case QuestIssuer.HunterGuild:
            case QuestIssuer.ThievesGuild:
                primary = hunterGuildIconPrimary;
                secondary = hunterGuildIconSecondary;

                if (issuerIconPrimary != null)
                    issuerIconPrimary.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 7f);

                if (issuerIconSecondary != null)
                    issuerIconSecondary.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -75f);
                break;

            case QuestIssuer.MageTower:
                primary = mageTowerIcon;
                break;

            case QuestIssuer.TownCouncil:
                primary = adventurerGuildIcon;
                break;
        }


        if (issuerIconPrimary != null)
        {
            issuerIconPrimary.sprite = primary;
            issuerIconPrimary.enabled = primary != null;
        }


        if (issuerIconSecondary != null)
        {
            issuerIconSecondary.sprite = secondary;
            issuerIconSecondary.enabled = secondary != null;
        }
    }


    private Color GetIssuerColor(QuestIssuer issuer)
    {
        return issuer switch
        {
            QuestIssuer.WarriorGuild => new Color32(122, 56, 47, 255),
            QuestIssuer.SwordDojo => new Color32(101, 121, 140, 255),
            QuestIssuer.HunterGuild => new Color32(47, 90, 58, 255),
            QuestIssuer.MageTower => new Color32(107, 63, 120, 255),
            QuestIssuer.TownCouncil => new Color32(166, 124, 50, 255),
            _ => normalColor
        };
    }


    private string GetMonsterGroupText(QuestDef def)
    {
        if (def.enemyPool == null ||
            GameDataRegistry.Instance == null)
        {
            return "정보 없음";
        }


        List<string> monsterNames =
            new List<string>();


        foreach (MonsterSpawn spawn in def.enemyPool)
        {
            if (spawn == null ||
                spawn.monsterUid == def.bossUid)
            {
                continue;
            }


            MonsterRoleSO monster =
                GameDataRegistry.Instance.GetMonsterRole(
                    spawn.monsterUid);


            if (monster == null ||
                string.IsNullOrEmpty(monster.roleName) ||
                monsterNames.Contains(monster.roleName))
            {
                continue;
            }


            monsterNames.Add(monster.roleName);
        }


        return monsterNames.Count > 0
            ? string.Join(", ", monsterNames)
            : "정보 없음";
    }


    private string GetEnvironmentText(
        QuestStageDefinitionSO stage)
    {
        if (stage == null ||
            stage.statusEffectIds == null ||
            stage.statusEffectIds.Count == 0 ||
            GameDataRegistry.Instance == null)
        {
            return "";
        }


        List<string> statusNames =
            new List<string>();


        foreach (int statusId in stage.statusEffectIds)
        {
            StatusEffectDefinitionSO status =
                GameDataRegistry.Instance.GetStatusEffect(statusId);


            if (status != null &&
                !string.IsNullOrEmpty(status.statusName))
            {
                statusNames.Add(status.statusName);
            }
        }


        return string.Join(", ", statusNames);
    }


    public static string GetRewardText(QuestReward reward)
    {
        if (reward == null)
            return "정보 없음";


        if (reward.kind == QuestRewardKind.Equipment)
        {
            return
                $"T{reward.tier} " +
                $"{GetRarityText(reward.maxEquipmentRarity)}등급 이하 " +
                GetEquipmentRewardCategoryText(reward);
        }


        if (reward.kind == QuestRewardKind.Skill)
        {
            return
                $"T{reward.tier} " +
                $"{GetSkillDisciplineText(reward.skillDiscipline)} 계열 스킬";
        }


        if (reward.equipmentUids != null &&
            reward.equipmentUids.Count > 0)
        {
            EquipmentDefinitionSO equipment =
                GameDataRegistry.Instance != null
                    ? GameDataRegistry.Instance.GetEquipment(
                        reward.equipmentUids[0])
                    : null;


            return equipment != null
                ? equipment.itemName
                : "장비 보상";
        }


        if (reward.skillUids != null &&
            reward.skillUids.Count > 0)
        {
            SkillDefinitionSO skill =
                GameDataRegistry.Instance != null
                    ? GameDataRegistry.Instance.GetSkill(
                        reward.skillUids[0])
                    : null;


            return skill != null
                ? skill.skillName
                : "스킬 보상";
        }


        return "정보 없음";
    }


    private static string GetEquipmentRewardCategoryText(
        QuestReward reward)
    {
        switch (reward.equipmentFilter)
        {
            case QuestEquipmentRewardFilter.WeaponType:
                return GetWeaponTypeText(reward.weaponType);

            case QuestEquipmentRewardFilter.ArmorCategory:
                return GetArmorCategoryText(reward.armorCategory);

            case QuestEquipmentRewardFilter.EquipmentType:
                return GetEquipmentTypeText(reward.equipmentType);

            case QuestEquipmentRewardFilter.Any:
                return "장비";

            default:
                return "장비";
        }
    }


    private string GetIssuerText(QuestIssuer issuer)
    {
        return issuer switch
        {
            QuestIssuer.HunterGuild => "사냥꾼 길드",
            QuestIssuer.WarriorGuild => "전사 길드",
            QuestIssuer.SwordDojo => "기사단",
            QuestIssuer.MageTower => "마법탑",
            QuestIssuer.TownCouncil => "모험가 길드",
            QuestIssuer.ThievesGuild => "도적 길드",
            _ => issuer.ToString()
        };
    }


    private string GetDifficultyText(QuestDifficulty difficulty)
    {
        return difficulty switch
        {
            QuestDifficulty.Easy => "쉬움",
            QuestDifficulty.Normal => "보통",
            QuestDifficulty.Hard => "어려움",
            _ => difficulty.ToString()
        };
    }


    private static string GetRarityText(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => "일반",
            EquipmentRarity.Uncommon => "고급",
            EquipmentRarity.Rare => "희귀",
            EquipmentRarity.Epic => "영웅",
            EquipmentRarity.Legendary => "전설",
            _ => rarity.ToString()
        };
    }


    private static string GetArmorCategoryText(ArmorCategory category)
    {
        return category switch
        {
            ArmorCategory.HeavyArmor => "중갑",
            ArmorCategory.LightArmor => "경갑",
            ArmorCategory.ClothArmor => "의복",
            _ => category.ToString()
        };
    }


    private static string GetWeaponTypeText(WeaponType type)
    {
        return type switch
        {
            WeaponType.Two_HandedSword => "양손검",
            WeaponType.Greatsword => "대검",
            WeaponType.LongSword => "장검",
            WeaponType.Dagger => "단검",
            WeaponType.Bow => "활",
            WeaponType.Mace => "철퇴",
            WeaponType.Hammer => "망치",
            WeaponType.Shield => "방패",
            WeaponType.Axe => "도끼",
            WeaponType.Spear => "창",
            WeaponType.Staff => "지팡이",
            WeaponType.Book => "마도서",
            WeaponType.Orb => "보주",
            _ => type.ToString()
        };
    }


    private static string GetEquipmentTypeText(EquipmentType type)
    {
        return type switch
        {
            EquipmentType.Helmet => "투구",
            EquipmentType.Armor => "갑옷",
            EquipmentType.Gloves => "장갑",
            EquipmentType.Shoes => "신발",
            EquipmentType.Ring => "반지",
            EquipmentType.Necklace => "목걸이",
            EquipmentType.Weapon => "무기",
            EquipmentType.SubWeapon => "보조무기",
            _ => type.ToString()
        };
    }


    private static string GetSkillDisciplineText(
        SkillDiscipline discipline)
    {
        return discipline switch
        {
            SkillDiscipline.Basic => "기본기",
            SkillDiscipline.WeaponArt => "무기술",
            SkillDiscipline.Swordsmanship => "검술",
            SkillDiscipline.Archery => "궁술",
            SkillDiscipline.ShieldArt => "방패술",
            SkillDiscipline.MartialArt => "체술",
            SkillDiscipline.DaggerArt => "단검술",
            SkillDiscipline.Magic => "마법",
            _ => discipline.ToString()
        };
    }
}
