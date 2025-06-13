using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public LayerMask obstacleLayer; // 遮挡层mask
    [Range(10, 100)] public int rayCount = 50; // 射线数量
    [Range(1, 20)] public float viewDistance = 10f; // 视距
    [Range(10, 120)] public float fieldOfViewAngle = 90f; // 视野角度

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uv;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        InitializeMesh();
    }

    void Update()
    {
        UpdateMesh();
    }

    void InitializeMesh()
    {
        // 初始化顶点和三角形数组
        vertices = new Vector3[rayCount + 2];
        uv = new Vector2[vertices.Length];
        triangles = new int[rayCount * 3];
    }

    void UpdateMesh()
    {
        float angleIncrement = fieldOfViewAngle / rayCount;

        // 设置初始顶点（原点）
        vertices[0] = Vector3.zero;

        int vertexIndex = 1;
        int triangleIndex = 0;

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = i * angleIncrement - fieldOfViewAngle / 2;
            Vector3 direction = AngleToVector3(angle);

            // 发射射线检测障碍物
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, viewDistance, obstacleLayer);

            // 根据是否检测到障碍物确定顶点位置
            if (hit.collider != null)
            {
                vertices[vertexIndex] = hit.point - (Vector2)transform.position; ;
            }
            else
            {
                vertices[vertexIndex] = direction * viewDistance;
            }

            // 构建三角形
            if (i > 0)
            {
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;
                triangleIndex += 3;
            }

            vertexIndex++;
        }

        // 更新网格数据
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
    }

    Vector3 AngleToVector3(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
    }
}