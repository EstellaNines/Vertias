using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ItemGrid : BaseItemGrid
{
    [SerializeField][FieldLabel("网格系统宽度格数")] private int inventoryWidth = 10;
    [SerializeField][FieldLabel("网格系统高度格数")] private int inventoryHeight = 30;
    [SerializeField] protected bool showDebugInfo = true; // 调试信息显示开关

    protected override void Awake()
    {
        // 确保在base.Awake()之前加载配置
        LoadFromGridConfig();
        base.Awake();
    }

    public void LoadFromGridConfig()
    {
        if (gridConfig != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            inventoryWidth = gridConfig.inventoryWidth;
            inventoryHeight = gridConfig.inventoryHeight;
            width = inventoryWidth;
            height = inventoryHeight;
            
            // 强制更新网格数组
            InitializeGridArrays();
            
            isUpdatingFromConfig = false;
            
            // 移除showDebugInfo引用，直接使用Debug.Log
            Debug.Log($"从GridConfig加载尺寸: {inventoryWidth}x{inventoryHeight}");
        }
    }

    protected override void Start()
    {
        LoadFromGridConfig();
        base.Start();
    }

    protected override void OnValidate()
    {
        if (isUpdatingFromConfig) return;

        inventoryWidth = Mathf.Clamp(inventoryWidth, 1, 50);
        inventoryHeight = Mathf.Clamp(inventoryHeight, 1, 50);

        width = inventoryWidth;
        height = inventoryHeight;

        base.OnValidate();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SaveToGridConfigDelayed();
        }
#endif
    }

#if UNITY_EDITOR
    private void SaveToGridConfigDelayed()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            SaveToGridConfig();
        };
    }
