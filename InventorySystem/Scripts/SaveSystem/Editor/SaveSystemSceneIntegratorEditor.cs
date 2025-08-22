using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using InventorySystem.SaveSystem;

/// <summary>
/// SaveSystemSceneIntegrator的自定义编辑器
/// 提供可视化的场景集成管理界面
/// </summary>
[CustomEditor(typeof(SaveSystemSceneIntegrator))]
public class SaveSystemSceneIntegratorEditor : Editor
{
    private SaveSystemSceneIntegrator sceneIntegrator;
    private bool showSceneConfig = true;
    private bool showIntegrationTargets = true;
    private bool showSyncConfig = true;
    private bool showIntegrationStats = true;
    private bool showSceneDataCache = false;
    private bool showAdvancedOptions = false;

    // 样式缓存
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private GUIStyle statusStyle;
    private GUIStyle errorStyle;
    private GUIStyle successStyle;

    private void OnEnable()
    {
        sceneIntegrator = (SaveSystemSceneIntegrator)target;
    }

    public override void OnInspectorGUI()
    {
        // 初始化样式
        InitializeStyles();

        serializedObject.Update();

        // 标题
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("保存系统场景集成器", headerStyle);
        EditorGUILayout.Space();

        // 集成状态显示
        DrawIntegrationStatus();

        EditorGUILayout.Space();

        // 场景集成配置
        DrawSceneConfig();

        EditorGUILayout.Space();

        // 集成目标配置
        DrawIntegrationTargets();

        EditorGUILayout.Space();

        // 同步配置
        DrawSyncConfig();

        EditorGUILayout.Space();

        // 集成统计
        DrawIntegrationStats();

        EditorGUILayout.Space();

        // 场景数据缓存
        DrawSceneDataCache();

        EditorGUILayout.Space();

        // 高级选项
        DrawAdvancedOptions();

        EditorGUILayout.Space();

        // 操作按钮
        DrawActionButtons();

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
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25
            };
        }

        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 5, 5)
            };
        }

        if (errorStyle == null)
        {
            errorStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = Color.red }
            };
        }

        if (successStyle == null)
        {
            successStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = Color.green }
            };
        }
    }

    /// <summary>
    /// 绘制集成状态
    /// </summary>
    private void DrawIntegrationStatus()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.LabelField("集成状态", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            bool isIntegrated = sceneIntegrator.IsSceneIntegrated();
            string statusText = isIntegrated ? "已集成" : "未集成";
            var statusColor = isIntegrated ? Color.green : Color.yellow;

            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField("当前状态:", statusText, statusStyle);
            GUI.color = originalColor;

            // 显示当前场景信息
            EditorGUILayout.LabelField("当前场景:", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            // 显示系统组件状态
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("系统组件状态:", EditorStyles.boldLabel);

            DrawSystemComponentStatus();
        }
        else
        {
            EditorGUILayout.HelpBox("运行时才能显示详细状态信息", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制系统组件状态
    /// </summary>
    private void DrawSystemComponentStatus()
    {
        var saveManager = FindObjectOfType<SaveManager>();
        var idManager = FindObjectOfType<ItemInstanceIDManager>();
        var deepIntegrator = FindObjectOfType<ItemInstanceIDManagerDeepIntegrator>();
        var autoInitializer = FindObjectOfType<SaveSystemAutoInitializer>();

        DrawComponentStatus("SaveManager", saveManager != null);
        DrawComponentStatus("ItemInstanceIDManager", idManager != null);
        DrawComponentStatus("DeepIntegrator", deepIntegrator != null);
        DrawComponentStatus("AutoInitializer", autoInitializer != null);
    }

    /// <summary>
    /// 绘制组件状态
    /// </summary>
    private void DrawComponentStatus(string componentName, bool exists)
    {
        var originalColor = GUI.color;
        GUI.color = exists ? Color.green : Color.red;

        string status = exists ? "?" : "?";
        EditorGUILayout.LabelField($"  {status} {componentName}", exists ? "存在" : "缺失");

        GUI.color = originalColor;
    }

    /// <summary>
    /// 绘制场景配置
    /// </summary>
    private void DrawSceneConfig()
    {
        showSceneConfig = EditorGUILayout.Foldout(showSceneConfig, "场景集成配置", true);

        if (showSceneConfig)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableAutoSceneIntegration"), new GUIContent("启用自动场景集成", "在场景加载时自动执行集成"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableCrossSceneSync"), new GUIContent("启用跨场景同步", "在场景切换时同步数据"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableSceneDataPersistence"), new GUIContent("启用场景数据持久化", "保持场景数据在场景切换时不丢失"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDebugLogging"), new GUIContent("启用调试日志", "在控制台输出详细的调试信息"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrationDelay"), new GUIContent("集成延迟(秒)", "延迟集成的时间，确保其他组件先加载"));

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制集成目标配置
    /// </summary>
    private void DrawIntegrationTargets()
    {
        showIntegrationTargets = EditorGUILayout.Foldout(showIntegrationTargets, "集成目标配置", true);

        if (showIntegrationTargets)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.LabelField("选择要集成的组件类型:", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateInventoryItems"), new GUIContent("集成库存物品", "自动集成场景中的InventorySystemItem组件"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateItemGrids"), new GUIContent("集成物品网格", "自动集成场景中的BaseItemGrid组件"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateSpawners"), new GUIContent("集成生成器", "自动集成场景中的BaseItemSpawn组件"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateEquipSlots"), new GUIContent("集成装备槽", "自动集成场景中的EquipSlot组件"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("integrateItemDataHolders"), new GUIContent("集成物品数据持有者", "自动集成场景中的ItemDataHolder组件"));

            // 显示当前场景中的组件数量
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("当前场景组件数量:", EditorStyles.boldLabel);

                var items = FindObjectsOfType<InventorySystemItem>();
                var grids = FindObjectsOfType<BaseItemGrid>();
                var spawners = FindObjectsOfType<BaseItemSpawn>();
                var equipSlots = FindObjectsOfType<EquipSlot>();
                var dataHolders = FindObjectsOfType<ItemDataHolder>();

                EditorGUILayout.LabelField($"  库存物品: {items.Length}");
                EditorGUILayout.LabelField($"  物品网格: {grids.Length}");
                EditorGUILayout.LabelField($"  生成器: {spawners.Length}");
                EditorGUILayout.LabelField($"  装备槽: {equipSlots.Length}");
                EditorGUILayout.LabelField($"  数据持有者: {dataHolders.Length}");
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制同步配置
    /// </summary>
    private void DrawSyncConfig()
    {
        showSyncConfig = EditorGUILayout.Foldout(showSyncConfig, "同步配置", true);

        if (showSyncConfig)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("syncOnSceneLoad"), new GUIContent("场景加载时同步", "在场景加载完成后自动执行同步"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("syncOnSceneUnload"), new GUIContent("场景卸载时同步", "在场景卸载前保存数据"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("validateAfterSync"), new GUIContent("同步后验证", "在同步完成后验证数据完整性"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resolveConflictsAutomatically"), new GUIContent("自动解决冲突", "自动解决ID冲突和数据冲突"));

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制集成统计
    /// </summary>
    private void DrawIntegrationStats()
    {
        showIntegrationStats = EditorGUILayout.Foldout(showIntegrationStats, "集成统计", true);

        if (showIntegrationStats)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            if (Application.isPlaying)
            {
                var stats = sceneIntegrator.GetIntegrationStats();

                EditorGUILayout.LabelField("当前集成统计:", EditorStyles.boldLabel);

                if (!string.IsNullOrEmpty(stats.sceneName))
                {
                    EditorGUILayout.LabelField($"场景名称: {stats.sceneName}");
                }

                EditorGUILayout.LabelField($"集成物品: {stats.integratedItems}");
                EditorGUILayout.LabelField($"集成网格: {stats.integratedGrids}");
                EditorGUILayout.LabelField($"集成生成器: {stats.integratedSpawners}");
                EditorGUILayout.LabelField($"集成装备槽: {stats.integratedEquipSlots}");
                EditorGUILayout.LabelField($"集成数据持有者: {stats.integratedDataHolders}");
                EditorGUILayout.LabelField($"解决冲突: {stats.resolvedConflicts}");

                if (stats.validationErrors > 0)
                {
                    var originalColor = GUI.color;
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField($"验证错误: {stats.validationErrors}");
                    GUI.color = originalColor;
                }
                else
                {
                    var originalColor = GUI.color;
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField($"验证错误: {stats.validationErrors}");
                    GUI.color = originalColor;
                }

                if (!string.IsNullOrEmpty(stats.integrationTime))
                {
                    EditorGUILayout.LabelField($"集成时间: {stats.integrationTime}");
                }

                if (!string.IsNullOrEmpty(stats.lastError))
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("最后错误:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(stats.lastError, MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("运行时才能显示统计信息", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制场景数据缓存
    /// </summary>
    private void DrawSceneDataCache()
    {
        showSceneDataCache = EditorGUILayout.Foldout(showSceneDataCache, "场景数据缓存", true);

        if (showSceneDataCache)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            if (Application.isPlaying)
            {
                var sceneDataCache = sceneIntegrator.GetSceneDataCache();

                if (sceneDataCache != null && sceneDataCache.Count > 0)
                {
                    EditorGUILayout.LabelField($"缓存场景数量: {sceneDataCache.Count}", EditorStyles.boldLabel);

                    foreach (var kvp in sceneDataCache)
                    {
                        EditorGUILayout.Space(3);
                        EditorGUILayout.LabelField($"场景: {kvp.Key}", EditorStyles.boldLabel);

                        var sceneData = kvp.Value;
                        EditorGUILayout.LabelField($"  注册物品ID: {sceneData.registeredItemIDs.Count}");
                        EditorGUILayout.LabelField($"  注册网格ID: {sceneData.registeredGridIDs.Count}");
                        EditorGUILayout.LabelField($"  注册生成器ID: {sceneData.registeredSpawnerIDs.Count}");
                        EditorGUILayout.LabelField($"  注册装备槽ID: {sceneData.registeredEquipSlotIDs.Count}");
                        EditorGUILayout.LabelField($"  ID映射: {sceneData.idMappings.Count}");

                        if (!string.IsNullOrEmpty(sceneData.lastSyncTime))
                        {
                            EditorGUILayout.LabelField($"  最后同步: {sceneData.lastSyncTime}");
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("暂无缓存数据");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("运行时才能显示缓存数据", MessageType.Info);
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

            EditorGUILayout.HelpBox("高级选项可能影响系统稳定性，请谨慎使用", MessageType.Warning);

            EditorGUILayout.Space(5);

            // 重置集成状态
            if (Application.isPlaying)
            {
                if (GUILayout.Button("重置集成状态", buttonStyle))
                {
                    if (EditorUtility.DisplayDialog("确认重置", "确定要重置集成状态吗？这将清除当前的集成记录。", "确定", "取消"))
                    {
                        sceneIntegrator.ResetIntegrationState();
                        Debug.Log("[SaveSystemSceneIntegratorEditor] 集成状态已重置");
                    }
                }

                // 清除场景数据缓存
                if (GUILayout.Button("清除场景数据缓存", buttonStyle))
                {
                    if (EditorUtility.DisplayDialog("确认清除", "确定要清除所有场景数据缓存吗？", "确定", "取消"))
                    {
                        sceneIntegrator.ClearSceneDataCache();
                        Debug.Log("[SaveSystemSceneIntegratorEditor] 场景数据缓存已清除");
                    }
                }
            }

            // 查找场景组件
            if (GUILayout.Button("查找场景组件", buttonStyle))
            {
                FindSceneComponents();
            }

            // 验证场景完整性
            if (GUILayout.Button("验证场景完整性", buttonStyle))
            {
                ValidateSceneIntegrity();
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制操作按钮
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // 手动场景集成按钮
        if (Application.isPlaying)
        {
            if (GUILayout.Button("手动场景集成", buttonStyle))
            {
                try
                {
                    sceneIntegrator.ManualSceneIntegration();
                    Debug.Log("[SaveSystemSceneIntegratorEditor] 手动场景集成完成");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SaveSystemSceneIntegratorEditor] 手动场景集成失败: {ex.Message}");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("需要在运行时才能执行集成操作", MessageType.Info);
        }

        // 刷新界面按钮
        if (GUILayout.Button("刷新界面", buttonStyle))
        {
            Repaint();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 查找场景组件
    /// </summary>
    private void FindSceneComponents()
    {
        Debug.Log("[SaveSystemSceneIntegratorEditor] 查找场景组件...");

        var items = FindObjectsOfType<InventorySystemItem>();
        var grids = FindObjectsOfType<BaseItemGrid>();
        var spawners = FindObjectsOfType<BaseItemSpawn>();
        var equipSlots = FindObjectsOfType<EquipSlot>();
        var dataHolders = FindObjectsOfType<ItemDataHolder>();

        Debug.Log($"[SaveSystemSceneIntegratorEditor] 找到组件数量:");
        Debug.Log($"  InventorySystemItem: {items.Length}");
        Debug.Log($"  BaseItemGrid: {grids.Length}");
        Debug.Log($"  BaseItemSpawn: {spawners.Length}");
        Debug.Log($"  EquipSlot: {equipSlots.Length}");
        Debug.Log($"  ItemDataHolder: {dataHolders.Length}");

        string summary = $"场景组件查找完成:\n" +
                        $"库存物品: {items.Length}\n" +
                        $"物品网格: {grids.Length}\n" +
                        $"生成器: {spawners.Length}\n" +
                        $"装备槽: {equipSlots.Length}\n" +
                        $"数据持有者: {dataHolders.Length}";

        EditorUtility.DisplayDialog("场景组件查找结果", summary, "确定");
    }

    /// <summary>
    /// 验证场景完整性
    /// </summary>
    private void ValidateSceneIntegrity()
    {
        Debug.Log("[SaveSystemSceneIntegratorEditor] 开始验证场景完整性...");

        int validationErrors = 0;
        List<string> errorMessages = new List<string>();

        // 验证库存物品
        var items = FindObjectsOfType<InventorySystemItem>();
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.GetItemInstanceID()))
            {
                errorMessages.Add($"库存物品 '{item.name}' 缺少实例ID");
                validationErrors++;
            }
        }

        // 验证物品网格
        var grids = FindObjectsOfType<BaseItemGrid>();
        foreach (var grid in grids)
        {
            if (string.IsNullOrEmpty(grid.GetSaveID()))
            {
                errorMessages.Add($"物品网格 '{grid.name}' 缺少保存ID");
                validationErrors++;
            }
        }

        // 验证生成器
        var spawners = FindObjectsOfType<BaseItemSpawn>();
        foreach (var spawner in spawners)
        {
            if (string.IsNullOrEmpty(spawner.GetSaveID()))
            {
                errorMessages.Add($"生成器 '{spawner.name}' 缺少保存ID");
                validationErrors++;
            }
        }

        // 验证装备槽
        var equipSlots = FindObjectsOfType<EquipSlot>();
        foreach (var slot in equipSlots)
        {
            if (string.IsNullOrEmpty(slot.GetSaveID()))
            {
                errorMessages.Add($"装备槽 '{slot.name}' 缺少保存ID");
                validationErrors++;
            }
        }

        // 验证数据持有者
        var dataHolders = FindObjectsOfType<ItemDataHolder>();
        foreach (var holder in dataHolders)
        {
            if (string.IsNullOrEmpty(holder.GetSaveID()))
            {
                errorMessages.Add($"数据持有者 '{holder.name}' 缺少保存ID");
                validationErrors++;
            }
        }

        // 输出验证结果
        if (validationErrors == 0)
        {
            Debug.Log("[SaveSystemSceneIntegratorEditor] 场景完整性验证通过");
            EditorUtility.DisplayDialog("验证结果", "场景完整性验证通过！", "确定");
        }
        else
        {
            Debug.LogWarning($"[SaveSystemSceneIntegratorEditor] 场景完整性验证失败，发现 {validationErrors} 个错误");

            foreach (string error in errorMessages)
            {
                Debug.LogError($"[SaveSystemSceneIntegratorEditor] {error}");
            }

            string errorSummary = $"发现 {validationErrors} 个验证错误：\n\n";
            errorSummary += string.Join("\n", errorMessages.Take(10)); // 只显示前10个错误

            if (errorMessages.Count > 10)
            {
                errorSummary += $"\n\n... 还有 {errorMessages.Count - 10} 个错误，请查看控制台日志。";
            }

            EditorUtility.DisplayDialog("验证结果", errorSummary, "确定");
        }
    }
}