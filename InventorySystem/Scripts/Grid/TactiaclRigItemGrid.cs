// TacticalRigItemGrid.cs
// 战术挂具专用网格，继承 BaseItemGrid，支持运行时切换数据
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TactiaclRigItemGrid : BaseItemGrid
{
    [Header("战术挂具网格参数")]
    [SerializeField, Tooltip("默认宽度")] private int defaultWidth = 1;
    [SerializeField, Tooltip("默认高度")] private int defaultHeight = 1;

    // 当前战术挂具数据（运行时动态设置）
    private InventorySystemItemDataSO currentTacticalRigData;

    /* ---------------- 生命周期 ---------------- */
    protected override void Awake()
    {
        LoadFromTacticalRigData();
        base.Awake();
    }

    protected override void Start()
    {
        LoadFromTacticalRigData();
        base.Start();
    }

    protected override void OnValidate()
    {
        if (isUpdatingFromConfig) return;
        LoadFromTacticalRigData();
        width = Mathf.Clamp(width, 1, 20);
        height = Mathf.Clamp(height, 1, 20);
        base.OnValidate();
    }

    protected override void Init(int w, int h)
    {
        if (rectTransform == null) return;
        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;
        rectTransform.sizeDelta = new Vector2(w * cellSize, h * cellSize);
        if (Application.isPlaying) InitializeGridArrays();
    }

    /* ---------------- 动态数据 ---------------- */
    private void LoadFromTacticalRigData()
    {
        if (currentTacticalRigData != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = currentTacticalRigData.CellH;
            height = currentTacticalRigData.CellV;
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

    /// <summary>运行时更换战术挂具</summary>
    public void SetTacticalRigData(InventorySystemItemDataSO data)
    {
        currentTacticalRigData = data;
        LoadFromTacticalRigData();
        if (Application.isPlaying)
        {
            InitializeGridArrays();
            placedItems.Clear();
        }
        Init(width, height);
    }

    public InventorySystemItemDataSO GetTacticalRigData() => currentTacticalRigData;

    /// <summary>挂具占用率</summary>
    public float GetTacticalRigOccupancyRate() => GetOccupancyRate();

    /// <summary>挂具剩余格子数</summary>
    public int GetRemainingSpace() => width * height - occupiedCells.Count;


    

    // ==================== ISaveable接口扩展实现 ====================
    
    /// <summary>
    /// 生成战术挂具网格专用的唯一标识ID
    /// 格式：TacticalRig_[挂具名称]_[8位GUID]_[实例ID]
    /// </summary>
    public override void GenerateNewSaveID()
    {
        string rigName = "Unknown";
        if (currentTacticalRigData != null && !string.IsNullOrEmpty(currentTacticalRigData.itemName))
        {
            // 清理挂具名称，移除特殊字符
            rigName = currentTacticalRigData.itemName.Replace(" ", "").Replace("/", "").Replace("\\", "");
        }
        
        gridID = $"TacticalRig_{rigName}_{System.Guid.NewGuid().ToString("N")[..8]}_{GetInstanceID()}";
        MarkAsModified();
        
        Debug.Log($"战术挂具网格生成新ID: {gridID}");
    }
    
    /// <summary>
    /// 获取战术挂具网格的保存数据
    /// 包含挂具数据路径和动态尺寸信息
    /// </summary>
    public override BaseItemGridSaveData GetSaveData()
    {
        var saveData = base.GetSaveData();
        
        // 创建战术挂具专用的保存数据
        var tacticalRigSaveData = new TacticalRigGridSaveData
        {
            gridID = saveData.gridID,
            saveVersion = saveData.saveVersion,
            gridWidth = saveData.gridWidth,
            gridHeight = saveData.gridHeight,
            placedItems = saveData.placedItems,
            lastModified = saveData.lastModified,
            isModified = saveData.isModified,
            
            // 战术挂具特定数据
            tacticalRigDataPath = GetTacticalRigDataPath(),
            defaultWidth = defaultWidth,
            defaultHeight = defaultHeight,
            hasActiveTacticalRig = currentTacticalRigData != null
        };
        
        return tacticalRigSaveData;
    }
    
    /// <summary>
    /// 从保存数据加载战术挂具网格状态
    /// 恢复挂具数据关联和网格配置
    /// </summary>
    public override bool LoadSaveData(BaseItemGridSaveData saveData)
    {
        try
        {
            // 尝试转换为战术挂具专用保存数据
            if (saveData is TacticalRigGridSaveData tacticalRigData)
            {
                // 恢复战术挂具数据关联
                if (!string.IsNullOrEmpty(tacticalRigData.tacticalRigDataPath))
                {
                    var rigData = Resources.Load<InventorySystemItemDataSO>(tacticalRigData.tacticalRigDataPath);
                    if (rigData != null)
                    {
                        SetTacticalRigData(rigData);
                        Debug.Log($"成功恢复战术挂具数据: {tacticalRigData.tacticalRigDataPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"无法加载战术挂具数据: {tacticalRigData.tacticalRigDataPath}");
                        // 使用默认尺寸
                        defaultWidth = tacticalRigData.defaultWidth;
                        defaultHeight = tacticalRigData.defaultHeight;
                        LoadFromTacticalRigData();
                    }
                }
                else if (!tacticalRigData.hasActiveTacticalRig)
                {
                    // 没有活动挂具，使用默认配置
                    defaultWidth = tacticalRigData.defaultWidth;
                    defaultHeight = tacticalRigData.defaultHeight;
                    currentTacticalRigData = null;
                    LoadFromTacticalRigData();
                }
            }
            
            // 调用基类加载方法
            bool baseResult = base.LoadSaveData(saveData);
            
            if (baseResult)
            {
                Debug.Log($"战术挂具网格数据加载成功: {saveData.gridID}");
            }
            
            return baseResult;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"战术挂具网格数据加载失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 处理战术挂具卸载时的数据清理
    /// 清空网格内容并重置为默认状态
    /// </summary>
    public void OnTacticalRigUnequipped()
    {
        try
        {
            // 清空网格中的所有物品
            ClearGrid();
            
            // 清理物品实例ID映射
            itemInstanceMap.Clear();
            objectToInstanceID.Clear();
            
            // 重置为默认状态
            currentTacticalRigData = null;
            LoadFromTacticalRigData();
            
            // 重新初始化网格数组
            InitializeGridArrays();
            Init(width, height);
            
            // 标记为已修改并更新时间戳
            MarkAsModified();
            
            Debug.Log("战术挂具卸载，网格数据已清理");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"战术挂具卸载数据清理失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 获取当前战术挂具数据的资源路径
    /// </summary>
    /// <returns>挂具数据资源路径，如果没有则返回空字符串</returns>
    private string GetTacticalRigDataPath()
    {
        if (currentTacticalRigData == null) return "";
        
#if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(currentTacticalRigData);
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
    /// 验证战术挂具网格数据的完整性
    /// </summary>
    /// <returns>数据是否有效</returns>
    public override bool ValidateData()
    {
        // 调用基类验证
        if (!base.ValidateData())
        {
            return false;
        }
        
        // 验证战术挂具特定数据
        if (currentTacticalRigData != null)
        {
            // 验证挂具数据的有效性
            if (currentTacticalRigData.CellH <= 0 || currentTacticalRigData.CellV <= 0)
            {
                Debug.LogError("战术挂具数据中的网格尺寸无效");
                return false;
            }
            
            // 验证当前网格尺寸与挂具数据是否匹配
            if (width != currentTacticalRigData.CellH || height != currentTacticalRigData.CellV)
            {
                Debug.LogWarning("当前网格尺寸与战术挂具数据不匹配，将自动同步");
                LoadFromTacticalRigData();
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 初始化战术挂具网格的保存系统
    /// 在Awake中调用以确保保存系统正确初始化
    /// </summary>
    protected override void InitializeSaveSystem()
    {
        base.InitializeSaveSystem();
        
        // 如果没有有效ID，生成战术挂具专用ID
        if (!IsSaveIDValid())
        {
            GenerateNewSaveID();
        }
        
        Debug.Log($"战术挂具网格保存系统初始化完成: {GetSaveID()}");
    }
    
    // ==================== 战术挂具专用保存数据类 ====================
    
    [System.Serializable]
    public class TacticalRigGridSaveData : BaseItemGridSaveData
    {
        public string tacticalRigDataPath;    // 战术挂具数据资源路径
        public int defaultWidth;              // 默认宽度
        public int defaultHeight;             // 默认高度
        public bool hasActiveTacticalRig;     // 是否有活动的战术挂具
    }
}