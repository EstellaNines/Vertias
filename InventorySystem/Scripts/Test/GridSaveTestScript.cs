using UnityEngine;
using InventorySystem.SaveSystem;

namespace InventorySystem.Test
{
    /// <summary>
    /// 网格保存功能测试脚本
    /// 用于测试背包和战术挂具网格的物品保存和恢复功能
    /// </summary>
    public class GridSaveTestScript : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private InventoryController inventoryController;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private bool enableTestLogs = true;

        [Header("测试按键")]
        [SerializeField] private KeyCode saveTestKey = KeyCode.F5;
        [SerializeField] private KeyCode loadTestKey = KeyCode.F9;
        [SerializeField] private KeyCode statusTestKey = KeyCode.F12;

        private void Start()
        {
            // 自动查找组件
            if (inventoryController == null)
            {
                inventoryController = FindObjectOfType<InventoryController>();
            }

            if (saveManager == null)
            {
                saveManager = SaveManager.Instance;
            }

            LogTest("网格保存测试脚本已启动");
            LogTest("按键说明:");
            LogTest($"  {saveTestKey} - 执行保存测试");
            LogTest($"  {loadTestKey} - 执行加载测试");
            LogTest($"  {statusTestKey} - 显示保存系统状态");
        }

        private void Update()
        {
            // 处理测试按键
            if (Input.GetKeyDown(saveTestKey))
            {
                TestSaveFunction();
            }

            if (Input.GetKeyDown(loadTestKey))
            {
                TestLoadFunction();
            }

            if (Input.GetKeyDown(statusTestKey))
            {
                ShowSaveSystemStatus();
            }
        }

        /// <summary>
        /// 测试保存功能
        /// </summary>
        private void TestSaveFunction()
        {
            LogTest("=== 开始保存功能测试 ===");

            if (saveManager == null)
            {
                LogTest("错误：SaveManager未找到");
                return;
            }

            try
            {
                // 执行快速保存
                saveManager.QuickSave();
                LogTest("保存操作已执行");

                // 显示已注册的网格信息
                ShowRegisteredGrids();
            }
            catch (System.Exception ex)
            {
                LogTest($"保存测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试加载功能
        /// </summary>
        private void TestLoadFunction()
        {
            LogTest("=== 开始加载功能测试 ===");

            if (saveManager == null)
            {
                LogTest("错误：SaveManager未找到");
                return;
            }

            try
            {
                // 执行快速加载
                saveManager.QuickLoad();
                LogTest("加载操作已执行");
            }
            catch (System.Exception ex)
            {
                LogTest($"加载测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示保存系统状态
        /// </summary>
        private void ShowSaveSystemStatus()
        {
            LogTest("=== 保存系统状态 ===");

            if (saveManager == null)
            {
                LogTest("错误：SaveManager未找到");
                return;
            }

            LogTest($"已注册对象数量: {saveManager.RegisteredObjectCount}");
            LogTest($"自动保存状态: {(saveManager.IsAutoSaveEnabled ? "启用" : "禁用")}");
            LogTest($"自动保存间隔: {saveManager.AutoSaveInterval}秒");

            // 显示已注册的对象ID
            var registeredIds = saveManager.GetAllRegisteredIds();
            LogTest($"已注册对象ID列表 ({registeredIds.Count}个):");
            foreach (var id in registeredIds)
            {
                LogTest($"  - {id}");
            }
        }

        /// <summary>
        /// 显示已注册的网格信息
        /// </summary>
        private void ShowRegisteredGrids()
        {
            LogTest("=== 已注册网格信息 ===");

            if (saveManager == null) return;

            var registeredIds = saveManager.GetAllRegisteredIds();
            int gridCount = 0;

            foreach (var id in registeredIds)
            {
                if (id.Contains("Backpack") || id.Contains("TacticalRig"))
                {
                    gridCount++;
                    var obj = saveManager.GetRegisteredObject(id);
                    if (obj != null)
                    {
                        LogTest($"  网格 {gridCount}: {id}");
                        LogTest($"    类型: {obj.GetType().Name}");
                        LogTest($"    是否已修改: {obj.IsModified()}");
                    }
                }
            }

            if (gridCount == 0)
            {
                LogTest("  未找到已注册的背包或战术挂具网格");
            }
        }

        /// <summary>
        /// 记录测试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        private void LogTest(string message)
        {
            if (enableTestLogs)
            {
                Debug.Log($"[GridSaveTest] {message}");
            }
        }

        /// <summary>
        /// 在Inspector中显示测试按钮
        /// </summary>
        [ContextMenu("执行保存测试")]
        private void InspectorSaveTest()
        {
            TestSaveFunction();
        }

        [ContextMenu("执行加载测试")]
        private void InspectorLoadTest()
        {
            TestLoadFunction();
        }

        [ContextMenu("显示系统状态")]
        private void InspectorStatusTest()
        {
            ShowSaveSystemStatus();
        }
    }
}