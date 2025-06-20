using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPerspectiveChange : MonoBehaviour
{
    [Header("武器设置")]
    [SerializeField] private Transform weapon; // 武器引用（需在检查器中挂载）
    [SerializeField] private float flipAngleThreshold = 90f; // 翻转阈值角度

    private bool isWeaponFlipped = false; // 武器是否已翻转
    private EnemyAim enemyAim; // 敌人瞄准组件引用

    void Awake()
    {
        enemyAim = GetComponent<EnemyAim>(); // 获取敌人瞄准组件引用
    }

    void Update()
    {
        // 根据瞄准状态执行不同逻辑
        if (enemyAim != null && enemyAim.isAiming)
        {
            CheckFlipState();
        }
        else
        {
            ResetWeaponState();
        }
    }

    void CheckFlipState()
    {
        if (weapon == null) return;

        // 获取武器在本地坐标系下的旋转角度
        float weaponAngle = weapon.localEulerAngles.z;

        // 处理欧拉角的环绕问题（-180~180度）
        if (weaponAngle > 180)
        {
            weaponAngle -= 360;
        }

        // 判断是否超过阈值
        bool shouldFlip = weaponAngle > flipAngleThreshold || weaponAngle < -flipAngleThreshold;

        // 如果状态变化则执行翻转
        if (shouldFlip != isWeaponFlipped)
        {
            FlipWeapon(shouldFlip);
            isWeaponFlipped = shouldFlip;
        }
    }

    void ResetWeaponState()
    {
        if (weapon == null) return;

        // 重置武器旋转
        weapon.rotation = Quaternion.Euler(0, 0, 0);

        // 如果已经翻转则恢复
        if (isWeaponFlipped)
        {
            FlipWeapon(false);
            isWeaponFlipped = false;
        }
    }

    void FlipWeapon(bool shouldFlip)
    {
        // 仅翻转武器自身的Y轴缩放
        Vector3 scale = weapon.localScale;
        scale.y = shouldFlip ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
        weapon.localScale = scale;
    }
}
