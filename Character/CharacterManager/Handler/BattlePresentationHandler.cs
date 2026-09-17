using System.Collections;
using System.Collections.Generic;
using Jorjouto.AnimComposerSystem;
using UnityEngine;
using UnityEngine.Playables;

public class BattlePresentationHandler : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float rotationSpeed = 1440f;
    [SerializeField] private float battleAnimationSpeed = 1.5f;
    [SerializeField] private float hitReactionAnimationSpeed = 1f;
    [SerializeField] private float standardSingleHitClipDuration = 2.6333334f;

    private const float MovementAnimationSpeed = 2f;
    private const float MinimumSkillPlaybackTimeout = 2f;
    private const float SkillPlaybackTimeoutPadding = 1f;
    private const string MaleHitComposerPath = "CombatPresentation/AC_HitReaction_Male";
    private const string FemaleHitComposerPath = "CombatPresentation/AC_HitReaction_Female";

    private static ScriptableObject_AnimComposer maleHitComposer;
    private static ScriptableObject_AnimComposer femaleHitComposer;

    private readonly HashSet<int> animatorParameters = new();
    private AnimCoordinatorComponent animCoordinator;
    private RuntimeAnimComposer activeSkillComposer;
    private Coroutine skillSequenceCoroutine;
    private bool isPlayingSkillSequence;
    private bool isSkillImpactReady;
    private float skillPlaybackDeadline;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private bool hasHomePose;
    private Coroutine deathPoseCoroutine;
    private Transform bowString;
    private Transform bowStringHandTarget;
    private Vector3 bowStringRestLocalPosition;
    private float bowStringPullWeight;
    private bool isAnimatingBowString;

    private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
    private static readonly int LocomotionStateHash = Animator.StringToHash("Base Layer.Blend Tree");
    private static readonly int GotHitHash = Animator.StringToHash("GotHit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int JumpBackHash = Animator.StringToHash("Jump2");

    public bool IsMoving { get; private set; }
    public Vector3 HomePosition => homePosition;
    public Animator Animator => animator;
    public float SkillAnimationSpeed
    {
        get
        {
            CharacterManager manager = GetComponentInParent<CharacterManager>();
            int bonus = manager?.character?.FinalSpecialStats?.SkillActivationSpeedBonus ?? 0;
            return battleAnimationSpeed * Mathf.Max(0.01f, 1f + bonus / 100f);
        }
    }
    public bool IsPlayingSkill => isPlayingSkillSequence || IsActiveComposer(activeSkillComposer);
    public bool CurrentSkillUsesOffHand { get; private set; }
    public SkillDefinitionSO CurrentSkill { get; private set; }
    public Transform CurrentSkillTarget { get; private set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        ConfigureAnimator(null, false);
        CaptureHomePose();
    }

    private void OnDisable()
    {
        if (deathPoseCoroutine != null)
        {
            StopCoroutine(deathPoseCoroutine);
            deathPoseCoroutine = null;
        }

        StopSkill();
    }

    private void LateUpdate()
    {
        if (!isAnimatingBowString || bowString == null || bowStringHandTarget == null)
            return;

        Vector3 restPosition = bowString.parent.TransformPoint(bowStringRestLocalPosition);
        bowString.position = Vector3.Lerp(
            restPosition,
            bowStringHandTarget.position,
            Mathf.Clamp01(bowStringPullWeight));
    }

    public void BindVisual(
        Transform visualRoot,
        RuntimeAnimatorController humanoidController = null,
        bool overrideHumanoidController = false)
    {
        Animator visualAnimator = FindPresentationAnimator(visualRoot);

        StopSkill();

        animator = visualAnimator != null
            ? visualAnimator
            : FindPresentationAnimator(transform);

        ConfigureAnimator(humanoidController, overrideHumanoidController);
        CaptureHomePose();
    }

    public void CaptureHomePose()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
        hasHomePose = true;
    }

    public IEnumerator MoveTo(Vector3 destination)
    {
        IsMoving = true;
        SetAnimatorPlaybackSpeed(MovementAnimationSpeed);

        Vector3 flatDirection = destination - transform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.0025f)
        {
            StopSkill();
            SetLocomotion(2f);
            if (animCoordinator != null)
            {
                if (animCoordinator.LocomotionPlayable.HasState(0, LocomotionStateHash))
                    animCoordinator.PlayAnimatorStateWithBlend(LocomotionStateHash, 0, 0.1f);
            }
            else if (animator != null && animator.HasState(0, LocomotionStateHash))
                animator.CrossFadeInFixedTime(LocomotionStateHash, 0.1f, 0);
        }

        while ((destination - transform.position).sqrMagnitude > 0.0025f)
        {
            Vector3 direction = destination - transform.position;
            Vector3 flatMoveDirection = direction;
            flatMoveDirection.y = 0f;

            if (flatMoveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(flatMoveDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime);

            yield return null;
        }

        transform.position = destination;
        SetLocomotion(0f);
        SetAnimatorPlaybackSpeed(battleAnimationSpeed);
        IsMoving = false;
    }

    public IEnumerator MoveBackToHome()
    {
        if (!hasHomePose)
            yield break;

        yield return MoveTo(homePosition);
        transform.rotation = homeRotation;
    }

    public IEnumerator JumpBackToHome(float duration, float jumpHeight)
    {
        if (!hasHomePose)
            yield break;

        IsMoving = true;
        SetAnimatorPlaybackSpeed(MovementAnimationSpeed);
        SetLocomotion(0f);
        SetTrigger(JumpBackHash);

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float moveDuration = Mathf.Max(0.01f, duration);

        for (float elapsed = 0f; elapsed < moveDuration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 position = Vector3.Lerp(startPosition, homePosition, eased);
            position.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;

            transform.position = position;
            transform.rotation = startRotation;
            yield return null;
        }

        transform.position = homePosition;
        transform.rotation = homeRotation;
        SetAnimatorPlaybackSpeed(battleAnimationSpeed);
        IsMoving = false;
    }

    public void FaceTarget(Transform target)
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    public void SetLocomotion(float value)
    {
        if (animator != null && animatorParameters.Contains(LocomotionHash))
        {
            if (animCoordinator != null)
                animCoordinator.LocomotionPlayable.SetFloat(LocomotionHash, value);
            else
                animator.SetFloat(LocomotionHash, value);
        }
    }

    public void PlaySkill(SkillDefinitionSO skill)
    {
        PlaySkill(skill, null);
    }

    public void PlaySkill(SkillDefinitionSO skill, Transform target)
    {
        ScriptableObject_AnimComposer composer = skill != null ? skill.composer : null;

        StopSkill();
        CurrentSkill = skill;
        CurrentSkillTarget = target;

        CharacterManager manager = GetComponentInParent<CharacterManager>();
        SkillRuntimeData runtime = manager != null && manager.character != null && skill != null
            ? SkillManager.GetRuntime(manager.character, skill.uid)
            : null;
        CurrentSkillUsesOffHand = runtime != null && runtime.useOffHand;

        if (composer == null)
        {
            PlayAttack();
            return;
        }

        if (skill.followUpComposers != null && skill.followUpComposers.Count > 0)
        {
            isPlayingSkillSequence = true;
            isSkillImpactReady = HasProjectileImpactBlock(composer);
            skillSequenceCoroutine = StartCoroutine(PlayComposerSequence(skill));
            return;
        }

        isSkillImpactReady = true;

        if (!PlayComposer(composer, GetSkillPlaybackRate(skill, composer)))
            PlayAttack();
    }

    public bool PlayComposer(ScriptableObject_AnimComposer composer)
    {
        float playbackRate = composer != null
            ? composer.PlayRate * battleAnimationSpeed
            : 0f;

        return PlayComposer(composer, playbackRate);
    }

    private bool PlayComposer(ScriptableObject_AnimComposer composer, float playbackRate)
    {
        if (composer == null)
            return false;

        if (composer.AnimationClip == null || composer.Loop || composer.PlayRate <= 0f)
        {
            Debug.LogWarning($"{composer.name}: 전투 Composer는 클립, 양수 재생 속도, Loop 해제가 필요합니다.", composer);
            return false;
        }

        if (animator == null)
            return false;

        if (animCoordinator == null)
            animCoordinator = animator.gameObject.AddComponent<AnimCoordinatorComponent>();

        SetAnimatorPlaybackSpeed(battleAnimationSpeed);
        animCoordinator.SetRootMotionBlocked(true);
        animator.applyRootMotion = false;
        activeSkillComposer = animCoordinator.PlayAnimComposer(composer);

        if (activeSkillComposer != null)
        {
            animCoordinator.SetAnimComposersRate(playbackRate);
            float playbackDuration = composer.AnimationClip.length /
                                     Mathf.Max(0.01f, Mathf.Abs(playbackRate));
            skillPlaybackDeadline = Time.time + Mathf.Max(
                MinimumSkillPlaybackTimeout,
                playbackDuration + composer.BlendOutTime + SkillPlaybackTimeoutPadding);
        }

        return activeSkillComposer != null;
    }

    public IEnumerator WaitForSkill(float endSkipRatio = 0f)
    {
        while (IsPlayingSkill)
        {
            if (HasSkillPlaybackTimedOut())
            {
                Debug.LogWarning($"{name}: 스킬 애니메이션 종료가 확인되지 않아 재생을 중단합니다.");
                StopSkill();
                yield break;
            }

            if (endSkipRatio > 0f &&
                activeSkillComposer != null &&
                activeSkillComposer.ElapsedTime >=
                activeSkillComposer.AnimationLength * (1f - Mathf.Clamp01(endSkipRatio)))
            {
                yield break;
            }

            yield return null;
        }
    }

    public IEnumerator WaitForSkillImpact()
    {
        while (isPlayingSkillSequence && !isSkillImpactReady)
        {
            if (HasSkillPlaybackTimedOut())
            {
                Debug.LogWarning($"{name}: 연속 스킬의 타격 시점이 확인되지 않아 재생을 중단합니다.");
                StopSkill();
                yield break;
            }

            yield return null;
        }
    }

    public float GetSkillContactLeadTime(SkillDefinitionSO skill)
    {
        ScriptableObject_AnimComposer composer = skill != null ? skill.composer : null;

        if (composer == null || composer.AnimationClip == null)
            return 0f;

        float contactTime = composer.AnimationClip.length * 0.2f;
        bool foundContactBlock = false;
        float projectileContactTime = 0f;
        bool foundProjectileContact = false;

        if (composer.Tracks != null)
        {
            foreach (AnimationTrack track in composer.Tracks)
            {
                if (track == null || track.ActionBlocks == null)
                    continue;

                foreach (ActionBlockData block in track.ActionBlocks)
                {
                    if (block == null || block.IsDisabled || block.Action == null)
                        continue;

                    if (block.Action is ActionBlock_BattleProjectileVfx)
                    {
                        projectileContactTime = Mathf.Max(projectileContactTime, block.EndTime);
                        foundProjectileContact = true;
                        continue;
                    }

                    string actionName = block.Action.CustomName;
                    bool isContactBlock =
                        !string.IsNullOrEmpty(actionName) &&
                        (actionName.Contains("Impact") ||
                         actionName.Contains("Intercept") ||
                         actionName.Contains("Swish") ||
                         actionName.Contains("Attack"));

                    if (!isContactBlock)
                        continue;

                    contactTime = foundContactBlock
                        ? Mathf.Min(contactTime, block.StartTime)
                        : block.StartTime;
                    foundContactBlock = true;
                }
            }
        }

        float playbackRate = GetSkillPlaybackRate(skill, composer);
        if (foundProjectileContact)
            contactTime = projectileContactTime;

        return contactTime / Mathf.Max(0.01f, Mathf.Abs(playbackRate));
    }

    public float GetSkillVisualImpactLeadTime(SkillDefinitionSO skill)
    {
        if (skill == null)
            return 0f;

        ScriptableObject_AnimComposer composer = skill.composer;

        if (!HasProjectileImpactBlock(composer) &&
            skill.followUpComposers != null && skill.followUpComposers.Count > 0)
        {
            composer = skill.followUpComposers[skill.followUpComposers.Count - 1];
        }

        if (composer == null || composer.AnimationClip == null || composer.Tracks == null)
            return 0f;

        float impactTime = 0f;

        foreach (AnimationTrack track in composer.Tracks)
        {
            if (track == null || track.ActionBlocks == null)
                continue;

            foreach (ActionBlockData block in track.ActionBlocks)
            {
                if (block == null || block.IsDisabled ||
                    block.Action is not ActionBlock_BattleProjectileVfx)
                {
                    continue;
                }

                impactTime = Mathf.Max(impactTime, block.EndTime);
            }
        }

        if (impactTime <= 0f)
            return 0f;

        float playbackRate = GetSkillPlaybackRate(skill, composer);
        return impactTime / Mathf.Max(0.01f, Mathf.Abs(playbackRate));
    }

    private static bool HasProjectileImpactBlock(ScriptableObject_AnimComposer composer)
    {
        if (composer == null || composer.Tracks == null)
            return false;

        foreach (AnimationTrack track in composer.Tracks)
        {
            if (track == null || track.ActionBlocks == null)
                continue;

            foreach (ActionBlockData block in track.ActionBlocks)
            {
                if (block != null && !block.IsDisabled &&
                    block.Action is ActionBlock_BattleProjectileVfx)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void StopSkill()
    {
        if (skillSequenceCoroutine != null)
        {
            StopCoroutine(skillSequenceCoroutine);
            skillSequenceCoroutine = null;
        }

        isPlayingSkillSequence = false;
        isSkillImpactReady = true;

        if (animCoordinator != null)
            animCoordinator.InterruptAllAnimComposers(0f);

        activeSkillComposer = null;
        skillPlaybackDeadline = 0f;
        CurrentSkillUsesOffHand = false;
        CurrentSkill = null;
        CurrentSkillTarget = null;
        ResetBowString();
    }

    private IEnumerator PlayComposerSequence(SkillDefinitionSO skill)
    {
        List<ScriptableObject_AnimComposer> sequence = new List<ScriptableObject_AnimComposer>
        {
            skill.composer
        };
        sequence.AddRange(skill.followUpComposers);

        bool animateBowString = skill.discipline == SkillDiscipline.Archery &&
                                BeginBowStringAnimation();

        for (int i = 0; i < sequence.Count; i++)
        {
            ScriptableObject_AnimComposer composer = sequence[i];

            if (composer == null || !PlayComposer(composer, GetSkillPlaybackRate(skill, composer)))
                continue;

            bool isLastComposer = i == sequence.Count - 1;

            if (isLastComposer)
                isSkillImpactReady = true;

            if (animateBowString && i > 0 && !isLastComposer)
                bowStringPullWeight = 1f;

            while (IsActiveComposer(activeSkillComposer))
            {
                if (HasSkillPlaybackTimedOut())
                {
                    Debug.LogWarning($"{name}: {composer.name} 재생이 종료되지 않아 연속 스킬을 중단합니다.", composer);
                    if (animCoordinator != null)
                        animCoordinator.InterruptAllAnimComposers(0f);

                    activeSkillComposer = null;
                    skillPlaybackDeadline = 0f;
                    isPlayingSkillSequence = false;
                    isSkillImpactReady = true;
                    skillSequenceCoroutine = null;
                    ResetBowString();
                    yield break;
                }

                if (animateBowString)
                    UpdateBowStringPull(i, sequence.Count, activeSkillComposer);

                if (!isLastComposer)
                {
                    ScriptableObject_AnimComposer nextComposer = sequence[i + 1];
                    float nextBlendTime = nextComposer != null
                        ? nextComposer.BlendInTime
                        : 0f;
                    float transitionTime = Mathf.Min(
                        activeSkillComposer.AnimationLength * 0.25f,
                        nextBlendTime * activeSkillComposer.PlayRate);

                    if (activeSkillComposer.ElapsedTime >=
                        activeSkillComposer.AnimationLength - transitionTime)
                    {
                        break;
                    }
                }

                yield return null;
            }
        }

        isPlayingSkillSequence = false;
        isSkillImpactReady = true;
        activeSkillComposer = null;
        skillPlaybackDeadline = 0f;
        skillSequenceCoroutine = null;
        ResetBowString();
    }

    private bool BeginBowStringAnimation()
    {
        ResetBowString();

        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            return false;

        Transform[] transforms = GetComponentsInChildren<Transform>(false);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name != "String" && candidate.name != "WB.string")
                continue;

            if (!IsBowTransform(candidate))
                continue;

            bowString = candidate;
            break;
        }

        if (bowString == null || bowString.parent == null)
            return false;

        bowStringHandTarget = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
        if (bowStringHandTarget == null)
            bowStringHandTarget = animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (bowStringHandTarget == null)
        {
            bowString = null;
            return false;
        }

        bowStringRestLocalPosition = bowString.localPosition;
        bowStringPullWeight = 0f;
        isAnimatingBowString = true;
        return true;
    }

    private void UpdateBowStringPull(
        int sequenceIndex,
        int sequenceCount,
        RuntimeAnimComposer composer)
    {
        if (!isAnimatingBowString || composer == null)
            return;

        float normalizedTime = composer.AnimationLength > 0f
            ? Mathf.Clamp01(composer.ElapsedTime / composer.AnimationLength)
            : 1f;

        if (sequenceIndex == 0)
        {
            bowStringPullWeight = Mathf.SmoothStep(0f, 1f, normalizedTime);
            return;
        }

        if (sequenceIndex == sequenceCount - 1)
        {
            bowStringPullWeight = 1f - Mathf.SmoothStep(0f, 1f, normalizedTime * 5f);
            return;
        }

        bowStringPullWeight = 1f;
    }

    private void ResetBowString()
    {
        if (bowString != null && bowString.parent != null)
            bowString.localPosition = bowStringRestLocalPosition;

        bowString = null;
        bowStringHandTarget = null;
        bowStringPullWeight = 0f;
        isAnimatingBowString = false;
    }

    private static bool IsBowTransform(Transform transformToCheck)
    {
        Transform current = transformToCheck;

        while (current != null)
        {
            if (current.name.ToLowerInvariant().Contains("bow"))
                return true;

            current = current.parent;
        }

        return false;
    }

    private bool HasSkillPlaybackTimedOut()
    {
        return skillPlaybackDeadline > 0f && Time.time >= skillPlaybackDeadline;
    }

    private bool IsActiveComposer(RuntimeAnimComposer composer)
    {
        if (composer == null || animCoordinator == null || !animCoordinator.isActiveAndEnabled)
            return false;

        int layer = composer.SourceAnimComposer.AnimationLayer;

        return layer >= 0 &&
               layer < animCoordinator.LayersAndActiveAnimComposers.Count &&
               animCoordinator.LayersAndActiveAnimComposers[layer].ActiveComposers.Contains(composer);
    }

    public void PlayAttack(int attackIndex = 1)
    {
        SetAnimatorPlaybackSpeed(SkillAnimationSpeed);
        SetTrigger(Animator.StringToHash($"Attack{Mathf.Clamp(attackIndex, 1, 10)}"));
    }

    public void PlayHit()
    {
        ScriptableObject_AnimComposer hitComposer = GetHitComposer();

        if (hitComposer != null &&
            animator != null &&
            animator.avatar != null &&
            animator.avatar.isHuman &&
            PlayComposer(hitComposer, hitComposer.PlayRate * hitReactionAnimationSpeed))
        {
            return;
        }

        SetAnimatorPlaybackSpeed(hitReactionAnimationSpeed);
        SetTrigger(GotHitHash);
    }

    public void PlayDeath()
    {
        SetAnimatorPlaybackSpeed(battleAnimationSpeed);
        SetTrigger(DeathHash);

        if (deathPoseCoroutine != null)
            StopCoroutine(deathPoseCoroutine);

        deathPoseCoroutine = StartCoroutine(HoldDeathPose());
    }

    public void SetDeadState(bool isDead)
    {
        if (deathPoseCoroutine != null)
        {
            StopCoroutine(deathPoseCoroutine);
            deathPoseCoroutine = null;
        }

        SetAnimatorPlaybackSpeed(battleAnimationSpeed);

        if (isDead)
        {
            PlayDeath();
            return;
        }

        if (animator == null)
            return;

        bool coordinatorReady = animCoordinator != null &&
                                animCoordinator.LocomotionPlayable.IsValid();
        bool hasLocomotionState = coordinatorReady
            ? animCoordinator.LocomotionPlayable.HasState(0, LocomotionStateHash)
            : animator.HasState(0, LocomotionStateHash);

        if (!hasLocomotionState)
            return;

        if (coordinatorReady)
            animCoordinator.LocomotionPlayable.Play(LocomotionStateHash, 0, 0f);
        else
            animator.Play(LocomotionStateHash, 0, 0f);
    }

    public void StopMovementAnimation()
    {
        IsMoving = false;
        SetLocomotion(0f);
    }

    private void ConfigureAnimator(
        RuntimeAnimatorController humanoidController,
        bool overrideHumanoidController)
    {
        animatorParameters.Clear();
        animCoordinator = null;

        if (animator == null)
            return;

        animCoordinator = animator.GetComponent<AnimCoordinatorComponent>();

        if (overrideHumanoidController &&
            humanoidController != null &&
            animator.avatar != null &&
            animator.avatar.isValid &&
            animator.avatar.isHuman)
        {
            if (animCoordinator != null)
            {
                if (animCoordinator.LocomotionController != humanoidController)
                    animCoordinator.SetLocomotionAnimatorController(humanoidController);
            }
            else
            {
                animator.runtimeAnimatorController = humanoidController;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        animator.applyRootMotion = false;
        SetAnimatorPlaybackSpeed(battleAnimationSpeed);

        if (animCoordinator != null)
        {
            animCoordinator.SetRootMotionBlocked(true);
            var locomotion = animCoordinator.LocomotionPlayable;
            for (int i = 0; i < locomotion.GetParameterCount(); i++)
                animatorParameters.Add(locomotion.GetParameter(i).nameHash);
        }
        else
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                animatorParameters.Add(parameter.nameHash);
        }
    }

    private void SetTrigger(int parameterHash)
    {
        StopSkill();

        if (animator != null && animatorParameters.Contains(parameterHash))
        {
            if (animCoordinator != null)
                animCoordinator.LocomotionPlayable.SetTrigger(parameterHash);
            else
                animator.SetTrigger(parameterHash);
        }
    }

    private IEnumerator HoldDeathPose()
    {
        const float stateTimeout = 2f;
        float elapsed = 0f;
        bool enteredDeath = false;

        while (elapsed < stateTimeout)
        {
            AnimatorStateInfo state = animCoordinator != null &&
                                      animCoordinator.LocomotionPlayable.IsValid()
                ? animCoordinator.LocomotionPlayable.GetCurrentAnimatorStateInfo(0)
                : animator.GetCurrentAnimatorStateInfo(0);

            if (state.shortNameHash == DeathHash)
            {
                enteredDeath = true;

                if (state.normalizedTime >= 0.98f)
                    break;
            }
            else if (enteredDeath)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (enteredDeath)
            SetAnimatorPlaybackSpeed(0f);

        deathPoseCoroutine = null;
    }

    private void SetAnimatorPlaybackSpeed(float speed)
    {
        if (animator == null)
            return;

        if (animCoordinator != null && animCoordinator.LocomotionPlayable.IsValid())
        {
            animator.speed = 1f;
            animCoordinator.LocomotionPlayable.SetSpeed(speed);
        }
        else
            animator.speed = speed;
    }

    private ScriptableObject_AnimComposer GetHitComposer()
    {
        CharacterManager manager = GetComponentInParent<CharacterManager>();
        bool useFemaleComposer = manager != null &&
                                 manager.character != null &&
                                 manager.character.customizationData != null &&
                                 !manager.character.customizationData.IsMale;

        if (useFemaleComposer)
        {
            if (femaleHitComposer == null)
                femaleHitComposer = Resources.Load<ScriptableObject_AnimComposer>(FemaleHitComposerPath);

            return femaleHitComposer;
        }

        if (maleHitComposer == null)
            maleHitComposer = Resources.Load<ScriptableObject_AnimComposer>(MaleHitComposerPath);

        return maleHitComposer;
    }

    private float GetSkillPlaybackRate(
        SkillDefinitionSO skill,
        ScriptableObject_AnimComposer composer)
    {
        AnimationClip clip = composer != null ? composer.AnimationClip : null;

        if (clip == null)
            return 0f;

        float playbackRate = composer.PlayRate * SkillAnimationSpeed;

        if (skill == null ||
            skill.isCounterSkill ||
            skill.counterActionType != CounterActionType.None)
        {
            return playbackRate;
        }

        float durationScale = Mathf.Clamp(2f - skill.activationSpeed, 0.25f, 2f);

        if (!IsSingleHitMeleeAttack(skill))
            return playbackRate / durationScale;

        float targetDuration = standardSingleHitClipDuration /
                               Mathf.Max(0.01f, SkillAnimationSpeed) *
                               durationScale;

        return clip.length / Mathf.Max(0.01f, targetDuration) * composer.PlayRate;
    }

    private static bool IsSingleHitMeleeAttack(SkillDefinitionSO skill)
    {
        if (skill == null ||
            skill.isCounterSkill ||
            skill.counterActionType != CounterActionType.None ||
            skill.isRangedSkill ||
            skill.type != SkillType.Physical ||
            skill.damageComponents == null)
        {
            return false;
        }

        int hitCount = 0;

        foreach (SkillDamageComponentData component in skill.damageComponents)
        {
            if (component != null)
                hitCount += Mathf.Max(1, component.hitCount);
        }

        return hitCount == 1;
    }

    private static Animator FindPresentationAnimator(Transform visualRoot)
    {
        if (visualRoot == null)
            return null;

        Animator[] animators = visualRoot.GetComponentsInChildren<Animator>(true);

        foreach (Animator candidate in animators)
        {
            if (candidate.gameObject.activeInHierarchy && HasParameter(candidate, LocomotionHash))
                return candidate;
        }

        foreach (Animator candidate in animators)
        {
            if (candidate.gameObject.activeInHierarchy)
                return candidate;
        }

        return animators.Length > 0 ? animators[0] : null;
    }

    private static bool HasParameter(Animator candidate, int parameterHash)
    {
        if (candidate == null)
            return false;

        AnimCoordinatorComponent coordinator = candidate.GetComponent<AnimCoordinatorComponent>();
        if (coordinator != null && coordinator.LocomotionPlayable.IsValid())
        {
            var locomotion = coordinator.LocomotionPlayable;
            for (int i = 0; i < locomotion.GetParameterCount(); i++)
            {
                if (locomotion.GetParameter(i).nameHash == parameterHash)
                    return true;
            }

            return false;
        }

        if (candidate.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter parameter in candidate.parameters)
        {
            if (parameter.nameHash == parameterHash)
                return true;
        }

        return false;
    }
}
