using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    // 单例实例
    public static BulletPool Instance { get; private set; }
    
    // 子弹池容器 - 使用字典来存储不同预制体的池
    private Dictionary<GameObject, Queue<GameObject>> bulletPools = new Dictionary<GameObject, Queue<GameObject>>();

    // 默认子弹预制体（向后兼容）
    public GameObject bulletPrefab;

    // 池的初始大小
    public int initialPoolSize = 20;

    // 池的最大大小
    public int maxPoolSize = 100;

    // 在脚本实例化时调用
    private void Awake()
    {
        // 单例模式实现
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 如果有默认预制体，初始化默认池
            if (bulletPrefab != null)
            {
                InitializePoolForPrefab(bulletPrefab);
            }
            
            Debug.Log("玩家子弹池单例已创建并设置为跨场景持久化");
        }
        else
        {
            Debug.Log("检测到重复的玩家子弹池实例，销毁当前对象");
            Destroy(gameObject);
        }
    }

    // 为特定预制体初始化池
    private void InitializePoolForPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        
        // 如果该预制体的池已存在，跳过
        if (bulletPools.ContainsKey(prefab)) return;
        
        // 创建新的池
        Queue<GameObject> newPool = new Queue<GameObject>();
        bulletPools[prefab] = newPool;
        
        // 实例化初始子弹并放入池中
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateBulletForPrefab(prefab);
        }
        
        Debug.Log($"为预制体 {prefab.name} 初始化了子弹池，初始大小: {initialPoolSize}");
    }

    // 为特定预制体创建子弹实例
    private void CreateBulletForPrefab(GameObject prefab)
    {
        if (prefab == null || !bulletPools.ContainsKey(prefab)) return;
        
        Queue<GameObject> pool = bulletPools[prefab];
        
        // 如果池中的子弹数量已经达到最大值，直接返回
        if (pool.Count >= maxPoolSize)
        {
            Debug.LogWarning($"预制体 {prefab.name} 的子弹池已满");
            return;
        }

        // 实例化一个子弹
        GameObject bulletInstance = Instantiate(prefab);
        bulletInstance.SetActive(false);
        bulletInstance.transform.SetParent(transform);

        // 将新创建的子弹加入对应的池
        pool.Enqueue(bulletInstance);
    }

    // 从池中获取指定预制体的子弹
    public GameObject GetBullet(GameObject prefab = null)
    {
        // 如果没有指定预制体，使用默认预制体
        if (prefab == null)
        {
            prefab = bulletPrefab;
        }
        
        if (prefab == null)
        {
            Debug.LogWarning("没有指定子弹预制体且没有默认预制体");
            return null;
        }
        
        // 确保该预制体的池已初始化
        if (!bulletPools.ContainsKey(prefab))
        {
            InitializePoolForPrefab(prefab);
        }
        
        Queue<GameObject> pool = bulletPools[prefab];
        
        // 如果池中有可用子弹，则返回
        if (pool.Count > 0)
        {
            GameObject bullet = pool.Dequeue();
            bullet.SetActive(true);
            return bullet;
        }

        // 如果池中没有可用子弹，尝试创建新的
        CreateBulletForPrefab(prefab);
        
        if (pool.Count > 0)
        {
            GameObject bullet = pool.Dequeue();
            bullet.SetActive(true);
            return bullet;
        }
        
        Debug.LogWarning($"无法获取预制体 {prefab.name} 的子弹");
        return null;
    }

    // 将子弹返回到对应的池中
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;
        
        // 查找该子弹属于哪个预制体的池
        GameObject originalPrefab = FindOriginalPrefab(bullet);
        
        if (originalPrefab != null && bulletPools.ContainsKey(originalPrefab))
        {
            bullet.transform.SetParent(transform);
            bullet.SetActive(false);
            bulletPools[originalPrefab].Enqueue(bullet);
        }
        else
        {
            // 如果找不到对应的池，销毁子弹
            Debug.LogWarning($"找不到子弹 {bullet.name} 对应的池，将销毁该子弹");
            Destroy(bullet);
        }
    }
    
    // 查找子弹的原始预制体
    private GameObject FindOriginalPrefab(GameObject bullet)
    {
        string bulletName = bullet.name.Replace("(Clone)", "").Trim();
        
        foreach (var kvp in bulletPools)
        {
            if (kvp.Key.name == bulletName)
            {
                return kvp.Key;
            }
        }
        
        return null;
    }
    
    // 清理特定预制体的池
    public void ClearPoolForPrefab(GameObject prefab)
    {
        if (prefab == null || !bulletPools.ContainsKey(prefab)) return;
        
        Queue<GameObject> pool = bulletPools[prefab];
        while (pool.Count > 0)
        {
            GameObject bullet = pool.Dequeue();
            if (bullet != null)
            {
                Destroy(bullet);
            }
        }
        
        bulletPools.Remove(prefab);
        Debug.Log($"已清理预制体 {prefab.name} 的子弹池");
    }
    
    // 获取池的状态信息
    public void LogPoolStatus()
    {
        Debug.Log($"当前共有 {bulletPools.Count} 个子弹池:");
        foreach (var kvp in bulletPools)
        {
            Debug.Log($"- {kvp.Key.name}: {kvp.Value.Count} 个可用子弹");
        }
    }
}