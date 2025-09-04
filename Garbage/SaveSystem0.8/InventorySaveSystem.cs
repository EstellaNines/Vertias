// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using Newtonsoft.Json;
// using System.Linq;
// using InventorySystem;
// using InventorySystem.Save;
// using System.IO;

// // 背包网格保存/加载系统
// // 专注于ItemGrid网格系统的数据持久化
// public class InventorySaveSystem : MonoBehaviour
// {
//     [Header("配置设置")]
//     [FieldLabel("保存配置资源")] public InventorySaveDataSO saveConfig;
//     [FieldLabel("物品数据库")] public ItemDatabase itemDatabase;
//     [FieldLabel("预制件管理器")] public PrefabManager prefabManager;

//     [Header("运行时状态")]
//     [FieldLabel("自动保存启用")] public bool autoSaveEnabled = true;
//     [FieldLabel("上次保存时间")] public string lastSaveTime = "";
//     [FieldLabel("保存数据大小")] public int saveDataSize = 0;
//     [FieldLabel("恢复统计")] public RestoreStatistics restoreStats = new RestoreStatistics();

//     // 内部状态
//     private float autoSaveTimer = 0f;
//     private bool isInitialized = false;
//     public Dictionary<string, ItemGrid> registeredGrids = new Dictionary<string, ItemGrid>();

//     // 新增：JSON文件保存路径
//     private string jsonSavePath;

//     // 事件系统
//     public static event Action<bool> OnSaveCompleted;
//     public static event Action<bool> OnLoadCompleted;
//     public static event Action<string> OnSaveError;
//     public static event Action<ItemSnapshot, bool> OnItemRestored; // 新增：物品恢复事件

//     // 恢复统计数据结构
//     [System.Serializable]
//     public class RestoreStatistics
//     {
//         public int totalAttempts = 0;
//         public int successfulRestores = 0;
//         public int failedRestores = 0;
//         public List<string> failureReasons = new List<string>();
        
//         public float SuccessRate => totalAttempts > 0 ? (float)successfulRestores / totalAttempts : 0f;
        
//         public void Reset()
//         {
//             totalAttempts = 0;
//             successfulRestores = 0;
//             failedRestores = 0;
//             failureReasons.Clear();
//         }
        
//         public void RecordAttempt(bool success, string reason = "")
//         {
//             totalAttempts++;
//             if (success)
//             {
//                 successfulRestores++;
//             }
//             else
//             {
//                 failedRestores++;
//                 if (!string.IsNullOrEmpty(reason))
//                 {
//                     failureReasons.Add(reason);
//                 }
//             }
//         }
//     }

//     private void Awake()
//     {
//         // 验证配置
//         if (saveConfig == null)
//         {
//             Debug.LogError("InventorySaveSystem: 保存配置未设置！");
//             return;
//         }

//         // 自动查找系统组件（如果未手动设置）
//         if (itemDatabase == null)
//         {
//             itemDatabase = ItemDatabase.Instance;
//             if (itemDatabase == null)
//             {
//                 Debug.LogWarning("InventorySaveSystem: 未找到 ItemDatabase 实例，物品恢复功能将受限");
//             }
//         }

//         if (prefabManager == null)
//         {
//             prefabManager = PrefabManager.Instance;
//             if (prefabManager == null)
//             {
//                 Debug.LogWarning("InventorySaveSystem: 未找到 PrefabManager 实例，物品恢复功能将受限");
//             }
//         }

//         // 初始化JSON保存路径
//         InitializeJsonSavePath();
//     }

//     // 新增：初始化JSON保存路径
//     private void InitializeJsonSavePath()
//     {
//         jsonSavePath = Path.Combine(Application.dataPath, "InventorySystem", "Save");

//         // 确保保存目录存在
//         if (!Directory.Exists(jsonSavePath))
//         {
//             Directory.CreateDirectory(jsonSavePath);
//             if (saveConfig != null && saveConfig.EnableDebugLog)
//             {
//                 Debug.Log($"InventorySaveSystem: 已创建保存目录 {jsonSavePath}");
//             }
//         }
//     }

//     private void Start()
//     {
//         StartCoroutine(DelayedInitialize());
//     }

//     private System.Collections.IEnumerator DelayedInitialize()
//     {
//         // 等 1 帧，保证 ItemDatabase、PrefabManager 等全部 Start 完毕
//         yield return null;
//         InitializeSystem();
//     }


