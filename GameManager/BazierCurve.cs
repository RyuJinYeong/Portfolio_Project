using UnityEngine;

public class BezierCurve : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public int segmentCount = 20;

    private Vector3[] points = new Vector3[3];

    public void Initialize(Vector3 startPoint, Vector3 endPoint)
    {
        points[0] = startPoint;
        points[1] = (startPoint + endPoint) / 2 + Vector3.up * 2; // 임의의 컨트롤 포인트
        points[2] = endPoint;

        points[0].y += 1;
        points[2].y += 1;

        DrawCurve();
    }

    private void DrawCurve()
    {
        lineRenderer.positionCount = segmentCount + 1;
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 point = CalculateBezierPoint(t, points[0], points[1], points[2]);
            lineRenderer.SetPosition(i, point);
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        return u * u * p0 + 2 * u * t * p1 + t * t * p2;
    }
}
