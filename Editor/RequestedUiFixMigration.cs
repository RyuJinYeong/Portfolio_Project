#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RequestedUiFixMigration
{
    private const string SessionKey = "LL.RequestedUiFixMigration.FriendlyBattleResult.20260914";
    private const string UiManagerPrefabPath = "Assets/Prefab/UIManager_Canvas.prefab";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string ItemTooltipPrefabPath = "Assets/Prefab/InventoryUI/ItemTooltip.prefab";
    private const string FriendlyWagerPrefabPath = "Assets/Prefab/InventoryUI/FriendlyWagerWindow.prefab";

    static RequestedUiFixMigration()
    {
        EditorApplication.delayCall += Run;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        try
        {
            UpdateEquipmentNavigation();
            UpdateFriendlyWagerPrefab();
            UpdateFriendlyRoomPanel();
            RevertLoadedSceneAlertButtonOverrides();
            UpdateItemTooltip();
            AssetDatabase.SaveAssets();
            SessionState.SetBool(SessionKey, true);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Debug.Log("[RequestedUiFixMigration] Completed");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void UpdateFriendlyRoomPanel()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(UiManagerPrefabPath);

        try
        {
            foreach (CharacterEquipmentWindowUI window in root.GetComponentsInChildren<CharacterEquipmentWindowUI>(true))
                ConfigureEquipmentNavigation(window);

            MultiplayerRoomPanel roomPanel = root.GetComponentInChildren<MultiplayerRoomPanel>(true);

            if (roomPanel == null || roomPanel.hostButton == null ||
                roomPanel.joinButton == null || roomPanel.closeButton == null)
            {
                throw new System.InvalidOperationException("친선전 방 패널의 버튼 참조를 찾을 수 없습니다.");
            }

            roomPanel.hostButton.gameObject.SetActive(true);
            SetButtonLayout(roomPanel.hostButton, -170f);
            SetButtonLayout(roomPanel.joinButton, 0f);
            SetButtonLayout(roomPanel.closeButton, 170f);
            SetButtonText(roomPanel.hostButton, "친선전 방 만들기");
            SetButtonText(roomPanel.joinButton, "친선전 코드 참가");
            SetButtonText(roomPanel.closeButton, "닫기");

            FriendlyMatchLobbyUI friendlyLobby = root.GetComponentInChildren<FriendlyMatchLobbyUI>(true);

            if (friendlyLobby == null || friendlyLobby.wagerPanel == null ||
                friendlyLobby.openButton == null || friendlyLobby.controls == null)
                throw new System.InvalidOperationException("친선전 내기 창의 참조를 찾을 수 없습니다.");

            ConfigureFriendlyWagerWindow(friendlyLobby.wagerPanel);
            friendlyLobby.wagerPanel.transform.SetAsLastSibling();
            friendlyLobby.openButton.transform.parent.SetAsLastSibling();

            Canvas misplacedCanvas = friendlyLobby.controls.GetComponent<Canvas>();
            bool removeMisplacedLayer = misplacedCanvas != null && misplacedCanvas.sortingOrder == 40;
            GraphicRaycaster misplacedRaycaster = friendlyLobby.controls.GetComponent<GraphicRaycaster>();
            if (removeMisplacedLayer && misplacedRaycaster != null)
                Object.DestroyImmediate(misplacedRaycaster);
            if (removeMisplacedLayer)
                Object.DestroyImmediate(misplacedCanvas);

            TooltipManager tooltipManager = root.GetComponentInChildren<TooltipManager>(true);

            if (tooltipManager != null)
            {
                ConfigureTopmostTooltip(tooltipManager.tooltipObject);

                if (tooltipManager.simpleTooltipObject != tooltipManager.tooltipObject)
                    ConfigureTopmostTooltip(tooltipManager.simpleTooltipObject);
            }

            ClearPersistentClickEvents(friendlyLobby.openButton);
            UnityEventTools.AddPersistentListener(friendlyLobby.openButton.onClick, friendlyLobby.Open);
            EditorUtility.SetDirty(friendlyLobby.openButton.transform.parent);
            EditorUtility.SetDirty(friendlyLobby.openButton);
            ConfigureAlertDialogs(root);
            ConfigureFriendlyResult(root, friendlyLobby);

            EditorUtility.SetDirty(roomPanel);
            EditorUtility.SetDirty(friendlyLobby);
            PrefabUtility.SaveAsPrefabAsset(root, UiManagerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UpdateEquipmentNavigation()
    {
        const string path = "Assets/Prefab/InventoryUI/CharacterEquipmentWindow.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ConfigureEquipmentNavigation(root.GetComponent<CharacterEquipmentWindowUI>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureEquipmentNavigation(CharacterEquipmentWindowUI window)
    {
            Transform title = window.legacyCharacterNameText.transform.parent;
            Transform left = title.GetChild(0);
            Transform right = title.GetChild(title.childCount - 1);
            if (left.name != "Arrow" || right.name != "Arrow" || left == right)
                throw new System.InvalidOperationException("장비창 이름 양쪽의 Arrow를 찾을 수 없습니다.");

            window.previousCharacterButton = left.GetComponent<Button>() ?? left.gameObject.AddComponent<Button>();
            window.nextCharacterButton = right.GetComponent<Button>() ?? right.gameObject.AddComponent<Button>();
            window.previousCharacterButton.targetGraphic = left.GetComponent<Image>();
            window.nextCharacterButton.targetGraphic = right.GetComponent<Image>();
            window.previousCharacterButton.targetGraphic.raycastTarget = true;
            window.nextCharacterButton.targetGraphic.raycastTarget = true;
            ClearPersistentClickEvents(window.previousCharacterButton);
            ClearPersistentClickEvents(window.nextCharacterButton);
            UnityEventTools.AddPersistentListener(window.previousCharacterButton.onClick, window.PreviousCharacter);
            UnityEventTools.AddPersistentListener(window.nextCharacterButton.onClick, window.NextCharacter);
    }

    private static void UpdateFriendlyWagerPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(FriendlyWagerPrefabPath);

        try
        {
            ConfigureFriendlyWagerWindow(root);
            PrefabUtility.SaveAsPrefabAsset(root, FriendlyWagerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureFriendlyWagerWindow(GameObject window)
    {
        SetLayerRecursively(window.transform, LayerMask.NameToLayer("UI"));

        Canvas canvas = window.GetComponent<Canvas>();
        if (canvas == null)
            canvas = window.AddComponent<Canvas>();

        SerializedObject serializedCanvas = new SerializedObject(canvas);
        serializedCanvas.FindProperty("m_OverrideSorting").boolValue = true;
        serializedCanvas.FindProperty("m_SortingOrder").intValue = 50;
        serializedCanvas.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(canvas);

        if (window.GetComponent<GraphicRaycaster>() == null)
            window.AddComponent<GraphicRaycaster>();
    }

    private static void ConfigureFriendlyResult(GameObject root, FriendlyMatchLobbyUI lobby)
    {
        const string path = "Assets/Prefab/InventoryUI/FriendlyBattleResult.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Transform source = FindDeepChild(root.transform, "Alert").Find("QuestAbandon");
            GameObject template = Object.Instantiate(source.gameObject);
            try
            {
                template.name = "FriendlyBattleResult";
                Transform panel = template.transform.Find("ConfirmPanel");
                TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
                TMP_Text title = texts[0];
                title.gameObject.name = "VictoryTitle";
                title.text = "친선전 승리";
                TMP_Text defeat = Object.Instantiate(title, title.transform.parent);
                defeat.gameObject.name = "DefeatTitle";
                defeat.text = "친선전 패배";
                defeat.gameObject.SetActive(false);
                texts[1].gameObject.name = "ResultStatus";
                texts[1].text = "전원 동의하면 친선전 편성 로비로 돌아갑니다.";
                foreach (Button button in panel.GetComponentsInChildren<Button>(true))
                    ClearPersistentClickEvents(button);
                Button rematch = panel.Find("ButtonYes").GetComponent<Button>();
                Button town = panel.Find("ButtonNo").GetComponent<Button>();
                SetButtonText(rematch, "경기 재개");
                SetButtonText(town, "마을로 돌아가기");
                foreach (Button button in new[] { rematch, town })
                {
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 180f);
                    Vector2 pos = rect.anchoredPosition;
                    pos.x = button == rematch ? -100f : 100f;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, rect.anchorMin.y);
                    rect.anchoredPosition = pos;
                    button.interactable = true;
                }
                Canvas canvas = template.GetComponent<Canvas>();
                if (canvas == null) canvas = template.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
                if (template.GetComponent<GraphicRaycaster>() == null) template.AddComponent<GraphicRaycaster>();
                template.SetActive(false);
                prefab = PrefabUtility.SaveAsPrefabAsset(template, path);
            }
            finally { Object.DestroyImmediate(template); }
        }
        if (lobby.resultPanel == null)
            lobby.resultPanel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
        Transform content = lobby.resultPanel.transform.Find("ConfirmPanel");
        lobby.victoryTitle = content.Find("VictoryTitle").gameObject;
        lobby.defeatTitle = content.Find("DefeatTitle").gameObject;
        lobby.resultStatus = content.Find("ResultStatus").GetComponent<TMP_Text>();
        lobby.rematchButton = content.Find("ButtonYes").GetComponent<Button>();
        lobby.townButton = content.Find("ButtonNo").GetComponent<Button>();
        ClearPersistentClickEvents(lobby.rematchButton);
        ClearPersistentClickEvents(lobby.townButton);
        UnityEventTools.AddPersistentListener(lobby.rematchButton.onClick, lobby.Rematch);
        UnityEventTools.AddPersistentListener(lobby.townButton.onClick, lobby.ReturnToTown);
        PrefabUtility.RecordPrefabInstancePropertyModifications(lobby.rematchButton);
        PrefabUtility.RecordPrefabInstancePropertyModifications(lobby.townButton);
        EditorUtility.SetDirty(lobby);
    }

    private static void ConfigureAlertDialogs(GameObject root)
    {
        Transform alert = FindDeepChild(root.transform, "Alert");
        Transform battleDefeat = alert != null ? alert.Find("battleDefeatDialog") : null;
        Transform questAbandon = alert != null ? alert.Find("QuestAbandon") : null;

        if (alert == null || battleDefeat == null || questAbandon == null)
            throw new System.InvalidOperationException("Alert 하위의 포기 또는 전멸 패널을 찾을 수 없습니다.");

        RemoveAnimator(battleDefeat.gameObject);
        RemoveAnimator(questAbandon.gameObject);

        UIManager uiManager = root.GetComponent<UIManager>();
        Transform confirmPanel = questAbandon.Find("ConfirmPanel");
        Button confirmButton = confirmPanel != null
            ? confirmPanel.Find("ButtonYes")?.GetComponent<Button>()
            : null;
        Button cancelButton = confirmPanel != null
            ? confirmPanel.Find("ButtonNo")?.GetComponent<Button>()
            : null;
        Transform defeatConfirmPanel = battleDefeat.Find("ConfirmPanel");
        Button defeatContinueButton = defeatConfirmPanel != null
            ? defeatConfirmPanel.Find("ButtonYes")?.GetComponent<Button>()
            : null;

        if (uiManager == null || confirmButton == null || cancelButton == null ||
            defeatContinueButton == null)
            throw new System.InvalidOperationException("Alert 패널의 버튼 또는 UIManager를 찾을 수 없습니다.");

        ClearPersistentClickEvents(confirmButton);
        ClearPersistentClickEvents(cancelButton);
        ClearPersistentClickEvents(defeatContinueButton);
        UnityEventTools.AddPersistentListener(confirmButton.onClick, uiManager.ConfirmQuestAbandon);
        UnityEventTools.AddPersistentListener(cancelButton.onClick, uiManager.CancelQuestAbandon);
        UnityEventTools.AddPersistentListener(defeatContinueButton.onClick, uiManager.ContinueAfterBattleDefeat);
        EditorUtility.SetDirty(confirmButton);
        EditorUtility.SetDirty(cancelButton);
        EditorUtility.SetDirty(defeatContinueButton);

        Canvas canvas = alert.GetComponent<Canvas>();
        if (canvas == null)
            canvas = alert.gameObject.AddComponent<Canvas>();

        canvas.sortingOrder = 90;

        if (alert.GetComponent<GraphicRaycaster>() == null)
            alert.gameObject.AddComponent<GraphicRaycaster>();

        alert.SetAsLastSibling();
    }

    private static void RevertLoadedSceneAlertButtonOverrides()
    {
        Scene scene = SceneManager.GetSceneByPath(GameScenePath);

        if (!scene.IsValid() || !scene.isLoaded)
            return;

        bool wasDirty = scene.isDirty;
        UIManager uiManager = null;

        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot.name != "TownCameras")
                continue;

            Transform oldCamera = sceneRoot.transform.Find("FriendlyBattleCamera");
            Transform oldArena = sceneRoot.transform.Find("TownBattleCameraPos/FriendlyBattleArena");
            if (oldCamera != null)
                Undo.DestroyObjectImmediate(oldCamera.gameObject);
            if (oldArena != null)
                Undo.DestroyObjectImmediate(oldArena.gameObject);
        }

        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            uiManager = sceneRoot.GetComponentInChildren<UIManager>(true);

            if (uiManager != null)
                break;
        }

        Transform alert = uiManager != null ? FindDeepChild(uiManager.transform, "Alert") : null;
        Transform questAbandon = alert != null ? alert.Find("QuestAbandon/ConfirmPanel") : null;
        Transform battleDefeat = alert != null ? alert.Find("battleDefeatDialog/ConfirmPanel") : null;
        Transform wagerButton = uiManager != null
            ? FindDeepChild(uiManager.transform, "WagerButton")
            : null;

        if (questAbandon == null || battleDefeat == null || wagerButton == null)
            throw new System.InvalidOperationException("GameScene의 Alert 또는 내기 버튼을 찾을 수 없습니다.");

        RevertOnClickOverride(questAbandon.Find("ButtonYes")?.GetComponent<Button>());
        RevertOnClickOverride(questAbandon.Find("ButtonNo")?.GetComponent<Button>());
        RevertOnClickOverride(battleDefeat.Find("ButtonYes")?.GetComponent<Button>());
        RevertOnClickOverride(wagerButton.GetComponent<Button>());

        if (!wasDirty)
            EditorSceneManager.SaveScene(scene);
        else
            Debug.LogWarning("[RequestedUiFixMigration] GameScene에 저장되지 않은 변경이 있어 Alert 버튼 수정 후 자동 저장하지 않았습니다.");
    }

    private static void RevertOnClickOverride(Button button)
    {
        if (button == null)
            throw new System.InvalidOperationException("Alert 버튼을 찾을 수 없습니다.");

        SerializedObject serializedButton = new SerializedObject(button);
        SerializedProperty onClick = serializedButton.FindProperty("m_OnClick");

        if (PrefabUtility.IsPartOfPrefabInstance(button))
            PrefabUtility.RevertPropertyOverride(onClick, InteractionMode.AutomatedAction);
    }

    private static void ClearPersistentClickEvents(Button button)
    {
        SerializedObject serializedButton = new SerializedObject(button);
        SerializedProperty calls = serializedButton.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        calls.ClearArray();
        serializedButton.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveAnimator(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();

        if (animator != null)
            Object.DestroyImmediate(animator, true);
    }

    private static void ConfigureTopmostTooltip(GameObject tooltip)
    {
        if (tooltip == null)
            return;

        Canvas canvas = tooltip.GetComponent<Canvas>();

        if (canvas == null)
            canvas = tooltip.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;
    }

    private static void UpdateItemTooltip()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ItemTooltipPrefabPath);

        try
        {
            Canvas tooltipCanvas = root.GetComponent<Canvas>();

            if (tooltipCanvas == null)
                tooltipCanvas = root.AddComponent<Canvas>();

            tooltipCanvas.overrideSorting = true;
            tooltipCanvas.sortingOrder = short.MaxValue;

            Transform stats = FindDeepChild(root.transform, "Stats");
            Transform source = FindDeepChild(root.transform, "Description_text");
            Transform statTemplate = stats != null ? stats.Find("Stat1") : null;

            if (stats == null || source == null || source.GetComponent<Text>() == null ||
                statTemplate == null || statTemplate is not RectTransform templateRect)
                throw new System.InvalidOperationException("아이템 툴팁의 설명 또는 하단 구획을 찾을 수 없습니다.");

            Transform existing = stats.Find("EssenceDetails");

            if (existing == null)
            {
                GameObject detailsObject = new GameObject(
                    "EssenceDetails",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                detailsObject.transform.SetParent(stats, false);
                existing = detailsObject.transform;

                Text sourceText = source.GetComponent<Text>();
                Text detailsText = detailsObject.GetComponent<Text>();
                detailsText.font = sourceText.font;
                detailsText.material = sourceText.material;
                detailsText.color = sourceText.color;
                detailsText.fontStyle = sourceText.fontStyle;
                detailsText.fontSize = 14;
                detailsText.lineSpacing = sourceText.lineSpacing;
                detailsText.alignment = TextAnchor.UpperLeft;
                detailsText.supportRichText = true;
                detailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
                detailsText.verticalOverflow = VerticalWrapMode.Overflow;
                detailsText.resizeTextForBestFit = false;
                detailsText.raycastTarget = false;
                detailsObject.SetActive(false);
            }

            existing.gameObject.layer = stats.gameObject.layer;
            RectTransform detailsRect = existing as RectTransform;
            detailsRect.anchorMin = templateRect.anchorMin;
            detailsRect.anchorMax = templateRect.anchorMax;
            detailsRect.pivot = templateRect.pivot;
            detailsRect.anchoredPosition = new Vector2(templateRect.anchoredPosition.x, 0f);
            detailsRect.sizeDelta = new Vector2(templateRect.sizeDelta.x, 285f);

            PrefabUtility.SaveAsPrefabAsset(root, ItemTooltipPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetButtonLayout(Button button, float x)
    {
        RectTransform rect = button.transform as RectTransform;

        if (rect == null)
            return;

        rect.anchoredPosition = new Vector2(x, -112f);
        rect.sizeDelta = new Vector2(150f, 46f);
    }

    private static void SetButtonText(Button button, string value)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
            text.text = value;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);

            if (found != null)
                return found;
        }

        return null;
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }
}
#endif
