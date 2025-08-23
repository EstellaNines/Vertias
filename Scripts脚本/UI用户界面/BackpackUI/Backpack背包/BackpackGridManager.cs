using UnityEngine;
using System.Collections.Generic;
using System;
using InventorySystem.SaveSystem;

/// <summary>
/// 背包网格管理器 - 负责管理背包网格的初始化、配置和状态保存
/// 实现ISaveable接口以支持保存系统集成
/// </summary>
public class BackpackGridManager : MonoBehaviour, ISaveable
{
    [Header("背包网格配置")]
    [SerializeField] private GridConfig backpackGridConfig;
    [SerializeField] private ItemGrid backpackGrid;
    [SerializeField] private InventoryController inventoryController;

    [Header("网格系统组件")]
    [SerializeField] private GridInteract gridInteract;

    [Header("保存系统设置")]
    [SerializeField] private string managerID = "";              // 管理器唯一ID
    [SerializeField] private bool isModified = false;           // 是否已修改
    [SerializeField] private string lastModified = "";          // 最后修改时间

    // 保存系统相关
    private SaveManager saveManager;
    private bool isInitialized = false;

    private void Start()
    {
        InitializeSaveSystem();
        InitializeBackpackGrid();
    }

    /// <summary>
    /// 初始化保存系统
    /// </summary>
    private void InitializeSaveSystem()
    {
        // 生成唯一ID
        if (string.IsNullOrEmpty(managerID))
        {
            GenerateNewSaveID();
        }

        // 获取SaveManager并注册
        saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.RegisterSaveable(this);
            Debug.Log($"BackpackGridManager已注册到保存系统: {GetSaveID()}");
        }
        else
        {
            Debug.LogWarning("SaveManager未找到，BackpackGridManager无法注册到保存系统");
        }

