using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFocusRig : MonoBehaviour
{
    public static CameraFocusRig Instance { get; private set; }

    public Camera cam;               // 메인 카메라
    public float moveTime = 0.6f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);

    [System.Serializable]
    public class FocusPoint { public string key; public Transform t; public float fov = 40f; }
    public List<FocusPoint> points = new();

    Vector3 _homePos; Quaternion _homeRot; float _homeFov;

    public bool isFocused = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!cam) cam = Camera.main;
        _homePos = cam.transform.position;
        _homeRot = cam.transform.rotation;
        _homeFov = cam.fieldOfView;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isFocused)
            {
                FocusHome();
                return;
            }
        }
    }

    public void Focus(string key)
    {
        var p = points.Find(x => x.key == key);
        if (p?.t == null) return;
        StopAllCoroutines();
        isFocused = true;
        StartCoroutine(FocusCo(p));
    }

    public void FocusHome()
    {
        StopAllCoroutines();
        StartCoroutine(FocusHomeCo());
    }

    IEnumerator FocusCo(FocusPoint p)
    {
        var t0 = cam.transform.position; var r0 = cam.transform.rotation; var f0 = cam.fieldOfView;
        for (float t=0; t<1f; t+=Time.deltaTime/moveTime)
        {
            float k = ease.Evaluate(t);
            cam.transform.position = Vector3.Lerp(t0, p.t.position, k);
            cam.transform.rotation = Quaternion.Slerp(r0, p.t.rotation, k);
            cam.fieldOfView = Mathf.Lerp(f0, p.fov, k);
            yield return null;
        }
        cam.transform.position = p.t.position;
        cam.transform.rotation = p.t.rotation;
        cam.fieldOfView = p.fov;
    }

    IEnumerator FocusHomeCo()
    {
        var t0 = cam.transform.position; var r0 = cam.transform.rotation; var f0 = cam.fieldOfView;
        for (float t=0; t<1f; t+=Time.deltaTime/moveTime)
        {
            float k = ease.Evaluate(t);
            cam.transform.position = Vector3.Lerp(t0, _homePos, k);
            cam.transform.rotation = Quaternion.Slerp(r0, _homeRot, k);
            cam.fieldOfView = Mathf.Lerp(f0, _homeFov, k);
            yield return null;
        }
        cam.transform.position = _homePos;
        cam.transform.rotation = _homeRot;
        cam.fieldOfView = _homeFov;

        isFocused = false;
    }
}
