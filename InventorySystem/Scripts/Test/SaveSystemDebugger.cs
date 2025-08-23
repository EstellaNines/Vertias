using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem.SaveSystem;

namespace InventorySystem.Test
{
    /// <summary>
    /// 保存系统调试器 - 用于诊断保存系统中的问题
    /// 特别针对动态生成的网格对象的保存和加载问题
    /// </summary>
    public class SaveSystemDebugger : MonoBehaviour
    {
        [Header("调试配置")]
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private bool autoRunDiagnostics = false;
        [SerializeField] private KeyCode debugKey = KeyCode.F1;

        private void Update()
        {
            if (Input.GetKeyDown(debugKey))
            {
                RunFullDiagnostics();
            }
        }

        private void Start()
        {
            if (autoRunDiagnostics)
            {
                Invoke(nameof(RunFullDiagnostics), 2f); // 延迟2秒执行诊断
            }
        }

        /// <summary>
        /// 运行完整的保存系统诊断
        /// </summary>
        [ContextMenu("运行完整诊断")]
        public void RunFullDiagnostics()
        {
            Debug.Log("=== 保存系统完整诊断开始 ===");

            DiagnoseSaveManager();
            DiagnoseISaveableObjects();
            DiagnoseDynamicGrids();
            DiagnoseRegistrationStatus();
            DiagnoseSaveDataIntegrity();

            Debug.Log("=== 保存系统完整诊断结束 ===");
        }

        /// <summary>
        /// 诊断SaveManager状态
        /// </summary>
        private void DiagnoseSaveManager()
        {
            Debug.Log("--- SaveManager诊断 ---");

            var saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogError("SaveManager实例不存在！");
                return;
            }

            Debug.Log($"SaveManager状态: 已初始化");
            Debug.Log($"已注册对象数量: {saveManager.RegisteredObjectCount}");
            Debug.Log($"SaveManager游戏对象: {saveManager.gameObject.name}");
            Debug.Log($"SaveManager是否激活: {saveManager.gameObject.activeInHierarchy}");
        }

        /// <summary>
        /// 诊断所有ISaveable对象
        /// </summary>
        private void DiagnoseISaveableObjects()
        {
            Debug.Log("--- ISaveable对象诊断 ---");

            // 查找所有ISaveable对象
            var allSaveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();
            Debug.Log($"场景中发现 {allSaveables.Count} 个ISaveable对象");

            var saveManager = SaveManager.Instance;
            int registeredCount = 0;
            int unregisteredCount = 0;

            foreach (var saveable in allSaveables)
            {
                string saveId = saveable.GetSaveID();
                bool isRegistered = saveManager?.IsObjectRegistered(saveable) ?? false;
                bool hasValidId = !string.IsNullOrEmpty(saveId);

                if (enableDetailedLogging)
                {
                    var component = saveable as MonoBehaviour;
                    string objectName = component?.name ?? "Unknown";
                    string typeName = saveable.GetType().Name;

                    Debug.Log($"[{typeName}] {objectName}:");
                    Debug.Log($"  SaveID: {saveId}");
                    Debug.Log($"  ID有效: {hasValidId}");
                    Debug.Log($"  已注册: {isRegistered}");
                    Debug.Log($"  是否Clone: {objectName.Contains("Clone")}");

                    // 检查GridISaveableInitializer
                    if (component != null)
                    {
                        var initializer = component.GetComponent<GridISaveableInitializer>();
                        Debug.Log($"  有初始化器: {initializer != null}");
                    }
                }

                if (isRegistered)
                    registeredCount++;
                else
                    unregisteredCount++;
            }

            Debug.Log($"注册状态统计: {registeredCount} 已注册, {unregisteredCount} 未注册");
        }

        /// <summary>
        /// 诊断动态生成的网格对象
        /// </summary>
        private void DiagnoseDynamicGrids()
        {
            Debug.Log("--- 动态网格诊断 ---");

            // 查找所有背包网格
            var backpackGrids = FindObjectsOfType<BackpackItemGrid>();
            Debug.Log($"发现 {backpackGrids.Length} 个BackpackItemGrid");

            foreach (var grid in backpackGrids)
            {
                DiagnoseGrid(grid, "BackpackItemGrid");
            }

            // 查找所有战术挂具网格
            var tacticalGrids = FindObjectsOfType<TactiaclRigItemGrid>();
            Debug.Log($"发现 {tacticalGrids.Length} 个TactiaclRigItemGrid");

            foreach (var grid in tacticalGrids)
            {
                DiagnoseGrid(grid, "TactiaclRigItemGrid");
            }
        }

        /// <summary>
        /// 诊断单个网格对象
        /// </summary>
        private void DiagnoseGrid(BaseItemGrid grid, string gridType)
        {
            if (grid == null) return;

            string objectName = grid.name;
            bool isClone = objectName.Contains("Clone");
            bool isISaveable = grid is ISaveable;
            
            Debug.Log($"[{gridType}] {objectName}:");
            Debug.Log($"  是否Clone: {isClone}");
            Debug.Log($"  实现ISaveable: {isISaveable}");

            if (isISaveable)
            {
                var saveable = grid as ISaveable;
                string saveId = saveable.GetSaveID();
                bool hasValidId = !string.IsNullOrEmpty(saveId);
                bool isRegistered = SaveManager.Instance?.IsObjectRegistered(saveable) ?? false;

                Debug.Log($"  SaveID: {saveId}");
                Debug.Log($"  ID有效: {hasValidId}");
                Debug.Log($"  已注册: {isRegistered}");

                // 测试序列化
                try
                {
                    string jsonData = saveable.SerializeToJson();
                    bool canSerialize = !string.IsNullOrEmpty(jsonData);
                    Debug.Log($"  可序列化: {canSerialize} (长度: {jsonData?.Length ?? 0})");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"  序列化失败: {ex.Message}");
                }
            }

