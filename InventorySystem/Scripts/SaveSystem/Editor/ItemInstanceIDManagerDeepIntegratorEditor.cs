using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using InventorySystem.SaveSystem;

/// <summary>
/// ItemInstanceIDManagerDeepIntegrator的自定义编辑器
/// 提供深度集成管理的可视化界面
/// </summary>
[CustomEditor(typeof(ItemInstanceIDManagerDeepIntegrator))]
public class ItemInstanceIDManagerDeepIntegratorEditor : Editor
{
    private ItemInstanceIDManagerDeepIntegrator integrator;
    private bool showIntegrationStats = true;
    private bool showIntegrationConfig = true;
    private bool showTargetConfig = true;
    private bool showCompatibilityConfig = true;
    private bool showIDMigrationMap = false;
    private bool showIntegratedObjects = false;
    private bool showAdvancedOptions = false;

    private Vector2 scrollPosition;
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;

    private void OnEnable()
    {
        integrator = (ItemInstanceIDManagerDeepIntegrator)target;
    }

    public override void OnInspectorGUI()
    {
        InitializeStyles();

        serializedObject.Update();

        EditorGUILayout.Space(10);

        // 标题
        EditorGUILayout.LabelField("ItemInstanceIDManager 深度集成器", headerStyle);
        EditorGUILayout.Space(5);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 集成统计信息
        DrawIntegrationStats();

        EditorGUILayout.Space(10);

        // 集成配置
        DrawIntegrationConfig();

        EditorGUILayout.Space(10);

        // 集成目标配置
        DrawTargetConfig();

        EditorGUILayout.Space(10);

        // 兼容性配置
        DrawCompatibilityConfig();

        EditorGUILayout.Space(10);

        // ID迁移映射
        DrawIDMigrationMap();

        EditorGUILayout.Space(10);

        // 已集成对象列表
        DrawIntegratedObjects();

        EditorGUILayout.Space(10);

        // 高级选项
        DrawAdvancedOptions();

        EditorGUILayout.Space(10);

        // 操作按钮
        DrawActionButtons();

        EditorGUILayout.EndScrollView();

        serializedObject.ApplyModifiedProperties();

        // 自动刷新
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    /// <summary>
    /// 初始化样式
    /// </summary>
    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }
    }

    /// <summary>
    /// 绘制集成统计信息
    /// </summary>
    private void DrawIntegrationStats()
    {
        showIntegrationStats = EditorGUILayout.Foldout(showIntegrationStats, "集成统计信息", true);

        if (showIntegrationStats)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            var stats = integrator.GetIntegrationStats();

            EditorGUILayout.LabelField("基本统计", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"集成对象总数: {stats.totalIntegratedObjects}");
            EditorGUILayout.LabelField($"迁移ID数量: {stats.migratedIDs}");
            EditorGUILayout.LabelField($"解决冲突数量: {stats.resolvedConflicts}");
            EditorGUILayout.LabelField($"验证错误数量: {stats.validationErrors}");
            EditorGUILayout.LabelField($"创建备份数量: {stats.backupCreated}");

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("时间信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"最后集成时间: {stats.lastIntegrationTime}");
            EditorGUILayout.LabelField($"集成版本: {stats.integrationVersion}");

            // 状态指示器
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("系统状态", EditorStyles.boldLabel);

            bool systemValid = integrator.ValidateSystemIntegration();
            Color originalColor = GUI.color;
            GUI.color = systemValid ? Color.green : Color.red;
            EditorGUILayout.LabelField($"系统状态: {(systemValid ? "正常" : "异常")}");
            GUI.color = originalColor;

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制集成配置
    /// </summary>
    private void DrawIntegrationConfig()
    {
        showIntegrationConfig = EditorGUILayout.Foldout(showIntegrationConfig, "集成配置", true);

        if (showIntegrationConfig)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableAutoIntegration"), new GUIContent("启用自动集成"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableScenePersistence"), new GUIContent("启用场景持久化"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableBackwardCompatibility"), new GUIContent("启用向后兼容性"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableSaveLogicUpdate"), new GUIContent("启用保存逻辑更新"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrationDelay"), new GUIContent("集成延迟时间"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDebugLogging"), new GUIContent("启用调试日志"));

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制集成目标配置
    /// </summary>
    private void DrawTargetConfig()
    {
        showTargetConfig = EditorGUILayout.Foldout(showTargetConfig, "集成目标配置", true);

        if (showTargetConfig)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateInventoryItems"), new GUIContent("集成库存物品"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateItemGrids"), new GUIContent("集成物品网格"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateItemSpawners"), new GUIContent("集成物品生成器"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateEquipSlots"), new GUIContent("集成装备槽"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateItemDataHolders"), new GUIContent("集成物品数据持有者"));

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制兼容性配置
    /// </summary>
    private void DrawCompatibilityConfig()
    {
        showCompatibilityConfig = EditorGUILayout.Foldout(showCompatibilityConfig, "兼容性配置", true);

        if (showCompatibilityConfig)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("preserveExistingIDs"), new GUIContent("保留现有ID"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("migrateOldSaveData"), new GUIContent("迁移旧保存数据"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("validateDataIntegrity"), new GUIContent("验证数据完整性"));

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制ID迁移映射
    /// </summary>
    private void DrawIDMigrationMap()
    {
        showIDMigrationMap = EditorGUILayout.Foldout(showIDMigrationMap, "ID迁移映射", true);

        if (showIDMigrationMap)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            var migrationMap = integrator.GetIDMigrationMap();

            if (migrationMap.Count == 0)
            {
                EditorGUILayout.LabelField("暂无ID迁移记录", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"迁移记录数量: {migrationMap.Count}", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                foreach (var migration in migrationMap.Take(10)) // 只显示前10条
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("旧ID:", GUILayout.Width(40));
                    EditorGUILayout.SelectableLabel(migration.Key, GUILayout.Height(16));
                    EditorGUILayout.LabelField("→", GUILayout.Width(20));
                    EditorGUILayout.LabelField("新ID:", GUILayout.Width(40));
                    EditorGUILayout.SelectableLabel(migration.Value, GUILayout.Height(16));
                    EditorGUILayout.EndHorizontal();
                }

                if (migrationMap.Count > 10)
                {
                    EditorGUILayout.LabelField($"... 还有 {migrationMap.Count - 10} 条记录", EditorStyles.centeredGreyMiniLabel);
                }
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制已集成对象列表
    /// </summary>
    private void DrawIntegratedObjects()
    {
        showIntegratedObjects = EditorGUILayout.Foldout(showIntegratedObjects, "已集成对象", true);

        if (showIntegratedObjects)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            if (Application.isPlaying && ItemInstanceIDManager.Instance != null)
            {
                var registeredIDs = ItemInstanceIDManager.Instance.GetAllRegisteredIDs();

                if (registeredIDs.Count == 0)
                {
                    EditorGUILayout.LabelField("暂无已集成对象", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"已注册对象数量: {registeredIDs.Count}", EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);

                    // 按类型分组显示
                    var groupedIDs = registeredIDs.GroupBy(id => GetIDType(id)).ToList();

                    foreach (var group in groupedIDs)
                    {
                        EditorGUILayout.LabelField($"{group.Key}: {group.Count()}个", EditorStyles.boldLabel);

                        foreach (var id in group.Take(5)) // 每个类型只显示前5个
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("  ? ", GUILayout.Width(20));
                            EditorGUILayout.SelectableLabel(id, GUILayout.Height(16));

                            // 检查状态
                            bool isIntegrated = integrator.IsObjectIntegrated(id);
                            Color originalColor = GUI.color;
                            GUI.color = isIntegrated ? Color.green : Color.yellow;
                            EditorGUILayout.LabelField(isIntegrated ? "已集成" : "未集成", GUILayout.Width(50));
                            GUI.color = originalColor;

                            EditorGUILayout.EndHorizontal();
                        }

                        if (group.Count() > 5)
                        {
                            EditorGUILayout.LabelField($"    ... 还有 {group.Count() - 5} 个对象", EditorStyles.centeredGreyMiniLabel);
                        }

                        EditorGUILayout.Space(3);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("请在运行时查看已集成对象", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制高级选项
    /// </summary>
    private void DrawAdvancedOptions()
    {
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "高级选项", true);

        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.LabelField("调试工具", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("验证系统集成", buttonStyle))
            {
                bool isValid = integrator.ValidateSystemIntegration();
                EditorUtility.DisplayDialog("系统验证",
                    isValid ? "系统集成验证通过！" : "系统集成验证失败，请检查控制台日志。",
                    "确定");
            }

            if (GUILayout.Button("清除集成状态", buttonStyle))
            {
                if (EditorUtility.DisplayDialog("确认清除",
                    "确定要清除所有集成状态吗？此操作不可撤销。",
                    "确定", "取消"))
                {
                    integrator.ClearIntegrationState();
                    EditorUtility.DisplayDialog("操作完成", "集成状态已清除。", "确定");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("数据管理", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("导出集成数据", buttonStyle))
            {
                ExportIntegrationData();
            }

            if (GUILayout.Button("导入集成数据", buttonStyle))
            {
                ImportIntegrationData();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制操作按钮
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.LabelField("集成操作", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // 手动集成按钮
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("执行深度集成", buttonStyle, GUILayout.Height(30)))
        {
            integrator.ManualDeepIntegration();
            EditorUtility.DisplayDialog("集成完成", "深度集成已执行完成，请查看控制台日志了解详情。", "确定");
        }
        GUI.enabled = true;

        // 刷新按钮
        if (GUILayout.Button("刷新界面", buttonStyle, GUILayout.Height(30)))
        {
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("深度集成功能需要在运行时执行。", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 获取ID类型
    /// </summary>
    private string GetIDType(string id)
    {
        if (id.StartsWith("Item_")) return "物品";
        if (id.StartsWith("Grid_")) return "网格";
        if (id.StartsWith("Spawner_")) return "生成器";
        if (id.StartsWith("EquipSlot_")) return "装备槽";
        if (id.StartsWith("DataHolder_")) return "数据持有者";
        return "未知";
    }

    /// <summary>
    /// 导出集成数据
    /// </summary>
    private void ExportIntegrationData()
    {
        if (!Application.isPlaying || ItemInstanceIDManager.Instance == null)
        {
            EditorUtility.DisplayDialog("导出失败", "请在运行时导出集成数据。", "确定");
            return;
        }

        string path = EditorUtility.SaveFilePanel("导出集成数据",
            "Assets/InventorySystem/Exports",
            $"integration_data_{System.DateTime.Now:yyyyMMdd_HHmmss}",
            "json");

        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                var exportData = ItemInstanceIDManager.Instance.ExportIDMappingData();
                System.IO.File.WriteAllText(path, exportData);

                EditorUtility.DisplayDialog("导出成功", $"集成数据已导出到：\n{path}", "确定");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("导出失败", $"导出过程中发生错误：\n{ex.Message}", "确定");
            }
        }
    }

    /// <summary>
    /// 导入集成数据
    /// </summary>
    private void ImportIntegrationData()
    {
        if (!Application.isPlaying || ItemInstanceIDManager.Instance == null)
        {
            EditorUtility.DisplayDialog("导入失败", "请在运行时导入集成数据。", "确定");
            return;
        }

        string path = EditorUtility.OpenFilePanel("导入集成数据",
            "Assets/InventorySystem/Exports",
            "json");

        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string importData = System.IO.File.ReadAllText(path);
                bool success = ItemInstanceIDManager.Instance.ImportIDMappingData(importData);

                if (success)
                {
                    EditorUtility.DisplayDialog("导入成功", "集成数据已成功导入。", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("导入失败", "导入数据格式无效或导入过程中发生错误。", "确定");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("导入失败", $"导入过程中发生错误：\n{ex.Message}", "确定");
            }
        }
    }
}