#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using InventorySystem.SpawnSystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// WarehouseFixedItemManager的自定义检查器
    /// </summary>
    [CustomEditor(typeof(WarehouseFixedItemManager))]
    public class WarehouseFixedItemManagerEditor : UnityEditor.Editor
    {
        private WarehouseFixedItemManager targetManager;
        
        private void OnEnable()
        {
            targetManager = (WarehouseFixedItemManager)base.target;
        }
        
        public override void OnInspectorGUI()
        {
            // 绘制默认Inspector
            base.DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("仓库固定物品管理器", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 显示当前状态信息
            DrawStatusInfo();
            
            EditorGUILayout.Space();
            
            // 操作按钮
            DrawActionButtons();
            
            EditorGUILayout.Space();
            
            // 存档管理
            DrawSaveManagement();
        }
        
        /// <summary>
        /// 绘制状态信息
        /// </summary>
        private void DrawStatusInfo()
        {
            EditorGUILayout.LabelField("当前状态", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (targetManager != null)
                {
                    string saveId = targetManager.GetCurrentSaveGameId();
                    bool hasGenerated = targetManager.HasWarehouseGenerated();
                    var config = targetManager.GetWarehouseConfig();
                    
                    EditorGUILayout.LabelField("存档ID", saveId);
                    
                    // 生成状态用颜色区分
                    GUI.color = hasGenerated ? Color.green : Color.red;
                    EditorGUILayout.LabelField("生成状态", hasGenerated ? "已生成" : "未生成");
                    GUI.color = Color.white;
                    
                    EditorGUILayout.LabelField("配置文件", config != null ? config.configName : "未设置");
                    
                    if (config != null)
                    {
                        var stats = config.GetStatistics();
                        EditorGUILayout.LabelField("配置物品数", stats.totalItems.ToString());
                        EditorGUILayout.LabelField("有效物品数", stats.validItems.ToString());
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("管理器实例不可用", EditorStyles.centeredGreyMiniLabel);
                }
            }
        }
        
        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("操作控制", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 第一行按钮
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 检测存档生成状态按钮
                    GUI.backgroundColor = Color.cyan;
                    if (GUILayout.Button("检测当前存档生成状态"))
                    {
                        DetectCurrentSaveGenerationStatus();
                    }
                    GUI.backgroundColor = Color.white;
                    
                    if (GUILayout.Button("显示存档详情"))
                    {
                        if (targetManager != null)
                        {
                            targetManager.ShowSaveInfo();
                        }
                    }
                }
                
                // 第二行按钮
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("强制生成物品"))
                    {
                        if (targetManager != null)
                        {
                            targetManager.ForceGenerateWarehouseItems();
                            EditorUtility.DisplayDialog("强制生成", "已执行强制生成仓库物品", "确定");
                        }
                    }
                    
                    GUI.backgroundColor = Color.yellow;
                    if (GUILayout.Button("重置生成状态"))
                    {
                        if (EditorUtility.DisplayDialog("确认重置", 
                            "这将重置当前存档的仓库生成状态，\n下次打开仓库时将重新生成物品。\n\n确定要继续吗？", 
                            "确定", "取消"))
                        {
                            if (targetManager != null)
                            {
                                targetManager.ResetWarehouseGenerationStatus();
                                EditorUtility.DisplayDialog("重置完成", "仓库生成状态已重置", "确定");
                            }
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                
                EditorGUILayout.Space();
                
                // 危险操作
                GUI.backgroundColor = Color.red;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("删除当前存档"))
                    {
                        if (targetManager != null)
                        {
                            targetManager.DeleteCurrentSave();
                        }
                    }
                    
                    if (GUILayout.Button("清除所有仓库数据"))
                    {
                        if (targetManager != null)
                        {
                            targetManager.ClearAllWarehouseData();
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }
        
        /// <summary>
        /// 绘制存档管理
        /// </summary>
        private void DrawSaveManagement()
        {
            EditorGUILayout.LabelField("存档管理", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (targetManager != null)
                {
                    // 显示PlayerPrefs中的相关键
                    string saveId = targetManager.GetCurrentSaveGameId();
                    string warehouseKey = $"WarehouseGenerated_{saveId}";
                    string persistentKey = "WarehouseManager_PersistentSaveId";
                    
                    EditorGUILayout.LabelField("PlayerPrefs 存储键:", EditorStyles.miniBoldLabel);
                    
                    // 仓库生成状态键
                    bool hasWarehouseKey = PlayerPrefs.HasKey(warehouseKey);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"• {warehouseKey}");
                        GUI.color = hasWarehouseKey ? Color.green : Color.red;
                        EditorGUILayout.LabelField(hasWarehouseKey ? "存在" : "不存在", GUILayout.Width(50));
                        GUI.color = Color.white;
                        
                        if (hasWarehouseKey)
                        {
                            int value = PlayerPrefs.GetInt(warehouseKey);
                            EditorGUILayout.LabelField($"值: {value}", GUILayout.Width(50));
                        }
                    }
                    
                    // 持久存档ID键
                    bool hasPersistentKey = PlayerPrefs.HasKey(persistentKey);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"• {persistentKey}");
                        GUI.color = hasPersistentKey ? Color.green : Color.red;
                        EditorGUILayout.LabelField(hasPersistentKey ? "存在" : "不存在", GUILayout.Width(50));
                        GUI.color = Color.white;
                        
                        if (hasPersistentKey)
                        {
                            string value = PlayerPrefs.GetString(persistentKey);
                            EditorGUILayout.LabelField($"值: {value}", GUILayout.Width(100));
                        }
                    }
                    
                    EditorGUILayout.Space();
                    
                    // 存档创建时间
                    string timeKey = $"WarehouseManager_SaveCreationTime_{saveId}";
                    if (PlayerPrefs.HasKey(timeKey))
                    {
                        string creationTime = PlayerPrefs.GetString(timeKey);
                        EditorGUILayout.LabelField($"存档创建时间: {creationTime}");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("存档创建时间: 未记录", EditorStyles.centeredGreyMiniLabel);
                    }
                }
            }
        }
        
        /// <summary>
        /// 检测当前存档生成状态
        /// </summary>
        private void DetectCurrentSaveGenerationStatus()
        {
            if (targetManager == null)
            {
                EditorUtility.DisplayDialog("错误", "管理器实例不可用", "确定");
                return;
            }
            
            // 收集详细的状态信息
            string saveId = targetManager.GetCurrentSaveGameId();
            bool hasGenerated = targetManager.HasWarehouseGenerated();
            var config = targetManager.GetWarehouseConfig();
            
            // 检查PlayerPrefs中的相关数据
            string warehouseKey = $"WarehouseGenerated_{saveId}";
            string persistentKey = "WarehouseManager_PersistentSaveId";
            string timeKey = $"WarehouseManager_SaveCreationTime_{saveId}";
            
            bool hasWarehouseKey = PlayerPrefs.HasKey(warehouseKey);
            bool hasPersistentKey = PlayerPrefs.HasKey(persistentKey);
            bool hasTimeKey = PlayerPrefs.HasKey(timeKey);
            
            string statusReport = "=== 仓库存档生成状态检测报告 ===\n\n";
            
            // 基本信息
            statusReport += $"当前存档ID: {saveId}\n";
            statusReport += $"仓库生成状态: {(hasGenerated ? "✅ 已生成" : "❌ 未生成")}\n\n";
            
            // 配置信息
            if (config != null)
            {
                var stats = config.GetStatistics();
                statusReport += $"配置文件: {config.configName}\n";
                statusReport += $"配置物品总数: {stats.totalItems}\n";
                statusReport += $"有效物品数: {stats.validItems}\n";
                statusReport += $"关键物品数: {stats.criticalItems}\n\n";
            }
            else
            {
                statusReport += "配置文件: ❌ 未设置\n\n";
            }
            
            // PlayerPrefs存储状态
            statusReport += "PlayerPrefs 存储状态:\n";
            statusReport += $"• 仓库生成键 ({warehouseKey}): {(hasWarehouseKey ? $"✅ 存在 (值: {PlayerPrefs.GetInt(warehouseKey)})" : "❌ 不存在")}\n";
            statusReport += $"• 持久存档键 ({persistentKey}): {(hasPersistentKey ? $"✅ 存在 (值: {PlayerPrefs.GetString(persistentKey)})" : "❌ 不存在")}\n";
            statusReport += $"• 创建时间键 ({timeKey}): {(hasTimeKey ? $"✅ 存在 (值: {PlayerPrefs.GetString(timeKey)})" : "❌ 不存在")}\n\n";
            
            // 系统建议
            statusReport += "系统建议:\n";
            if (!hasGenerated && config != null)
            {
                statusReport += "• 可以执行物品生成操作\n";
                statusReport += "• 建议先检查目标仓库网格是否存在\n";
            }
            else if (hasGenerated)
            {
                statusReport += "• 仓库已生成过物品，若要重新生成请先重置状态\n";
                statusReport += "• 可以使用'强制生成物品'进行测试\n";
            }
            else
            {
                statusReport += "• 请先设置仓库生成配置文件\n";
            }
            
            // 在控制台也输出详细信息
            Debug.Log($"[WarehouseFixedItemManagerEditor] {statusReport}");
            
            // 显示对话框
            EditorUtility.DisplayDialog("存档生成状态检测", statusReport, "确定");
        }
    }
    
    /// <summary>
    /// 仓库固定物品管理器的菜单项
    /// </summary>
    public static class WarehouseFixedItemManagerMenuItems
    {
        [MenuItem("Tools/Inventory System/Warehouse Fixed Item Manager/Create Manager")]
        public static void CreateWarehouseFixedItemManager()
        {
            GameObject go = new GameObject("WarehouseFixedItemManager");
            go.AddComponent<WarehouseFixedItemManager>();
            Selection.activeGameObject = go;
            
            Debug.Log("仓库固定物品管理器已创建");
        }
        
        [MenuItem("Tools/Inventory System/Warehouse Fixed Item Manager/Detect Save Status")]
        public static void DetectSaveStatus()
        {
            var manager = WarehouseFixedItemManager.Instance;
            if (manager != null)
            {
                string saveId = manager.GetCurrentSaveGameId();
                bool hasGenerated = manager.HasWarehouseGenerated();
                
                string message = $"当前存档ID: {saveId}\n" +
                               $"仓库生成状态: {(hasGenerated ? "已生成" : "未生成")}\n" +
                               $"配置: {(manager.GetWarehouseConfig() != null ? manager.GetWarehouseConfig().configName : "未设置")}";
                
                EditorUtility.DisplayDialog("存档状态", message, "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到仓库固定物品管理器", "确定");
            }
        }
        
        [MenuItem("Tools/Inventory System/Warehouse Fixed Item Manager/Show Save Info")]
        public static void ShowSaveInfo()
        {
            var manager = WarehouseFixedItemManager.Instance;
            if (manager != null)
            {
                manager.ShowSaveInfo();
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到仓库固定物品管理器", "确定");
            }
        }
        
        [MenuItem("Tools/Inventory System/Warehouse Fixed Item Manager/Force Generate Items")]
        public static void ForceGenerateItems()
        {
            var manager = WarehouseFixedItemManager.Instance;
            if (manager != null)
            {
                manager.ForceGenerateWarehouseItems();
                EditorUtility.DisplayDialog("强制生成", "已执行强制生成仓库物品", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "未找到仓库固定物品管理器", "确定");
            }
        }
        
        [MenuItem("Tools/Inventory System/Warehouse Fixed Item Manager/Reset Generation Status")]
        public static void ResetGenerationStatus()
        {
            if (EditorUtility.DisplayDialog("确认重置", 
                "这将重置当前存档的仓库生成状态。\n确定要继续吗？", 
                "确定", "取消"))
            {
                var manager = WarehouseFixedItemManager.Instance;
                if (manager != null)
                {
                    manager.ResetWarehouseGenerationStatus();
                    EditorUtility.DisplayDialog("重置完成", "仓库生成状态已重置", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "未找到仓库固定物品管理器", "确定");
                }
            }
        }
        
        [MenuItem("Tools/Inventory System/Warehouse Fixed Item Manager/Clear All Warehouse Data")]
        public static void ClearAllWarehouseData()
        {
            if (EditorUtility.DisplayDialog("确认清除", 
                "这将清除所有仓库相关数据。\n确定要继续吗？", 
                "确定", "取消"))
            {
                var manager = WarehouseFixedItemManager.Instance;
                if (manager != null)
                {
                    manager.ClearAllWarehouseData();
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "未找到仓库固定物品管理器", "确定");
                }
            }
        }
    }
}
#endif