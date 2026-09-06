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

    private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
    private static readonly int LocomotionStateHash = Animator.StringToHash("Base Layer.Blend Tree");
    private static readonly int GotHitHash = Animator.StringToHash("GotHit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int JumpBackHash = Animator.StringToHash("Jump2");

    public bool IsMoving { get; private set; }
    public Vector3 HomePosition => homePosition;
    public Animator Animator => animator;
    public float SkillAnimationSpeed => battleAnimationSpeed;
    public bool IsPlayingSkill => isPlayingSkillSequence || IsActiveComposer(activeSkillComposer);

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
        ScriptableObject_AnimComposer composer = skill != null ? skill.composer : null;

        StopSkill();

        if (composer == null)
        {
            PlayAttack();
            return;
        }

        if (skill.followUpComposers != null && skill.followUpComposers.Count > 0)
        {
            isPlayingSkillSequence = true;
            isSkillImpactReady = false;
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
    }

    private IEnumerator PlayComposerSequence(SkillDefinitionSO skill)
    {
        List<ScriptableObject_AnimComposer> sequence = new List<ScriptableObject_AnimComposer>
        {
            skill.composer
        };
        sequence.AddRange(skill.followUpComposers);

        for (int i = 0; i < sequence.Count; i++)
        {
            ScriptableObject_AnimComposer composer = sequence[i];

            if (composer == null || !PlayComposer(composer, GetSkillPlaybackRate(skill, composer)))
                continue;

            bool isLastComposer = i == sequence.Count - 1;

            if (isLastComposer)
                isSkillImpactReady = true;

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
                    yield break;
                }

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
        SetAnimatorPlaybackSpeed(battleAnimationSpeed);
        SetTrigger(Animator.StringToHash($"Attack{Mathf.Clamp(attackIndex, 1, 10)}"));
    }

    public void PlayHit()
    {
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

    private float GetSkillPlaybackRate(
        SkillDefinitionSO skill,
        ScriptableObject_AnimComposer composer)
    {
        AnimationClip clip = composer != null ? composer.AnimationClip : null;

        if (clip == null)
            return 0f;

        float playbackRate = composer.PlayRate * battleAnimationSpeed;

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
                               Mathf.Max(0.01f, battleAnimationSpeed) *
                               durationScale;

        return clip.length / Mathf.Max(0.01f, targetDuration);
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
