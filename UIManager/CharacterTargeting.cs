using SoftKitty.InventoryEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using static SoftKitty.InventoryEngine.InventoryHolder;

public class CharacterTargeting : MonoBehaviour
{
    public LineRenderer lineRenderer;
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

    void Start()
    {
        mainCamera = Camera.main;
        lineRenderer.enabled = false;
    }

    void Update()
    {
        HandleHoverOutline();

        if (!isTargeting &&
            !isDefenseCharacterTargeting &&
            Input.GetMouseButtonDown(0) &&
            !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.TryGetComponent<CharacterManager>(out var characterManager))
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
                if (hit.transform.TryGetComponent<CharacterManager>(out var targetCharacter))
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
                if (hit.transform.TryGetComponent<CharacterManager>(out var targetCharacter))
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
                if (hit.transform.TryGetComponent<CharacterManager>(out var targetCharacter))
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
            if (hit.transform.TryGetComponent<CharacterManager>(out var characterManager))
            {
                if (selectedCharacter == characterManager)
                    return;

                if (currentHoverOutline != null &&
                    currentHoverOutline != characterManager.GetComponent<Outline>())
                {
                    currentHoverOutline.enabled = false;
                }

                currentHoverOutline = characterManager.GetComponent<Outline>();

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

        selectedCharacterOutline = selectedCharacter.GetComponent<Outline>();

        if (selectedCharacterOutline != null)
        {
            selectedCharacterOutline.OutlineColor = selectedOutlineColor;
            selectedCharacterOutline.enabled = true;
        }

        if (currentHoverOutline == selectedCharacterOutline)
        {
            currentHoverOutline = null;
        }
    }

    public void StartTargeting(SkillDefinitionSO skill)
    {
        if (selectedCharacter == null || skill == null)
            return;

        selectedSkill = skill;

        if (selectedSkill.isCounterSkill)
        {
            ChangeCursor("Deff");
            isTargeting = true;
            isDefenseSkillTargeting = true;
            lineRenderer.enabled = true;
        }
        else if (selectedSkill.isRangedSkill)
        {
            ChangeCursor("Shoot");
            isTargeting = true;
            isDefenseSkillTargeting = false;
            lineRenderer.enabled = true;
        }
        else
        {
            ChangeCursor("Attack");
            isTargeting = true;
            isDefenseSkillTargeting = false;
            lineRenderer.enabled = true;
        }
    }

    void UpdateBezierCurve()
    {
        if (selectedCharacter == null || lineRenderer == null || mainCamera == null)
            return;

        Vector3 startPosition = selectedCharacter.transform.position + Vector3.up * 1;

        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(
            new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                mainCamera.transform.position.y - selectedCharacter.transform.position.y));

        Vector3 controlPoint = (startPosition + mousePosition) / 2;
        controlPoint.y += 2.0f;

        lineRenderer.positionCount = curveResolution;

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 curvePoint = CalculateBezierPoint(t, startPosition, controlPoint, mousePosition);
            lineRenderer.SetPosition(i, curvePoint);
        }
    }

    public void SelectTarget(CharacterManager target)
    {
        if (selectedCharacter == null || selectedSkill == null || target == null)
            return;

        selectedTarget = target;
        isTargeting = false;

        Vector3 startPosition = selectedCharacter.transform.position + Vector3.up * 1;
        Vector3 targetPosition = target.transform.position + Vector3.up * 1;
        Vector3 controlPoint = (startPosition + targetPosition) / 2;
        controlPoint.y += 2.0f;

        lineRenderer.positionCount = curveResolution;

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 curvePoint = CalculateBezierPoint(t, startPosition, controlPoint, targetPosition);
            lineRenderer.SetPosition(i, curvePoint);
        }

        if (selectedSkill.isCounterSkill)
            selectedCharacter.SelectCounterSkill(selectedSkill, target);
        else
            selectedCharacter.SelectSkill(selectedSkill, target);

        ChangeCursor("Basic");
    }

    public void StartDefenseCharacterTargeting(CharacterManager defenseCharacter)
    {
        if (defenseCharacter == null)
            return;

        isDefenseCharacterTargeting = true;
        lineRenderer.enabled = true;

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

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        ChangeCursor("Basic");
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