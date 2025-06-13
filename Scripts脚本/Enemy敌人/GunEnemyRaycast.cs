using UnityEngine;

public class GunEnemyRaycast : MonoBehaviour
{
    public float fovAngle = 60f; // 检测视角角度
    public float fovRange = 10f; // 检测视角范围
    public Vector2 lookDirection = Vector2.down; // 检测视野的方向，现在假设为向下

    private bool IsTargetInsideFov(Transform target)
    {
        Vector2 directionToTarget = (target.position - transform.position).normalized; // 获取目标方向
        float angleToTarget = Vector2.Angle(lookDirection, directionToTarget); // 获取目标与视野方向的夹角
        if (angleToTarget < fovAngle / 2) //  判断目标是否在视野内
        {
            float distance = Vector2.Distance(target.position, transform.position); // 计算目标与 Player 的距离
            return distance < fovRange;
        }
        return false;
    }
}
