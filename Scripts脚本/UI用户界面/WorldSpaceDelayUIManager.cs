using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 世界空间延迟UI管理器
/// 负责统一管理所有货架的延迟显示UI，确保一致的显示机制
/// </summary>
public class WorldSpaceDelayUIManager : MonoBehaviour
{
    [Header("世界空间Canvas设置")]
    [SerializeField][FieldLabel("世界空间Canvas")] private Canvas worldSpaceCanvas;
    [SerializeField][FieldLabel("延迟UI预制体")] private GameObject delayUIPrefab;
    [SerializeField][FieldLabel("跟随玩家头顶距离")] private float offsetAbovePlayer = 2f;
    [SerializeField][FieldLabel("UI到玩家的水平距离")] private float horizontalDistanceFromPlayer = 0f;
    [SerializeField][FieldLabel("UI缩放因子")] private float uiScale = 0.01f;
    
    [Header("UI池设置")]
    [SerializeField][FieldLabel("预创建UI数量")] private int preCreateCount = 5;
    [SerializeField][FieldLabel("最大UI数量")] private int maxUICount = 10;
    
    [Header("调试设置")]
    [SerializeField][FieldLabel("启用调试日志")] private bool enableDebugLogs = true;
    [SerializeField][FieldLabel("显示UI边界")] private bool showUIBounds = false;
    
    // 单例实例
    public static WorldSpaceDelayUIManager Instance { get; private set; }
    
    // 私有变量
    private Transform playerTransform;
    private Camera playerCamera;
    private List<DelayMagnifierUIController> delayUIPool = new List<DelayMagnifierUIController>();
    private Dictionary<GameObject, DelayMagnifierUIController> activeDelayUIs = new Dictionary<GameObject, DelayMagnifierUIController>();
    