//     private void Update()
//     {
//         // 自动保存计时器
//         if (autoSaveEnabled && isInitialized && saveConfig.AutoSaveInterval > 0)
//         {
//             autoSaveTimer += Time.deltaTime;
//             if (autoSaveTimer >= saveConfig.AutoSaveInterval)
//             {
//                 autoSaveTimer = 0f;
//                 SaveInventoryData();
//             }
//         }
//     }

//     // 初始化保存系统
//     private void InitializeSystem()
//     {
//         if (itemDatabase == null || prefabManager == null || registeredGrids.Count == 0)
//         {
//             Debug.LogWarning("依赖未就绪，跳过本次加载；稍后将自动重试。");
//             return;
//         }
        
//         if (isInitialized) return;

//         try
//         {
//             // 验证配置有效性
//             if (!saveConfig.ValidateConfig())
//             {
//                 Debug.LogError("InventorySaveSystem: 配置验证失败！");
//                 return;
//             }

//             // 注册现有的背包网格
//             RegisterExistingGrids();

//             // 尝试加载现有数据
//             LoadInventoryData();

//             isInitialized = true;

//             if (saveConfig.EnableDebugLog)
//             {
//                 Debug.Log("InventorySaveSystem: 网格保存系统初始化完成");
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"InventorySaveSystem: 初始化失败 - {ex.Message}");
//             OnSaveError?.Invoke($"初始化失败: {ex.Message}");
//         }
//     }

//     // 注册现有的背包网格
//     private void RegisterExistingGrids()
//     {
//         // 查找所有ItemGrid组件
//         ItemGrid[] grids = FindObjectsOfType<ItemGrid>();

//         foreach (ItemGrid grid in grids)
//         {
//             // 为每个网格生成唯一ID
//             string gridId = GenerateGridId(grid);
//             RegisterGrid(gridId, grid);
//         }

//         if (saveConfig.EnableDebugLog)
//         {
//             Debug.Log($"InventorySaveSystem: 已注册 {registeredGrids.Count} 个网格");
//         }
//     }

//     // 生成网格唯一ID
//     // 生成网格唯一ID - 修复版本
//     private string GenerateGridId(ItemGrid grid)
//     {
//         if (grid == null) return "unknown_grid";
        
//         // 优先使用ItemGrid自带的GridId
//         if (!string.IsNullOrEmpty(grid.GridId))
//         {
//             return grid.GridId;
//         }
        
//         // 备用方案：基于层级路径生成
//         string path = GetGameObjectPath(grid.transform);
//         string size = $"{grid.gridSizeWidth}x{grid.gridSizeHeight}";
//         string gridId = $"{path}_{size}_{grid.GetInstanceID()}";
        
//         // 确保GridId不为空
//         if (string.IsNullOrEmpty(gridId))
//         {
//             gridId = $"grid_{grid.GetInstanceID()}";
//         }
        
//         return gridId;
//     }

//     // 获取GameObject的完整路径
//     private string GetGameObjectPath(Transform transform)
//     {
//         string path = transform.name;
//         Transform parent = transform.parent;

//         while (parent != null)
//         {
//             path = parent.name + "/" + path;
//             parent = parent.parent;
//         }

//         return path;
//     }

//     // 注册背包网格（公共接口，供外部调用）
//     public void RegisterGrid(string gridId, ItemGrid grid)
//     {
//         if (string.IsNullOrEmpty(gridId) || grid == null)
//         {
//             Debug.LogWarning("InventorySaveSystem: 无效的网格注册参数");
//             return;
//         }

//         registeredGrids[gridId] = grid;

//         if (saveConfig.EnableDebugLog)
//         {
//             Debug.Log($"InventorySaveSystem: 已注册网格 {gridId}");
//         }
//     }

//     // 注销背包网格（公共接口，供外部调用）
//     public void UnregisterGrid(string gridId)
//     {
//         if (registeredGrids.ContainsKey(gridId))
//         {
//             registeredGrids.Remove(gridId);

//             if (saveConfig.EnableDebugLog)
//             {
//                 Debug.Log($"InventorySaveSystem: 已注销网格 {gridId}");
//             }
//         }
//     }

//     // 保存背包数据
//     public bool SaveInventoryData()
//     {
//         if (!isInitialized)
//         {
//             Debug.LogWarning("InventorySaveSystem: 系统未初始化，无法保存数据");
//             return false;
//         }