#endif

    protected override void Init(int width, int height)
    {
        if (rectTransform == null) return;

        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;

        Vector2 size = new Vector2(
            width * cellSize,
            height * cellSize
        );
        rectTransform.sizeDelta = size;
    }

    // 删除重复的LoadFromGridConfig方法，只保留上面的一个

    public void SaveToGridConfig()
    {
#if UNITY_EDITOR
        if (gridConfig != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;

            bool hasChanged = false;
            if (gridConfig.inventoryWidth != inventoryWidth)
            {
                gridConfig.inventoryWidth = inventoryWidth;
                hasChanged = true;
            }
            if (gridConfig.inventoryHeight != inventoryHeight)
            {
                gridConfig.inventoryHeight = inventoryHeight;
                hasChanged = true;
            }

            if (hasChanged)
            {
                EditorUtility.SetDirty(gridConfig);
                AssetDatabase.SaveAssets();
            }

            isUpdatingFromConfig = false;
        }
#endif
    }

    public void SyncToGridConfig()
    {
        SaveToGridConfig();
    }

    // ===== ISaveable接口扩展实现 =====

    /// <summary>
    /// 仓库网格保存数据类（继承基类）
    /// </summary>
    [System.Serializable]
    public class ItemGridSaveData : BaseItemGridSaveData
    {
        // 静态网格配置数据
        public int configInventoryWidth; // 配置文件中的宽度
        public int configInventoryHeight; // 配置文件中的高度
        public string gridConfigPath; // GridConfig资源路径
        public bool isStaticGrid; // 是否为静态网格
        
        // 网格状态数据
        public bool isConfigSynced; // 配置是否已同步
        public float lastConfigUpdateTime; // 最后配置更新时间
        public string gridDescription; // 网格描述信息
        public Vector2 gridWorldPosition; // 网格在世界中的位置
    }

    // 静态网格配置缓存
    private bool isStaticGrid = true;
    private float lastConfigUpdateTime = 0f;
    private string gridDescription = "";

    /// <summary>
    /// 重写生成新的保存ID方法
    /// 格式: StorageGrid_[网格名称]_[宽度x高度]_[8位GUID]_[实例ID]
    /// </summary>
    public override void GenerateNewSaveID()
    {
        string gridName = gameObject.name.Replace(" ", "").Replace("(", "").Replace(")", "");
        string gridSize = $"{inventoryWidth}x{inventoryHeight}";
        string guidPart = System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        int instanceID = GetInstanceID();
        string newSaveID = $"StorageGrid_{gridName}_{gridSize}_{guidPart}_{instanceID}";
        SetSaveID(newSaveID);
        
        if (Application.isPlaying && showDebugInfo)
        {
            Debug.Log($"为仓库网格生成新的保存ID: {newSaveID}");
        }
    }

    /// <summary>
    /// 重写获取保存数据方法
    /// </summary>
    public override BaseItemGridSaveData GetSaveData()
    {
        // 先获取基类数据
        BaseItemGridSaveData baseData = base.GetSaveData();
        
        // 创建ItemGrid专用的保存数据
        ItemGridSaveData saveData = new ItemGridSaveData();
        
        // 继承基类数据
        saveData.gridID = baseData.gridID;
        saveData.saveVersion = baseData.saveVersion;
        saveData.gridWidth = baseData.gridWidth;
        saveData.gridHeight = baseData.gridHeight;
        saveData.placedItems = baseData.placedItems;
        saveData.lastModified = baseData.lastModified;
        saveData.isModified = baseData.isModified;
        
        // 保存静态网格配置数据
        saveData.configInventoryWidth = inventoryWidth;
        saveData.configInventoryHeight = inventoryHeight;
        saveData.gridConfigPath = gridConfig != null ? GetGridConfigAssetPath() : "";
        saveData.isStaticGrid = isStaticGrid;
        
        // 保存网格状态数据
        saveData.isConfigSynced = IsConfigSynced();
        saveData.lastConfigUpdateTime = lastConfigUpdateTime;
        saveData.gridDescription = gridDescription;
        saveData.gridWorldPosition = new Vector2(transform.position.x, transform.position.z);
        
        return saveData;
    }

    /// <summary>
    /// 重写加载保存数据方法
    /// </summary>
    public override bool LoadSaveData(BaseItemGridSaveData data)
    {
        if (data is ItemGridSaveData saveData)
        {
            try
            {
                // 先调用基类加载方法
                bool baseResult = base.LoadSaveData(saveData);
                if (!baseResult)
                {
                    return false;
                }
                
                // 恢复静态网格配置
                if (saveData.configInventoryWidth > 0 && saveData.configInventoryHeight > 0)
                {
                    inventoryWidth = saveData.configInventoryWidth;
                    inventoryHeight = saveData.configInventoryHeight;
                    width = inventoryWidth;
                    height = inventoryHeight;
                }
                
                // 恢复GridConfig引用
                if (!string.IsNullOrEmpty(saveData.gridConfigPath))
                {
                    RestoreGridConfigReference(saveData.gridConfigPath);
                }
                
                // 恢复网格状态数据
                isStaticGrid = saveData.isStaticGrid;
                lastConfigUpdateTime = saveData.lastConfigUpdateTime;
                gridDescription = saveData.gridDescription ?? "";
                
                // 恢复世界位置（如果需要）
                if (saveData.gridWorldPosition != Vector2.zero)
                {
                    Vector3 worldPos = new Vector3(saveData.gridWorldPosition.x, transform.position.y, saveData.gridWorldPosition.y);
                    transform.position = worldPos;
                }
                
                // 重新初始化网格数组
                InitializeGridArrays();
                
                if (showDebugInfo)
                {
                    Debug.Log($"成功加载仓库网格保存数据: {GetSaveID()}, 尺寸: {inventoryWidth}x{inventoryHeight}");
                }
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载仓库网格保存数据失败: {e.Message}");
                return false;
            }
        }
        else
        {
            // 如果不是ItemGridSaveData，尝试调用基类方法
            return base.LoadSaveData(data);
        }
    }

    /// <summary>
    /// 获取GridConfig资源路径
    /// </summary>
    private string GetGridConfigAssetPath()
    {
        if (gridConfig == null) return "";
        
#if UNITY_EDITOR
        return AssetDatabase.GetAssetPath(gridConfig);
#else
        return gridConfig.name; // 运行时返回资源名称
#endif
    }

    /// <summary>
    /// 恢复GridConfig引用
    /// </summary>
    private void RestoreGridConfigReference(string configPath)
    {
        if (string.IsNullOrEmpty(configPath)) return;
        
#if UNITY_EDITOR
        GridConfig loadedConfig = AssetDatabase.LoadAssetAtPath<GridConfig>(configPath);
        if (loadedConfig != null)
        {
            gridConfig = loadedConfig;
            if (showDebugInfo)
            {
                Debug.Log($"成功恢复GridConfig引用: {configPath}");
            }
        }
        else
        {
            Debug.LogWarning($"无法找到GridConfig资源: {configPath}");
        }
#else
        // 运行时通过Resources.Load或Addressables加载
        GridConfig loadedConfig = Resources.Load<GridConfig>(configPath);
        if (loadedConfig != null)
        {
            gridConfig = loadedConfig;
        }
#endif
    }

    /// <summary>
    /// 检查配置是否已同步
    /// </summary>
    private bool IsConfigSynced()
    {
        if (gridConfig == null) return false;
        
        return gridConfig.inventoryWidth == inventoryWidth && 
               gridConfig.inventoryHeight == inventoryHeight;
    }

    /// <summary>
    /// 保存静态网格配置到GridConfig
    /// </summary>
    public void SaveStaticGridConfig()
    {
        if (gridConfig != null && isStaticGrid)
        {
            bool hasChanged = false;
            
            if (gridConfig.inventoryWidth != inventoryWidth)
            {
                gridConfig.inventoryWidth = inventoryWidth;
                hasChanged = true;
            }
            
            if (gridConfig.inventoryHeight != inventoryHeight)
            {
                gridConfig.inventoryHeight = inventoryHeight;
                hasChanged = true;
            }
            
            if (hasChanged)
            {
                lastConfigUpdateTime = Time.time;
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(gridConfig);
                AssetDatabase.SaveAssets();
#endif
                
                if (showDebugInfo)
                {
                    Debug.Log($"已保存静态网格配置: {inventoryWidth}x{inventoryHeight}");
                }
            }
        }
    }

    /// <summary>
    /// 从GridConfig加载静态配置
    /// </summary>
    public void LoadStaticGridConfig()
    {
        if (gridConfig != null && isStaticGrid)
        {
            bool hasChanged = false;
            
            if (inventoryWidth != gridConfig.inventoryWidth)
            {
                inventoryWidth = gridConfig.inventoryWidth;
                width = inventoryWidth;
                hasChanged = true;
            }
            
            if (inventoryHeight != gridConfig.inventoryHeight)
            {
                inventoryHeight = gridConfig.inventoryHeight;
                height = inventoryHeight;
                hasChanged = true;
            }
            
            if (hasChanged)
            {
                lastConfigUpdateTime = Time.time;
                InitializeGridArrays();
                
                if (showDebugInfo)
                {
                    Debug.Log($"已从GridConfig加载静态配置: {inventoryWidth}x{inventoryHeight}");
                }
            }
        }
    }

    /// <summary>
    /// 设置网格描述
    /// </summary>
    public void SetGridDescription(string description)
    {
        gridDescription = description ?? "";
        
        if (showDebugInfo)
        {
            Debug.Log($"设置网格描述: {gridDescription}");
        }
    }

    /// <summary>
    /// 获取网格描述
    /// </summary>
    public string GetGridDescription()
    {
        return gridDescription;
    }

    /// <summary>
    /// 设置是否为静态网格
    /// </summary>
    public void SetStaticGrid(bool isStatic)
    {
        isStaticGrid = isStatic;
        
        if (showDebugInfo)
        {
            Debug.Log($"设置静态网格状态: {isStaticGrid}");
        }
    }

    /// <summary>
    /// 获取是否为静态网格
    /// </summary>
    public bool IsStaticGrid()
    {
        return isStaticGrid;
    }

    /// <summary>
    /// 重写验证数据方法
    /// </summary>
    public override bool ValidateData()
    {
        bool isValid = base.ValidateData();
        
        // 验证网格尺寸
        if (inventoryWidth <= 0 || inventoryHeight <= 0)
        {
            inventoryWidth = Mathf.Max(1, inventoryWidth);
            inventoryHeight = Mathf.Max(1, inventoryHeight);
            width = inventoryWidth;
            height = inventoryHeight;
            isValid = false;
            Debug.LogWarning("网格尺寸无效，已重置为最小值");
        }
        
        // 验证GridConfig引用
        if (gridConfig == null)
        {
            Debug.LogWarning("GridConfig引用为空，某些功能可能无法正常工作");
        }
        
        // 验证配置同步状态
        if (gridConfig != null && !IsConfigSynced())
        {
            Debug.LogWarning("网格配置与GridConfig不同步");
        }
        
        return isValid;
    }

    /// <summary>
    /// 重写初始化保存系统方法
    /// </summary>
    protected override void InitializeSaveSystem()
    {
        base.InitializeSaveSystem();
        
        // 初始化静态网格特有属性
        if (string.IsNullOrEmpty(gridDescription))
        {
            gridDescription = $"仓库网格 {inventoryWidth}x{inventoryHeight}";
        }
        
        lastConfigUpdateTime = Time.time;
        
        if (showDebugInfo)
        {
            Debug.Log($"仓库网格保存系统初始化完成: {GetSaveID()}, 静态网格: {isStaticGrid}");
        }
    }

    /// <summary>
    /// 获取网格状态摘要方法
    /// </summary>
    public string GetGridStatusSummary()
    {
        string baseInfo = $"网格ID: {GetSaveID()}, 尺寸: {inventoryWidth}x{inventoryHeight}, 物品数量: {placedItems.Count}";
        string configSync = IsConfigSynced() ? "已同步" : "未同步";
        string staticStatus = isStaticGrid ? "静态" : "动态";
        string configPath = gridConfig != null ? GetGridConfigAssetPath() : "无";
        
        return $"{baseInfo}, 配置:{configSync}, 类型:{staticStatus}, 配置文件:{configPath}";
    }

    /// <summary>
    /// 获取网格配置信息摘要
    /// </summary>
    public string GetGridConfigSummary()
    {
        if (gridConfig == null)
        {
            return "无配置文件";
        }
        
        string configSize = $"{gridConfig.inventoryWidth}x{gridConfig.inventoryHeight}";
        string currentSize = $"{inventoryWidth}x{inventoryHeight}";
        string syncStatus = IsConfigSynced() ? "同步" : "不同步";
        
        return $"配置尺寸:{configSize}, 当前尺寸:{currentSize}, 状态:{syncStatus}";
    }

    /// <summary>
    /// 强制同步到GridConfig
    /// </summary>
    public void ForceSyncToGridConfig()
    {
        if (gridConfig != null)
        {
            SaveStaticGridConfig();
            
            if (showDebugInfo)
            {
                Debug.Log("已强制同步到GridConfig");
            }
        }
    }

    /// <summary>
    /// 强制从GridConfig同步
    /// </summary>
    public void ForceSyncFromGridConfig()
    {
        if (gridConfig != null)
        {
            LoadStaticGridConfig();
            
            if (showDebugInfo)
            {
                Debug.Log("已强制从GridConfig同步");
            }
        }
    }

}
