using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using GridSystem.Editor;
using InventorySystem.Grid;

namespace GridSystem.Editor
{
    /// <summary>
    /// 背包占用状态历史记录数据结构
    /// </summary>
    [System.Serializable]
    public class BackpackOccupancyHistory
    {
        public string backpackName;           // 背包名称
        public int backpackInstanceID;        // 背包实例ID
        public Vector2Int gridSize;           // 网格尺寸
        public bool[,] occupancyState;        // 占用状态矩阵
        public float occupancyRate;           // 占用率
        public int totalItems;                // 物品总数
        public System.DateTime recordTime;    // 记录时间
        public string backpackDataPath;       // 背包数据路径
        
        public BackpackOccupancyHistory()
        {
            backpackName = "未知背包";
            backpackInstanceID = 0;
            gridSize = Vector2Int.zero;
            occupancyState = null;
            occupancyRate = 0f;
            totalItems = 0;
            recordTime = System.DateTime.Now;
            backpackDataPath = "";
        }
        
        /// <summary>
        /// 从背包网格创建历史记录
        /// </summary>
        public static BackpackOccupancyHistory CreateFromGrid(BackpackItemGrid grid)
        {
            if (grid == null)
            {
                Debug.LogWarning("[BackpackOccupancyHistory] 传入的背包网格为null");
                return new BackpackOccupancyHistory();
            }
            
            var history = new BackpackOccupancyHistory();
            
            try
            {
                history.backpackName = grid.gameObject.name;
                history.backpackInstanceID = grid.GetInstanceID();
                
                // 检查网格尺寸
                history.gridSize = new Vector2Int(grid.Width, grid.Height);
                Debug.Log($"[BackpackOccupancyHistory] 背包网格尺寸: {history.gridSize}");
                
                // 获取占用状态
                history.occupancyState = grid.GetGridOccupancy();
                Debug.Log($"[BackpackOccupancyHistory] 占用状态矩阵: {(history.occupancyState != null ? "有效" : "null")}");
                
                // 获取占用率
                history.occupancyRate = grid.GetBackpackOccupancyRate();
                Debug.Log($"[BackpackOccupancyHistory] 背包占用率: {history.occupancyRate:P1}");
                
                // 获取物品列表
                var placedItems = grid.GetPlacedItems();
                history.totalItems = placedItems?.Count ?? 0;
                Debug.Log($"[BackpackOccupancyHistory] 已放置物品数: {history.totalItems}");
                
                history.recordTime = System.DateTime.Now;
                
                // 尝试获取背包数据路径
                var backpackData = grid.GetCurrentBackpackData();
                if (backpackData != null)
                {
                    history.backpackDataPath = UnityEditor.AssetDatabase.GetAssetPath(backpackData);
                    Debug.Log($"[BackpackOccupancyHistory] 背包数据路径: {history.backpackDataPath}");
                }
                else
                {
                    Debug.Log($"[BackpackOccupancyHistory] 背包数据为null");
                }
                
                Debug.Log($"[BackpackOccupancyHistory] 成功创建历史记录: {history.backpackName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BackpackOccupancyHistory] 创建历史记录时发生错误: {ex.Message}\n{ex.StackTrace}");
            }
            
            return history;
        }
        
        /// <summary>
        /// 检查占用状态是否有效
        /// </summary>
        public bool IsValid()
        {
            bool isValid = occupancyState != null && gridSize.x > 0 && gridSize.y > 0;
            Debug.Log($"[BackpackOccupancyHistory] 有效性检查: 占用状态={occupancyState != null}, 网格尺寸={gridSize}, 结果={isValid}");
            return isValid;
        }
    }

