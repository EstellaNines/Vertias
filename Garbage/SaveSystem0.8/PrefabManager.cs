// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
// using InventorySystem;
// #if UNITY_EDITOR
// using UnityEditor;
// #endif

// namespace InventorySystem
// {
//     /// <summary>
//     /// 预制件管理器 - 负责从ItemDataSO创建物品GameObject实例
//     /// 管理预制件的加载、缓存和实例化过程
//     /// </summary>
//     [CreateAssetMenu(fileName = "PrefabManager", menuName = "Inventory System/Prefab Manager")]
//     public class PrefabManager : ScriptableObject
//     {
//         [Header("预制件配置")]
//         [SerializeField, FieldLabel("预制件路径")] 
//         private string prefabPath = "Assets/InventorySystem/Prefab";
        
//         [SerializeField, FieldLabel("默认物品预制件")] 
//         private GameObject defaultItemPrefab;
        
//         [SerializeField, FieldLabel("自动缓存预制件")] 
//         private bool enablePrefabCaching = true;
        
//         [Header("实例化设置")]
//         [SerializeField, FieldLabel("默认父对象")] 
//         private Transform defaultParent;
        
//         [SerializeField, FieldLabel("实例化位置")] 
//         private Vector3 defaultSpawnPosition = Vector3.zero;
        
//         [SerializeField, FieldLabel("实例化旋转")] 
//         private Vector3 defaultSpawnRotation = Vector3.zero;
        
//         [Header("管理器状态")]
//         [SerializeField, FieldLabel("已缓存预制件数量")] 
//         private int cachedPrefabCount = 0;
        
//         [SerializeField, FieldLabel("管理器状态")] 
//         private string managerStatus = "未初始化";

//         // 预制件缓存
//         private Dictionary<int, GameObject> prefabCache = new Dictionary<int, GameObject>();
//         private Dictionary<ItemCategory, GameObject> categoryPrefabCache = new Dictionary<ItemCategory, GameObject>();
        
//         // 单例模式
//         private static PrefabManager _instance;
//         public static PrefabManager Instance
//         {
//             get
//             {
//                 if (_instance == null)
//                 {
//                     _instance = Resources.Load<PrefabManager>("PrefabManager");
//                     if (_instance == null)
//                     {
//                         Debug.LogError("PrefabManager: 未找到PrefabManager资源文件，请在Resources文件夹中创建");
//                     }
//                     else
//                     {
//                         _instance.Initialize();
//                     }
//                 }
//                 return _instance;
//             }
//         }

//         private void Awake()
//         {
//             if (_instance == null)
//             {
//                 _instance = this;
//                 Initialize();
//             }
//         }

//         /// <summary>
//         /// 初始化预制件管理器
//         /// </summary>
//         public void Initialize()
//         {
//             try
//             {
//                 managerStatus = "正在初始化...";
                
//                 if (enablePrefabCaching)
//                 {
//                     LoadAndCachePrefabs();
//                 }
                
//                 cachedPrefabCount = prefabCache.Count;
//                 managerStatus = $"已缓存 {cachedPrefabCount} 个预制件";
                
//                 Debug.Log($"PrefabManager: 初始化完成，缓存了 {cachedPrefabCount} 个预制件");
//             }
//             catch (System.Exception e)
//             {
//                 managerStatus = $"初始化失败: {e.Message}";
//                 Debug.LogError($"PrefabManager: 初始化失败 - {e.Message}");
//             }
//         }

//         /// <summary>
//         /// 加载并缓存预制件
//         /// </summary>
//         private void LoadAndCachePrefabs()
//         {
// #if UNITY_EDITOR
//             LoadPrefabsInEditor();
// #else
//             LoadPrefabsInBuild();
// #endif
//         }

// #if UNITY_EDITOR
//         /// <summary>
//         /// 编辑器模式下加载预制件
//         /// </summary>
//         private void LoadPrefabsInEditor()
//         {
//             if (!Directory.Exists(prefabPath))
//             {
//                 Debug.LogWarning($"PrefabManager: 预制件路径不存在 - {prefabPath}");
//                 return;
//             }

