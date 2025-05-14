using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class NPCVisionCone : MonoBehaviour
{
    public float viewRadius = 6f;
    public float viewAngle = 90f;
    public int rayCount = 50;

    public LayerMask solidObjectLayer;

    private Mesh mesh;
    private Vector3 origin;
    private float angle;

    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        origin = Vector3.zero;
    }

    void LateUpdate()
    {
        DrawVisionCone();
    }

    public void SetOrigin(Vector3 origin)
    {
        this.origin = origin;
    }

    public void SetAimDirection(Vector3 dir)
    {
        angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        angle -= viewAngle / 2f;
    }

    void DrawVisionCone()
    {
        float angleIncrement = viewAngle / rayCount;
        List<Vector3> vertices = new List<Vector3> { Vector3.zero };
        List<int> triangles = new List<int>();

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = angle + i * angleIncrement;
            Vector3 dir = DirFromAngle(currentAngle);
            Vector3 endPoint = origin + dir * viewRadius;

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, viewRadius, solidObjectLayer);
            if (hit.collider != null)
            {
                endPoint = hit.point;
            }

            vertices.Add(transform.InverseTransformPoint(endPoint));

            if (i > 0)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
    }

    Vector3 DirFromAngle(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad));
    }

}
