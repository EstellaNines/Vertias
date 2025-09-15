using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 装备槽持久化数据
    /// 记录单个装备槽的装备信息
    /// </summary>
    [System.Serializable]
    public class EquipmentSlotPersistenceData
    {
        [Header("槽位信息")]
        public EquipmentSlotType slotType;          // 装备槽类型
        public string slotName;                     // 装备槽名称
        
        [Header("装备状态")]
        public bool hasEquipment;                   // 是否有装备
        
        [Header("装备数据")]
        public string itemConfigGUID;               // 物品配置文件的GUID
        public string itemID;                       // 物品ID（备用查找）
        public string itemName;                     // 物品名称（用于调试）
        
        [Header("运行时数据")]
        public ItemRuntimeData runtimeData;         // 物品运行时数据
        
        [Header("元数据")]
        public string saveTimestamp;                // 保存时间戳
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public EquipmentSlotPersistenceData()
        {
            saveTimestamp = System.DateTime.Now.ToBinary().ToString();
            runtimeData = new ItemRuntimeData();
        }
        
        /// <summary>
        /// 从装备槽创建持久化数据
        /// </summary>
        /// <param name="slot">装备槽</param>
        public EquipmentSlotPersistenceData(EquipmentSlot slot)
        {
            slotType = slot.SlotType;
            slotName = slot.SlotName;
            hasEquipment = slot.HasEquippedItem;
            
            if (hasEquipment && slot.CurrentEquippedItem != null)
            {
                var item = slot.CurrentEquippedItem;
                
                // 获取物品配置的GUID (如果可能的话)
                itemConfigGUID = GetItemConfigGUID(item.ItemData);
                itemID = item.ItemData.GlobalId.ToString();
                itemName = item.ItemData.itemName;
                
                // 创建运行时数据
                runtimeData = new ItemRuntimeData(item);
            }
            
            saveTimestamp = System.DateTime.Now.ToBinary().ToString();
        }
        
        /// <summary>
        /// 获取物品配置的GUID
        /// </summary>
        /// <param name="itemData">物品数据</param>
        /// <returns>GUID字符串</returns>
        private string GetItemConfigGUID(ItemDataSO itemData)
        {
#if UNITY_EDITOR
            // 在编辑器中可以获取资源的GUID
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(itemData);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            }
#endif
            // 运行时使用物品GlobalId作为备用标识
            return itemData.GlobalId.ToString();
        }
        
        /// <summary>
        /// 验证数据完整性
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            if (!hasEquipment) return true;
            
            return !string.IsNullOrEmpty(itemID) && 
                   !string.IsNullOrEmpty(itemName) && 
                   runtimeData != null;
        }
    }
    
    /// <summary>
    /// 物品运行时数据
    /// 记录物品的动态属性
    /// </summary>
    [System.Serializable]
    public class ItemRuntimeData
    {
        [Header("基础属性")]
        public int stackCount = 1;                  // 堆叠数量
        public float durability = 100f;             // 耐久度
        public int usageCount = 0;                  // 使用次数
        
        [Header("位置信息")]
        public Vector2Int originalGridPosition;     // 原始网格位置
        public string originalGridName;             // 原始网格名称
        
        [Header("元数据")]
        public int globalID;                        // 全局唯一ID
        public bool isRotated;                      // 是否旋转
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ItemRuntimeData()
        {
            stackCount = 1;
            durability = 100f;
            usageCount = 0;
            isRotated = false;
        }
        
        /// <summary>
        /// 从物品读取器创建运行时数据
        /// </summary>
        /// <param name="itemReader">物品读取器</param>
        public ItemRuntimeData(ItemDataReader itemReader)
        {
            if (itemReader == null) return;
            
            // 基础属性
            stackCount = itemReader.CurrentStack;
            durability = itemReader.CurrentDurability;
            usageCount = itemReader.CurrentUsageCount;
            globalID = (int)itemReader.ItemData.GlobalId;
            
            // 位置信息
            var itemComponent = itemReader.GetComponent<Item>();
            if (itemComponent != null)
            {
                originalGridPosition = itemComponent.OnGridPosition;
                originalGridName = itemComponent.OnGridReference?.GridName ?? "";
                isRotated = itemComponent.IsRotated();
            }
        }
    }
    
    /// <summary>
    /// 装备系统持久化数据
    /// 记录整个装备系统的状态
    /// </summary>
    [System.Serializable]
    public class EquipmentSystemPersistenceData
    {
        [Header("系统信息")]
        public string version = "1.0";              // 数据版本
        public string saveTimestamp;                // 保存时间戳
        
        [Header("装备数据")]
        public List<EquipmentSlotPersistenceData> equipmentSlots;  // 所有装备槽数据
        
        [Header("统计信息")]
        public int totalSlots;                      // 总槽位数
        public int equippedSlots;                   // 已装备槽位数
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public EquipmentSystemPersistenceData()
        {
            equipmentSlots = new List<EquipmentSlotPersistenceData>();
            saveTimestamp = System.DateTime.Now.ToBinary().ToString();
        }
        
        /// <summary>
        /// 添加装备槽数据
        /// </summary>
        /// <param name="slotData">装备槽数据</param>
        public void AddSlotData(EquipmentSlotPersistenceData slotData)
        {
            if (slotData == null) return;
            
            // 移除相同类型的旧数据
            equipmentSlots.RemoveAll(data => data.slotType == slotData.slotType);
            
            // 添加新数据
            equipmentSlots.Add(slotData);
            
            // 更新统计信息
            UpdateStatistics();
        }
        
        /// <summary>
        /// 获取指定槽位的数据
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <returns>装备槽数据，如果不存在返回null</returns>
        public EquipmentSlotPersistenceData GetSlotData(EquipmentSlotType slotType)
        {
            return equipmentSlots.Find(data => data.slotType == slotType);
        }
        
        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            totalSlots = equipmentSlots.Count;
            equippedSlots = equipmentSlots.Where(data => data.hasEquipment).Count();
        }
        
        /// <summary>
        /// 验证数据完整性
        /// </summary>
        /// <returns>验证结果和错误信息</returns>
        public (bool isValid, string errorMessage) Validate()
        {
            if (equipmentSlots == null)
            {
                return (false, "装备槽数据列表为空");
            }
            
            // 检查重复的槽位类型
            var duplicateSlots = equipmentSlots
                .GroupBy(data => data.slotType)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);
            
            if (duplicateSlots.Any())
            {
                return (false, $"发现重复的装备槽类型: {string.Join(", ", duplicateSlots)}");
            }
            
            // 验证每个槽位数据
            foreach (var slotData in equipmentSlots)
            {
                if (!slotData.IsValid())
                {
                    return (false, $"装备槽 {slotData.slotType} 的数据无效");
                }
            }
            
            return (true, string.Empty);
        }
        
        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息字符串</returns>
        public string GetDebugInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"装备系统数据 (版本: {version})");
            info.AppendLine($"保存时间: {saveTimestamp}");
            info.AppendLine($"总槽位: {totalSlots}, 已装备: {equippedSlots}");
            info.AppendLine("装备详情:");
            
            foreach (var slotData in equipmentSlots)
            {
                string status = slotData.hasEquipment ? $"装备: {slotData.itemName}" : "空";
                info.AppendLine($"  {slotData.slotType}: {status}");
            }
            
            return info.ToString();
        }
    }
}
