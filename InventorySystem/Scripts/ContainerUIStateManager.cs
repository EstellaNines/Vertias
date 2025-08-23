// ContainerUIStateManager.cs
// 容器UI状态管理器 - 负责保存和恢复背包容器、战术挂具容器的UI状态信息
// 包括显示状态、位置、尺寸、可见性等UI相关属性
using UnityEngine;
using System.Collections.Generic;
using InventorySystem.SaveSystem;

/// <summary>
/// 容器UI状态管理器
/// 负责管理背包容器和战术挂具容器的UI状态保存和恢复
/// 实现ISaveable接口以支持保存系统集成
/// </summary>
public class ContainerUIStateManager : MonoBehaviour, ISaveable
{
    [Header("容器UI状态管理")]
    [SerializeField] private string managerID = "";                    // 管理器唯一ID
    [SerializeField] private bool autoGenerateID = true;              // 是否自动生成ID
    [SerializeField] private int saveVersion = 1;                     // 保存版本号

    [Header("背包容器引用")]
    [SerializeField] private Transform backpackContainer;             // 背包容器Transform
    [SerializeField] private RectTransform backpackContainerRect;     // 背包容器RectTransform
    [SerializeField] private Canvas backpackCanvas;                   // 背包Canvas

    [Header("战术挂具容器引用")]
    [SerializeField] private Transform tacticalRigContainer;          // 战术挂具容器Transform
    [SerializeField] private RectTransform tacticalRigContainerRect;  // 战术挂具容器RectTransform
    [SerializeField] private Canvas tacticalRigCanvas;                // 战术挂具Canvas

    [Header("状态跟踪")]
    [SerializeField] private bool isModified = false;                 // 是否已修改
    [SerializeField] private string lastModified = "";                // 最后修改时间

    // 保存系统相关
    private SaveManager saveManager;
    private bool isInitialized = false;

    // 容器状态缓存
    private Dictionary<string, ContainerUIState> containerStates = new Dictionary<string, ContainerUIState>();

    // 事件回调
    public System.Action<string> OnContainerStateChanged;
    public System.Action<string> OnSaveCompleted;
    public System.Action<string> OnLoadCompleted;

    void Awake()
    {
        InitializeManager();
    }

    void Start()
    {
        RegisterToSaveSystem();
        CacheInitialStates();
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
        if (containerStates == null)
            containerStates = new Dictionary<string, ContainerUIState>();

        // 自动查找容器引用（如果未手动设置）
        AutoFindContainerReferences();

        isInitialized = true;
        Debug.Log($"[ContainerUIStateManager] 管理器初始化完成，ID: {GetManagerID()}");
    }

    /// <summary>
    /// 自动查找容器引用
    /// </summary>
    private void AutoFindContainerReferences()
    {
        // 查找背包容器
        if (backpackContainer == null)
        {
            GameObject backpackObj = GameObject.Find("BackpackContainer");
            if (backpackObj != null)
            {
                backpackContainer = backpackObj.transform;
                backpackContainerRect = backpackObj.GetComponent<RectTransform>();
            }
        }

        // 查找战术挂具容器
        if (tacticalRigContainer == null)
        {
            GameObject tacticalRigObj = GameObject.Find("TacticalRigContainer");
            if (tacticalRigObj != null)
            {
                tacticalRigContainer = tacticalRigObj.transform;
                tacticalRigContainerRect = tacticalRigObj.GetComponent<RectTransform>();
            }
        }

        // 查找Canvas引用
        if (backpackCanvas == null && backpackContainer != null)
        {
            backpackCanvas = backpackContainer.GetComponentInParent<Canvas>();
        }

        if (tacticalRigCanvas == null && tacticalRigContainer != null)
        {
            tacticalRigCanvas = tacticalRigContainer.GetComponentInParent<Canvas>();
        }
    }

