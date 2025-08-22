// SaveDependencyManager.cs
// 保存依赖关系管理器 - 负责管理保存系统中各组件的依赖关系和保存顺序
// 确保正确的保存顺序：物品数据 → 网格配置 → 网格内容 → UI状态
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using InventorySystem.SaveSystem;

/// <summary>
/// 保存依赖关系管理器
/// 负责协调保存系统中各组件的保存顺序和依赖关系
/// 确保装备槽先于其动态网格保存，UI状态最后保存
/// </summary>
public class SaveDependencyManager : MonoBehaviour
{
    [Header("依赖关系管理")]
    [SerializeField] private string managerID = "";                    // 管理器唯一ID
    [SerializeField] private bool autoGenerateID = true;              // 是否自动生成ID
    [SerializeField] private int saveVersion = 1;                     // 保存版本号

    [Header("保存优先级配置")]
    [SerializeField] private int itemDataPriority = 100;              // 物品数据优先级（最高）
    [SerializeField] private int gridConfigPriority = 80;             // 网格配置优先级
    [SerializeField] private int equipSlotPriority = 70;              // 装备槽优先级
    [SerializeField] private int gridContentPriority = 60;            // 网格内容优先级
    [SerializeField] private int inventoryControllerPriority = 50;    // 库存控制器优先级
    [SerializeField] private int uiStatePriority = 30;                // UI状态优先级（较低）
    [SerializeField] private int containerUIPriority = 10;            // 容器UI优先级（最低）

    [Header("依赖关系跟踪")]
    [SerializeField] private bool enableDependencyTracking = true;    // 是否启用依赖跟踪
    [SerializeField] private bool enableSaveOrderValidation = true;   // 是否启用保存顺序验证
    [SerializeField] private bool logDependencyInfo = true;           // 是否记录依赖信息

    // 保存系统相关
    private SaveManager saveManager;
    private bool isInitialized = false;

    // 依赖关系数据
    private Dictionary<string, SaveableComponent> saveableComponents = new Dictionary<string, SaveableComponent>();
    private Dictionary<string, List<string>> dependencyGraph = new Dictionary<string, List<string>>();
    private Dictionary<SaveableType, int> typePriorities = new Dictionary<SaveableType, int>();
    private List<string> saveOrder = new List<string>();

    // 事件回调
    public System.Action<string> OnDependencyRegistered;
    public System.Action<string> OnDependencyUnregistered;
    public System.Action<List<string>> OnSaveOrderCalculated;
    public System.Action<string> OnSaveOrderValidationFailed;

    void Awake()
    {
        InitializeManager();
    }

    void Start()
    {
        RegisterToSaveSystem();
        InitializePriorities();
    }

    /// <summary>
    /// 初始化管理器
    /// </summary>
    private void InitializeManager()
    {
        // 确保管理器有有效的ID
        if (string.IsNullOrEmpty(managerID) && autoGenerateID)
        {
            GenerateNewManagerID();
        }

        // 初始化字典
        if (saveableComponents == null)
            saveableComponents = new Dictionary<string, SaveableComponent>();
        if (dependencyGraph == null)
            dependencyGraph = new Dictionary<string, List<string>>();
        if (typePriorities == null)
            typePriorities = new Dictionary<SaveableType, int>();
        if (saveOrder == null)
            saveOrder = new List<string>();

        isInitialized = true;
        Debug.Log($"[SaveDependencyManager] 管理器初始化完成，ID: {GetManagerID()}");
    }

    /// <summary>
    /// 初始化优先级配置
    /// </summary>
    private void InitializePriorities()
    {
        typePriorities[SaveableType.ItemData] = itemDataPriority;
        typePriorities[SaveableType.GridConfig] = gridConfigPriority;
        typePriorities[SaveableType.EquipSlot] = equipSlotPriority;
        typePriorities[SaveableType.GridContent] = gridContentPriority;
        typePriorities[SaveableType.InventoryController] = inventoryControllerPriority;
        typePriorities[SaveableType.UIState] = uiStatePriority;
        typePriorities[SaveableType.ContainerUI] = containerUIPriority;

        Debug.Log($"[SaveDependencyManager] 优先级配置已初始化，共 {typePriorities.Count} 种类型");
    }

