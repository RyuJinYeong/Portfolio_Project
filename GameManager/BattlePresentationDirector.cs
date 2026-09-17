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

    [Header("Combat Impact Sound")]
    [SerializeField] private AudioClip[] unarmedImpactSounds;
    [SerializeField] private AudioClip[] bladeBlockSounds;
    [SerializeField] private AudioClip[] bladeHitSounds;
    [SerializeField] private AudioClip[] bluntBlockSounds;
    [SerializeField] private AudioClip[] bluntHitSounds;
    [SerializeField] private AudioClip[] arrowBlockSounds;
    [SerializeField] private AudioClip[] arrowHitSounds;
    [SerializeField] private AudioClip[] magicBlockSounds;
    [SerializeField] private AudioClip[] magicHitSounds;
    [SerializeField, Range(0f, 1f)] private float impactSoundVolume = 0.68f;
    [SerializeField, Range(0f, 1f)] private float impactSoundSpatialBlend = 0.85f;
    [SerializeField, Range(0f, 0.2f)] private float impactSoundPitchVariation = 0.04f;

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
    private readonly List<PendingDamagePopup> pendingDamagePopups = new();
    private Coroutine cameraShakeRoutine;
    private Vector3 cameraShakeBaseLocalPosition;

    private struct PendingDamagePopup
    {
        public int damage;
        public Vector3 position;
    }

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
        pendingDamagePopups.Clear();
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
        pendingDamagePopups.Clear();

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

        attacker.combatHandler?.PrepareProtectionForPresentation(action);

        List<KeyValuePair<CharacterManager, SkillDefinitionSO>> successfulCounters = new();

        if (attacker.combatHandler != null)
        {
            foreach (KeyValuePair<CharacterManager, SkillDefinitionSO> counter in
                     attacker.combatHandler.LastSuccessfulCounters)
            {
                if (counter.Key != null && counter.Value != null)
                    successfulCounters.Add(counter);
            }
        }

        bool hasInterception =
            protector != null &&
            protector != originalTarget &&
            protectorPresentation != null &&
            attacker.combatHandler != null &&
            attacker.combatHandler.LastProtectionSucceeded &&
            attacker.combatHandler.LastResolvedTarget == protector;

        bool protectionFailed =
            protector != null &&
            protector != originalTarget &&
            protectorPresentation != null &&
            attacker.combatHandler != null &&
            attacker.combatHandler.LastProtectionAttempted &&
            !attacker.combatHandler.LastProtectionSucceeded;

        if (protectionFailed)
            protectorPresentation.FaceTarget(originalTarget.transform);

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

        CharacterManager effectTarget = attacker.combatHandler != null
            ? attacker.combatHandler.LastResolvedTarget
            : null;
        attackerPresentation.PlaySkill(
            action.skill,
            (effectTarget != null ? effectTarget : approachTarget).transform);

        yield return attackerPresentation.WaitForSkillImpact();

        float impactWait = 0f;

        if (impactDelay > 0f)
        {
            float impactDurationScale = Mathf.Clamp(
                2f - action.skill.activationSpeed,
                0.25f,
                2f);
            impactWait = impactDelay /
                         Mathf.Max(0.01f, attackerPresentation.SkillAnimationSpeed) *
                         impactDurationScale;
        }

        impactWait = Mathf.Max(
            impactWait,
            attackerPresentation.GetSkillVisualImpactLeadTime(action.skill));

        foreach (KeyValuePair<CharacterManager, SkillDefinitionSO> counter in successfulCounters)
        {
            BattlePresentationHandler counterPresentation =
                counter.Key != null ? counter.Key.battlePresentationHandler : null;

            if (counterPresentation == null ||
                counter.Key.character == null ||
                !counter.Key.character.IsAlive)
            {
                continue;
            }

            impactWait = Mathf.Max(
                impactWait,
                counterPresentation.GetSkillContactLeadTime(counter.Value));
        }

        foreach (KeyValuePair<CharacterManager, SkillDefinitionSO> counter in successfulCounters)
        {
            BattlePresentationHandler counterPresentation =
                counter.Key != null ? counter.Key.battlePresentationHandler : null;

            if (counterPresentation == null ||
                counter.Key.character == null ||
                !counter.Key.character.IsAlive)
            {
                continue;
            }

            float contactLeadTime = counterPresentation.GetSkillContactLeadTime(counter.Value);
            StartCoroutine(PlayCounterForImpact(
                counter.Key,
                counter.Value,
                attacker.transform,
                Mathf.Max(0f, impactWait - contactLeadTime)));
        }

        if (impactWait > 0f)
            yield return new WaitForSeconds(impactWait);

        try
        {
            applyImpact?.Invoke();
        }
        catch
        {
            pendingDamagePopups.Clear();
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

        bool counterSucceeded = resolvedTarget != null && attacker.combatHandler != null &&
            attacker.combatHandler.LastSuccessfulCounters.ContainsKey(resolvedTarget);

        if (successfulCounters.Count > 0)
        {
            foreach (KeyValuePair<CharacterManager, SkillDefinitionSO> counter in successfulCounters)
            {
                if (counter.Key == null || counter.Value == null ||
                    counter.Value.counterActionType == CounterActionType.Evade)
                {
                    continue;
                }

                PlayImpactSound(action.skill, true, counter.Key.transform.position);
            }
        }

        if (resolvedTarget != null)
        {
            if (resolvedTarget.character != null && resolvedTarget.character.IsAlive)
            {
                if (!counterSucceeded)
                {
                    PlayImpactSound(action.skill, false, resolvedTarget.transform.position);
                    resolvedTarget.battlePresentationHandler?.PlayHit();
                }
            }
            else
            {
                if (!counterSucceeded)
                    PlayImpactSound(action.skill, false, resolvedTarget.transform.position);

                resolvedTarget.battlePresentationHandler?.PlayDeath();
            }
        }

        FlushDamagePopups();

        bool blendIntoRepeatedSkill =
            repeatsSameSkill &&
            !hasInterception &&
            successfulCounters.Count == 0 &&
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

    public bool TryQueueDamagePopup(int damage, Vector3 position)
    {
        if (!IsPresenting)
            return false;

        pendingDamagePopups.Add(new PendingDamagePopup
        {
            damage = damage,
            position = position
        });
        return true;
    }

    private void FlushDamagePopups()
    {
        if (UIManager.Instance != null)
        {
            foreach (PendingDamagePopup popup in pendingDamagePopups)
                UIManager.Instance.ShowDamage(popup.damage, popup.position);
        }

        pendingDamagePopups.Clear();
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

    private IEnumerator PlayCounterForImpact(
        CharacterManager counterUser,
        SkillDefinitionSO counterSkill,
        Transform attacker,
        float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (counterUser == null || counterUser.character == null ||
            !counterUser.character.IsAlive || counterSkill == null)
        {
            yield break;
        }

        counterUser.battlePresentationHandler?.FaceTarget(attacker);
        counterUser.battlePresentationHandler?.PlaySkill(counterSkill, attacker);
    }

    private void PlayImpactSound(
        SkillDefinitionSO attackSkill,
        bool wasBlocked,
        Vector3 position)
    {
        AudioClip[] sounds = GetImpactSounds(attackSkill, wasBlocked);

        if (sounds == null || sounds.Length == 0)
            return;

        AudioClip clip = sounds[UnityEngine.Random.Range(0, sounds.Length)];

        if (clip == null)
            return;

        GameObject soundObject = new GameObject("OneShot: " + clip.name);
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = impactSoundVolume;
        source.pitch = 1f + UnityEngine.Random.Range(
            -impactSoundPitchVariation,
            impactSoundPitchVariation);
        source.spatialBlend = impactSoundSpatialBlend;
        source.dopplerLevel = 0f;
        source.playOnAwake = false;
        source.Play();

        Destroy(
            soundObject,
            clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch)) + 0.1f);
    }

    private AudioClip[] GetImpactSounds(SkillDefinitionSO attackSkill, bool wasBlocked)
    {
        switch (GetImpactSoundCategory(attackSkill))
        {
            case SkillImpactSoundCategory.Unarmed:
                return unarmedImpactSounds;
            case SkillImpactSoundCategory.Blade:
                return wasBlocked ? bladeBlockSounds : bladeHitSounds;
            case SkillImpactSoundCategory.Blunt:
                return wasBlocked ? bluntBlockSounds : bluntHitSounds;
            case SkillImpactSoundCategory.Arrow:
                return wasBlocked ? arrowBlockSounds : arrowHitSounds;
            case SkillImpactSoundCategory.Magic:
                return wasBlocked ? magicBlockSounds : magicHitSounds;
            default:
                return bluntHitSounds;
        }
    }

    private static SkillImpactSoundCategory GetImpactSoundCategory(
        SkillDefinitionSO attackSkill)
    {
        if (attackSkill == null)
            return SkillImpactSoundCategory.Blunt;

        if (attackSkill.impactSoundCategory != SkillImpactSoundCategory.Auto)
            return attackSkill.impactSoundCategory;

        switch (attackSkill.discipline)
        {
            case SkillDiscipline.MartialArt:
                return SkillImpactSoundCategory.Unarmed;
            case SkillDiscipline.Swordsmanship:
            case SkillDiscipline.DaggerArt:
                return SkillImpactSoundCategory.Blade;
            case SkillDiscipline.ShieldArt:
                return SkillImpactSoundCategory.Blunt;
            case SkillDiscipline.Archery:
                return SkillImpactSoundCategory.Arrow;
            case SkillDiscipline.Magic:
                return SkillImpactSoundCategory.Magic;
        }

        if (attackSkill.type == SkillType.Magical)
            return SkillImpactSoundCategory.Magic;

        if (attackSkill.damageComponents != null)
        {
            foreach (SkillDamageComponentData component in attackSkill.damageComponents)
            {
                if (component == null)
                    continue;

                switch (component.attribute)
                {
                    case SkillAttribute.Fire:
                    case SkillAttribute.Ice:
                    case SkillAttribute.Lightning:
                    case SkillAttribute.Magic:
                        return SkillImpactSoundCategory.Magic;
                    case SkillAttribute.Pierce:
                    case SkillAttribute.Slash:
                        return SkillImpactSoundCategory.Blade;
                    case SkillAttribute.Smash:
                        return SkillImpactSoundCategory.Blunt;
                }
            }
        }

        return SkillImpactSoundCategory.Blunt;
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
