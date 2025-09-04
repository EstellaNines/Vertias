using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 装备系统迁移工具
    /// 
    /// 【功能说明】
    /// 这个脚本用于从旧的EquipmentSlotSaveExtension系统迁移到新的EquipmentPersistenceManager系统。
    /// 主要负责清理旧系统的冗余代码调用，避免新旧系统冲突。
    /// 
    /// 【使用方式】
    /// 1. 在编辑器中运行迁移工具
    /// 2. 或者在运行时自动检测和处理冲突
    /// </summary>
    public static class EquipmentSystemMigration
    {
        /// <summary>
        /// 检查是否存在新旧系统冲突
        /// </summary>
        /// <returns>是否存在冲突</returns>
        public static bool HasSystemConflict()
        {
            // 检查是否同时存在新旧系统的数据
            bool hasOldData = EquipmentSlotSaveExtension.HasEquipmentSaveData();
            bool hasNewData = false;
            
            var newManager = EquipmentPersistenceManager.Instance;
            if (newManager != null)
            {
                hasNewData = newManager.HasSavedData();
            }
            
            return hasOldData && hasNewData;
        }
        
        /// <summary>
        /// 从旧系统迁移数据到新系统
        /// </summary>
        /// <returns>迁移是否成功</returns>
        public static bool MigrateFromOldSystem()
        {
            try
            {
                Debug.Log("[EquipmentSystemMigration] 开始从旧系统迁移数据");
                
                // 检查旧系统是否有数据
                if (!EquipmentSlotSaveExtension.HasEquipmentSaveData())
                {
                    Debug.Log("[EquipmentSystemMigration] 旧系统没有数据，跳过迁移");
                    return true;
                }
                
                // 加载旧系统数据
                var oldData = EquipmentSlotSaveExtension.LoadEquipmentDataFromPlayerPrefs();
                if (oldData == null || oldData.equipmentSlots == null)
                {
                    Debug.LogWarning("[EquipmentSystemMigration] 旧系统数据无效");
                    return false;
                }
                
                Debug.Log($"[EquipmentSystemMigration] 发现旧数据: {oldData.equipmentSlots.Count} 个装备槽");
                
                // 获取新系统管理器
                var newManager = EquipmentPersistenceManager.Instance;
                if (newManager == null)
                {
                    Debug.LogError("[EquipmentSystemMigration] 无法获取新的装备持久化管理器");
                    return false;
                }
                
                // 创建新系统数据结构
                var newData = new EquipmentSystemPersistenceData();
                
                // 转换数据格式
                foreach (var oldSlot in oldData.equipmentSlots)
                {
                    if (oldSlot.hasEquippedItem && oldSlot.equippedItemData != null)
                    {
                        var newSlot = new EquipmentSlotPersistenceData
                        {
                            slotType = oldSlot.slotType,
                            slotName = oldSlot.slotName,
                            hasEquipment = true,
                            itemID = oldSlot.equippedItemData.itemID,
                            itemName = oldSlot.equippedItemData.itemID, // 使用ID作为临时名称
                            runtimeData = new ItemRuntimeData
                            {
                                stackCount = oldSlot.equippedItemData.stackCount,
                                durability = oldSlot.equippedItemData.durability,
                                usageCount = oldSlot.equippedItemData.usageCount
                            },
                            saveTimestamp = oldSlot.saveTime
                        };
                        
                        newData.equipmentSlots.Add(newSlot);
                    }
                    else
                    {
                        var emptySlot = new EquipmentSlotPersistenceData
                        {
                            slotType = oldSlot.slotType,
                            slotName = oldSlot.slotName,
                            hasEquipment = false
                        };
                        
                        newData.equipmentSlots.Add(emptySlot);
                    }
                }
                
                // 保存到新系统
                bool saveSuccess = newManager.SaveDataToStorage(newData);
                if (saveSuccess)
                {
                    Debug.Log($"[EquipmentSystemMigration] �7�3 成功迁移 {newData.equipmentSlots.Count} 个装备槽数据");
                    
                    // 迁移成功后，清理旧数据
                    CleanupOldSystemData();
                    
                    return true;
                }
                else
                {
                    Debug.LogError("[EquipmentSystemMigration] �7�4 保存新系统数据失败");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSystemMigration] 迁移过程中发生异常: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 清理旧系统数据
        /// </summary>
        public static void CleanupOldSystemData()
        {
            try
            {
                Debug.Log("[EquipmentSystemMigration] 开始清理旧系统数据");
                
                // 删除旧系统的PlayerPrefs数据
                EquipmentSlotSaveExtension.DeleteEquipmentSaveData();
                
                Debug.Log("[EquipmentSystemMigration] �7�3 旧系统数据清理完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSystemMigration] 清理旧系统数据时发生异常: {e.Message}");
            }
        }
        
        /// <summary>
        /// 生成系统状态报告
        /// </summary>
        /// <returns>状态报告</returns>
        public static string GenerateSystemStatusReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== 装备系统状态报告 ===");
            
            // 检查新系统
            var newManager = EquipmentPersistenceManager.Instance;
            if (newManager != null)
            {
                report.AppendLine("�7�3 新系统 (EquipmentPersistenceManager): 已启用");
                report.AppendLine($"   - 初始化状态: {newManager.IsInitialized}");
                report.AppendLine($"   - 存在保存数据: {newManager.HasSavedData()}");
            }
            else
            {
                report.AppendLine("�7�4 新系统 (EquipmentPersistenceManager): 未启用");
            }
            
            // 检查旧系统
            bool hasOldData = EquipmentSlotSaveExtension.HasEquipmentSaveData();
            report.AppendLine($"�9�4 旧系统 (EquipmentSlotSaveExtension): {(hasOldData ? "存在数据" : "无数据")}");
            
            // 检查冲突
            if (HasSystemConflict())
            {
                report.AppendLine("�7�2�1�5  警告: 检测到新旧系统数据冲突");
                report.AppendLine("   建议: 运行数据迁移工具");
            }
            else
            {
                report.AppendLine("�7�3 无系统冲突");
            }
            
            return report.ToString();
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器菜单：迁移装备系统数据
        /// </summary>
        [UnityEditor.MenuItem("Tools/Inventory System/Migrate Equipment System Data", false, 300)]
        public static void MigrateEquipmentSystemData()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("请在运行时执行数据迁移");
                return;
            }
            
            Debug.Log("=== 开始装备系统数据迁移 ===");
            Debug.Log(GenerateSystemStatusReport());
            
            if (HasSystemConflict())
            {
                bool migrationSuccess = MigrateFromOldSystem();
                if (migrationSuccess)
                {
                    Debug.Log("=== 数据迁移完成 ===");
                    Debug.Log(GenerateSystemStatusReport());
                }
                else
                {
                    Debug.LogError("=== 数据迁移失败 ===");
                }
            }
            else
            {
                Debug.Log("=== 无需迁移 ===");
            }
        }
        
        /// <summary>
        /// 编辑器菜单：显示系统状态
        /// </summary>
        [UnityEditor.MenuItem("Tools/Inventory System/Show Equipment System Status", false, 301)]
        public static void ShowEquipmentSystemStatus()
        {
            Debug.Log(GenerateSystemStatusReport());
        }
        
        /// <summary>
        /// 编辑器菜单：清理旧系统数据
        /// </summary>
        [UnityEditor.MenuItem("Tools/Inventory System/Cleanup Old Equipment System", false, 302)]
        public static void CleanupOldEquipmentSystem()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("清理旧系统数据", 
                "这将删除所有旧装备系统的保存数据。\n\n建议先执行数据迁移。\n\n确定要继续吗？", 
                "确定", "取消"))
            {
                CleanupOldSystemData();
                Debug.Log("=== 旧系统数据清理完成 ===");
                Debug.Log(GenerateSystemStatusReport());
            }
        }
#endif
    }
}
