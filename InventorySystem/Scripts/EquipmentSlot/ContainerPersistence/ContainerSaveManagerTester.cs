using UnityEngine;
using InventorySystem.Database;

namespace InventorySystem
{
    /// <summary>
    /// ContainerSaveManager 测试器
    /// 用于验证容器持久化功能是否正常工作
    /// </summary>
    public class ContainerSaveManagerTester : MonoBehaviour
    {
        [Header("测试设置")]
        [FieldLabel("自动运行测试")]
        [Tooltip("场景启动时自动运行基础功能测试")]
        public bool autoRunTests = true;
        
        [FieldLabel("显示详细日志")]
        [Tooltip("显示详细的测试过程日志")]
        public bool verboseLogging = true;
        
        [Header("测试结果")]
        [FieldLabel("最后测试结果")]
        [SerializeField] private bool lastTestResult = false;
        
        [FieldLabel("测试状态信息")]
        [SerializeField] private string testStatusInfo = "未运行测试";
        
        private void Start()
        {
            if (autoRunTests)
            {
                // 延迟执行测试，确保所有系统初始化完成
                Invoke(nameof(RunBasicTests), 1f);
            }
        }
        
        /// <summary>
        /// 运行基础功能测试
        /// </summary>
        [ContextMenu("运行基础测试")]
        public void RunBasicTests()
        {
            StartCoroutine(RunBasicTestsCoroutine());
        }
        
        /// <summary>
        /// 运行基础功能测试协程
        /// </summary>
        private System.Collections.IEnumerator RunBasicTestsCoroutine()
        {
            LogTest("=== ContainerSaveManager 基础功能测试开始 ===");
            
            bool allTestsPassed = true;
            
            // 测试1: 检查单例实例
            allTestsPassed &= TestSingletonInstance();
            
            // 测试2: 检查数据结构
            allTestsPassed &= TestDataStructures();
            
            // 测试3: 检查EquipmentSlot集成
            allTestsPassed &= TestEquipmentSlotIntegration();
            
            // 测试4: 检查PlayerPrefs操作
            allTestsPassed &= TestES3Operations();
            
            // 测试5: 检查ItemDatabase和ItemDataSO系统
            yield return StartCoroutine(TestItemDatabaseSystemCoroutine());
            
            // 更新测试结果
            lastTestResult = allTestsPassed;
            testStatusInfo = allTestsPassed ? "所有测试通过" : "部分测试失败";
            
            LogTest($"=== 测试完成，总体结果: {(allTestsPassed ? "�7�3 通过" : "�7�4 失败")} ===");
        }
        
