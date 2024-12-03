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
    private SkillBase selectedSkill;
    private bool isTargeting = false;
    private bool isDefenseTargeting = false;  // 방어 타겟팅 상태 플래그

    public int curveResolution = 50;  // 곡선의 변곡점 수

    public Color hoverOutlineColor = Color.red;
    public Color selectedOutlineColor = Color.white;

    private Outline currentHoverOutline;  // 마우스 커서가 가리키는 캐릭터의 외곽선
    private Outline selectedCharacterOutline;  // 현재 선택된 캐릭터의 외곽선

    void Start()
    {
        mainCamera = Camera.main;
        lineRenderer.enabled = false;
    }

    void Update()
    {
        // 마우스 커서가 올라간 캐릭터에 외곽선 적용
        HandleHoverOutline();

        if (!isTargeting && !isDefenseTargeting && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
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
            HandleSkillTargeting();
        }

        if (isDefenseTargeting)
        {
            HandleDefenseTargeting();
        }
    }

    // 기존 타겟팅 로직
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

    // 방어 타겟팅 로직
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

    //인벤토리 열람
    public void OpenPlayerInventory()
    {
        if (selectedCharacter.character.IsMine)
            selectedCharacter.character.CharacterInventory.OpenWindow();
        Debug.Log(selectedCharacter.character.CharacterInventory);
                //ItemManager.PlayerInventoryHolder.OpenWindow();
    }

    public void OpenPlayerEquipment()
    {
        if (selectedCharacter.character.IsMine)
            selectedCharacter.character.CharacterEquipment.OpenWindow();
        //ItemManager.PlayerEquipmentHolder.OpenWindow();
    }
    public void OpenSkills()
    {
        if (selectedCharacter.character.IsMine)
            ItemManager.PlayerInventoryHolder.OpenWindowByName("Skills", "Skills"); //An example to use "Hidden Items" to achive "Skills" management.
    }

    private void HandleHoverOutline()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.TryGetComponent<CharacterManager>(out var characterManager))
            {
                // 현재 선택된 캐릭터와 동일한 캐릭터에 커서가 있을 경우에는 외곽선을 업데이트하지 않음
                if (selectedCharacter == characterManager)
                    return;

                // 기존 커서 외곽선을 지워준다
                if (currentHoverOutline != null && currentHoverOutline != characterManager.GetComponent<Outline>())
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
                // 커서가 다른 곳을 가리키면 외곽선을 제거
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
        Vector2 cursorHotspot = new Vector2(cursorTexture.width * 0.3f, 0);
        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);        
    }

    public void SelectCharacter(CharacterManager characterManager)
    {
        // 타겟팅 상태에서는 선택된 캐릭터를 변경하지 않음
        if (isTargeting || isDefenseTargeting)
        {
            ChangeCursor("Basic");
            return;
        }

        // 기존 선택된 캐릭터 외곽선 해제
        if (selectedCharacterOutline != null)
        {
            selectedCharacterOutline.enabled = false;
        }

        selectedCharacter = characterManager;
        UIManager.Instance.DisplayCharacterInfo(characterManager);

        if (selectedCharacter.character.IsMine)
        {
            // Player Inventory Holder 선택된 아군 캐릭터로 교체
            ItemManager.PlayerInventoryHolder = selectedCharacter.character.CharacterInventory;
            ItemManager.PlayerEquipmentHolder = selectedCharacter.character.CharacterEquipment;
            HoverInformation.SetCompareHolder(selectedCharacter.character.CharacterEquipment);
        }

        // 선택된 캐릭터 외곽선 적용
        selectedCharacterOutline = selectedCharacter.GetComponent<Outline>();
        if (selectedCharacterOutline != null)
        {
            selectedCharacterOutline.OutlineColor = selectedOutlineColor;
            selectedCharacterOutline.enabled = true;
        }

        // 커서 외곽선이 동일한 캐릭터를 가리키고 있으면 제거
        if (currentHoverOutline == selectedCharacterOutline)
        {
            currentHoverOutline = null;
        }
    }

    public void StartTargeting(SkillBase skill)
    {
        if (selectedCharacter == null) return;

        selectedSkill = skill;

        if (selectedSkill.IsRangedSkill)
        {
            // 커서를 원거리 스킬용으로 변경
            ChangeCursor("Shoot");
            isTargeting = true;
            // 원거리 타겟팅 로직 구현 필요
        }
        else if (!selectedSkill.IsRangedSkill)
        {            
            ChangeCursor("Attack");
            isTargeting = true;
            lineRenderer.enabled = true;
        }
    }

    void UpdateBezierCurve()
    {
        if (selectedCharacter == null)
            return;

        Vector3 startPosition = selectedCharacter.transform.position + Vector3.up * 1;
        // 마우스 위치를 월드 좌표로 변환
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

        // 스킬 아이콘 표시
        //targetUIManager.DisplaySkillIcon(selectedSkill, target);

        // 베지어 곡선 업데이트
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

        // 스킬 실행 큐에 추가
        if(selectedSkill.IsCounterSkill)
            selectedCharacter.SelectCounterSkill(selectedSkill, target);
        else
            selectedCharacter.SelectSkill(selectedSkill, target);
        ChangeCursor("Basic");
    }

    public void StartDefenseTargeting(CharacterManager defenseCharacter)
    {
        if (defenseCharacter == null) return;

isDefenseTargeting = true;
        lineRenderer.enabled = true;

        selectedCharacter = defenseCharacter;  // 방어 캐릭터 지정
        ChangeCursor("Deff");  // 방어 커서로 변경
        Debug.Log($"{defenseCharacter.character.Name}의 방어 대상 타겟팅을 시작합니다.");

        // 라인 렌더러 초기화
        UpdateBezierCurve();
    }

    private void SelectDefenseTarget(CharacterManager target)
    {
        // 방어 캐릭터와 방어 대상은 같은 진영이어야 하며, 방어 캐릭터 자신을 방어 대상으로 선택할 수 없음
        if (target.character.IsMine == selectedCharacter.character.IsMine && target != selectedCharacter)
        {
            selectedTarget = target;
            isDefenseTargeting = false;

            // 베지어 곡선으로 방어 관계 표시
            UpdateBezierCurve();
            Debug.Log($"{selectedCharacter.character.Name}가 {target.character.Name}을 방어합니다.");

            // 방어 캐릭터에 방어 대상을 설정
            selectedCharacter.combatHandler.SetDefenseTarget(target);
            StopTargeting();

            // 방어 스킬 UI 출력
            UIManager.Instance.UpdateHotbarSkills(selectedCharacter);
        }
    }


    // 타겟팅 종료 메서드
    public void StopTargeting()
    {
        isTargeting = false;
        isDefenseTargeting = false;  // 방어 타겟팅도 종료
        lineRenderer.enabled = false;
        ChangeCursor("Basic");
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
