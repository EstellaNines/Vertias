// ShelfTrigger.cs
using UnityEngine;
using InventorySystem;
using InventorySystem.SpawnSystem;

public class ShelfTrigger : BaseContainerTrigger
{
    public static bool isInShelf = false; // 全局状态
    
    [Header("背包设置")]
    [SerializeField] private BackpackState backpackState;
    
    [Header("跨场景调试")]
    [SerializeField] private bool debugCrossScene = true;
    
    [Header("随机物品生成设置")]
    [SerializeField] private bool enableRandomGeneration = true;
    [SerializeField] private RandomItemSpawnConfig randomConfig;
    [SerializeField] private ItemGrid targetItemGrid;
    // 移除未使用的字段 - 生成逻辑已改为在Container网格创建后触发
    // [SerializeField] private bool generateOnFirstOpen = true;
    [SerializeField] private bool debugRandomGeneration = false;
    
    // 运行时状态
    private string assignedShelfId;
    private bool hasTriggeredGeneration = false;
    
    /// <summary>
    /// 获取分配的货架编号（用于外部访问）
    /// </summary>
    public string AssignedShelfId => assignedShelfId;

    protected override bool IsContainerOpen()
    {
        return backpackState != null && backpackState.IsBackpackOpen();
    }

    protected override void ToggleContainer()
    {
        if (backpackState != null)
        {
            // 注意：货架触发器不需要主动处理Tab键切换
            // Tab键由正常输入系统处理，货架只负责设置isInShelf状态标志
            // 这个方法仅在需要程序化切换时调用（如F键，但已被移除）
            if (debugCrossScene) Debug.Log($"<color=#AB47BC>[ShelfTrigger]</color> <color=#E0E0E0>程序化切换货架状态，isInShelf={isInShelf}</color>");
            
            if (backpackState.IsBackpackOpen())
            {
                backpackState.ForceCloseBackpack();
            }
            else
            {
                // 使用和Tab键相同的打开方式
                if (backpackState.topNavigationTransform != null)
                {
                    backpackState.topNavigationTransform.ToggleBackpack();
                }
                else
                {
                    backpackState.ForceOpenBackpack();
                }
            }
        }
    }

    protected override void ForceCloseContainer()
    {
        if (backpackState != null)
        {
            backpackState.ForceCloseBackpack();
        }
    }

    protected override string GetContainerTypeName()
    {
        return "货架";
    }

    protected override void OnPlayerEnterTrigger(Collider2D playerCollider)
    {
        base.OnPlayerEnterTrigger(playerCollider);
        
        // ✨ 跨场景保险：再次确保BackpackState引用有效
        EnsureBackpackStateReference();
        
        isInShelf = true;
        
        // 分配货架编号（如果还没有分配）
        if (string.IsNullOrEmpty(assignedShelfId))
        {
            assignedShelfId = ShelfNumberingSystem.GetOrAssignShelfNumber(gameObject);
            if (debugRandomGeneration)
            {
                Debug.Log($"[ShelfTrigger] 分配货架编号: {assignedShelfId}");
            }
        }
        
        // 更新UI文本为货架专用文本
        if (tmpText != null)
        {
            tmpText.text = "[Tab] Search";
        }
        
        // 跨场景验证：确认BackpackState连接状态
        if (debugCrossScene)
        {
            bool hasValidBackpackState = (backpackState != null && backpackState.gameObject != null);
            var tag = "<color=#AB47BC>[ShelfTrigger]</color>"; // 紫色标签
            Debug.Log($"{tag} <color=#9FA8DA>进入触发器</color> <color=#90CAF9>AssignedShelfId={assignedShelfId}</color> <color=#80CBC4>BackpackState有效={hasValidBackpackState}</color>");
            
            if (hasValidBackpackState)
            {
                Debug.Log($"{tag} <color=#81C784>连接到</color> <color=#FFF176>{backpackState.gameObject.name}</color> <color=#B39DDB>Tab应正常工作</color>");
            }
            else
            {
                Debug.LogError($"{tag} <color=#EF9A9A>BackpackState连接失败</color> <color=#FFCDD2>Tab可能异常</color>");
            }
        }
    }

