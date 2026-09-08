using System;
using Jorjouto.AnimComposerSystem;
using UnityEngine;

[Serializable]
[ActionSubGroup("Battle")]
[BlockColor("#8B5CF6")]
public class ActionBlock_BattleProjectileVfx : ActionBlock_Base
{
    public GameObject castVfxPrefab;
    public GameObject projectileModelPrefab;
    public GameObject projectileVfxPrefab;
    public GameObject impactVfxPrefab;
    public bool useOffHand;
    public bool skipCounterSkills = true;
    public Vector3 spawnOffset;
    public Vector3 targetOffset;
    public Vector3 projectileModelScale = Vector3.one;
    public Vector3 projectileModelRotation;
    public Vector3 vfxScale = Vector3.one;
    public Vector3 vfxRotation;
    [Min(0f)] public float arcHeight;
    public bool tumbleModel;
    [Min(0f)] public float effectLifetime = 4f;
    [Range(0f, 1f)] public float embeddedSoundVolume = 0.35f;
    [Range(0f, 1f)] public float embeddedSoundSpatialBlend = 0.85f;

    private BattlePresentationHandler presentation;
    private Transform target;
    private GameObject projectileRoot;
    private Transform projectileModel;
    private Vector3 startPosition;
    private float elapsed;
    private bool impactSpawned;

    public override void OnStart(
        GameObject owner,
        float startTime,
        float endTime,
        float rate,
        float startOffset)
    {
        base.OnStart(owner, startTime, endTime, rate, startOffset);

        presentation = owner != null
            ? owner.GetComponentInParent<BattlePresentationHandler>()
            : null;

        if (presentation == null ||
            (skipCounterSkills && presentation.CurrentSkill != null &&
             presentation.CurrentSkill.isCounterSkill))
        {
            return;
        }

        target = presentation.CurrentSkillTarget;
        impactSpawned = false;
        elapsed = startOffset / Mathf.Max(0.01f, Mathf.Abs(rate));

        Transform origin = ResolveOrigin(owner, useOffHand);
        startPosition = origin != null
            ? origin.TransformPoint(spawnOffset)
            : owner.transform.TransformPoint(spawnOffset);

        if (castVfxPrefab != null)
            SpawnEffect(castVfxPrefab, startPosition, owner.transform.rotation, vfxScale);

        if ((projectileModelPrefab == null && projectileVfxPrefab == null) || target == null)
            return;

        projectileRoot = new GameObject("Battle Projectile VFX");
        projectileRoot.transform.position = startPosition;

        if (projectileModelPrefab != null)
        {
            GameObject model = UnityEngine.Object.Instantiate(
                projectileModelPrefab,
                projectileRoot.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(projectileModelRotation);
            model.transform.localScale = projectileModelScale;
            projectileModel = model.transform;
        }

        if (projectileVfxPrefab != null)
        {
            GameObject vfx = UnityEngine.Object.Instantiate(
                projectileVfxPrefab,
                projectileRoot.transform);
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localRotation = Quaternion.Euler(vfxRotation);
            vfx.transform.localScale = vfxScale;
            PrepareEffect(vfx);
        }

        UpdateProjectilePosition();
    }

    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (projectileRoot == null || target == null)
            return;

        elapsed += deltaTime;
        UpdateProjectilePosition();

        if (tumbleModel && projectileModel != null)
            projectileModel.Rotate(Vector3.right, 720f * deltaTime, Space.Self);
    }

    public override void OnFinish()
    {
        CompleteProjectile();
    }

    public override void OnExit()
    {
        if (projectileRoot != null)
            UnityEngine.Object.Destroy(projectileRoot);

        projectileRoot = null;
        projectileModel = null;
        target = null;
        presentation = null;
        base.OnExit();
    }

    public override bool CheckCanStartActionBlock()
    {
        return Owner != null &&
               (castVfxPrefab != null ||
                projectileModelPrefab != null ||
                projectileVfxPrefab != null ||
                impactVfxPrefab != null);
    }

    private void UpdateProjectilePosition()
    {
        if (projectileRoot == null || target == null)
            return;

        Vector3 endPosition = ResolveTargetPosition(target) + targetOffset;
        float progress = duration > 0f
            ? Mathf.Clamp01(elapsed / duration)
            : 1f;
        Vector3 position = Vector3.Lerp(startPosition, endPosition, progress);
        position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
        projectileRoot.transform.position = position;

        Vector3 direction = endPosition - position;
        if (direction.sqrMagnitude > 0.0001f)
            projectileRoot.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void CompleteProjectile()
    {
        if (impactSpawned)
            return;

        impactSpawned = true;

        if (target != null && impactVfxPrefab != null)
        {
            Vector3 impactPosition = ResolveTargetPosition(target) + targetOffset;
            Vector3 direction = impactPosition - startPosition;
            Quaternion rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
            SpawnEffect(impactVfxPrefab, impactPosition, rotation, vfxScale);
        }

        if (projectileRoot != null)
            UnityEngine.Object.Destroy(projectileRoot);

        projectileRoot = null;
        projectileModel = null;
    }

    private GameObject SpawnEffect(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale)
    {
        GameObject effect = UnityEngine.Object.Instantiate(prefab, position, rotation);
        effect.transform.localScale = scale;
        PrepareEffect(effect);
        UnityEngine.Object.Destroy(effect, Mathf.Max(0.1f, effectLifetime));
        return effect;
    }

    private void PrepareEffect(GameObject effect)
    {
        if (effect == null)
            return;

        foreach (MonoBehaviour behaviour in effect.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null && behaviour.GetType().Name == "MagicAttacks_Projectile")
                behaviour.enabled = false;
        }

        foreach (AudioSource source in effect.GetComponentsInChildren<AudioSource>(true))
        {
            source.volume *= embeddedSoundVolume;
            source.spatialBlend = embeddedSoundSpatialBlend;
            source.dopplerLevel = 0f;
        }
    }

    private static Transform ResolveOrigin(GameObject owner, bool offHand)
    {
        Animator animator = owner != null ? owner.GetComponent<Animator>() : null;
        if (animator == null && owner != null)
            animator = owner.GetComponentInChildren<Animator>();

        if (animator != null && animator.isHuman)
        {
            Transform hand = animator.GetBoneTransform(
                offHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            if (hand != null)
                return hand;
        }

        return owner != null ? owner.transform : null;
    }

    private static Vector3 ResolveTargetPosition(Transform targetTransform)
    {
        if (targetTransform == null)
            return Vector3.zero;

        Animator animator = targetTransform.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest != null)
                return chest.position;
        }

        CharacterController controller = targetTransform.GetComponentInChildren<CharacterController>();
        if (controller != null)
            return controller.bounds.center;

        Collider collider = targetTransform.GetComponentInChildren<Collider>();
        return collider != null
            ? collider.bounds.center
            : targetTransform.position + Vector3.up;
    }
}