    /// <summary>
    /// 注册可保存组件
    /// </summary>
    public bool RegisterSaveableComponent(ISaveable saveable, SaveableType type, List<string> dependencies = null)
    {
        if (saveable == null)
        {
            Debug.LogError("[SaveDependencyManager] 尝试注册空的可保存组件");
            return false;
        }

        string saveID = saveable.GetSaveID();
        if (string.IsNullOrEmpty(saveID))
        {
            Debug.LogError("[SaveDependencyManager] 可保存组件的保存ID为空");
            return false;
        }

        // 检查是否已注册
        if (saveableComponents.ContainsKey(saveID))
        {
            Debug.LogWarning($"[SaveDependencyManager] 组件已注册: {saveID}");
            return false;
        }

        // 创建组件信息
        var component = new SaveableComponent
        {
            saveID = saveID,
            saveable = saveable,
            type = type,
            priority = GetTypePriority(type),
            dependencies = dependencies ?? new List<string>(),
            registrationTime = System.DateTime.Now
        };

        // 注册组件
        saveableComponents[saveID] = component;

        // 更新依赖图
        if (dependencies != null && dependencies.Count > 0)
        {
            dependencyGraph[saveID] = new List<string>(dependencies);
        }

        // 重新计算保存顺序
        CalculateSaveOrder();

        OnDependencyRegistered?.Invoke(saveID);

        if (logDependencyInfo)
        {
            Debug.Log($"[SaveDependencyManager] 已注册组件: {saveID}, 类型: {type}, 优先级: {component.priority}");
        }

        return true;
    }

    /// <summary>
    /// 注销可保存组件
    /// </summary>
    public bool UnregisterSaveableComponent(string saveID)
    {
        if (string.IsNullOrEmpty(saveID))
        {
            Debug.LogError("[SaveDependencyManager] 保存ID为空");
            return false;
        }

        if (!saveableComponents.ContainsKey(saveID))
        {
            Debug.LogWarning($"[SaveDependencyManager] 组件未注册: {saveID}");
            return false;
        }

        // 移除组件
        saveableComponents.Remove(saveID);

        // 移除依赖关系
        if (dependencyGraph.ContainsKey(saveID))
        {
            dependencyGraph.Remove(saveID);
        }

        // 移除其他组件对此组件的依赖
        foreach (var kvp in dependencyGraph.ToList())
        {
            if (kvp.Value.Contains(saveID))
            {
                kvp.Value.Remove(saveID);
            }
        }

        // 重新计算保存顺序
        CalculateSaveOrder();

        OnDependencyUnregistered?.Invoke(saveID);

        if (logDependencyInfo)
        {
            Debug.Log($"[SaveDependencyManager] 已注销组件: {saveID}");
        }

        return true;
    }

