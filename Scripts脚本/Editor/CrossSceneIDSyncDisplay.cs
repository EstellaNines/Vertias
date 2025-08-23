using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 跨场景ID同步显示组件
/// 在Editor窗口中显示跨场景ID同步的状态和统计信息
/// </summary>
public static class CrossSceneIDSyncDisplay
{
    // 显示设置
    private static bool showSyncStatistics = true;
    private static bool showIDMappings = true;
    private static bool showPendingItems = true;
    private static bool showReferences = false;
    private static bool autoRefresh = true;
    
    // 滚动位置
    private static Vector2 syncScrollPosition = Vector2.zero;
    private static Vector2 mappingScrollPosition = Vector2.zero;
    private static Vector2 pendingScrollPosition = Vector2.zero;
    private static Vector2 referenceScrollPosition = Vector2.zero;
    
    // 缓存数据
    private static CrossSceneIDManager.SyncStatistics cachedStatistics;
    private static DateTime lastRefreshTime = DateTime.MinValue;
    private static readonly float REFRESH_INTERVAL = 1.0f; // 1秒刷新间隔
    
    // GUI样式缓存
    private static GUIStyle headerStyle;
    private static GUIStyle boxStyle;
    private static GUIStyle buttonStyle;
    private static GUIStyle labelStyle;
    private static GUIStyle warningStyle;
    private static GUIStyle successStyle;
    
    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private static void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
        }
        
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };
        }
        
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 5, 5)
            };
        }
        
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };
        }
        
        if (warningStyle == null)
        {
            warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.yellow },
                fontStyle = FontStyle.Bold
            };
        }
        
        if (successStyle == null)
        {
            successStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.green },
                fontStyle = FontStyle.Bold
            };
        }
    }
    
    /// <summary>
    /// 绘制跨场景ID同步监控面板
    /// </summary>
    public static void DrawCrossSceneIDSync()
    {
        InitializeStyles();
        
        // 首次初始化时加载设置
        if (lastRefreshTime == DateTime.MinValue)
        {
            LoadDisplaySettings();
            RefreshSyncData();
        }
        
        // 检查是否需要刷新数据
        if (autoRefresh && (DateTime.Now - lastRefreshTime).TotalSeconds > REFRESH_INTERVAL)
        {
            RefreshSyncData();
        }
        
        EditorGUILayout.BeginVertical(boxStyle);
        
        // 标题和控制按钮
        DrawHeader();
        
        EditorGUILayout.Space(10);
        
        // 同步统计信息
        if (showSyncStatistics)
        {
            DrawSyncStatistics();
            EditorGUILayout.Space(10);
        }
        
        // ID映射表
        if (showIDMappings)
        {
            DrawIDMappings();
            EditorGUILayout.Space(10);
        }
        
        // 待同步物品
        if (showPendingItems)
        {
            DrawPendingItems();
            EditorGUILayout.Space(10);
        }
        
        // 物品引用
        if (showReferences)
        {
            DrawItemReferences();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 绘制标题和控制按钮
    /// </summary>
    private static void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField("跨场景ID同步监控", headerStyle);
        
        GUILayout.FlexibleSpace();
        
        // 自动刷新切换
        bool newAutoRefresh = GUILayout.Toggle(autoRefresh, "自动刷新", GUILayout.Width(80));
        if (newAutoRefresh != autoRefresh)
        {
            autoRefresh = newAutoRefresh;
            EditorPrefs.SetBool("CrossSceneIDSync_AutoRefresh", autoRefresh);
        }
        
        // 手动刷新按钮
        if (GUILayout.Button("刷新", buttonStyle, GUILayout.Width(60)))
        {
            RefreshSyncData();
        }
        
        // 清除所有映射按钮
        if (GUILayout.Button("清除映射", buttonStyle, GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("确认清除", "确定要清除所有ID映射和引用吗？", "确定", "取消"))
            {
                ClearAllMappings();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 显示选项
        EditorGUILayout.BeginHorizontal();
        bool newShowSyncStatistics = EditorGUILayout.ToggleLeft("同步统计", showSyncStatistics, GUILayout.Width(80));
        bool newShowIDMappings = EditorGUILayout.ToggleLeft("ID映射", showIDMappings, GUILayout.Width(80));
        bool newShowPendingItems = EditorGUILayout.ToggleLeft("待同步物品", showPendingItems, GUILayout.Width(100));
        bool newShowReferences = EditorGUILayout.ToggleLeft("物品引用", showReferences, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        
        // 保存设置变化
        if (newShowSyncStatistics != showSyncStatistics || newShowIDMappings != showIDMappings ||
            newShowPendingItems != showPendingItems || newShowReferences != showReferences)
        {
            showSyncStatistics = newShowSyncStatistics;
            showIDMappings = newShowIDMappings;
            showPendingItems = newShowPendingItems;
            showReferences = newShowReferences;
            SaveDisplaySettings();
        }
    }
    
    /// <summary>
    /// 绘制同步统计信息
    /// </summary>
    private static void DrawSyncStatistics()
    {
        EditorGUILayout.LabelField("同步统计信息", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        // 场景信息
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("当前场景:", GUILayout.Width(80));
        EditorGUILayout.LabelField(cachedStatistics.currentScene, labelStyle);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("前一场景:", GUILayout.Width(80));
        EditorGUILayout.LabelField(cachedStatistics.previousScene, labelStyle);
        EditorGUILayout.EndHorizontal();
        
        // 状态信息
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("切换状态:", GUILayout.Width(80));
        GUIStyle statusStyle = cachedStatistics.isTransitioning ? warningStyle : successStyle;
        string statusText = cachedStatistics.isTransitioning ? "场景切换中" : "正常";
        EditorGUILayout.LabelField(statusText, statusStyle);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 数量统计
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID映射数量:", GUILayout.Width(80));
        EditorGUILayout.LabelField(cachedStatistics.totalMappings.ToString(), labelStyle);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("引用数量:", GUILayout.Width(80));
        EditorGUILayout.LabelField(cachedStatistics.totalReferences.ToString(), labelStyle);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("待同步物品:", GUILayout.Width(80));
        GUIStyle pendingStyle = cachedStatistics.pendingItems > 0 ? warningStyle : successStyle;
        EditorGUILayout.LabelField(cachedStatistics.pendingItems.ToString(), pendingStyle);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        // 操作按钮
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("立即同步", buttonStyle))
        {
            PerformImmediateSync();
        }
        
        if (GUILayout.Button("扫描场景", buttonStyle))
        {
            ScanCurrentScene();
        }
        
        if (cachedStatistics.pendingItems > 0)
        {
            if (GUILayout.Button($"同步待处理物品 ({cachedStatistics.pendingItems})", buttonStyle))
            {
                SyncPendingItems();
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// 绘制ID映射表
    /// </summary>
    private static void DrawIDMappings()
    {
        EditorGUILayout.LabelField("ID映射表", EditorStyles.boldLabel);
        
        var manager = CrossSceneIDManager.Instance;
        if (manager == null)
        {
            EditorGUILayout.HelpBox("CrossSceneIDManager 未初始化", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        if (cachedStatistics.totalMappings == 0)
        {
            EditorGUILayout.LabelField("暂无ID映射", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            mappingScrollPosition = EditorGUILayout.BeginScrollView(mappingScrollPosition, GUILayout.Height(150));
            
            // 获取并显示实际的映射数据
            var mappingDetails = manager.GetMappingDetails();
            
            // 表头
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("旧ID", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("新ID", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("使用次数", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            
            // 映射数据
            foreach (var mapping in mappingDetails)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(mapping.oldID, labelStyle, GUILayout.Width(120));
                EditorGUILayout.LabelField(mapping.newID, labelStyle, GUILayout.Width(120));
                EditorGUILayout.LabelField(mapping.usageCount.ToString(), labelStyle, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 绘制待同步物品
    /// </summary>
    private static void DrawPendingItems()
    {
        EditorGUILayout.LabelField("待同步物品", EditorStyles.boldLabel);
        
        var manager = CrossSceneIDManager.Instance;
        if (manager == null)
        {
            EditorGUILayout.HelpBox("CrossSceneIDManager 未初始化", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        if (cachedStatistics.pendingItems == 0)
        {
            EditorGUILayout.LabelField("暂无待同步物品", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            pendingScrollPosition = EditorGUILayout.BeginScrollView(pendingScrollPosition, GUILayout.Height(120));
            
            // 获取并显示实际的待同步物品信息
            var pendingItems = manager.GetPendingItemsInfo();
            
            // 表头
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("物品ID", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("物品名称", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("类型", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("目标场景", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            
            // 待同步物品数据
            foreach (var item in pendingItems)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(item.itemID, labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField(item.itemName, labelStyle, GUILayout.Width(120));
                EditorGUILayout.LabelField(item.itemType, labelStyle, GUILayout.Width(80));
                EditorGUILayout.LabelField(item.targetScene, labelStyle, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 绘制物品引用
    /// </summary>
    private static void DrawItemReferences()
    {
        EditorGUILayout.LabelField("物品引用", EditorStyles.boldLabel);
        
        var manager = CrossSceneIDManager.Instance;
        if (manager == null)
        {
            EditorGUILayout.HelpBox("CrossSceneIDManager 未初始化", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        if (cachedStatistics.totalReferences == 0)
        {
            EditorGUILayout.LabelField("暂无物品引用", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            referenceScrollPosition = EditorGUILayout.BeginScrollView(referenceScrollPosition, GUILayout.Height(100));
            
            // 获取并显示实际的引用信息
            var referenceInfos = manager.GetAllItemReferences();
            
            // 表头
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("物品ID", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("引用数量", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("引用类型", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            
            // 引用数据
            foreach (var refInfo in referenceInfos)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(refInfo.itemID, labelStyle, GUILayout.Width(120));
                EditorGUILayout.LabelField(refInfo.referenceCount.ToString(), labelStyle, GUILayout.Width(80));
                
                string referenceTypes = string.Join(", ", refInfo.referenceTypes);
                EditorGUILayout.LabelField(referenceTypes, labelStyle, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 刷新同步数据
    /// </summary>
    private static void RefreshSyncData()
    {
        var manager = CrossSceneIDManager.Instance;
        if (manager != null)
        {
            cachedStatistics = manager.GetSyncStatistics();
        }
        else
        {
            cachedStatistics = new CrossSceneIDManager.SyncStatistics
            {
                totalMappings = 0,
                totalReferences = 0,
                pendingItems = 0,
                currentScene = "未知",
                previousScene = "未知",
                isTransitioning = false
            };
        }
        
        lastRefreshTime = DateTime.Now;
    }
    
    /// <summary>
    /// 执行立即同步
    /// </summary>
    private static void PerformImmediateSync()
    {
        var manager = CrossSceneIDManager.Instance;
        if (manager != null)
        {
            manager.SyncPendingItems();
            RefreshSyncData();
            Debug.Log("[CrossSceneIDSyncDisplay] 执行立即同步");
        }
        else
        {
            EditorUtility.DisplayDialog("同步失败", "CrossSceneIDManager 未初始化", "确定");
        }
    }
    
    /// <summary>
    /// 扫描当前场景
    /// </summary>
    private static void ScanCurrentScene()
    {
        var manager = CrossSceneIDManager.Instance;
        if (manager != null)
        {
            manager.ScanCurrentScene();
            RefreshSyncData();
            Debug.Log("[CrossSceneIDSyncDisplay] 扫描当前场景");
        }
        else
        {
            EditorUtility.DisplayDialog("扫描失败", "CrossSceneIDManager 未初始化", "确定");
        }
    }
    
    /// <summary>
    /// 同步待处理物品
    /// </summary>
    private static void SyncPendingItems()
    {
        var manager = CrossSceneIDManager.Instance;
        if (manager != null)
        {
            manager.SyncPendingItems();
            RefreshSyncData();
            Debug.Log("[CrossSceneIDSyncDisplay] 同步待处理物品");
        }
    }
    
    /// <summary>
    /// 清除所有映射
    /// </summary>
    private static void ClearAllMappings()
    {
        var manager = CrossSceneIDManager.Instance;
        if (manager != null)
        {
            manager.ClearAllMappings();
            RefreshSyncData();
            Debug.Log("[CrossSceneIDSyncDisplay] 清除所有映射");
        }
    }
    
    /// <summary>
    /// 加载显示设置
    /// </summary>
    public static void LoadDisplaySettings()
    {
        autoRefresh = EditorPrefs.GetBool("CrossSceneIDSync_AutoRefresh", true);
        showSyncStatistics = EditorPrefs.GetBool("CrossSceneIDSync_ShowStatistics", true);
        showIDMappings = EditorPrefs.GetBool("CrossSceneIDSync_ShowMappings", true);
        showPendingItems = EditorPrefs.GetBool("CrossSceneIDSync_ShowPending", true);
        showReferences = EditorPrefs.GetBool("CrossSceneIDSync_ShowReferences", false);
    }
    
    /// <summary>
    /// 保存显示设置
    /// </summary>
    public static void SaveDisplaySettings()
    {
        EditorPrefs.SetBool("CrossSceneIDSync_AutoRefresh", autoRefresh);
        EditorPrefs.SetBool("CrossSceneIDSync_ShowStatistics", showSyncStatistics);
        EditorPrefs.SetBool("CrossSceneIDSync_ShowMappings", showIDMappings);
        EditorPrefs.SetBool("CrossSceneIDSync_ShowPending", showPendingItems);
        EditorPrefs.SetBool("CrossSceneIDSync_ShowReferences", showReferences);
    }
    
    /// <summary>
    /// 获取同步状态摘要
    /// </summary>
    /// <returns>状态摘要字符串</returns>
    public static string GetSyncStatusSummary()
    {
        RefreshSyncData();
        
        if (cachedStatistics.isTransitioning)
        {
            return "场景切换中";
        }
        
        if (cachedStatistics.pendingItems > 0)
        {
            return $"有 {cachedStatistics.pendingItems} 个待同步物品";
        }
        
        if (cachedStatistics.totalMappings > 0)
        {
            return $"活跃映射: {cachedStatistics.totalMappings}";
        }
        
        return "同步正常";
    }
}