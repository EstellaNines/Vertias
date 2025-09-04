using System.Collections;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 跨会话持久化测试脚本
    /// 用于验证修复后的跨会话容器恢复功能
    /// </summary>
    public class CrossSessionPersistenceTest : MonoBehaviour
    {
        [Header("测试设置")]
        [Tooltip("是否启用详细日志")]
        public bool verboseLogging = true;
        
        [Tooltip("测试延迟时间（秒）")]
        public float testDelay = 2f;

        private void Start()
        {
            if (verboseLogging)
            {
                StartCoroutine(TestCrossSessionPersistence());
            }
        }

        /// <summary>
        /// 测试跨会话持久化功能
        /// </summary>
        private IEnumerator TestCrossSessionPersistence()
        {
            yield return new WaitForSeconds(testDelay);
            
            LogTest("=== 跨会话持久化测试开始 ===");
            
            // 测试1: 检查ContainerSessionManager状态
            var sessionManager = FindObjectOfType<ContainerSessionManager>();
            if (sessionManager != null)
            {
                LogTest("�7�3 ContainerSessionManager已找到");
                LogTest($"状态: {sessionManager.GetCurrentStatus()}");
            }
            else
            {
                LogTest("�7�4 ContainerSessionManager未找到");
            }
            
            // 测试2: 检查EquipmentPersistenceManager事件
            LogTest("�9�3 检查装备恢复事件机制...");
            if (EquipmentPersistenceManager.OnEquipmentRestored != null)
            {
                var listeners = EquipmentPersistenceManager.OnEquipmentRestored.GetInvocationList();
                LogTest($"�7�3 装备恢复事件有 {listeners.Length} 个监听器");
                
                foreach (var listener in listeners)
                {
                    LogTest($"  - 监听器: {listener.Target?.GetType().Name}.{listener.Method.Name}");
                }
            }
            else
            {
                LogTest("�7�4 没有装备恢复事件监听器");
            }
            
            // 测试3: 检查ContainerSaveManager状态
            var containerManager = ContainerSaveManager.Instance;
            if (containerManager != null)
            {
                LogTest("�7�3 ContainerSaveManager已找到");
                
                // 检查是否有保存的跨会话数据
                yield return new WaitForSeconds(0.5f);
                LogTest("�9�3 检查跨会话数据状态...");
            }
            else
            {
                LogTest("�7�4 ContainerSaveManager未找到");
            }
            
            // 测试4: 检查装备槽状态
            var slotManager = EquipmentSlotManager.Instance;
            if (slotManager != null)
            {
                LogTest("�7�3 EquipmentSlotManager已找到");
                
                var backpackSlot = slotManager.GetEquipmentSlot(EquipmentSlotType.Backpack);
                if (backpackSlot != null && backpackSlot.HasEquippedItem)
                {
                    LogTest("�7�3 背包已装备");
                    
                    var containerGrid = backpackSlot.GetComponentInChildren<ItemGrid>();
                    if (containerGrid != null)
                    {
                        int itemCount = 0;
                        for (int x = 0; x < containerGrid.gridSizeWidth; x++)
                        {
                            for (int y = 0; y < containerGrid.gridSizeHeight; y++)
                            {
                                if (containerGrid.GetItemAt(x, y) != null)
                                {
                                    itemCount++;
                                }
                            }
                        }
                        LogTest($"�9�4 背包容器中有 {itemCount} 个物品");
                    }
                    else
                    {
                        LogTest("�7�4 背包容器网格未找到");
                    }
                }
                else
                {
                    LogTest("�7�2�1�5 背包未装备");
                }
            }
            else
            {
                LogTest("�7�4 EquipmentSlotManager未找到");
            }
            
            LogTest("=== 跨会话持久化测试完成 ===");
        }

        private void LogTest(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[CrossSessionTest] {message}");
            }
        }
        
        /// <summary>
        /// 手动强制保存跨会话数据（测试用）
        /// </summary>
        [ContextMenu("强制保存跨会话数据")]
        public void ForceSaveCrossSessionData()
        {
            var containerManager = ContainerSaveManager.Instance;
            if (containerManager != null)
            {
                containerManager.SaveCrossSessionData();
                LogTest("�9�4 已强制保存跨会话数据");
            }
            else
            {
                LogTest("�7�4 ContainerSaveManager未找到");
            }
        }
        
        /// <summary>
        /// 清理跨会话数据（测试用）
        /// </summary>
        [ContextMenu("清理跨会话数据")]
        public void ClearCrossSessionData()
        {
            var containerManager = ContainerSaveManager.Instance;
            if (containerManager != null)
            {
                containerManager.ClearCrossSessionData();
                LogTest("�9�9�1�5 已清理跨会话数据");
            }
            else
            {
                LogTest("�7�4 ContainerSaveManager未找到");
            }
        }
        
        /// <summary>
        /// 验证跨会话数据完整性（测试用）
        /// </summary>
        [ContextMenu("验证跨会话数据")]
        public void ValidateCrossSessionData()
        {
            LogTest("�9�3 开始验证跨会话数据...");
            
            if (ES3.KeyExists("CrossSessionContainerData", "ContainerData.es3"))
            {
                try
                {
                    // 由于CrossSessionContainerData是private，我们通过ContainerSaveManager的公共方法来验证
                    var containerManager = ContainerSaveManager.Instance;
                    bool hasValidData = containerManager.LoadCrossSessionData();
                    
                    LogTest($"跨会话数据加载结果: {(hasValidData ? "成功" : "失败")}");
                }
                catch (System.Exception e)
                {
                    LogTest($"�7�4 加载跨会话数据时发生异常: {e.Message}");
                }
            }
            else
            {
                LogTest("�7�2�1�5 跨会话数据不存在");
            }
        }
    }
}