    /// <summary>
    /// 缓存初始状态
    /// </summary>
    private void CacheInitialStates()
    {
        // 缓存背包容器状态
        if (backpackContainer != null)
        {
            var backpackState = CaptureContainerState("Backpack", backpackContainer, backpackContainerRect, backpackCanvas);
            containerStates["Backpack"] = backpackState;
        }

        // 缓存战术挂具容器状态
        if (tacticalRigContainer != null)
        {
            var tacticalRigState = CaptureContainerState("TacticalRig", tacticalRigContainer, tacticalRigContainerRect, tacticalRigCanvas);
            containerStates["TacticalRig"] = tacticalRigState;
        }

        Debug.Log($"[ContainerUIStateManager] 已缓存 {containerStates.Count} 个容器的初始状态");
    }

    /// <summary>
    /// 捕获容器状态
    /// </summary>
    private ContainerUIState CaptureContainerState(string containerType, Transform container, RectTransform rectTransform, Canvas canvas)
    {
        var state = new ContainerUIState
        {
            containerType = containerType,
            isActive = container.gameObject.activeInHierarchy,
            localPosition = container.localPosition,
            localRotation = container.localRotation,
            localScale = container.localScale
        };

        // 捕获RectTransform信息
        if (rectTransform != null)
        {
            state.anchoredPosition = rectTransform.anchoredPosition;
            state.sizeDelta = rectTransform.sizeDelta;
            state.anchorMin = rectTransform.anchorMin;
            state.anchorMax = rectTransform.anchorMax;
            state.pivot = rectTransform.pivot;
            state.hasRectTransform = true;
        }

        // 捕获Canvas信息
        if (canvas != null)
        {
            state.canvasSortingOrder = canvas.sortingOrder;
            state.canvasEnabled = canvas.enabled;
            state.hasCanvas = true;
        }

        state.lastCaptured = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return state;
    }

    /// <summary>
    /// 更新容器状态
    /// </summary>
    public void UpdateContainerState(string containerType)
    {
        Transform container = null;
        RectTransform rectTransform = null;
        Canvas canvas = null;

        // 根据容器类型获取引用
        switch (containerType)
        {
            case "Backpack":
                container = backpackContainer;
                rectTransform = backpackContainerRect;
                canvas = backpackCanvas;
                break;
            case "TacticalRig":
                container = tacticalRigContainer;
                rectTransform = tacticalRigContainerRect;
                canvas = tacticalRigCanvas;
                break;
            default:
                Debug.LogWarning($"[ContainerUIStateManager] 未知的容器类型: {containerType}");
                return;
        }

        if (container != null)
        {
            var newState = CaptureContainerState(containerType, container, rectTransform, canvas);
            containerStates[containerType] = newState;

            MarkAsModified();
            OnContainerStateChanged?.Invoke(containerType);

            Debug.Log($"[ContainerUIStateManager] 已更新容器状态: {containerType}");
        }
    }

