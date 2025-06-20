using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFire : MonoBehaviour
{
    [Header("子弹池引用")]
    public EnemyBulletPool enemyBulletPool; // 引用敌人子弹池

    [Header("发射配置")]
    [SerializeField] private Transform muzzle; // 通过Inspector挂载的发射点
    public float fireRate = 0.5f; // 射击间隔时间
    private float nextFireTime; // 下次射击时间

    [Header("检测范围")]
    [Range(0, 20)] public float detectRange = 5f; // 检测半径
    [SerializeField] private LayerMask playerLayerMask; // 玩家层掩码
    private bool isPlayerInRange = false; // 玩家是否在范围内

    void Awake()
    {
        if (muzzle == null)
        {
            Debug.LogError("请在Inspector中挂载Muzzle发射点");
        }

        if (playerLayerMask == LayerMask.GetMask("Ignore Raycast"))
        {
            Debug.LogWarning("检测层未设置，建议设置为Player层");
        }
    }

    void Update()
    {
        // 实时检测玩家是否在范围内
        DetectPlayer();

        // 检查射击冷却时间并判断范围条件
        if (Time.time >= nextFireTime && isPlayerInRange)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void DetectPlayer()
    {
        // 使用圆形检测（2D场景）
        Collider2D player = Physics2D.OverlapCircle(
            transform.position,
            detectRange,
            playerLayerMask
        );

        isPlayerInRange = player != null;

        // 可视化检测范围（仅在Scene视图显示）
        DebugExtension.DebugCircle(
            transform.position,
            Color.yellow,
            detectRange,
            false,
            0.1f
        );
    }

    void Shoot()
    {
        GameObject bullet = enemyBulletPool.GetBullet(muzzle.position, muzzle.rotation);

        if (bullet != null)
        {
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.enabled = true;
                // 设置子弹发射者为敌人
                bulletComponent.shooter = this.transform;
                Debug.Log($"[敌人开火] 发射者: {gameObject.name}");
            }
            else
            {
                Debug.LogWarning("子弹缺少Bullet组件");
            }
        }
    }

    // 可视化扩展方法（仅在Scene视图显示）
    private static class DebugExtension
    {
        public static void DebugCircle(Vector3 position, Color color, float radius, bool filled = false, float duration = 0f)
        {
            int segments = 36;
            Vector3[] points = new Vector3[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                points[i] = position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            }

            for (int i = 0; i < segments; i++)
            {
                Debug.DrawLine(points[i], points[i + 1], color, duration);
                if (filled && i < segments - 1)
                {
                    Debug.DrawLine(position, points[i], color * 0.5f, duration);
                }
            }
        }
    }
}