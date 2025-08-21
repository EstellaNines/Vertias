// BackpackItemGrid.cs
// 背包专用网格，直接继承 BaseItemGrid 并支持运行时切换背包数据
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class BackpackItemGrid : BaseItemGrid
{
    [Header("背包网格参数")]
    [SerializeField, Tooltip("默认宽度")] private int defaultWidth = 6;
    [SerializeField, Tooltip("默认高度")] private int defaultHeight = 8;

    // 当前背包数据（运行时动态设置）
    private InventorySystemItemDataSO currentBackpackData;

    /* ---------------- 生命周期 ---------------- */
    protected override void Awake()
    {
        LoadFromBackpackData();
        base.Awake();
    }

    protected override void Start()
    {
        LoadFromBackpackData();
        base.Start();
    }

    protected override void OnValidate()
    {
        if (isUpdatingFromConfig) return;
        LoadFromBackpackData();
        width = Mathf.Clamp(width, 1, 50);
        height = Mathf.Clamp(height, 1, 50);
        base.OnValidate();
    }

    protected override void Init(int w, int h)
    {
        if (rectTransform == null) return;
        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;
        rectTransform.sizeDelta = new Vector2(w * cellSize, h * cellSize);
        if (Application.isPlaying) InitializeGridArrays();
    }

    /* ---------------- 动态背包 ---------------- */
    private void LoadFromBackpackData()
    {
        if (currentBackpackData != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = currentBackpackData.CellH;
            height = currentBackpackData.CellV;
            isUpdatingFromConfig = false;
        }
        else if (!isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = defaultWidth;
            height = defaultHeight;
            isUpdatingFromConfig = false;
        }
    }

    /// <summary>运行时更换背包（换装更大的背包）</summary>
    public void SetBackpackData(InventorySystemItemDataSO data)
    {
        currentBackpackData = data;
        LoadFromBackpackData();
        if (Application.isPlaying)
        {
            InitializeGridArrays();
            placedItems.Clear();
        }
        Init(width, height);
    }

    public InventorySystemItemDataSO GetCurrentBackpackData() => currentBackpackData;

    /// <summary>背包占用率</summary>
    public float GetBackpackOccupancyRate() => GetOccupancyRate();

    /// <summary>背包剩余格子数</summary>
    public int GetRemainingSpace() => width * height - occupiedCells.Count;




    // ==================== ISaveable接口扩展实现 ====================
    
    /// <summary>
    /// 生成背包网格专用的唯一标识ID
    /// 格式：Backpack_[背包名称]_[8位GUID]_[实例ID]
    /// </summary>
    public override void GenerateNewSaveID()
    {
        string backpackName = "Unknown";
        if (currentBackpackData != null && !string.IsNullOrEmpty(currentBackpackData.itemName))
        {
            // 清理背包名称，移除特殊字符
            backpackName = currentBackpackData.itemName.Replace(" ", "").Replace("/", "").Replace("\\", "");
        }
        
        gridID = $"Backpack_{backpackName}_{System.Guid.NewGuid().ToString("N")[..8]}_{GetInstanceID()}";
        MarkAsModified();
        
        Debug.Log($"背包网格生成新ID: {gridID}");
    }
    
    /// <summary>
    /// 获取背包网格的保存数据
    /// 包含背包数据路径和动态尺寸信息
    /// </summary>
    public override BaseItemGridSaveData GetSaveData()
    {
        var saveData = base.GetSaveData();
        
        // 创建背包专用的保存数据
        var backpackSaveData = new BackpackGridSaveData
        {
            gridID = saveData.gridID,
            saveVersion = saveData.saveVersion,
            gridWidth = saveData.gridWidth,
            gridHeight = saveData.gridHeight,
            placedItems = saveData.placedItems,
            lastModified = saveData.lastModified,
            isModified = saveData.isModified,
            
            // 背包特定数据
            backpackDataPath = GetBackpackDataPath(),
            defaultWidth = defaultWidth,
            defaultHeight = defaultHeight,
            hasActiveBackpack = currentBackpackData != null,
            occupancyRate = GetBackpackOccupancyRate()
        };
        
        return backpackSaveData;
    }
    
    /// <summary>
    /// 从保存数据加载背包网格状态
    /// 恢复背包数据关联和网格配置
    /// </summary>
    public override bool LoadSaveData(BaseItemGridSaveData saveData)
    {
        try
        {
            // 尝试转换为背包专用保存数据
            if (saveData is BackpackGridSaveData backpackData)
            {
                // 恢复背包数据关联
                if (!string.IsNullOrEmpty(backpackData.backpackDataPath))
                {
                    var backpackItemData = Resources.Load<InventorySystemItemDataSO>(backpackData.backpackDataPath);
                    if (backpackItemData != null)
                    {
                        SetBackpackData(backpackItemData);
                        Debug.Log($"成功恢复背包数据: {backpackData.backpackDataPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"无法加载背包数据: {backpackData.backpackDataPath}");
                        // 使用默认尺寸
                        defaultWidth = backpackData.defaultWidth;
                        defaultHeight = backpackData.defaultHeight;
                        LoadFromBackpackData();
                    }
                }
                else if (!backpackData.hasActiveBackpack)
                {
                    // 没有活动背包，使用默认配置
                    defaultWidth = backpackData.defaultWidth;
                    defaultHeight = backpackData.defaultHeight;
                    currentBackpackData = null;
                    LoadFromBackpackData();
                }
            }
            
            // 调用基类加载方法
            bool baseResult = base.LoadSaveData(saveData);
            
            if (baseResult)
            {
                Debug.Log($"背包网格数据加载成功: {saveData.gridID}");
            }
            
            return baseResult;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"背包网格数据加载失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 处理背包卸载时的数据清理
    /// 清空网格内容并重置为默认状态
    /// </summary>
    public void OnBackpackUnequipped()
    {
        try
        {
            // 清空网格中的所有物品
            ClearGrid();
            
            // 清理物品实例ID映射
            itemInstanceMap.Clear();
            objectToInstanceID.Clear();
            
            // 重置为默认状态
            currentBackpackData = null;
            LoadFromBackpackData();
            
            // 重新初始化网格数组
            InitializeGridArrays();
            Init(width, height);
            
            // 标记为已修改并更新时间戳
            MarkAsModified();
            
            Debug.Log("背包卸载，网格数据已清理");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"背包卸载数据清理失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 获取当前背包数据的资源路径
    /// </summary>
    /// <returns>背包数据资源路径，如果没有则返回空字符串</returns>
    private string GetBackpackDataPath()
    {
        if (currentBackpackData == null) return "";
        
#if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(currentBackpackData);
        if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/Resources/"))
        {
            // 转换为Resources.Load可用的路径
            string resourcePath = assetPath.Substring("Assets/Resources/".Length);
            if (resourcePath.EndsWith(".asset"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - ".asset".Length);
            }
            return resourcePath;
        }
#endif
        return "";
    }
    
    /// <summary>
    /// 验证背包网格数据的完整性
    /// </summary>
    /// <returns>数据是否有效</returns>
    public override bool ValidateData()
    {
        // 调用基类验证
        if (!base.ValidateData())
        {
            return false;
        }
        
        // 验证背包特定数据
        if (currentBackpackData != null)
        {
            // 验证背包数据的有效性
            if (currentBackpackData.CellH <= 0 || currentBackpackData.CellV <= 0)
            {
                Debug.LogError("背包数据中的网格尺寸无效");
                return false;
            }
            
            // 验证当前网格尺寸与背包数据是否匹配
            if (width != currentBackpackData.CellH || height != currentBackpackData.CellV)
            {
                Debug.LogWarning("当前网格尺寸与背包数据不匹配，将自动同步");
                LoadFromBackpackData();
            }
        }
        
        // 验证占用率是否合理（不应超过100%）
        float occupancyRate = GetBackpackOccupancyRate();
        if (occupancyRate > 1.0f)
        {
            Debug.LogError($"背包占用率异常: {occupancyRate * 100:F1}%");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 初始化背包网格的保存系统
    /// 在Awake中调用以确保保存系统正确初始化
    /// </summary>
    protected override void InitializeSaveSystem()
    {
        base.InitializeSaveSystem();
        
        // 如果没有有效ID，生成背包专用ID
        if (!IsSaveIDValid())
        {
            GenerateNewSaveID();
        }
        
        Debug.Log($"背包网格保存系统初始化完成: {GetSaveID()}");
    }
    
    /// <summary>
    /// 获取背包状态摘要信息
    /// 用于调试和状态监控
    /// </summary>
    public string GetBackpackStatusSummary()
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"背包网格ID: {GetSaveID()}");
        summary.AppendLine($"背包数据: {(currentBackpackData != null ? currentBackpackData.itemName : "无")}");
        summary.AppendLine($"网格尺寸: {width}x{height}");
        summary.AppendLine($"占用率: {GetBackpackOccupancyRate() * 100:F1}%");
        summary.AppendLine($"剩余空间: {GetRemainingSpace()} 格");
        summary.AppendLine($"已放置物品数: {placedItems.Count}");
        summary.AppendLine($"最后修改: {GetLastModified()}");
        return summary.ToString();
    }
    
    // ==================== 背包专用保存数据类 ====================
    
    [System.Serializable]
    public class BackpackGridSaveData : BaseItemGridSaveData
    {
        public string backpackDataPath;       // 背包数据资源路径
        public int defaultWidth;              // 默认宽度
        public int defaultHeight;             // 默认高度
        public bool hasActiveBackpack;        // 是否有活动的背包
        public float occupancyRate;           // 占用率快照
    }
}