    /// <summary>
    /// 恢复容器状态
    /// </summary>
    public bool RestoreContainerState(string containerType)
    {
        if (!containerStates.ContainsKey(containerType))
        {
            Debug.LogWarning($"[ContainerUIStateManager] 未找到容器状态: {containerType}");
            return false;
        }

        var state = containerStates[containerType];
        Transform container = null;
        RectTransform rectTransform = null;
        Canvas canvas = null;

        // 根据容器类型获取引用
        switch (containerType)
        {
            case "Backpack":
                container = backpackContainer;
                rectTransform = backpackContainerRect;
                canvas = backpackCanvas;
                break;
            case "TacticalRig":
                container = tacticalRigContainer;
                rectTransform = tacticalRigContainerRect;
                canvas = tacticalRigCanvas;
                break;
            default:
                Debug.LogWarning($"[ContainerUIStateManager] 未知的容器类型: {containerType}");
                return false;
        }

        if (container == null)
        {
            Debug.LogWarning($"[ContainerUIStateManager] 容器引用为空: {containerType}");
            return false;
        }

        try
        {
            // 恢复基本Transform信息
            container.gameObject.SetActive(state.isActive);
            container.localPosition = state.localPosition;
            container.localRotation = state.localRotation;
            container.localScale = state.localScale;

            // 恢复RectTransform信息
            if (state.hasRectTransform && rectTransform != null)
            {
                rectTransform.anchoredPosition = state.anchoredPosition;
                rectTransform.sizeDelta = state.sizeDelta;
                rectTransform.anchorMin = state.anchorMin;
                rectTransform.anchorMax = state.anchorMax;
                rectTransform.pivot = state.pivot;
            }

            // 恢复Canvas信息
            if (state.hasCanvas && canvas != null)
            {
                canvas.sortingOrder = state.canvasSortingOrder;
                canvas.enabled = state.canvasEnabled;
            }

            Debug.Log($"[ContainerUIStateManager] 已恢复容器状态: {containerType}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ContainerUIStateManager] 恢复容器状态时发生错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取容器状态信息
    /// </summary>
    public ContainerUIState GetContainerState(string containerType)
    {
        return containerStates.ContainsKey(containerType) ? containerStates[containerType] : null;
    }

    /// <summary>
    /// 获取所有容器状态
    /// </summary>
    public Dictionary<string, ContainerUIState> GetAllContainerStates()
    {
        return new Dictionary<string, ContainerUIState>(containerStates);
    }

    /// <summary>
    /// 清除所有容器状态
    /// </summary>
    public void ClearAllStates()
    {
        containerStates.Clear();
        MarkAsModified();
        Debug.Log("[ContainerUIStateManager] 已清除所有容器状态");
    }

    // ==================== ID管理 ====================

    /// <summary>
    /// 生成新的管理器ID
    /// </summary>
    private void GenerateNewManagerID()
    {
        managerID = $"ContainerUIManager_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}_{GetInstanceID()}";
        MarkAsModified();
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
            MarkAsModified();
        }
    }

    /// <summary>
    /// 生成新的保存ID (ISaveable接口实现)
    /// </summary>
    public void GenerateNewSaveID()
    {
        GenerateNewManagerID();
    }

    /// <summary>
    /// 重置修改标志 (ISaveable接口实现)
    /// </summary>
    public void ResetModifiedFlag()
    {
        // 容器UI状态管理器总是被认为是修改过的
        Debug.Log($"[ContainerUIStateManager] 重置修改标志: {GetManagerID()}");
    }

    /// <summary>
    /// 检查是否已修改 (ISaveable接口实现)
    /// </summary>
    /// <returns>是否已修改</returns>
    public bool IsModified()
    {
        // 容器UI状态管理器总是返回true，因为UI状态可能随时变化
        return true;
    }

    /// <summary>
    /// 获取最后修改时间 (ISaveable接口实现)
    /// </summary>
    /// <returns>最后修改时间字符串</returns>
    public string GetLastModified()
    {
        return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 更新最后修改时间 (ISaveable接口实现)
    /// </summary>
    public void UpdateLastModified()
    {
        // 容器UI状态管理器的修改时间由UI状态变化决定
        Debug.Log($"[ContainerUIStateManager] 更新最后修改时间: {GetLastModified()}");
    }

    /// <summary>
    /// 验证管理器ID是否有效
    /// </summary>
    public bool IsManagerIDValid()
    {
        return !string.IsNullOrEmpty(managerID) && managerID.Length >= 8;
    }

    // ==================== ISaveable接口实现 ====================

    public string GetSaveID()
    {
        return GetManagerID();
    }

    public void SetSaveID(string newID)
    {
        SetManagerID(newID);
    }

    public bool IsSaveIDValid()
    {
        return IsManagerIDValid();
    }

    public string SerializeToJson()
    {
        try
        {
            var saveData = new ContainerUIStateManagerSaveData
            {
                managerID = this.managerID,
                saveVersion = this.saveVersion,
                lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                isModified = this.isModified,
                containerStates = new List<ContainerUIState>(containerStates.Values)
            };

            return JsonUtility.ToJson(saveData, true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ContainerUIStateManager] 序列化时发生错误: {ex.Message}");
            return null;
        }
    }

    public bool DeserializeFromJson(string jsonData)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError("[ContainerUIStateManager] 保存数据为空");
                return false;
            }

            var saveData = JsonUtility.FromJson<ContainerUIStateManagerSaveData>(jsonData);
            if (saveData == null)
            {
                Debug.LogError("[ContainerUIStateManager] 反序列化保存数据失败");
                return false;
            }

            // 验证数据版本
            if (saveData.saveVersion > saveVersion)
            {
                Debug.LogWarning($"[ContainerUIStateManager] 保存数据版本 ({saveData.saveVersion}) 高于当前版本 ({saveVersion})，可能存在兼容性问题");
            }

            // 恢复管理器状态
            this.managerID = saveData.managerID;
            this.isModified = saveData.isModified;
            this.lastModified = saveData.lastModified;

            // 恢复容器状态
            containerStates.Clear();
            if (saveData.containerStates != null)
            {
                foreach (var state in saveData.containerStates)
                {
                    containerStates[state.containerType] = state;
                }
            }

            Debug.Log($"[ContainerUIStateManager] 已加载保存数据，恢复了 {containerStates.Count} 个容器状态");
            OnLoadCompleted?.Invoke(GetManagerID());
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ContainerUIStateManager] 加载保存数据时发生错误: {ex.Message}");
            return false;
        }
    }

    public bool ValidateData()
    {
        try
        {
            // 验证管理器ID
            if (!IsManagerIDValid())
            {
                Debug.LogError("[ContainerUIStateManager] 管理器ID无效");
                return false;
            }

            // 验证容器状态数据
            foreach (var kvp in containerStates)
            {
                var state = kvp.Value;
                if (state == null)
                {
                    Debug.LogError($"[ContainerUIStateManager] 容器状态为空: {kvp.Key}");
                    return false;
                }

                if (string.IsNullOrEmpty(state.containerType))
                {
                    Debug.LogError($"[ContainerUIStateManager] 容器类型为空: {kvp.Key}");
                    return false;
                }
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ContainerUIStateManager] 验证数据时发生错误: {ex.Message}");
            return false;
        }
    }

    public void MarkAsModified()
    {
        isModified = true;
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
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
            saveManager.RegisterSaveable(this);
            Debug.Log($"[ContainerUIStateManager] 已注册到保存系统: {GetManagerID()}");
        }
        else
        {
            Debug.LogWarning("[ContainerUIStateManager] SaveManager实例未找到，无法注册到保存系统");
        }
    }

    /// <summary>
    /// 从保存系统注销
    /// </summary>
    private void UnregisterFromSaveSystem()
    {
        if (saveManager != null)
        {
            saveManager.UnregisterSaveable(this);
            Debug.Log($"[ContainerUIStateManager] 已从保存系统注销: {GetManagerID()}");
        }
    }

    void OnDestroy()
    {
        UnregisterFromSaveSystem();

        // 清理事件回调
        OnContainerStateChanged = null;
        OnSaveCompleted = null;
        OnLoadCompleted = null;

        Debug.Log($"[ContainerUIStateManager] 管理器已销毁: {managerID}");
    }
}