//         try
//         {
//             // 收集所有背包数据
//             InventoryData inventoryData = CollectInventoryData();

//             // 序列化数据
//             string jsonData = JsonConvert.SerializeObject(inventoryData, Formatting.Indented);

//             // 验证数据大小 (转换KB到字节)
//             int maxDataSizeBytes = saveConfig.MaxSaveDataSizeKB * 1024;
//             if (jsonData.Length > maxDataSizeBytes)
//             {
//                 Debug.LogError($"InventorySaveSystem: 保存数据过大 ({jsonData.Length} > {maxDataSizeBytes})");
//                 OnSaveError?.Invoke("保存数据过大");
//                 return false;
//             }

//             // 保存到PlayerPrefs
//             PlayerPrefs.SetString(saveConfig.SaveKey, jsonData);
//             PlayerPrefs.Save();

//             // 保存到JSON文件
//             SaveToJsonFile(jsonData);

//             // 创建备份
//             if (saveConfig.EnableBackupSave)
//             {
//                 string backupKey = saveConfig.GetBackupSaveKey();
//                 PlayerPrefs.SetString(backupKey, jsonData);

//                 // 创建JSON备份文件
//                 SaveToJsonFile(jsonData, true);
//             }

//             // 更新状态信息
//             lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
//             saveDataSize = jsonData.Length;

//             if (saveConfig.EnableDebugLog)
//             {
//                 Debug.Log($"InventorySaveSystem: 网格数据保存成功 - 大小: {saveDataSize} 字节");
//             }

//             OnSaveCompleted?.Invoke(true);
//             return true;
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"InventorySaveSystem: 保存失败 - {ex.Message}");
//             OnSaveError?.Invoke($"保存失败: {ex.Message}");
//             OnSaveCompleted?.Invoke(false);
//             return false;
//         }
//     }

//     // 保存到JSON文件
//     private void SaveToJsonFile(string jsonData, bool isBackup = false)
//     {
//         try
//         {
//             string fileName = isBackup ? "InventoryData_Backup.json" : "InventoryData.json";
//             string filePath = Path.Combine(jsonSavePath, fileName);

//             File.WriteAllText(filePath, jsonData, System.Text.Encoding.UTF8);

//             if (saveConfig.EnableDebugLog)
//             {
//                 Debug.Log($"InventorySaveSystem: JSON文件保存成功 - {filePath}");
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"InventorySaveSystem: JSON文件保存失败 - {ex.Message}");
//         }
//     }

//     // 加载背包数据
//     public bool LoadInventoryData()
//     {
//         if (!isInitialized && saveConfig == null)
//         {
//             Debug.LogWarning("InventorySaveSystem: 系统未初始化，无法加载数据");
//             return false;
//         }

//         try
//         {
//             string jsonData = null;

//             // 优先尝试从JSON文件加载
//             jsonData = LoadFromJsonFile();

//             // 如果JSON文件加载失败，尝试从PlayerPrefs加载
//             if (string.IsNullOrEmpty(jsonData))
//             {
//                 if (!PlayerPrefs.HasKey(saveConfig.SaveKey))
//                 {
//                     if (saveConfig.EnableDebugLog)
//                     {
//                         Debug.Log("InventorySaveSystem: 未找到保存数据，跳过加载");
//                     }
//                     OnLoadCompleted?.Invoke(true);
//                     return true;
//                 }

//                 jsonData = PlayerPrefs.GetString(saveConfig.SaveKey);
//             }

//             if (string.IsNullOrEmpty(jsonData))
//             {
//                 Debug.LogWarning("InventorySaveSystem: 保存数据为空");
//                 return TryLoadBackup();
//             }

//             // 反序列化数据
//             InventoryData inventoryData = JsonConvert.DeserializeObject<InventoryData>(jsonData);

//             if (inventoryData == null)
//             {
//                 Debug.LogError("InventorySaveSystem: 数据反序列化失败");
//                 return TryLoadBackup();
//             }

//             // 验证数据版本
//             if (inventoryData.version != saveConfig.SaveDataVersion)
//             {
//                 Debug.LogWarning($"InventorySaveSystem: 数据版本不匹配 ({inventoryData.version} != {saveConfig.SaveDataVersion})");
//                 // 可以在这里添加版本迁移逻辑
//             }

