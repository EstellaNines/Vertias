using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 装备槽存档数据
    /// </summary>
    [System.Serializable]
    public class EquipmentSlotSaveData
    {
        public EquipmentSlotType slotType;          // 槽位类型
        public string slotName;                     // 槽位名称
        public bool hasEquippedItem;                // 是否有装备
        public ItemSaveData equippedItemData;       // 装备的物品数据
        public string saveTime;                     // 保存时间
        
        public EquipmentSlotSaveData()
        {
            saveTime = System.DateTime.Now.ToBinary().ToString();
        }
        
        public EquipmentSlotSaveData(EquipmentSlot slot)
        {
            slotType = slot.SlotType;
            slotName = slot.SlotName;
            hasEquippedItem = slot.HasEquippedItem;
            
            if (hasEquippedItem && slot.CurrentEquippedItem != null)
            {
                equippedItemData = new ItemSaveData(slot.CurrentEquippedItem, Vector2Int.zero);
            }
            
            saveTime = System.DateTime.Now.ToBinary().ToString();
        }
    }
    
    /// <summary>
    /// 装备系统存档数据
    /// </summary>
    [System.Serializable]
    public class EquipmentSystemSaveData
    {
        public List<EquipmentSlotSaveData> equipmentSlots;  // 所有装备槽数据
        public int totalEquippedItems;                      // 总装备数量
        public string lastSaveTime;                         // 最后保存时间
        
        public EquipmentSystemSaveData()
        {
            equipmentSlots = new List<EquipmentSlotSaveData>();
            lastSaveTime = System.DateTime.Now.ToBinary().ToString();
        }
    }
    
    /// <summary>
    /// 装备槽存档系统扩展
    /// 为现有存档系统添加装备槽支持
    /// </summary>
    public static class EquipmentSlotSaveExtension
    {
        private const string EQUIPMENT_SAVE_KEY = "EquipmentSystemData";
        
        /// <summary>
        /// 收集装备系统数据
        /// </summary>
        /// <returns>装备系统存档数据</returns>
        public static EquipmentSystemSaveData CollectEquipmentSystemData()
        {
            var saveData = new EquipmentSystemSaveData();
            var equipmentManager = EquipmentSlotManager.Instance;
            
            if (equipmentManager == null)
            {
                Debug.LogWarning("[EquipmentSlotSaveExtension] 未找到装备槽管理器");
                return saveData;
            }
            
            Debug.Log("[EquipmentSlotSaveExtension] 开始收集装备系统数据...");
            
            // 收集所有装备槽数据
            var allEquippedItems = equipmentManager.GetAllEquippedItems();
            Debug.Log($"[EquipmentSlotSaveExtension] 当前装备物品总数: {allEquippedItems.Count}");
            
            foreach (var kvp in allEquippedItems)
            {
                string itemName = kvp.Value?.ItemData?.itemName ?? "未知物品";
                Debug.Log($"[EquipmentSlotSaveExtension] 装备: {kvp.Key} -> {itemName}");
            }
            
            // 首先检查装备槽管理器的状态
            var allRegisteredSlots = equipmentManager.GetAllEquipmentSlots();
            Debug.Log($"[EquipmentSlotSaveExtension] 装备槽管理器中已注册槽位数量: {allRegisteredSlots.Count}");
            
            foreach (var kvp in allRegisteredSlots)
            {
                Debug.Log($"[EquipmentSlotSaveExtension] 已注册槽位: {kvp.Key} -> {kvp.Value.name} (是否有装备: {kvp.Value.HasEquippedItem})");
            }
            
            foreach (var slotType in System.Enum.GetValues(typeof(EquipmentSlotType)))
            {
                var slot = equipmentManager.GetEquipmentSlot((EquipmentSlotType)slotType);
                if (slot != null)
                {
                    var slotSaveData = new EquipmentSlotSaveData(slot);
                    saveData.equipmentSlots.Add(slotSaveData);
                    
                    Debug.Log($"[EquipmentSlotSaveExtension] 槽位 {slotType}: {(slotSaveData.hasEquippedItem ? "有装备" : "无装备")}");
                    
                    if (slotSaveData.hasEquippedItem)
                    {
                        Debug.Log($"[EquipmentSlotSaveExtension] -> 装备物品: {slotSaveData.equippedItemData.itemID}");
                    }
                }
                else
                {
                    Debug.Log($"[EquipmentSlotSaveExtension] 槽位 {slotType}: 未找到");
                }
            }
            
            saveData.totalEquippedItems = allEquippedItems.Count;
            
            Debug.Log($"[EquipmentSlotSaveExtension] 收集装备数据完成，共 {saveData.equipmentSlots.Count} 个槽位，{saveData.totalEquippedItems} 个装备");
            
            return saveData;
        }
        
        /// <summary>
        /// 应用装备系统数据
        /// </summary>
        /// <param name="saveData">装备系统存档数据</param>
        public static void ApplyEquipmentSystemData(EquipmentSystemSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("[EquipmentSlotSaveExtension] 装备存档数据为空");
                return;
            }
            
            Debug.Log($"[EquipmentSlotSaveExtension] 开始应用装备系统数据，存档中有 {saveData.totalEquippedItems} 个装备");
            
            var equipmentManager = EquipmentSlotManager.Instance;
            if (equipmentManager == null)
            {
                Debug.LogError("[EquipmentSlotSaveExtension] 未找到装备槽管理器");
                return;
            }
            
            // 检查装备槽是否已注册
            var allSlots = equipmentManager.GetAllEquipmentSlots();
            Debug.Log($"[EquipmentSlotSaveExtension] 当前已注册装备槽数量: {allSlots.Count}");
            
            if (allSlots.Count == 0)
            {
                Debug.LogError("[EquipmentSlotSaveExtension] 没有注册的装备槽，无法恢复装备");
                return;
            }
            
            // 先清空所有装备
            equipmentManager.UnequipAllItems();
            Debug.Log("[EquipmentSlotSaveExtension] 已清空所有当前装备");
            
            int restoredCount = 0;
            int attemptedCount = 0;
            
            // 恢复每个槽位的装备
            foreach (var slotData in saveData.equipmentSlots)
            {
                Debug.Log($"[EquipmentSlotSaveExtension] 处理槽位 {slotData.slotType}: {(slotData.hasEquippedItem ? "有装备" : "无装备")}");
                
                if (slotData.hasEquippedItem && slotData.equippedItemData != null)
                {
                    attemptedCount++;
                    Debug.Log($"[EquipmentSlotSaveExtension] 尝试恢复装备: {slotData.equippedItemData.itemID} -> {slotData.slotType}");
                    
                    bool restored = RestoreEquippedItem(slotData);
                    if (restored)
                    {
                        restoredCount++;
                        Debug.Log($"[EquipmentSlotSaveExtension] ✅ 装备恢复成功: {slotData.slotType}");
                    }
                    else
                    {
                        Debug.LogError($"[EquipmentSlotSaveExtension] �7�4 装备恢复失败: {slotData.slotType}");
                    }
                }
            }
            
            Debug.Log($"[EquipmentSlotSaveExtension] 装备恢复完成，成功恢复 {restoredCount}/{attemptedCount} 个装备（存档总数: {saveData.totalEquippedItems}）");
            
            // 验证恢复结果
            var currentEquipped = equipmentManager.GetAllEquippedItems();
            Debug.Log($"[EquipmentSlotSaveExtension] 恢复后当前装备数量: {currentEquipped.Count}");
            
            foreach (var kvp in currentEquipped)
            {
                Debug.Log($"[EquipmentSlotSaveExtension] 当前装备: {kvp.Key} -> {kvp.Value.ItemData.itemName}");
            }
        }
        
        /// <summary>
        /// 恢复装备物品
        /// </summary>
        /// <param name="slotData">槽位存档数据</param>
        /// <returns>是否恢复成功</returns>
        private static bool RestoreEquippedItem(EquipmentSlotSaveData slotData)
        {
            Debug.Log($"[EquipmentSlotSaveExtension] 开始恢复装备到槽位 {slotData.slotType}");
            
            var equipmentManager = EquipmentSlotManager.Instance;
            var slot = equipmentManager.GetEquipmentSlot(slotData.slotType);
            
            if (slot == null)
            {
                Debug.LogError($"[EquipmentSlotSaveExtension] �7�4 未找到类型为 {slotData.slotType} 的装备槽");
                return false;
            }
            
            Debug.Log($"[EquipmentSlotSaveExtension] ✅ 找到装备槽: {slot.name}");
            
            // 创建物品实例
            Debug.Log($"[EquipmentSlotSaveExtension] 开始创建物品实例: {slotData.equippedItemData.itemID}");
            var itemInstance = CreateItemFromSaveData(slotData.equippedItemData);
            if (itemInstance == null)
            {
                Debug.LogError($"[EquipmentSlotSaveExtension] �7�4 无法创建物品实例: {slotData.equippedItemData.itemID}");
                return false;
            }
            
            Debug.Log($"[EquipmentSlotSaveExtension] ✅ 成功创建物品实例: {itemInstance.name}");
            
            // 装备物品
            var itemDataReader = itemInstance.GetComponent<ItemDataReader>();
            if (itemDataReader == null)
            {
                Debug.LogError($"[EquipmentSlotSaveExtension] �7�4 物品实例缺少 ItemDataReader 组件");
                Object.Destroy(itemInstance);
                return false;
            }
            
            Debug.Log($"[EquipmentSlotSaveExtension] ✅ 找到 ItemDataReader，物品名称: {itemDataReader.ItemData.itemName}");
            
            // 尝试装备物品
            Debug.Log($"[EquipmentSlotSaveExtension] 尝试将物品装备到槽位...");
            bool success = slot.EquipItem(itemDataReader);
            
            if (success)
            {
                Debug.Log($"[EquipmentSlotSaveExtension] ✅ 成功恢复装备: {itemDataReader.ItemData.itemName} -> {slotData.slotType}");
                
                // 验证装备是否真的在槽位中
                if (slot.HasEquippedItem)
                {
                    Debug.Log($"[EquipmentSlotSaveExtension] ✅ 验证成功，槽位中有装备: {slot.CurrentEquippedItem.ItemData.itemName}");
                }
                else
                {
                    Debug.LogError($"[EquipmentSlotSaveExtension] �7�4 验证失败，槽位中没有装备");
                }
                
                return true;
            }
            else
            {
                Debug.LogError($"[EquipmentSlotSaveExtension] �7�4 装备到槽位失败");
                
                // 如果装备失败，销毁创建的实例
                Object.Destroy(itemInstance);
                return false;
            }
        }
        
        /// <summary>
        /// 从存档数据创建物品实例
        /// </summary>
        /// <param name="itemSaveData">物品存档数据</param>
        /// <returns>创建的物品GameObject</returns>
        private static GameObject CreateItemFromSaveData(ItemSaveData itemSaveData)
        {
            if (itemSaveData == null) return null;
            
            // 获取物品类别
            var category = GetCategoryByID(itemSaveData.itemID);
            
            // 加载物品预制件
            var prefab = LoadItemPrefabByCategory(category, itemSaveData.itemID);
            if (prefab == null)
            {
                Debug.LogWarning($"[EquipmentSlotSaveExtension] 无法找到物品预制件: {itemSaveData.itemID}");
                return null;
            }
            
            // 实例化物品
            var itemInstance = Object.Instantiate(prefab);
            
            // 恢复物品数据
            var itemDataReader = itemInstance.GetComponent<ItemDataReader>();
            if (itemDataReader != null)
            {
                // 恢复堆叠数量
                itemDataReader.SetStack(itemSaveData.stackCount);
                
                // 恢复耐久度
                if (itemSaveData.durability > 0)
                {
                    itemDataReader.SetDurability(Mathf.RoundToInt(itemSaveData.durability));
                }
                
                // 恢复使用次数
                if (itemSaveData.usageCount > 0)
                {
                    itemDataReader.SetUsageCount(itemSaveData.usageCount);
                }
            }
            
            return itemInstance;
        }
        
        /// <summary>
        /// 根据物品ID获取类别
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <returns>物品类别</returns>
        private static ItemCategory GetCategoryByID(string itemID)
        {
            if (string.IsNullOrEmpty(itemID)) return ItemCategory.Special;
            
            // 根据ID前缀判断类别
            if (itemID.StartsWith("1")) return ItemCategory.Helmet;
            if (itemID.StartsWith("2")) return ItemCategory.Armor;
            if (itemID.StartsWith("3")) return ItemCategory.TacticalRig;
            if (itemID.StartsWith("4")) return ItemCategory.Backpack;
            if (itemID.StartsWith("5")) return ItemCategory.Weapon;
            
            return ItemCategory.Special;
        }
        
        /// <summary>
        /// 根据类别和ID加载物品预制件
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <param name="itemID">物品ID</param>
        /// <returns>物品预制件</returns>
        private static GameObject LoadItemPrefabByCategory(ItemCategory category, string itemID)
        {
            string categoryFolder = GetCategoryFolderName(category);
            string prefabPath = $"InventorySystemResources/Prefabs/{categoryFolder}";
            
            // 尝试按ID查找预制件
            string[] possibleNames = {
                $"{itemID}__*",
                $"{itemID}_*",
                $"*{itemID}*"
            };
            
            foreach (var namePattern in possibleNames)
            {
                var prefabs = Resources.LoadAll<GameObject>(prefabPath);
                foreach (var prefab in prefabs)
                {
                    if (prefab.name.Contains(itemID))
                    {
                        return prefab;
                    }
                }
            }
            
            Debug.LogWarning($"[EquipmentSlotSaveExtension] 未找到物品预制件: {itemID} in {prefabPath}");
            return null;
        }
        
        /// <summary>
        /// 获取类别文件夹名称
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <returns>文件夹名称</returns>
        private static string GetCategoryFolderName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Helmet: return "Helmet_头盔";
                case ItemCategory.Armor: return "Armor_护甲";
                case ItemCategory.TacticalRig: return "TacticalRig_战术背心";
                case ItemCategory.Backpack: return "Backpack_背包";
                case ItemCategory.Weapon: return "Weapon_武器";
                case ItemCategory.Ammunition: return "Ammunition_弹药";
                case ItemCategory.Food: return "Food_食物";
                case ItemCategory.Drink: return "Drink_饮料";
                case ItemCategory.Sedative: return "Sedative_镇静剂";
                case ItemCategory.Hemostatic: return "Hemostatic_止血剂";
                case ItemCategory.Healing: return "Healing_治疗药物";
                case ItemCategory.Intelligence: return "Intelligence_情报";
                case ItemCategory.Currency: return "Currency_货币";
                case ItemCategory.Special: return "Special_特殊物品";
                default: return "Unknown";
            }
        }
        
        /// <summary>
        /// 保存装备系统数据到PlayerPrefs
        /// </summary>
        /// <param name="saveData">要保存的数据</param>
        /// <param name="saveSlot">保存槽位</param>
        public static void SaveEquipmentDataToPlayerPrefs(EquipmentSystemSaveData saveData, string saveSlot = "default")
        {
            string key = $"{EQUIPMENT_SAVE_KEY}_{saveSlot}";
            string jsonData = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(key, jsonData);
            PlayerPrefs.Save();
            
            Debug.Log($"[EquipmentSlotSaveExtension] 装备数据已保存到 PlayerPrefs: {key}");
        }
        
        /// <summary>
        /// 从PlayerPrefs加载装备系统数据
        /// </summary>
        /// <param name="saveSlot">保存槽位</param>
        /// <returns>装备系统数据</returns>
        public static EquipmentSystemSaveData LoadEquipmentDataFromPlayerPrefs(string saveSlot = "default")
        {
            string key = $"{EQUIPMENT_SAVE_KEY}_{saveSlot}";
            
            if (PlayerPrefs.HasKey(key))
            {
                string jsonData = PlayerPrefs.GetString(key);
                try
                {
                    var saveData = JsonUtility.FromJson<EquipmentSystemSaveData>(jsonData);
                    Debug.Log($"[EquipmentSlotSaveExtension] 装备数据已从 PlayerPrefs 加载: {key}");
                    return saveData;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EquipmentSlotSaveExtension] 装备数据解析失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[EquipmentSlotSaveExtension] 未找到装备存档数据: {key}");
            }
            
            return new EquipmentSystemSaveData();
        }
        
        /// <summary>
        /// 检查是否存在装备存档数据
        /// </summary>
        /// <param name="saveSlot">保存槽位</param>
        /// <returns>是否存在存档</returns>
        public static bool HasEquipmentSaveData(string saveSlot = "default")
        {
            string key = $"{EQUIPMENT_SAVE_KEY}_{saveSlot}";
            return PlayerPrefs.HasKey(key);
        }
        
        /// <summary>
        /// 删除装备存档数据
        /// </summary>
        /// <param name="saveSlot">保存槽位</param>
        public static void DeleteEquipmentSaveData(string saveSlot = "default")
        {
            string key = $"{EQUIPMENT_SAVE_KEY}_{saveSlot}";
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                Debug.Log($"[EquipmentSlotSaveExtension] 已删除装备存档数据: {key}");
            }
        }
        
        #region ES3存储支持
        
        /// <summary>
        /// 保存装备系统数据到ES3文件
        /// </summary>
        /// <param name="saveData">要保存的数据</param>
        /// <param name="filePath">ES3文件路径</param>
        public static void SaveEquipmentDataToES3(EquipmentSystemSaveData saveData, string filePath)
        {
            try
            {
                // 创建备份
                string backupPath = filePath.Replace(".es3", "_backup.es3");
                if (ES3.FileExists(filePath))
                {
                    ES3.CopyFile(filePath, backupPath);
                }
                
                // 保存数据
                ES3.Save("EquipmentData", saveData, filePath);
                
                Debug.Log($"[EquipmentSlotSaveExtension] 装备数据已保存到ES3文件: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlotSaveExtension] 保存装备数据到ES3失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 从ES3文件加载装备系统数据
        /// </summary>
        /// <param name="filePath">ES3文件路径</param>
        /// <returns>装备系统数据</returns>
        public static EquipmentSystemSaveData LoadEquipmentDataFromES3(string filePath)
        {
            try
            {
                if (ES3.FileExists(filePath) && ES3.KeyExists("EquipmentData", filePath))
                {
                    var saveData = ES3.Load<EquipmentSystemSaveData>("EquipmentData", filePath);
                    Debug.Log($"[EquipmentSlotSaveExtension] 装备数据已从ES3文件加载: {filePath}");
                    return saveData;
                }
                else
                {
                    Debug.LogWarning($"[EquipmentSlotSaveExtension] ES3文件不存在或无数据: {filePath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlotSaveExtension] 从ES3文件加载装备数据失败: {e.Message}");
                
                // 尝试从备份加载
                string backupPath = filePath.Replace(".es3", "_backup.es3");
                if (ES3.FileExists(backupPath))
                {
                    try
                    {
                        var backupData = ES3.Load<EquipmentSystemSaveData>("EquipmentData", backupPath);
                        Debug.LogWarning($"[EquipmentSlotSaveExtension] 已从备份文件恢复: {backupPath}");
                        return backupData;
                    }
                    catch (System.Exception backupError)
                    {
                        Debug.LogError($"[EquipmentSlotSaveExtension] 备份文件也无法加载: {backupError.Message}");
                    }
                }
            }
            
            return new EquipmentSystemSaveData();
        }
        
        /// <summary>
        /// 检查ES3文件中是否存在装备存档数据
        /// </summary>
        /// <param name="filePath">ES3文件路径</param>
        /// <returns>是否存在存档</returns>
        public static bool HasEquipmentSaveDataInES3(string filePath)
        {
            return ES3.FileExists(filePath) && ES3.KeyExists("EquipmentData", filePath);
        }
        
        /// <summary>
        /// 删除ES3文件中的装备存档数据
        /// </summary>
        /// <param name="filePath">ES3文件路径</param>
        public static void DeleteEquipmentSaveDataFromES3(string filePath)
        {
            try
            {
                if (ES3.FileExists(filePath))
                {
                    ES3.DeleteFile(filePath);
                    Debug.Log($"[EquipmentSlotSaveExtension] 已删除ES3装备存档: {filePath}");
                }
                
                // 删除备份文件
                string backupPath = filePath.Replace(".es3", "_backup.es3");
                if (ES3.FileExists(backupPath))
                {
                    ES3.DeleteFile(backupPath);
                    Debug.Log($"[EquipmentSlotSaveExtension] 已删除ES3装备备份: {backupPath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlotSaveExtension] 删除ES3装备存档失败: {e.Message}");
            }
        }
        
        #endregion
    }
}
