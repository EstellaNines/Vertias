using UnityEngine;
using UnityEditor;
using InventorySystem.SaveSystem;
using System.Linq;
using System.Collections.Generic;

namespace InventorySystem.SaveSystem.Editor
{
    /// <summary>
    /// ItemInstanceIDManager的自定义编辑器
    /// 提供可视化的管理界面和调试工具
    /// </summary>
    [CustomEditor(typeof(ItemInstanceIDManager))]
    public class ItemInstanceIDManagerEditor : UnityEditor.Editor
    {
        #region 私有字段
        private ItemInstanceIDManager manager;
        private bool showRegisteredIDs = false;
        private bool showActiveConflicts = false;
        private bool showConflictHistory = false;
        private bool showSystemStats = false;
        private Vector2 scrollPosition;
        private string validationReport = "";
        #endregion

        #region Unity编辑器生命周期
        /// <summary>
        /// 启用时初始化
        /// </summary>
        private void OnEnable()
        {
            manager = (ItemInstanceIDManager)target;
        }

        /// <summary>
        /// 绘制Inspector界面
        /// </summary>
        public override void OnInspectorGUI()
        {
            // 绘制默认属性
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ID管理器控制面板", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 绘制控制按钮
            DrawControlButtons();

            EditorGUILayout.Space(10);

            // 绘制信息面板
            DrawInformationPanels();

            // 如果有变化，标记为脏数据
            if (GUI.changed)
            {
                EditorUtility.SetDirty(manager);
            }
        }
        #endregion

        #region 界面绘制方法
        /// <summary>
        /// 绘制控制按钮
        /// </summary>
        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // 执行系统验证按钮
            if (GUILayout.Button("执行系统验证", GUILayout.Height(30)))
            {
                if (Application.isPlaying && manager != null)
                {
                    validationReport = manager.PerformSystemValidation();
                    Debug.Log("系统验证完成，查看报告详情");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在运行时执行系统验证", "确定");
                }
            }

