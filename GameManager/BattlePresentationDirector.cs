using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlePresentationDirector : MonoBehaviour
{
    public static BattlePresentationDirector Instance { get; private set; }

    [Header("Camera Timing")]
    [SerializeField] private float cameraMoveTime = 0.35f;
    [SerializeField] private float attackerIntroTime = 0.2f;
    [SerializeField] private float impactDelay = 0.35f;
    [SerializeField] private float resultHoldTime = 0.35f;

    [Header("Camera Framing")]
    [SerializeField] private float attackerFocusDistance = 5.5f;
    [SerializeField] private float meleeFocusDistance = 6f;
    [SerializeField] private float attackerFocusFov = 34f;
    [SerializeField] private float meleeFocusFov = 35f;
    [SerializeField] private int cameraFollowFrameDelay = 3;
    [SerializeField] private float cameraFollowSharpness = 8f;

    [Header("Movement")]
    [SerializeField] private float meleeStopDistance = 1f;
    [SerializeField] private float protectedTargetRetreatDistance = 0.8f;
    [SerializeField] private float returnJumpDuration = 0.45f;
    [SerializeField] private float returnJumpHeight = 0.6f;

    private readonly HashSet<BattlePresentationHandler> movingHandlers = new();
    private Camera battleCamera;
    private Vector3 cameraHomePosition;
    private Quaternion cameraHomeRotation;
    private float cameraHomeFov;
    private bool hasCameraHome;
    private readonly List<GameObject> hiddenHudObjects = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResolveBattleCamera();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PrepareBattle(IList<CharacterManager> characters)
    {
        StopAllCoroutines();
        StopTrackedMovement();
        ResolveBattleCamera();
        CaptureCameraHome();

        if (characters == null)
            return;

        foreach (CharacterManager character in characters)
            character?.battlePresentationHandler?.CaptureHomePose();
    }

    public IEnumerator PlaySkill(SkillQueueData action, Action applyImpact)
    {
        if (action == null || action.user == null || action.target == null || action.skill == null)
        {
            applyImpact?.Invoke();
            yield break;
        }

        CharacterManager attacker = action.user;
        CharacterManager originalTarget = action.target;
        bool isRanged = action.skill.isRangedSkill;
        BattlePresentationHandler attackerPresentation = attacker.battlePresentationHandler;
        BattlePresentationHandler targetPresentation = originalTarget.battlePresentationHandler;

        if (attackerPresentation == null || targetPresentation == null)
        {
            applyImpact?.Invoke();
            yield break;
        }

        ResolveBattleCamera();

        if (!isRanged)
            HideBattleHud();

        if (!isRanged)
        {
            Vector3 attackerFocus = GetFocusPoint(attacker.transform);
            yield return MoveCameraToFocus(
                attackerFocus,
                attackerFocusDistance,
                attackerFocusFov);

            if (attackerIntroTime > 0f)
                yield return new WaitForSeconds(attackerIntroTime);
        }

        CharacterManager protector = GetProtector(originalTarget);
        bool hasInterception = !isRanged && protector != null && protector != originalTarget;

        BattlePresentationHandler protectorPresentation = hasInterception
            ? protector.battlePresentationHandler
            : null;

        if (!isRanged)
        {
            CharacterManager approachTarget = hasInterception ? protector : originalTarget;
            Vector3 attackDirection = GetFlatDirection(attacker.transform.position, originalTarget.transform.position);

            if (hasInterception && protectorPresentation != null)
            {
                Vector3 originalPosition = originalTarget.transform.position;
                Vector3 protectorPosition = originalPosition - attackDirection * meleeStopDistance;
                Vector3 retreatPosition = originalPosition + attackDirection * protectedTargetRetreatDistance;
                Vector3 attackerPosition = protectorPosition - attackDirection * meleeStopDistance;

                StartTrackedMove(targetPresentation, retreatPosition);
                StartTrackedMove(protectorPresentation, protectorPosition);
                StartTrackedMove(attackerPresentation, attackerPosition);
                yield return FollowTrackedMovement(attackerPresentation, attacker.transform);
                yield return WaitForTrackedMovement();
            }
            else
            {
                Vector3 destination = approachTarget.transform.position - attackDirection * meleeStopDistance;
                StartTrackedMove(attackerPresentation, destination);
                yield return FollowTrackedMovement(attackerPresentation, attacker.transform);
                yield return WaitForTrackedMovement();
            }

            attackerPresentation.FaceTarget(approachTarget.transform);
            approachTarget.battlePresentationHandler?.FaceTarget(attacker.transform);

            yield return MoveCameraToPair(
                attacker.transform,
                approachTarget.transform,
                meleeFocusDistance,
                meleeFocusFov);
        }
        else
        {
            attackerPresentation.FaceTarget(originalTarget.transform);
        }

        attackerPresentation.PlayAttack();

        if (impactDelay > 0f)
            yield return new WaitForSeconds(impactDelay);

        try
        {
            applyImpact?.Invoke();
        }
        catch
        {
            RestoreBattleHud();
            throw;
        }

        CharacterManager resolvedTarget = attacker.combatHandler != null
            ? attacker.combatHandler.LastResolvedTarget
            : originalTarget;

        if (resolvedTarget != null)
        {
            if (resolvedTarget.character != null && resolvedTarget.character.IsAlive)
                resolvedTarget.battlePresentationHandler?.PlayHit();
            else
                resolvedTarget.battlePresentationHandler?.PlayDeath();
        }

        if (resultHoldTime > 0f)
            yield return new WaitForSeconds(resultHoldTime);

        bool keepMeleeEngagement =
            !isRanged &&
            attacker.character != null &&
            attacker.character.IsAlive &&
            resolvedTarget != null &&
            resolvedTarget.character != null &&
            resolvedTarget.character.IsAlive &&
            attacker.isInMeleeCombat &&
            attacker.meleeTarget == resolvedTarget;

        if (!isRanged && !keepMeleeEngagement)
        {
            if (attacker.character != null && attacker.character.IsAlive)
                StartTrackedReturn(attackerPresentation);

            if (hasInterception && protectorPresentation != null)
            {
                if (protector.character != null && protector.character.IsAlive)
                    StartTrackedReturn(protectorPresentation);

                if (originalTarget.character != null && originalTarget.character.IsAlive)
                    StartTrackedReturn(targetPresentation);
            }

            for (int frame = 0; frame < cameraFollowFrameDelay; frame++)
                yield return null;

            yield return RestoreCameraHome();
            yield return WaitForTrackedMovement();
        }
        else
        {
            yield return RestoreCameraHome();
        }

        RestoreBattleHud();
    }

    public void CancelAndRestore()
    {
        StopAllCoroutines();
        StopTrackedMovement();
        RestoreBattleHud();

        if (battleCamera != null && hasCameraHome)
        {
            battleCamera.transform.SetPositionAndRotation(
                cameraHomePosition,
                cameraHomeRotation);
            battleCamera.fieldOfView = cameraHomeFov;
        }
    }

    private CharacterManager GetProtector(CharacterManager originalTarget)
    {
        if (originalTarget == null ||
            originalTarget.combatHandler == null ||
            !originalTarget.combatHandler.isDefenseTarget ||
            TurnManager.Instance == null)
        {
            return null;
        }

        return TurnManager.Instance.defenseCharacter;
    }

    private void ResolveBattleCamera()
    {
        Camera childCamera = GetComponentInChildren<Camera>(true);

        if (childCamera != null)
        {
            battleCamera = childCamera;
            return;
        }

        if (SpawnPointManager.Instance != null)
            battleCamera = SpawnPointManager.Instance.GetComponentInChildren<Camera>(true);

        if (battleCamera == null)
            battleCamera = Camera.main;
    }

    private void CaptureCameraHome()
    {
        if (battleCamera == null)
            return;

        cameraHomePosition = battleCamera.transform.position;
        cameraHomeRotation = battleCamera.transform.rotation;
        cameraHomeFov = battleCamera.fieldOfView;
        hasCameraHome = true;
    }

    private IEnumerator MoveCameraToPair(
        Transform first,
        Transform second,
        float minimumDistance,
        float fov)
    {
        if (first == null || second == null)
            yield break;

        Vector3 center = (GetFocusPoint(first) + GetFocusPoint(second)) * 0.5f;
        float separation = Vector3.Distance(first.position, second.position);
        float distance = Mathf.Max(minimumDistance, minimumDistance + separation * 0.35f);

        yield return MoveCameraToFocus(center, distance, fov);
    }

    private IEnumerator MoveCameraToFocus(Vector3 focus, float distance, float fov)
    {
        if (battleCamera == null || !battleCamera.isActiveAndEnabled)
            yield break;

        if (!hasCameraHome)
            CaptureCameraHome();

        Vector3 forward = cameraHomeRotation * Vector3.forward;
        Vector3 targetPosition = focus - forward * distance;
        Quaternion targetRotation = Quaternion.LookRotation(focus - targetPosition, Vector3.up);

        yield return MoveCamera(targetPosition, targetRotation, fov);
    }

    private IEnumerator RestoreCameraHome()
    {
        if (battleCamera == null || !hasCameraHome || !battleCamera.isActiveAndEnabled)
            yield break;

        yield return MoveCamera(cameraHomePosition, cameraHomeRotation, cameraHomeFov);
    }

    private IEnumerator MoveCamera(Vector3 position, Quaternion rotation, float fov)
    {
        if (battleCamera == null)
            yield break;

        Vector3 startPosition = battleCamera.transform.position;
        Quaternion startRotation = battleCamera.transform.rotation;
        float startFov = battleCamera.fieldOfView;
        float duration = Mathf.Max(0.01f, cameraMoveTime);

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            battleCamera.transform.position = Vector3.Lerp(startPosition, position, t);
            battleCamera.transform.rotation = Quaternion.Slerp(startRotation, rotation, t);
            battleCamera.fieldOfView = Mathf.Lerp(startFov, fov, t);
            yield return null;
        }

        battleCamera.transform.SetPositionAndRotation(position, rotation);
        battleCamera.fieldOfView = fov;
    }

    private void StartTrackedMove(BattlePresentationHandler handler, Vector3 destination)
    {
        if (handler == null)
            return;

        movingHandlers.Add(handler);
        StartCoroutine(TrackMovement(handler, handler.MoveTo(destination)));
    }

    private void StartTrackedReturn(BattlePresentationHandler handler)
    {
        if (handler == null)
            return;

        movingHandlers.Add(handler);
        StartCoroutine(TrackMovement(
            handler,
            handler.JumpBackToHome(returnJumpDuration, returnJumpHeight)));
    }

    private IEnumerator FollowTrackedMovement(
        BattlePresentationHandler movingHandler,
        Transform movingCharacter)
    {
        if (battleCamera == null || movingHandler == null || movingCharacter == null)
            yield break;

        Queue<Vector3> delayedFocusPoints = new();
        int frameDelay = Mathf.Max(0, cameraFollowFrameDelay);
        Vector3 delayedFocus = GetFocusPoint(movingCharacter);

        while (movingHandlers.Contains(movingHandler))
        {
            delayedFocusPoints.Enqueue(GetFocusPoint(movingCharacter));

            if (delayedFocusPoints.Count > frameDelay)
                delayedFocus = delayedFocusPoints.Dequeue();

            Vector3 forward = cameraHomeRotation * Vector3.forward;
            Vector3 targetPosition = delayedFocus - forward * attackerFocusDistance;
            Quaternion targetRotation = Quaternion.LookRotation(
                delayedFocus - targetPosition,
                Vector3.up);
            float followT = 1f - Mathf.Exp(-cameraFollowSharpness * Time.deltaTime);

            battleCamera.transform.position = Vector3.Lerp(
                battleCamera.transform.position,
                targetPosition,
                followT);
            battleCamera.transform.rotation = Quaternion.Slerp(
                battleCamera.transform.rotation,
                targetRotation,
                followT);
            battleCamera.fieldOfView = Mathf.Lerp(
                battleCamera.fieldOfView,
                attackerFocusFov,
                followT);

            yield return null;
        }
    }

    private IEnumerator TrackMovement(BattlePresentationHandler handler, IEnumerator movement)
    {
        yield return movement;
        movingHandlers.Remove(handler);
    }

    private IEnumerator WaitForTrackedMovement()
    {
        while (movingHandlers.Count > 0)
            yield return null;
    }

    private void StopTrackedMovement()
    {
        foreach (BattlePresentationHandler handler in movingHandlers)
            handler?.StopMovementAnimation();

        movingHandlers.Clear();
    }

    private void HideBattleHud()
    {
        RestoreBattleHud();

        GameObject battleUiRoot = UIManager.Instance != null
            ? UIManager.Instance.battleUiRoot
            : null;

        HideHudObject(battleUiRoot);

        if (GameManager.Instance == null)
            return;

        List<CharacterManager> characters = GameManager.Instance.GetAllCharacters();

        if (characters == null)
            return;

        foreach (CharacterManager character in characters)
        {
            if (character != null && character.characterUIHandler != null)
                HideHudObject(character.characterUIHandler.StatusCanvas);
        }
    }

    private void HideHudObject(GameObject hudObject)
    {
        if (hudObject == null || !hudObject.activeSelf)
            return;

        hiddenHudObjects.Add(hudObject);
        hudObject.SetActive(false);
    }

    private void RestoreBattleHud()
    {
        foreach (GameObject hudObject in hiddenHudObjects)
        {
            if (hudObject != null)
                hudObject.SetActive(true);
        }

        hiddenHudObjects.Clear();
    }

    private static Vector3 GetFocusPoint(Transform target)
    {
        return target.position + Vector3.up * 1.2f;
    }

    private static Vector3 GetFlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return direction.normalized;
    }
}
