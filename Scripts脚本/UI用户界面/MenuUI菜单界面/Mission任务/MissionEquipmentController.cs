using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using InventorySystem;

/// <summary>
/// 任务界面装备控制器 - 专注于装备栏的显示和同步
/// 使用现有的装备槽预制体，实现与背包系统的装备状态同步
/// 职责：管理任务界面中装备槽的同步和生命周期
/// </summary>
public class MissionEquipmentController : MonoBehaviour
{
    [Header("装备槽配置")]
    [FieldLabel("装备槽容器")]
    [Tooltip("装备槽实例化的父容器")]
    [SerializeField] private Transform equipmentSlotsContainer;
    
    [FieldLabel("支持的装备槽类型")]
    [Tooltip("任务界面显示的装备槽类型列表")]
    [SerializeField] private List<EquipmentSlotType> supportedSlotTypes = new List<EquipmentSlotType>
    {
        EquipmentSlotType.Backpack,
        EquipmentSlotType.TacticalRig
    };
    
    [Header("装备槽引用")]
    [FieldLabel("背包装备槽")]
    [Tooltip("场景中已存在的背包装备槽实例")]
    [SerializeField] private EquipmentSlot backpackEquipmentSlot;
    
    [FieldLabel("挂具装备槽")]
    [Tooltip("场景中已存在的挂具装备槽实例")]
    [SerializeField] private EquipmentSlot tacticalRigEquipmentSlot;
    
    [Header("同步设置")]
    [FieldLabel("启用装备同步")]
    [Tooltip("是否自动同步背包系统的装备状态到任务界面")]
    [SerializeField] private bool enableEquipmentSync = true;
    
    [FieldLabel("同步延迟时间")]
    [Tooltip("界面激活后延迟多少秒开始同步装备状态")]
    [Range(0f, 2f)]
    [SerializeField] private float syncDelay = 0.1f;
    
    [Header("UI显示设置")]
    [FieldLabel("装备区域标题")]
    [Tooltip("装备栏区域的标题文本")]
    [SerializeField] private string equipmentAreaTitle = "当前装备";
    
    [FieldLabel("空装备提示文本")]
    [Tooltip("未装备时显示的提示文本")]
    [SerializeField] private string noEquipmentText = "未装备";
    
    [FieldLabel("装备区域标题文本")]
    [Tooltip("装备栏区域的标题文本组件")]
    [SerializeField] private TextMeshProUGUI equipmentAreaTitleText;
    
    [Header("调试设置")]
    [FieldLabel("显示调试日志")]
    [Tooltip("是否在控制台显示详细的调试信息")]
    [SerializeField] private bool showDebugLog = false;
    
    [FieldLabel("详细日志")]
    [Tooltip("显示更详细的调试信息")]
    [SerializeField] private bool verboseLogging = false;
    
    // 装备槽管理
    private Dictionary<EquipmentSlotType, EquipmentSlot> missionEquipmentSlots = new Dictionary<EquipmentSlotType, EquipmentSlot>();
    
    // 系统引用
    private EquipmentSlotManager equipmentManager;
    private EquipmentPersistenceManager persistenceManager;
    
    // 状态管理
    private bool isInitialized = false;
    private bool isEquipmentLoaded = false;
    private int equipmentLoadedCount = 0;
    
    // 事件系统
    public System.Action OnEquipmentSyncCompleted;
    public System.Action<EquipmentSlotType, ItemDataReader> OnEquipmentChanged;
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 核心初始化，确保在其他组件之前完成
        InitializeMissionEquipmentController();
        
