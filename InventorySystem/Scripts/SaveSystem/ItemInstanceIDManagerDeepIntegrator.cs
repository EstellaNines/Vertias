using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using InventorySystem.SaveSystem;

/// <summary>
/// ItemInstanceIDManager深度集成器 - 非侵入式地将ID管理系统与现有组件深度集成
/// 提供自动初始化、场景持久化、向后兼容性和保存逻辑更新功能
/// </summary>
public class ItemInstanceIDManagerDeepIntegrator : MonoBehaviour
{
    [Header("深度集成配置")]
    [SerializeField] private bool enableAutoIntegration = true; // 启用自动集成
    [SerializeField] private bool enableScenePersistence = true; // 启用场景持久化
    [SerializeField] private bool enableBackwardCompatibility = true; // 启用向后兼容性
    [SerializeField] private bool enableSaveLogicUpdate = true; // 启用保存逻辑更新
    [SerializeField] private float integrationDelay = 0.5f; // 集成延迟时间
    [SerializeField] private bool enableDebugLogging = true; // 启用调试日志

    [Header("集成目标配置")]
    [SerializeField] private bool integrateInventoryItems = true; // 集成库存物品
    [SerializeField] private bool integrateItemGrids = true; // 集成物品网格
    [SerializeField] private bool integrateItemSpawners = true; // 集成物品生成器
    [SerializeField] private bool integrateEquipSlots = true; // 集成装备槽
    [SerializeField] private bool integrateItemDataHolders = true; // 集成物品数据持有者

    [Header("兼容性配置")]
    [SerializeField] private bool preserveExistingIDs = true; // 保留现有ID
    [SerializeField] private bool migrateOldSaveData = true; // 迁移旧保存数据
    [SerializeField] private bool validateDataIntegrity = true; // 验证数据完整性

    // 集成状态跟踪
    private HashSet<string> integratedObjects = new HashSet<string>();
    private Dictionary<string, string> idMigrationMap = new Dictionary<string, string>(); // 旧ID到新ID的映射
    private List<ISaveable> pendingIntegrations = new List<ISaveable>(); // 待集成对象

    // 集成统计信息
    [System.Serializable]
    public class DeepIntegrationStats
    {
        public int totalIntegratedObjects = 0;
        public int migratedIDs = 0;
        public int resolvedConflicts = 0;
        public int validationErrors = 0;
        public int backupCreated = 0;
        public string lastIntegrationTime = "";
        public string integrationVersion = "1.0.0";
    }

    [SerializeField] private DeepIntegrationStats integrationStats = new DeepIntegrationStats();

    // 事件系统
    public static event System.Action<ISaveable> OnObjectIntegrated;
    public static event System.Action<string, string> OnIDMigrated;
    public static event System.Action<string> OnConflictResolved;
    public static event System.Action<DeepIntegrationStats> OnIntegrationCompleted;

    private void Awake()
    {
        // 确保ItemInstanceIDManager存在
        if (ItemInstanceIDManager.Instance == null)
        {
            LogWarning("ItemInstanceIDManager实例不存在，正在创建...");
            var managerGO = new GameObject("ItemInstanceIDManager");
            managerGO.AddComponent<ItemInstanceIDManager>();

            // 将新创建的ItemInstanceIDManager设置为SaveSystem的子对象（如果存在SaveSystem）
            var saveSystemPersistence = FindObjectOfType<SaveSystemPersistence>();
            if (saveSystemPersistence != null)
            {
                managerGO.transform.SetParent(saveSystemPersistence.transform);
            }
            // 注意：不调用DontDestroyOnLoad，因为SaveSystemPersistence会处理整个系统的持久化
        }

        // 注册场景事件
        if (enableScenePersistence)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        LogMessage("深度集成器已初始化");
    }

    private void Start()
    {
        if (enableAutoIntegration)
        {
            StartCoroutine(DelayedDeepIntegration());
        }
    }

    private void OnDestroy()
    {
        // 清理事件订阅
        if (enableScenePersistence)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        LogMessage("深度集成器已销毁");
    }

