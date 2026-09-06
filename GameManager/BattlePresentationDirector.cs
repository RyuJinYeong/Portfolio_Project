using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlePresentationDirector : MonoBehaviour
{
    public static BattlePresentationDirector Instance { get; private set; }
    public bool IsPresenting { get; private set; }

    [Header("Camera Timing")]
    [SerializeField] private float cameraMoveTime = 0.35f;
    [SerializeField] private float attackerIntroTime = 0.2f;
    [SerializeField] private float impactDelay = 0.35f;

    [Header("Camera Framing")]
    [SerializeField] private float attackerFocusDistance = 5.5f;
    [SerializeField] private float meleeFocusDistance = 6f;
    [SerializeField] private float attackerFocusFov = 34f;
    [SerializeField] private float meleeFocusFov = 35f;
    [SerializeField] private int cameraFollowFrameDelay = 3;
    [SerializeField] private float cameraFollowSharpness = 8f;

    [Header("Camera Impact")]
    [SerializeField] private float impactShakeDuration = 0.14f;
    [SerializeField] private float impactShakeStrength = 0.08f;

    [Header("Movement")]
    [SerializeField] private float meleeStopDistance = 1f;
    [SerializeField] private float protectedTargetRetreatDistance = 0.8f;
    [SerializeField] private float returnJumpDuration = 0.45f;
    [SerializeField] private float returnJumpHeight = 0.6f;
    [SerializeField] private float returnCameraDelay = 0.5f;
    [SerializeField] private float battleSpeedMultiplier = 2f;
    [SerializeField, Range(0f, 0.5f)] private float repeatedSkillEndSkipRatio = 0.2f;

    private readonly HashSet<BattlePresentationHandler> movingHandlers = new();
    private readonly HashSet<BattlePresentationHandler> skillHandlers = new();
    private readonly HashSet<BattlePresentationHandler> runBackHandlers = new();
    private Camera battleCamera;
    private Vector3 cameraHomePosition;
    private Quaternion cameraHomeRotation;
    private float cameraHomeFov;
    private bool hasCameraHome;
    private CharacterManager sequenceAttacker;
    private CharacterManager sequenceTarget;
    private bool sequenceRanged;
    private bool cameraZoomed;
    private readonly List<GameObject> hiddenHudObjects = new();
    private Coroutine cameraShakeRoutine;
    private Vector3 cameraShakeBaseLocalPosition;

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
        ResetCameraShake();
        StopAllCoroutines();
        StopTrackedMovement();
        IsPresenting = false;
        sequenceAttacker = null;
        sequenceTarget = null;
        cameraZoomed = false;
        runBackHandlers.Clear();
        ResolveBattleCamera();
        CaptureCameraHome();

        if (characters == null)
            return;

        foreach (CharacterManager character in characters)
            character?.battlePresentationHandler?.CaptureHomePose();
    }

    public IEnumerator PlaySkill(
        SkillQueueData action,
        Action applyImpact,
        bool repeatsSameSkill = false)
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

        IsPresenting = true;

        ResolveBattleCamera();

        bool startsSequence = sequenceAttacker != attacker;
        bool needsFraming = startsSequence || sequenceTarget != originalTarget || sequenceRanged != isRanged;

        if (isRanged && cameraZoomed)
        {
            yield return RestoreCameraHome();
            RestoreBattleHud();
            cameraZoomed = false;
        }

        if (startsSequence && !isRanged)
        {
            Vector3 attackerFocus = GetFocusPoint(attacker.transform);
            yield return MoveCameraToFocus(
                attackerFocus,
                attackerFocusDistance,
                attackerFocusFov);

            if (attackerIntroTime > 0f)
                yield return new WaitForSeconds(attackerIntroTime / battleSpeedMultiplier);
        }

        CharacterManager protector = GetProtector(originalTarget);

        BattlePresentationHandler protectorPresentation = protector != null
            ? protector.battlePresentationHandler
            : null;

        CharacterManager anticipatedCounterUser = GetAnticipatedCounterUser(
            originalTarget,
            protector);
        SkillDefinitionSO anticipatedCounterSkill = GetFirstCounterSkill(
            anticipatedCounterUser);

        attacker.combatHandler?.PrepareProtectionForPresentation(action);

        bool hasInterception =
            protector != null &&
            protector != originalTarget &&
            protectorPresentation != null &&
            attacker.combatHandler != null &&
            attacker.combatHandler.LastProtectionSucceeded &&
            attacker.combatHandler.LastResolvedTarget == protector;

        if (hasInterception)
        {
            Vector3 protectedPosition = originalTarget.transform.position;
            Vector3 attackDirection = GetFlatDirection(
                attacker.transform.position,
                protectedPosition);

            StartTrackedMove(
                targetPresentation,
                protectedPosition + attackDirection * protectedTargetRetreatDistance);
            StartTrackedMove(protectorPresentation, protectedPosition);
            runBackHandlers.Add(targetPresentation);
            runBackHandlers.Add(protectorPresentation);

            if (!isRanged)
            {
                StartTrackedMove(
                    attackerPresentation,
                    protectedPosition - attackDirection * meleeStopDistance);
                yield return FollowTrackedMovement(attackerPresentation, attacker.transform);
            }

            yield return WaitForTrackedMovement();
            targetPresentation.FaceTarget(protector.transform);
        }

        CharacterManager approachTarget = hasInterception ? protector : originalTarget;

        if (!isRanged && needsFraming)
        {
            cameraZoomed = true;
            HideBattleHud();
            Vector3 attackDirection = GetFlatDirection(attacker.transform.position, originalTarget.transform.position);

            if (!hasInterception)
            {
                Vector3 destination = approachTarget.transform.position - attackDirection * meleeStopDistance;
                StartTrackedMove(attackerPresentation, destination);
                yield return FollowTrackedMovement(attackerPresentation, attacker.transform);
                yield return WaitForTrackedMovement();
            }

            attackerPresentation.FaceTarget(approachTarget.transform);
            approachTarget.battlePresentationHandler?.FaceTarget(attacker.transform);
            if (hasInterception)
                protectorPresentation.FaceTarget(attacker.transform);

            yield return MoveCameraToPair(
                attacker.transform,
                approachTarget.transform,
                meleeFocusDistance,
                meleeFocusFov);
        }
        else
        {
            attackerPresentation.FaceTarget(approachTarget.transform);
        }

        skillHandlers.Add(attackerPresentation);
        skillHandlers.Add(targetPresentation);
        if (protectorPresentation != null)
            skillHandlers.Add(protectorPresentation);

        attackerPresentation.PlaySkill(action.skill);

        if (anticipatedCounterSkill != null)
        {
            anticipatedCounterUser.battlePresentationHandler?.FaceTarget(attacker.transform);
            anticipatedCounterUser.battlePresentationHandler?.PlaySkill(anticipatedCounterSkill);
        }

        yield return attackerPresentation.WaitForSkillImpact();

        if (impactDelay > 0f)
        {
            float impactDurationScale = Mathf.Clamp(
                2f - action.skill.activationSpeed,
                0.25f,
                2f);
            yield return new WaitForSeconds(
                impactDelay /
                Mathf.Max(0.01f, attackerPresentation.SkillAnimationSpeed) *
                impactDurationScale);
        }

        try
        {
            applyImpact?.Invoke();
        }
        catch
        {
            foreach (BattlePresentationHandler handler in skillHandlers)
                handler?.StopSkill();
            skillHandlers.Clear();
            RestoreBattleHud();
            IsPresenting = false;
            throw;
        }

        if (attacker.combatHandler != null && attacker.combatHandler.LastSkillWasCancelled)
            attackerPresentation.StopSkill();

        if (attacker.character != null && !attacker.character.IsAlive)
            attackerPresentation.PlayDeath();

        CharacterManager resolvedTarget = attacker.combatHandler != null
            ? attacker.combatHandler.LastResolvedTarget
            : originalTarget;

        if (resolvedTarget != null &&
            (attacker.combatHandler == null || !attacker.combatHandler.LastSkillWasCancelled) &&
            action.skill.GetTotalDamageMultiplier() > 0f)
        {
            float strengthMultiplier = Mathf.Clamp(
                action.skill.GetTotalDamageMultiplier(),
                0.75f,
                1.35f);
            StartImpactShake(strengthMultiplier);
        }

        if (attacker.combatHandler != null)
        {
            foreach (var counter in attacker.combatHandler.LastSuccessfulCounters)
            {
                if (counter.Key.character != null && counter.Key.character.IsAlive)
                {
                    counter.Key.battlePresentationHandler?.FaceTarget(attacker.transform);

                    if (counter.Key != anticipatedCounterUser)
                        counter.Key.battlePresentationHandler?.PlaySkill(counter.Value);
                }
            }
        }

        bool anticipatedCounterSucceeded = anticipatedCounterUser != null &&
            attacker.combatHandler != null &&
            attacker.combatHandler.LastSuccessfulCounters.ContainsKey(anticipatedCounterUser);

        if (anticipatedCounterUser != null &&
            anticipatedCounterUser != resolvedTarget &&
            !anticipatedCounterSucceeded &&
            anticipatedCounterUser.character != null &&
            anticipatedCounterUser.character.IsAlive)
        {
            anticipatedCounterUser.battlePresentationHandler?.PlayHit();
        }

        bool counterSucceeded = resolvedTarget != null && attacker.combatHandler != null &&
            attacker.combatHandler.LastSuccessfulCounters.ContainsKey(resolvedTarget);

        if (resolvedTarget != null)
        {
            if (resolvedTarget.character != null && resolvedTarget.character.IsAlive)
            {
                if (!counterSucceeded)
                    resolvedTarget.battlePresentationHandler?.PlayHit();
            }
            else
                resolvedTarget.battlePresentationHandler?.PlayDeath();
        }

        bool blendIntoRepeatedSkill =
            repeatsSameSkill &&
            !hasInterception &&
            anticipatedCounterSkill == null &&
            !attacker.combatHandler.LastSkillWasCancelled &&
            resolvedTarget != null &&
            resolvedTarget.character != null &&
            resolvedTarget.character.IsAlive;

        yield return attackerPresentation.WaitForSkill(
            blendIntoRepeatedSkill ? repeatedSkillEndSkipRatio : 0f);
        yield return targetPresentation.WaitForSkill();
        if (protectorPresentation != null)
            yield return protectorPresentation.WaitForSkill();
        skillHandlers.Clear();

        sequenceAttacker = attacker;
        sequenceTarget = resolvedTarget != null ? resolvedTarget : originalTarget;
        sequenceRanged = isRanged;
    }

    public IEnumerator ReturnCharactersHome(IList<CharacterManager> characters)
    {
        IsPresenting = true;

        foreach (CharacterManager character in characters)
        {
            if (character == null || character.character == null || !character.character.IsAlive)
                continue;

            BattlePresentationHandler handler = character.battlePresentationHandler;
            if (handler != null &&
                (character.transform.position - handler.HomePosition).sqrMagnitude > 0.0001f)
                StartTrackedReturn(handler);
        }

        if (cameraZoomed)
        {
            if (movingHandlers.Count > 0 && returnCameraDelay > 0f)
                yield return new WaitForSeconds(returnCameraDelay);

            yield return RestoreCameraHome();
        }

        yield return WaitForTrackedMovement();
        sequenceAttacker = null;
        sequenceTarget = null;
        cameraZoomed = false;
        runBackHandlers.Clear();
        RestoreBattleHud();
        IsPresenting = false;
    }

    public void CancelAndRestore()
    {
        ResetCameraShake();
        StopAllCoroutines();
        StopTrackedMovement();
        RestoreBattleHud();
        IsPresenting = false;
        sequenceAttacker = null;
        sequenceTarget = null;
        cameraZoomed = false;
        runBackHandlers.Clear();

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

    private void StartImpactShake(float strengthMultiplier)
    {
        if (battleCamera == null || !battleCamera.isActiveAndEnabled ||
            impactShakeDuration <= 0f || impactShakeStrength <= 0f)
        {
            return;
        }

        ResetCameraShake();
        cameraShakeBaseLocalPosition = battleCamera.transform.localPosition;
        cameraShakeRoutine = StartCoroutine(ShakeCamera(
            impactShakeDuration,
            impactShakeStrength * Mathf.Max(0f, strengthMultiplier)));
    }

    private IEnumerator ShakeCamera(float duration, float strength)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float remaining = 1f - elapsed / duration;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * strength * remaining;
            battleCamera.transform.localPosition = cameraShakeBaseLocalPosition +
                                                   new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        if (battleCamera != null)
            battleCamera.transform.localPosition = cameraShakeBaseLocalPosition;

        cameraShakeRoutine = null;
    }

    private void ResetCameraShake()
    {
        if (cameraShakeRoutine == null)
            return;

        StopCoroutine(cameraShakeRoutine);

        if (battleCamera != null)
            battleCamera.transform.localPosition = cameraShakeBaseLocalPosition;

        cameraShakeRoutine = null;
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

        ResetCameraShake();
        Vector3 startPosition = battleCamera.transform.position;
        Quaternion startRotation = battleCamera.transform.rotation;
        float startFov = battleCamera.fieldOfView;
        float duration = Mathf.Max(0.01f, cameraMoveTime / battleSpeedMultiplier);

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
        IEnumerator movement = runBackHandlers.Contains(handler)
            ? handler.MoveBackToHome()
            : handler.JumpBackToHome(
                returnJumpDuration / battleSpeedMultiplier,
                returnJumpHeight);
        StartCoroutine(TrackMovement(handler, movement));
    }

    private static CharacterManager GetAnticipatedCounterUser(
        CharacterManager originalTarget,
        CharacterManager protector)
    {
        if (GetFirstCounterSkill(protector) != null)
            return protector;

        if (GetFirstCounterSkill(originalTarget) != null)
            return originalTarget;

        return null;
    }

    private static SkillDefinitionSO GetFirstCounterSkill(CharacterManager character)
    {
        if (character == null || character.combatHandler == null)
            return null;

        List<SkillQueueData> queue = character.combatHandler.GetCounterSkillQueue();

        return queue != null && queue.Count > 0 && queue[0] != null
            ? queue[0].skill
            : null;
    }

    private IEnumerator FollowTrackedMovement(
        BattlePresentationHandler movingHandler,
        Transform movingCharacter)
    {
        if (battleCamera == null || movingHandler == null || movingCharacter == null)
            yield break;

        ResetCameraShake();
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
        foreach (BattlePresentationHandler handler in skillHandlers)
            handler?.StopSkill();

        skillHandlers.Clear();

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
