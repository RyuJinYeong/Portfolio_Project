using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterTargeting : MonoBehaviour
{
    public LineRenderer lineRenderer;
    private Camera mainCamera;
    private CharacterManager selectedCharacter;
    private CharacterManager selectedTarget;
    private SkillBase selectedSkill;
    private bool isTargeting = false;

    public int curveResolution = 50;  // °î¼±ÀÇ º¯°îÁ¡ ¼ö

    void Start()
    {
        mainCamera = Camera.main;
        lineRenderer.enabled = false;
    }

    void Update()
    {
        if (!isTargeting && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
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
    }

    void SelectCharacter(CharacterManager characterManager)
    {
        selectedCharacter = characterManager;
        UIManager.Instance.DisplayCharacterInfo(characterManager);
    }

    public void StartTargeting(SkillBase skill)
    {
        if (selectedCharacter == null) return;

        selectedSkill = skill;
        isTargeting = true;
        lineRenderer.enabled = true;
    }

    void UpdateBezierCurve()
    {
        if (selectedCharacter == null)
            return;

        Vector3 startPosition = selectedCharacter.transform.position;
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.transform.position.y - selectedCharacter.transform.position.y));

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

    void SelectTarget(CharacterManager target)
    {
        selectedTarget = target;
        isTargeting = false;

        Vector3 startPosition = selectedCharacter.transform.position;
        Vector3 targetPosition = target.transform.position;

        Vector3 controlPoint = (startPosition + targetPosition) / 2;
        controlPoint.y += 2.0f;

        lineRenderer.positionCount = curveResolution;

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 curvePoint = CalculateBezierPoint(t, startPosition, controlPoint, targetPosition);
            lineRenderer.SetPosition(i, curvePoint);
        }

        selectedCharacter.SelectSkill(selectedSkill, target);
    }

    public void StopTargeting()
    {
        isTargeting = false;
        lineRenderer.enabled = false;
    }

    Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 point = uu * p0; // (1-t)^2 * p0
        point += 2 * u * t * p1; // 2 * (1-t) * t * p1
        point += tt * p2;        // t^2 * p2

        return point;
    }
}
