using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 装备槽管理器
    /// 统一管理所有装备槽，提供全局装备系统接口
    /// </summary>
    public class EquipmentSlotManager : MonoBehaviour
    {
        [Header("装备槽管理")]
        [FieldLabel("自动查找装备槽")]
        [Tooltip("启动时自动查找场景中的所有装备槽")]
        public bool autoFindSlots = true;
        
        [FieldLabel("手动指定装备槽")]
        [Tooltip("手动指定的装备槽列表")]
        public List<EquipmentSlot> manualSlots = new List<EquipmentSlot>();
        
        [Header("调试信息")]
        [FieldLabel("显示调试信息")]
        public bool showDebugInfo = false;
        
        // 装备槽字典，按类型索引
        private Dictionary<EquipmentSlotType, EquipmentSlot> equipmentSlots = new Dictionary<EquipmentSlotType, EquipmentSlot>();
        
        // 标记是否为自动创建的实例
        private bool isAutoCreated = false;
        
        // 单例实例
        private static EquipmentSlotManager instance;
        public static EquipmentSlotManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EquipmentSlotManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("EquipmentSlotManager");
                        instance = go.AddComponent<EquipmentSlotManager>();
                        
                        // 确保跨场景持久化 - 仅在此处设置，避免Awake中重复设置
                        DontDestroyOnLoad(go);
                        
                        // 标记为自动创建的实例
                        instance.isAutoCreated = true;
                    }
                }
                return instance;
            }
        }
        
        // 装备事件
        public static event System.Action<EquipmentSlotType, ItemDataReader> OnEquipmentChanged;
        public static event System.Action<Dictionary<EquipmentSlotType, ItemDataReader>> OnEquipmentSetChanged;
        
        #region Unity生命周期
        
        private void Awake()
        {
            // 单例模式处理
            if (instance == null)
            {
                instance = this;
                
                // 只有手动添加到场景的实例才需要设置DontDestroyOnLoad
                // 自动创建的实例已经在Instance getter中设置过了
                if (!isAutoCreated)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (instance != this)
            {
                if (showDebugInfo)
                {
                    Debug.Log("[EquipmentSlotManager] 检测到重复实例，销毁当前实例");
                }
                Destroy(gameObject);
                return;
            }
            
            InitializeManager();
        }
        
        private void OnDestroy()
        {
            // 清理单例引用
            if (instance == this)
            {
                instance = null;
            }
            
            // 清理事件
            UnregisterEventHandlers();
            
            if (showDebugInfo)
            {
                Debug.Log("[EquipmentSlotManager] 实例已销毁并清理");
            }
        }
        
        private void OnApplicationQuit()
        {
            // 应用退出时清理
            if (instance == this)
            {
                instance = null;
                if (showDebugInfo)
                {
                    Debug.Log("[EquipmentSlotManager] 应用退出，清理实例");
                }
            }
        }
        
        /// <summary>
        /// 强制清理实例（编辑器模式下使用）
        /// </summary>
        public static void ForceCleanup()
        {
            if (instance != null)
            {
                if (instance.gameObject != null)
                {
                    DestroyImmediate(instance.gameObject);
                }
                instance = null;
                Debug.Log("[EquipmentSlotManager] 强制清理完成");
            }
        }
        
        private void Start()
        {
            RegisterEventHandlers();
            
            if (showDebugInfo)
            {
                LogEquipmentSlotStatus();
            }
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void InitializeManager()
        {
            equipmentSlots.Clear();
            
            // 使用延迟查找策略：先查找一次，然后在背包面板打开时再补充查找
            if (autoFindSlots)
            {
                if (showDebugInfo) Debug.Log("[EquipmentSlotManager] 执行初始装备槽查找...");
                FindAllEquipmentSlots();
                
                // 如果没找到装备槽，启动协程延迟查找
                if (equipmentSlots.Count == 0)
                {
                    if (showDebugInfo) Debug.Log("[EquipmentSlotManager] 初始查找未发现装备槽，启动延迟查找机制");
                    StartCoroutine(DelayedSlotDetection());
                }
            }
            
            RegisterManualSlots();
            
            // 确保自动保存管理器被初始化
            InitializeAutoSaveManager();
            
            if (showDebugInfo) Debug.Log($"[EquipmentSlotManager] 初始化完成，管理 {equipmentSlots.Count} 个装备槽（装备槽将在背包打开时检测）");
        }
        
        /// <summary>
        /// 初始化自动保存管理器
        /// </summary>
        private void InitializeAutoSaveManager()
        {
            // 注释：自动保存管理器已被移除，使用传统保存方法
            if (showDebugInfo) Debug.Log("[EquipmentSlotManager] 使用传统装备数据保存方法");
        }
        
        /// <summary>
        /// 强制重新查找并注册所有装备槽
        /// </summary>
        [ContextMenu("重新查找装备槽")]
        public void RefreshEquipmentSlots()
        {
            if (showDebugInfo) Debug.Log("[EquipmentSlotManager] 开始重新查找装备槽...");
            
            // 清空现有装备槽
            equipmentSlots.Clear();
            
            // 强制查找所有装备槽
            FindAllEquipmentSlots();
            
            // 注册手动指定的装备槽
            RegisterManualSlots();
            
            Debug.Log($"[EquipmentSlotManager] ✅ 装备槽查找完成，管理 {equipmentSlots.Count} 个装备槽");
            
            // 显示详细信息（仅调试模式）
            if (showDebugInfo)
            {
                foreach (var kvp in equipmentSlots)
                {
                    Debug.Log($"[EquipmentSlotManager] 已注册装备槽: {kvp.Key} -> {kvp.Value.name}");
                }
            }
        }
        
        /// <summary>
        /// 查找所有装备槽（包括非激活的）
        /// </summary>
        private void FindAllEquipmentSlots()
        {
            if (showDebugInfo) Debug.Log("[EquipmentSlotManager] 开始查找场景中的装备槽...");
            
            // 使用 includeInactive = true 来查找所有装备槽，包括隐藏的
            var allSlots = FindObjectsOfType<EquipmentSlot>(true);
            if (showDebugInfo) Debug.Log($"[EquipmentSlotManager] FindObjectsOfType 找到 {allSlots.Length} 个装备槽组件 (包括非激活的)");
            
            foreach (var slot in allSlots)
            {
                if (showDebugInfo) Debug.Log($"[EquipmentSlotManager] 检查装备槽: {slot.name} " +
                         $"(激活: {slot.gameObject.activeInHierarchy}) " +
                         $"(配置: {(slot.config != null ? slot.config.slotType.ToString() : "NULL")})");
                RegisterEquipmentSlot(slot);
            }
            
            if (showDebugInfo) Debug.Log($"[EquipmentSlotManager] 查找完成，已注册 {equipmentSlots.Count} 个装备槽");
        }
        
        /// <summary>
        /// 注册手动指定的装备槽
        /// </summary>
        private void RegisterManualSlots()
        {
            foreach (var slot in manualSlots)
            {
                if (slot != null)
                {
                    RegisterEquipmentSlot(slot);
                }
            }
        }
        
        /// <summary>
        /// 注册装备槽
        /// </summary>
        /// <param name="slot">要注册的装备槽</param>
        public void RegisterEquipmentSlot(EquipmentSlot slot)
        {
            if (slot == null)
            {
                Debug.LogWarning("[EquipmentSlotManager] 尝试注册空的装备槽");
                return;
            }
            
            // 排除带有 ExcludeFromEquipmentSystem 标记的槽（例如任务面板镜像槽）
            if (slot.GetComponent<ExcludeFromEquipmentSystem>() != null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[EquipmentSlotManager] 跳过注册被排除的槽: {slot.name}");
                }
                return;
            }

            // 如果该槽位处于任务镜像面板层级下，也跳过注册（即便镜像面板未激活）
            try
            {
                var mirrorParent = slot.GetComponentInParent<InventorySystem.Mission.MissionAcceptEquipmentMirror>(true);
                if (mirrorParent != null)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"[EquipmentSlotManager] 跳过注册镜像面板下的槽: {slot.name}");
                    }
                    return;
                }
            }
            catch { }

            if (slot.config == null)
            {
                Debug.LogWarning($"[EquipmentSlotManager] 装备槽 '{slot.name}' 缺少配置数据，跳过注册");
                return;
            }
            
            var slotType = slot.config.slotType;
            
            if (equipmentSlots.ContainsKey(slotType))
            {
                Debug.LogWarning($"[EquipmentSlotManager] 装备槽类型 {slotType} 已存在，将覆盖原有槽位");
            }
            
            equipmentSlots[slotType] = slot;
            
            Debug.Log($"[EquipmentSlotManager] ✅ 成功注册装备槽: {slot.name} ({slotType})");
        }
        
        /// <summary>
        /// 注册事件处理器
        /// </summary>
        private void RegisterEventHandlers()
        {
            EquipmentSlot.OnItemEquipped += HandleItemEquipped;
            EquipmentSlot.OnItemUnequipped += HandleItemUnequipped;
        }
        
        /// <summary>
        /// 注销事件处理器
        /// </summary>
        private void UnregisterEventHandlers()
        {
            EquipmentSlot.OnItemEquipped -= HandleItemEquipped;
            EquipmentSlot.OnItemUnequipped -= HandleItemUnequipped;
        }
        
        /// <summary>
        /// 延迟装备槽检测协程
        /// </summary>
        /// <returns></returns>
        private System.Collections.IEnumerator DelayedSlotDetection()
        {
            // 每2秒检测一次，最多检测10次
            int maxAttempts = 10;
            int attempts = 0;
            
            while (attempts < maxAttempts && equipmentSlots.Count == 0)
            {
                attempts++;
                yield return new WaitForSeconds(2f);
                
                Debug.Log($"[EquipmentSlotManager] 延迟检测装备槽 (尝试 {attempts}/{maxAttempts})");
                
                // 重新查找装备槽
                FindAllEquipmentSlots();
                
                if (equipmentSlots.Count > 0)
                {
                    Debug.Log($"[EquipmentSlotManager] ✅ 延迟检测成功找到 {equipmentSlots.Count} 个装备槽");
                    break;
                }
            }
            
            if (equipmentSlots.Count == 0)
            {
                Debug.LogWarning("[EquipmentSlotManager] ⚠️ 延迟检测完成，仍未找到装备槽。请检查装备槽组件是否正确配置。");
            }
        }
        
        /// <summary>
        /// 手动触发装备槽检测（供外部调用）
        /// </summary>
        public void TriggerSlotDetection()
        {
            Debug.Log("[EquipmentSlotManager] 手动触发装备槽检测");
            RefreshEquipmentSlots();
        }
        
        #endregion
        
        #region 装备操作
        
        /// <summary>
        /// 装备物品到指定槽位
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <param name="item">要装备的物品</param>
        /// <returns>是否装备成功</returns>
        public bool EquipItem(EquipmentSlotType slotType, ItemDataReader item)
        {
            if (!equipmentSlots.TryGetValue(slotType, out var slot))
            {
                Debug.LogWarning($"[EquipmentSlotManager] 未找到类型为 {slotType} 的装备槽");
                return false;
            }
            
            return slot.EquipItem(item);
        }
        
        /// <summary>
        /// 自动装备物品（根据物品类型自动选择槽位）
        /// </summary>
        /// <param name="item">要装备的物品</param>
        /// <returns>是否装备成功</returns>
        public bool AutoEquipItem(ItemDataReader item)
        {
            if (item == null) return false;
            
            // 查找可以装备该物品的槽位
            var compatibleSlots = equipmentSlots.Values
                .Where(slot => slot.CanAcceptItem(item))
                .OrderBy(slot => slot.config.slotPriority)
                .ToList();
            
            if (compatibleSlots.Count == 0)
            {
                Debug.LogWarning($"[EquipmentSlotManager] 没有槽位可以装备物品: {item.ItemData.itemName}");
                return false;
            }
            
            // 优先选择空槽位
            var emptySlot = compatibleSlots.FirstOrDefault(slot => !slot.HasEquippedItem);
            if (emptySlot != null)
            {
                return emptySlot.EquipItem(item);
            }
            
            // 如果没有空槽位，选择优先级最高的槽位进行替换
            var highestPrioritySlot = compatibleSlots.First();
            return highestPrioritySlot.EquipItem(item);
        }
        
        /// <summary>
        /// 卸下指定槽位的装备
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <returns>卸下的物品</returns>
        public ItemDataReader UnequipItem(EquipmentSlotType slotType)
        {
            if (!equipmentSlots.TryGetValue(slotType, out var slot))
            {
                Debug.LogWarning($"[EquipmentSlotManager] 未找到类型为 {slotType} 的装备槽");
                return null;
            }
            
            return slot.UnequipItem();
        }
        
        /// <summary>
        /// 卸下所有装备
        /// </summary>
        /// <returns>卸下的物品列表</returns>
        public List<ItemDataReader> UnequipAllItems()
        {
            var unequippedItems = new List<ItemDataReader>();
            
            foreach (var slot in equipmentSlots.Values)
            {
                if (slot.HasEquippedItem)
                {
                    var item = slot.UnequipItem();
                    if (item != null)
                    {
                        unequippedItems.Add(item);
                    }
                }
            }
            
            return unequippedItems;
        }
        
        #endregion
        
        #region 查询接口
        
        /// <summary>
        /// 获取指定槽位的装备
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <returns>装备的物品，如果没有则返回null</returns>
        public ItemDataReader GetEquippedItem(EquipmentSlotType slotType)
        {
            if (equipmentSlots.TryGetValue(slotType, out var slot))
            {
                return slot.CurrentEquippedItem;
            }
            return null;
        }
        
        /// <summary>
        /// 获取指定槽位
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <returns>装备槽</returns>
        public EquipmentSlot GetEquipmentSlot(EquipmentSlotType slotType)
        {
            equipmentSlots.TryGetValue(slotType, out var slot);
            return slot;
        }
        
        /// <summary>
        /// 获取所有装备的物品
        /// </summary>
        /// <returns>装备字典</returns>
        public Dictionary<EquipmentSlotType, ItemDataReader> GetAllEquippedItems()
        {
            var equippedItems = new Dictionary<EquipmentSlotType, ItemDataReader>();
            
            foreach (var kvp in equipmentSlots)
            {
                if (kvp.Value.HasEquippedItem)
                {
                    equippedItems[kvp.Key] = kvp.Value.CurrentEquippedItem;
                }
            }
            
            return equippedItems;
        }
        
        /// <summary>
        /// 获取所有装备槽
        /// </summary>
        /// <returns>装备槽字典</returns>
        public Dictionary<EquipmentSlotType, EquipmentSlot> GetAllEquipmentSlots()
        {
            return new Dictionary<EquipmentSlotType, EquipmentSlot>(equipmentSlots);
        }
        
        /// <summary>
        /// 检查是否有装备
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <returns>是否有装备</returns>
        public bool HasEquippedItem(EquipmentSlotType slotType)
        {
            if (equipmentSlots.TryGetValue(slotType, out var slot))
            {
                return slot.HasEquippedItem;
            }
            return false;
        }
        
        /// <summary>
        /// 获取装备统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public EquipmentStatistics GetEquipmentStatistics()
        {
            var stats = new EquipmentStatistics();
            
            foreach (var slot in equipmentSlots.Values)
            {
                stats.totalSlots++;
                if (slot.HasEquippedItem)
                {
                    stats.equippedSlots++;
                }
            }
            
            stats.emptySlots = stats.totalSlots - stats.equippedSlots;
            
            return stats;
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 处理物品装备事件
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <param name="item">装备的物品</param>
        private void HandleItemEquipped(EquipmentSlotType slotType, ItemDataReader item)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[EquipmentSlotManager] 装备事件: {item.ItemData.itemName} -> {slotType}");
            }
            
            // 触发装备变化事件
            OnEquipmentChanged?.Invoke(slotType, item);
            OnEquipmentSetChanged?.Invoke(GetAllEquippedItems());
            
            // 触发装备持久化保存
            TriggerEquipmentSave();
        }
        
        /// <summary>
        /// 处理物品卸装事件
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <param name="item">卸装的物品</param>
        private void HandleItemUnequipped(EquipmentSlotType slotType, ItemDataReader item)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[EquipmentSlotManager] 卸装事件: {item.ItemData.itemName} <- {slotType}");
            }
            
            // 触发装备变化事件
            OnEquipmentChanged?.Invoke(slotType, null);
            OnEquipmentSetChanged?.Invoke(GetAllEquippedItems());
            
            // 触发装备持久化保存
            TriggerEquipmentSave();
        }
        
        /// <summary>
        /// 触发装备数据保存
        /// </summary>
        private void TriggerEquipmentSave()
        {
            // 获取装备持久化管理器并触发保存
            var persistenceManager = EquipmentPersistenceManager.Instance;
            if (persistenceManager != null)
            {
                // 🔧 关键修复：确保在有装备时重置启动期保存抑制
                // 这个备用机制解决BackpackEquipmentEventHandler初始化失败时suppressSavesUntilFirstLoad永久为true的问题
                persistenceManager.EnsureSaveNotSuppressed();
                
                persistenceManager.SaveEquipmentData();
                
                if (showDebugInfo)
                {
                    Debug.Log("[EquipmentSlotManager] 已触发装备数据保存");
                }
            }
            else
            {
                Debug.LogWarning("[EquipmentSlotManager] 无法找到装备持久化管理器，跳过自动保存");
            }
        }
        
        #endregion
        
        #region 调试功能
        
        /// <summary>
        /// 输出装备槽状态
        /// </summary>
        private void LogEquipmentSlotStatus()
        {
            Debug.Log($"[EquipmentSlotManager] 装备槽状态:");
            foreach (var kvp in equipmentSlots)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value.GetSlotStatusInfo()}");
            }
        }
        
        /// <summary>
        /// 验证所有装备槽配置
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ValidateAllSlotConfigs()
        {
            foreach (var slot in equipmentSlots.Values)
            {
                if (slot.config != null)
                {
                    var (isValid, errorMessage) = slot.config.ValidateConfig();
                    if (!isValid)
                    {
                        Debug.LogError($"[EquipmentSlotManager] 槽位配置错误 {slot.SlotName}: {errorMessage}");
                    }
                }
            }
        }
        
        #endregion
        
    }
    
    /// <summary>
    /// 装备统计信息
    /// </summary>
    [System.Serializable]
    public struct EquipmentStatistics
    {
        public int totalSlots;      // 总槽位数
        public int equippedSlots;   // 已装备槽位数
        public int emptySlots;      // 空槽位数
        
        public float equipmentRate => totalSlots > 0 ? (float)equippedSlots / totalSlots : 0f;
    }
}