//             string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabPath });
            
//             foreach (string guid in prefabGuids)
//             {
//                 string assetPath = AssetDatabase.GUIDToAssetPath(guid);
//                 GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                
//                 if (prefab != null)
//                 {
//                     // 尝试从预制件获取ItemDataReader组件
//                     ItemDataReader reader = prefab.GetComponent<ItemDataReader>();
//                     if (reader != null && reader.ItemData != null)
//                     {
//                         int itemId = reader.ItemData.id;
//                         if (!prefabCache.ContainsKey(itemId))
//                         {
//                             prefabCache[itemId] = prefab;
//                         }
                        
//                         // 按分类缓存
//                         ItemCategory category = reader.ItemData.category;
//                         if (!categoryPrefabCache.ContainsKey(category))
//                         {
//                             categoryPrefabCache[category] = prefab;
//                         }
//                     }
//                 }
//             }
//         }
// #endif

//         /// <summary>
//         /// 运行时模式下加载预制件
//         /// </summary>
//         private void LoadPrefabsInBuild()
//         {
//             // 在构建版本中，需要将预制件放在Resources文件夹中
//             GameObject[] prefabs = Resources.LoadAll<GameObject>("");
            
//             foreach (GameObject prefab in prefabs)
//             {
//                 ItemDataReader reader = prefab.GetComponent<ItemDataReader>();
//                 if (reader != null && reader.ItemData != null)
//                 {
//                     int itemId = reader.ItemData.id;
//                     if (!prefabCache.ContainsKey(itemId))
//                     {
//                         prefabCache[itemId] = prefab;
//                     }
                    
//                     // 按分类缓存
//                     ItemCategory category = reader.ItemData.category;
//                     if (!categoryPrefabCache.ContainsKey(category))
//                     {
//                         categoryPrefabCache[category] = prefab;
//                     }
//                 }
//             }
//         }

//         /// <summary>
//         /// 从ItemDataSO创建物品GameObject
//         /// </summary>
//         /// <param name="itemData">物品数据</param>
//         /// <param name="parent">父对象</param>
//         /// <param name="position">生成位置</param>
//         /// <param name="rotation">生成旋转</param>
//         /// <returns>创建的物品GameObject</returns>
//         public GameObject CreateItem(ItemDataSO itemData, Transform parent = null, Vector3? position = null, Quaternion? rotation = null)
//         {
//             if (itemData == null)
//             {
//                 Debug.LogError("PrefabManager: ItemDataSO不能为空");
//                 return null;
//             }

//             try
//             {
//                 // 获取预制件
//                 GameObject prefab = GetPrefabForItem(itemData);
//                 if (prefab == null)
//                 {
//                     Debug.LogError($"PrefabManager: 未找到物品 {itemData.itemName} (ID: {itemData.id}) 对应的预制件");
//                     return null;
//                 }

//                 // 设置实例化参数
//                 Transform targetParent = parent ?? defaultParent;
//                 Vector3 spawnPosition = position ?? defaultSpawnPosition;
//                 Quaternion spawnRotation = rotation ?? Quaternion.Euler(defaultSpawnRotation);

//                 // 实例化物品
//                 GameObject itemInstance = Instantiate(prefab, spawnPosition, spawnRotation, targetParent);
                
//                 // 设置物品数据
//                 ItemDataReader reader = itemInstance.GetComponent<ItemDataReader>();
//                 if (reader != null)
//                 {
//                     reader.SetItemData(itemData);
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"PrefabManager: 预制件 {prefab.name} 缺少ItemDataReader组件");
//                 }

//                 // 设置物品名称
//                 itemInstance.name = $"{itemData.itemName} (ID: {itemData.id})";

//                 Debug.Log($"PrefabManager: 成功创建物品 {itemData.itemName} (ID: {itemData.id})");
//                 return itemInstance;
//             }
//             catch (System.Exception e)
//             {
//                 Debug.LogError($"PrefabManager: 创建物品失败 - {e.Message}");
//                 return null;
//             }
//         }