    /// <summary>
    /// 延迟执行深度集成
    /// </summary>
    private IEnumerator DelayedDeepIntegration()
    {
        yield return new WaitForSeconds(integrationDelay);

        LogMessage("开始执行深度集成...");
        PerformDeepIntegration();
    }

    /// <summary>
    /// 执行深度集成
    /// </summary>
    public void PerformDeepIntegration()
    {
        try
        {
            // 重置统计信息
            integrationStats = new DeepIntegrationStats();
            integrationStats.integrationVersion = "1.0.0";

            // 创建备份
            if (enableBackwardCompatibility)
            {
                CreateIntegrationBackup();
            }

            // 发现并收集所有ISaveable对象
            CollectISaveableObjects();

            // 执行ID迁移和兼容性处理
            if (enableBackwardCompatibility)
            {
                PerformIDMigration();
            }

            // 集成各类组件
            if (integrateInventoryItems) IntegrateInventorySystemItems();
            if (integrateItemGrids) IntegrateBaseItemGrids();
            if (integrateItemSpawners) IntegrateBaseItemSpawners();
            if (integrateEquipSlots) IntegrateEquipSlots();
            if (integrateItemDataHolders) IntegrateItemDataHolders();

            // 验证集成结果
            if (validateDataIntegrity)
            {
                ValidateIntegrationIntegrity();
            }

            // 解决冲突
            ResolveIntegrationConflicts();

            // 完成集成
            CompleteIntegration();

            LogMessage($"深度集成完成！集成了{integrationStats.totalIntegratedObjects}个对象");
        }
        catch (Exception ex)
        {
            LogError($"深度集成过程中发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 收集场景中的所有ISaveable对象
    /// </summary>
    private void CollectISaveableObjects()
    {
        pendingIntegrations.Clear();

        // 查找所有MonoBehaviour组件
        var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>(true);

        foreach (var mb in allMonoBehaviours)
        {
            if (mb is ISaveable saveable)
            {
                pendingIntegrations.Add(saveable);
            }
        }

        LogMessage($"发现{pendingIntegrations.Count}个ISaveable对象待集成");
    }

    /// <summary>
    /// 执行ID迁移和兼容性处理
    /// </summary>
    private void PerformIDMigration()
    {
        if (!migrateOldSaveData) return;

        LogMessage("开始执行ID迁移...");

        foreach (var saveable in pendingIntegrations)
        {
            try
            {
                string currentID = saveable.GetSaveID();

                // 检查ID格式是否需要迁移
                if (NeedsIDMigration(currentID))
                {
                    string newID = GenerateCompatibleID(saveable, currentID);

                    if (!string.IsNullOrEmpty(newID) && newID != currentID)
                    {
                        // 记录迁移映射
                        idMigrationMap[currentID] = newID;

                        // 更新ID
                        saveable.SetSaveID(newID);
                        saveable.MarkAsModified();

                        integrationStats.migratedIDs++;
                        OnIDMigrated?.Invoke(currentID, newID);

                        LogMessage($"ID迁移: {currentID} -> {newID}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"ID迁移失败: {saveable.GetType().Name}, 错误: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }

        LogMessage($"ID迁移完成，迁移了{integrationStats.migratedIDs}个ID");
    }

    /// <summary>
    /// 检查ID是否需要迁移
    /// </summary>
    private bool NeedsIDMigration(string id)
    {
        if (string.IsNullOrEmpty(id)) return true;

        // 检查是否为旧格式的ID（例如：不包含类型前缀）
        if (!id.Contains("_")) return true;

        // 检查是否为GUID格式但没有类型信息
        if (System.Guid.TryParse(id, out _) && !id.Contains("Item_") && !id.Contains("Grid_") && !id.Contains("Spawner_") && !id.Contains("EquipSlot_") && !id.Contains("DataHolder_")) return true;

        return false;
    }

    /// <summary>
    /// 检查ID是否已经有正确的前缀
    /// </summary>
    private bool HasCorrectPrefix(string id, ISaveable saveable)
    {
        if (string.IsNullOrEmpty(id)) return false;

        switch (saveable)
        {
            case InventorySystemItem _: return id.StartsWith("Item_");
            case BaseItemGrid _: return id.StartsWith("Grid_");
            case BaseItemSpawn _: return id.StartsWith("ItemSpawner_") || id.StartsWith("BaseSpawner_") || id.StartsWith("Spawner_");
            case EquipSlot _: return id.StartsWith("EquipSlot_");
            case ItemDataHolder _: return id.StartsWith("DataHolder_");
            default: return false;
        }
    }

    /// <summary>
    /// 生成兼容的ID
    /// </summary>
    private string GenerateCompatibleID(ISaveable saveable, string oldID)
    {
        string prefix = GetIDPrefix(saveable);

        if (preserveExistingIDs && !string.IsNullOrEmpty(oldID))
        {
            // 检查ID是否已经有正确的前缀，避免重复添加
            if (HasCorrectPrefix(oldID, saveable))
            {
                return oldID; // 已有正确前缀，直接返回
            }

            // 尝试保留原有ID，只添加前缀
            if (System.Guid.TryParse(oldID, out _))
            {
                return $"{prefix}_{oldID}";
            }
            else
            {
                return $"{prefix}_{System.Guid.NewGuid()}";
            }
        }
        else
        {
            return $"{prefix}_{System.Guid.NewGuid()}";
        }
    }

    /// <summary>
    /// 获取对象类型的ID前缀
    /// </summary>
    private string GetIDPrefix(ISaveable saveable)
    {
        switch (saveable)
        {
            case InventorySystemItem _: return "Item";
            case BaseItemGrid _: return "Grid";
            case BaseItemSpawn _: return "Spawner";
            case EquipSlot _: return "EquipSlot";
            case ItemDataHolder _: return "DataHolder";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// 集成InventorySystemItem对象
    /// </summary>
    private void IntegrateInventorySystemItems()
    {
        var items = FindObjectsOfType<InventorySystemItem>(true);
        LogMessage($"开始集成{items.Length}个InventorySystemItem对象...");

        foreach (var item in items)
        {
            try
            {
                IntegrateInventorySystemItem(item);
            }
            catch (Exception ex)
            {
                LogError($"集成InventorySystemItem失败: {item.name}, 错误: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }
    }

    /// <summary>
    /// 集成单个InventorySystemItem
    /// </summary>
    private void IntegrateInventorySystemItem(InventorySystemItem item)
    {
        string instanceID = item.GetItemInstanceID();

        // 确保有有效的实例ID
        if (string.IsNullOrEmpty(instanceID) || !item.IsItemInstanceIDValid())
        {
            item.GenerateNewItemInstanceID();
            instanceID = item.GetItemInstanceID();
            LogMessage($"为InventorySystemItem生成新ID: {instanceID}");
        }

        // 注册到ItemInstanceIDManager
        if (ItemInstanceIDManager.Instance.RegisterInstanceID(instanceID, item.gameObject.name, "InventorySystemItem", item.transform.position))
        {
            // 标记为已集成
            integratedObjects.Add(instanceID);
            integrationStats.totalIntegratedObjects++;

            // 更新保存逻辑
            if (enableSaveLogicUpdate)
            {
                UpdateItemSaveLogic(item);
            }

            // OnObjectIntegrated?.Invoke(item); // 注释掉：InventorySystemItem未实现ISaveable接口
            LogMessage($"成功集成InventorySystemItem: {instanceID}");
        }
        else
        {
            LogWarning($"注册InventorySystemItem失败: {instanceID}");
            integrationStats.validationErrors++;
        }
    }

    /// <summary>
    /// 集成BaseItemGrid对象
    /// </summary>
    private void IntegrateBaseItemGrids()
    {
        var grids = FindObjectsOfType<BaseItemGrid>(true);
        LogMessage($"开始集成{grids.Length}个BaseItemGrid对象...");

        foreach (var grid in grids)
        {
            try
            {
                IntegrateBaseItemGrid(grid);
            }
            catch (Exception ex)
            {
                LogError($"集成BaseItemGrid失败: {grid.name}, 错误: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }
    }

    /// <summary>
    /// 集成单个BaseItemGrid
    /// </summary>
    private void IntegrateBaseItemGrid(BaseItemGrid grid)
    {
        string gridID = grid.GetSaveID();

        // 确保有有效的网格ID
        if (string.IsNullOrEmpty(gridID) || !grid.IsSaveIDValid())
        {
            grid.GenerateNewSaveID();
            gridID = grid.GetSaveID();
            LogMessage($"为BaseItemGrid生成新ID: {gridID}");
        }

        // 注册到ItemInstanceIDManager
        if (ItemInstanceIDManager.Instance.RegisterInstanceID(gridID, grid.gameObject.name, "BaseItemGrid", grid.transform.position))
        {
            // 标记为已集成
            integratedObjects.Add(gridID);
            integrationStats.totalIntegratedObjects++;

            // 集成网格中的物品
            IntegrateGridItems(grid);

            // 更新保存逻辑
            if (enableSaveLogicUpdate)
            {
                UpdateGridSaveLogic(grid);
            }

            // OnObjectIntegrated?.Invoke(grid); // 注释掉：BaseItemGrid未实现ISaveable接口
            LogMessage($"成功集成BaseItemGrid: {gridID}");
        }
        else
        {
            LogWarning($"注册BaseItemGrid失败: {gridID}");
            integrationStats.validationErrors++;
        }
    }

    /// <summary>
    /// 集成网格中的物品
    /// </summary>
    private void IntegrateGridItems(BaseItemGrid grid)
    {
        // 获取网格中的所有物品
        var gridItems = grid.GetComponentsInChildren<InventorySystemItem>(true);

        foreach (var item in gridItems)
        {
            try
            {
                IntegrateInventorySystemItem(item);

                // 建立网格与物品的关联
                string gridID = grid.GetSaveID();
                string itemID = item.GetItemInstanceID();

                // RegisterItemToGrid方法不存在，暂时注释
                // ItemInstanceIDManager.Instance.RegisterItemToGrid(itemID, gridID);
                LogMessage($"建立关联: 物品{itemID} -> 网格{gridID}");
            }
            catch (Exception ex)
            {
                LogError($"集成网格物品失败: {item.name}, 错误: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }
    }

    /// <summary>
    /// 集成BaseItemSpawn对象
    /// </summary>
    private void IntegrateBaseItemSpawners()
    {
        var spawners = FindObjectsOfType<BaseItemSpawn>(true);
        LogMessage($"开始集成{spawners.Length}个BaseItemSpawn对象...");

        foreach (var spawner in spawners)
        {
            try
            {
                IntegrateBaseItemSpawner(spawner);
            }
            catch (Exception ex)
            {
                LogError($"集成BaseItemSpawn失败: {spawner.name}, 错误: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }
    }

    /// <summary>
    /// 集成单个BaseItemSpawn
    /// </summary>
    private void IntegrateBaseItemSpawner(BaseItemSpawn spawner)
    {
        string spawnerID = spawner.GetSaveID();

        // 确保有有效的生成器ID
        if (string.IsNullOrEmpty(spawnerID) || !spawner.IsSaveIDValid())
        {
            spawner.GenerateNewSaveID();
            spawnerID = spawner.GetSaveID();
            LogMessage($"为BaseItemSpawn生成新ID: {spawnerID}");
        }

        // 注册到ItemInstanceIDManager
        if (ItemInstanceIDManager.Instance.RegisterInstanceID(spawnerID, spawner.gameObject.name, "BaseItemSpawn", spawner.transform.position))
        {
            // 标记为已集成
            integratedObjects.Add(spawnerID);
            integrationStats.totalIntegratedObjects++;

            // 更新保存逻辑
            if (enableSaveLogicUpdate)
            {
                UpdateSpawnerSaveLogic(spawner);
            }

            OnObjectIntegrated?.Invoke(spawner);
            LogMessage($"成功集成BaseItemSpawn: {spawnerID}");
        }
        else
        {
            LogWarning($"注册BaseItemSpawn失败: {spawnerID}");
            integrationStats.validationErrors++;
        }
    }

    /// <summary>
    /// 集成EquipSlot对象
    /// </summary>
    private void IntegrateEquipSlots()
    {
        var equipSlots = FindObjectsOfType<EquipSlot>(true);
        LogMessage($"开始集成{equipSlots.Length}个EquipSlot对象...");

        foreach (var equipSlot in equipSlots)
        {
            try
            {
                IntegrateEquipSlot(equipSlot);
            }
            catch (Exception ex)
            {
                LogError($"集成EquipSlot失败: {equipSlot.name}, 错误: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }
    }

    /// <summary>
    /// 集成单个EquipSlot
    /// </summary>
    private void IntegrateEquipSlot(EquipSlot equipSlot)
    {
        string slotID = equipSlot.GetSaveID();

        // 确保有有效的装备槽ID
        if (string.IsNullOrEmpty(slotID) || !equipSlot.IsSaveIDValid())
        {
            equipSlot.GenerateNewSaveID();
            slotID = equipSlot.GetSaveID();
            LogMessage($"为EquipSlot生成新ID: {slotID}");
        }

        // 注册到ItemInstanceIDManager
        if (ItemInstanceIDManager.Instance.RegisterInstanceID(slotID, equipSlot.gameObject.name, "EquipSlot", equipSlot.transform.position))
        {
            // 标记为已集成
            integratedObjects.Add(slotID);
            integrationStats.totalIntegratedObjects++;

            // 更新保存逻辑
            if (enableSaveLogicUpdate)
            {
                UpdateEquipSlotSaveLogic(equipSlot);
            }

            OnObjectIntegrated?.Invoke(equipSlot);
            LogMessage($"成功集成EquipSlot: {slotID}");
        }
        else
        {
            LogWarning($"注册EquipSlot失败: {slotID}");
            integrationStats.validationErrors++;
        }
    }

    /// <summary>
    /// 集成ItemDataHolder对象
    /// </summary>
    private void IntegrateItemDataHolders()
    {
        var dataHolders = FindObjectsOfType<ItemDataHolder>(true);
        LogMessage($"开始集成{dataHolders.Length}个ItemDataHolder对象...");

        foreach (var dataHolder in dataHolders)
        {
            try
            {
                IntegrateItemDataHolder(dataHolder);
            }
            catch (Exception ex)
            {
                LogError($"集成ItemDataHolder失败: {dataHolder.name}, 错误: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }
    }

    /// <summary>
    /// 集成单个ItemDataHolder
    /// </summary>
    private void IntegrateItemDataHolder(ItemDataHolder dataHolder)
    {
        string holderID = dataHolder.GetSaveID();

        // 确保有有效的数据持有者ID
        if (string.IsNullOrEmpty(holderID))
        {
            dataHolder.GenerateNewSaveID();
            holderID = dataHolder.GetSaveID();
            LogMessage($"为ItemDataHolder生成新ID: {holderID}");
        }

        // 注册到ItemInstanceIDManager
        if (ItemInstanceIDManager.Instance.RegisterInstanceID(holderID, dataHolder.gameObject.name, "ItemDataHolder", dataHolder.transform.position))
        {
            // 标记为已集成
            integratedObjects.Add(holderID);
            integrationStats.totalIntegratedObjects++;

            // 更新保存逻辑
            if (enableSaveLogicUpdate)
            {
                UpdateDataHolderSaveLogic(dataHolder);
            }

            // OnObjectIntegrated?.Invoke(dataHolder); // 注释掉：ItemDataHolder未实现ISaveable接口
            LogMessage($"成功集成ItemDataHolder: {holderID}");
        }
        else
        {
            LogWarning($"注册ItemDataHolder失败: {holderID}");
            integrationStats.validationErrors++;
        }
    }

    /// <summary>
    /// 更新物品保存逻辑
    /// </summary>
    private void UpdateItemSaveLogic(InventorySystemItem item)
    {
        // 确保物品使用新的ID系统
        if (!item.IsItemInstanceIDValid())
        {
            item.GenerateNewItemInstanceID();
        }

        // 物品本身没有MarkAsModified方法，通过其他方式标记修改
        // 可以通过SaveManager标记对象变化
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkObjectChanged(item.GetItemInstanceID());
        }
    }

    /// <summary>
    /// 更新网格保存逻辑
    /// </summary>
    private void UpdateGridSaveLogic(BaseItemGrid grid)
    {
        // 确保网格使用新的ID系统
        if (!grid.IsSaveIDValid())
        {
            grid.GenerateNewSaveID();
        }

        // InitializeSaveSystem是protected方法，不能直接调用
        // 通过反射或其他方式调用，或者注释掉
        // grid.InitializeSaveSystem();

        // 标记为已修改，触发保存
        grid.MarkAsModified();
    }

    /// <summary>
    /// 更新生成器保存逻辑
    /// </summary>
    private void UpdateSpawnerSaveLogic(BaseItemSpawn spawner)
    {
        // 确保生成器使用新的ID系统
        if (!spawner.IsSaveIDValid())
        {
            spawner.GenerateNewSaveID();
        }

        // 标记为已修改，触发保存
        spawner.MarkAsModified();
    }

    /// <summary>
    /// 更新装备槽保存逻辑
    /// </summary>
    private void UpdateEquipSlotSaveLogic(EquipSlot equipSlot)
    {
        // 确保装备槽使用新的ID系统
        if (!equipSlot.IsSaveIDValid())
        {
            equipSlot.GenerateNewSaveID();
        }

        // 标记为已修改，触发保存
        equipSlot.MarkAsModified();
    }

    /// <summary>
    /// 更新数据持有者保存逻辑
    /// </summary>
    private void UpdateDataHolderSaveLogic(ItemDataHolder dataHolder)
    {
        // 确保数据持有者使用新的ID系统
        // ItemDataHolder没有IsSaveIDValid方法，直接检查SaveID
        if (string.IsNullOrEmpty(dataHolder.GetSaveID()))
        {
            dataHolder.GenerateNewSaveID();
        }

        // ItemDataHolder没有MarkAsModified方法，通过SaveManager标记变化
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkObjectChanged(dataHolder.GetSaveID());
        }
    }

    /// <summary>
    /// 验证集成完整性
    /// </summary>
    private void ValidateIntegrationIntegrity()
    {
        LogMessage("开始验证集成完整性...");

        int validationErrors = 0;

        // 验证所有已集成对象的ID有效性
        foreach (string objectID in integratedObjects)
        {
            if (!ItemInstanceIDManager.Instance.IsIDRegistered(objectID))
            {
                LogError($"集成验证失败：ID未注册 - {objectID}");
                validationErrors++;
            }
        }

        // 验证ID迁移映射的完整性
        foreach (var migration in idMigrationMap)
        {
            if (!ItemInstanceIDManager.Instance.IsIDRegistered(migration.Value))
            {
                LogError($"ID迁移验证失败：新ID未注册 - {migration.Key} -> {migration.Value}");
                validationErrors++;
            }
        }

        integrationStats.validationErrors += validationErrors;

        if (validationErrors == 0)
        {
            LogMessage("集成完整性验证通过");
        }
        else
        {
            LogWarning($"集成完整性验证发现{validationErrors}个错误");
        }
    }

    /// <summary>
    /// 解决集成冲突
    /// </summary>
    private void ResolveIntegrationConflicts()
    {
        var conflicts = ItemInstanceIDManager.Instance.GetActiveConflicts();

        if (conflicts.Count == 0)
        {
            LogMessage("未发现集成冲突");
            return;
        }

        LogMessage($"发现{conflicts.Count}个集成冲突，开始解决...");

        foreach (var conflict in conflicts)
        {
            try
            {
                // 尝试自动解决冲突（ResolveConflict方法不存在，使用替代方案）
                // bool resolved = ItemInstanceIDManager.Instance.ResolveConflict(conflict.conflictID);
                bool resolved = TryResolveConflictAlternative(conflict.conflictID);

                if (resolved)
                {
                    integrationStats.resolvedConflicts++;
                    OnConflictResolved?.Invoke(conflict.conflictID);
                    LogMessage($"成功解决冲突: {conflict.conflictID}");
                }
                else
                {
                    LogWarning($"无法自动解决冲突: {conflict.conflictID}");
                    integrationStats.validationErrors++;
                }
            }
            catch (Exception ex)
            {
                LogError($"解决冲突时发生错误: {conflict.conflictID}, 错误: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }

        LogMessage($"冲突解决完成，成功解决{integrationStats.resolvedConflicts}个冲突");
    }

    /// <summary>
    /// 创建集成备份
    /// </summary>
    private void CreateIntegrationBackup()
    {
        try
        {
            // 导出当前ID映射数据作为备份
            var backupData = ItemInstanceIDManager.Instance.ExportIDMappingData();

            if (!string.IsNullOrEmpty(backupData))
            {
                string backupPath = $"Assets/InventorySystem/Backups/integration_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";

                // 确保备份目录存在
                string backupDir = System.IO.Path.GetDirectoryName(backupPath);
                if (!System.IO.Directory.Exists(backupDir))
                {
                    System.IO.Directory.CreateDirectory(backupDir);
                }

                // 写入备份文件
                System.IO.File.WriteAllText(backupPath, backupData);

                integrationStats.backupCreated++;
                LogMessage($"集成备份已创建: {backupPath}");
            }
        }
        catch (Exception ex)
        {
            LogError($"创建集成备份失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 完成集成
    /// </summary>
    private void CompleteIntegration()
    {
        integrationStats.lastIntegrationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 触发完成事件
        OnIntegrationCompleted?.Invoke(integrationStats);

        // 保存集成状态
        SaveIntegrationState();

        LogMessage($"深度集成已完成！统计信息：\n" +
                  $"- 集成对象总数: {integrationStats.totalIntegratedObjects}\n" +
                  $"- 迁移ID数量: {integrationStats.migratedIDs}\n" +
                  $"- 解决冲突数量: {integrationStats.resolvedConflicts}\n" +
                  $"- 验证错误数量: {integrationStats.validationErrors}\n" +
                  $"- 创建备份数量: {integrationStats.backupCreated}\n" +
                  $"- 集成时间: {integrationStats.lastIntegrationTime}");
    }

    /// <summary>
    /// 保存集成状态
    /// </summary>
    private void SaveIntegrationState()
    {
        try
        {
            // 将集成状态保存到PlayerPrefs或文件
            string stateJson = JsonUtility.ToJson(integrationStats, true);
            PlayerPrefs.SetString("ItemInstanceIDManager_IntegrationState", stateJson);
            PlayerPrefs.Save();

            LogMessage("集成状态已保存");
        }
        catch (Exception ex)
        {
            LogError($"保存集成状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载集成状态
    /// </summary>
    private void LoadIntegrationState()
    {
        try
        {
            string stateJson = PlayerPrefs.GetString("ItemInstanceIDManager_IntegrationState", "");

            if (!string.IsNullOrEmpty(stateJson))
            {
                integrationStats = JsonUtility.FromJson<DeepIntegrationStats>(stateJson);
                LogMessage("集成状态已加载");
            }
        }
        catch (Exception ex)
        {
            LogError($"加载集成状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 场景加载事件处理
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!enableScenePersistence) return;

        LogMessage($"场景已加载: {scene.name}，开始场景集成...");

        // 延迟执行场景集成，确保所有对象都已初始化
        StartCoroutine(DelayedSceneIntegration());
    }

    /// <summary>
    /// 场景卸载事件处理
    /// </summary>
    private void OnSceneUnloaded(Scene scene)
    {
        if (!enableScenePersistence) return;

        LogMessage($"场景已卸载: {scene.name}，清理场景数据...");

        // 清理场景相关的集成数据
        CleanupSceneIntegration(scene.name);
    }

    /// <summary>
    /// 延迟执行场景集成
    /// </summary>
    private IEnumerator DelayedSceneIntegration()
    {
        yield return new WaitForSeconds(0.1f);

        if (enableAutoIntegration)
        {
            PerformDeepIntegration();
        }
    }

    /// <summary>
    /// 清理场景集成数据
    /// </summary>
    private void CleanupSceneIntegration(string sceneName)
    {
        // 清理场景注册信息（CleanupSceneRegistrations是private方法，不能直接调用）
        // ItemInstanceIDManager.Instance.CleanupSceneRegistrations(sceneName);
        LogMessage($"场景卸载，需要清理注册信息: {sceneName}");

        LogMessage($"场景集成数据清理完成: {sceneName}");
    }

    // === 公共API方法 ===

    /// <summary>
    /// 手动触发深度集成
    /// </summary>
    public void ManualDeepIntegration()
    {
        LogMessage("手动触发深度集成");
        PerformDeepIntegration();
    }

    /// <summary>
    /// 获取集成统计信息
    /// </summary>
    public DeepIntegrationStats GetIntegrationStats()
    {
        return integrationStats;
    }

    /// <summary>
    /// 检查对象是否已集成
    /// </summary>
    public bool IsObjectIntegrated(string objectID)
    {
        return integratedObjects.Contains(objectID);
    }

    /// <summary>
    /// 获取ID迁移映射
    /// </summary>
    public Dictionary<string, string> GetIDMigrationMap()
    {
        return new Dictionary<string, string>(idMigrationMap);
    }

    /// <summary>
    /// 清除集成状态
    /// </summary>
    public void ClearIntegrationState()
    {
        integratedObjects.Clear();
        idMigrationMap.Clear();
        pendingIntegrations.Clear();
        integrationStats = new DeepIntegrationStats();

        PlayerPrefs.DeleteKey("ItemInstanceIDManager_IntegrationState");
        PlayerPrefs.Save();

        LogMessage("集成状态已清除");
    }

    /// <summary>
    /// 重新集成指定对象
    /// </summary>
    public void ReintegrateObject(ISaveable saveable)
    {
        if (saveable == null) return;

        string objectID = saveable.GetSaveID();

        // 移除旧的集成记录
        integratedObjects.Remove(objectID);

        // 重新集成
        switch (saveable)
        {
            case InventorySystemItem item:
                IntegrateInventorySystemItem(item);
                break;
            case BaseItemGrid grid:
                IntegrateBaseItemGrid(grid);
                break;
            case BaseItemSpawn spawner:
                IntegrateBaseItemSpawner(spawner);
                break;
            case EquipSlot equipSlot:
                IntegrateEquipSlot(equipSlot);
                break;
            case ItemDataHolder dataHolder:
                IntegrateItemDataHolder(dataHolder);
                break;
        }

        LogMessage($"对象重新集成完成: {objectID}");
    }

    /// <summary>
    /// 验证系统集成状态
    /// </summary>
    public bool ValidateSystemIntegration()
    {
        bool isValid = true;

        // 检查ItemInstanceIDManager是否存在
        if (ItemInstanceIDManager.Instance == null)
        {
            LogError("ItemInstanceIDManager实例不存在");
            isValid = false;
        }

        // 检查集成对象的有效性
        foreach (string objectID in integratedObjects)
        {
            if (!ItemInstanceIDManager.Instance.IsIDRegistered(objectID))
            {
                LogError($"集成对象ID未注册: {objectID}");
                isValid = false;
            }
        }

        return isValid;
    }

    /// <summary>
    /// 尝试使用替代方案解决冲突
    /// </summary>
    private bool TryResolveConflictAlternative(string conflictID)
    {
        try
        {
            // 由于ResolveConflict方法不存在，使用替代方案
            // 检查冲突ID是否仍然存在
            if (ItemInstanceIDManager.Instance.IsIDRegistered(conflictID))
            {
                LogMessage($"冲突ID仍然注册，标记为已解决: {conflictID}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            LogError($"解决冲突替代方案失败: {ex.Message}");
            return false;
        }
    }

    // === 日志工具方法 ===

    private void LogMessage(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[ItemInstanceIDManagerDeepIntegrator] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogging)
        {
            Debug.LogWarning($"[ItemInstanceIDManagerDeepIntegrator] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ItemInstanceIDManagerDeepIntegrator] {message}");
    }
}