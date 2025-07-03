using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOV : MonoBehaviour
{
    [Header("敌人引用")]
    [FieldLabel("敌人组件")] public Enemy enemyComponent; // 敌人组件引用
    
    [Header("视野设置")]
    [FieldLabel("视野角度")] public float fov = 90f; // 视野角度
    [FieldLabel("射线数量")] public int rayCount = 50; // 射线数量
    [FieldLabel("视野距离")] public float viewDistance = 10f; // 视野距离
    [FieldLabel("障碍物层")] public LayerMask obstacleLayer = -1; // 障碍物层
    
    [Header("渲染设置")]
    public Material fovMaterial; // FOV材质
    
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    private void Start()
    {
        // 自动获取Enemy组件（如果没有手动设置）
        if (enemyComponent == null)
        {
            enemyComponent = GetComponent<Enemy>();
            if (enemyComponent == null)
            {
                enemyComponent = GetComponentInParent<Enemy>();
            }
        }
        
        // 从Enemy组件同步参数
        SyncParametersFromEnemy();
        
        // 确保有必要的组件
        SetupComponents();
        
        // 创建网格
        CreateFOVMesh();
    }
    
    // 从Enemy组件同步参数
    private void SyncParametersFromEnemy()
    {
        if (enemyComponent != null)
        {
            // 同步视野角度（Enemy中是半角度，FOV中是全角度）
            fov = enemyComponent.viewHalfAngle * 2f;
            
            // 同步视野距离
            viewDistance = enemyComponent.viewRadius;
            
            // 同步射线数量（使用Enemy的扇形边数）
            rayCount = enemyComponent.viewAngleStep;
            
            // 同步障碍物层
            obstacleLayer = enemyComponent.obstacleLayer;
            
            Debug.Log($"FOV参数已从Enemy同步: 视野角度={fov}, 视野距离={viewDistance}, 射线数量={rayCount}");
        }
        else
        {
            Debug.LogWarning("未找到Enemy组件，使用默认FOV参数");
        }
    }
    
    private void SetupComponents()
    {
        // 获取或添加MeshFilter组件
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        
        // 获取或添加MeshRenderer组件
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        
        // 创建默认材质（如果没有指定）
        if (fovMaterial == null)
        {
            fovMaterial = new Material(Shader.Find("Sprites/Default"));
            fovMaterial.color = new Color(1f, 1f, 0f, 0.3f); // 半透明黄色
        }
        
        meshRenderer.material = fovMaterial;
        
        // 创建网格
        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }
    
    private void CreateFOVMesh()
    {
        // 实时同步参数（可选）
        if (enemyComponent != null)
        {
            SyncParametersFromEnemy();
        }
        
        Vector3 origin = Vector3.zero; // 原点
        float angle = fov / 2f; // 从正方向开始的角度
        float angleIncrease = fov / rayCount; // 角度增量
    
        // 构建三角形点结构
        Vector3[] vertices = new Vector3[rayCount + 2]; // +2 是因为需要原点和最后一个点
        Vector2[] uv = new Vector2[vertices.Length]; // UV坐标
        int[] triangles = new int[rayCount * 3]; // 三角形索引
    
        // 设置原点
        vertices[0] = origin;
        uv[0] = Vector2.zero;
    
        int vertexIndex = 1;
        int triangleIndex = 0;
        
        // 获取射线起点（优先使用Enemy的eyePoint）
        Vector3 rayOrigin = origin;
        if (enemyComponent != null && enemyComponent.eyePoint != null)
        {
            rayOrigin = transform.InverseTransformPoint(enemyComponent.eyePoint.transform.position);
        }
        
        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 vertex;
            // 使用Enemy的DirectionFromAngle方法来获取正确的射线方向
            Vector3 rayDirection = GetDirectionFromAngle(angle);
            
            // 射线检测（从世界坐标进行）
            Vector3 worldRayOrigin = transform.TransformPoint(rayOrigin);
            RaycastHit2D raycastHit = Physics2D.Raycast(worldRayOrigin, rayDirection, viewDistance, obstacleLayer);
            
            if (raycastHit.collider == null)
            {
                // 没有碰撞，使用最大视野距离
                vertex = rayOrigin + rayDirection * viewDistance;
            }
            else
            {
                // 有碰撞，使用碰撞点（转换为本地坐标）
                vertex = transform.InverseTransformPoint(raycastHit.point);
            }
            
            vertices[vertexIndex] = vertex;
            uv[vertexIndex] = new Vector2((float)i / rayCount, 1f); // 设置UV坐标
    
            // 创建三角形（除了第一个点）
            if (i > 0)
            {
                triangles[triangleIndex + 0] = 0; // 原点
                triangles[triangleIndex + 1] = vertexIndex - 1; // 前一个顶点
                triangles[triangleIndex + 2] = vertexIndex; // 当前顶点
                triangleIndex += 3;
            }
            
            vertexIndex++;
            angle -= angleIncrease; // 角度递减
        }
    
        // 应用网格数据
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // 重新计算法线
    }
    
    // 新增方法：获取考虑Enemy方向的射线方向
    private Vector3 GetDirectionFromAngle(float angleInDegrees)
    {
        if (enemyComponent != null)
        {
            // 获取Enemy的当前方向
            Vector2 enemyDirection = enemyComponent.GetCurrentDirection();
            
            // 计算Enemy当前朝向的角度
            float currentAngle = Mathf.Atan2(enemyDirection.y, enemyDirection.x) * Mathf.Rad2Deg;
            
            // 将相对角度转换为绝对角度
            angleInDegrees += currentAngle;
        }
        
        // 返回方向向量
        float radians = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
    }
    
    // 实时更新FOV（可选）
    private void Update()
    {
        // 检查敌人是否死亡，如果死亡则禁用FOV显示
        if (enemyComponent != null && enemyComponent.isDead)
        {
            // 禁用MeshRenderer来隐藏FOV显示
            if (meshRenderer != null && meshRenderer.enabled)
            {
                meshRenderer.enabled = false;
                Debug.Log($"敌人 {enemyComponent.gameObject.name} 已死亡，FOV显示已禁用");
            }
            return; // 敌人死亡后不再更新FOV网格
        }
        
        // 确保敌人活着时FOV显示是启用的
        if (meshRenderer != null && !meshRenderer.enabled)
        {
            meshRenderer.enabled = true;
        }
        
        CreateFOVMesh(); // 实时更新网格
    }

    // 角度转换函数
    public static Vector3 GetVectorFromAngle(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
    }
    
    // 可视化调试
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
    }
    
    // 手动同步参数的公共方法
    public void ManualSyncParameters()
    {
        SyncParametersFromEnemy();
        CreateFOVMesh();
    }
}