//             // 应用数据到背包系统
//             ApplyInventoryData(inventoryData);

//             // 更新状态信息
//             saveDataSize = jsonData.Length;

//             if (saveConfig.EnableDebugLog)
//             {
//                 Debug.Log($"InventorySaveSystem: 网格数据加载成功 - 物品数量: {inventoryData.items.Count}");
//             }

//             OnLoadCompleted?.Invoke(true);
//             return true;
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"InventorySaveSystem: 加载失败 - {ex.Message}");
//             OnSaveError?.Invoke($"加载失败: {ex.Message}");

//             // 尝试加载备份
//             bool backupResult = TryLoadBackup();
//             OnLoadCompleted?.Invoke(backupResult);
//             return backupResult;
//         }
//     }

//     // 加载从JSON文件加载
//     public string LoadFromJsonFile()
//     {
//         try
//         {
//             string filePath = Path.Combine(jsonSavePath, "InventoryData.json");

//             if (File.Exists(filePath))
//             {
//                 string jsonData = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

//                 if (saveConfig.EnableDebugLog)
//                 {
//                     Debug.Log($"InventorySaveSystem: 从JSON文件加载成功 - {filePath}");
//                 }

//                 return jsonData;
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"InventorySaveSystem: JSON文件加载失败 - {ex.Message}");
//         }

//         return null;
//     }

//     // 尝试加载备份数据
//     private bool TryLoadBackup()
//     {
//         if (!saveConfig.EnableBackupSave)
//         {
//             return false;
//         }

//         try
//         {
//             string backupData = null;

//             // 优先尝试从JSON备份文件加载
//             backupData = LoadFromJsonBackupFile();

//             // 如果JSON备份加载失败，尝试从PlayerPrefs备份加载
//             if (string.IsNullOrEmpty(backupData))
//             {
//                 string backupKey = saveConfig.GetBackupSaveKey();

//                 if (!PlayerPrefs.HasKey(backupKey))
//                 {
//                     Debug.LogWarning("InventorySaveSystem: 备份数据不存在");
//                     return false;
//                 }

//                 backupData = PlayerPrefs.GetString(backupKey);
//             }

//             if (!string.IsNullOrEmpty(backupData))
//             {
//                 InventoryData inventoryData = JsonConvert.DeserializeObject<InventoryData>(backupData);

//                 if (inventoryData != null)
//                 {
//                     ApplyInventoryData(inventoryData);
//                     Debug.Log("InventorySaveSystem: 已从备份恢复数据");
//                     return true;
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"InventorySaveSystem: 备份加载失败 - {ex.Message}");
//         }

//         return false;
//     }

//     // 加载从JSON备份文件加载
//     private string LoadFromJsonBackupFile()
//     {
//         try
//         {
//             string filePath = Path.Combine(jsonSavePath, "InventoryData_Backup.json");

//             if (File.Exists(filePath))
//             {
//                 string jsonData = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

//                 if (saveConfig.EnableDebugLog)
//                 {
//                     Debug.Log($"InventorySaveSystem: 从JSON备份文件加载成功 - {filePath}");
//                 }

//                 return jsonData;
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"InventorySaveSystem: JSON备份文件加载失败 - {ex.Message}");
//         }

//         return null;
//     }

//     // 收集背包数据
//     private InventoryData CollectInventoryData()
//     {
//         InventoryData data = new InventoryData
//         {
//             version = saveConfig.SaveDataVersion,
//             saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
//             items = new List<ItemSnapshot>(),
//             grids = new Dictionary<string, GridData>()
//         };

//         // 遍历所有已注册的网格
//         foreach (var kvp in registeredGrids)
//         {
//             string gridId = kvp.Key;
//             ItemGrid grid = kvp.Value;

//             if (grid == null) continue;

//             // --- 防御：跳过空/空白的 gridId ---
//             if (string.IsNullOrEmpty(gridId))
//             {
//                 Debug.LogWarning("InventorySaveSystem: 跳过保存，gridId 为空或空白");
//                 continue;
//             }

//             // 收集网格元数据
//             GridData gridData = new GridData
//             {
//                 gridId = gridId,
//                 width = grid.gridSizeWidth,
//                 height = grid.gridSizeHeight,
//                 itemCount = 0
//             };

