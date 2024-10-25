using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusCanvasManager : MonoBehaviour
{
    void Update()
    {
        // 캔버스가 항상 카메라를 바라보도록 설정
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0); // 방향이 반대로 되면 180도 회전
    }
}
