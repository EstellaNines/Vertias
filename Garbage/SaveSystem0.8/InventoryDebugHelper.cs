// using UnityEngine;
// using InventorySystem;
// using InventorySystem.Save;
// using System.Collections.Generic;
// using System.Linq;

// /// <summary>
// /// 背包系统调试助手 - 用于诊断和修复常见问题
// /// </summary>
// public class InventoryDebugHelper : MonoBehaviour
// {
//     [Header("调试配置")]
//     [SerializeField] private bool enableDebugLogs = true;
//     [SerializeField] private bool autoFixIssues = true;
    
//     [Header("诊断结果")]
//     [SerializeField] private string lastDiagnosisResult = "未执行诊断";
    
//     private void Start()
//     {
//         if (enableDebugLogs)
//         {
//             // 延迟执行诊断，确保所有系统都已初始化
//             Invoke(nameof(RunFullDiagnosis), 1f);
//         }
//     }
    
//     /// <summary>
//     /// 运行完整诊断
//     /// </summary>
//     [ContextMenu("运行完整诊断")]
//     public void RunFullDiagnosis()
//     {
//         Debug.Log("=== 背包系统诊断开始 ===");
        
//         var issues = new List<string>();
        
//         // 1. 检查ItemDatabase
//         CheckItemDatabase(issues);
        
//         // 2. 检查已注册的网格
//         CheckRegisteredGrids(issues);
        
//         // 3. 检查现有物品
//         CheckExistingItems(issues);
        
//         // 4. 检查保存系统
//         CheckSaveSystem(issues);
        
//         // 输出诊断结果
//         if (issues.Count == 0)
//         {
//             lastDiagnosisResult = "✓ 所有系统正常";
//             Debug.Log("✓ 背包系统诊断完成：所有系统正常");
//         }
//         else
//         {
//             lastDiagnosisResult = $"✗ 发现 {issues.Count} 个问题";
//             Debug.LogWarning($"✗ 背包系统诊断完成：发现 {issues.Count} 个问题：");
//             foreach (string issue in issues)
//             {
//                 Debug.LogWarning($"  - {issue}");
//             }
//         }
        
//         // Debug.Log("=== 背包系统诊断结束 ===");
//     }
    
//     /// <summary>
//     /// 检查ItemDatabase
//     /// </summary>
//     private void CheckItemDatabase(List<string> issues)
//     {
//         Debug.Log("→ 检查ItemDatabase...");
        
//         var database = ItemDatabase.Instance;
//         if (database == null)
//         {
//             issues.Add("ItemDatabase实例未找到");
//             return;
//         }
        
//         Debug.Log($"ItemDatabase状态: {database.databaseStatus}");
//         Debug.Log($"已加载物品数量: {database.loadedItemCount}");
        
//         if (database.loadedItemCount == 0)
//         {
//             issues.Add("ItemDatabase未加载任何物品数据");
            
//             if (autoFixIssues)
//             {
//                 Debug.Log("→ 尝试重新初始化ItemDatabase...");
//                 database.Initialize();
                
//                 if (database.loadedItemCount > 0)
//                 {
//                     Debug.Log($"✓ ItemDatabase重新初始化成功，加载了 {database.loadedItemCount} 个物品");
//                 }
//                 else
//                 {
//                     issues.Add("ItemDatabase重新初始化失败");
//                 }
//             }
//         }
        
//         // 测试获取特定物品
//         var testItem = database.GetItemData(1204); // 使用您图片中显示的物品ID
//         if (testItem != null)
//         {
//             Debug.Log($"✓ 成功获取测试物品: {testItem.itemName} (ID: {testItem.id})");
//         }
//         else
//         {
//             issues.Add($"无法获取测试物品 ID: 1204");
//         }
//     }
    
//     /// <summary>
//     /// 检查已注册的网格
//     /// </summary>
//     private void CheckRegisteredGrids(List<string> issues)
//     {
//         Debug.Log("→ 检查已注册的网格...");
        
//         var saveSystem = FindObjectOfType<InventorySaveSystem>();
//         if (saveSystem == null)
//         {
//             issues.Add("InventorySaveSystem实例未找到");
//             return;
//         }
        
//         // 使用反射获取私有字段（仅用于调试）
//         var registeredGridsField = typeof(InventorySaveSystem).GetField("registeredGrids", 
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
//         if (registeredGridsField != null)
//         {
//             var registeredGrids = registeredGridsField.GetValue(saveSystem) as Dictionary<string, ItemGrid>;
            
//             if (registeredGrids != null)
//             {
//                 Debug.Log($"已注册网格数量: {registeredGrids.Count}");
                
