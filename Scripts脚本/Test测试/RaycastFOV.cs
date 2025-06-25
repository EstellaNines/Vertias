// ... existing code ...
using UnityEngine;

public class RaycastFOV : MonoBehaviour
{
    [FieldLabel("视角起点")] public Transform eyes; // 视觉检测起点
    [FieldLabel("视野角度")] public float lookAngle = 90f; // 视野角度
    [FieldLabel("视觉距离")] public float lookRange = 10f; // 视距范围
    [FieldLabel("射线密度")] public int rayDensity = 15; // 射线密度（建议10-20）
    private Transform chaseTarget; // 追踪目标

    void Update()
    {
        Look();
    }

    bool Look()
    {
        // 获取玩家层碰撞体
        Collider2D[] playersInArc = Physics2D.OverlapCircleAll(
            eyes.position,
            lookRange,
            LayerMask.GetMask("Player"));

        foreach (Collider2D player in playersInArc)
        {
            // 计算2D方向和角度
            Vector2 directionToTarget = player.transform.position - eyes.position;
            float angleToTarget = Vector2.Angle(eyes.right, directionToTarget);

            // 扇形角度过滤（左右各半角）
            if (angleToTarget < lookAngle / 2 &&
                directionToTarget.magnitude <= lookRange)
            {
                chaseTarget = player.transform;
                return true;
            }
        }

        // 精确扇形射线检测
        float stepAngle = lookAngle / rayDensity;
        for (int i = 0; i <= rayDensity; i++)
        {
            float currentAngle = -lookAngle / 2 + stepAngle * i;
            Quaternion rayRotation = Quaternion.Euler(0, 0, currentAngle);

            if (LookAround(rayRotation, Color.green))
            {
                return true;
            }
        }

        return false;
    }

    bool LookAround(Quaternion rotationOffset, Color debugColor)
    {
        // 2D射线方向计算（使用right轴）
        Vector2 direction = rotationOffset * eyes.right;
        Debug.DrawRay(eyes.position, (Vector3)direction.normalized * lookRange, debugColor);

        RaycastHit2D hit = Physics2D.Raycast(
            eyes.position,
            direction,
            lookRange,
            LayerMask.GetMask("Player"));

        if (hit && hit.collider.CompareTag("Player"))
        {
            chaseTarget = hit.transform;
            return true;
        }
        return false;
    }
}