    /// <summary>
    /// 网格动态监控窗口 - 专门显示动态创建的背包/挂具网格状态
    /// 解决用户无法看到动态网格检测状态的问题
    /// 新增功能：记录背包占用状态历史
    /// </summary>
    public class GridDynamicMonitorWindow : EditorWindow
    {
        [MenuItem("工具/网格系统/动态网格监控窗口")]
        public static void ShowWindow()
        {
            GridDynamicMonitorWindow window = GetWindow<GridDynamicMonitorWindow>("动态网格监控");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private Vector2 scrollPosition;
        private bool showStatistics = true;
        private bool showGridList = true;
        private bool showDebugInfo = false;
        private bool showBackpackHistory = true;  // 新增：显示背包历史记录
        private float refreshInterval = 1f;
        private double lastRefreshTime = 0;

        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        
        // 背包占用状态历史记录
        private static List<BackpackOccupancyHistory> backpackHistoryList = new List<BackpackOccupancyHistory>();
        private static BackpackItemGrid lastTrackedBackpack = null;
        private Vector2 historyScrollPosition;

        /// <summary>
        /// 初始化GUI样式
        /// </summary>
        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true
                };
            }
        }

        /// <summary>
        /// 绘制GUI
        /// </summary>
        private void OnGUI()
        {
            InitializeStyles();

            // 自动刷新检查
            if (EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
            {
                Repaint();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 标题
            EditorGUILayout.LabelField("动态网格监控系统", headerStyle);
            EditorGUILayout.Space();

            // 控制面板
            DrawControlPanel();
            EditorGUILayout.Space();

            // 统计信息
            if (showStatistics)
            {
                DrawStatistics();
                EditorGUILayout.Space();
            }

            // 网格列表
            if (showGridList)
            {
                DrawGridList();
                EditorGUILayout.Space();
            }
            
            // 背包历史记录
            if (showBackpackHistory)
            {
                DrawBackpackHistory();
                EditorGUILayout.Space();
            }

            // 调试信息
            if (showDebugInfo)
            {
                DrawDebugInfo();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制控制面板
        /// </summary>
        private void DrawControlPanel()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("控制面板", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // 监控状态
            var stats = GridSystem.Editor.GridDynamicDetectionSystem.Instance.GetStatistics();
            string statusText = stats.IsMonitoring ? "运行中" : "已停止";
            Color statusColor = stats.IsMonitoring ? Color.green : Color.red;

            GUI.color = statusColor;
            EditorGUILayout.LabelField($"状态: {statusText}", EditorStyles.boldLabel);
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            // 控制按钮
            if (stats.IsMonitoring)
            {
                if (GUILayout.Button("停止监控", GUILayout.Width(80)))
                {
                    GridSystem.Editor.GridDynamicDetectionSystem.Instance.StopMonitoring();
                }
            }
            else
            {
                if (GUILayout.Button("开始监控", GUILayout.Width(80)))
                {
                    GridSystem.Editor.GridDynamicDetectionSystem.Instance.StartMonitoring();
                }
            }

            if (GUILayout.Button("强制刷新", GUILayout.Width(80)))
            {
                GridSystem.Editor.GridDynamicDetectionSystem.Instance.ForceRefresh();
            }

            EditorGUILayout.EndHorizontal();

            // 刷新间隔设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("刷新间隔:", GUILayout.Width(60));
            refreshInterval = EditorGUILayout.Slider(refreshInterval, 0.1f, 5f);
            EditorGUILayout.LabelField("秒", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            // 显示选项
            EditorGUILayout.BeginHorizontal();
            showStatistics = EditorGUILayout.Toggle("显示统计", showStatistics);
            showGridList = EditorGUILayout.Toggle("显示网格列表", showGridList);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            showBackpackHistory = EditorGUILayout.Toggle("显示背包历史", showBackpackHistory);
            showDebugInfo = EditorGUILayout.Toggle("显示调试信息", showDebugInfo);
            
            // 清空历史记录按钮
            if (GUILayout.Button("清空历史", GUILayout.Width(80)))
            {
                backpackHistoryList.Clear();
                lastTrackedBackpack = null;
                Debug.Log("背包占用状态历史记录已清空");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制统计信息
        /// </summary>
        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);

            var stats = GridSystem.Editor.GridDynamicDetectionSystem.Instance.GetStatistics();

            EditorGUILayout.LabelField($"总动态网格数: {stats.TotalDynamicGrids}");
            EditorGUILayout.LabelField($"背包网格数: {stats.BackpackGrids}");
            EditorGUILayout.LabelField($"战术挂具网格数: {stats.TacticalRigGrids}");
            EditorGUILayout.LabelField($"仓库网格数: {stats.WarehouseGrids}");

            if (stats.LastScanTime > 0)
            {
                System.DateTime lastScan = System.DateTime.FromOADate(stats.LastScanTime / 86400.0 + 25569.0);
                EditorGUILayout.LabelField($"最后扫描时间: {lastScan:HH:mm:ss}");
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制网格列表
        /// </summary>
        private void DrawGridList()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("动态网格列表", EditorStyles.boldLabel);

            var dynamicGrids = GridSystem.Editor.GridDynamicDetectionSystem.Instance.GetTrackedDynamicGrids();
            
            // 检查背包变化并记录历史
            CheckAndRecordBackpackChanges(dynamicGrids);

            if (dynamicGrids.Count == 0)
            {
                EditorGUILayout.LabelField("当前没有检测到动态网格", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var grid in dynamicGrids)
                {
                    if (grid == null) continue;

                    EditorGUILayout.BeginHorizontal(GUI.skin.box);

                    // 网格信息
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"名称: {grid.name}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"类型: {grid.GetType().Name}");
                    EditorGUILayout.LabelField($"实例ID: {grid.GetInstanceID()}");

                    // 获取网格详细信息
                    try
                    {
                        var gridInfo = grid.GetGridDetectorInfo();
                        EditorGUILayout.LabelField($"尺寸: {gridInfo.gridSize.x} x {gridInfo.gridSize.y}");
                        EditorGUILayout.LabelField($"已放置物品: {gridInfo.placedItemsCount}");
                        EditorGUILayout.LabelField($"占用率: {gridInfo.occupancyRate:P1}");

                        // 绘制网格可视化
                        EditorGUILayout.Space(5);
                        DrawGridVisualization(grid, gridInfo.gridSize);
                    }
                    catch (System.Exception ex)
                    {
                        EditorGUILayout.LabelField($"获取信息失败: {ex.Message}", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    // 操作按钮
                    EditorGUILayout.BeginVertical(GUILayout.Width(80));

                    if (GUILayout.Button("选择", GUILayout.Height(20)))
                    {
                        Selection.activeGameObject = grid.gameObject;
                        EditorGUIUtility.PingObject(grid.gameObject);
                    }

                    if (GUILayout.Button("定位", GUILayout.Height(20)))
                    {
                        SceneView.FrameLastActiveSceneView();
                        Selection.activeGameObject = grid.gameObject;
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制网格可视化显示
        /// </summary>
        /// <param name="grid">要显示的网格</param>
        /// <param name="gridSize">网格尺寸</param>
        private void DrawGridVisualization(BaseItemGrid grid, Vector2Int gridSize)
        {
            if (grid == null || gridSize.x <= 0 || gridSize.y <= 0)
                return;

            try
            {
                // 获取网格占用状态
                bool[,] occupancy = grid.GetGridOccupancy();
                if (occupancy == null)
                    return;

                // 计算网格显示的尺寸
                float maxDisplaySize = 150f; // 最大显示尺寸
                float cellDisplaySize = Mathf.Min(maxDisplaySize / Mathf.Max(gridSize.x, gridSize.y), 8f);
                cellDisplaySize = Mathf.Max(cellDisplaySize, 2f); // 最小格子尺寸

                EditorGUILayout.LabelField("网格占用状态:", EditorStyles.miniLabel);

                // 开始绘制网格
                Rect gridRect = GUILayoutUtility.GetRect(gridSize.x * cellDisplaySize, gridSize.y * cellDisplaySize);

                // 绘制网格背景
                EditorGUI.DrawRect(gridRect, new Color(0.3f, 0.3f, 0.3f, 1f));

                // 绘制每个格子
                for (int y = 0; y < gridSize.y; y++)
                {
                    for (int x = 0; x < gridSize.x; x++)
                    {
                        Rect cellRect = new Rect(
                            gridRect.x + x * cellDisplaySize,
                            gridRect.y + y * cellDisplaySize,
                            cellDisplaySize - 1f, // 留出1像素的间隔
                            cellDisplaySize - 1f
                        );

                        // 根据占用状态选择颜色
                        Color cellColor;
                        if (x < occupancy.GetLength(0) && y < occupancy.GetLength(1) && occupancy[x, y])
                        {
                            cellColor = Color.red; // 占用的格子显示红色
                        }
                        else
                        {
                            cellColor = Color.white; // 空闲的格子显示白色
                        }

                        EditorGUI.DrawRect(cellRect, cellColor);
                    }
                }

                // 绘制网格线
                Handles.color = Color.black;
                for (int x = 0; x <= gridSize.x; x++)
                {
                    Vector3 start = new Vector3(gridRect.x + x * cellDisplaySize, gridRect.y, 0);
                    Vector3 end = new Vector3(gridRect.x + x * cellDisplaySize, gridRect.y + gridSize.y * cellDisplaySize, 0);
                    Handles.DrawLine(start, end);
                }
                for (int y = 0; y <= gridSize.y; y++)
                {
                    Vector3 start = new Vector3(gridRect.x, gridRect.y + y * cellDisplaySize, 0);
                    Vector3 end = new Vector3(gridRect.x + gridSize.x * cellDisplaySize, gridRect.y + y * cellDisplaySize, 0);
                    Handles.DrawLine(start, end);
                }

                // 添加图例说明
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("图例:", EditorStyles.miniLabel, GUILayout.Width(30));

                // 红色方块 - 占用
                Rect redLegend = GUILayoutUtility.GetRect(12, 12);
                EditorGUI.DrawRect(redLegend, Color.red);
                EditorGUILayout.LabelField("占用", EditorStyles.miniLabel, GUILayout.Width(30));

                GUILayout.Space(10);

                // 白色方块 - 空闲
                Rect whiteLegend = GUILayoutUtility.GetRect(12, 12);
                EditorGUI.DrawRect(whiteLegend, Color.white);
                EditorGUI.DrawRect(new Rect(whiteLegend.x, whiteLegend.y, whiteLegend.width, whiteLegend.height), Color.black); // 黑色边框
                EditorGUILayout.LabelField("空闲", EditorStyles.miniLabel, GUILayout.Width(30));

                EditorGUILayout.EndHorizontal();
            }
            catch (System.Exception ex)
            {
                EditorGUILayout.LabelField($"网格可视化错误: {ex.Message}", EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 绘制调试信息
        /// </summary>
        private void DrawDebugInfo()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("调试信息", EditorStyles.boldLabel);

            // 主监控系统状态
            var mainStats = GridMonitorDataManager.Instance.GetSystemStatistics();
            EditorGUILayout.LabelField($"主监控系统 - 总网格数: {mainStats.TotalGrids}");
            EditorGUILayout.LabelField($"主监控系统 - 活跃网格数: {mainStats.ActiveGrids}");
            EditorGUILayout.LabelField($"主监控系统 - 总刷新次数: {mainStats.TotalRefreshCount}");

            EditorGUILayout.Space();

            // 所有网格对象（用于对比）
            var allGrids = UnityEngine.Object.FindObjectsOfType<BaseItemGrid>();
            EditorGUILayout.LabelField($"场景中所有网格数: {allGrids.Length}");

            if (allGrids.Length > 0)
            {
                EditorGUILayout.LabelField("所有网格类型分布:");
                var typeGroups = allGrids.GroupBy(g => g.GetType().Name);
                foreach (var group in typeGroups)
                {
                    EditorGUILayout.LabelField($"  {group.Key}: {group.Count()}");
                }
            }

            EditorGUILayout.Space();

            // 系统信息
            EditorGUILayout.LabelField($"当前时间: {System.DateTime.Now:HH:mm:ss}");
            EditorGUILayout.LabelField($"编辑器运行时间: {EditorApplication.timeSinceStartup:F1}秒");
            EditorGUILayout.LabelField($"播放模式: {(EditorApplication.isPlaying ? "是" : "否")}");

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 窗口获得焦点时
        /// </summary>
        private void OnFocus()
        {
            // 确保动态检测系统正在运行
            if (!GridSystem.Editor.GridDynamicDetectionSystem.Instance.GetStatistics().IsMonitoring)
            {
                GridSystem.Editor.GridDynamicDetectionSystem.Instance.StartMonitoring();
            }
        }

        /// <summary>
        /// 窗口失去焦点时
        /// </summary>
        private void OnLostFocus()
        {
            // 可以选择在失去焦点时停止监控以节省性能
            // GridDynamicDetectionSystem.Instance.StopMonitoring();
        }

        /// <summary>
        /// 检查并记录背包变化
        /// </summary>
        private void CheckAndRecordBackpackChanges(List<BaseItemGrid> dynamicGrids)
        {
            // 调试信息：显示当前检测到的网格
            int backpackCount = 0;
            foreach (var grid in dynamicGrids)
            {
                if (grid is BackpackItemGrid)
                {
                    backpackCount++;
                }
            }
            
            // 查找当前的背包网格
            BackpackItemGrid currentBackpack = null;
            foreach (var grid in dynamicGrids)
            {
                if (grid is BackpackItemGrid backpackGrid)
                {
                    currentBackpack = backpackGrid;
                    Debug.Log($"[背包历史记录] 检测到背包网格: {backpackGrid.name}, 实例ID: {backpackGrid.GetInstanceID()}");
                    break;
                }
            }
            
            // 调试信息：显示当前状态
            if (currentBackpack == null)
            {
                Debug.Log($"[背包历史记录] 当前没有检测到背包网格，总网格数: {dynamicGrids.Count}, 背包网格数: {backpackCount}");
            }
            else
            {
                Debug.Log($"[背包历史记录] 当前背包: {currentBackpack.name}, 上次跟踪: {(lastTrackedBackpack?.name ?? "无")}");
            }
            
            // 如果当前背包与上次跟踪的不同，记录上次背包的状态
            if (currentBackpack != lastTrackedBackpack)
            {
                Debug.Log($"[背包历史记录] 背包发生变化，准备记录历史状态");
                
                // 记录上一个背包的占用状态（如果存在且有效）
                if (lastTrackedBackpack != null && lastTrackedBackpack.gameObject != null)
                {
                    Debug.Log($"[背包历史记录] 开始记录上一个背包的状态: {lastTrackedBackpack.name}");
                    
                    try
                    {
                        var history = BackpackOccupancyHistory.CreateFromGrid(lastTrackedBackpack);
                        Debug.Log($"[背包历史记录] 创建历史记录成功，有效性: {history.IsValid()}");
                        
                        if (history.IsValid())
                        {
                            // 避免重复记录相同的背包
                            bool isDuplicate = backpackHistoryList.Any(h => 
                                h.backpackInstanceID == history.backpackInstanceID && 
                                System.Math.Abs((h.recordTime - history.recordTime).TotalSeconds) < 2.0);
                                
                            Debug.Log($"[背包历史记录] 重复检查结果: {isDuplicate}");
                                
                            if (!isDuplicate)
                            {
                                backpackHistoryList.Add(history);
                                Debug.Log($"[背包历史记录] 成功记录背包占用状态历史: {history.backpackName}, 占用率: {history.occupancyRate:P1}, 物品数: {history.totalItems}");
                                
                                // 限制历史记录数量，避免内存过度使用
                                if (backpackHistoryList.Count > 10)
                                {
                                    backpackHistoryList.RemoveAt(0);
                                    Debug.Log($"[背包历史记录] 移除最旧的历史记录，当前记录数: {backpackHistoryList.Count}");
                                }
                            }
                            else
                            {
                                Debug.Log($"[背包历史记录] 跳过重复记录");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[背包历史记录] 历史记录无效，跳过记录");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[背包历史记录] 创建历史记录时发生错误: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                else
                {
                    Debug.Log($"[背包历史记录] 上一个背包为空或已销毁，无需记录");
                }
                
                // 更新当前跟踪的背包
                lastTrackedBackpack = currentBackpack;
                Debug.Log($"[背包历史记录] 更新当前跟踪背包: {(currentBackpack?.name ?? "无")}");
            }
            else
            {
                Debug.Log($"[背包历史记录] 背包未发生变化，无需记录");
            }
        }
        
        /// <summary>
        /// 绘制背包历史记录
        /// </summary>
        private void DrawBackpackHistory()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("背包占用状态历史记录", EditorStyles.boldLabel);
            
            if (backpackHistoryList.Count == 0)
            {
                EditorGUILayout.LabelField("暂无背包历史记录", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField("当背包从装备栏拿起时，会自动记录其占用状态", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"共有 {backpackHistoryList.Count} 条历史记录:", EditorStyles.miniLabel);
                EditorGUILayout.Space(5);
                
                historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition, GUILayout.MaxHeight(200));
                
                // 按时间倒序显示（最新的在前）
                var sortedHistory = backpackHistoryList.OrderByDescending(h => h.recordTime).ToList();
                
                foreach (var history in sortedHistory)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    
                    // 背包基本信息
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"背包: {history.backpackName}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"记录时间: {history.recordTime:HH:mm:ss}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    // 背包统计信息
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"尺寸: {history.gridSize.x}x{history.gridSize.y}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"占用率: {history.occupancyRate:P1}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"物品数: {history.totalItems}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    // 背包数据路径（如果有）
                    if (!string.IsNullOrEmpty(history.backpackDataPath))
                    {
                        EditorGUILayout.LabelField($"数据路径: {history.backpackDataPath}", EditorStyles.miniLabel);
                    }
                    
                    // 绘制历史占用状态可视化
                    if (history.IsValid())
                    {
                        EditorGUILayout.Space(3);
                        DrawHistoryOccupancyVisualization(history);
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制历史占用状态可视化
        /// </summary>
        private void DrawHistoryOccupancyVisualization(BackpackOccupancyHistory history)
        {
            if (!history.IsValid()) return;
            
            try
            {
                // 计算显示尺寸（比当前网格显示稍小）
                float maxDisplaySize = 120f;
                float cellDisplaySize = Mathf.Min(maxDisplaySize / Mathf.Max(history.gridSize.x, history.gridSize.y), 6f);
                cellDisplaySize = Mathf.Max(cellDisplaySize, 1.5f);
                
                EditorGUILayout.LabelField("历史占用状态:", EditorStyles.miniLabel);
                
                Rect gridRect = GUILayoutUtility.GetRect(history.gridSize.x * cellDisplaySize, history.gridSize.y * cellDisplaySize);
                
                // 绘制背景
                EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f, 1f));
                
                // 绘制每个格子
                for (int y = 0; y < history.gridSize.y; y++)
                {
                    for (int x = 0; x < history.gridSize.x; x++)
                    {
                        Rect cellRect = new Rect(
                            gridRect.x + x * cellDisplaySize,
                            gridRect.y + y * cellDisplaySize,
                            cellDisplaySize - 0.5f,
                            cellDisplaySize - 0.5f
                        );
                        
                        Color cellColor;
                        if (x < history.occupancyState.GetLength(0) && y < history.occupancyState.GetLength(1) && history.occupancyState[x, y])
                        {
                            cellColor = new Color(1f, 0.6f, 0.6f, 1f); // 历史占用格子显示浅红色
                        }
                        else
                        {
                            cellColor = new Color(0.9f, 0.9f, 0.9f, 1f); // 历史空闲格子显示浅灰色
                        }
                        
                        EditorGUI.DrawRect(cellRect, cellColor);
                    }
                }
                
                // 绘制简化的网格线
                Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                for (int x = 0; x <= history.gridSize.x; x++)
                {
                    Vector3 start = new Vector3(gridRect.x + x * cellDisplaySize, gridRect.y, 0);
                    Vector3 end = new Vector3(gridRect.x + x * cellDisplaySize, gridRect.y + history.gridSize.y * cellDisplaySize, 0);
                    Handles.DrawLine(start, end);
                }
                for (int y = 0; y <= history.gridSize.y; y++)
                {
                    Vector3 start = new Vector3(gridRect.x, gridRect.y + y * cellDisplaySize, 0);
                    Vector3 end = new Vector3(gridRect.x + history.gridSize.x * cellDisplaySize, gridRect.y + y * cellDisplaySize, 0);
                    Handles.DrawLine(start, end);
                }
            }
            catch (System.Exception ex)
            {
                EditorGUILayout.LabelField($"历史可视化错误: {ex.Message}", EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 窗口销毁时
        /// </summary>
        private void OnDestroy()
        {
            // 窗口关闭时不停止监控系统，因为其他系统可能还在使用
            // 但可以选择保存历史记录到EditorPrefs中以便下次打开时恢复
        }
    }
}