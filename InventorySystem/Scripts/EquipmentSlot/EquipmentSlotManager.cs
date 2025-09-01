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
                        DontDestroyOnLoad(go);
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
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeManager();
        }
        
        private void Start()
        {
            RegisterEventHandlers();
            
            if (showDebugInfo)
            {
                LogEquipmentSlotStatus();
            }
        }
        
        private void OnDestroy()
        {
            UnregisterEventHandlers();
            
            if (instance == this)
            {
                instance = null;
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
            
            if (autoFindSlots)
            {
                FindAllEquipmentSlots();
            }
            
            RegisterManualSlots();
            
            Debug.Log($"[EquipmentSlotManager] 初始化完成，管理 {equipmentSlots.Count} 个装备槽");
        }
        
        /// <summary>
        /// 查找所有装备槽
        /// </summary>
        private void FindAllEquipmentSlots()
        {
            var allSlots = FindObjectsOfType<EquipmentSlot>();
            foreach (var slot in allSlots)
            {
                RegisterEquipmentSlot(slot);
            }
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
            if (slot == null || slot.config == null) return;
            
            var slotType = slot.config.slotType;
            
            if (equipmentSlots.ContainsKey(slotType))
            {
                Debug.LogWarning($"[EquipmentSlotManager] 装备槽类型 {slotType} 已存在，将覆盖原有槽位");
            }
            
            equipmentSlots[slotType] = slot;
            
            if (showDebugInfo)
            {
                Debug.Log($"[EquipmentSlotManager] 注册装备槽: {slot.SlotName} ({slotType})");
            }
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