        if (showDebugLog)
            Debug.Log($"MissionEquipmentController: Awake初始化完成");
    }
    
    private void OnEnable()
    {
        if (showDebugLog)
            Debug.Log("MissionEquipmentController: 任务界面激活，装备同步已禁用");
        
        // ? 临时禁用：完全停用装备同步功能，专注于背包系统的装备持久化
        // 注册装备系统事件
        // RegisterEquipmentEvents();
        
        // 延迟同步装备状态，确保界面完全激活
        // if (enableEquipmentSync)
        // {
        //     StartCoroutine(DelayedSyncEquipmentState());
        // }
    }
    
    private void OnDisable()
    {
        if (showDebugLog)
            Debug.Log("MissionEquipmentController: 任务界面关闭，装备同步已禁用");
        
        // ? 临时禁用：完全停用装备同步功能
        // 注销装备系统事件
        // UnregisterEquipmentEvents();
    }
    
    private void OnDestroy()
    {
        // ? 临时禁用：完全停用装备同步功能
        // 确保事件清理
        // UnregisterEquipmentEvents();
        
        if (showDebugLog)
            Debug.Log("MissionEquipmentController: 组件销毁，装备同步已禁用");
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化任务装备控制器
    /// </summary>
    private void InitializeMissionEquipmentController()
    {
        if (isInitialized) return;
        
        // ? 临时禁用：完全停用装备同步功能，专注于背包系统的装备持久化
        if (showDebugLog)
            Debug.Log("MissionEquipmentController: 装备同步功能已禁用，跳过初始化");
        
        isInitialized = true;
        return;
        
        // 验证必要组件
        // if (!ValidateRequiredComponents())
        // {
        //     Debug.LogError("MissionEquipmentController: 必要组件验证失败，无法初始化");
        //     return;
        // }
        
        // 获取系统引用
        // InitializeSystemReferences();
        
        // 初始化数据结构
        // InitializeDataStructures();
        
        // if (showDebugLog)
        //     Debug.Log("MissionEquipmentController: 初始化完成");
    }
    
    /// <summary>
    /// 验证必要组件
    /// </summary>
    private bool ValidateRequiredComponents()
    {
        bool isValid = true;
        
        if (backpackEquipmentSlot == null)
        {
            Debug.LogWarning("MissionEquipmentController: backpackEquipmentSlot 未设置");
            isValid = false;
        }
        
        if (tacticalRigEquipmentSlot == null)
        {
            Debug.LogWarning("MissionEquipmentController: tacticalRigEquipmentSlot 未设置");
            isValid = false;
        }
        
        if (supportedSlotTypes == null || supportedSlotTypes.Count == 0)
        {
            Debug.LogWarning("MissionEquipmentController: supportedSlotTypes 为空，将使用默认配置");
            supportedSlotTypes = new List<EquipmentSlotType> { EquipmentSlotType.Backpack, EquipmentSlotType.TacticalRig };
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 初始化系统引用
    /// </summary>
    private void InitializeSystemReferences()
    {
        // 获取装备槽管理器
        equipmentManager = EquipmentSlotManager.Instance;
        if (equipmentManager == null)
        {
            Debug.LogWarning("MissionEquipmentController: EquipmentSlotManager 不可用");
        }
        
        // 获取装备持久化管理器
        persistenceManager = EquipmentPersistenceManager.Instance;
        if (persistenceManager == null)
        {
            Debug.LogWarning("MissionEquipmentController: EquipmentPersistenceManager 不可用");
        }
        
        if (verboseLogging)
            Debug.Log("MissionEquipmentController: 系统引用初始化完成");
    }
    
    /// <summary>
    /// 初始化数据结构
    /// </summary>
    private void InitializeDataStructures()
    {
        // 清理现有数据
        missionEquipmentSlots.Clear();
        
        // ? 修复：任务系统装备槽应该是只读的，不应该注册到全局EquipmentSlotManager
        // 只在本地记录，不注册到全局管理器以防止冲突
        if (backpackEquipmentSlot != null)
        {
            missionEquipmentSlots[EquipmentSlotType.Backpack] = backpackEquipmentSlot;
            // 确保任务装备槽不注册到全局管理器
            backpackEquipmentSlot.enabled = false; // 禁用以防止自动注册
        }
        
        if (tacticalRigEquipmentSlot != null)
        {
            missionEquipmentSlots[EquipmentSlotType.TacticalRig] = tacticalRigEquipmentSlot;
            // 确保任务装备槽不注册到全局管理器
            tacticalRigEquipmentSlot.enabled = false; // 禁用以防止自动注册
        }
        
        // 重置状态
        isEquipmentLoaded = false;
        equipmentLoadedCount = 0;
        
        if (verboseLogging)
            Debug.Log($"MissionEquipmentController: 数据结构初始化完成，配置了 {missionEquipmentSlots.Count} 个任务装备槽（已禁用自动注册）");
    }
    
    #endregion
    
    #region 事件管理
    
    /// <summary>
    /// 注册装备系统事件
    /// </summary>
    private void RegisterEquipmentEvents()
    {
        if (equipmentManager != null)
        {
            // ? 修复：防止与主装备系统的装备槽冲突，只监听不注册新槽位
            // 注册装备变化事件
            EquipmentSlot.OnItemEquipped += HandleEquipmentEquipped;
            EquipmentSlot.OnItemUnequipped += HandleEquipmentUnequipped;
            
            if (verboseLogging)
                Debug.Log("MissionEquipmentController: 装备系统事件注册完成");
        }
    }
    
    /// <summary>
    /// 注销装备系统事件
    /// </summary>
    private void UnregisterEquipmentEvents()
    {
        // 注销装备变化事件
        EquipmentSlot.OnItemEquipped -= HandleEquipmentEquipped;
        EquipmentSlot.OnItemUnequipped -= HandleEquipmentUnequipped;
        
        if (verboseLogging)
            Debug.Log("MissionEquipmentController: 装备系统事件注销完成");
    }
    
    #endregion
    
    #region 装备状态同步
    
    /// <summary>
    /// 延迟同步装备状态
    /// </summary>
    private IEnumerator DelayedSyncEquipmentState()
    {
        // 等待界面完全激活
        yield return new WaitForSeconds(syncDelay);
        
        if (showDebugLog)
            Debug.Log("MissionEquipmentController: 开始同步装备状态");
        
        // 同步装备状态
        SyncCurrentEquipmentState();
        
        // 等待装备加载完成
        yield return new WaitForEndOfFrame();
        
        if (showDebugLog)
            Debug.Log("MissionEquipmentController: 装备状态同步完成");
    }
    
    /// <summary>
    /// 同步当前装备状态
    /// </summary>
    public void SyncCurrentEquipmentState()
    {
        if (equipmentManager == null)
        {
            Debug.LogWarning("MissionEquipmentController: 装备管理器不可用，无法同步装备状态");
            return;
        }
        
        try
        {
            equipmentLoadedCount = 0;
            
            foreach (var slotType in supportedSlotTypes)
            {
                // 获取当前装备
                var equippedItem = GetEquippedItem(slotType);
                
                // 同步装备状态到任务界面装备槽
                SyncEquipmentSlot(slotType, equippedItem);
                
                if (equippedItem != null)
                {
                    equipmentLoadedCount++;
                }
            }
            
            isEquipmentLoaded = true;
            
            // 更新装备区域标题
            UpdateEquipmentAreaTitle();
            
            // 触发装备同步完成事件
            OnEquipmentSyncCompleted?.Invoke();
            
            if (showDebugLog)
                Debug.Log($"MissionEquipmentController: 装备状态同步完成，加载了 {equipmentLoadedCount} 个装备");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionEquipmentController: 同步装备状态时发生错误: {e.Message}");
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 手动刷新装备显示
    /// </summary>
    public void RefreshEquipmentDisplay()
    {
        if (showDebugLog)
            Debug.Log("MissionEquipmentController: 手动刷新装备显示");
        
        SyncCurrentEquipmentState();
    }
    
    /// <summary>
    /// 获取当前装备数量
    /// </summary>
    public int GetEquippedItemCount()
    {
        return equipmentLoadedCount;
    }
    
    /// <summary>
    /// 检查特定装备槽是否有装备
    /// </summary>
    public bool HasEquipment(EquipmentSlotType slotType)
    {
        return missionEquipmentSlots.ContainsKey(slotType) && missionEquipmentSlots[slotType] != null && missionEquipmentSlots[slotType].HasEquippedItem;
    }
    
    /// <summary>
    /// 获取装备槽实例
    /// </summary>
    public EquipmentSlot GetEquipmentSlot(EquipmentSlotType slotType)
    {
        return missionEquipmentSlots.ContainsKey(slotType) ? missionEquipmentSlots[slotType] : null;
    }
    
    /// <summary>
    /// 获取装备区域标题文本
    /// </summary>
    public string GetEquipmentAreaTitle()
    {
        return equipmentAreaTitle;
    }
    
    /// <summary>
    /// 获取空装备提示文本
    /// </summary>
    public string GetNoEquipmentText()
    {
        return noEquipmentText;
    }
    
    /// <summary>
    /// 设置装备区域标题文本
    /// </summary>
    public void SetEquipmentAreaTitle(string title)
    {
        if (!string.IsNullOrEmpty(title))
        {
            equipmentAreaTitle = title;
            UpdateEquipmentAreaTitle();
            
            if (showDebugLog)
                Debug.Log($"MissionEquipmentController: 装备区域标题已更新为: {title}");
        }
    }
    
    /// <summary>
    /// 设置空装备提示文本
    /// </summary>
    public void SetNoEquipmentText(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            noEquipmentText = text;
            
            if (showDebugLog)
                Debug.Log($"MissionEquipmentController: 空装备提示文本已更新为: {text}");
        }
    }
    
    /// <summary>
    /// 获取背包装备槽
    /// </summary>
    public EquipmentSlot GetBackpackEquipmentSlot()
    {
        return backpackEquipmentSlot;
    }
    
    /// <summary>
    /// 获取挂具装备槽
    /// </summary>
    public EquipmentSlot GetTacticalRigEquipmentSlot()
    {
        return tacticalRigEquipmentSlot;
    }
    
    #endregion
    
    #region 私有辅助方法
    
    /// <summary>
    /// 获取装备物品
    /// </summary>
    private ItemDataReader GetEquippedItem(EquipmentSlotType slotType)
    {
        if (equipmentManager == null) return null;
        
        try
        {
            // 使用EquipmentSlotManager的API获取装备
            return equipmentManager.GetEquippedItem(slotType);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionEquipmentController: 获取装备时发生错误 {slotType}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 同步装备槽状态
    /// </summary>
    private void SyncEquipmentSlot(EquipmentSlotType slotType, ItemDataReader equippedItem)
    {
        try
        {
            // 获取任务界面的装备槽
            var missionEquipmentSlot = GetMissionEquipmentSlot(slotType);
            if (missionEquipmentSlot == null)
            {
                if (verboseLogging)
                    Debug.Log($"MissionEquipmentController: 未找到任务界面的装备槽 {slotType}");
                return;
            }
            
            // ? 修复：任务装备槽只显示状态，不实际装备物品，防止与主装备系统冲突
            // 任务系统应该是只读的装备显示，不应该进行实际的装备操作
            if (equippedItem != null)
            {
                // 只更新显示状态，不实际装备
                if (verboseLogging)
                    Debug.Log($"MissionEquipmentController: 检测到装备 {slotType} - {equippedItem?.name}");
            }
            else
            {
                // 清空显示状态
                if (verboseLogging)
                    Debug.Log($"MissionEquipmentController: 检测到装备槽为空 {slotType}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionEquipmentController: 同步装备槽时发生错误 {slotType}: {e.Message}");
        }
    }
    
    /// <summary>
    /// 获取任务界面的装备槽
    /// </summary>
    private EquipmentSlot GetMissionEquipmentSlot(EquipmentSlotType slotType)
    {
        if (missionEquipmentSlots.ContainsKey(slotType))
        {
            return missionEquipmentSlots[slotType];
        }
        
        if (verboseLogging)
            Debug.Log($"MissionEquipmentController: 未找到装备槽类型 {slotType}");
        
        return null;
    }
    
    /// <summary>
    /// 更新装备区域标题
    /// </summary>
    private void UpdateEquipmentAreaTitle()
    {
        if (equipmentAreaTitleText == null) return;
        
        try
        {
            // 根据装备数量显示不同的标题
            string titleText = equipmentAreaTitle;
            if (equipmentLoadedCount > 0)
            {
                titleText = $"{equipmentAreaTitle} ({equipmentLoadedCount})";
            }
            
            equipmentAreaTitleText.text = titleText;
            
            if (verboseLogging)
                Debug.Log($"MissionEquipmentController: 装备区域标题更新为 '{titleText}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionEquipmentController: 更新装备区域标题时发生错误: {e.Message}");
        }
    }
    
    #endregion
    
    #region 装备事件处理器
    
    /// <summary>
    /// 处理装备装备事件
    /// </summary>
    private void HandleEquipmentEquipped(EquipmentSlotType slotType, ItemDataReader equippedItem)
    {
        if (!supportedSlotTypes.Contains(slotType)) return;
        
        if (showDebugLog)
            Debug.Log($"MissionEquipmentController: 检测到装备变化 - {slotType} 装备了 {equippedItem?.name}");
        
        // 同步到任务界面装备槽
        SyncEquipmentSlot(slotType, equippedItem);
        
        // 更新装备区域标题
        UpdateEquipmentAreaTitle();
        
        // 触发装备变化事件
        OnEquipmentChanged?.Invoke(slotType, equippedItem);
    }
    
    /// <summary>
    /// 处理装备卸装事件
    /// </summary>
    private void HandleEquipmentUnequipped(EquipmentSlotType slotType, ItemDataReader unequippedItem)
    {
        if (!supportedSlotTypes.Contains(slotType)) return;
        
        if (showDebugLog)
            Debug.Log($"MissionEquipmentController: 检测到装备变化 - {slotType} 卸装了 {unequippedItem?.name}");
        
        // 同步到任务界面装备槽（清空）
        SyncEquipmentSlot(slotType, null);
        
        // 更新装备区域标题
        UpdateEquipmentAreaTitle();
        
        // 触发装备变化事件
        OnEquipmentChanged?.Invoke(slotType, null);
    }
    
    #endregion
    
    #region 调试工具
    
    /// <summary>
    /// 设置调试日志开关
    /// </summary>
    public void SetDebugLog(bool enabled)
    {
        showDebugLog = enabled;
        
        if (showDebugLog)
            Debug.Log($"MissionEquipmentController: 调试日志已{(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 获取装备状态统计
    /// </summary>
    public string GetEquipmentStats()
    {
        string equipmentTitle = equipmentLoadedCount > 0 ? $"{equipmentAreaTitle} ({equipmentLoadedCount})" : equipmentAreaTitle;
        string emptySlotInfo = equipmentLoadedCount < supportedSlotTypes.Count ? $" | 空槽提示: {noEquipmentText}" : "";
        
        return $"装备状态: {(isEquipmentLoaded ? "已加载" : "未加载")} | " +
               $"装备数量: {equipmentLoadedCount}/{supportedSlotTypes.Count} | " +
               $"装备同步: {(enableEquipmentSync ? "启用" : "禁用")} | " +
               $"区域标题: {equipmentTitle}{emptySlotInfo}";
    }
    
    /// <summary>
    /// 打印当前装备状态（调试用）
    /// </summary>
    [ContextMenu("打印装备状态")]
    public void PrintEquipmentStatus()
    {
        Debug.Log($"MissionEquipmentController 状态报告:\n{GetEquipmentStats()}");
        
        foreach (var slotType in supportedSlotTypes)
        {
            bool hasEquipment = HasEquipment(slotType);
            Debug.Log($"- {slotType}: {(hasEquipment ? "已装备" : "未装备")}");
        }
    }
    
    #endregion
}