        /// <summary>
        /// 测试单例实例创建
        /// </summary>
        private bool TestSingletonInstance()
        {
            LogTest("--- 测试1: 单例实例创建 ---");
            
            try
            {
                var instance = ContainerSaveManager.Instance;
                if (instance != null)
                {
                    LogTest("�7�3 ContainerSaveManager 单例实例创建成功");
                    return true;
                }
                else
                {
                    LogTest("�7�4 ContainerSaveManager 单例实例创建失败");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                LogTest($"�7�4 单例实例测试异常: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 测试数据结构
        /// </summary>
        private bool TestDataStructures()
        {
            LogTest("--- 测试2: 数据结构验证 ---");
            
            try
            {
                // 测试ContainerSaveData构造
                var saveData = new ContainerSaveData();
                if (saveData.containerItems != null)
                {
                    LogTest("�7�3 ContainerSaveData 构造成功");
                }
                else
                {
                    LogTest("�7�4 ContainerSaveData 构造失败");
                    return false;
                }
                
                // 测试ContainerSaveDataCollection构造
                var collection = new ContainerSaveDataCollection();
                if (collection.containers != null)
                {
                    LogTest("�7�3 ContainerSaveDataCollection 构造成功");
                    return true;
                }
                else
                {
                    LogTest("�7�4 ContainerSaveDataCollection 构造失败");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                LogTest($"�7�4 数据结构测试异常: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 测试与EquipmentSlot的集成
        /// </summary>
        private bool TestEquipmentSlotIntegration()
        {
            LogTest("--- 测试3: EquipmentSlot集成检查 ---");
            
            var equipmentSlots = FindObjectsOfType<EquipmentSlot>();
            LogTest($"找到 {equipmentSlots.Length} 个装备槽");
            
            if (equipmentSlots.Length == 0)
            {
                LogTest("�7�2�1�5 场景中没有EquipmentSlot，跳过集成测试");
                return true; // 不算失败，只是没有测试对象
            }
            
            int containerSlotCount = 0;
            foreach (var slot in equipmentSlots)
            {
                if (slot.SlotType == EquipmentSlotType.Backpack || 
                    slot.SlotType == EquipmentSlotType.TacticalRig)
                {
                    containerSlotCount++;
                    LogTest($"找到容器类型装备槽: {slot.SlotName} ({slot.SlotType})");
                }
            }
            
            LogTest($"�7�3 找到 {containerSlotCount} 个容器类型装备槽");
            return true;
        }
        
        /// <summary>
        /// 测试PlayerPrefs操作
        /// </summary>
        private bool TestES3Operations()
        {
            LogTest("--- 测试4: ES3保存系统操作测试 ---");
            
            try
            {
                var manager = ContainerSaveManager.Instance;
                
                // 测试统计信息获取
                string stats = manager.GetContainerStats();
                LogTest($"�9�6 容器统计信息: {stats}");
                
                // 测试手动保存功能
                manager.ManualSave();
                LogTest("�7�3 手动保存功能测试成功");
                
                // 测试手动加载功能
                manager.ManualLoad();
                LogTest("�7�3 手动加载功能测试成功");
                
                // 注意：不在运行时清理数据，以免影响用户保存的容器内容
                if (Application.isEditor && !Application.isPlaying)
                {
                    // 只在编辑器非运行模式下测试清理功能
                    manager.ClearAllContainerData();
                    LogTest("�7�3 ClearAllContainerData 执行成功（仅限编辑器模式）");
                }
                else
                {
                    LogTest("�7�2�1�5 跳过数据清理测试（保护运行时容器数据）");
                }
                
                return true;
            }
            catch (System.Exception e)
            {
                LogTest($"�7�4 ES3操作测试异常: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取系统状态信息
        /// </summary>
        [ContextMenu("显示系统状态")]
        public void ShowSystemStatus()
        {
            LogTest("=== ContainerSaveManager 系统状态 ===");
            
            var manager = ContainerSaveManager.Instance;
            if (manager != null)
            {
                LogTest("�7�3 ContainerSaveManager 实例存在");
                LogTest($"实例对象名称: {manager.gameObject.name}");
                LogTest($"实例是否激活: {manager.gameObject.activeInHierarchy}");
            }
            else
            {
                LogTest("�7�4 ContainerSaveManager 实例不存在");
            }
            
            // 检查装备槽状态
            var equipmentSlots = FindObjectsOfType<EquipmentSlot>();
            LogTest($"当前场景装备槽数量: {equipmentSlots.Length}");
            
            foreach (var slot in equipmentSlots)
            {
                LogTest($"  - {slot.SlotName} ({slot.SlotType}): {slot.GetSlotStatusInfo()}");
            }
        }
        
        /// <summary>
        /// 清理测试数据
        /// </summary>
        [ContextMenu("清理测试数据")]
        public void ClearTestData()
        {
            LogTest("清理所有容器测试数据...");
            
            try
            {
                var manager = ContainerSaveManager.Instance;
                manager.ClearAllContainerData();
                LogTest("�7�3 测试数据清理完成");
            }
            catch (System.Exception e)
            {
                LogTest($"�7�4 清理测试数据失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 记录测试日志
        /// </summary>
        private void LogTest(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[ContainerSaveManagerTester] {message}");
            }
        }
        
        #region 编辑器工具
        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Inventory System/Container Save Manager/Run Tests", false, 500)]
        public static void RunTestsFromMenu()
        {
            var tester = FindObjectOfType<ContainerSaveManagerTester>();
            if (tester == null)
            {
                Debug.LogWarning("场景中未找到 ContainerSaveManagerTester 组件");
                return;
            }
            
            tester.RunBasicTests();
        }
        
        [UnityEditor.MenuItem("Tools/Inventory System/Container Save Manager/Show System Status", false, 501)]
        public static void ShowSystemStatusFromMenu()
        {
            var tester = FindObjectOfType<ContainerSaveManagerTester>();
            if (tester == null)
            {
                Debug.LogWarning("场景中未找到 ContainerSaveManagerTester 组件");
                return;
            }
            
            tester.ShowSystemStatus();
        }
        
        [UnityEditor.MenuItem("Tools/Inventory System/Container Save Manager/Clear Test Data", false, 502)]
        public static void ClearTestDataFromMenu()
        {
            var tester = FindObjectOfType<ContainerSaveManagerTester>();
            if (tester == null)
            {
                Debug.LogWarning("场景中未找到 ContainerSaveManagerTester 组件");
                return;
            }
            
            tester.ClearTestData();
        }
#endif
        
        /// <summary>
        /// 测试ItemDatabase和ItemDataSO系统
        /// </summary>
        private System.Collections.IEnumerator TestItemDatabaseSystemCoroutine()
        {
            LogTest("--- 测试5: ItemDatabase和ItemDataSO系统测试 ---");
            
            // 使用实例变量而不是局部变量
            bool databaseTestPassed = true;
            
            // 基础测试部分（在try-catch中，不含yield）
            ItemDataSO item1402 = null;
            bool basicTestsPassed = true;
            
            try
            {
                // 测试ItemDatabase初始化
                if (!ItemDatabase.Instance.IsInitialized)
                {
                    LogTest("ItemDatabase未初始化，尝试初始化...");
                    ItemDatabase.Instance.InitializeDatabase();
                }
                
                if (ItemDatabase.Instance.IsInitialized)
                {
                    LogTest($"�7�3 ItemDatabase已初始化，包含 {ItemDatabase.Instance.TotalItemCount} 个物品");
                }
                else
                {
                    LogTest("�7�4 ItemDatabase初始化失败");
                    databaseTestPassed = false;
                    LogTest($"�7�3 ItemDatabase系统测试完成");
                    LogTest($"=== 测试完成，总体结果: {(databaseTestPassed ? "�7�3 通过" : "�7�4 失败")} ===");
                    yield break;
                }
                
                // 测试已知物品的查找 (ID=1402)
                LogTest("测试特殊物品查找 (ID=1402)...");
                item1402 = FindItemByID(1402);
                if (item1402 != null)
                {
                    LogTest($"�7�3 找到ID=1402的物品: {item1402.itemName} (类别: {item1402.category}, GlobalId: {item1402.GlobalId})");
                }
                else
                {
                    LogTest("�7�4 未找到ID=1402的物品");
                    databaseTestPassed = false;
                }
                
                // 测试几个不同类别的物品
                int[] testIds = { 101, 401, 1401, 1402, 601 }; // 头盔、背包、特殊物品、弹药
                LogTest("测试多个物品类别...");
                
                foreach (int testId in testIds)
                {
                    ItemDataSO testItem = FindItemByID(testId);
                    if (testItem != null)
                    {
                        LogTest($"  ID={testId}: {testItem.itemName} ({testItem.category}, {testItem.width}x{testItem.height}, GlobalId: {testItem.GlobalId})");
                    }
                    else
                    {
                        LogTest($"  ID={testId}: 未找到");
                    }
                }
                
                // 额外测试：显示数据库中前几个物品的ID对应关系
                LogTest("数据库物品ID对应关系示例:");
                var allItems = ItemDatabase.Instance.GetAllItems();
                for (int i = 0; i < Mathf.Min(5, allItems.Count); i++)
                {
                    var item = allItems[i];
                    LogTest($"  示例{i+1}: ID={item.id}, GlobalId={item.GlobalId}, 名称={item.itemName}");
                }
                
                // 测试物品创建能力（模拟创建）
                LogTest("测试物品创建系统兼容性...");
                if (item1402 != null)
                {
                    // 测试能否获取到创建物品所需的基本信息
                    bool hasValidData = !string.IsNullOrEmpty(item1402.itemName) && 
                                       item1402.width > 0 && 
                                       item1402.height > 0;
                    
                    if (hasValidData)
                    {
                        LogTest($"�7�3 物品数据完整，可用于创建实例");
                        LogTest($"  尺寸: {item1402.width}x{item1402.height}");
                        LogTest($"  最大堆叠: {item1402.maxStack}");
                        LogTest($"  GlobalId: {item1402.GlobalId}");
                        
                        // 测试背景颜色和图标是否存在
                        if (item1402.itemIcon != null)
                        {
                            LogTest($"  图标: �7�3 已设置");
                        }
                        else
                        {
                            LogTest($"  图标: �7�4 未设置");
                        }
                    }
                    else
                    {
                        LogTest("�7�4 物品数据不完整");
                        databaseTestPassed = false;
                        basicTestsPassed = false;
                    }
                }
                else
                {
                    basicTestsPassed = false;
                }
                
                LogTest($"�7�3 ItemDatabase基础测试完成");
            }
            catch (System.Exception e)
            {
                LogTest($"�7�4 ItemDatabase系统测试失败: {e.Message}");
                databaseTestPassed = false;
                basicTestsPassed = false;
            }
            
            // 协程测试部分（移出try-catch块）
            if (basicTestsPassed && item1402 != null && ContainerSaveManager.Instance != null)
            {
                LogTest("测试实际物品创建...");
                
                // 将物品创建测试移到协程中
                bool itemCreationResult = false;
                yield return StartCoroutine(TestItemCreation(item1402, result => itemCreationResult = result));
                if (!itemCreationResult)
                {
                    databaseTestPassed = false;
                }
            }
            
            LogTest($"=== 测试完成，总体结果: {(databaseTestPassed ? "�7�3 通过" : "�7�4 失败")} ===");
            yield break;
        }

        /// <summary>
        /// 测试物品创建协程（避免在try-catch中使用yield）
        /// </summary>
        private System.Collections.IEnumerator TestItemCreation(ItemDataSO itemDataSO, System.Action<bool> onComplete)
        {
            GameObject testItem = null;
            bool testPassed = true; // 局部变量记录测试结果
            
            try
            {
                // 创建模拟的保存数据
                var testSaveData = new ItemSaveData
                {
                    itemID = itemDataSO.id.ToString(),
                    categoryID = (int)itemDataSO.category,
                    stackCount = 1,
                    durability = itemDataSO.durability,
                    usageCount = 0,
                    gridPosition = new Vector2Int(0, 0)
                };
                
                // 使用反射调用私有方法LoadItemPrefab
                var createMethod = typeof(ContainerSaveManager).GetMethod("LoadItemPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (createMethod != null)
                {
                    testItem = (GameObject)createMethod.Invoke(ContainerSaveManager.Instance, new object[] { testSaveData });
                }
                else
                {
                    LogTest($"�7�2�1�5 无法访问LoadItemPrefab方法");
                    onComplete?.Invoke(false);
                    yield break;
                }
            }
            catch (System.Exception e)
            {
                LogTest($"�7�4 物品创建测试异常: {e.Message}");
                onComplete?.Invoke(false);
                yield break;
            }
            
            // 检查创建结果（不在try-catch中）
            if (testItem != null)
            {
                LogTest($"�7�3 成功创建测试物品: {testItem.name}");
                
                // 验证组件（等待一帧让Awake完成）
                yield return null;
                
                ItemDataReader reader = testItem.GetComponent<ItemDataReader>();
                Item itemComponent = testItem.GetComponent<Item>();
                DraggableItem draggableComponent = testItem.GetComponent<DraggableItem>();
                CanvasGroup canvasGroup = testItem.GetComponent<CanvasGroup>();
                
                if (reader != null && itemComponent != null && draggableComponent != null && canvasGroup != null)
                {
                    LogTest($"�7�3 物品组件完整: ItemDataReader + Item + DraggableItem + CanvasGroup");
                    LogTest($"  物品名称: {reader.ItemData?.itemName}");
                    LogTest($"  可交互: {canvasGroup.interactable}");
                    LogTest($"  阻止射线: {canvasGroup.blocksRaycasts}");
                    
                    // 检查子组件
                    Transform background = testItem.transform.Find("ItemBackground");
                    Transform icon = testItem.transform.Find("ItemIcon");
                    Transform text = testItem.transform.Find("ItemText");
                    Transform highlight = testItem.transform.Find("ItemHighlight");
                    
                    if (background != null && icon != null && text != null && highlight != null)
                    {
                        LogTest($"�7�3 视觉组件完整: 背景、图标、文字、高亮");
                    }
                    else
                    {
                        LogTest($"�7�4 部分视觉组件缺失");
                        LogTest($"  背景: {background != null}, 图标: {icon != null}, 文字: {text != null}, 高亮: {highlight != null}");
                        testPassed = false;
                    }
                }
                else
                {
                    LogTest($"�7�4 物品交互组件不完整");
                    LogTest($"  ItemDataReader: {reader != null}");
                    LogTest($"  Item: {itemComponent != null}");
                    LogTest($"  DraggableItem: {draggableComponent != null}");
                    LogTest($"  CanvasGroup: {canvasGroup != null}");
                    testPassed = false;
                }
                
                // 清理测试物品
                if (Application.isPlaying)
                {
                    Destroy(testItem);
                }
                else
                {
                    DestroyImmediate(testItem);
                }
            }
            else
            {
                LogTest($"�7�4 物品创建失败");
                testPassed = false;
            }
            
            // 调用回调返回结果
            onComplete?.Invoke(testPassed);
        }
        
        /// <summary>
        /// 通过物品ID查找ItemDataSO（遍历所有物品）
        /// </summary>
        private ItemDataSO FindItemByID(int itemId)
        {
            var allItems = ItemDatabase.Instance.GetAllItems();
            foreach (var item in allItems)
            {
                if (item.id == itemId)
                {
                    return item;
                }
            }
            return null;
        }

        #endregion
    }
}