        isInitialized = true;
        UpdateLastModified();
    }

    private void InitializeBackpackGrid()
    {
        if (backpackGrid != null && backpackGridConfig != null)
        {
            // 设置网格配置
            backpackGrid.SetGridConfig(backpackGridConfig);

            // 初始化网格
            backpackGrid.LoadFromGridConfig();

            // 设置库存控制器 - 修复：使用新的API
            if (inventoryController != null)
            {
                inventoryController.SetSelectedMainGrid(backpackGrid);
            }

            Debug.Log("背包网格系统初始化完成");
        }
        else
        {
            Debug.LogError("背包网格配置或网格组件未设置！");
        }
    }

    // 获取背包网格
    public ItemGrid GetBackpackGrid()
    {
        return backpackGrid;
    }

    // 设置网格配置
    public void SetGridConfig(GridConfig config)
    {
        backpackGridConfig = config;
        if (backpackGrid != null)
        {
            backpackGrid.SetGridConfig(config);
            backpackGrid.LoadFromGridConfig();
        }
    }

    // 清空背包网格
    public void ClearBackpackGrid()
    {
        if (backpackGrid != null)
        {
            // 获取所有已放置的物品
            var placedItems = new List<ItemGrid.PlacedItem>();
            // 注意：需要根据ItemGrid的实际API调整这里的代码

            for (int i = placedItems.Count - 1; i >= 0; i--)
            {
                if (placedItems[i].itemObject != null)
                {
                    backpackGrid.RemoveItem(placedItems[i].itemObject);
                    Destroy(placedItems[i].itemObject);
                }
            }
        }
    }

    // 保存背包状态
    public void SaveBackpackState()
    {
        if (backpackGrid != null)
        {
            backpackGrid.SaveToGridConfig();
        }
    }

    // 新增：切换到背包网格模式的方法
    public void SwitchToBackpackMode()
    {
        if (inventoryController != null && backpackGrid != null)
        {
            inventoryController.SetSelectedMainGrid(backpackGrid);
        }
    }

    // 新增：获取当前是否为活跃网格
    public bool IsActiveGrid()
    {
        if (inventoryController == null) return false;

        return inventoryController.GetActiveGridType() == InventoryController.ActiveGridType.MainGrid &&
               inventoryController.selectedItemGrid == backpackGrid;
    }

    // ==================== ISaveable接口实现 ====================

    /// <summary>
    /// 获取管理器的唯一标识ID
    /// </summary>
    /// <returns>管理器的唯一ID字符串</returns>
    public string GetSaveID()
    {
        if (string.IsNullOrEmpty(managerID))
        {
            GenerateNewSaveID();
        }
        return managerID;
    }

    /// <summary>
    /// 设置管理器的唯一标识ID
    /// </summary>
    /// <param name="id">新的ID字符串</param>
    public void SetSaveID(string id)
    {
        if (!string.IsNullOrEmpty(id) && managerID != id)
        {
            managerID = id;
            MarkAsModified();
        }
    }

    /// <summary>
    /// 生成新的唯一标识ID
    /// </summary>
    public void GenerateNewSaveID()
    {
        managerID = $"BackpackGridManager_{System.Guid.NewGuid().ToString("N")[..12]}_{GetInstanceID()}";
        MarkAsModified();
        Debug.Log($"生成新的BackpackGridManager ID: {managerID}");
    }

    /// <summary>
    /// 验证保存ID是否有效
    /// </summary>
    /// <returns>ID是否有效</returns>
    public bool IsSaveIDValid()
    {
        return !string.IsNullOrEmpty(managerID) && managerID.Length > 10;
    }

    /// <summary>
    /// 序列化管理器数据为JSON字符串
    /// </summary>
    /// <returns>序列化后的JSON字符串</returns>
    public string SerializeToJson()
    {
        try
        {
            var saveData = CreateSaveData();
            return JsonUtility.ToJson(saveData, true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"BackpackGridManager序列化失败: {ex.Message}");
            return "{}";
        }
    }

    /// <summary>
    /// 从JSON字符串反序列化管理器数据
    /// </summary>
    /// <param name="jsonData">JSON数据字符串</param>
    /// <returns>反序列化是否成功</returns>
    public bool DeserializeFromJson(string jsonData)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogWarning("BackpackGridManager反序列化数据为空");
                return false;
            }

            var saveData = JsonUtility.FromJson<BackpackGridManagerSaveData>(jsonData);
            return LoadFromSaveData(saveData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"BackpackGridManager反序列化失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 标记为已修改
    /// </summary>
    public void MarkAsModified()
    {
        isModified = true;
        UpdateLastModified();
    }

    /// <summary>
    /// 重置修改标记
    /// </summary>
    public void ResetModifiedFlag()
    {
        isModified = false;
    }

    /// <summary>
    /// 检查是否已修改
    /// </summary>
    /// <returns>是否已修改</returns>
    public bool IsModified()
    {
        return isModified;
    }

    /// <summary>
    /// 验证管理器数据的完整性
    /// </summary>
    /// <returns>数据是否有效</returns>
    public bool ValidateData()
    {
        // 验证管理器ID
        if (!IsSaveIDValid())
        {
            Debug.LogError("BackpackGridManager ID无效");
            return false;
        }

        // 验证关键组件引用
        if (backpackGrid == null)
        {
            Debug.LogError("BackpackGrid引用缺失");
            return false;
        }

        if (inventoryController == null)
        {
            Debug.LogError("InventoryController引用缺失");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取最后修改时间
    /// </summary>
    /// <returns>最后修改时间字符串</returns>
    public string GetLastModified()
    {
        return lastModified;
    }

    /// <summary>
    /// 更新最后修改时间为当前时间
    /// </summary>
    public void UpdateLastModified()
    {
        lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // ==================== 保存数据管理 ====================

    /// <summary>
    /// 创建管理器保存数据
    /// </summary>
    /// <returns>管理器保存数据</returns>
    public BackpackGridManagerSaveData CreateSaveData()
    {
        var saveData = new BackpackGridManagerSaveData
        {
            managerID = GetSaveID(),
            saveVersion = "1.0",
            lastModified = GetLastModified(),
            isModified = IsModified(),

            // 网格配置信息
            hasGridConfig = backpackGridConfig != null,
            gridConfigName = backpackGridConfig != null ? backpackGridConfig.name : "",

            // 网格状态信息
            hasBackpackGrid = backpackGrid != null,
            backpackGridID = backpackGrid != null && backpackGrid is ISaveable saveableGrid ? saveableGrid.GetSaveID() : "",

            // 控制器关联信息
            hasInventoryController = inventoryController != null,
            inventoryControllerID = inventoryController != null && inventoryController is ISaveable saveableController ? saveableController.GetSaveID() : "",

            // 状态信息
            isActiveGrid = IsActiveGrid(),
            isInitialized = this.isInitialized
        };

        return saveData;
    }

    /// <summary>
    /// 从保存数据加载管理器状态
    /// </summary>
    /// <param name="saveData">保存数据</param>
    /// <returns>加载是否成功</returns>
    public bool LoadFromSaveData(BackpackGridManagerSaveData saveData)
    {
        try
        {
            // 恢复基本信息
            managerID = saveData.managerID;
            lastModified = saveData.lastModified;
            isModified = saveData.isModified;

            // 恢复初始化状态
            isInitialized = saveData.isInitialized;

            // 注意：网格和控制器的引用恢复需要在场景加载完成后进行
            // 这里只记录ID，实际的引用恢复在RestoreReferences方法中处理

            Debug.Log($"BackpackGridManager数据加载成功: {managerID}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"BackpackGridManager数据加载失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 恢复对象引用（在场景加载完成后调用）
    /// </summary>
    /// <param name="saveData">保存数据</param>
    public void RestoreReferences(BackpackGridManagerSaveData saveData)
    {
        // 这个方法将在后续的网格保存实现中完善
        // 目前主要用于准备接口扩展
        Debug.Log($"BackpackGridManager引用恢复准备: {saveData.managerID}");
    }

    /// <summary>
    /// 在对象销毁时清理保存系统注册
    /// </summary>
    private void OnDestroy()
    {
        if (saveManager != null && !string.IsNullOrEmpty(managerID))
        {
            saveManager.UnregisterSaveable(managerID);
            Debug.Log($"BackpackGridManager已从保存系统注销: {managerID}");
        }
    }
}

// ==================== 保存数据结构 ====================

/// <summary>
/// 背包网格管理器保存数据结构
/// </summary>
[System.Serializable]
public class BackpackGridManagerSaveData
{
    [Header("基本信息")]
    public string managerID;                    // 管理器唯一ID
    public string saveVersion;                  // 保存版本
    public string lastModified;                 // 最后修改时间
    public bool isModified;                     // 是否已修改

    [Header("网格配置信息")]
    public bool hasGridConfig;                  // 是否有网格配置
    public string gridConfigName;               // 网格配置名称

    [Header("网格状态信息")]
    public bool hasBackpackGrid;                // 是否有背包网格
    public string backpackGridID;               // 背包网格ID

    [Header("控制器关联信息")]
    public bool hasInventoryController;         // 是否有库存控制器
    public string inventoryControllerID;        // 库存控制器ID

    [Header("状态信息")]
    public bool isActiveGrid;                   // 是否为活跃网格
    public bool isInitialized;                  // 是否已初始化
}