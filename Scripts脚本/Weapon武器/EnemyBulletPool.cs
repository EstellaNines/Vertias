using UnityEngine;
using System.Collections.Generic;

public class EnemyBulletPool : MonoBehaviour // 敌人子弹对象池管理类
{
    [SerializeField] private GameObject bulletPrefab; // 子弹预制体模板
    [SerializeField] private int initialPoolSize = 20; // 初始池容量
    [SerializeField] private int maxPoolSize = 100; // 最大池容量
    [SerializeField] private bool expandable = true; // 是否允许扩容

    private Queue<GameObject> bulletPool = new Queue<GameObject>(); // 使用队列实现对象池

    void Awake() // 初始化方法
    {
        InitializePool(); // 调用初始化方法
    }

    // 初始化对象池
    private void InitializePool()
    {
        // 创建初始数量的子弹对象
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBullet(); // 创建单个子弹
        }
    }

    // 创建新子弹
    private void CreateNewBullet()
    {
        // 达到上限时阻止创建
        if (bulletPool.Count >= maxPoolSize)
        {
            Debug.LogWarning("敌人子弹池已满"); // 容量警告
            return;
        }

        // 实例化子弹并初始化
        GameObject bullet = Instantiate(bulletPrefab, transform);
        bullet.SetActive(false); // 设置为非激活状态
        bulletPool.Enqueue(bullet); // 加入对象池队列
    }

    // 获取可用子弹
    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        // 池中有可用子弹时
        if (bulletPool.Count > 0)
        {
            GameObject bullet = bulletPool.Dequeue(); // 从队列取出
            bullet.transform.position = position; // 设置生成位置
            bullet.transform.rotation = rotation; // 设置旋转角度
            bullet.SetActive(true); // 激活对象
            return bullet; // 返回可用子弹
        }

        // 可扩容时创建新子弹
        if (expandable)
        {
            CreateNewBullet(); // 创建新子弹
            return GetBullet(position, rotation); // 递归获取
        }

        return null; // 无可用子弹时返回null
    }

    // 回收子弹到池中
    public void ReturnBullet(GameObject bullet)
    {
        bullet.transform.SetParent(transform); // 重置父对象
        bullet.SetActive(false); // 设置为非激活状态
        bulletPool.Enqueue(bullet); // 放回对象池队列
    }

    private void Update() // 每帧更新
    {
        // 遍历所有池中子弹
        foreach (GameObject bullet in bulletPool)
        {
            if (bullet.activeInHierarchy) // 仅处理激活状态的子弹
            {
                // 检测是否超出屏幕范围
                if (IsBulletOffScreen(bullet))
                {
                    ReturnBullet(bullet); // 回收屏幕外的子弹
                }
            }
        }
    }

    // 检测子弹是否超出屏幕
    private bool IsBulletOffScreen(GameObject bullet)
    {
        // 将世界坐标转换为屏幕坐标系
        Vector3 screenPos = Camera.main.WorldToViewportPoint(bullet.transform.position);
        // 判断是否超出屏幕边界（含0.1容差）
        return screenPos.x < -0.1f || screenPos.x > 1.1f ||
               screenPos.y < -0.1f || screenPos.y > 1.1f;
    }
}