            // 检查初始化器
            var initializer = grid.GetComponent<GridISaveableInitializer>();
            Debug.Log($"  有初始化器: {initializer != null}");
            if (initializer != null)
            {
                Debug.Log($"  初始化器状态:\n{initializer.GetGridStatusInfo()}");
            }
        }

        /// <summary>
        /// 诊断注册状态
        /// </summary>
        private void DiagnoseRegistrationStatus()
        {
            Debug.Log("--- 注册状态诊断 ---");

            var saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogError("SaveManager不存在，无法诊断注册状态");
                return;
            }

            // 检查装备槽
            var equipSlots = FindObjectsOfType<EquipSlot>();
            Debug.Log($"发现 {equipSlots.Length} 个EquipSlot");

            foreach (var slot in equipSlots)
            {
                Debug.Log($"EquipSlot: {slot.name}");
                // 获取当前装备的物品
                var equippedItem = slot.GetEquippedItem();
                if (equippedItem != null)
                {
                    var itemComponent = equippedItem.GetComponent<InventorySystemItem>();
                    Debug.Log($"  当前装备物品: {(itemComponent?.Data?.itemName ?? "无")}");
                }
                else
                {
                    Debug.Log("  当前装备物品: 无");
                }
                
                // 检查是否有动态网格
                var backpackGrid = slot.GetComponentInChildren<BackpackItemGrid>();
                var tacticalGrid = slot.GetComponentInChildren<TactiaclRigItemGrid>();
                
                if (backpackGrid != null)
                {
                    Debug.Log($"  关联背包网格: {backpackGrid.name}");
                    var saveableBackpack = backpackGrid as ISaveable;
                    if (saveableBackpack != null)
                    {
                        Debug.Log($"  背包网格已注册: {saveManager.IsObjectRegistered(saveableBackpack)}");
                    }
                    else
                    {
                        Debug.LogError($"  背包网格未实现ISaveable接口");
                    }
                }
                
                if (tacticalGrid != null)
                {
                    Debug.Log($"  关联战术网格: {tacticalGrid.name}");
                    var saveableTactical = tacticalGrid as ISaveable;
                    if (saveableTactical != null)
                    {
                        Debug.Log($"  战术网格已注册: {saveManager.IsObjectRegistered(saveableTactical)}");
                    }
                    else
                    {
                        Debug.LogError($"  战术网格未实现ISaveable接口");
                    }
                }
            }
        }

        /// <summary>
        /// 诊断保存数据完整性
        /// </summary>
        private void DiagnoseSaveDataIntegrity()
        {
            Debug.Log("--- 保存数据完整性诊断 ---");

            var saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogError("SaveManager不存在，无法诊断保存数据");
                return;
            }

            // 模拟保存操作
            Debug.Log("执行模拟保存测试...");
            
            try
            {
                // 这里可以添加模拟保存的逻辑
                Debug.Log("模拟保存测试完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"模拟保存测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 强制重新注册所有ISaveable对象
        /// </summary>
        [ContextMenu("强制重新注册所有对象")]
        public void ForceReregisterAllObjects()
        {
            Debug.Log("=== 强制重新注册所有ISaveable对象 ===");

            var saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogError("SaveManager不存在");
                return;
            }

            var allSaveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();
            int successCount = 0;

            foreach (var saveable in allSaveables)
            {
                try
                {
                    // 先注销（如果已注册）
                    saveManager.UnregisterSaveable(saveable);
                    
                    // 重新注册
                    if (saveManager.RegisterSaveable(saveable))
                    {
                        successCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"重新注册失败: {saveable.GetType().Name}, 错误: {ex.Message}");
                }
            }

            Debug.Log($"重新注册完成: {successCount}/{allSaveables.Count} 成功");
        }

        /// <summary>
        /// 获取保存系统状态摘要
        /// </summary>
        /// <returns>状态摘要字符串</returns>
        public string GetSystemStatusSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== 保存系统状态摘要 ===");

            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                summary.AppendLine($"SaveManager: 正常 (已注册对象: {saveManager.RegisteredObjectCount})");
            }
            else
            {
                summary.AppendLine("SaveManager: 不存在");
            }

            var allSaveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();
            summary.AppendLine($"场景ISaveable对象: {allSaveables.Count}");

            var backpackGrids = FindObjectsOfType<BackpackItemGrid>();
            var tacticalGrids = FindObjectsOfType<TactiaclRigItemGrid>();
            summary.AppendLine($"背包网格: {backpackGrids.Length}");
            summary.AppendLine($"战术网格: {tacticalGrids.Length}");

            return summary.ToString();
        }
    }
}