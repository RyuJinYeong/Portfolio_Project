using System.Collections.Generic;
using SoftKitty.InventoryEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using static SoftKitty.InventoryEngine.InventoryHolder;

public class CharacterTargeting : MonoBehaviour
{
    public LineRenderer lineRenderer;
    [Range(0f, 1f)] public float dottedLineAlpha = 0.45f;
    [Min(0.1f)] public float dottedPatternLength = 1.2f;

    private Camera mainCamera;
    private CharacterManager selectedCharacter;
    private CharacterManager selectedTarget;

    public SkillDefinitionSO selectedSkill;

    private bool isTargeting = false;
    public bool isDefenseSkillTargeting = false;
    private bool isDefenseCharacterTargeting = false;

    public int curveResolution = 50;

    public Color hoverOutlineColor = Color.red;
    public Color selectedOutlineColor = Color.white;

    private Outline currentHoverOutline;
    private Outline selectedCharacterOutline;

    private readonly List<LineRenderer> confirmedLineRenderers = new();
    private LineRenderer previewLineRenderer;
    private Material dottedLineMaterial;
    private Texture2D dottedLineTexture;
    private Gradient solidLineGradient;
    private Material[] solidLineMaterials;
    private LineTextureMode solidLineTextureMode;
    private Vector2 solidLineTextureScale;

    void Start()
    {
        mainCamera = Camera.main;
        InitializeLineRenderers();
    }

    void Update()
    {
        if (mainCamera == null || !mainCamera.isActiveAndEnabled)
        {
            GameObject mainCameraObject = GameObject.Find("MainCamera");
            mainCamera = mainCameraObject != null
                ? mainCameraObject.GetComponent<Camera>()
                : Camera.main;
        }

        if (mainCamera == null)
            return;

        HandleHoverOutline();

        if (!isTargeting &&
            !isDefenseCharacterTargeting &&
            Input.GetMouseButtonDown(0) &&
            !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CharacterManager characterManager = hit.transform.GetComponentInParent<CharacterManager>();

                if (characterManager != null)
                {
                    SelectCharacter(characterManager);
                }
            }
        }

        if (isTargeting)
        {
            if (isDefenseSkillTargeting)
                HandleDefenseSkillTargeting();
            else
                HandleSkillTargeting();
        }

