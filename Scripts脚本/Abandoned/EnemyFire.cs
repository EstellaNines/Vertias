using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFire : MonoBehaviour
{
    [Header("子弹池引用")]
    public EnemyBulletPool enemyBulletPool; // 引用敌人子弹池

    [Header("发射配置")]
    [SerializeField] private Transform muzzle; // 通过Inspector挂载的发射点
    [SerializeField] private Transform weapon; // 武器子对象引用（用于独立瞄准）
    public float fireRate = 0.5f; // 射击间隔时间
    private float nextFireTime; // 下次射击时间

    void Awake()
    {
        if (muzzle == null)
        {
            Debug.LogError("请在Inspector中挂载Muzzle发射点");
        }
    }

    void Update()
    {
        // 持续瞄准玩家
        AimAtPlayer();

        // 检查射击冷却时间
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    /// <summary>
    /// 独立瞄准逻辑，确保武器始终朝向玩家位置
    /// </summary>
    void AimAtPlayer()
    {
        RaycastFOV fov = GetComponentInParent<RaycastFOV>();
        if (fov == null || weapon == null || !fov.IsPlayerDetected())
        {
            return;
        }

        Vector2 playerPosition = fov.GetPlayerPosition();
        Vector2 aimDirection = playerPosition - (Vector2)weapon.position;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        // 仅调整Z轴旋转
        weapon.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void Shoot()
    {
        RaycastFOV fov = GetComponentInParent<RaycastFOV>();
        if (fov == null)
        {
            Debug.LogWarning("未找到RaycastFOV组件");
            return;
        }

        bool playerDetected = fov.IsPlayerDetected();
        PlayerCrouch playerCrouch = fov.GetPlayerCrouch();

        // 仅当玩家被检测到且未潜行时射击
        if (!playerDetected || (playerCrouch != null && playerCrouch.IsCrouching()))
        {
            return;
        }

        Vector2 playerPosition = fov.GetPlayerPosition();
        Vector2 shootDirection = playerPosition - (Vector2)muzzle.position;
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;

        // 创建子弹并设置旋转
        Quaternion bulletRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        GameObject bullet = enemyBulletPool.GetBullet(muzzle.position, bulletRotation);

        if (bullet != null)
        {
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.enabled = true;
                bulletComponent.shooter = this.transform;
                Debug.Log($"[敌人开火] 发射者: {gameObject.name}");
            }
            else
            {
                Debug.LogWarning("子弹缺少Bullet组件");
            }
        }
    }
}