    /// <summary>
    /// 计算保存顺序
    /// </summary>
    private void CalculateSaveOrder()
    {
        saveOrder.Clear();

        if (saveableComponents.Count == 0)
        {
            return;
        }

        try
        {
            // 使用拓扑排序和优先级排序
            var sortedComponents = TopologicalSort();

            // 按优先级进一步排序
            sortedComponents = sortedComponents
                .OrderByDescending(id => saveableComponents[id].priority)
                .ThenBy(id => saveableComponents[id].registrationTime)
                .ToList();

            saveOrder = sortedComponents;

            OnSaveOrderCalculated?.Invoke(new List<string>(saveOrder));

            if (logDependencyInfo)
            {
                Debug.Log($"[SaveDependencyManager] 保存顺序已计算，共 {saveOrder.Count} 个组件");
                for (int i = 0; i < saveOrder.Count; i++)
                {
                    var component = saveableComponents[saveOrder[i]];
                    Debug.Log($"  {i + 1}. {saveOrder[i]} (类型: {component.type}, 优先级: {component.priority})");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveDependencyManager] 计算保存顺序时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 拓扑排序
    /// </summary>
    private List<string> TopologicalSort()
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var componentID in saveableComponents.Keys)
        {
            if (!visited.Contains(componentID))
            {
                if (!TopologicalSortVisit(componentID, visited, visiting, result))
                {
                    throw new System.InvalidOperationException("检测到循环依赖");
                }
            }
        }

        result.Reverse(); // 反转以获得正确的顺序
        return result;
    }

    /// <summary>
    /// 拓扑排序访问节点
    /// </summary>
    private bool TopologicalSortVisit(string componentID, HashSet<string> visited, HashSet<string> visiting, List<string> result)
    {
        if (visiting.Contains(componentID))
        {
            return false; // 检测到循环依赖
        }

        if (visited.Contains(componentID))
        {
            return true; // 已访问过
        }

        visiting.Add(componentID);

        // 访问依赖项
        if (dependencyGraph.ContainsKey(componentID))
        {
            foreach (var dependency in dependencyGraph[componentID])
            {
                if (saveableComponents.ContainsKey(dependency))
                {
                    if (!TopologicalSortVisit(dependency, visited, visiting, result))
                    {
                        return false;
                    }
                }
            }
        }

        visiting.Remove(componentID);
        visited.Add(componentID);
        result.Add(componentID);

        return true;
    }

    /// <summary>
    /// 获取类型优先级
    /// </summary>
    private int GetTypePriority(SaveableType type)
    {
        return typePriorities.ContainsKey(type) ? typePriorities[type] : 0;
    }

    /// <summary>
    /// 验证保存顺序
    /// </summary>
    public bool ValidateSaveOrder()
    {
        if (!enableSaveOrderValidation)
        {
            return true;
        }

        try
        {
            // 检查依赖关系是否满足
            for (int i = 0; i < saveOrder.Count; i++)
            {
                string componentID = saveOrder[i];

                if (dependencyGraph.ContainsKey(componentID))
                {
                    foreach (var dependency in dependencyGraph[componentID])
                    {
                        int dependencyIndex = saveOrder.IndexOf(dependency);
                        if (dependencyIndex >= 0 && dependencyIndex < i)
                        {
                            Debug.LogError($"[SaveDependencyManager] 保存顺序验证失败: {componentID} 依赖于 {dependency}，但 {dependency} 在其之前保存");
                            OnSaveOrderValidationFailed?.Invoke(componentID);
                            return false;
                        }
                    }
                }
            }

            // 检查优先级是否正确
            for (int i = 0; i < saveOrder.Count - 1; i++)
            {
                var current = saveableComponents[saveOrder[i]];
                var next = saveableComponents[saveOrder[i + 1]];

                if (current.priority < next.priority)
                {
                    Debug.LogWarning($"[SaveDependencyManager] 优先级顺序可能不正确: {current.saveID} (优先级: {current.priority}) 在 {next.saveID} (优先级: {next.priority}) 之前");
                }
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveDependencyManager] 验证保存顺序时发生错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取保存顺序
    /// </summary>
    public List<string> GetSaveOrder()
    {
        return new List<string>(saveOrder);
    }

    /// <summary>
    /// 获取组件信息
    /// </summary>
    public SaveableComponent GetComponentInfo(string saveID)
    {
        return saveableComponents.ContainsKey(saveID) ? saveableComponents[saveID] : null;
    }

    /// <summary>
    /// 获取所有组件信息
    /// </summary>
    public Dictionary<string, SaveableComponent> GetAllComponents()
    {
        return new Dictionary<string, SaveableComponent>(saveableComponents);
    }

    /// <summary>
    /// 获取依赖图
    /// </summary>
    public Dictionary<string, List<string>> GetDependencyGraph()
    {
        var result = new Dictionary<string, List<string>>();
        foreach (var kvp in dependencyGraph)
        {
            result[kvp.Key] = new List<string>(kvp.Value);
        }
        return result;
    }

    /// <summary>
    /// 添加依赖关系
    /// </summary>
    public bool AddDependency(string componentID, string dependencyID)
    {
        if (!saveableComponents.ContainsKey(componentID) || !saveableComponents.ContainsKey(dependencyID))
        {
            Debug.LogError($"[SaveDependencyManager] 组件未注册: {componentID} 或 {dependencyID}");
            return false;
        }

        if (!dependencyGraph.ContainsKey(componentID))
        {
            dependencyGraph[componentID] = new List<string>();
        }

        if (!dependencyGraph[componentID].Contains(dependencyID))
        {
            dependencyGraph[componentID].Add(dependencyID);
            CalculateSaveOrder();

            if (logDependencyInfo)
            {
                Debug.Log($"[SaveDependencyManager] 已添加依赖关系: {componentID} 依赖于 {dependencyID}");
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 移除依赖关系
    /// </summary>
    public bool RemoveDependency(string componentID, string dependencyID)
    {
        if (dependencyGraph.ContainsKey(componentID) && dependencyGraph[componentID].Contains(dependencyID))
        {
            dependencyGraph[componentID].Remove(dependencyID);
            CalculateSaveOrder();

            if (logDependencyInfo)
            {
                Debug.Log($"[SaveDependencyManager] 已移除依赖关系: {componentID} 不再依赖于 {dependencyID}");
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 清除所有依赖关系
    /// </summary>
    public void ClearAllDependencies()
    {
        saveableComponents.Clear();
        dependencyGraph.Clear();
        saveOrder.Clear();

        Debug.Log("[SaveDependencyManager] 已清除所有依赖关系");
    }

    // ==================== ID管理 ====================

    /// <summary>
    /// 生成新的管理器ID
    /// </summary>
    private void GenerateNewManagerID()
    {
        managerID = $"SaveDependencyManager_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}_{GetInstanceID()}";
    }

    /// <summary>
    /// 获取管理器ID
    /// </summary>
    public string GetManagerID()
    {
        if (string.IsNullOrEmpty(managerID))
        {
            GenerateNewManagerID();
        }
        return managerID;
    }

    /// <summary>
    /// 设置管理器ID
    /// </summary>
    public void SetManagerID(string newID)
    {
        if (!string.IsNullOrEmpty(newID) && newID != managerID)
        {
            managerID = newID;
        }
    }

    // ==================== 保存系统集成 ====================

    /// <summary>
    /// 注册到保存系统
    /// </summary>
    private void RegisterToSaveSystem()
    {
        saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            Debug.Log($"[SaveDependencyManager] 已连接到保存系统: {GetManagerID()}");
        }
        else
        {
            Debug.LogWarning("[SaveDependencyManager] SaveManager实例未找到");
        }
    }

    void OnDestroy()
    {
        // 清理事件回调
        OnDependencyRegistered = null;
        OnDependencyUnregistered = null;
        OnSaveOrderCalculated = null;
        OnSaveOrderValidationFailed = null;

        Debug.Log($"[SaveDependencyManager] 管理器已销毁: {managerID}");
    }
}

// ==================== 数据结构定义 ====================

/// <summary>
/// 可保存组件类型枚举
/// </summary>
public enum SaveableType
{
    ItemData,           // 物品数据（最高优先级）
    GridConfig,         // 网格配置
    EquipSlot,          // 装备槽
    GridContent,        // 网格内容
    InventoryController,// 库存控制器
    UIState,            // UI状态
    ContainerUI,        // 容器UI（最低优先级）
    Other               // 其他类型
}

/// <summary>
/// 可保存组件信息
/// </summary>
[System.Serializable]
public class SaveableComponent
{
    [Header("基本信息")]
    public string saveID;                   // 保存ID
    public ISaveable saveable;              // 可保存对象引用
    public SaveableType type;               // 组件类型
    public int priority;                    // 优先级

    [Header("依赖信息")]
    public List<string> dependencies;       // 依赖的组件ID列表
    public System.DateTime registrationTime; // 注册时间

    [Header("状态信息")]
    public bool isValid;                    // 是否有效
    public string lastError;                // 最后错误信息

    public SaveableComponent()
    {
        dependencies = new List<string>();
        isValid = true;
        lastError = "";
    }
}