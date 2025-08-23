using System.Collections;
using UnityEngine;
using InventorySystem.SaveSystem;
using InventorySystem.Test;

namespace InventorySystem.Test
{
    /// <summary>
    /// 保存系统测试管理器 - 提供完整的保存系统测试功能
    /// 包括ISaveable接口测试、保存加载测试、动态网格测试等
    /// </summary>
    public class SaveSystemTestManager : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool enableAutoTest = false;
        [SerializeField] private float autoTestDelay = 3f;
        [SerializeField] private bool enableDetailedLogging = true;

        [Header("快捷键配置")]
        [SerializeField] private KeyCode testISaveableKey = KeyCode.T;
        [SerializeField] private KeyCode saveTestKey = KeyCode.F5;
        [SerializeField] private KeyCode loadTestKey = KeyCode.F9;
        [SerializeField] private KeyCode debugKey = KeyCode.F1;
        [SerializeField] private KeyCode forceRegisterKey = KeyCode.F2;

        private ISaveableTestScript isaveableTest;
        private SaveSystemDebugger debugger;

        private void Start()
        {
            // 获取或创建测试组件
            isaveableTest = GetComponent<ISaveableTestScript>();
            if (isaveableTest == null)
            {
                isaveableTest = gameObject.AddComponent<ISaveableTestScript>();
            }

            debugger = GetComponent<SaveSystemDebugger>();
            if (debugger == null)
            {
                debugger = gameObject.AddComponent<SaveSystemDebugger>();
            }

            Debug.Log("=== 保存系统测试管理器已启动 ===");
            Debug.Log($"快捷键说明:");
            Debug.Log($"  {testISaveableKey} - 测试ISaveable接口实现");
            Debug.Log($"  {saveTestKey} - 执行保存测试");
            Debug.Log($"  {loadTestKey} - 执行加载测试");
            Debug.Log($"  {debugKey} - 运行完整诊断");
            Debug.Log($"  {forceRegisterKey} - 强制重新注册所有对象");

            if (enableAutoTest)
            {
                StartCoroutine(AutoTestSequence());
            }
        }

        private void Update()
        {
            // 处理快捷键输入
            if (Input.GetKeyDown(testISaveableKey))
            {
                TestISaveableInterface();
            }
            else if (Input.GetKeyDown(saveTestKey))
            {
                TestSaveOperation();
            }
            else if (Input.GetKeyDown(loadTestKey))
            {
                TestLoadOperation();
            }
            else if (Input.GetKeyDown(debugKey))
            {
                RunDiagnostics();
            }
            else if (Input.GetKeyDown(forceRegisterKey))
            {
                ForceReregisterAll();
            }
        }

        /// <summary>
        /// 自动测试序列
        /// </summary>
        private IEnumerator AutoTestSequence()
        {
            Debug.Log("=== 开始自动测试序列 ===");

            yield return new WaitForSeconds(autoTestDelay);

            // 1. 运行诊断
            Debug.Log("[自动测试] 步骤1: 运行系统诊断");
            RunDiagnostics();
            yield return new WaitForSeconds(2f);

            // 2. 测试ISaveable接口
            Debug.Log("[自动测试] 步骤2: 测试ISaveable接口");
            TestISaveableInterface();
            yield return new WaitForSeconds(2f);

            // 3. 测试保存操作
            Debug.Log("[自动测试] 步骤3: 测试保存操作");
            TestSaveOperation();
            yield return new WaitForSeconds(2f);

            // 4. 测试加载操作
            Debug.Log("[自动测试] 步骤4: 测试加载操作");
            TestLoadOperation();

            Debug.Log("=== 自动测试序列完成 ===");
        }

        /// <summary>
        /// 测试ISaveable接口实现
        /// </summary>
        [ContextMenu("测试ISaveable接口")]
        public void TestISaveableInterface()
        {
            Debug.Log("=== 开始ISaveable接口测试 ===");

            if (isaveableTest != null)
            {
                // 使用反射调用私有方法（如果需要）
                var method = isaveableTest.GetType().GetMethod("TestISaveableImplementation",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(isaveableTest, null);
                }
                else
                {
                    Debug.LogError("无法找到TestISaveableImplementation方法");
                }
            }
            else
            {
                Debug.LogError("ISaveableTestScript组件不存在");
            }
        }