//         /// <summary>
//         /// 从ItemDataSO创建物品并设置运行时状态
//         /// </summary>
//         /// <param name="itemData">物品数据</param>
//         /// <param name="stackCount">堆叠数量</param>
//         /// <param name="durability">耐久度</param>
//         /// <param name="usageCount">使用次数</param>
//         /// <param name="parent">父对象</param>
//         /// <param name="position">生成位置</param>
//         /// <param name="rotation">生成旋转</param>
//         /// <returns>创建的物品GameObject</returns>
//         public GameObject CreateItemWithState(ItemDataSO itemData, int stackCount = 1, int durability = -1, int usageCount = 0, 
//                                             Transform parent = null, Vector3? position = null, Quaternion? rotation = null)
//         {
//             GameObject itemInstance = CreateItem(itemData, parent, position, rotation);
            
//             if (itemInstance != null)
//             {
//                 ItemDataReader reader = itemInstance.GetComponent<ItemDataReader>();
//                 if (reader != null)
//                 {
//                     // 设置运行时状态 - 使用正确的方法名
//                     reader.SetStack(stackCount);  // 改为SetStack
                    
//                     if (durability >= 0)
//                     {
//                         reader.SetDurability(durability);
//                     }
                    
//                     reader.SetUsageCount(usageCount);
                    
//                     Debug.Log($"PrefabManager: 设置物品状态 - 堆叠: {stackCount}, 耐久: {durability}, 使用次数: {usageCount}");
//                 }
//             }
            
//             return itemInstance;
//         }

//         /// <summary>
//         /// 获取物品对应的预制件
//         /// </summary>
//         /// <param name="itemData">物品数据</param>
//         /// <returns>预制件GameObject</returns>
//         private GameObject GetPrefabForItem(ItemDataSO itemData)
//         {
//             // 优先通过物品ID查找
//             if (prefabCache.TryGetValue(itemData.id, out GameObject cachedPrefab))
//             {
//                 return cachedPrefab;
//             }

//             // 通过分类查找
//             if (categoryPrefabCache.TryGetValue(itemData.category, out GameObject categoryPrefab))
//             {
//                 Debug.LogWarning($"PrefabManager: 使用分类预制件为物品 {itemData.itemName} (ID: {itemData.id})");
//                 return categoryPrefab;
//             }

//             // 使用默认预制件
//             if (defaultItemPrefab != null)
//             {
//                 Debug.LogWarning($"PrefabManager: 使用默认预制件为物品 {itemData.itemName} (ID: {itemData.id})");
//                 return defaultItemPrefab;
//             }

//             return null;
//         }

//         /// <summary>
//         /// 批量创建物品
//         /// </summary>
//         /// <param name="itemDataList">物品数据列表</param>
//         /// <param name="parent">父对象</param>
//         /// <returns>创建的物品GameObject列表</returns>
//         public List<GameObject> CreateItems(List<ItemDataSO> itemDataList, Transform parent = null)
//         {
//             List<GameObject> createdItems = new List<GameObject>();
            
//             foreach (ItemDataSO itemData in itemDataList)
//             {
//                 GameObject item = CreateItem(itemData, parent);
//                 if (item != null)
//                 {
//                     createdItems.Add(item);
//                 }
//             }
            
//             Debug.Log($"PrefabManager: 批量创建了 {createdItems.Count}/{itemDataList.Count} 个物品");
//             return createdItems;
//         }

//         /// <summary>
//         /// 销毁物品GameObject
//         /// </summary>
//         /// <param name="itemGameObject">要销毁的物品GameObject</param>
//         public void DestroyItem(GameObject itemGameObject)
//         {
//             if (itemGameObject != null)
//             {
//                 ItemDataReader reader = itemGameObject.GetComponent<ItemDataReader>();
//                 string itemName = reader?.ItemData?.itemName ?? "未知物品";
                
//                 DestroyImmediate(itemGameObject);
//                 Debug.Log($"PrefabManager: 销毁物品 {itemName}");
//             }
//         }

