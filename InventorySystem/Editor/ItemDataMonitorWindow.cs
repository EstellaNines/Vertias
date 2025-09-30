using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using InventorySystem;

public class ItemDataMonitorWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private InventorySystem.ItemCategory selectedCategory = (InventorySystem.ItemCategory)(-1); // -1 表示全部
    private bool autoRefresh = true;
    private double lastRefreshTime;
    private const double REFRESH_INTERVAL = 1.0; // 1秒刷新一次
    
    [MenuItem("Tools/物品数据监控器")]
    public static void ShowWindow()
    {
        GetWindow<ItemDataMonitorWindow>("物品数据监控器");
    }
    
    private void OnEnable()
    {
        lastRefreshTime = EditorApplication.timeSinceStartup;
    }
    
    private void Update()
    {
        if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL)
        {
            Repaint();
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }
    }
    
    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("物品数据监控器只能在运行时使用", MessageType.Info);
            return;
        }
        
        ItemDataMonitor monitor = ItemDataMonitor.Instance;
        if (monitor == null)
        {
            EditorGUILayout.HelpBox("未找到 ItemDataMonitor 实例", MessageType.Warning);
            return;
        }
        
        DrawToolbar(monitor);
        DrawStatistics(monitor);
        DrawCategoryStats(monitor);
        DrawItemList(monitor);
        DrawChangeHistory(monitor);
    }
    
    private void DrawToolbar(ItemDataMonitor monitor)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // 搜索框
        GUILayout.Label("搜索:", GUILayout.Width(40));
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
        
        // 分类过滤
        GUILayout.Label("分类:", GUILayout.Width(40));
        var categories = new List<string> { "全部" };
        categories.AddRange(System.Enum.GetNames(typeof(InventorySystem.ItemCategory)).Select(GetCategoryDisplayName));
        
        int selectedIndex = (int)selectedCategory + 1;
        selectedIndex = EditorGUILayout.Popup(selectedIndex, categories.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(100));
        selectedCategory = (InventorySystem.ItemCategory)(selectedIndex - 1);
        
        GUILayout.FlexibleSpace();
        
        // 自动刷新开关
        autoRefresh = GUILayout.Toggle(autoRefresh, "自动刷新", EditorStyles.toolbarButton);
        
        // 手动刷新按钮
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
        {
            monitor.RefreshMonitoringData();
        }
        
        // 清除历史按钮
        if (GUILayout.Button("清除历史", EditorStyles.toolbarButton))
        {
            monitor.ClearHistory();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawStatistics(ItemDataMonitor monitor)
    {
        EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"物品总数: {monitor.totalItemCount}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"活跃物品: {monitor.activeItemCount}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"网格中物品: {monitor.itemsInGridCount}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"装备栏物品: {monitor.equippedItemCount}", GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawCategoryStats(ItemDataMonitor monitor)
    {
        EditorGUILayout.LabelField("分类统计", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        var categoryStats = monitor.GetCategoryStats();
        
        EditorGUILayout.BeginHorizontal();
        int count = 0;
        foreach (var kvp in categoryStats)
        {
            if (count % 4 == 0 && count > 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            
            EditorGUILayout.LabelField($"{GetCategoryDisplayName(kvp.Key.ToString())}: {kvp.Value}", GUILayout.Width(120));
            count++;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawItemList(ItemDataMonitor monitor)
    {
        EditorGUILayout.LabelField("物品列表", EditorStyles.boldLabel);
        
        var allItems = monitor.GetAllMonitoredItems();
        var filteredItems = allItems.Values.AsEnumerable();
        
        // 应用搜索过滤
        if (!string.IsNullOrEmpty(searchFilter))
        {
            filteredItems = filteredItems.Where(item => 
                item.itemName.ToLower().Contains(searchFilter.ToLower()) ||
                item.itemId.ToString().Contains(searchFilter));
        }
        
        // 应用分类过滤
        if (selectedCategory != (InventorySystem.ItemCategory)(-1))
        {
            filteredItems = filteredItems.Where(item => item.category == selectedCategory);
        }
        
        var itemList = filteredItems.ToList();
        
        EditorGUILayout.LabelField($"显示 {itemList.Count} / {allItems.Count} 个物品");
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box", GUILayout.Height(300));
        
        // 表头
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("名称", EditorStyles.toolbarButton, GUILayout.Width(100));
        EditorGUILayout.LabelField("ID", EditorStyles.toolbarButton, GUILayout.Width(50));
        EditorGUILayout.LabelField("类别", EditorStyles.toolbarButton, GUILayout.Width(80));
        EditorGUILayout.LabelField("稀有度", EditorStyles.toolbarButton, GUILayout.Width(60));
        EditorGUILayout.LabelField("尺寸", EditorStyles.toolbarButton, GUILayout.Width(60));
        EditorGUILayout.LabelField("堆叠", EditorStyles.toolbarButton, GUILayout.Width(50));
        EditorGUILayout.LabelField("耐久", EditorStyles.toolbarButton, GUILayout.Width(50));
        EditorGUILayout.LabelField("使用次数", EditorStyles.toolbarButton, GUILayout.Width(60));
        EditorGUILayout.LabelField("网格位置", EditorStyles.toolbarButton, GUILayout.Width(80));
        EditorGUILayout.LabelField("治疗量", EditorStyles.toolbarButton, GUILayout.Width(50));
        EditorGUILayout.LabelField("情报值", EditorStyles.toolbarButton, GUILayout.Width(50));
        EditorGUILayout.LabelField("货币量", EditorStyles.toolbarButton, GUILayout.Width(50));
        EditorGUILayout.LabelField("售价", EditorStyles.toolbarButton, GUILayout.Width(60));
        EditorGUILayout.LabelField("实例ID", EditorStyles.toolbarButton, GUILayout.Width(80));
        EditorGUILayout.LabelField("更新时间", EditorStyles.toolbarButton, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();
        
        // 物品数据行
        foreach (var item in itemList)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(item.itemName, GUILayout.Width(100));
            EditorGUILayout.LabelField(item.itemId.ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(GetCategoryDisplayName(item.category.ToString()), GUILayout.Width(80));
            EditorGUILayout.LabelField(GetRarityDisplayName(item.rarity), GUILayout.Width(60));
            EditorGUILayout.LabelField($"{item.itemWidth}x{item.itemHeight}", GUILayout.Width(60)); // 修正：使用itemWidth和itemHeight
            EditorGUILayout.LabelField(item.currentStack.ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(item.currentDurability.ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(item.currentUsageCount.ToString(), GUILayout.Width(60));
            EditorGUILayout.LabelField($"{item.gridPosition} ({item.gridName})", GUILayout.Width(80));
            EditorGUILayout.LabelField(item.currentHealAmount.ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(item.intelligenceValue.ToString(), GUILayout.Width(50)); // 修正：使用intelligenceValue
            EditorGUILayout.LabelField(item.currencyAmount.ToString(), GUILayout.Width(50)); // 修正：使用currencyAmount
            EditorGUILayout.LabelField(item.price > 0 ? item.price.ToString() : "-", GUILayout.Width(60));
            EditorGUILayout.LabelField(item.gameObjectInstanceId.ToString(), GUILayout.Width(80)); // 修正：使用gameObjectInstanceId
            EditorGUILayout.LabelField(System.DateTime.FromBinary((long)item.lastUpdateTime).ToString("HH:mm:ss"), GUILayout.Width(120));
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawChangeHistory(ItemDataMonitor monitor)
    {
        EditorGUILayout.LabelField("变化历史", EditorStyles.boldLabel);
        
        var history = monitor.GetChangeHistory();
        
        EditorGUILayout.LabelField($"历史记录数: {history.Count}");
        
        EditorGUILayout.BeginVertical("box", GUILayout.Height(150));
        
        var recentHistory = history.TakeLast(10).Reverse().ToList();
        
        foreach (var record in recentHistory)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(System.DateTime.FromBinary((long)record.timestamp).ToString("HH:mm:ss"), GUILayout.Width(60));
            EditorGUILayout.LabelField(record.itemName, GUILayout.Width(100));
            EditorGUILayout.LabelField(record.changeType, GUILayout.Width(80));
            EditorGUILayout.LabelField($"{record.oldValue} → {record.newValue}", GUILayout.Width(150));
            EditorGUILayout.LabelField(record.description);
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private string GetCategoryDisplayName(string category)
    {
        switch (category)
        {
            case "Helmet": return "头盔";
            case "Armor": return "护甲";
            case "Weapon": return "武器";
            case "Ammunition": return "弹药";
            case "Food": return "食物";
            case "Drink": return "饮料";
            case "Medicine": return "药物";
            case "Intelligence": return "情报";
            case "Currency": return "货币";
            default: return category;
        }
    }
    
    private string GetRarityDisplayName(string rarity)
    {
        switch (rarity)
        {
            case "1": return "普通";
            case "2": return "稀有";
            case "3": return "史诗";
            case "4": return "传说";
            default: return rarity;
        }
    }
}