    protected override void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        base.OnPlayerExitTrigger(playerCollider);
        isInShelf = false;
    }
    
    
    #region 随机物品生成
    
    /// <summary>
    /// 尝试触发随机物品生成（已弃用，改为在Container网格创建后触发）
    /// </summary>
    [System.Obsolete("此方法已弃用，随机生成现在在Container网格创建后自动触发")]
    private void TryTriggerRandomGeneration()
    {
        // 此方法已不再使用，生成逻辑已移动到OnContainerGridCreated方法
        if (debugRandomGeneration)
        {
            Debug.Log($"[ShelfTrigger] {assignedShelfId}: TryTriggerRandomGeneration已弃用，生成将在Container网格创建后自动触发");
        }
    }
    
    /// <summary>
    /// 执行随机物品生成
    /// </summary>
    private void GenerateRandomItems()
    {
        var manager = ShelfRandomItemManager.Instance;
        if (manager == null)
        {
            if (debugRandomGeneration)
            {
                Debug.LogError($"[ShelfTrigger] {assignedShelfId}: ShelfRandomItemManager实例不存在");
            }
            return;
        }
        
        if (debugRandomGeneration)
        {
            Debug.Log($"[ShelfTrigger] {assignedShelfId}: 开始生成随机物品");
        }
        
        bool success = manager.TryGenerateRandomItems(gameObject, targetItemGrid, randomConfig);
        
        if (success)
        {
            hasTriggeredGeneration = true;
            if (debugRandomGeneration)
            {
                Debug.Log($"[ShelfTrigger] {assignedShelfId}: 随机生成请求已发送");
            }
        }
        else
        {
            if (debugRandomGeneration)
            {
                Debug.LogWarning($"[ShelfTrigger] {assignedShelfId}: 随机生成请求失败");
            }
        }
    }
    
    /// <summary>
    /// 强制重新生成物品
    /// </summary>
    [ContextMenu("强制重新生成物品")]
    public void ForceRegenerateItems()
    {
        if (!enableRandomGeneration || randomConfig == null || targetItemGrid == null)
        {
            Debug.LogWarning($"[ShelfTrigger] {assignedShelfId}: 无法重新生成，配置不完整");
            return;
        }
        
        var manager = ShelfRandomItemManager.Instance;
        if (manager == null)
        {
            Debug.LogError($"[ShelfTrigger] {assignedShelfId}: ShelfRandomItemManager实例不存在");
            return;
        }
        
        hasTriggeredGeneration = false;
        bool success = manager.ForceRegenerateItems(gameObject, targetItemGrid, randomConfig);
        
        if (success)
        {
            hasTriggeredGeneration = true;
            Debug.Log($"[ShelfTrigger] {assignedShelfId}: 强制重新生成成功");
        }
        else
        {
            Debug.LogWarning($"[ShelfTrigger] {assignedShelfId}: 强制重新生成失败");
        }
    }
    
    /// <summary>
    /// 获取独立的容器标识符，确保每个货架有独立的存档
    /// </summary>
    public string GetUniqueContainerIdentifier()
    {
        if (string.IsNullOrEmpty(assignedShelfId))
        {
            assignedShelfId = ShelfNumberingSystem.GetOrAssignShelfNumber(gameObject);
        }
        return $"shelf_container_{assignedShelfId}";
    }
    
    /// <summary>
    /// 当Container网格创建完成后，尝试触发随机物品生成
    /// </summary>
    /// <param name="containerGrid">创建的Container网格</param>
    public void OnContainerGridCreated(ItemGrid containerGrid)
    {
        if (!enableRandomGeneration || hasTriggeredGeneration)
        {
            if (debugRandomGeneration)
            {
                Debug.Log($"[ShelfTrigger] {assignedShelfId}: 跳过生成 - 启用:{enableRandomGeneration}, 已触发:{hasTriggeredGeneration}");
            }
            return;
        }
        
        if (randomConfig == null)
        {
            Debug.LogWarning($"[ShelfTrigger] {gameObject.name}: 启用了随机生成但未设置配置");
            return;
        }
        
        if (targetItemGrid == null)
        {
            Debug.LogWarning($"[ShelfTrigger] {gameObject.name}: 启用了随机生成但未设置目标ItemGrid");
            return;
        }
        
        // 使用传入的containerGrid而不是targetItemGrid，因为targetItemGrid是预制体引用
        if (debugRandomGeneration)
        {
            Debug.Log($"[ShelfTrigger] {assignedShelfId}: Container网格已创建，开始生成随机物品");
        }
        
        // 执行随机物品生成，使用实际创建的网格
        GenerateRandomItemsForGrid(containerGrid);
        hasTriggeredGeneration = true;
    }
    
    /// <summary>
    /// 为指定网格生成随机物品
    /// </summary>
    /// <param name="itemGrid">目标网格</param>
    private void GenerateRandomItemsForGrid(ItemGrid itemGrid)
    {
        var manager = ShelfRandomItemManager.Instance;
        if (manager == null)
        {
            Debug.LogError($"[ShelfTrigger] {assignedShelfId}: ShelfRandomItemManager实例不存在");
            return;
        }
        
        if (debugRandomGeneration)
        {
            Debug.Log($"[ShelfTrigger] {assignedShelfId}: 开始为网格生成随机物品");
        }
        
        bool success = manager.TryGenerateRandomItems(gameObject, itemGrid, randomConfig);
        
        if (success)
        {
            if (debugRandomGeneration)
            {
                Debug.Log($"[ShelfTrigger] {assignedShelfId}: 随机生成请求已发送");
            }
        }
        else
        {
            if (debugRandomGeneration)
            {
                Debug.LogWarning($"[ShelfTrigger] {assignedShelfId}: 随机生成请求失败");
            }
        }
    }
    
    /// <summary>
    /// 获取货架状态信息
    /// </summary>
    [ContextMenu("显示货架状态")]
    public void ShowShelfStatus()
    {
        var manager = ShelfRandomItemManager.Instance;
        if (manager != null)
        {
            var status = manager.GetShelfStatus(gameObject);
            Debug.Log($"[ShelfTrigger] 货架状态:\n" +
                     $"ID: {status.shelfId}\n" +
                     $"已生成: {status.isGenerated}\n" +
                     $"正在生成: {status.isGenerating}\n" +
                     $"有配置覆盖: {status.hasConfigOverride}\n" +
                     $"触发过生成: {hasTriggeredGeneration}");
        }
        else
        {
            Debug.LogWarning("[ShelfTrigger] ShelfRandomItemManager实例不存在");
        }
    }
    
    /// <summary>
    /// 重置触发状态
    /// </summary>
    [ContextMenu("重置触发状态")]
    public void ResetTriggerState()
    {
        hasTriggeredGeneration = false;
        Debug.Log($"[ShelfTrigger] {assignedShelfId}: 触发状态已重置");
    }
    
    #endregion
    
    #region Unity生命周期
    
    protected override void Start()
    {
        // 调用基类的Start方法
        base.Start();
        
        // ✨ 跨场景修复：动态查找BackpackState，解决场景切换后引用失效问题
        EnsureBackpackStateReference();
        
        // 验证配置
        ValidateConfiguration();
        
        // 如果启用了随机生成但没有配置，尝试使用默认配置
        if (enableRandomGeneration && randomConfig == null)
        {
            var manager = ShelfRandomItemManager.Instance;
            if (manager != null)
            {
                // 这里可以设置使用默认配置的逻辑
                if (debugRandomGeneration)
                {
                    Debug.LogWarning($"[ShelfTrigger] {gameObject.name}: 没有设置随机配置，将使用管理器默认配置");
                }
            }
        }
    }
    
    private void OnValidate()
    {
        // 编辑器中验证配置
        ValidateConfiguration();
    }
    
    /// <summary>
    /// 确保BackpackState引用有效（跨场景修复）
    /// 这解决了从Shelter场景切换到Mall场景后，ShelfTrigger的backpackState引用失效的问题
    /// </summary>
    private void EnsureBackpackStateReference()
    {
        // 检查当前引用是否有效
        if (backpackState != null && backpackState.gameObject != null)
        {
            if (debugCrossScene)
            {
                Debug.Log($"[ShelfTrigger] {assignedShelfId}: BackpackState引用有效: {backpackState.gameObject.name}");
            }
            return; // 引用有效，无需重新查找
        }
        
        if (debugCrossScene)
        {
            Debug.LogWarning($"[ShelfTrigger] {assignedShelfId}: BackpackState引用无效，开始跨场景动态查找");
        }
        
        // 方法1：通过BackpackSystemManager获取
        var systemManager = BackpackSystemManager.Instance;
        if (systemManager != null)
        {
            var foundBackpackState = systemManager.GetBackpackState();
            if (foundBackpackState != null)
            {
                backpackState = foundBackpackState;
                if (debugCrossScene)
                {
                    Debug.Log($"[ShelfTrigger] {assignedShelfId}: 通过BackpackSystemManager找到BackpackState: {backpackState.gameObject.name}");
                }
                return;
            }
        }
        
        // 方法2：直接在场景中查找（DontDestroyOnLoad对象）
        var foundStates = FindObjectsOfType<BackpackState>(true); // 包括非激活对象
        if (foundStates != null && foundStates.Length > 0)
        {
            // 优先选择DontDestroyOnLoad的对象
            foreach (var state in foundStates)
            {
                if (state.gameObject.scene.name == "DontDestroyOnLoad" || 
                    state.transform.root.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    backpackState = state;
                    if (debugCrossScene)
                    {
                        Debug.Log($"[ShelfTrigger] {assignedShelfId}: 找到DontDestroyOnLoad的BackpackState: {backpackState.gameObject.name}");
                    }
                    return;
                }
            }
            
            // 如果没有DontDestroyOnLoad的，使用第一个找到的
            backpackState = foundStates[0];
            if (debugCrossScene)
            {
                Debug.Log($"[ShelfTrigger] {assignedShelfId}: 使用第一个找到的BackpackState: {backpackState.gameObject.name}");
            }
            return;
        }
        
        // 都找不到，记录错误
        Debug.LogError($"[ShelfTrigger] {assignedShelfId}: 无法在场景中找到任何BackpackState！这将导致背包功能无法正常工作。");
        Debug.LogError($"[ShelfTrigger] 请检查BackpackSystemManager是否正确初始化，或在Inspector中手动设置backpackState字段。");
    }
    
    /// <summary>
    /// 验证配置
    /// </summary>
    private void ValidateConfiguration()
    {
        if (enableRandomGeneration)
        {
            if (randomConfig == null)
            {
                Debug.LogWarning($"[ShelfTrigger] {gameObject.name}: 启用了随机生成但未设置配置");
            }
            
            if (targetItemGrid == null)
            {
                Debug.LogWarning($"[ShelfTrigger] {gameObject.name}: 启用了随机生成但未设置目标ItemGrid");
            }
        }
    }
    
    #endregion
}