//         /// <summary>
//         /// 批量销毁物品
//         /// </summary>
//         /// <param name="itemGameObjects">要销毁的物品GameObject列表</param>
//         public void DestroyItems(List<GameObject> itemGameObjects)
//         {
//             int destroyedCount = 0;
            
//             foreach (GameObject item in itemGameObjects)
//             {
//                 if (item != null)
//                 {
//                     DestroyItem(item);
//                     destroyedCount++;
//                 }
//             }
            
//             Debug.Log($"PrefabManager: 批量销毁了 {destroyedCount} 个物品");
//         }

//         /// <summary>
//         /// 检查物品是否有对应的预制件
//         /// </summary>
//         /// <param name="itemId">物品ID</param>
//         /// <returns>是否有预制件</returns>
//         public bool HasPrefabForItem(int itemId)
//         {
//             return prefabCache.ContainsKey(itemId) || defaultItemPrefab != null;
//         }

//         /// <summary>
//         /// 获取管理器统计信息
//         /// </summary>
//         /// <returns>统计信息字符串</returns>
//         public string GetManagerStats()
//         {
//             var stats = new System.Text.StringBuilder();
//             stats.AppendLine($"预制件缓存数量: {prefabCache.Count}");
//             stats.AppendLine($"分类预制件数量: {categoryPrefabCache.Count}");
//             stats.AppendLine($"默认预制件: {(defaultItemPrefab != null ? defaultItemPrefab.name : "未设置")}");
//             stats.AppendLine($"预制件路径: {prefabPath}");
            
//             stats.AppendLine("分类预制件统计:");
//             foreach (var category in categoryPrefabCache)
//             {
//                 stats.AppendLine($"  {category.Key}: {category.Value.name}");
//             }
            
//             return stats.ToString();
//         }

//         /// <summary>
//         /// 重新加载预制件缓存
//         /// </summary>
//         [ContextMenu("重新加载预制件缓存")]
//         public void ReloadPrefabCache()
//         {
//             prefabCache.Clear();
//             categoryPrefabCache.Clear();
//             Initialize();
//         }

//         /// <summary>
//         /// 设置默认父对象
//         /// </summary>
//         /// <param name="parent">默认父对象</param>
//         public void SetDefaultParent(Transform parent)
//         {
//             defaultParent = parent;
//             Debug.Log($"PrefabManager: 设置默认父对象为 {(parent != null ? parent.name : "null")}");
//         }

//         /// <summary>
//         /// 设置默认生成位置
//         /// </summary>
//         /// <param name="position">默认生成位置</param>
//         public void SetDefaultSpawnPosition(Vector3 position)
//         {
//             defaultSpawnPosition = position;
//             Debug.Log($"PrefabManager: 设置默认生成位置为 {position}");
//         }

// #if UNITY_EDITOR
//         /// <summary>
//         /// 编辑器下验证预制件完整性
//         /// </summary>
//         [ContextMenu("验证预制件完整性")]
//         public void ValidatePrefabs()
//         {
//             Initialize();
            
//             Debug.Log("=== PrefabManager 验证报告 ===");
//             Debug.Log(GetManagerStats());
            
//             // 检查缺少ItemDataReader的预制件
//             int invalidPrefabCount = 0;
//             foreach (var prefab in prefabCache.Values)
//             {
//                 ItemDataReader reader = prefab.GetComponent<ItemDataReader>();
//                 if (reader == null || reader.ItemData == null)
//                 {
//                     Debug.LogWarning($"预制件 {prefab.name} 缺少有效的ItemDataReader组件");
//                     invalidPrefabCount++;
//                 }
//             }
            
//             if (invalidPrefabCount == 0)
//             {
//                 Debug.Log("所有预制件验证通过！");
//             }
//             else
//             {
//                 Debug.LogWarning($"发现 {invalidPrefabCount} 个无效预制件");
//             }
            
//             Debug.Log("=== 验证完成 ===");
//         }
// #endif
//     }
// }