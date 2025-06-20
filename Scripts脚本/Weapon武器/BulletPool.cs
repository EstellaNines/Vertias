using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    // 子弹池容器
    private Queue<GameObject> bulletPool = new Queue<GameObject>();

    // 子弹预制体
    public GameObject bulletPrefab;

    // 池的初始大小
    public int initialPoolSize = 20;

    // 池的最大大小
    public int maxPoolSize = 100;

    // 在脚本实例化时调用
    private void Awake()
    {
        // 初始化池
        InitializePool();
    }

    // 初始化池
    private void InitializePool()
    {
        // 实例化初始子弹并放入池中
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateBullet();
        }
    }

    // 创建并返回一个子弹实例
    private void CreateBullet()
    {
        // 如果池中的子弹数量已经达到最大值，直接返回
        if (bulletPool.Count >= maxPoolSize)
        {
            Debug.LogWarning("塞满了塞满了");
            return;
        }

        // 实例化一个子弹
        GameObject bulletInstance = Instantiate(bulletPrefab);
        bulletInstance.SetActive(false);
        bulletInstance.transform.SetParent(transform);

        // 将新创建的子弹加入池
        bulletPool.Enqueue(bulletInstance);
    }

    // 从池中获取一个子弹
    public GameObject GetBullet()
    {
        // 如果池中有可用子弹，则返回
        if (bulletPool.Count > 0)
        {
            GameObject bullet = bulletPool.Dequeue();
            bullet.SetActive(true);
            return bullet;
        }

        // 如果池中没有可用子弹，直接返回 null
        Debug.LogWarning("一滴也没有");
        return null;
    }

    // 将子弹返回到池中
    public void ReturnBullet(GameObject bullet)
    {
        bullet.transform.SetParent(transform);
        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
    }
}