        if (isDefenseCharacterTargeting)
        {
            HandleDefenseTargeting();
        }
    }

    private void HandleSkillTargeting()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CharacterManager targetCharacter = hit.transform.GetComponentInParent<CharacterManager>();

                if (targetCharacter != null)
                {
                    SelectTarget(targetCharacter);
                }
            }
        }
        else
        {
            UpdateBezierCurve();
        }
    }

    private void HandleDefenseSkillTargeting()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CharacterManager targetCharacter = hit.transform.GetComponentInParent<CharacterManager>();

                if (targetCharacter != null)
                {
                    SelectTarget(targetCharacter);
                }
            }
        }
        else
        {
            UpdateBezierCurve();
        }
    }

    private void HandleDefenseTargeting()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CharacterManager targetCharacter = hit.transform.GetComponentInParent<CharacterManager>();

                if (targetCharacter != null)
                {
                    SelectDefenseTarget(targetCharacter);
                }
            }
        }
        else
        {
            UpdateBezierCurve();
        }
    }

    public void OpenPlayerInventory()
    {
        //CharacterInventory.OpenWindow();

        Debug.Log("원정대 창고 열람");
    }

    public void OpenPlayerEquipment()
    {
        /*
        if (selectedCharacter != null && selectedCharacter.character.IsMine)
            selectedCharacter.character.CharacterEquipment.OpenWindow();
        */
    }

    public void OpenSkills()
    {
        if (selectedCharacter != null && selectedCharacter.character.IsMine)
            ItemManager.PlayerInventoryHolder.OpenWindowByName("Skills", "Skills");
    }

    private void HandleHoverOutline()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            CharacterManager characterManager = hit.transform.GetComponentInParent<CharacterManager>();

            if (characterManager != null)
            {
                if (selectedCharacter == characterManager)
                {
                    if (currentHoverOutline != null)
                    {
                        currentHoverOutline.enabled = false;
                        currentHoverOutline = null;
                    }

                    return;
                }

                Outline hoveredOutline = characterManager.GetComponentInChildren<Outline>(true);

                if (currentHoverOutline != null &&
                    currentHoverOutline != hoveredOutline)
                {
                    currentHoverOutline.enabled = false;
                }

                currentHoverOutline = hoveredOutline;

                if (currentHoverOutline != null)
                {
                    currentHoverOutline.OutlineColor = hoverOutlineColor;
                    currentHoverOutline.enabled = true;
                }
            }
            else
            {
                if (currentHoverOutline != null)
                {
                    currentHoverOutline.enabled = false;
                    currentHoverOutline = null;
                }
            }
        }
        else if (currentHoverOutline != null)
        {
            currentHoverOutline.enabled = false;
            currentHoverOutline = null;
        }
    }

    private void ChangeCursor(string cursorType)
    {
        Texture2D cursorTexture = Resources.Load<Texture2D>($"Cursor/Cursor_{cursorType}");

        if (cursorTexture == null)
            return;

        Vector2 cursorHotspot = new Vector2(cursorTexture.width * 0.3f, 0);
        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
    }

    public void SelectCharacter(CharacterManager characterManager)
    {
        if (isTargeting || isDefenseCharacterTargeting)
        {
            ChangeCursor("Basic");
            return;
        }

        if (selectedCharacterOutline != null)
        {
            selectedCharacterOutline.enabled = false;
        }

        selectedCharacter = characterManager;
        UIManager.Instance.DisplayCharacterInfo(characterManager);

        if (selectedCharacter.character.IsMine)
        {
            /*
            ItemManager.PlayerEquipmentHolder = selectedCharacter.character.CharacterEquipment;
            HoverInformation.SetCompareHolder(selectedCharacter.character.CharacterEquipment);
            */
        }

        selectedCharacterOutline = selectedCharacter.GetComponentInChildren<Outline>(true);

        if (selectedCharacterOutline != null)
        {
            selectedCharacterOutline.OutlineColor = selectedOutlineColor;
            selectedCharacterOutline.enabled = true;
        }

        RefreshConfirmedTargetLines();

        if (currentHoverOutline == selectedCharacterOutline)
        {
            currentHoverOutline = null;
        }
    }

    public void StartTargeting(SkillDefinitionSO skill)
    {
        if (selectedCharacter == null || skill == null)
            return;

        bool isDefenseTurn = TurnManager.Instance != null &&
                             TurnManager.Instance.defenseCharacter == selectedCharacter;

        if (!selectedCharacter.isPlayerTurn && !(skill.isCounterSkill && isDefenseTurn))
            return;

        selectedSkill = skill;
        RefreshConfirmedTargetLines();
        SetLineRendererVisible(previewLineRenderer, true);

        if (selectedSkill.isCounterSkill)
        {
            ChangeCursor("Deff");
            isTargeting = true;
            isDefenseSkillTargeting = true;
        }
        else if (selectedSkill.isRangedSkill)
        {
            ChangeCursor("Shoot");
            isTargeting = true;
            isDefenseSkillTargeting = false;
        }
        else
        {
            ChangeCursor("Attack");
            isTargeting = true;
            isDefenseSkillTargeting = false;
        }
    }

    void UpdateBezierCurve()
    {
        if (selectedCharacter == null || previewLineRenderer == null || mainCamera == null)
            return;

        Vector3 startPosition = selectedCharacter.transform.position + Vector3.up * 1;

        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(
            new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                mainCamera.transform.position.y - selectedCharacter.transform.position.y));

        Vector3 controlPoint = (startPosition + mousePosition) / 2;
        controlPoint.y += 2.0f;

        DrawBezierCurve(previewLineRenderer, startPosition, controlPoint, mousePosition, true);
    }

    public void SelectTarget(CharacterManager target)
    {
        if (selectedCharacter == null || selectedSkill == null || target == null)
            return;

        selectedTarget = target;
        isTargeting = false;
        SetLineRendererVisible(previewLineRenderer, false);

        if (selectedSkill.isCounterSkill)
            selectedCharacter.SelectCounterSkill(selectedSkill, target);
        else
            selectedCharacter.SelectSkill(selectedSkill, target);

        RefreshConfirmedTargetLines();

        ChangeCursor("Basic");
    }

    public void StartDefenseCharacterTargeting(CharacterManager defenseCharacter)
    {
        if (defenseCharacter == null)
            return;

        InitializeLineRenderers();
        ClearConfirmedTargetLines();
        isDefenseCharacterTargeting = true;
        SetLineRendererVisible(previewLineRenderer, true);

        selectedCharacter = defenseCharacter;
        ChangeCursor("Deff");

        Debug.Log($"{defenseCharacter.character.Name}의 방어 대상 타겟팅을 시작합니다.");

        UpdateBezierCurve();
    }

    private void SelectDefenseTarget(CharacterManager target)
    {
        if (target == null || selectedCharacter == null)
            return;

        if (target.character.IsMine == selectedCharacter.character.IsMine)
        {
            selectedTarget = target;
            isDefenseCharacterTargeting = false;

            UpdateBezierCurve();

            Debug.Log($"{selectedCharacter.character.Name}가 {target.character.Name}을 방어합니다.");

            selectedCharacter.combatHandler.SetDefenseTarget(target);
            StopTargeting();

            UIManager.Instance.UpdateHotbarSkills(selectedCharacter);
        }
    }

    public void StopTargeting()
    {
        isTargeting = false;
        isDefenseSkillTargeting = false;
        isDefenseCharacterTargeting = false;

        SetLineRendererVisible(previewLineRenderer, false);
        ClearConfirmedTargetLines();

        ChangeCursor("Basic");
    }

    public void RefreshConfirmedTargetLines()
    {
        InitializeLineRenderers();

        Dictionary<CharacterManager, bool> targetStyles = new();

        if (selectedCharacter != null && selectedCharacter.combatHandler != null)
        {
            AddQueuedTargets(selectedCharacter.combatHandler.GetSkillQueue(), targetStyles);
            AddQueuedTargets(selectedCharacter.combatHandler.GetCounterSkillQueue(), targetStyles);
        }

        int lineIndex = 0;

        foreach (KeyValuePair<CharacterManager, bool> targetStyle in targetStyles)
        {
            if (targetStyle.Key == null)
                continue;

            LineRenderer targetLine = GetConfirmedLineRenderer(lineIndex++);
            Vector3 startPosition = selectedCharacter.transform.position + Vector3.up;
            Vector3 targetPosition = targetStyle.Key.transform.position + Vector3.up;
            Vector3 controlPoint = (startPosition + targetPosition) / 2f;
            controlPoint.y += 2f;

            SetLineRendererVisible(targetLine, true);
            DrawBezierCurve(
                targetLine,
                startPosition,
                controlPoint,
                targetPosition,
                !targetStyle.Value);
        }

        for (int i = lineIndex; i < confirmedLineRenderers.Count; i++)
            SetLineRendererVisible(confirmedLineRenderers[i], false);
    }

    private void AddQueuedTargets(
        List<SkillQueueData> queue,
        Dictionary<CharacterManager, bool> targetStyles)
    {
        if (queue == null)
            return;

        foreach (SkillQueueData queuedSkill in queue)
        {
            if (queuedSkill == null || queuedSkill.skill == null || queuedSkill.target == null)
                continue;

            bool containsMeleeSkill = !queuedSkill.skill.isRangedSkill;

            if (targetStyles.TryGetValue(queuedSkill.target, out bool existingContainsMelee))
                targetStyles[queuedSkill.target] = existingContainsMelee || containsMeleeSkill;
            else
                targetStyles.Add(queuedSkill.target, containsMeleeSkill);
        }
    }

    private void InitializeLineRenderers()
    {
        if (lineRenderer == null || previewLineRenderer != null)
            return;

        solidLineGradient = CopyGradient(lineRenderer.colorGradient, 1f);
        solidLineMaterials = lineRenderer.sharedMaterials;
        solidLineTextureMode = lineRenderer.textureMode;
        solidLineTextureScale = lineRenderer.textureScale;
        lineRenderer.enabled = false;
        confirmedLineRenderers.Add(lineRenderer);

        dottedLineTexture = new Texture2D(16, 1, TextureFormat.RGBA32, false, true)
        {
            name = "TargetingDottedLineTexture",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = i < 9 ? Color.white : Color.clear;

        dottedLineTexture.SetPixels(pixels);
        dottedLineTexture.Apply();

        if (solidLineMaterials.Length > 0 && solidLineMaterials[0] != null)
        {
            dottedLineMaterial = new Material(solidLineMaterials[0])
            {
                name = "TargetingDottedLineMaterial",
                mainTexture = dottedLineTexture
            };
        }

        previewLineRenderer = CreateLineRenderer("TargetingPreviewLine");
        SetLineRendererVisible(previewLineRenderer, false);
    }

    private LineRenderer GetConfirmedLineRenderer(int index)
    {
        while (confirmedLineRenderers.Count <= index)
            confirmedLineRenderers.Add(CreateLineRenderer($"ConfirmedTargetLine_{confirmedLineRenderers.Count}"));

        return confirmedLineRenderers[index];
    }

    private LineRenderer CreateLineRenderer(string objectName)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.layer = lineRenderer.gameObject.layer;
        lineObject.transform.SetParent(lineRenderer.transform.parent, false);

        LineRenderer createdLine = lineObject.AddComponent<LineRenderer>();
        createdLine.sharedMaterials = solidLineMaterials;
        createdLine.widthMultiplier = lineRenderer.widthMultiplier;
        createdLine.widthCurve = lineRenderer.widthCurve;
        createdLine.colorGradient = solidLineGradient;
        createdLine.numCornerVertices = lineRenderer.numCornerVertices;
        createdLine.numCapVertices = lineRenderer.numCapVertices;
        createdLine.alignment = lineRenderer.alignment;
        createdLine.useWorldSpace = lineRenderer.useWorldSpace;
        createdLine.loop = false;
        createdLine.sortingLayerID = lineRenderer.sortingLayerID;
        createdLine.sortingOrder = lineRenderer.sortingOrder;
        createdLine.enabled = false;
        return createdLine;
    }

    private void DrawBezierCurve(
        LineRenderer targetLine,
        Vector3 startPosition,
        Vector3 controlPoint,
        Vector3 endPosition,
        bool dotted)
    {
        if (targetLine == null)
            return;

        if (dotted && dottedLineMaterial != null)
            targetLine.sharedMaterial = dottedLineMaterial;
        else
            targetLine.sharedMaterials = solidLineMaterials;

        targetLine.colorGradient = CopyGradient(
            solidLineGradient,
            dotted ? dottedLineAlpha : 1f);
        targetLine.textureMode = dotted
            ? LineTextureMode.Stretch
            : solidLineTextureMode;
        targetLine.textureScale = dotted
            ? new Vector2(
                Mathf.Max(1f, Vector3.Distance(startPosition, endPosition) / dottedPatternLength),
                1f)
            : solidLineTextureScale;
        targetLine.positionCount = curveResolution;

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            targetLine.SetPosition(
                i,
                CalculateBezierPoint(t, startPosition, controlPoint, endPosition));
        }
    }

    private void ClearConfirmedTargetLines()
    {
        foreach (LineRenderer confirmedLine in confirmedLineRenderers)
            SetLineRendererVisible(confirmedLine, false);
    }

    private static void SetLineRendererVisible(LineRenderer targetLine, bool visible)
    {
        if (targetLine == null)
            return;

        targetLine.enabled = visible;

        if (!visible)
            targetLine.positionCount = 0;
    }

    private static Gradient CopyGradient(Gradient source, float alphaMultiplier)
    {
        Gradient gradient = new Gradient();

        if (source == null)
            return gradient;

        GradientAlphaKey[] alphaKeys = source.alphaKeys;
        for (int i = 0; i < alphaKeys.Length; i++)
            alphaKeys[i].alpha *= alphaMultiplier;

        gradient.SetKeys(source.colorKeys, alphaKeys);
        return gradient;
    }

    private void OnDestroy()
    {
        if (dottedLineMaterial != null)
            Destroy(dottedLineMaterial);

        if (dottedLineTexture != null)
            Destroy(dottedLineTexture);
    }

    Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 point = uu * p0;
        point += 2 * u * t * p1;
        point += tt * p2;

        return point;
    }
}
