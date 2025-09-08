// ShelfTrigger.cs
using UnityEngine;
using InventorySystem;
using InventorySystem.SpawnSystem;

public class ShelfTrigger : BaseContainerTrigger
{
    public static bool isInShelf = false; // 全局状态
    
    [Header("背包设置")]
    [SerializeField] private BackpackState backpackState;
    
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
            Debug.Log("ShelfTrigger: 程序化切换货架状态，isInShelf = " + isInShelf);
            
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
        
        // 不再在进入触发器时生成物品，改为在打开背包时生成
        if (debugRandomGeneration)
        {
            Debug.Log($"[ShelfTrigger] {assignedShelfId}: 玩家进入触发器，等待Tab键打开网格后生成物品");
        }
        
        // 更新UI文本为货架专用文本
        if (tmpText != null)
        {
            tmpText.text = "[Tab] Search";
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