//                 foreach (var kvp in registeredGrids)
//                 {
//                     string gridId = kvp.Key;
//                     ItemGrid grid = kvp.Value;
                    
//                     if (string.IsNullOrEmpty(gridId))
//                     {
//                         issues.Add("发现空的GridId");
//                     }
                    
//                     if (grid == null)
//                     {
//                         issues.Add($"GridId '{gridId}' 对应的ItemGrid为null");
//                     }
//                     else
//                     {
//                         Debug.Log($"  网格: {gridId} ({grid.gridSizeWidth}x{grid.gridSizeHeight})");
//                     }
//                 }
//             }
//         }
//     }
    
//     /// <summary>
//     /// 检查现有物品
//     /// </summary>
//     private void CheckExistingItems(List<string> issues)
//     {
//         Debug.Log("→ 检查现有物品...");
        
//         var allItems = FindObjectsOfType<Item>();
//         Debug.Log($"场景中物品数量: {allItems.Length}");
        
//         foreach (var item in allItems)
//         {
//             var reader = item.ItemDataReader;
//             if (reader == null)
//             {
//                 issues.Add($"物品 '{item.name}' 缺少ItemDataReader组件");
//                 continue;
//             }
            
//             if (reader.ItemData == null)
//             {
//                 issues.Add($"物品 '{item.name}' 的ItemData为null");
//                 continue;
//             }
            
//             if (reader.ItemData.id <= 0)
//             {
//                 issues.Add($"物品 '{item.name}' 的ItemId无效: {reader.ItemData.id}");
//             }
            
//             Debug.Log($"  物品: {reader.ItemData.itemName} (ID: {reader.ItemData.id})");
//         }
//     }
    
//     /// <summary>
//     /// 检查保存系统
//     /// </summary>
//     private void CheckSaveSystem(List<string> issues)
//     {
//         Debug.Log("→ 检查保存系统...");
        
//         var saveSystem = FindObjectOfType<InventorySaveSystem>();
//         if (saveSystem == null)
//         {
//             issues.Add("InventorySaveSystem实例未找到");
//             return;
//         }
        
//         // 测试创建ItemSnapshot
//         var testItems = FindObjectsOfType<Item>();
//         if (testItems.Length > 0)
//         {
//             var testItem = testItems[0];
//             var snapshot = ItemSnapshot.CreateFromItem(testItem, "TestGrid");
            
//             if (snapshot == null)
//             {
//                 issues.Add("无法创建ItemSnapshot");
//             }
//             else
//             {
//                 Debug.Log($"测试ItemSnapshot: GridId='{snapshot.GridId}', ItemId={snapshot.ItemId}, Valid={snapshot.IsValid()}");
                
//                 if (!snapshot.IsValid())
//                 {
//                     issues.Add($"创建的ItemSnapshot无效: GridId='{snapshot.GridId}', ItemId={snapshot.ItemId}");
//                 }
//             }
//         }
//     }
    
//     /// <summary>
//     /// 修复ItemDatabase路径问题
//     /// </summary>
//     [ContextMenu("修复ItemDatabase路径")]
//     public void FixItemDatabasePath()
//     {
//         var database = ItemDatabase.Instance;
//         if (database == null)
//         {
//             Debug.LogError("ItemDatabase实例未找到");
//             return;
//         }
        
//         Debug.Log("→ 修复ItemDatabase路径...");
        
//         // 使用反射修改私有字段
//         var pathField = typeof(ItemDatabase).GetField("databasePath", 
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
//         if (pathField != null)
//         {
//             string correctPath = "Assets/InventorySystem/DataBase/ItemScriptableObject";
//             pathField.SetValue(database, correctPath);
            
//             Debug.Log($"✓ 已将数据库路径修改为: {correctPath}");
            
//             // 重新初始化数据库
//             database.Initialize();
            
//             Debug.Log($"✓ 数据库重新初始化完成，加载了 {database.loadedItemCount} 个物品");
//         }
//     }
    
//     /// <summary>
//     /// 清理无效物品
//     /// </summary>
//     [ContextMenu("清理无效物品")]
//     public void CleanupInvalidItems()
//     {
//         Debug.Log("→ 清理无效物品...");
        
//         var allItems = FindObjectsOfType<Item>();
//         int cleanedCount = 0;
        
//         foreach (var item in allItems)
//         {
//             var reader = item.ItemDataReader;
//             if (reader == null || reader.ItemData == null || reader.ItemData.id <= 0)
//             {
//                 Debug.Log($"清理无效物品: {item.name}");
//                 DestroyImmediate(item.gameObject);
//                 cleanedCount++;
//             }
//         }
        
//         Debug.Log($"✓ 清理完成，移除了 {cleanedCount} 个无效物品");
//     }
// }