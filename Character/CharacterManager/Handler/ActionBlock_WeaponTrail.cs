using System;
using INab.Common;
using Jorjouto.AnimComposerSystem;
using UnityEngine;

[Serializable]
[ActionSubGroup("Battle")]
[BlockColor("#79C9D8")]
public class ActionBlock_WeaponTrail : ActionBlock_Base
{
    public string trailName = "Sword_001";
    public bool useOffHand;
    [Min(0f)] public float fadeInDuration = 0.03f;
    [Min(0f)] public float fadeOutDuration = 0.05f;
    [Min(0.01f)] public float trailLifetime = 0.12f;

    private WeaponTrailEffect activeTrail;
    private bool finished;

    public override void OnStart(GameObject owner, float startTime, float endTime, float rate, float startOffset)
    {
        base.OnStart(owner, startTime, endTime, rate, startOffset);
        activeTrail = null;
        finished = false;

        BattlePresentationHandler presentation = owner.GetComponentInParent<BattlePresentationHandler>();
        if (presentation == null)
            return;

        foreach (WeaponTrailEffect trail in presentation.GetComponentsInChildren<WeaponTrailEffect>(true))
        {
            if (trail.trailName != trailName)
                continue;

            BindToEquippedWeapon(presentation, trail);

            if (!trail.isActiveAndEnabled)
                continue;

            activeTrail = trail;
            activeTrail.vfxComponent.Reinit();
            activeTrail.StartTrailWithLength(fadeInDuration / rate, trailLifetime / rate);
            break;
        }
    }

    public override void OnFinish()
    {
        finished = true;
        if (activeTrail != null && activeTrail.gameObject.activeInHierarchy)
            activeTrail.StopTrail(fadeOutDuration / rate);
    }

    public override void OnExit()
    {
        if (activeTrail != null && (!finished || !activeTrail.gameObject.activeInHierarchy))
        {
            if (activeTrail.gameObject.activeInHierarchy)
                activeTrail.StopTrail(0f);
            else
            {
                activeTrail.SetProperty_EffectActive(false);
                activeTrail.SetProperty_EffectAlive(0f);
                activeTrail.SendStopEvent();
                activeTrail.currentEffectState = WeaponTrailEffect.EffectState.Off;
            }
            activeTrail.vfxComponent.Reinit();
        }

        activeTrail = null;
        base.OnExit();
    }

    private void BindToEquippedWeapon(
        BattlePresentationHandler presentation,
        WeaponTrailEffect trail)
    {
        CharacterManager manager = presentation.GetComponentInParent<CharacterManager>();
        CharacterCustomization customization =
            presentation.GetComponentInChildren<CharacterCustomization>(true);

        if (manager == null || manager.character == null ||
            customization == null || customization.weaponRoot == null)
            return;

        bool bindOffHand = useOffHand || presentation.CurrentSkillUsesOffHand;
        EquipmentRuntimeData equipment = bindOffHand
            ? manager.character.GetSubWeaponRuntime()
            : manager.character.GetMainWeaponRuntime();

        if (equipment == null || string.IsNullOrEmpty(equipment.visualKey))
            return;

        Transform weaponVisual = FindChildRecursive(
            customization.weaponRoot,
            equipment.visualKey);

        if (weaponVisual == null || !weaponVisual.gameObject.activeInHierarchy)
            return;

        if (trail.transform.parent == weaponVisual)
            return;

        trail.transform.SetParent(weaponVisual, false);
        trail.transform.localPosition = Vector3.zero;
        trail.transform.localRotation = Quaternion.identity;
        trail.transform.localScale = Vector3.one;
        trail.weaponMountTransform = weaponVisual;
        FitTrailToWeapon(trail, weaponVisual);
        trail.gameObject.SetActive(true);
    }

    private Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void FitTrailToWeapon(WeaponTrailEffect trail, Transform weaponVisual)
    {
        Renderer[] renderers = weaponVisual.GetComponentsInChildren<Renderer>(false);
        Vector3 minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer.transform.IsChildOf(trail.transform))
                continue;

            Bounds bounds = renderer.bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 worldPoint = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 localPoint = weaponVisual.InverseTransformPoint(worldPoint);
                        minimum = Vector3.Min(minimum, localPoint);
                        maximum = Vector3.Max(maximum, localPoint);
                        hasBounds = true;
                    }
                }
            }
        }

        if (!hasBounds || trail.lineBottomTransform == null || trail.lineTipTransform == null)
            return;

        Vector3 size = maximum - minimum;
        int axis = size.y > size.x ? 1 : 0;
        if (size.z > size[axis])
            axis = 2;

        float farEnd = Mathf.Abs(maximum[axis]) >= Mathf.Abs(minimum[axis])
            ? maximum[axis]
            : minimum[axis];

        if (Mathf.Abs(farEnd) < 0.01f)
            return;

        Vector3 center = (minimum + maximum) * 0.5f;
        Vector3 bottom = center;
        Vector3 tip = center;
        bottom[axis] = farEnd * 0.2f;
        tip[axis] = farEnd * 0.95f;

        trail.lineBottomTransform.position = weaponVisual.TransformPoint(bottom);
        trail.lineTipTransform.position = weaponVisual.TransformPoint(tip);
    }
}