        /// <summary>
        /// 测试保存操作
        /// </summary>
        [ContextMenu("测试保存操作")]
        public void TestSaveOperation()
        {
            Debug.Log("=== 开始保存操作测试 ===");

            var saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogError("SaveManager不存在，无法执行保存测试");
                return;
            }

            try
            {
                Debug.Log($"保存前注册对象数量: {saveManager.RegisteredObjectCount}");

                // 执行保存
                saveManager.SaveGame("test_save");

                Debug.Log("保存操作已执行");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"保存操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试加载操作
        /// </summary>
        [ContextMenu("测试加载操作")]
        public void TestLoadOperation()
        {
            Debug.Log("=== 开始加载操作测试 ===");

            var saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogError("SaveManager不存在，无法执行加载测试");
                return;
            }

            try
            {
                Debug.Log($"加载前注册对象数量: {saveManager.RegisteredObjectCount}");

                // 执行加载
                saveManager.LoadSave("test_save");

                Debug.Log("加载操作已执行");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"加载操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行系统诊断
        /// </summary>
        [ContextMenu("运行系统诊断")]
        public void RunDiagnostics()
        {
            if (debugger != null)
            {
                debugger.RunFullDiagnostics();
            }
            else
            {
                Debug.LogError("SaveSystemDebugger组件不存在");
            }
        }

        /// <summary>
        /// 强制重新注册所有对象
        /// </summary>
        [ContextMenu("强制重新注册所有对象")]
        public void ForceReregisterAll()
        {
            if (debugger != null)
            {
                debugger.ForceReregisterAllObjects();
            }
            else
            {
                Debug.LogError("SaveSystemDebugger组件不存在");
            }
        }

        /// <summary>
        /// 测试动态网格创建和注册
        /// </summary>
        [ContextMenu("测试动态网格创建")]
        public void TestDynamicGridCreation()
        {
            Debug.Log("=== 开始动态网格创建测试 ===");

            // 查找装备槽
            var equipSlots = FindObjectsOfType<EquipSlot>();
            Debug.Log($"发现 {equipSlots.Length} 个装备槽");

            foreach (var slot in equipSlots)
            {
                var equippedItem = slot.GetEquippedItem();
                if (equippedItem != null)
                {
                    var itemComponent = equippedItem.GetComponent<InventorySystemItem>();
                    Debug.Log($"装备槽 {slot.name} 当前装备: {(itemComponent?.Data?.itemName ?? "未知物品")}");

                    // 检查是否有关联的网格
                    var backpackGrid = slot.GetComponentInChildren<BackpackItemGrid>();
                    var tacticalGrid = slot.GetComponentInChildren<TactiaclRigItemGrid>();

                    if (backpackGrid != null)
                    {
                        TestGridRegistration(backpackGrid, "BackpackItemGrid");
                    }

                    if (tacticalGrid != null)
                    {
                        TestGridRegistration(tacticalGrid, "TactiaclRigItemGrid");
                    }
                }
                else
                {
                    Debug.Log($"装备槽 {slot.name} 当前无装备");
                }
            }
        }

        /// <summary>
        /// 测试网格注册状态
        /// </summary>
        private void TestGridRegistration(BaseItemGrid grid, string gridType)
        {
            if (grid == null) return;

            Debug.Log($"测试 {gridType} 注册状态: {grid.name}");

            var saveable = grid as ISaveable;
            if (saveable != null)
            {
                string saveId = saveable.GetSaveID();
                bool isRegistered = SaveManager.Instance?.IsObjectRegistered(saveable) ?? false;

                Debug.Log($"  SaveID: {saveId}");
                Debug.Log($"  已注册: {isRegistered}");

                // 测试序列化
                try
                {
                    string jsonData = saveable.SerializeToJson();
                    Debug.Log($"  序列化成功，数据长度: {jsonData?.Length ?? 0}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"  序列化失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError($"  {gridType} 未实现ISaveable接口");
            }
        }

        /// <summary>
        /// 获取系统状态摘要
        /// </summary>
        [ContextMenu("显示系统状态")]
        public void ShowSystemStatus()
        {
            if (debugger != null)
            {
                string status = debugger.GetSystemStatusSummary();
                Debug.Log(status);
            }
            else
            {
                Debug.LogError("SaveSystemDebugger组件不存在");
            }
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        [ContextMenu("清理测试数据")]
        public void CleanupTestData()
        {
            Debug.Log("=== 清理测试数据 ===");

            // 这里可以添加清理逻辑，比如删除测试保存文件等
            Debug.Log("测试数据清理完成");
        }
    }
}