//             // 遍历网格中所有格子
//             for (int x = 0; x < grid.gridSizeWidth; x++)
//             {
//                 for (int y = 0; y < grid.gridSizeHeight; y++)
//                 {
//                     Item item = grid.GetItem(x, y);
//                     if (item == null) continue;

//                     // 避免重复：同一个物品只保存一次
//                     if (data.items.Any(s => s.InstanceId == item.GetInstanceID()))
//                         continue;

//                     ItemSnapshot snapshot = ItemSnapshot.CreateFromItem(item, gridId);
//                     if (snapshot != null && snapshot.IsValid())
//                     {
//                         data.items.Add(snapshot);
//                         gridData.itemCount++;
//                     }
//                 }
//             }

//             data.grids[gridId] = gridData;
//         }

//         return data;
//     }

//     // 清空所有网格 - 修复：移到正确的位置
//     private void ClearAllGrids()
//     {
//         foreach (var kvp in registeredGrids)
//         {
//             ItemGrid grid = kvp.Value;
//             if (grid == null) continue;
    
//             // 清空网格中的所有物品
//             for (int x = 0; x < grid.gridSizeWidth; x++)
//             {
//                 for (int y = 0; y < grid.gridSizeHeight; y++)
//                 {
//                     Item item = grid.GetItem(x, y);
//                     if (item != null)
//                     {
//                         // 销毁物品GameObject
//                         DestroyImmediate(item.gameObject);
//                     }
//                 }
//             }
    
//             // 重新初始化网格
//             grid.Init(grid.gridSizeWidth, grid.gridSizeHeight);
//         }
    
//         if (saveConfig.EnableDebugLog)
//         {
//             Debug.Log("InventorySaveSystem: 已清空所有网格");
//         }
//     }

//     // 应用背包数据 - 修复：移除嵌套的ClearAllGrids方法
//     private void ApplyInventoryData(InventoryData data)
//     {
//         if (data?.items == null)
//         {
//             Debug.LogWarning("InventorySaveSystem: 无效的背包数据");
//             return;
//         }

//         restoreStats.Reset();

//         // 清空现有网格
//         ClearAllGrids();

//         // 按 gridId 分组，跳过空 key
//         var itemsByGrid = data.items
//                               .Where(s => !string.IsNullOrEmpty(s.GridId))
//                               .GroupBy(s => s.GridId);

//         foreach (var group in itemsByGrid)
//         {
//             string gridId = group.Key;

//             if (!registeredGrids.TryGetValue(gridId, out ItemGrid targetGrid) || targetGrid == null)
//             {
//                 Debug.LogWarning($"InventorySaveSystem: 未注册网格 {gridId}，跳过相关物品");
//                 continue;
//             }

//             // 逐个恢复物品，单物品异常不影响后续
//             foreach (var snapshot in group)
//             {
//                 try
//                 {
//                     RestoreItemFromSnapshot(snapshot, targetGrid);
//                 }
//                 catch (Exception ex)
//                 {
//                     restoreStats.RecordAttempt(false, $"物品恢复异常: {ex.Message}");
//                     Debug.LogError($"InventorySaveSystem: 恢复物品失败 - {ex}");
//                 }
//             }
//         }

//         if (saveConfig.EnableDebugLog)
//         {
//             Debug.Log($"InventorySaveSystem: 物品恢复完成 - " +
//                       $"成功: {restoreStats.successfulRestores}/{restoreStats.totalAttempts} " +
//                       $"(成功率: {restoreStats.SuccessRate:P1})");
//         }
//     }

//     // 从快照恢复物品 - 修复版本
//     public void RestoreItemFromSnapshot(ItemSnapshot snapshot, ItemGrid targetGrid)
//     {
//         string failureReason = "";
        
//         try
//         {
//             // 基础验证
//             if (snapshot == null || !snapshot.IsValid())
//             {
//                 failureReason = "无效快照";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogWarning($"InventorySaveSystem: {failureReason}");
//                 return;
//             }

//             if (targetGrid == null)
//             {
//                 failureReason = "目标网格为空";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogWarning($"InventorySaveSystem: {failureReason}");
//                 return;
//             }

//             // 验证系统组件
//             if (itemDatabase == null)
//             {
//                 failureReason = "ItemDatabase未初始化";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogError($"InventorySaveSystem: {failureReason}");
//                 return;
//             }

