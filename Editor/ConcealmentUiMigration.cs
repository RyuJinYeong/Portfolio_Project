#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ConcealmentUiMigration
{
    private const string SessionKey = "LL.ConcealmentUiMigration.ResourceLineFix.20260912";
    private const string UiManagerPrefabPath = "Assets/Prefab/UIManager_Canvas.prefab";
    private const string CharacterStatusPrefabPath = "Assets/Prefab/CharacterStatusCanvas.prefab";
    private const string SkillQueuePrefabPath = "Assets/Prefab/UIPrefab/SkillQueueButton.prefab";
    private const string DefenseQueuePrefabPath = "Assets/Prefab/UIPrefab/DefenseSkillQueue.prefab";
    private const string GameManagerPrefabPath = "Assets/Prefab/GameManager.prefab";
    private const string LacerationSkillPath = "Assets/SO/Skills/WeaponArt/2600_상처내기.asset";
    private const string BaseSlashSkillPath = "Assets/SO/Skills/WeaponArt/2002_참격.asset";
    private const string HelpIconPath = "Assets/Assets/ClassicRPGUI2/IconsUI/Icon_Help_t.png";
    private const string ConcealEnabledIconPath = "Assets/Assets/Clean Vector Icons/T_5_eye_cross_.png";
    private const string ConcealDisabledIconPath = "Assets/Assets/Clean Vector Icons/T_1_eye_.png";

    static ConcealmentUiMigration()
    {
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        try
        {
            Texture2D helpIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(HelpIconPath);
            Texture2D concealEnabledIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ConcealEnabledIconPath);
            Texture2D concealDisabledIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ConcealDisabledIconPath);

            if (helpIcon == null || concealEnabledIcon == null || concealDisabledIcon == null)
                throw new System.InvalidOperationException("은닉 UI 아이콘 에셋을 찾을 수 없습니다.");

            SkillDefinitionSO lacerationSkill = CreateOrUpdateLacerationSkill();
            RegisterSkill(lacerationSkill);
            UpdateUiManagerPrefab(helpIcon, concealEnabledIcon, concealDisabledIcon);
            UpdateCharacterStatusPrefab(helpIcon, concealEnabledIcon);
            UpdateQueuePrefab(SkillQueuePrefabPath, concealEnabledIcon);
            UpdateQueuePrefab(DefenseQueuePrefabPath, concealEnabledIcon);

            AssetDatabase.SaveAssets();
            SessionState.SetBool(SessionKey, true);
            Debug.Log("[LLConcealmentUiMigration] Completed");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static SkillDefinitionSO CreateOrUpdateLacerationSkill()
    {
        SkillDefinitionSO skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(LacerationSkillPath);

        if (skill == null)
        {
            skill = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            AssetDatabase.CreateAsset(skill, LacerationSkillPath);
        }

        SkillDefinitionSO baseSkill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(BaseSlashSkillPath);
        skill.uid = 2600;
        skill.skillName = "상처내기";
        skill.description = "날카로운 공격으로 참격 피해를 주고 열상을 유발한다.";
        skill.icon = baseSkill != null ? baseSkill.icon : skill.icon;
        skill.composer = baseSkill != null ? baseSkill.composer : skill.composer;
        skill.followUpComposers = new List<Jorjouto.AnimComposerSystem.ScriptableObject_AnimComposer>();
        skill.impactSoundCategory = SkillImpactSoundCategory.Auto;
        skill.activationSpeed = 1.1f;
        skill.staminaCost = 1;
        skill.mentalCost = 0;
        skill.type = SkillType.Physical;
        skill.style = SkillStyle.Dexterity;
        skill.discipline = SkillDiscipline.WeaponArt;
        skill.firstRewardTier = 1;
        skill.damageComponents = new List<SkillDamageComponentData>
        {
            new SkillDamageComponentData
            {
                damageType = SkillType.Physical,
                attribute = SkillAttribute.Slash,
                damageMultiplier = 0.9f,
                hitCount = 1
            }
        };
        skill.equipmentRequirement = new SkillEquipmentRequirementData();
        skill.isCounterSkill = false;
        skill.isStatusEffectSkill = true;
        skill.isRangedSkill = false;
        skill.monsterOnly = false;
        skill.counterActionType = CounterActionType.None;
        skill.successArmorAttackMultiplier = 1f;
        skill.minEvadeReductionRate = 0.1f;
        skill.attackEffects = new List<AttackEffectType>();
        skill.statusAccuracyBonus = 0;
        skill.statusEffects = new List<StatusEffectApplyData>
        {
            new StatusEffectApplyData
            {
                statusEffectId = 1011,
                baseChance = 50,
                stackAmount = 1
            }
        };
        skill.statusConsumeEffects = new List<StatusConsumeEffectData>();
        skill.isConditionalSkill = false;
        skill.requiredStats = new List<StatRequirementData>();
        skill.requiredTraitIds = new List<int>();
        skill.isEvolvableSkill = false;
        skill.evolvedSkillUid = 0;
        skill.evolRequiredUseCount = 0;
        skill.evolRequiredKillCount = 0;
        skill.evolRequiredDamageCount = 0;
        skill.evolRequiredTraitIds = new List<int>();

        EditorUtility.SetDirty(skill);
        return skill;
    }

    private static void RegisterSkill(SkillDefinitionSO skill)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameManagerPrefabPath);

        try
        {
            GameDataRegistry registry = root.GetComponentInChildren<GameDataRegistry>(true);
            if (registry == null)
                throw new System.InvalidOperationException("GameManager 프리팹에서 GameDataRegistry를 찾을 수 없습니다.");

            registry.skills.RemoveAll(candidate => candidate != null && candidate.uid == skill.uid);
            registry.skills.Add(skill);
            EditorUtility.SetDirty(registry);
            PrefabUtility.SaveAsPrefabAsset(root, GameManagerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UpdateUiManagerPrefab(
        Texture2D helpIcon,
        Texture2D concealEnabledIcon,
        Texture2D concealDisabledIcon)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(UiManagerPrefabPath);

        try
        {
            UIManager uiManager = root.GetComponentInChildren<UIManager>(true);
            TooltipManager tooltipManager = root.GetComponentInChildren<TooltipManager>(true);
            Transform staminaTransform = FindDeepChild(root.transform, "Stamina");
            Transform mentalityTransform = FindDeepChild(root.transform, "Men");
            Transform staminaLineTransform = FindDeepChild(root.transform, "HPline");
            Transform mentalityLineTransform = FindDeepChild(root.transform, "MPline");
            Transform skillBar = FindDeepChild(root.transform, "SkillBar2");

            if (uiManager == null || tooltipManager == null || staminaTransform == null ||
                mentalityTransform == null || staminaLineTransform == null ||
                mentalityLineTransform == null || skillBar == null)
            {
                throw new System.InvalidOperationException("UIManager_Canvas 프리팹의 전투 UI 계층을 찾을 수 없습니다.");
            }

            RemoveAccidentalInfoPanelGauge(staminaTransform.gameObject);
            RemoveAccidentalInfoPanelGauge(mentalityTransform.gameObject);

            Image staminaLine = staminaLineTransform.GetComponent<Image>();
            Image mentalityLine = mentalityLineTransform.GetComponent<Image>();
            if (staminaLine == null || mentalityLine == null)
                throw new System.InvalidOperationException("HPline 또는 MPline의 Image를 찾을 수 없습니다.");

            uiManager.characterStaminaImage = staminaLine;
            uiManager.characterMentalityImage = mentalityLine;

            Transform concealFrame = skillBar.Find("SkillFrameBg13");
            if (concealFrame == null)
            {
                Transform template = skillBar.Find("SkillFrameBg12");
                if (template == null)
                    throw new System.InvalidOperationException("SkillFrameBg12를 찾을 수 없습니다.");

                GameObject clone = Object.Instantiate(template.gameObject, skillBar, false);
                clone.name = "SkillFrameBg13";
                concealFrame = clone.transform;

                if (concealFrame is RectTransform concealRect && template is RectTransform templateRect)
                    concealRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(50f, 0f);
            }

            Transform skillImageTransform = FindDeepChild(concealFrame, "SkillImage");
            if (skillImageTransform == null)
                throw new System.InvalidOperationException("SkillFrameBg13의 SkillImage를 찾을 수 없습니다.");

            RawImage skillImage = skillImageTransform.GetComponent<RawImage>();
            Button concealButton = skillImageTransform.GetComponent<Button>();
            SkillButton skillButton = skillImageTransform.GetComponent<SkillButton>();
            if (skillImage == null || concealButton == null || skillButton == null)
                throw new System.InvalidOperationException("은닉 버튼에 필요한 UI 컴포넌트가 없습니다.");

            Transform binds = concealFrame.Find("Binds");
            if (binds != null)
                binds.gameObject.SetActive(false);

            skillImage.texture = concealDisabledIcon;
            skillImage.enabled = true;
            concealButton.targetGraphic = skillImage;
            concealButton.interactable = true;
            concealButton.onClick = new Button.ButtonClickedEvent();
            skillButton.skill = null;
            skillButton.queueData = null;
            skillButton.queueIndex = -1;
            skillButton.isCounterSkill = false;
            skillButton.isConcealToggle = true;

            uiManager.skillConcealButton = concealButton;
            uiManager.skillConcealButtonImage = skillImage;
            uiManager.concealEnabledIcon = concealEnabledIcon;
            uiManager.concealDisabledIcon = concealDisabledIcon;
            tooltipManager.concealedSkillIcon = helpIcon;

            if (tooltipManager.costText != null)
            {
                RectTransform costRect = tooltipManager.costText.rectTransform;
                costRect.anchoredPosition = new Vector2(-110f, costRect.anchoredPosition.y);
                costRect.sizeDelta = new Vector2(200f, costRect.sizeDelta.y);
                tooltipManager.costText.resizeTextForBestFit = true;
                tooltipManager.costText.resizeTextMinSize = 11;
                tooltipManager.costText.resizeTextMaxSize = 15;
            }

            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(tooltipManager);
            EditorUtility.SetDirty(skillButton);
            PrefabUtility.SaveAsPrefabAsset(root, UiManagerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RemoveAccidentalInfoPanelGauge(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image != null)
            Object.DestroyImmediate(image, true);

        CanvasRenderer renderer = target.GetComponent<CanvasRenderer>();
        if (renderer != null)
            Object.DestroyImmediate(renderer, true);
    }

    private static void UpdateCharacterStatusPrefab(Texture2D helpIcon, Texture2D concealBadgeIcon)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CharacterStatusPrefabPath);

        try
        {
            CharacterUIHandler handler = root.GetComponentInChildren<CharacterUIHandler>(true);
            if (handler == null)
                throw new System.InvalidOperationException("CharacterStatusCanvas 프리팹에서 CharacterUIHandler를 찾을 수 없습니다.");

            handler.concealedSkillIcon = helpIcon;
            handler.concealedSkillBadgeIcon = concealBadgeIcon;
            EditorUtility.SetDirty(handler);
            PrefabUtility.SaveAsPrefabAsset(root, CharacterStatusPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UpdateQueuePrefab(string path, Texture2D concealBadgeIcon)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            SkillButton[] buttons = root.GetComponentsInChildren<SkillButton>(true);
            if (buttons.Length == 0)
                throw new System.InvalidOperationException($"{path}에서 SkillButton을 찾을 수 없습니다.");

            foreach (SkillButton button in buttons)
            {
                Transform existing = button.transform.Find("ConcealBadge");
                RawImage badge;

                if (existing == null)
                {
                    GameObject badgeObject = new GameObject(
                        "ConcealBadge",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(RawImage));
                    badgeObject.transform.SetParent(button.transform, false);
                    existing = badgeObject.transform;
                }

                RectTransform badgeRect = (RectTransform)existing;
                badgeRect.anchorMin = new Vector2(0.8f, 0f);
                badgeRect.anchorMax = new Vector2(1f, 0.2f);
                badgeRect.pivot = new Vector2(1f, 0f);
                badgeRect.offsetMin = Vector2.zero;
                badgeRect.offsetMax = Vector2.zero;
                badge = existing.GetComponent<RawImage>();
                badge.texture = concealBadgeIcon;
                badge.color = Color.white;
                badge.raycastTarget = false;
                badge.maskable = true;
                existing.SetAsLastSibling();
                existing.gameObject.SetActive(false);
                button.concealBadge = badge;
                EditorUtility.SetDirty(button);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root.name == name)
            return root;

        foreach (Transform child in root)
        {
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }

        return null;
    }
}
#endif
