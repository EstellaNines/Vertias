// 导入必要的命名空间
using UnityEngine;
using UnityEngine.InputSystem;

// 定义PlayerAim类，继承自MonoBehaviour
public class PlayerAim : MonoBehaviour
{
    // 公有Transform变量，用于指向玩家控制的瞄准物体
    public Transform hand;

    // Update每帧调用一次，处理实时逻辑
    void Update()
    {
        // 获取鼠标屏幕坐标并转换为世界坐标（z轴默认为相机距离）
        var pos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // 计算从手部位置到鼠标位置的方向向量并归一化，设置为手部的右侧方向
        hand.right = (Vector2)(pos - hand.position).normalized;

    }
}