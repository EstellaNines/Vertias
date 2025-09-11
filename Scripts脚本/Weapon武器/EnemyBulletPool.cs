using UnityEngine;
using System.Collections.Generic;

public class EnemyBulletPool : MonoBehaviour // 敌人子弹对象池管理类
{
    // 单例实例
    public static EnemyBulletPool Instance { get; private set; }
    
    // 子弹池容器 - 使用字典来存储不同预制体的池
    private Dictionary<GameObject, Queue<GameObject>> bulletPools = new Dictionary<GameObject, Queue<GameObject>>();
    
    // 默认子弹预制体（向后兼容）
    [SerializeField] private GameObject bulletPrefab; // 子弹预制体模板
    [SerializeField] private int initialPoolSize = 20; // 初始池容量
    [SerializeField] private int maxPoolSize = 100; // 最大池容量
    [SerializeField] private bool expandable = true; // 是否允许扩容

    void Awake() // 初始化方法
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
            
            Debug.Log("敌人子弹池单例已创建并设置为跨场景持久化");
        }
        else
        {
            Debug.Log("检测到重复的敌人子弹池实例，销毁当前对象");
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
        
        Debug.Log($"为敌人预制体 {prefab.name} 初始化了子弹池，初始大小: {initialPoolSize}");
    }

    // 预热：确保某个预制体在池中至少存在指定数量
    public void Prewarm(GameObject prefab, int minCount)
    {
        if (prefab == null) return;
        if (!bulletPools.ContainsKey(prefab)) InitializePoolForPrefab(prefab);
        Queue<GameObject> pool = bulletPools[prefab];
        int need = Mathf.Max(0, minCount - pool.Count);
        for (int i = 0; i < need; i++) CreateBulletForPrefab(prefab);
    }

    // 为特定预制体创建子弹实例
    private void CreateBulletForPrefab(GameObject prefab)
    {
        if (prefab == null || !bulletPools.ContainsKey(prefab)) return;
        
        Queue<GameObject> pool = bulletPools[prefab];
        
        // 如果池中的子弹数量已经达到最大值，直接返回
        if (pool.Count >= maxPoolSize)
        {
            Debug.LogWarning($"敌人预制体 {prefab.name} 的子弹池已满");
            return;
        }

        // 实例化一个子弹
        GameObject bulletInstance = Instantiate(prefab);
        bulletInstance.SetActive(false);
        bulletInstance.transform.SetParent(transform);

        // 将新创建的子弹加入对应的池
        pool.Enqueue(bulletInstance);
    }

    // 获取指定预制体的子弹（新方法）
    public GameObject GetBullet(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        // 如果没有指定预制体，使用默认预制体
        if (prefab == null)
        {
            prefab = bulletPrefab;
        }
        
        if (prefab == null)
        {
            Debug.LogWarning("没有指定敌人子弹预制体且没有默认预制体");
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
            bullet.transform.position = position;
            bullet.transform.rotation = rotation;
            bullet.SetActive(true);
            var b = bullet.GetComponent<Bullet>();
            if (b != null) b.ReturnToPool = ReturnBullet;
            return bullet;
        }

        // 如果池中没有可用子弹且可扩容，尝试创建新的
        if (expandable)
        {
            CreateBulletForPrefab(prefab);
            
            if (pool.Count > 0)
            {
                GameObject bullet = pool.Dequeue();
                bullet.transform.position = position;
                bullet.transform.rotation = rotation;
                bullet.SetActive(true);
                var b = bullet.GetComponent<Bullet>();
                if (b != null) b.ReturnToPool = ReturnBullet;
                return bullet;
            }
        }
        
        Debug.LogWarning($"无法获取敌人预制体 {prefab.name} 的子弹");
        return null;
    }

    // 获取可用子弹（保持向后兼容）
    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        return GetBullet(bulletPrefab, position, rotation);
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
            Debug.LogWarning($"找不到敌人子弹 {bullet.name} 对应的池，将销毁该子弹");
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

    private void Update() // 每帧更新
    {
        // 遍历所有池中的子弹
        foreach (var poolPair in bulletPools)
        {
            Queue<GameObject> pool = poolPair.Value;
            List<GameObject> bulletsToReturn = new List<GameObject>();
            
            foreach (GameObject bullet in pool)
            {
                if (bullet.activeInHierarchy) // 仅处理激活状态的子弹
                {
                    // 检测是否超出屏幕范围
                    if (IsBulletOffScreen(bullet))
                    {
                        bulletsToReturn.Add(bullet);
                    }
                }
            }
            
            // 回收屏幕外的子弹
            foreach (GameObject bullet in bulletsToReturn)
            {
                ReturnBullet(bullet);
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
    
    // 获取池的状态信息
    public void LogPoolStatus()
    {
        Debug.Log($"敌人子弹池当前共有 {bulletPools.Count} 个子弹池:");
        foreach (var kvp in bulletPools)
        {
            Debug.Log($"- {kvp.Key.name}: {kvp.Value.Count} 个可用子弹");
        }
    }
}