//             if (prefabManager == null)
//             {
//                 failureReason = "PrefabManager未初始化";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogError($"InventorySaveSystem: {failureReason}");
//                 return;
//             }

//             // 验证位置是否有效
//             if (snapshot.GridX < 0 || snapshot.GridX >= targetGrid.gridSizeWidth ||
//                 snapshot.GridY < 0 || snapshot.GridY >= targetGrid.gridSizeHeight)
//             {
//                 failureReason = $"位置超出范围({snapshot.GridX}, {snapshot.GridY})";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogWarning($"InventorySaveSystem: {failureReason}");
//                 return;
//             }

//             // 检查位置是否已被占用
//             if (targetGrid.GetItem(snapshot.GridX, snapshot.GridY) != null)
//             {
//                 failureReason = $"位置已被占用({snapshot.GridX}, {snapshot.GridY})";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogWarning($"InventorySaveSystem: {failureReason}");
//                 return;
//             }

//             // 1. 从数据库获取物品数据
//             ItemDataSO itemData = itemDatabase.GetItemData(snapshot.ItemId);
//             if (itemData == null)
//             {
//                 failureReason = $"未找到物品数据: {snapshot.ItemId}";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogError($"InventorySaveSystem: {failureReason}");
//                 return;
//             }

//             // 2. 创建物品GameObject
//             GameObject itemObject = prefabManager.CreateItem(itemData);
//             if (itemObject == null)
//             {
//                 failureReason = $"创建物品失败: {snapshot.ItemId}";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogError($"InventorySaveSystem: {failureReason}");
//                 return;
//             }

//             // 3. 获取Item组件并验证
//             Item item = itemObject.GetComponent<Item>();
//             if (item == null)
//             {
//                 failureReason = $"物品缺少Item组件: {snapshot.ItemId}";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogError($"InventorySaveSystem: {failureReason}");
                
//                 // 清理创建的GameObject
//                 if (itemObject != null)
//                 {
//                     DestroyImmediate(itemObject);
//                 }
//                 return;
//             }

//             // 4. 恢复物品状态
//             // 获取ItemDataReader组件
//             ItemDataReader itemDataReader = item.GetComponent<ItemDataReader>();
//             if (itemDataReader != null)
//             {
//                 RestoreItemState(itemDataReader, snapshot);
//             }
//             else
//             {
//                 Debug.LogWarning($"InventorySaveSystem: 物品 {snapshot.ItemId} 缺少ItemDataReader组件");
//             }

//             // 5. 设置旋转状态 - 修复方法名
//             if (snapshot.IsRotated)
//             {
//                 item.ToggleRotation(); // 使用正确的方法名
//             }

//             // 6. 验证物品尺寸是否适合目标位置 - 修复方法名
//             Vector2Int itemSize = item.GetCurrentSize(); // 使用正确的方法名
//             if (!targetGrid.BoundryCheck(snapshot.GridX, snapshot.GridY, itemSize.x, itemSize.y))
//             {
//                 failureReason = $"物品尺寸不适合目标位置: {itemSize} at ({snapshot.GridX}, {snapshot.GridY})";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogWarning($"InventorySaveSystem: {failureReason}");
                
//                 // 清理创建的GameObject
//                 DestroyImmediate(itemObject);
//                 return;
//             }

//             // 7. 将物品放置到网格中
//             bool placementSuccess = targetGrid.PlaceItem(item, snapshot.GridX, snapshot.GridY);
//             if (!placementSuccess)
//             {
//                 failureReason = $"放置物品失败: ({snapshot.GridX}, {snapshot.GridY})";
//                 restoreStats.RecordAttempt(false, failureReason);
//                 Debug.LogWarning($"InventorySaveSystem: {failureReason}");
                
//                 // 清理创建的GameObject
//                 DestroyImmediate(itemObject);
//                 return;
//             }

//             // 8. 恢复成功
//             restoreStats.RecordAttempt(true);
            
//             if (saveConfig.EnableDebugLog)
//             {
//                 Debug.Log($"InventorySaveSystem: 成功恢复物品 {snapshot.ItemId} 到位置 ({snapshot.GridX}, {snapshot.GridY})");
//             }