// ==================== 数据结构定义 ====================

/// <summary>
/// 容器UI状态数据结构
/// </summary>
[System.Serializable]
public class ContainerUIState
{
    [Header("基本信息")]
    public string containerType;           // 容器类型（Backpack/TacticalRig）
    public bool isActive;                   // 是否激活
    public string lastCaptured;            // 最后捕获时间

    [Header("Transform信息")]
    public Vector3 localPosition;          // 本地位置
    public Quaternion localRotation;       // 本地旋转
    public Vector3 localScale;             // 本地缩放

    [Header("RectTransform信息")]
    public bool hasRectTransform;          // 是否有RectTransform组件
    public Vector2 anchoredPosition;       // 锚点位置
    public Vector2 sizeDelta;              // 尺寸增量
    public Vector2 anchorMin;              // 最小锚点
    public Vector2 anchorMax;              // 最大锚点
    public Vector2 pivot;                  // 轴心点

    [Header("Canvas信息")]
    public bool hasCanvas;                 // 是否有Canvas组件
    public int canvasSortingOrder;         // Canvas排序顺序
    public bool canvasEnabled;             // Canvas是否启用
}

/// <summary>
/// 容器UI状态管理器保存数据结构
/// </summary>
[System.Serializable]
public class ContainerUIStateManagerSaveData
{
    [Header("管理器信息")]
    public string managerID;               // 管理器唯一ID
    public int saveVersion;                // 保存版本号
    public string lastModified;            // 最后修改时间
    public bool isModified;                // 是否已修改

    [Header("容器状态数据")]
    public List<ContainerUIState> containerStates; // 所有容器状态列表
}