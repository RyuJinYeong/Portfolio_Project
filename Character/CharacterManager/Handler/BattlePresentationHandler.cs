using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlePresentationHandler : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 720f;

    private readonly HashSet<int> animatorParameters = new();
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private bool hasHomePose;

    private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
    private static readonly int GotHitHash = Animator.StringToHash("GotHit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int JumpBackHash = Animator.StringToHash("Jump2");

    public bool IsMoving { get; private set; }
    public Vector3 HomePosition => homePosition;
    public Animator Animator => animator;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        ConfigureAnimator(null, false);
        CaptureHomePose();
    }

    public void BindVisual(
        Transform visualRoot,
        RuntimeAnimatorController humanoidController = null,
        bool overrideHumanoidController = false)
    {
        Animator visualAnimator = FindPresentationAnimator(visualRoot);

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

        Vector3 flatDirection = destination - transform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.0001f)
            SetLocomotion(2f);

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
            animator.SetFloat(LocomotionHash, value);
    }

    public void PlayAttack(int attackIndex = 1)
    {
        SetTrigger(Animator.StringToHash($"Attack{Mathf.Clamp(attackIndex, 1, 10)}"));
    }

    public void PlayHit()
    {
        SetTrigger(GotHitHash);
    }

    public void PlayDeath()
    {
        SetTrigger(DeathHash);
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

        if (animator == null)
            return;

        if (overrideHumanoidController &&
            humanoidController != null &&
            animator.avatar != null &&
            animator.avatar.isValid &&
            animator.avatar.isHuman)
        {
            animator.runtimeAnimatorController = humanoidController;
            animator.Rebind();
            animator.Update(0f);
        }

        animator.applyRootMotion = false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            animatorParameters.Add(parameter.nameHash);
    }

    private void SetTrigger(int parameterHash)
    {
        if (animator != null && animatorParameters.Contains(parameterHash))
            animator.SetTrigger(parameterHash);
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
        if (candidate == null || candidate.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter parameter in candidate.parameters)
        {
            if (parameter.nameHash == parameterHash)
                return true;
        }

        return false;
    }
}