//             // 触发恢复事件
//             OnItemRestored?.Invoke(snapshot, true);
//         }
//         catch (Exception ex)
//         {
//             failureReason = $"异常: {ex.Message}";
//             restoreStats.RecordAttempt(false, failureReason);
//             Debug.LogError($"InventorySaveSystem: 恢复物品失败 - {ex.Message}\n{ex.StackTrace}");
            
//             // 触发恢复失败事件
//             OnItemRestored?.Invoke(snapshot, false);
//         }
//     }

//     // 恢复物品状态 - 修复方法名
//     private void RestoreItemState(ItemDataReader reader, ItemSnapshot snapshot)
//     {
//         if (reader == null || snapshot == null) return;
    
//         // 恢复堆叠数量 - 使用SetStack而不是SetStackCount
//         reader.SetStack(snapshot.StackCount);
        
//         // 恢复耐久度
//         reader.SetDurability((int)snapshot.Durability);
        
//         // 恢复使用次数 - 现在可以使用SetUsageCount
//         reader.SetUsageCount(snapshot.UsageCount);
        
//         // 刷新UI - 使用UpdateUI而不是RefreshUI
//         reader.UpdateUI();
//     }

//     // 批量恢复物品（性能优化版本）
//     public void RestoreItemsBatch(List<ItemSnapshot> snapshots, ItemGrid targetGrid, int batchSize = 10)
//     {
//         if (snapshots == null || snapshots.Count == 0 || targetGrid == null)
//         {
//             Debug.LogWarning("InventorySaveSystem: 批量恢复参数无效");
//             return;
//         }

//         StartCoroutine(RestoreItemsBatchCoroutine(snapshots, targetGrid, batchSize));
//     }

//     private System.Collections.IEnumerator RestoreItemsBatchCoroutine(List<ItemSnapshot> snapshots, ItemGrid targetGrid, int batchSize)
//     {
//         int processedCount = 0;
        
//         for (int i = 0; i < snapshots.Count; i += batchSize)
//         {
//             int endIndex = Mathf.Min(i + batchSize, snapshots.Count);
            
//             // 处理当前批次
//             for (int j = i; j < endIndex; j++)
//             {
//                 RestoreItemFromSnapshot(snapshots[j], targetGrid);
//                 processedCount++;
//             }
            
//             // 每批次后暂停一帧，避免卡顿
//             yield return null;
            
//             if (saveConfig.EnableDebugLog)
//             {
//                 Debug.Log($"InventorySaveSystem: 批量恢复进度 {processedCount}/{snapshots.Count}");
//             }
//         }
        
//         Debug.Log($"InventorySaveSystem: 批量恢复完成，共处理 {processedCount} 个物品");
//     }

//     // 获取恢复统计信息
//     public RestoreStatistics GetRestoreStatistics()
//     {
//         return restoreStats;
//     }

//     // 验证系统完整性
//     public bool ValidateSystemIntegrity()
//     {
//         List<string> issues = new List<string>();
        
//         if (saveConfig == null)
//             issues.Add("保存配置未设置");
            
//         if (itemDatabase == null)
//             issues.Add("ItemDatabase未设置");
            
//         if (prefabManager == null)
//             issues.Add("PrefabManager未设置");
            
//         if (registeredGrids.Count == 0)
//             issues.Add("未注册任何网格");
        
//         if (issues.Count > 0)
//         {
//             Debug.LogWarning($"InventorySaveSystem: 系统完整性检查发现问题: {string.Join(", ", issues)}");
//             return false;
//         }
        
//         return true;
//     }
// }

// // 背包数据结构
// [System.Serializable]
// public class InventoryData
// {
//     public string version;                              // 数据版本
//     public string saveTime;                             // 保存时间
//     public List<ItemSnapshot> items;                    // 物品快照列表
//     public Dictionary<string, GridData> grids;          // 网格数据字典
// }

// // 网格数据结构
// [System.Serializable]
// public class GridData
// {
//     public string gridId;                               // 网格ID
//     public int width;                                   // 网格宽度
//     public int height;                                  // 网格高度
//     public int itemCount;                               // 物品数量
// }

// // 保存数据信息
// [System.Serializable]
// public class SaveDataInfo
// {
//     public bool hasData;                                // 是否有保存数据
//     public int dataSize;                                // 数据大小
//     public string lastSaveTime;                         // 最后保存时间
//     public int registeredGridCount;                     // 注册的网格数量
//     public bool autoSaveEnabled;                        // 自动保存是否启用
// }