    // 当前激活的延迟UI
    private DelayMagnifierUIController currentActiveDelayUI;
    private GameObject currentTriggerObject;
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 实现单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        StartCoroutine(DelayedInitialization());
    }
    
    private void Update()
    {
        UpdateWorldSpaceUIPositions();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化管理器
    /// </summary>
    private void InitializeManager()
    {
        LogDebug("初始化世界空间延迟UI管理器");
        
        // 验证世界空间Canvas设置
        ValidateWorldSpaceCanvas();
        
        // 查找玩家对象
        FindPlayerReferences();
        
        // 创建UI池
        CreateDelayUIPool();
    }
    
    /// <summary>
    /// 延迟初始化（确保所有对象都已加载）
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialization()
    {
        yield return new WaitForSeconds(0.5f);
        
        // 重新查找玩家引用
        if (playerTransform == null || playerCamera == null)
        {
            FindPlayerReferences();
        }
        
        LogDebug($"延迟初始化完成 - 玩家: {(playerTransform != null ? "找到" : "未找到")}, 相机: {(playerCamera != null ? "找到" : "未找到")}");
    }
    
    /// <summary>
    /// 验证世界空间Canvas设置
    /// </summary>
    private void ValidateWorldSpaceCanvas()
    {
        if (worldSpaceCanvas == null)
        {
            LogError("世界空间Canvas未设置！正在自动创建...");
            CreateWorldSpaceCanvas();
        }
        else
        {
            // 确保Canvas设置为世界空间模式
            if (worldSpaceCanvas.renderMode != RenderMode.WorldSpace)
            {
                LogWarning("Canvas不是世界空间模式，正在修正...");
                worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
                worldSpaceCanvas.worldCamera = Camera.main;
            }
            
            LogDebug($"世界空间Canvas验证完成: {worldSpaceCanvas.name}");
        }
    }
    
    /// <summary>
    /// 创建世界空间Canvas
    /// </summary>
    private void CreateWorldSpaceCanvas()
    {
        GameObject canvasGO = new GameObject("WorldSpaceDelayUICanvas");
        canvasGO.transform.SetParent(transform);
        
        worldSpaceCanvas = canvasGO.AddComponent<Canvas>();
        worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
        worldSpaceCanvas.worldCamera = Camera.main;
        worldSpaceCanvas.sortingOrder = 100;
        
        // 添加CanvasScaler - 对于世界空间，使用Constant Pixel Size模式
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        
        // 添加GraphicRaycaster
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // 设置Canvas大小和位置 - 使用更小的尺寸以提高精度
        RectTransform rectTransform = canvasGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1000, 1000); // 像素单位，但会被缩放
        rectTransform.localScale = Vector3.one * uiScale; // 通过缩放控制实际大小
        
        LogDebug("自动创建世界空间Canvas完成");
    }
    
    /// <summary>
    /// 查找玩家引用
    /// </summary>
    private void FindPlayerReferences()
    {
        // 查找玩家对象
        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                playerTransform = playerGO.transform;
                LogDebug($"找到玩家对象: {playerGO.name}");
            }
            else
            {
                LogWarning("未找到带有'Player'标签的游戏对象");
            }
        }
        
        // 查找玩家相机
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null && playerTransform != null)
            {
                playerCamera = playerTransform.GetComponentInChildren<Camera>();
            }
            
            if (playerCamera != null)
            {
                LogDebug($"找到玩家相机: {playerCamera.name}");
                
                // 设置世界空间Canvas的相机引用
                if (worldSpaceCanvas != null)
                {
                    worldSpaceCanvas.worldCamera = playerCamera;
                }
            }
            else
            {
                LogWarning("未找到玩家相机");
            }
        }
    }
    
    /// <summary>
    /// 创建延迟UI池
    /// </summary>
    private void CreateDelayUIPool()
    {
        if (delayUIPrefab == null)
        {
            LogError("延迟UI预制体未设置！");
            return;
        }
        
        for (int i = 0; i < preCreateCount; i++)
        {
            CreateDelayUIInstance();
        }
        
        LogDebug($"延迟UI池创建完成，预创建数量: {delayUIPool.Count}");
    }
    
    /// <summary>
    /// 创建延迟UI实例
    /// </summary>
    private DelayMagnifierUIController CreateDelayUIInstance()
    {
        GameObject uiGO = Instantiate(delayUIPrefab, worldSpaceCanvas.transform);
        DelayMagnifierUIController delayUI = uiGO.GetComponent<DelayMagnifierUIController>();
        
        if (delayUI == null)
        {
            LogError("延迟UI预制体缺少DelayMagnifierUIController组件！");
            Destroy(uiGO);
            return null;
        }
        
        // 初始化为隐藏状态
        delayUI.HideImmediate();
        uiGO.SetActive(false);
        
        delayUIPool.Add(delayUI);
        return delayUI;
    }
    
    #endregion
    
    #region 公共API
    
    /// <summary>
    /// 为货架触发器获取延迟UI
    /// </summary>
    /// <param name="triggerObject">货架触发器对象</param>
    /// <returns>延迟UI控制器</returns>
    public DelayMagnifierUIController GetDelayUIForShelf(GameObject triggerObject)
    {
        if (triggerObject == null) return null;
        
        // 检查是否已有激活的UI
        if (activeDelayUIs.ContainsKey(triggerObject))
        {
            return activeDelayUIs[triggerObject];
        }
        
        // 从池中获取可用的UI
        DelayMagnifierUIController availableUI = GetAvailableUIFromPool();
        if (availableUI == null) return null;
        
        // 配置UI
        ConfigureDelayUIForShelf(availableUI, triggerObject);
        
        // 添加到激活列表
        activeDelayUIs[triggerObject] = availableUI;
        
        LogDebug($"为货架 {triggerObject.name} 分配延迟UI");
        return availableUI;
    }
    
    /// <summary>
    /// 释放货架的延迟UI
    /// </summary>
    /// <param name="triggerObject">货架触发器对象</param>
    public void ReleaseDelayUIForShelf(GameObject triggerObject)
    {
        if (triggerObject == null || !activeDelayUIs.ContainsKey(triggerObject)) return;
        
        DelayMagnifierUIController delayUI = activeDelayUIs[triggerObject];
        
        // 停止和隐藏UI
        delayUI.Cancel();
        delayUI.HideImmediate();
        delayUI.gameObject.SetActive(false);
        
        // 从激活列表移除
        activeDelayUIs.Remove(triggerObject);
        
        // 如果是当前激活的UI，清除引用
        if (currentActiveDelayUI == delayUI)
        {
            currentActiveDelayUI = null;
            currentTriggerObject = null;
        }
        
        LogDebug($"释放货架 {triggerObject.name} 的延迟UI");
    }
    
    /// <summary>
    /// 开始延迟显示
    /// </summary>
    /// <param name="triggerObject">货架触发器对象</param>
    /// <param name="delayDuration">延迟时长</param>
    /// <param name="onComplete">完成回调</param>
    /// <param name="onCancel">取消回调</param>
    public void StartDelayDisplay(GameObject triggerObject, float delayDuration, System.Action onComplete = null, System.Action onCancel = null)
    {
        DelayMagnifierUIController delayUI = GetDelayUIForShelf(triggerObject);
        if (delayUI == null) return;
        
        // 设置当前激活的UI
        currentActiveDelayUI = delayUI;
        currentTriggerObject = triggerObject;
        
        // 更新UI位置
        UpdateDelayUIPosition(delayUI, triggerObject);
        
        // 开始延迟显示
        delayUI.StartDelay(delayDuration, 
            () => {
                LogDebug($"货架 {triggerObject.name} 延迟完成");
                onComplete?.Invoke();
            }, 
            () => {
                LogDebug($"货架 {triggerObject.name} 延迟被取消");
                onCancel?.Invoke();
            }
        );
        
        LogDebug($"开始货架 {triggerObject.name} 的延迟显示，时长: {delayDuration:F1}秒");
    }
    
    /// <summary>
    /// 取消延迟显示
    /// </summary>
    /// <param name="triggerObject">货架触发器对象</param>
    public void CancelDelayDisplay(GameObject triggerObject)
    {
        if (!activeDelayUIs.ContainsKey(triggerObject)) return;
        
        DelayMagnifierUIController delayUI = activeDelayUIs[triggerObject];
        delayUI.Cancel();
        
        LogDebug($"取消货架 {triggerObject.name} 的延迟显示");
    }
    
    /// <summary>
    /// 隐藏所有延迟UI（背包打开时调用）
    /// </summary>
    public void HideAllDelayUIs()
    {
        foreach (var kvp in activeDelayUIs)
        {
            kvp.Value.Cancel();
            kvp.Value.HideImmediate();
        }
        
        currentActiveDelayUI = null;
        currentTriggerObject = null;
        
        LogDebug("隐藏所有延迟UI");
    }
    
    /// <summary>
    /// 检查是否有延迟UI正在显示
    /// </summary>
    /// <returns>是否有UI在延迟中</returns>
    public bool IsAnyDelayUIActive()
    {
        return activeDelayUIs.Values.Any(ui => ui.IsDelaying());
    }
    
    /// <summary>
    /// 获取当前激活的延迟UI
    /// </summary>
    /// <returns>当前激活的延迟UI</returns>
    public DelayMagnifierUIController GetCurrentActiveDelayUI()
    {
        return currentActiveDelayUI;
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 从池中获取可用的UI
    /// </summary>
    /// <returns>可用的延迟UI</returns>
    private DelayMagnifierUIController GetAvailableUIFromPool()
    {
        // 查找未使用的UI
        DelayMagnifierUIController availableUI = delayUIPool.FirstOrDefault(ui => !ui.gameObject.activeInHierarchy);
        
        if (availableUI == null && delayUIPool.Count < maxUICount)
        {
            // 池中没有可用的，且未达到最大数量，创建新的
            availableUI = CreateDelayUIInstance();
        }
        
        return availableUI;
    }
    
    /// <summary>
    /// 为货架配置延迟UI
    /// </summary>
    /// <param name="delayUI">延迟UI</param>
    /// <param name="triggerObject">货架触发器对象</param>
    private void ConfigureDelayUIForShelf(DelayMagnifierUIController delayUI, GameObject triggerObject)
    {
        if (delayUI == null || triggerObject == null) return;
        
        // 激活UI
        delayUI.gameObject.SetActive(true);
        
        // 更新位置
        UpdateDelayUIPosition(delayUI, triggerObject);
        
        LogDebug($"为货架 {triggerObject.name} 配置延迟UI完成");
    }
    
    /// <summary>
    /// 更新延迟UI位置
    /// </summary>
    /// <param name="delayUI">延迟UI</param>
    /// <param name="triggerObject">货架触发器对象</param>
    private void UpdateDelayUIPosition(DelayMagnifierUIController delayUI, GameObject triggerObject)
    {
        if (delayUI == null || triggerObject == null || playerTransform == null || playerCamera == null) return;
        
        // 首先更新Canvas位置以跟随玩家
        UpdateCanvasPosition();
        
        // 计算目标位置：玩家头顶偏上
        Vector3 playerHeadPosition = playerTransform.position + Vector3.up * offsetAbovePlayer;
        
        // 添加水平偏移（如果需要的话）
        if (horizontalDistanceFromPlayer > 0)
        {
            Vector3 cameraToPlayer = (playerTransform.position - playerCamera.transform.position).normalized;
            cameraToPlayer.y = 0; // 保持水平
            playerHeadPosition += cameraToPlayer * horizontalDistanceFromPlayer;
        }
        
        // 将世界坐标转换为Canvas本地坐标
        Vector3 canvasLocalPosition = worldSpaceCanvas.transform.InverseTransformPoint(playerHeadPosition);
        
        // 设置UI的本地位置
        RectTransform uiRect = delayUI.GetComponent<RectTransform>();
        uiRect.localPosition = canvasLocalPosition;
        
        // 让UI朝向相机（使用世界坐标系）
        Vector3 lookDirection = (playerCamera.transform.position - playerHeadPosition).normalized;
        uiRect.rotation = Quaternion.LookRotation(-lookDirection); // 负号是为了面向相机
        
        // 调试信息
        if (enableDebugLogs)
        {
            LogDebug($"玩家位置: {playerTransform.position}");
            LogDebug($"目标世界位置: {playerHeadPosition}");
            LogDebug($"Canvas本地位置: {canvasLocalPosition}");
            LogDebug($"UI最终位置: {uiRect.position}");
        }
    }
    
    /// <summary>
    /// 更新Canvas位置以跟随玩家区域
    /// </summary>
    private void UpdateCanvasPosition()
    {
        if (worldSpaceCanvas == null || playerTransform == null) return;
        
        // Canvas跟随玩家，但保持一定距离以避免遮挡视野
        Vector3 canvasPosition = playerTransform.position;
        
        // Canvas稍微向上偏移，以确保UI显示在合适的高度
        canvasPosition.y += offsetAbovePlayer * 0.5f;
        
        worldSpaceCanvas.transform.position = canvasPosition;
        
        // 确保Canvas面向相机
        if (playerCamera != null)
        {
            Vector3 lookDirection = (playerCamera.transform.position - canvasPosition).normalized;
            worldSpaceCanvas.transform.rotation = Quaternion.LookRotation(-lookDirection);
        }
    }
    
    /// <summary>
    /// 更新所有世界空间UI的位置
    /// </summary>
    private void UpdateWorldSpaceUIPositions()
    {
        if (playerTransform == null || playerCamera == null) return;
        
        // 更新当前激活的延迟UI位置
        if (currentActiveDelayUI != null && currentTriggerObject != null)
        {
            UpdateDelayUIPosition(currentActiveDelayUI, currentTriggerObject);
        }
    }
    
    #endregion
    
    #region 调试方法
    
    /// <summary>
    /// 调试日志
    /// </summary>
    /// <param name="message">日志消息</param>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUIManager]</color> {message}");
        }
    }
    
    /// <summary>
    /// 警告日志
    /// </summary>
    /// <param name="message">警告消息</param>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"<color=#FF9800>[WorldSpaceDelayUIManager]</color> {message}");
    }
    
    /// <summary>
    /// 错误日志
    /// </summary>
    /// <param name="message">错误消息</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=#F44336>[WorldSpaceDelayUIManager]</color> {message}");
    }
    
    /// <summary>
    /// 获取管理器状态信息
    /// </summary>
    /// <returns>状态信息</returns>
    [ContextMenu("显示管理器状态")]
    public string GetManagerStatus()
    {
        string status = $"=== 世界空间延迟UI管理器状态 ===\n" +
                       $"Canvas: {(worldSpaceCanvas != null ? worldSpaceCanvas.name : "未设置")}\n" +
                       $"玩家: {(playerTransform != null ? playerTransform.name : "未找到")}\n" +
                       $"相机: {(playerCamera != null ? playerCamera.name : "未找到")}\n" +
                       $"UI池数量: {delayUIPool.Count}\n" +
                       $"激活UI数量: {activeDelayUIs.Count}\n" +
                       $"当前激活延迟: {(currentActiveDelayUI != null ? "是" : "否")}";
        
        LogDebug(status);
        return status;
    }
    
    /// <summary>
    /// 测试延迟UI创建
    /// </summary>
    [ContextMenu("测试创建延迟UI")]
    public void TestCreateDelayUI()
    {
        if (playerTransform == null)
        {
            LogWarning("无法测试：玩家对象未找到");
            return;
        }
        
        // 创建临时测试对象
        GameObject testShelf = new GameObject("TestShelf");
        testShelf.transform.position = playerTransform.position + Vector3.forward * 2f;
        
        // 开始3秒延迟测试
        StartDelayDisplay(testShelf, 3f, 
            () => LogDebug("测试延迟完成！"),
            () => LogDebug("测试延迟被取消！")
        );
        
        // 5秒后清理测试对象
        Destroy(testShelf, 5f);
    }
    
    /// <summary>
    /// 测试UI定位精度
    /// </summary>
    [ContextMenu("测试UI定位精度")]
    public void TestUIPositionAccuracy()
    {
        if (playerTransform == null || playerCamera == null)
        {
            LogWarning("无法测试：玩家或相机未找到");
            return;
        }
        
        LogDebug("=== UI定位精度测试 ===");
        
        // 测试各种位置
        Vector3[] testPositions = {
            playerTransform.position + Vector3.forward * 5f,   // 前方
            playerTransform.position + Vector3.back * 5f,      // 后方
            playerTransform.position + Vector3.left * 5f,      // 左侧
            playerTransform.position + Vector3.right * 5f,     // 右侧
            playerTransform.position + Vector3.forward * 10f + Vector3.right * 10f // 边界位置
        };
        
        string[] positionNames = { "前方", "后方", "左侧", "右侧", "边界" };
        
        for (int i = 0; i < testPositions.Length; i++)
        {
            // 临时移动玩家到测试位置
            Vector3 originalPosition = playerTransform.position;
            playerTransform.position = testPositions[i];
            
            // 计算预期的UI位置
            Vector3 expectedUIPosition = testPositions[i] + Vector3.up * offsetAbovePlayer;
            
            // 更新Canvas位置
            UpdateCanvasPosition();
            
            // 计算Canvas本地坐标
            Vector3 canvasLocalPosition = worldSpaceCanvas.transform.InverseTransformPoint(expectedUIPosition);
            
            LogDebug($"位置测试 [{positionNames[i]}]:");
            LogDebug($"  玩家位置: {testPositions[i]}");
            LogDebug($"  预期UI世界位置: {expectedUIPosition}");
            LogDebug($"  Canvas位置: {worldSpaceCanvas.transform.position}");
            LogDebug($"  Canvas本地坐标: {canvasLocalPosition}");
            
            // 还原玩家位置
            playerTransform.position = originalPosition;
        }
        
        // 更新Canvas位置到原始状态
        UpdateCanvasPosition();
        
        LogDebug("=== 测试完成 ===");
    }
    
    #endregion
    
    #region Gizmos绘制
    
    private void OnDrawGizmos()
    {
        if (!showUIBounds || playerTransform == null) return;
        
        // 绘制玩家头顶UI目标位置
        Gizmos.color = Color.green;
        Vector3 playerHeadPosition = playerTransform.position + Vector3.up * offsetAbovePlayer;
        Gizmos.DrawWireSphere(playerHeadPosition, 0.3f);
        
        // 绘制Canvas位置
        if (worldSpaceCanvas != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(worldSpaceCanvas.transform.position, Vector3.one * 0.5f);
            
            // 绘制从Canvas到目标位置的连线
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(worldSpaceCanvas.transform.position, playerHeadPosition);
        }
        
        // 绘制水平偏移（如果有）
        if (horizontalDistanceFromPlayer > 0 && playerCamera != null)
        {
            Vector3 cameraToPlayer = (playerTransform.position - playerCamera.transform.position).normalized;
            cameraToPlayer.y = 0;
            Vector3 offsetPosition = playerHeadPosition + cameraToPlayer * horizontalDistanceFromPlayer;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(offsetPosition, 0.2f);
            Gizmos.DrawLine(playerHeadPosition, offsetPosition);
        }
        
        // 绘制相机到玩家的视线
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playerCamera.transform.position, playerTransform.position);
        }
    }
    
    #endregion
}
