using UnityEngine;

public class RaycastFOV : MonoBehaviour
{
    public float viewRadius = 5f;
    public int viewAngleStep = 20;
    [Range(0, 360)]
    public float viewAngle = 270f;
    public LayerMask targetLayer;
    public GameObject eyePoint; // 参考点对象

    void Update()
    {
        DrawFieldOfView();
    }

    void DrawFieldOfView()
    {
        // 获取射线起点（优先使用指定眼点，否则使用自身位置）
        Vector2 rayOrigin = eyePoint ? (Vector2)eyePoint.transform.position : (Vector2)transform.position;

        // 计算初始方向向量
        Vector2 forward_left = Quaternion.AngleAxis(-(viewAngle / 2f), Vector3.forward) * Vector2.right * viewRadius;

        for (int i = 0; i <= viewAngleStep; i++)
        {
            // 旋转计算方向向量
            Vector2 v = Quaternion.AngleAxis((viewAngle / viewAngleStep) * i, Vector3.forward) * forward_left;
            Vector2 pos = rayOrigin + v;

            // 绘制调试线
            Debug.DrawLine(rayOrigin, pos, Color.red);

            // 执行2D射线检测
            RaycastHit2D hitInfo = Physics2D.Raycast(rayOrigin, v, viewRadius, targetLayer);
            if (hitInfo.collider != null)
            {
                Debug.Log("视野内检测到目标");
                // 触发自定义逻辑
            }
        }
    }
}