            // 同步场景ID按钮
            if (GUILayout.Button("同步场景ID", GUILayout.Height(30)))
            {
                if (Application.isPlaying && manager != null)
                {
                    bool success = manager.SynchronizeSceneIDs();
                    string message = success ? "场景ID同步成功" : "场景ID同步失败";
                    EditorUtility.DisplayDialog("同步结果", message, "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在运行时执行场景ID同步", "确定");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // 导出ID映射按钮
            if (GUILayout.Button("导出ID映射", GUILayout.Height(25)))
            {
                if (Application.isPlaying && manager != null)
                {
                    string exportData = manager.ExportIDMappingData();
                    if (!string.IsNullOrEmpty(exportData))
                    {
                        string path = EditorUtility.SaveFilePanel("导出ID映射数据", "", "id_mapping", "json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            System.IO.File.WriteAllText(path, exportData);
                            EditorUtility.DisplayDialog("导出成功", $"ID映射数据已导出到: {path}", "确定");
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在运行时执行导出操作", "确定");
                }
            }

            // 导入ID映射按钮
            if (GUILayout.Button("导入ID映射", GUILayout.Height(25)))
            {
                if (Application.isPlaying && manager != null)
                {
                    string path = EditorUtility.OpenFilePanel("导入ID映射数据", "", "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        string importData = System.IO.File.ReadAllText(path);
                        bool success = manager.ImportIDMappingData(importData);
                        string message = success ? "ID映射数据导入成功" : "ID映射数据导入失败";
                        EditorUtility.DisplayDialog("导入结果", message, "确定");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在运行时执行导入操作", "确定");
                }
            }

            EditorGUILayout.EndHorizontal();

            // 危险操作区域
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("危险操作", EditorStyles.boldLabel);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("清理所有数据", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("警告", "此操作将清理所有ID管理数据，无法撤销！\n确定要继续吗？", "确定", "取消"))
                {
                    if (Application.isPlaying && manager != null)
                    {
                        manager.ClearAllData();
                        EditorUtility.DisplayDialog("完成", "所有数据已清理", "确定");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "请在运行时执行清理操作", "确定");
                    }
                }
            }
            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// 绘制信息面板
        /// </summary>
        private void DrawInformationPanels()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请在运行时查看详细信息", MessageType.Info);
                return;
            }

            if (manager == null)
            {
                EditorGUILayout.HelpBox("管理器实例不可用", MessageType.Warning);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));

            // 系统统计信息
            DrawSystemStats();

            EditorGUILayout.Space(10);

            // 已注册ID列表
            DrawRegisteredIDs();

            EditorGUILayout.Space(10);

            // 活跃冲突列表
            DrawActiveConflicts();

            EditorGUILayout.Space(10);

            // 冲突历史
            DrawConflictHistory();

            EditorGUILayout.Space(10);

            // 验证报告
            DrawValidationReport();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制系统统计信息
        /// </summary>
        private void DrawSystemStats()
        {
            showSystemStats = EditorGUILayout.Foldout(showSystemStats, "系统统计信息", true);

            if (showSystemStats)
            {
                EditorGUILayout.BeginVertical("box");

                var allIDs = manager.GetAllRegisteredIDs();
                var activeConflicts = manager.GetActiveConflicts();
                var conflictHistory = manager.GetConflictHistory();

                EditorGUILayout.LabelField($"已注册ID总数: {allIDs.Count}");
                EditorGUILayout.LabelField($"当前活跃冲突: {activeConflicts.Count}");
                EditorGUILayout.LabelField($"历史冲突总数: {conflictHistory.Count}");

                // 按场景统计
                var sceneStats = new System.Collections.Generic.Dictionary<string, int>();
                foreach (string id in allIDs)
                {
                    var sceneIDs = manager.GetSceneIDs(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    if (sceneIDs.Contains(id))
                    {
                        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                        if (sceneStats.ContainsKey(sceneName))
                        {
                            sceneStats[sceneName]++;
                        }
                        else
                        {
                            sceneStats[sceneName] = 1;
                        }
                    }
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("按场景统计:", EditorStyles.boldLabel);
                foreach (var kvp in sceneStats)
                {
                    EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value} 个ID");
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 绘制已注册ID列表
        /// </summary>
        private void DrawRegisteredIDs()
        {
            showRegisteredIDs = EditorGUILayout.Foldout(showRegisteredIDs, "已注册ID列表", true);

            if (showRegisteredIDs)
            {
                EditorGUILayout.BeginVertical("box");

                var allIDs = manager.GetAllRegisteredIDs();

                if (allIDs.Count == 0)
                {
                    EditorGUILayout.LabelField("暂无已注册的ID");
                }
                else
                {
                    EditorGUILayout.LabelField($"共 {allIDs.Count} 个已注册ID:");

                    for (int i = 0; i < Mathf.Min(allIDs.Count, 20); i++) // 限制显示数量
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(30));
                        EditorGUILayout.SelectableLabel(allIDs[i], GUILayout.Height(16));
                        EditorGUILayout.EndHorizontal();
                    }

                    if (allIDs.Count > 20)
                    {
                        EditorGUILayout.LabelField($"... 还有 {allIDs.Count - 20} 个ID");
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 绘制活跃冲突列表
        /// </summary>
        private void DrawActiveConflicts()
        {
            showActiveConflicts = EditorGUILayout.Foldout(showActiveConflicts, "活跃冲突列表", true);

            if (showActiveConflicts)
            {
                EditorGUILayout.BeginVertical("box");

                var activeConflicts = manager.GetActiveConflicts();

                if (activeConflicts.Count == 0)
                {
                    EditorGUILayout.LabelField("暂无活跃冲突");
                }
                else
                {
                    EditorGUILayout.LabelField($"共 {activeConflicts.Count} 个活跃冲突:");

                    foreach (var conflict in activeConflicts)
                    {
                        EditorGUILayout.BeginVertical("helpbox");
                        EditorGUILayout.LabelField($"ID: {conflict.conflictID}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"类型: {conflict.type}");
                        EditorGUILayout.LabelField($"检测时间: {conflict.detectedTime}");
                        EditorGUILayout.LabelField($"解决状态: {(conflict.isResolved ? "已解决" : "未解决")}");

                        if (conflict.conflictObjects != null && conflict.conflictObjects.Count > 0)
                        {
                            EditorGUILayout.LabelField($"涉及对象: {string.Join(", ", conflict.conflictObjects)}");
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 绘制冲突历史
        /// </summary>
        private void DrawConflictHistory()
        {
            showConflictHistory = EditorGUILayout.Foldout(showConflictHistory, "冲突历史", true);

            if (showConflictHistory)
            {
                EditorGUILayout.BeginVertical("box");

                var conflictHistory = manager.GetConflictHistory();

                if (conflictHistory.Count == 0)
                {
                    EditorGUILayout.LabelField("暂无冲突历史");
                }
                else
                {
                    EditorGUILayout.LabelField($"共 {conflictHistory.Count} 个历史冲突:");

                    // 只显示最近的10个冲突
                    var recentConflicts = conflictHistory.OrderByDescending(c => c.detectedTime).Take(10);

                    foreach (var conflict in recentConflicts)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{conflict.detectedTime:HH:mm:ss}", GUILayout.Width(60));
                        EditorGUILayout.LabelField($"{conflict.type}", GUILayout.Width(80));
                        EditorGUILayout.LabelField(conflict.conflictID);
                        EditorGUILayout.EndHorizontal();
                    }

                    if (conflictHistory.Count > 10)
                    {
                        EditorGUILayout.LabelField($"... 还有 {conflictHistory.Count - 10} 个历史冲突");
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 绘制验证报告
        /// </summary>
        private void DrawValidationReport()
        {
            if (!string.IsNullOrEmpty(validationReport))
            {
                EditorGUILayout.LabelField("最新验证报告", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.TextArea(validationReport, GUILayout.Height(150));

                if (GUILayout.Button("清除报告"))
                {
                    validationReport = "";
                }

                EditorGUILayout.EndVertical();
            }
        }
        #endregion
    }

    /// <summary>
    /// ItemInstanceIDManagerIntegrator的自定义编辑器
    /// </summary>
    [CustomEditor(typeof(ItemInstanceIDManagerIntegrator))]
    public class ItemInstanceIDManagerIntegratorEditor : UnityEditor.Editor
    {
        private ItemInstanceIDManagerIntegrator integrator;
        private bool showStats = false;

        private void OnEnable()
        {
            integrator = (ItemInstanceIDManagerIntegrator)target;
        }

        public override void OnInspectorGUI()
        {
            // 绘制默认属性
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("集成器控制面板", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 手动集成按钮
            if (GUILayout.Button("手动执行集成", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    integrator.ManualIntegration();
                    EditorUtility.DisplayDialog("完成", "手动集成已执行", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在运行时执行集成操作", "确定");
                }
            }

            EditorGUILayout.BeginHorizontal();

            // 清理集成状态按钮
            if (GUILayout.Button("清理集成状态", GUILayout.Height(25)))
            {
                integrator.ClearIntegrationState();
                EditorUtility.DisplayDialog("完成", "集成状态已清理", "确定");
            }

            EditorGUILayout.EndHorizontal();

            // 显示统计信息
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                showStats = EditorGUILayout.Foldout(showStats, "集成统计信息", true);

                if (showStats)
                {
                    EditorGUILayout.BeginVertical("box");

                    var stats = integrator.GetIntegrationStats();
                    EditorGUILayout.LabelField($"已集成物品: {stats.totalItemsIntegrated}");
                    EditorGUILayout.LabelField($"已集成网格: {stats.totalGridsIntegrated}");
                    EditorGUILayout.LabelField($"已集成生成器: {stats.totalSpawnersIntegrated}");
                    EditorGUILayout.LabelField($"已集成装备槽: {stats.totalEquipSlotsIntegrated}");
                    EditorGUILayout.LabelField($"已解决冲突: {stats.totalConflictsResolved}");
                    EditorGUILayout.LabelField($"最后集成时间: {stats.lastIntegrationTime}");

                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请在运行时查看集成统计信息", MessageType.Info);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(integrator);
            }
        }
    }
}